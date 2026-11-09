// Reminder queue recovery and policy in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/reminder-queue-browser');
const executable = process.env.QA_BROWSER_EXECUTABLE;
fs.mkdirSync(evidence, { recursive: true });
const resultFile=path.join(evidence,'result.json');
if(fs.existsSync(resultFile)) fs.renameSync(resultFile,path.join(evidence,'result-previous-'+Date.now()+'.json'));
fs.writeFileSync(resultFile,JSON.stringify({status:'RUNNING'}));
const types = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.txt': 'text/plain', '.json': 'application/json', '.woff2': 'font/woff2', '.ico': 'image/x-icon', '.svg': 'image/svg+xml' };
const server = http.createServer((req, res) => {
  try {
    if (req.method !== 'GET') { res.writeHead(405); return res.end(); }
    const relative = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname);
    let file = path.resolve(out, '.' + relative);
    if (!file.startsWith(out + path.sep) && file !== out) throw Error('path');
    if (file === out) file = path.join(out, 'index.html'); else if (!path.extname(file)) file += '.html';
    if (!fs.existsSync(file)) { res.writeHead(404); return res.end(); }
    file=fs.realpathSync(file);if(!file.startsWith(out+path.sep))throw Error('Static path escaped export');
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream' }); fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404); res.end(); }
});
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
class CDP {
  constructor(socket) { this.socket = socket; this.id = 0; this.pending = new Map(); this.handlers = new Map(); socket.addEventListener('message', event => { const msg = JSON.parse(event.data); if (msg.id) { const item = this.pending.get(msg.id); if (item) { clearTimeout(item.timer); this.pending.delete(msg.id); msg.error ? item.reject(Error(msg.error.message)) : item.resolve(msg.result); } } else this.handlers.get(msg.sessionId + ':' + msg.method)?.(msg.params); }); }
  send(method, params = {}, sessionId) { const id = ++this.id; return new Promise((resolve, reject) => { const timer = setTimeout(() => { this.pending.delete(id); reject(Error('CDP timeout ' + method)); }, 15000); this.pending.set(id, { resolve, reject, timer }); this.socket.send(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) })); }); }
  on(session, name, fn) { this.handlers.set(session + ':' + name, fn); }
}

