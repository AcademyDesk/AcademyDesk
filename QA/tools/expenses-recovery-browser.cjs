// Expenses currency and guarded submission recovery in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/expenses-recovery-browser');
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
 const selected=process.env.QA_CURRENCY_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 function fixture(){return[
  {id:'inr',description:'General cost',amount:10.10,currency:'INR',category:'General',status:'Recorded',branchId:null,expenseDate:'2026-11-01'},
  {id:'usd',description:'USD teaching cost',amount:5.25,currency:'USD',category:'TeacherPayout',status:'Recorded',branchId:'b',expenseDate:'2026-11-02'},
  {id:'teacher',description:'INR teaching cost',amount:2.15,currency:'INR',category:'TeacherPayout',status:'Recorded',branchId:null,expenseDate:'2026-11-03'}];}
 try{
  if(!executable||!fs.existsSync(path.join(out,'expenses.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c22-expenses-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const variant of ['mixed','empty','legacy','missing','readRetry','branchDenied','saveSuccess','rejected','lostAfterCommit','unsent','unknown503','savedRefreshFailed']){
   const rows=variant==='empty'?[]:fixture();if(variant==='legacy')rows[0].currency='bad unit';if(variant==='missing')delete rows[0].currency;
   let postAttempts=0,commits=0,readFailed=false,expenseReads=0,releasePost;
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+7000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out');};
   const metric=label=>evaluate(`Array.from(document.querySelectorAll('.expenses-kpis article')).find(n=>n.querySelector('span')?.textContent===${JSON.stringify(label)})?.querySelector('b')?.textContent`);
   const click=label=>evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent===${JSON.stringify(label)}).click()`);
   const noReady=async()=>{a.equal(await evaluate(`Boolean(document.querySelector('.expenses-kpis,form'))`),false);a.doesNotMatch(await evaluate('document.body.textContent'),/5092|localhost/);};
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});
     const entry={width,variant,method:request.method,path:url.pathname};requests.push(entry);
     if(request.method==='POST'){
      a.equal(url.pathname,base+'/expenses');postAttempts++;a.equal(postAttempts,1,'No duplicate or replayed submission');const body=JSON.parse(request.postData);a.deepEqual(body,{description:'Native owned cost',amount:25.55,currency:'INR',category:'General',branchId:null,expenseDate:null});
      if(variant==='rejected')return reply(400,{message:'Synthetic validation rejection'});
      if(variant!=='unsent'){rows.push({...body,id:'saved',status:'Recorded',expenseDate:'2026-11-08'});commits++;}
      if(variant==='lostAfterCommit'||variant==='unsent')return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionReset'},sid);
      if(variant==='unknown503')return reply(503,{message:'Synthetic ambiguous server outcome'});
      if(variant==='saveSuccess'){releasePost=()=>reply(201,rows.at(-1));return;}
      return reply(201,rows.at(-1));
     }
     a.equal(request.method,'GET');let data;
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['AcademyAdmin'],displayName:'Synthetic admin',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Finance","MultiBranch"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname===base+'/expenses/branch-options'){if(variant==='branchDenied')return reply(403,{});data=[{id:'b',name:'Synthetic branch'}];}
     else if(url.pathname===base+'/expenses'){
      expenseReads++;
      if(variant==='readRetry'&&!readFailed||variant==='savedRefreshFailed'&&postAttempts&&!readFailed){readFailed=true;return reply(503,{});}data=rows;
     }else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/expenses'},sid);
    if(variant==='branchDenied'){await wait(`document.querySelector('[role="alert"]')?.textContent.includes('do not have access')`);await noReady();results.push({width,variant,pass:true,readGate:true,postAttempts,commits});continue;}
    if(variant==='readRetry'){await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be loaded')`);await noReady();await click('Try again');}
    await wait(`Boolean(document.querySelector('.expenses-kpis'))`);
    if(variant==='empty'){a.equal(await metric('Total recorded'),'0.00');a.equal(await metric('Teacher payouts'),'0.00');}
    else if(variant==='legacy')a.match(await metric('Total recorded'),/BAD UNIT.*10\.10.*INR.*2\.15.*USD.*5\.25/);
    else if(variant==='missing')a.equal(await metric('Total recorded'),'Currency details unavailable');
    else{a.match(await metric('Total recorded'),/INR.*12\.25.*USD.*5\.25/);a.match(await metric('Teacher payouts'),/INR.*2\.15.*USD.*5\.25/);}
    const saving=['saveSuccess','rejected','lostAfterCommit','unsent','unknown503','savedRefreshFailed'].includes(variant);
    if(saving){
     await evaluate(`(()=>{const set=Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set;for(const [selector,value]of [['input[placeholder^="e.g."]','Native owned cost'],['input[placeholder="Amount in INR"]','25.55']]){const input=document.querySelector(selector);set.call(input,value);input.dispatchEvent(new Event('input',{bubbles:true}));input.dispatchEvent(new Event('change',{bubbles:true}));}})()`);
     await wait(`document.querySelector('input[placeholder="Amount in INR"]').value==='25.55'&&!document.querySelector('form button[type="submit"],form .expenses-action').disabled`);
     await evaluate(`(()=>{const form=document.querySelector('form');form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));})()`);
     if(variant==='saveSuccess'){
      const end=Date.now()+7000;while(!releasePost&&Date.now()<end)await sleep(25);a.equal(typeof releasePost,'function');a.equal(postAttempts,1);
      await wait(`document.querySelector('fieldset')?.disabled===true&&document.querySelector('form')?.getAttribute('aria-busy')==='true'`);await releasePost();
      await wait(`document.body.textContent.includes('Expense recorded.')`);a.equal(await evaluate(`document.querySelector('input[placeholder^="e.g."]').value`),'');a.match(await metric('Total recorded'),/INR.*37\.80.*USD.*5\.25/);
     }else if(variant==='rejected'){
      await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be saved')`);a.equal(await evaluate(`document.querySelector('input[placeholder^="e.g."]').value`),'Native owned cost');a.equal(await evaluate(`document.querySelector('fieldset').disabled`),false);
     }else{
      await wait(`document.querySelector('[role="alert"]')?.textContent.includes(${JSON.stringify(variant==='savedRefreshFailed'?'Expense saved, but financial records could not be refreshed':'Save could not be confirmed')})`);await noReady();a.equal(postAttempts,1);
      await click('Reload financial records');await wait(`Boolean(document.querySelector('.expenses-kpis'))`);a.equal(postAttempts,1);
      if(variant==='savedRefreshFailed'){a.equal(await evaluate(`document.querySelector('input[placeholder^="e.g."]').value`),'');a.equal(await evaluate(`document.querySelector('fieldset').disabled`),false);}
      else{
       a.equal(await evaluate(`document.querySelector('input[placeholder^="e.g."]').value`),'Native owned cost');a.equal(await evaluate(`document.querySelector('fieldset').disabled`),true);
       a.equal(await evaluate(`document.querySelectorAll('.expenses-list li').length`),variant==='unsent'?3:4);
       await evaluate(`document.querySelector('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}))`);a.equal(postAttempts,1);
       if(variant==='lostAfterCommit'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,'review-'+width+'.png'),Buffer.from(shot.data,'base64'));}
       await click('Start a new expense');await wait(`document.querySelector('input[placeholder^="e.g."]').value===''&&!document.querySelector('fieldset').disabled`);
      }
     }
     a.equal(postAttempts,1);a.equal(commits,['rejected','unsent'].includes(variant)?0:1);
    }else a.equal(postAttempts,0);
    if(variant==='mixed'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,'mixed-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    a.equal(await evaluate(`localStorage.getItem('academydesk.accessToken.AcademyAdmin')`),'synthetic-access');results.push({width,variant,pass:true,postAttempts,commits,expenseReads});
   }catch(e){results.push({width,variant,pass:false,error:e.message,postAttempts,commits});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,24);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c22-expenses-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium with synthetic expense persistence and faults. No live Identity/HTTP/SQL/audit/device/provider/production qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('24 native Expenses currency and submission recovery scenarios PASS; all persistence synthetic, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
