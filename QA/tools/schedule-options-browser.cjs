// Scheduling defaults, IST payloads and readiness in Chromium; synthetic API only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/schedule-options-browser');
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
 const selected=process.env.QA_SCHEDULE_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 const variants=['create','denied','malformed','empty','readback','defaults','override','pending'];
 try{
  if(!executable||!fs.existsSync(path.join(out,'schedule.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c36-schedule-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);


  for(const width of [320,1440])for(const role of ['Operations','delegated'])for(const variant of variants){
   let attempts=0,optionReads=0,recordReads=0,holdWrite;
   const writes=[];
   const row={id:'one',batchId:'b',teacherId:'t',branchId:'r',startUtc:'2027-01-01T04:30:00Z',endUtc:'2027-01-01T05:30:00Z',deliveryMode:'InPerson',roomName:null,status:'Scheduled'};
   const stored=[row];
   const options={batches:[{id:'b',name:'Piano',teacherId:'t',branchId:'r'},{id:'u',name:'Unassigned',teacherId:null,branchId:null}],teachers:[{id:'t',firstName:'Synthetic',lastName:'Teacher'}],branches:[{id:'r',name:'Central'}]};
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(25);}throw Error('DOM wait timed out: '+expression);};
   const click=label=>evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent.trim()===${JSON.stringify(label)}).click()`);
   const select=(index,value)=>evaluate(`(()=>{const n=document.querySelectorAll('select')[${index}];n.value=${JSON.stringify(value)};n.dispatchEvent(new Event('change',{bubbles:true}));})()`);
   const times=()=>evaluate(`(()=>{document.querySelectorAll('input[type="datetime-local"]').forEach((n,i)=>{Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(n,i?'2027-01-01T11:00':'2027-01-01T10:00');n.dispatchEvent(new Event('input',{bubbles:true}));});})()`);
   const enabled=`Boolean(Array.from(document.querySelectorAll('button')).find(n=>n.textContent.trim()==='Schedule class'&&!n.disabled))`;
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return await reply(204);
     requests.push({width,role,variant,method:request.method,path:url.pathname});a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');let data;
     if(request.method!=='GET'){
      a.equal(url.pathname,base+'/sessions'+(request.method==='PUT'?'/one':''));
      const body=JSON.parse(request.postData);writes.push({method:request.method,body});attempts++;
      if(request.method==='POST')a.deepEqual(body,{batchId:variant==='defaults'||variant==='override'?'u':'b',teacherId:variant==='defaults'?null:'t',branchId:variant==='defaults'?null:'r',startUtc:'2027-01-01T04:30:00.000Z',endUtc:'2027-01-01T05:30:00.000Z',deliveryMode:'InPerson',roomName:null});
      else a.deepEqual(body,{startUtc:row.startUtc,endUtc:row.endUtc,deliveryMode:'InPerson',roomName:null,status:'Completed'});
      const finish=()=>{if(request.method==='POST')stored.push({id:'new',...body,status:'Scheduled'});else row.status=body.status;return reply(200,stored.at(-1));};
      if(variant==='pending'){holdWrite=finish;return;}return await finish();
     }
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:[role],displayName:'Synthetic '+role,isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic Academy',enabledModulesJson:'["Core"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname===base+'/sessions/options'){
      optionReads++;if(variant==='denied')return await reply(403,{});if(variant==='readback'&&attempts)return await reply(503,{});
      data=variant==='malformed'?{...options,batches:[{id:'b',name:'Piano',teacherId:'foreign'}]}:variant==='empty'?{batches:[],teachers:[],branches:[]}:options;
     }else if(url.pathname===base+'/sessions'){recordReads++;data=variant==='empty'?[]:stored;}
     else throw Error('Forbidden/unexpected full lookup '+url.pathname);
     return await reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, PUT, OPTIONS'}],body:status===204||data===undefined?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);fs.writeFileSync(path.join(evidence,'interception-errors.json'),JSON.stringify(errors,null,2));await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/schedule'},sid);
    if(['denied','malformed'].includes(variant)){
     await wait(`document.body?.textContent.includes('Schedule records unavailable')`);a.doesNotMatch(await evaluate('document.body.textContent'),/No classes scheduled yet/);a.equal(await evaluate(enabled),false);a.equal(attempts,0);
    }else if(variant==='empty'){
     await wait(`document.body?.textContent.includes('No classes scheduled yet')`);a.equal(await evaluate(enabled),false);a.equal(attempts,0);
    }else{
     await wait(enabled);a.deepEqual(await evaluate(`Array.from(document.querySelectorAll('select')).slice(0,3).map(n=>n.value)`),['b','t','r']);
     if(['defaults','override'].includes(variant)){await select(0,'u');a.deepEqual(await evaluate(`Array.from(document.querySelectorAll('select')).slice(0,3).map(n=>n.value)`),['u','','']);if(variant==='override'){await select(1,'t');await select(2,'r');}}
     await times();await click('Schedule class');
     if(variant==='pending'){
      await wait(`!(${enabled})`);await evaluate(`document.querySelector('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}))`);a.equal(attempts,1);const end=Date.now()+10000;while(!holdWrite&&Date.now()<end)await sleep(25);a.ok(holdWrite);await holdWrite();
     }
     if(variant==='readback'){await wait(`document.body?.textContent.includes('Session scheduled, but records could not be refreshed')`);a.equal(await evaluate(enabled),false);}
     else {await wait(`document.body?.textContent.includes('Session scheduled.')&&(${enabled})`);a.equal(attempts,1);if(variant==='create'){await select(4,'Completed');await wait(`document.body?.textContent.includes('Session updated.')&&(${enabled})`);a.equal(attempts,2);}}
    }
    a.equal(await evaluate(`document.documentElement.scrollWidth<=innerWidth+1`),true,'No horizontal page overflow');a.doesNotMatch(await evaluate('document.body.textContent'),/5092|localhost/);
    if(variant==='create'||variant==='denied'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,role+'-'+variant+'-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    results.push({width,role,variant,pass:true,attempts,optionReads,recordReads});
   }catch(e){results.push({width,role,variant,pass:false,error:e.message,attempts,optionReads,recordReads});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+role+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,32);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c36-schedule-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium and synthetic API persistence/faults only; no live Identity/SQL, required-audit rollback, actual devices or production qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('32 native scheduling cases PASS; mobile/desktop, synthetic writes, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