async function main(){
 const results=[],errors=[],requests=[];let child,socket,cdp,profile,started=false,passed=false,failure=null;
 const selected=process.env.QA_REMINDER_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',invoice='22222222-2222-4222-8222-222222222222',student='33333333-3333-4333-8333-333333333333',base='/api/academies/'+academy;
 const variants=['mixed','empty','cancelled','legacy','missing','denied','readRetry','queued','rejected','unsent','lostAfterCommit','confirmedRefresh','ambiguous503','malformed'];
 try{
  if(!executable||!fs.existsSync(path.join(out,'fee-reminders.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c25-reminders-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const variant of variants){
   const row={id:invoice,studentId:student,invoiceNumber:'USD-DUE',balance:70.15,currency:'USD',dueDate:'2026-11-01',status:'PartiallyPaid'};
   const cancel={...row,id:'cancelled',invoiceNumber:'CANCELLED-HISTORY',balance:900,status:'Cancelled'};let rows=[row,cancel];
   if(variant==='mixed')rows.push({...row,id:'inr',invoiceNumber:'INR-DUE',currency:'INR',balance:200.25});if(variant==='empty')rows=[];if(variant==='cancelled')rows=[cancel];if(variant==='legacy')row.currency='bad currency';if(variant==='missing')delete row.currency;
   const notices=[];let posts=0,commits=0,historyReads=0,releaseRead,releasePost,failedRefresh=false;
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out: '+expression);};
   const click=label=>evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent===${JSON.stringify(label)}).click()`);
   const metric=label=>evaluate(`Array.from(document.querySelectorAll('.reminders-kpis article')).find(n=>n.querySelector('span')?.textContent===${JSON.stringify(label)})?.querySelector('b')?.textContent`);
   const unavailable=async()=>{a.equal(await evaluate(`Boolean(document.querySelector('.reminders-kpis,.reminders-panel,.reminders-history'))`),false);a.doesNotMatch(await evaluate('document.body.textContent'),/49142|5092|localhost/);};
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});
     requests.push({width,variant,method:request.method,path:url.pathname});a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');
     if(request.method==='POST'){
      a.equal(url.pathname,base+'/fee-reminders');posts++;a.equal(posts,1,'No duplicate or replayed queue');a.deepEqual(JSON.parse(request.postData),{invoiceId:variant==='queued'?null:invoice,channel:'InApp'});
      if(variant==='rejected')return reply(400,{message:'Synthetic rejection'});
      if(variant!=='unsent'){notices.push({id:'n',title:'Fee payment reminder',channel:'InApp',status:'Queued',sentAtUtc:null});commits++;}
      if(['unsent','lostAfterCommit'].includes(variant))return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionReset'},sid);
      if(variant==='ambiguous503')return reply(503,{});
      if(variant==='malformed')return reply(200,{message:'No confirmation count'});
      if(variant==='queued'){releasePost=()=>reply(200,{queuedCount:1,message:'Queued 1 fee reminder(s).'});return;}
      return reply(200,{queuedCount:1,message:'Queued 1 fee reminder(s).'});
     }
     a.equal(request.method,'GET');let data;
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['FinanceUser'],displayName:'Synthetic Finance',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic Academy',enabledModulesJson:'["Core","Finance"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname===base+'/invoices/student-options')data=[{id:student,firstName:'Synthetic',lastName:'Learner'}];
     else if(url.pathname===base+'/invoices')data=rows;
     else if(url.pathname===base+'/fee-reminders/history'){
      historyReads++;if(variant==='denied')return reply(403,{});if(variant==='readRetry'&&historyReads===1){releaseRead=()=>reply(503,{});return;}
      if(variant==='confirmedRefresh'&&posts&&!failedRefresh){failedRefresh=true;return reply(503,{});}data=notices;
     }else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);fs.writeFileSync(path.join(evidence,'interception-errors.json'),JSON.stringify(errors,null,2));await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/fee-reminders'},sid);
    if(variant==='denied'){await wait(`document.querySelector('[role="alert"]')?.textContent.includes('does not have access')`);await unavailable();a.equal(posts,0);results.push({width,variant,pass:true,posts,commits,historyReads});continue;}
    if(variant==='readRetry'){const end=Date.now()+7000;while(!releaseRead&&Date.now()<end)await sleep(30);a.equal(typeof releaseRead,'function');await unavailable();await releaseRead();await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be loaded')`);await unavailable();await click('Try again');}
    await wait(`Boolean(document.querySelector('.reminders-kpis'))`);
    if(['empty','cancelled'].includes(variant)){a.equal(await metric('Outstanding'),'0.00');a.equal(await evaluate(`document.querySelector('.reminders-panel header button').disabled`),true);}
    else if(variant==='mixed'){a.match(await metric('Outstanding'),/INR.*200\.25.*USD.*70\.15/);a.doesNotMatch(await evaluate(`document.querySelector('.reminders-panel').textContent`),/CANCELLED-HISTORY|900/);}
    else if(variant==='legacy')a.match(await metric('Outstanding'),/BAD CURRENCY 70\.15/);
    else if(variant==='missing'){a.equal(await metric('Outstanding'),'Currency details unavailable');a.equal(await evaluate(`document.querySelector('.reminders-panel header button').disabled`),true);}
    else a.match(await metric('Outstanding'),/USD.*70\.15/);
    if(['queued','rejected','unsent','lostAfterCommit','confirmedRefresh','ambiguous503','malformed'].includes(variant)){
     await evaluate(`(()=>{const b=${variant==='queued'?"document.querySelector('.reminders-panel header button')":"document.querySelector('.reminders-panel li button')"};b.click();b.click();})()`);
     if(variant==='queued'){const end=Date.now()+7000;while(!releasePost&&Date.now()<end)await sleep(30);a.equal(typeof releasePost,'function');a.equal(posts,1);a.equal(await evaluate(`document.querySelector('.reminders-panel header button').disabled`),true);await releasePost();await wait(`document.querySelector('.reminders-message')?.textContent==='Queued 1 fee reminder(s).'`);a.equal(await metric('Reminders queued'),'1');}
     else if(variant==='rejected'){await wait(`document.querySelector('[role="alert"]')?.textContent.includes('not queued')`);a.equal(await metric('Reminders queued'),'0');a.equal(await evaluate(`document.querySelector('.reminders-panel header button').disabled`),false);}
     else{
      await wait(`document.querySelector('[role="alert"]')?.textContent.includes(${JSON.stringify(variant==='confirmedRefresh'?'confirmed, but reminder records could not be refreshed':'could not be confirmed')})`);await unavailable();a.equal(posts,1);
      await click('Reload reminder records');await wait(`Boolean(document.querySelector('.reminders-kpis'))`);a.equal(posts,1);a.equal(await metric('Reminders queued'),variant==='unsent'?'0':'1');
      if(variant==='confirmedRefresh'){a.match(await evaluate(`document.querySelector('.reminders-message').textContent`),/queue was confirmed/);a.equal(await evaluate(`document.querySelector('.reminders-panel header button').disabled`),false);}
      else{a.equal(await evaluate(`document.querySelector('.reminders-panel header button').disabled`),true);a.match(await evaluate(`document.querySelector('.reminders-message').textContent`),/unconfirmed/);await click('Start a new reminder');await wait(`document.querySelector('.reminders-panel header button').disabled===false`);a.equal(posts,1);}
     }
     a.equal(posts,1);a.equal(commits,['unsent','rejected'].includes(variant)?0:1);
    }else a.equal(posts,0);
    if(['mixed','lostAfterCommit'].includes(variant)){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,variant+'-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    a.equal(await evaluate(`localStorage.getItem('academydesk.accessToken.AcademyAdmin')`),'synthetic-access');results.push({width,variant,pass:true,posts,commits,historyReads});
   }catch(e){results.push({width,variant,pass:false,error:e.message,posts,commits,historyReads});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,28);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c25-reminders-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium with strictly synthetic queue/history commits and faults. No real Identity/SQL/provider/device notification or production acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('28 native reminder policy/read/recovery scenarios PASS; all persistence synthetic, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
