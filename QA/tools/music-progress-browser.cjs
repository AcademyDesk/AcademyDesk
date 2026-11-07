// Current music progress UI in real Chromium. Synthetic APIs only; not live Identity/SQL/device acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/music-progress-browser');
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



async function main() {
 const results=[],runtimeErrors=[],requests=[];
 let child,socket,cdp,profile,serverStarted=false,passed=false,failure=null,apiOrigin;
 const academy='11111111-1111-4111-8111-111111111111',student='22222222-2222-4222-8222-222222222222',piece='33333333-3333-4333-8333-333333333333',progress='44444444-4444-4444-8444-444444444444',other='55555555-5555-4555-8555-555555555555';
 const base=`/api/academies/${academy}`,endpoint=base+'/music-progress/'+progress;
 const states=['Assigned','Learning','ReadyForReview','Mastered','Paused'];
 const label=s=>s==='ReadyForReview'?'Ready for review':s;
 const deferred=()=>{let release;return{promise:new Promise(r=>release=r),release:()=>release()};};
 try {
  const selected=process.env.QA_MUSIC_API_ORIGIN||'http://127.0.0.1:49142',url=new URL(selected);
  if(url.protocol!=='http:'||url.hostname!=='127.0.0.1'||!url.port||Number(url.port)<1024||selected!==url.origin||url.username||url.password)throw Error('Only an explicit unprivileged HTTP127.0.0.1origin is accepted.');
  apiOrigin=url.origin;
  if(!executable||!fs.existsSync(path.join(out,'music.html')))throw Error('Select installed Chromium and build the current static export first.');
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));serverStarted=true;
  const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c8-music-'));
  child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe'],windowsHide:true});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Chromium startup timeout')),15000);child.once('error',e=>{clearTimeout(timer);reject(e);});child.once('exit',()=>{clearTimeout(timer);reject(Error('Chromium exited before debugger startup'));});child.stderr.on('data',data=>{log+=data;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((resolve,reject)=>{socket.addEventListener('open',resolve,{once:true});socket.addEventListener('error',reject,{once:true});});cdp=new CDP(socket);
  async function page(config={}) {
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'});const {sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});
   await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width:config.width||1280,height:900,deviceScaleFactor:1,mobile:false},sid);
   const initial={id:progress,studentId:student,musicPieceId:piece,status:'Assigned',targetDate:'2026-12-01',notes:'Synthetic learning note',...(config.absent?{}:{score:config.score??null})};
   const state={stored:[initial,{...initial,id:other,status:'Learning',score:37.5}],writes:[],reads:0,holdWrite:null,holdRead:null,reject:false,transport:false,commitBeforeLoss:false,failRead:false};
   cdp.on(sid,'Runtime.exceptionThrown',e=>runtimeErrors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
   cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')runtimeErrors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
   // Chromium logs expected net::ERR_FAILED for deliberately failed synthetic transports.
   // JS exceptions remain failures; synthetic transport diagnostics are separately retained.
   cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{
    try {
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);
     if(url.origin!==apiOrigin)throw Error('Unexpected external request');
     if(request.method==='OPTIONS')return await reply(204,{});
     requests.push({method:request.method,path:url.pathname});
     if(request.method==='PATCH') {
      a.equal(url.pathname,endpoint);const body=JSON.parse(request.postData);state.writes.push({path:url.pathname,body});
      if(state.holdWrite)await state.holdWrite;
      if(state.reject)return await reply(503,{message:'Synthetic rejection'});
      a.ok(states.includes(body.status));a.ok(body.score===null||typeof body.score==='number');
      if(!state.transport||state.commitBeforeLoss)state.stored=state.stored.map(row=>row.id===progress?{...row,...body}:row);
      if(state.transport)return await cdp.send('Fetch.failRequest',{requestId,errorReason:'Failed'},sid);
      return await reply(200,state.stored[0]);
     }
     a.equal(request.method,'GET','Only progress PATCH is permitted');
     if(url.pathname==='/api/academies')return await reply(200,[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Academics","Students"]'}]);
     if(url.pathname==='/api/auth/session')return await reply(200,{workspace:'AcademyAdmin',academyId:academy,displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false});
     if(url.pathname==='/api/portal/announcements')return await reply(200,[]);
     if(url.pathname===base+'/students')return await reply(200,[{id:student,firstName:'Synthetic',lastName:'Learner'}]);
     if(url.pathname===base+'/music-pieces')return await reply(200,[{id:piece,title:'Synthetic piece',difficulty:'Beginner'}]);
     if(url.pathname===base+'/music-progress'){state.reads++;const snapshot=structuredClone(state.stored);if(state.holdRead)await state.holdRead;if(state.failRead)return await reply(503,{});return await reply(200,snapshot);}
     throw Error('Unexpected API path '+url.pathname);
     async function reply(status,body){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, PATCH, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(body)).toString('base64')},sid);}
    }catch(e){runtimeErrors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}
   });
   await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
   await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(config.theme||'light')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
   const evaluate=async expression=>{const r=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true},sid);if(r.exceptionDetails)throw Error(r.exceptionDetails.exception?.description||r.exceptionDetails.text);return r.result.value;};
   const wait=async expression=>{for(let i=0;i<100;i++){if(await evaluate(expression))return;await sleep(40);}throw Error('DOM wait failed: '+expression);};
   const field=id=>`document.querySelector('[name="progress-${id||progress}"]')`;
   const trigger=id=>`${field(id)}?.parentElement.querySelector('button')`;
   const ready=async(status='Assigned')=>wait(`${field()}?.value===${JSON.stringify(status)}&&!${trigger()}?.disabled`);
   const select=async status=>{await evaluate(`${trigger()}.click()`);await wait(`Array.from(document.querySelectorAll('[role="option"]')).some(b=>b.textContent===${JSON.stringify(status?label(status):'Progress')})`);await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).find(b=>b.textContent===${JSON.stringify(status?label(status):'Progress')}).click()`);};
   const retry=async()=>{await wait(`Array.from(document.querySelectorAll('button')).some(b=>b.textContent==='Retry loading progress')`);await evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Retry loading progress').click()`);};
   await cdp.send('Page.navigate',{url:origin+'/music'},sid);await ready();
   return{sid,state,evaluate,wait,field,trigger,ready,select,retry,reload:()=>cdp.send('Page.reload',{ignoreCache:true},sid),close:()=>cdp.send('Target.closeTarget',{targetId})};
  }
  async function run(name,config,check) {const p=await page(config);try{await check(p);results.push({case:name,...config,pass:true,writes:p.state.writes});}catch(e){results.push({case:name,...config,pass:false,error:e.message,writes:p.state.writes});}finally{p.state.holdRead=null;p.state.holdWrite=null;await p.close();}}
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const item of [{name:'zero',score:0},{name:'positive',score:37.5},{name:'null',score:null},{name:'absent',absent:true}])for(const status of ['Learning','Mastered']) {
   await run(item.name+'-'+status,{...item,width,theme},async p=>{
    const otherBefore=structuredClone(p.state.stored[1]);
    for(const next of [status,status==='Learning'?'Mastered':'Learning']){
     await p.select(next);await p.ready(next);await p.reload();await p.ready(next);
     const expected={status:next,targetDate:'2026-12-01',score:item.score??null,notes:'Synthetic learning note'};
     a.deepEqual(p.state.writes.at(-1),{path:endpoint,body:expected});a.deepEqual(p.state.stored[0],{...p.state.stored[0],...expected});a.deepEqual(p.state.stored[1],otherBefore);
    }
    a.equal(p.state.writes.length,2);a.equal(await p.evaluate('document.documentElement.scrollWidth<=innerWidth+1'),true);a.equal(await p.evaluate('document.documentElement.dataset.theme'),theme);
    if(width===320&&theme==='light'&&item.name==='zero'&&status==='Learning'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},p.sid);fs.writeFileSync(path.join(evidence,'320-light-zero.png'),Buffer.from(shot.data,'base64'));}
   });
  }
  for(const status of ['Assigned','ReadyForReview','Paused'])await run('status-'+status,{score:0},async p=>{await p.select('Mastered');await p.ready('Mastered');await p.select(status);await p.ready(status);a.equal(p.state.writes.length,2);a.equal(p.state.writes[1].body.score,0);});
  await run('rejection-retry',{score:0},async p=>{p.state.reject=true;await p.select('Learning');await p.wait(`document.querySelector('.music-message')?.textContent.includes('could not be updated')`);a.equal(await p.evaluate(`${p.field()}.value`),'Assigned');a.equal(p.state.stored[0].score,0);a.equal(p.state.writes.length,1);a.equal(await p.evaluate(`${p.trigger()}.disabled`),true);p.state.reject=false;await p.retry();await p.ready();a.equal(p.state.writes.length,1);await p.select('Mastered');await p.ready('Mastered');a.equal(p.state.writes.length,2);a.equal(p.state.stored[0].score,0);});
  for(const committed of [false,true])await run('transport-loss-'+committed,{score:0},async p=>{p.state.transport=true;p.state.commitBeforeLoss=committed;await p.select('Learning');await p.wait(`document.querySelector('.music-message')?.textContent.includes('could not be confirmed')`);a.equal(await p.evaluate(`${p.trigger()}.disabled`),true);a.equal(p.state.writes.length,1);p.state.transport=false;await p.retry();await p.ready(committed?'Learning':'Assigned');a.equal(p.state.writes.length,1,'Read retry must never replay PATCH');await p.select('Mastered');await p.ready('Mastered');a.equal(p.state.writes.length,2);a.equal(p.state.stored[0].score,0);});
  await run('saved-refresh-failure',{score:0},async p=>{p.state.failRead=true;await p.select('Learning');await p.wait(`document.querySelector('.music-message')?.textContent.includes('saved')&&document.querySelector('.music-message')?.textContent.includes('reload')`);a.equal(p.state.stored[0].status,'Learning');a.equal(await p.evaluate(`${p.trigger()}.disabled`),true);a.equal(p.state.writes.length,1);p.state.failRead=false;await p.retry();await p.ready('Learning');a.equal(p.state.writes.length,1);});
  await run('pending-write',{score:0},async p=>{const gate=deferred();p.state.holdWrite=gate.promise;try{await p.select('Learning');for(let i=0;i<100&&!p.state.writes.length;i++)await sleep(20);a.equal(await p.evaluate(`${p.trigger()}.disabled`),true);a.equal(await p.evaluate(`${p.trigger(other)}.disabled`),true);a.equal(p.state.writes.length,1);}finally{gate.release();}await p.ready('Learning');a.equal(p.state.writes.length,1);});
  await run('pending-refresh',{score:0},async p=>{const gate=deferred();p.state.holdRead=gate.promise;try{await p.select('Learning');for(let i=0;i<100&&p.state.reads<2;i++)await sleep(20);a.ok(p.state.reads>=2);a.equal(await p.evaluate(`${p.trigger()}.disabled`),true);a.equal(p.state.writes.length,1);}finally{gate.release();}await p.ready('Learning');});
  await run('placeholder-no-write',{score:0},async p=>{await p.select('');await sleep(120);a.equal(p.state.writes.length,0);a.equal(await p.evaluate(`${p.field()}.value`),'Assigned');});
  a.equal(results.length,42);a.equal(results.filter(r=>!r.pass).length,0,'Every music case must pass');a.deepEqual(runtimeErrors,[]);a.equal(requests.filter(r=>/penta|model|inference/.test(r.path)).length,0);passed=true;
 }catch(e){failure=e.message;throw e;}finally {
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}
  if(serverStarted){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile){try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c8-music-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned profile cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;failure??='Owned profile cleanup incomplete';}}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,apiOrigin,sandboxDisabled:false,browserSpawnRequested:Boolean(child),ownedProfileRemoved:!cleanupError,cleanupError,results,runtimeErrors,requests,scope:'Real Chromium/React/DOM with synthetic APIs; not Identity/SQL/server concurrency/physical-device acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('42 native Chromium music groups PASS; synthetic APIs only, normal sandbox, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
