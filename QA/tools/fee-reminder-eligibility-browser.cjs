// Reminder eligibility in real Chromium. Synthetic APIs; not live Identity/SQL/device acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/fee-reminder-eligibility-browser');
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
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API origin required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 try{
  if(!executable||!fs.existsSync(path.join(out,'fee-reminders.html')))throw Error('Installed Chromium/exact export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c14-reminders-'));
  child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const scenario of ['mixed','cancelled-only','settled-only']){
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;const writes=[];
   const row=(id,changes={})=>({id,studentId:id,invoiceNumber:id,totalAmount:1000,balance:1000,currency:'INR',dueDate:'2020-01-01',status:'Issued',...changes});
   const cancelled=['Cancelled','cancelled','CaNcElLeD'].map((status,i)=>row('cancel-'+i,{status}));
   const settled=[row('settled',{status:'Paid',balance:0}),row('credited',{adjustedAmount:1000,balance:0})];
   const invoices=scenario==='cancelled-only'?cancelled:scenario==='settled-only'?settled:[...cancelled,...settled,row('partial',{adjustedAmount:200,balance:200}),row('usd',{currency:'USD',balance:50})];
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+7000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out: '+expression);};
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});requests.push({method:request.method,path:url.pathname});
     if(request.method==='POST'){a.equal(url.pathname,base+'/fee-reminders');writes.push(JSON.parse(request.postData));return reply(200,{queuedCount:1,message:'Synthetic queued'});}
     a.equal(request.method,'GET');let data;
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['AcademyAdmin'],displayName:'Synthetic admin',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Finance"]'}];
     else if(url.pathname==='/api/portal/announcements'||url.pathname===base+'/fee-reminders/history')data=[];
     else if(url.pathname===base+'/invoices')data=invoices;
     else if(url.pathname===base+'/invoices/student-options')data=invoices.map(x=>({id:x.studentId,firstName:x.id,lastName:'Learner'}));
     else if(url.pathname===base+'/payments')data=[{invoiceId:'partial',amount:600,status:'Reconciled'}];
     else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/fee-reminders'},sid);
    await wait(`Boolean(document.querySelector('.reminders-panel')&&!document.body.textContent.includes('Loading fee reminders'))`);
    a.equal(await evaluate(`document.querySelectorAll('.reminders-panel li').length`),scenario==='mixed'?2:0);
    a.equal(await evaluate(`Array.from(document.querySelectorAll('.reminders-panel li')).some(x=>x.textContent.includes('cancel-'))`),false);
    if(scenario==='mixed'){
     const summary=await evaluate(`document.querySelectorAll('.reminders-kpis article')[1].textContent`);a.match(summary,/200\.00/);a.match(summary,/50\.00/);a.match(summary,/US\$|USD/);a.doesNotMatch(summary,/250\.00/);
     await evaluate(`Array.from(document.querySelectorAll('.reminders-panel li')).find(x=>x.textContent.includes('partial')).querySelector('button').click()`);await wait(`Boolean(document.querySelector('.reminders-panel button')&&!document.querySelector('.reminders-panel button').disabled)`);a.deepEqual(writes,[{invoiceId:'partial',channel:'InApp'}]);
    }else{a.equal(await evaluate(`document.querySelector('.reminders-panel button').disabled`),true);a.equal(writes.length,0);}
    results.push({width,scenario,pass:true,writes:writes.length});
   }catch(e){results.push({width,scenario,pass:false,error:e.message});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});}
  }
  a.equal(results.length,6);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c14-reminders-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Chromium DOM with synthetic API replies, not real Identity/SQL/device or provider-send acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('6 native reminder eligibility cases PASS; 2 synthetic POSTs, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
