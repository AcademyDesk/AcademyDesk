// Support triage in real Chromium. Synthetic APIs only; not live Identity/SQL/device acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/support-triage-browser');
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
 const results=[],errors=[],requests=[];let child,socket,cdp,profile,started=false,passed=false,failure=null,apiOrigin;
 const academy='11111111-1111-4111-8111-111111111111',invoice='22222222-2222-4222-8222-222222222222',support='33333333-3333-4333-8333-333333333333';const base='/api/academies/'+academy,clock=Date.parse('2026-11-07T12:00:00Z');
 const defer=()=>{let release;return{promise:new Promise(r=>release=r),release:()=>release()};};
 try{
  const selected=process.env.QA_STATUS_API_ORIGIN||'http://127.0.0.1:49142',url=new URL(selected);if(url.protocol!=='http:'||url.hostname!=='127.0.0.1'||!url.port||Number(url.port)<1024||selected!==url.origin||url.username||url.password)throw Error('Only a canonical unprivileged HTTP127.0.0.1origin is allowed.');apiOrigin=url.origin;
  if(!executable||!fs.existsSync(path.join(out,'platform/control.html')))throw Error('Use installed Chromium and an exact-source export.');await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c16-support-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe'],windowsHide:true});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',e=>{clearTimeout(timer);reject(e);});child.once('exit',()=>{clearTimeout(timer);reject(Error('Browser exited before debugger'));});child.stderr.on('data',data=>{log+=data;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  async function page(route,config){
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width:config.width||1280,height:1000,deviceScaleFactor:1,mobile:false},sid);await cdp.send('Emulation.setTimezoneOverride',{timezoneId:config.zone||'UTC'},sid);
   const state={writes:[],grantReads:0,closing:false,status:config.kind==='case'?'Open':'Issued',commits:0,reject:0,transport:false,commitOnLoss:true,failRead:false,hold:null,authority:null};
   cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
   const recordInvoices=()=>[{id:invoice,academyId:academy,academyName:'Synthetic academy',invoiceNumber:'QA-BILL',amount:100,currency:'INR',dueDate:'2026-12-01',status:config.kind==='invoice'?state.status:'Issued'},{id:'other-invoice',academyId:academy,academyName:'Unrelated academy',invoiceNumber:'QA-OTHER',amount:200,currency:'INR',dueDate:'2026-12-01',status:'Issued'}];
   const recordCases=()=>config.caseRows;
   cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
    const u=new URL(request.url);if(u.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);if(u.origin!==apiOrigin)throw Error('Unexpected external origin');if(request.method==='OPTIONS')return await reply(204,{});requests.push({method:request.method,path:u.pathname});
    if(request.method!=='GET'){
     a.ok(config.kind);a.equal(request.method,'PATCH');a.equal(u.pathname,config.kind==='invoice'?'/api/platform/billing-invoices/'+invoice+'/status':'/api/platform/support-cases/'+support);const body=JSON.parse(request.postData);state.writes.push({path:u.pathname,body});if(state.hold)await state.hold;
     if(state.reject)return await reply(state.reject,{message:'Synthetic denial'});if(!body.status)return await reply(400,{message:'Status required'});
     if(!state.transport||state.commitOnLoss){state.commits++;state.status=state.authority||body.status;}if(state.transport)return await cdp.send('Fetch.failRequest',{requestId,errorReason:'Failed'},sid);return await reply(200,{});
    }
    if(u.pathname==='/api/academies')return await reply(200,[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Sales","Students"]'}]);
    if(u.pathname==='/api/auth/session')return await reply(200,{workspace:config.kind?'PlatformOwner':'AcademyAdmin',academyId:academy,displayName:config.kind?'Synthetic owner':'Synthetic admin',roles:[config.kind?'PlatformOwner':'AcademyAdmin'],isPlatformOwner:Boolean(config.kind)});
    if(u.pathname==='/api/portal/announcements')return await reply(200,[]);
    if(u.pathname===base+'/staff')return await reply(200,[]);
    if(u.pathname===base+'/access-grants'){
     state.grantReads++;return await reply(200,config.invalid?[{id:'invalid',userName:'Invalid',permissions:[],isPermanent:false,expiresAtUtc:'invalid'}]:[
      {id:'permanent',userName:'Permanent',permissions:['students.manage'],isPermanent:true,expiresAtUtc:'2026-01-01T00:00:00Z'},
      {id:'future',userName:'Future',permissions:['students.manage'],isPermanent:false,expiresAtUtc:new Date(clock+150).toISOString()},
      {id:'boundary',userName:'Boundary',permissions:['students.manage'],isPermanent:false,expiresAtUtc:new Date(clock).toISOString()},
      {id:'past',userName:'Past',permissions:['students.manage'],isPermanent:false,expiresAtUtc:new Date(clock-1).toISOString()},
      {id:'revoked',userName:'Revoked',permissions:['students.manage'],isPermanent:true,revokedAtUtc:new Date(clock-1000).toISOString()}
     ]);
    }
    if(u.pathname===base+'/leads')return await reply(200,[{id:'due',fullName:'Due lead',source:'Referral',stage:'Contacted',email:'due@example.invalid',followUpAtUtc:'2026-01-01T00:00:00Z'},{id:'trial',fullName:'Trial lead',source:'Web',stage:'TrialBooked'},{id:'converted',fullName:'Converted lead',source:'Web',stage:'Converted',convertedStudentId:'student'}]);
    if(u.pathname==='/api/platform/academies')return await reply(200,[{id:academy,name:'Synthetic academy',subscriptionPlan:'Trial',subscriptionStatus:'Trial',enabledModulesJson:'["Core"]',studentLimit:10,staffLimit:5}]);
    if(u.pathname==='/api/platform/academies/'+academy+'/onboarding')return await reply(200,{});
    if(u.pathname==='/api/platform/billing-invoices'){if(state.failRead&&state.commits)return await reply(503,{});return await reply(200,recordInvoices());}
    if(u.pathname==='/api/platform/support-cases'){if(state.failRead&&state.commits)return await reply(503,{});return await reply(200,recordCases());}
    if(u.pathname==='/api/platform/settings')return await reply(200,{platformName:'Synthetic platform',defaultCurrency:'INR',defaultTrialDays:14,dataRetentionDays:90});
    if(u.pathname==='/api/platform/health')return await reply(200,{api:'Healthy',database:'Healthy',backgroundJobs:'Ready',communicationProviders:'Ready'});
    if(['/api/platform/admins','/api/platform/audit','/api/platform/announcements'].includes(u.pathname))return await reply(200,[]);
    throw Error('Unexpected API path '+u.pathname);
    async function reply(status,body){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, PATCH, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(body)).toString('base64')},sid);}
   }catch(e){if(state.closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
   await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);const clockScript=route==='access-review'?`globalThis.__qaNow=${clock};Date.now=()=>globalThis.__qaNow;`:'';
   await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){${clockScript}localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(config.width===320?'light':'dark')});for(const portal of ['AcademyAdmin','PlatformOwner']){localStorage.setItem('academydesk.accessToken.'+portal,'synthetic-access');localStorage.setItem('academydesk.refreshToken.'+portal,'synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.'+portal,'synthetic-epoch');}}`},sid);
   const evaluate=async expr=>{const v=await cdp.send('Runtime.evaluate',{expression:expr,awaitPromise:true,returnByValue:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expr=>{const end=Date.now()+6500;while(Date.now()<end){if(await evaluate('Boolean('+expr+')'))return;await sleep(30);}throw Error('DOM wait failed: '+expr);};
   const name=config.kind==='invoice'?'invoice-status-'+invoice:'case-status-'+support,field=`document.querySelector('[name="${name}"]')`,trigger=field+`?.parentElement.querySelector('button')`;
   const choose=async value=>{await evaluate(`${trigger}.click()`);const label=value||(config.kind==='invoice'?'Update status':'Update status');await wait(`Array.from(document.querySelectorAll('[role="option"]')).some(b=>b.textContent===${JSON.stringify(label)})`);await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).find(b=>b.textContent===${JSON.stringify(label)}).click()`);};
   const refresh=async()=>{await wait(`Array.from(document.querySelectorAll('button')).some(b=>b.textContent==='Refresh platform status lists')`);await evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Refresh platform status lists').click()`);};
   await cdp.send('Page.navigate',{url:origin+'/'+route+(config.kind?'?tab='+(config.kind==='invoice'?'Billing':'Support'):config.view?'?view='+config.view:'')},sid);
   return{sid,state,evaluate,wait,field,trigger,choose,refresh,close:()=>{state.closing=true;return cdp.send('Target.closeTarget',{targetId});}};
  }
  async function run(route,name,config,check){let p;try{p=await page(route,config);await check(p);results.push({route,case:name,...config,pass:true,writes:p.state.writes});}catch(e){results.push({route,case:name,...config,pass:false,error:e.message,writes:p?.state.writes||[]});}finally{if(p)await p.close();}}
  const caseRows=['Low','Normal','High','Urgent','Critical'].flatMap(priority=>['Open','In progress','Resolved','Closed'].map(status=>({id:priority+'-'+status,academyId:academy,academyName:'Synthetic academy',subject:priority+'-'+status,priority,status,description:'Synthetic request'}))).concat([{id:'lower-critical',subject:'lower-critical',priority:'critical',status:'Open'},{id:'spaced-critical',subject:'spaced-critical',priority:' CRITICAL ',status:'Open'},{id:'unknown',subject:'unknown',priority:'Deferred',status:'Open'}]);
  for(const width of [320,1440])for(const label of ['All cases','Open cases','High priority','Resolved'])await run('platform/control',label,{width,kind:'case',caseRows},async p=>{
   const expected=caseRows.filter(x=>label==='All cases'||(label==='Open cases'?!['Resolved','Closed'].includes(x.status):label==='Resolved'?['Resolved','Closed'].includes(x.status):['high','urgent','critical'].includes(x.priority.trim().toLowerCase())&&!['Resolved','Closed'].includes(x.status)));
   const button=`Array.from(document.querySelectorAll('.platform-support-kpis button')).find(b=>b.querySelector('span')?.textContent===${JSON.stringify(label)})`;
   await p.wait(`document.querySelectorAll('.platform-case-register li').length===23`);await p.wait(button);a.equal(await p.evaluate(button+`.querySelector('b').textContent`),String(expected.length));await p.evaluate(button+'.click()');await p.wait(`document.querySelector('[role="dialog"]')`);
   a.deepEqual((await p.evaluate(`Array.from(document.querySelectorAll('[role="dialog"] li b')).map(n=>n.textContent)`)).sort(),expected.map(x=>x.subject).sort());a.equal(p.state.writes.length,0);
   await p.evaluate(`document.querySelector('[aria-label="Close support case details"]').click()`);a.equal(await p.evaluate(`Boolean(document.querySelector('[role="dialog"]'))`),false);
  });
  a.equal(results.length,8);a.equal(results.filter(r=>!r.pass).length,0,'Every native support triage case must pass');a.deepEqual(errors,[]);a.equal(requests.filter(r=>/penta|model|inference/.test(r.path)).length,0);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile){try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c16-support-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;failure??='Owned cleanup failed';}}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,apiOrigin,sandboxDisabled:false,browserSpawnRequested:Boolean(child),ownedProfileRemoved:!cleanupError,cleanupError,results,errors,requests,scope:'Real Chromium DOM triage counts/details and modal close with synthetic APIs. Not real Identity/SQL/permission/device acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('8 native support triage cases PASS; synthetic APIs, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
