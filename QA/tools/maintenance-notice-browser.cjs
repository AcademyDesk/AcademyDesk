// Maintenance notices and GET-only recovery in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/maintenance-notice-browser');
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
 const selected=process.env.QA_MAINTENANCE_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 const variants=['recovery','generic','failed','logout'];
 try{
  if(!executable||!fs.existsSync(path.join(out,'academic-governance.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c31-maintenance-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);

  for(const width of [320,1440])for(const workspace of ['AcademyAdmin','Teacher','Portal','Platform'])for(const variant of variants){
   let mode=true,reads=0,attempts=0;
   const route={AcademyAdmin:'/academic-governance',Teacher:'/teacher',Portal:'/portal',Platform:'/platform/control?tab=Settings'}[workspace];
   const primary={Teacher:'/api/teacher/me',Portal:'/api/portal/me',Platform:'/api/platform/academies'}[workspace];
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(25);}throw Error('DOM wait timed out: '+expression);};
   const click=label=>evaluate(`Array.from(document.querySelectorAll('[data-service-availability] button')).find(n=>n.textContent===${JSON.stringify(label)}).click()`);
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return await reply(204);
     requests.push({width,workspace,variant,method:request.method,path:url.pathname});a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');let data;
     if(url.pathname==='/api/auth/session/availability'){
      a.equal(request.method,'GET');reads++;
      if(variant==='failed'&&mode)return await reply(503,{});
      return await reply(200,{maintenanceMode:mode,restricted:mode&&workspace!=='Platform',message:mode?'Saved synthetic <script>window.injected=true</script> maintenance message with a very-long-token-'+ 'x'.repeat(140):null});
     }
     if(request.method==='POST'){
      a.equal(workspace,'AcademyAdmin');a.equal(url.pathname,base+'/academic-governance/grading-schemes');attempts++;
      a.deepEqual(JSON.parse(request.postData),{name:'Retained draft',passingPercent:60,bandsJson:'[]'});return await maintenance();
     }
     a.equal(request.method,'GET');
     if(url.pathname===primary)return await maintenance();
     if(url.pathname==='/api/auth/session')data={workspace,academyId:academy,roles:['AcademyAdmin'],displayName:'Synthetic actor',isPlatformOwner:workspace==='Platform'};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic Academy',enabledModulesJson:'["Core","AcademicGovernance"]'}];
     else if(['/api/portal/announcements','/api/portal/notifications','/api/platform/admins','/api/platform/support-cases','/api/platform/billing-invoices','/api/platform/audit','/api/platform/announcements'].includes(url.pathname))data=[];
     else if(url.pathname==='/api/platform/settings')data={platformName:'Synthetic platform',defaultCurrency:'INR',defaultTrialDays:30,dataRetentionDays:2555,maintenanceMode:true,statusMessage:'Synthetic status'};
     else if(url.pathname==='/api/platform/health')data={api:'Available',database:'Available',backgroundJobs:'Unavailable',communicationProviders:'Unavailable',maintenanceMode:true};
     else if(url.pathname===base+'/courses')data=[];
     else if(url.pathname===base+'/academic-governance/grading-schemes')data=[{id:'scheme',name:'Saved policy',passingPercent:50,bandsJson:'[]',isActive:true}];
     else if(url.pathname===base+'/academic-governance/prerequisites')data=[];
     else throw Error('Unexpected API path '+url.pathname);
     return await reply(200,data);
     async function maintenance(){return await reply(503,variant==='generic'?{}:{code:'platform_maintenance',maintenanceMode:true,message:'Synthetic maintenance'});}
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, OPTIONS'}],body:status===204||data===undefined?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.'+${JSON.stringify(workspace)},'synthetic-access');localStorage.setItem('academydesk.refreshToken.'+${JSON.stringify(workspace)},'synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.'+${JSON.stringify(workspace)},'synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+route},sid);
    if(workspace==='AcademyAdmin'){
     await wait(`Boolean(document.querySelector('.governance-tiles'))&&!document.querySelector('fieldset').disabled`);
     await evaluate(`window.retainedForm=document.querySelector('input[name="name"]');window.retainedForm.value='Retained draft';document.querySelector('input[name="passingPercent"]').value='60';window.retainedForm.closest('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}))`);
     await wait(`document.body.textContent.includes('could not be confirmed')`);
    }else await wait(`document.body?.textContent.includes(${JSON.stringify(workspace==='Teacher'?'could not be loaded':workspace==='Portal'?'not linked':'could not be loaded')})`);
    if(variant==='generic'){
     await sleep(100);a.equal(await evaluate(`Boolean(document.querySelector('[data-service-availability]'))`),false);a.equal(reads,0);
    }else{
     await wait(`document.querySelector('[data-service-availability]')?.textContent.includes(${JSON.stringify(variant==='failed'?'could not be verified':workspace==='Platform'?'Maintenance is enabled':'Maintenance in progress')})`);
     a.equal(await evaluate('Boolean(window.injected)'),false);
     if(variant!=='failed'){a.equal(await evaluate(`document.querySelector('[data-service-availability]').textContent.includes('<script>window.injected=true</script>')`),true);a.equal(await evaluate(`Boolean(document.querySelector('[data-service-availability] a[href="/platform/control?tab=Settings"]'))`),workspace==='Platform');}
     a.equal(await evaluate(`document.documentElement.scrollWidth<=innerWidth+1`),true,'Active notice has no horizontal overflow');
     if(variant==='recovery'||variant==='failed'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,workspace+'-'+variant+'-'+width+'.png'),Buffer.from(shot.data,'base64'));}
     if(variant==='logout'){
      await evaluate(`localStorage.removeItem('academydesk.accessToken.'+${JSON.stringify(workspace)});window.dispatchEvent(new CustomEvent('academydesk:portal-sign-out',{detail:${JSON.stringify(workspace)}}))`);
      await wait(`document.body.textContent.includes('Session ended')`);a.equal(await evaluate(`Boolean(document.querySelector('[data-service-availability]'))`),false);
     }else{
      mode=false;await click('Check availability');await wait(`document.querySelector('[data-service-availability]')?.textContent.includes('Service is available again')`);
      a.match(await evaluate(`document.querySelector('[data-service-availability]').textContent`),/Review saved records before submitting again/);
      await click('Dismiss notice');await wait(`!document.querySelector('[data-service-availability]')`);
     }
    }
    a.equal(attempts,workspace==='AcademyAdmin'?1:0);
    if(workspace==='AcademyAdmin'&&variant!=='logout'){a.equal(await evaluate(`window.retainedForm===document.querySelector('input[name="name"]')&&window.retainedForm.value==='Retained draft'`),true,'Actual draft and DOM input remain mounted');}
    if(variant!=='logout')a.equal(await evaluate(`localStorage.getItem('academydesk.accessToken.'+${JSON.stringify(workspace)})`),'synthetic-access');
    a.equal(await evaluate(`document.documentElement.scrollWidth<=innerWidth+1`),true,'No horizontal page overflow');
    results.push({width,workspace,variant,pass:true,attempts,reads});
   }catch(e){results.push({width,workspace,variant,pass:false,error:e.message,attempts,reads});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+workspace+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,32);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c31-maintenance-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium all4 workspaces; admin actual form draft and zero write replay, synthetic API maintenance/faults only; no live Identity/SQL, required-audit rollback, actual devices or production qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('32 native maintenance cases PASS; mobile/desktop, synthetic API faults, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
