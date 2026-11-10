// Enrollment lifecycle review and recovery in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/enrollment-lifecycle-browser');
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
 const selected=process.env.QA_ENROLLMENT_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 const variants=['status-Active','status-Waitlisted','status-Paused','status-Completed','status-Withdrawn','status-Cancelled','cancel','blank','stale','lost-before','lost-after','bad-receipt','readback','denied','create-Active','create-Waitlisted','create-lost','create-readback','initial-fail','preflight-fail'];
 try{
  if(!executable||!fs.existsSync(path.join(out,'enrollments.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c32-enrollment-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);

  for(const width of [320,1440])for(const variant of variants){
   const creating=variant.startsWith('create-'),targetStatus=variant.startsWith('status-')?variant.slice(7):'Paused';
   let rows=creating?[]:[{id:'enrollment',studentId:'student',batchId:'batch',startDate:'2026-10-01',endDate:'2026-11-09',status:targetStatus==='Active'?'Paused':'Active'}],attempts=0,commits=0,reads=0,holdWrite,submitted=false;
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(25);}throw Error('DOM wait timed out: '+expression);};
   const click=label=>evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent===${JSON.stringify(label)}).click()`);
   const setSelect=(name,value)=>evaluate(`(()=>{const n=document.querySelector('select[name="'+${JSON.stringify(name)}+'"]');n.value=${JSON.stringify(value)};n.dispatchEvent(new Event('change',{bubbles:true}));})()`);
   const setReason=reason=>evaluate(`(()=>{const n=document.querySelector('textarea[name="lifecycleReason"]');Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype,'value').set.call(n,${JSON.stringify(reason)});n.dispatchEvent(new Event('input',{bubbles:true}));})()`);
   const submit=()=>evaluate(`(document.querySelector(${JSON.stringify(creating?'form:not([data-enrollment-review])':'form[data-enrollment-review]')})||window.reviewForm).dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}))`);
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return await reply(204);
     requests.push({width,variant,method:request.method,path:url.pathname});a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');let data;
     if(request.method==='POST'||request.method==='PUT'){
      a.equal(request.method,creating?'POST':'PUT');a.equal(url.pathname,base+'/enrollments'+(creating?'':'/enrollment'));attempts++;submitted=true;const b=JSON.parse(request.postData);
      if(creating)a.deepEqual(b,{studentId:'student',batchId:'batch',startDate:'2026-11-10',status:variant==='create-Waitlisted'?'Waitlisted':'Active'});
      else a.deepEqual(b,{status:targetStatus,endDate:targetStatus==='Active'?null:'2026-11-09',lifecycleReason:targetStatus==='Active'?null:'Reviewed synthetic reason'});
      if(variant==='denied')return await reply(409,{message:'Synthetic capacity refusal'});
      if(variant==='lost-before')return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionReset'},sid);
      const saved=creating?{id:'created',studentId:b.studentId,batchId:b.batchId,startDate:b.startDate,endDate:null,status:b.status}:{...rows[0],status:b.status,endDate:b.endDate};if(creating)rows.push(saved);else rows[0]=saved;commits++;
      if(variant==='lost-after'||variant==='create-lost')return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionReset'},sid);
      if(variant==='status-Paused'){holdWrite=()=>reply(200,saved);return;}
      return await reply(200,variant==='bad-receipt'?{...saved,studentId:'foreign'}:saved);
     }
     a.equal(request.method,'GET');
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['AcademyAdmin'],displayName:'Synthetic admin',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic Academy',enabledModulesJson:'["Core"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname===base+'/students'){if(variant==='initial-fail')return await reply(403,{});data=[{id:'student',firstName:'Synthetic',lastName:'Learner'}];}
     else if(url.pathname===base+'/batches')data=[{id:'batch',name:'Piano batch'}];
     else if(url.pathname===base+'/enrollments'){
      reads++;
      if(variant==='preflight-fail'&&reads===2)return await reply(503,{});
      if((variant==='readback'||variant==='create-readback')&&submitted&&reads===(creating?2:3))return await reply(503,{});
      if(variant==='stale'&&reads===2)rows[0]={...rows[0],status:'Completed'};
      data=rows;
     }else throw Error('Unexpected API path '+url.pathname);
     return await reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, PUT, OPTIONS'}],body:status===204||data===undefined?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);fs.writeFileSync(path.join(evidence,'interception-errors.json'),JSON.stringify(errors,null,2));await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/enrollments'},sid);
    if(variant==='initial-fail'){
     await wait(`document.body?.textContent.includes('records could not be loaded')`);a.doesNotMatch(await evaluate('document.body.textContent'),/No students enrolled yet/);await evaluate(`document.querySelector('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}))`);a.equal(attempts,0);
    }else{
     await wait(`Boolean(document.querySelector('select[name="studentId"]'))&&!document.querySelector('form fieldset').disabled`);
     if(creating){
      await setSelect('initialStatus',variant==='create-Waitlisted'?'Waitlisted':'Active');
      await evaluate(`document.querySelector('input[name="startDate"]').closest('label').querySelector('button').click()`);await wait(`Boolean(document.querySelector('[role="dialog"][aria-label="Start date calendar"]'))`);
      await evaluate(`(()=>{const n=document.querySelector('select[aria-label="Select year"]');n.value='2026';n.dispatchEvent(new Event('change',{bubbles:true}));})()`);
      await evaluate(`(()=>{const n=document.querySelector('select[aria-label="Select month"]');n.value='10';n.dispatchEvent(new Event('change',{bubbles:true}));})()`);
      await evaluate(`Array.from(document.querySelectorAll('.standard-date-days button')).find(n=>n.textContent==='10'&&n.dataset.current==='true').click()`);
      await wait(`document.querySelector('input[name="startDate"]').value==='2026-11-10'`);await submit();
     }else{
      await setSelect('status.enrollment',targetStatus);await wait(`Boolean(document.querySelector('form[data-enrollment-review]'))`);a.equal(attempts,0);
      await evaluate(`Boolean(window.reviewForm=document.querySelector('form[data-enrollment-review]'))`);
      a.match(await evaluate(`document.querySelector('form[data-enrollment-review]').textContent`),/Synthetic Learner.*Piano batch/);
      if(variant==='cancel'){await click('Cancel status change');await wait(`!document.querySelector('form[data-enrollment-review]')`);a.equal(attempts,0);}
      else if(variant==='blank'){await setReason('   ');await submit();await wait(`document.body.textContent.includes('Enter a lifecycle reason')`);a.equal(attempts,0);}
      else{if(targetStatus!=='Active')await setReason('Reviewed synthetic reason');await submit();}
     }
     if(variant==='status-Paused'){await wait(`document.querySelector('form[data-enrollment-review] fieldset').disabled`);await submit();const until=Date.now()+10000;while(!holdWrite&&Date.now()<until)await sleep(25);a.ok(holdWrite);a.equal(attempts,1);await holdWrite();}
     if(['lost-before','lost-after','bad-receipt','create-lost'].includes(variant)){
      await wait(`document.body.textContent.includes('could not be confirmed')`);a.equal(await evaluate(creating?`document.querySelector('input[name="startDate"]').value`:`document.querySelector('textarea[name="lifecycleReason"]').value`),creating?'2026-11-10':'Reviewed synthetic reason');await submit();a.equal(attempts,1);
      await click('Reload enrolment records');await wait(`Boolean(Array.from(document.querySelectorAll('button')).find(n=>n.textContent===${JSON.stringify(creating?'Start a new enrolment':'Start a new status review')}))`);await submit();a.equal(attempts,1);await click(creating?'Start a new enrolment':'Start a new status review');a.equal(commits,variant==='lost-before'?0:1);
     }else if(variant==='readback'||variant==='create-readback'){
      await wait(`document.body.textContent.includes('confirmed, but records could not be refreshed')`);await submit();a.equal(attempts,1);await click('Reload enrolment records');await wait(`document.body.textContent.includes('confirmed. Enrolment records refreshed')`);a.equal(attempts,1);a.equal(commits,1);
     }else if(variant==='stale'){
      await wait(`document.body.textContent.includes('changed since this review')`);a.equal(attempts,0);a.equal(await evaluate(`document.querySelector('textarea[name="lifecycleReason"]').value`),'Reviewed synthetic reason');await click('Reload enrolment records');await wait(`Boolean(Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Start a new status review'))`);a.equal(attempts,0);
     }else if(variant==='preflight-fail'){
      await wait(`document.body.textContent.includes('No change was submitted')`);a.equal(attempts,0);a.equal(await evaluate(`document.querySelector('textarea[name="lifecycleReason"]').value`),'Reviewed synthetic reason');
     }else if(variant==='denied'){
      await wait(`document.body.textContent.includes('Synthetic capacity refusal')`);a.equal(attempts,1);a.equal(commits,0);
     }else if(variant.startsWith('status-')||creating){await wait(`document.body.textContent.includes(${JSON.stringify(creating?'Enrolment creation confirmed':'Enrolment status change confirmed')})`);a.equal(attempts,1);a.equal(commits,1);}
    }
    a.equal(await evaluate(`document.documentElement.scrollWidth<=innerWidth+1`),true,'No horizontal page overflow');a.doesNotMatch(await evaluate('document.body.textContent'),/5092|localhost/);
    if(['blank','stale','lost-after','create-readback'].includes(variant)){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,variant+'-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    results.push({width,variant,pass:true,attempts,commits,reads});
   }catch(e){results.push({width,variant,pass:false,error:e.message,attempts,commits,reads});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,40);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c32-enrollment-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium enrollment forms/review and synthetic API persistence/faults only; no live Identity/SQL, required-audit rollback, actual devices or production qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('40 native enrollment lifecycle cases PASS; mobile/desktop, synthetic writes, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
