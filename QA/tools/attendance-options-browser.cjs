// Delegated attendance workspace and notes in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/attendance-options-browser');
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
 const selected=process.env.QA_ATTENDANCE_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 const variants=['mark','denied','malformed','empty','record-denied','refresh-draft','switch','delayed-record'];
 try{
  if(!executable||!fs.existsSync(path.join(out,'attendance.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c35-attendance-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);


  for(const width of [320,1440])for(const role of ['Operations','Manager'])for(const variant of variants){
   let attempts=0,optionReads=0,recordReads=0,holdRead;
   const stored={one:[{studentId:'s',status:'Present',notes:'Saved one note'}],two:[{studentId:'s',status:'Late',notes:'Saved two note'}]};
   const options={students:[{id:'s',firstName:'Synthetic',lastName:'Learner'}],batches:[{id:'b',name:'Piano'}],enrollments:[{studentId:'s',batchId:'b',status:'Active'}],sessions:[{id:'one',batchId:'b',startUtc:'2026-11-11T10:00:00Z'},{id:'two',batchId:'b',startUtc:'2026-11-11T11:00:00Z'}]};
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(25);}throw Error('DOM wait timed out: '+expression);};
   const click=label=>evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent.trim()===${JSON.stringify(label)}).click()`);
   const note=value=>evaluate(`(()=>{const n=document.querySelector('input[placeholder="Notes (optional)"]');Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(n,${JSON.stringify(value)});n.dispatchEvent(new Event('input',{bubbles:true}));})()`);
   const select=async(name,value)=>{await evaluate(`document.querySelector('input[name="'+${JSON.stringify(name)}+'"]').closest('.standard-select-field').querySelector('button').click()`);await wait(`Boolean(document.querySelector('[role="listbox"]'))`);if(name==='session')await evaluate(`(()=>{const ns=document.querySelectorAll('[role="option"]');if(ns.length!==3)throw Error('Unexpected session options');ns[${value==='two'?2:1}].click();})()`);else await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).find(n=>n.textContent.trim()===${JSON.stringify(value)}).click()`);};
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return await reply(204);
     requests.push({width,role,variant,method:request.method,path:url.pathname});a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');let data;
     const session=url.pathname.match(/\/sessions\/(one|two)\/attendance$/)?.[1];
     if(request.method==='POST'){
      a.equal(variant,'mark');a.equal(session,'one');const b=JSON.parse(request.postData);a.deepEqual(b,{studentId:'s',status:'Late',notes:attempts===0?'Reviewed ♪ note':'Saved with explicit click'});stored[session]=[{...b,notes:b.notes.trim()}];attempts++;return await reply(200,stored[session][0]);
     }
     a.equal(request.method,'GET');
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:[role],displayName:'Synthetic '+role,isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic Academy',enabledModulesJson:'["Core"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname===base+'/attendance/options'){
      optionReads++;if(variant==='denied')return await reply(403,{});data=variant==='malformed'?{...options,enrollments:[{studentId:'foreign',batchId:'b',status:'Active'}]}:variant==='empty'?{students:[],batches:[],sessions:[],enrollments:[]}:options;
     }else if(session){
      recordReads++;if(variant==='record-denied'&&recordReads===1)return await reply(503,{});
      if(variant==='delayed-record'&&recordReads===1){holdRead=()=>reply(200,stored[session]);return;}data=stored[session];
     }else throw Error('Forbidden/unexpected full lookup '+url.pathname);
     return await reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, OPTIONS'}],body:status===204||data===undefined?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);fs.writeFileSync(path.join(evidence,'interception-errors.json'),JSON.stringify(errors,null,2));await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/attendance'},sid);
    if(['denied','malformed'].includes(variant)){
     await wait(`document.body?.textContent.includes('Attendance workspace could not be loaded')`);a.doesNotMatch(await evaluate('document.body.textContent'),/Schedule a class first|No active enrolments|0 students/);a.equal(await evaluate(`Boolean(document.querySelector('input[placeholder="Notes (optional)"]'))`),false);a.equal(recordReads,0);
    }else if(variant==='empty'){
     await wait(`document.body?.textContent.includes('Schedule a class first')`);a.equal(recordReads,0);
    }else{
     if(variant==='record-denied'){await wait(`document.body?.textContent.includes('Attendance records are unavailable')`);await click('Retry records');}
     if(variant==='delayed-record'){await wait(`document.body?.textContent.includes('Loading attendance records')`);a.equal(await evaluate(`Boolean(document.querySelector('input[placeholder="Notes (optional)"]'))`),false);const end=Date.now()+10000;while(!holdRead&&Date.now()<end)await sleep(25);a.ok(holdRead);a.equal(attempts,0);await holdRead();}
     await wait(`document.querySelector('input[placeholder="Notes (optional)"]')?.value==='Saved one note'`);
     if(variant==='mark'){
      await note('Reviewed ♪ note');await select('attendance-s','Late');await wait(`document.body.textContent.includes('Attendance saved.')&&document.querySelector('input[placeholder="Notes (optional)"]').value==='Reviewed ♪ note'&&!document.querySelector('.attendance-save').disabled`);a.equal(attempts,1);
      await note('Saved with explicit click');await click('Save');await wait(`document.body.textContent.includes('Attendance saved.')&&document.querySelector('input[placeholder="Notes (optional)"]').value==='Saved with explicit click'&&!document.querySelector('.attendance-save').disabled`);a.equal(attempts,2);
     }else if(variant==='refresh-draft'){
      await note('Retained draft');await click('Reload attendance workspace');await wait(`document.querySelector('input[placeholder="Notes (optional)"]')?.value==='Retained draft'&&!document.querySelector('.attendance-save').disabled`);a.equal(optionReads,2);a.equal(attempts,0);
     }else if(variant==='switch'){
      await note('Session one draft');await select('session','two');await wait(`document.querySelector('input[placeholder="Notes (optional)"]')?.value==='Saved two note'`);await note('Session two draft');await select('session','one');await wait(`document.querySelector('input[placeholder="Notes (optional)"]')?.value==='Session one draft'`);a.equal(attempts,0);
     }
    }
    a.equal(await evaluate(`document.documentElement.scrollWidth<=innerWidth+1`),true,'No horizontal page overflow');a.doesNotMatch(await evaluate('document.body.textContent'),/5092|localhost/);
    if(variant==='mark'||variant==='denied'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,role+'-'+variant+'-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    results.push({width,role,variant,pass:true,attempts,optionReads,recordReads});
   }catch(e){results.push({width,role,variant,pass:false,error:e.message,attempts,optionReads,recordReads});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+role+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,32);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c35-attendance-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium and synthetic API persistence/faults only; no live Identity/SQL, required-audit rollback, actual devices or production qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('32 native delegated attendance cases PASS; mobile/desktop, synthetic writes, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
