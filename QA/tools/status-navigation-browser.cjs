// Access/status/navigation UI in real Chromium. Synthetic APIs only; not live Identity/SQL/device acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/status-navigation-browser');
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
  if(!executable||!fs.existsSync(path.join(out,'access-review.html')))throw Error('Use installed Chromium and an exact-source export.');await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c11-status-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe'],windowsHide:true});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',e=>{clearTimeout(timer);reject(e);});child.once('exit',()=>{clearTimeout(timer);reject(Error('Browser exited before debugger'));});child.stderr.on('data',data=>{log+=data;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  async function page(route,config){
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width:config.width||1280,height:1000,deviceScaleFactor:1,mobile:false},sid);await cdp.send('Emulation.setTimezoneOverride',{timezoneId:config.zone||'UTC'},sid);
   const state={writes:[],grantReads:0,closing:false,status:config.kind==='case'?'Open':'Issued',commits:0,reject:0,transport:false,commitOnLoss:true,failRead:false,hold:null,authority:null};
   cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
   const recordInvoices=()=>[{id:invoice,academyId:academy,academyName:'Synthetic academy',invoiceNumber:'QA-BILL',amount:100,currency:'INR',dueDate:'2026-12-01',status:config.kind==='invoice'?state.status:'Issued'},{id:'other-invoice',academyId:academy,academyName:'Unrelated academy',invoiceNumber:'QA-OTHER',amount:200,currency:'INR',dueDate:'2026-12-01',status:'Issued'}];
   const recordCases=()=>[{id:support,academyId:academy,academyName:'Synthetic academy',subject:'Synthetic case',description:'Synthetic detail',priority:'High',status:config.kind==='case'?state.status:'Open'},{id:'other-case',academyId:academy,academyName:'Unrelated academy',subject:'Unrelated case',priority:'Normal',status:'Open'}];
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
  for(const zone of ['Asia/Kolkata','UTC','Pacific/Honolulu','Pacific/Kiritimati'])for(const width of [320,1440])await run('access-review','expiry-boundary',{zone,width},async p=>{
   await p.wait(`document.querySelector('.access-grants-panel header > span')?.textContent.trim()==='2 active'`);
   const initial=await p.evaluate(`Object.fromEntries(Array.from(document.querySelectorAll('.access-grants-panel li')).map(row=>[row.querySelector('b').textContent,row.querySelector('[data-grant-status]')?.textContent]))`);a.deepEqual(initial,{Permanent:'Active',Future:'Active',Boundary:'Expired',Past:'Expired',Revoked:'Revoked'});
   const reads=p.state.grantReads;await p.evaluate(`globalThis.__qaNow=${clock+149}`);await sleep(180);a.equal(await p.evaluate(`document.querySelector('.access-grants-panel header > span').textContent.trim()`),'2 active');await p.evaluate(`globalThis.__qaNow=${clock+150}`);await p.wait(`document.querySelector('.access-grants-panel header > span')?.textContent.trim()==='1 active'`);a.equal(p.state.grantReads,reads);a.equal(p.state.writes.length,0);
  });
  await run('access-review','invalid-expiry',{invalid:true,width:1280},async p=>{await p.wait(`document.querySelector('.access-grants-panel')?.textContent.includes('No valid expiry')`);a.match(await p.evaluate(`document.querySelector('.access-grants-panel header > span').textContent`),/0 active/);a.equal(p.state.writes.length,0);});
  for(const kind of ['invoice','case'])for(const width of [320,1440])for(const scenario of ['rejected','server-failure','lost-commit','failed-read','fresh-authority','pending','placeholder'])await run('platform/control',scenario,{kind,width},async p=>{
   const initial=kind==='invoice'?'Issued':'Open',target=kind==='invoice'?'Paid':'Closed',gate=defer();await p.wait(`${p.field}?.value===${JSON.stringify(initial)}`);
   try{
    if(scenario==='rejected')p.state.reject=403;if(scenario==='server-failure')p.state.reject=500;if(scenario==='lost-commit')p.state.transport=true;if(scenario==='failed-read')p.state.failRead=true;if(scenario==='fresh-authority')p.state.authority=kind==='invoice'?'Overdue':'In progress';if(scenario==='pending')p.state.hold=gate.promise;
    await p.choose(scenario==='placeholder'?'':target);
    if(scenario==='placeholder'){await sleep(80);a.equal(p.state.writes.length,0);a.equal(await p.evaluate(`${p.field}.value`),initial);return;}
    for(let i=0;i<100&&!p.state.writes.length;i++)await sleep(20);a.equal(p.state.writes.length,1);
    if(scenario==='pending'){a.equal(await p.evaluate(`${p.field}.value`),initial);a.equal(await p.evaluate(`${p.trigger}.disabled`),true);await p.evaluate(`${p.trigger}.click()`);await sleep(60);a.equal(p.state.writes.length,1);gate.release();await p.wait(`${p.field}?.value===${JSON.stringify(target)}`);}
    else if(scenario==='rejected'){await p.wait(`document.querySelector('main')?.textContent.includes('Synthetic denial')||document.querySelector('.platform-control-content')?.textContent.includes('Synthetic denial')`);a.equal(await p.evaluate(`${p.field}.value`),initial);a.equal(p.state.commits,0);}
    else if(scenario==='server-failure'||scenario==='lost-commit'){await p.wait(`document.body.textContent.includes('could not be confirmed')`);a.equal(await p.evaluate(`${p.field}.value`),initial);a.equal(await p.evaluate(`${p.trigger}.disabled`),true);await p.evaluate(`${p.trigger}.click()`);a.equal(p.state.writes.length,1);if(scenario==='lost-commit'){a.equal(p.state.commits,1);p.state.transport=false;await p.refresh();await p.wait(`${p.field}?.value===${JSON.stringify(target)}&&!${p.trigger}?.disabled`);a.equal(p.state.writes.length,1);}}
    else if(scenario==='failed-read'){await p.wait(`document.body.textContent.includes('saved status could not be refreshed')`);a.equal(await p.evaluate(`${p.field}.value`),initial);a.equal(await p.evaluate(`${p.trigger}.disabled`),true);await p.refresh();await p.wait(`document.body.textContent.includes('lists could not be refreshed')`);a.equal(p.state.writes.length,1);p.state.failRead=false;await p.refresh();await p.wait(`${p.field}?.value===${JSON.stringify(target)}&&!${p.trigger}?.disabled`);a.equal(p.state.writes.length,1);}
    else await p.wait(`${p.field}?.value===${JSON.stringify(p.state.authority)}`);
    const otherName=kind==='invoice'?'invoice-status-other-invoice':'case-status-other-case';a.equal(await p.evaluate(`document.querySelector('[name="${otherName}"]').value`),kind==='invoice'?'Issued':'Open');if(kind==='case')a.equal(p.state.writes[0].body.priority,'High');
   }finally{gate.release();}
  });
  for(const width of [320,1440])for(const [label,href,view] of [['Leads','/leads','overview'],['Follow-ups due','/sales-marketing?view=follow-ups','overview'],['Trial bookings','/trial-bookings','overview'],['Converted','/sales-marketing?view=conversion','overview'],['Total leads','/leads','conversion'],['Conversion rate','/sales-marketing?view=conversion','conversion']])await run('sales-marketing',label,{width,view},async p=>{
   await p.wait(`Array.from(document.querySelectorAll('.sales-tile')).some(b=>b.querySelector('span')?.textContent===${JSON.stringify(label)})`);await p.evaluate(`Array.from(document.querySelectorAll('.sales-tile')).find(b=>b.querySelector('span')?.textContent===${JSON.stringify(label)}).click()`);await p.wait(`document.querySelector('[role="dialog"] a')?.textContent==='Open details'`);a.equal(await p.evaluate(`document.querySelector('[role="dialog"] a').getAttribute('href')`),href);
   if(label==='Follow-ups due'){await p.evaluate(`document.querySelector('[role="dialog"] a').focus()`);await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'Enter',code:'Enter',windowsVirtualKeyCode:13},p.sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'Enter',code:'Enter',windowsVirtualKeyCode:13},p.sid);await p.wait(`location.search==='?view=follow-ups'&&document.querySelector('h1')?.textContent==='Follow-ups'`);a.equal(await p.evaluate(`Boolean(document.querySelector('[role="dialog"]'))`),false);await p.evaluate('history.back()');await p.wait(`document.querySelector('h1')?.textContent==='Sales Overview'`);}
   a.equal(p.state.writes.length,0);
  });
  a.equal(results.length,49);a.equal(results.filter(r=>!r.pass).length,0,'Every native status/navigation case must pass');a.deepEqual(errors,[]);a.equal(requests.filter(r=>/penta|model|inference/.test(r.path)).length,0);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile){try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c11-status-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;failure??='Owned cleanup failed';}}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,apiOrigin,sandboxDisabled:false,browserSpawnRequested:Boolean(child),ownedProfileRemoved:!cleanupError,cleanupError,results,errors,requests,scope:'Real Chromium DOM/keyboard/navigation with synthetic APIs and controlled display clock. Not real Identity/SQL/permission/device acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('49 native access/status/navigation cases PASS; synthetic APIs, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
