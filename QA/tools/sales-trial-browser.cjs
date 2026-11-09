// Sales trial lifecycle and recovery in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/sales-trial-browser');
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
 const selected=process.env.QA_TRIAL_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 const variants=['book','assigned','denied','retry','lost-before','lost-after','readback','status','status-lost','leads'];
 try{
  if(!executable||!fs.existsSync(path.join(out,'trial-bookings.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c27-trials-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const zone of ['UTC','America/Los_Angeles'])for(const variant of variants){
   const leads=[{id:'lead',fullName:'Synthetic Prospect',stage:'New',convertedStudentId:null,source:'WalkIn'},{id:'converted',fullName:'Converted History',stage:' Converted ',convertedStudentId:null,source:'WalkIn'},{id:'linked',fullName:'Linked History',stage:'New',convertedStudentId:'student',source:'WalkIn'}],teachers=[{id:'teacher',firstName:'Owned',lastName:'Teacher'}];
   let trials=[{id:'history',leadId:'converted',teacherId:null,scheduledAtUtc:'2026-12-01T04:30:00.000Z',status:'Booked',notes:null}],attempts=0,commits=0,patches=0,failedOnce=false,readbackFailed=false,holdPost;
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(25);}throw Error('DOM wait timed out: '+expression);};
   const click=label=>evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent===${JSON.stringify(label)}).click()`);
   const select=async(name,label)=>{await evaluate(`document.querySelector('input[name="'+${JSON.stringify(name)}+'"]').closest('.standard-select-field').querySelector('button').click()`);await wait(`Boolean(document.querySelector('[role="listbox"]'))`);await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).find(n=>n.textContent===${JSON.stringify(label)}).click()`);};
   const submit=()=>evaluate(`document.querySelector('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}))`);
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);await cdp.send('Emulation.setTimezoneOverride',{timezoneId:zone},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return await reply(204,{});
     requests.push({width,zone,variant,method:request.method,path:url.pathname});a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');let data;
     if(request.method==='POST'){
      a.equal(url.pathname,base+'/sales-marketing/trials');attempts++;const body=JSON.parse(request.postData);a.equal(body.leadId,'lead');a.equal(body.teacherId,variant==='assigned'?'teacher':null);a.match(body.scheduledAtUtc,/T04:30:00.000Z$/);
      const commit=()=>{const row={id:'created',status:'Booked',...body};trials.push(row);commits++;return row;};
      if(variant==='lost-before')return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionReset'},sid);
      const row=commit();if(variant==='lost-after')return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionReset'},sid);
      if(variant==='book'){holdPost=()=>reply(200,row);return;}return await reply(200,row);
     }
     if(request.method==='PATCH'){
      a.equal(url.pathname,base+'/sales-marketing/trials/history/status');patches++;const body=JSON.parse(request.postData);a.equal(body.status,'Completed');trials=trials.map(r=>r.id==='history'?{...r,status:body.status}:r);
      if(variant==='status-lost')return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionReset'},sid);return await reply(200,trials[0]);
     }
     a.equal(request.method,'GET');
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['Sales'],displayName:'Synthetic Sales',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic Academy',timeZone:'Asia/Kolkata',enabledModulesJson:'["Core","Sales"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname===base+'/leads')data=leads;
     else if(url.pathname===base+'/sales-marketing/teacher-options'){
      if(variant==='denied')return await reply(403,{});
      if(variant==='retry'&&!failedOnce){failedOnce=true;return await reply(503,{});}data=teachers;
     }else if(url.pathname===base+'/sales-marketing/trials'){
      if(variant==='readback'&&commits&&!readbackFailed){readbackFailed=true;return await reply(503,{});}data=trials;
     }else throw Error('Unexpected API path '+url.pathname);
     return await reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, PATCH, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);fs.writeFileSync(path.join(evidence,'interception-errors.json'),JSON.stringify(errors,null,2));await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+(variant==='leads'?'/leads':'/trial-bookings')},sid);
    if(variant==='leads'){
     await wait(`Boolean(document.querySelector('input[name="lead-stage-lead"]'))`);
     await evaluate(`document.querySelector('input[name="lead-stage-lead"]').closest('.standard-select-field').querySelector('button').click()`);await wait(`Boolean(document.querySelector('[role="listbox"]'))`);
     a.equal(await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).some(n=>n.textContent==='Converted')`),false);await evaluate(`document.body.click()`);
     a.equal(await evaluate(`document.querySelector('input[name="lead-stage-converted"]').closest('.standard-select-field').querySelector('button').disabled`),true);
     a.equal(attempts,0);a.equal(patches,0);
    }else if(variant==='denied'){
     await wait(`document.body?.textContent.includes('Trial bookings could not be loaded')`);a.doesNotMatch(await evaluate('document.body.textContent'),/No trial classes booked yet/);a.equal(await evaluate(`document.querySelector('fieldset').disabled`),true);await submit();a.equal(attempts,0);
    }else{
     if(variant==='retry'){await wait(`Boolean(Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Try again'))`);await click('Try again');}
     await wait(`Boolean(document.querySelector('fieldset'))&&!document.querySelector('fieldset').disabled`);
     a.match(await evaluate('document.body.textContent'),/Converted History/);
     if(variant.startsWith('status')){
      await select('trial-status-history','Completed');await wait(`document.body.textContent.includes(${JSON.stringify(variant==='status-lost'?'could not be confirmed':'Trial status updated')})`);
      if(variant==='status-lost'){await click('Reload trial records');await wait(`Boolean(Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Continue after review'))`);a.equal(patches,1);await click('Continue after review');}
      a.equal(patches,1);a.equal(trials[0].status,'Completed');a.equal(attempts,0);
     }else{
      await evaluate(`document.querySelector('input[name="trial-lead"]').closest('.standard-select-field').querySelector('button').click()`);await wait(`Boolean(document.querySelector('[role="listbox"]'))`);
      a.deepEqual(await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).map(n=>n.textContent)`),['Select lead','Synthetic Prospect']);await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).find(n=>n.textContent==='Synthetic Prospect').click()`);
      if(variant==='assigned')await select('trial-teacher','Owned Teacher');
      await evaluate(`document.querySelector('input[name="trial-date"]').closest('label').querySelector('button').click()`);await wait(`Boolean(document.querySelector('.standard-date-days'))`);await evaluate(`Array.from(document.querySelectorAll('.standard-date-days button[data-current="true"]')).find(n=>n.textContent==='15').click()`);await wait(`Boolean(document.querySelector('input[name="trial-date"]').value)`);
      const date=await evaluate(`document.querySelector('input[name="trial-date"]').value`);await submit();
      if(variant==='book'){await wait(`document.querySelector('fieldset').disabled`);await submit();const until=Date.now()+10000;while(!holdPost&&Date.now()<until)await sleep(25);a.equal(attempts,1);a.ok(holdPost);await holdPost();}
      if(variant.startsWith('lost-')){
       await wait(`document.body.textContent.includes('could not be confirmed')`);a.equal(await evaluate(`document.querySelector('input[name="trial-date"]').value`),date);await submit();a.equal(attempts,1);
       await click('Reload trial records');await wait(`Boolean(Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Start a new booking'))`);a.equal(attempts,1);await submit();a.equal(attempts,1);await click('Start a new booking');await wait(`!document.querySelector('fieldset').disabled`);
       a.equal(commits,variant==='lost-after'?1:0);
      }else if(variant==='readback'){
       await wait(`document.body.textContent.includes('records could not be refreshed')`);a.match(await evaluate('document.body.textContent'),/Trial class booked.*confirmed/);await click('Reload trial records');await wait(`document.body.textContent.includes('Trial records refreshed')`);a.equal(attempts,1);a.equal(commits,1);
      }else{await wait(`document.body.textContent.includes('Trial class booked.')`);a.equal(commits,1);a.equal(attempts,1);a.equal(await evaluate(`document.querySelector('input[name="trial-date"]').value`),'');}
      if(commits)a.equal(trials.at(-1).scheduledAtUtc,date+'T04:30:00.000Z');
     }
     a.equal(await evaluate(`document.documentElement.scrollWidth<=innerWidth+1`),true,'No horizontal page overflow');
    }
    a.doesNotMatch(await evaluate('document.body.textContent'),/5092|localhost/);
    if(zone==='UTC'&&['book','denied','lost-after'].includes(variant)){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,variant+'-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    results.push({width,zone,variant,pass:true,attempts,commits,patches});
   }catch(e){results.push({width,zone,variant,pass:false,error:e.message,attempts,commits,patches});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+zone+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,40);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c27-trials-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium and synthetic API persistence/faults only; no live Identity/SQL, races, devices, protected host or production qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('40 native Sales trial cases PASS; UTC/Los Angeles, mobile/desktop, synthetic writes, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
