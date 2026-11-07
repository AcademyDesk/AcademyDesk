// Batched calendar/date/input UI in real Chromium. Synthetic APIs only; not live Identity/SQL/device acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/academic-inputs-browser');
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
 const academy='11111111-1111-4111-8111-111111111111',student='22222222-2222-4222-8222-222222222222',course='33333333-3333-4333-8333-333333333333',teacher='44444444-4444-4444-8444-444444444444',batch='55555555-5555-4555-8555-555555555555';const base='/api/academies/'+academy;
 try{
  const selected=process.env.QA_ACADEMIC_API_ORIGIN||'http://127.0.0.1:49142',url=new URL(selected);if(url.protocol!=='http:'||url.hostname!=='127.0.0.1'||!url.port||Number(url.port)<1024||selected!==url.origin||url.username||url.password)throw Error('Only a canonical unprivileged HTTP127.0.0.1origin is allowed.');apiOrigin=url.origin;
  if(!executable||!fs.existsSync(path.join(out,'calendar.html')))throw Error('Use installed Chromium and the current exact-source export.');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c10-academic-'));
  child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe'],windowsHide:true});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',e=>{clearTimeout(timer);reject(e);});child.once('exit',()=>{clearTimeout(timer);reject(Error('Browser exited before debugger'));});child.stderr.on('data',data=>{log+=data;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  async function page(route,config){
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width:config.width||1280,height:1000,deviceScaleFactor:1,mobile:false},sid);await cdp.send('Emulation.setTimezoneOverride',{timezoneId:config.zone||'Asia/Kolkata'},sid);
   const state={writes:[],closing:false,fees:[],modules:[]};
   cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
   cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
    const u=new URL(request.url);if(u.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);if(u.origin!==apiOrigin)throw Error('Unexpected external origin');if(request.method==='OPTIONS')return await reply(204,{});requests.push({method:request.method,path:u.pathname});
    if(request.method!=='GET'){
     a.equal(request.method,'POST');const body=JSON.parse(request.postData);state.writes.push({path:u.pathname,body});
     if(route==='curriculum'){a.equal(u.pathname,base+'/course-modules');if(!Number.isInteger(body.sequence)||body.sequence<1)return await reply(400,{});state.modules.push({id:'saved',...body,isPublished:false});return await reply(200,state.modules[0]);}
     a.equal(route,'student-fees');a.equal(u.pathname,base+'/students/'+student+'/fee-arrangements');a.ok(body.amount>0);state.fees.push({id:'saved',...body,isActive:true});return await reply(200,state.fees[0]);
    }
    if(u.pathname==='/api/academies')return await reply(200,[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Finance","Academics","Students","Teachers"]'}]);
    if(u.pathname==='/api/auth/session')return await reply(200,{workspace:'AcademyAdmin',academyId:academy,displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false});
    if(u.pathname==='/api/portal/announcements')return await reply(200,[]);
    if(u.pathname===base+'/students')return await reply(200,[{id:student,firstName:'Synthetic',lastName:'Learner',isActive:true}]);
    if(u.pathname===base+'/teachers')return await reply(200,[{id:teacher,firstName:'Synthetic',lastName:'Teacher'}]);
    if(u.pathname===base+'/courses')return await reply(200,[{id:course,name:'Synthetic subject',isActive:true}]);
    if(u.pathname===base+'/batches')return await reply(200,[{id:batch,name:'Synthetic class',courseId:course,teacherId:teacher}]);
    if(u.pathname===base+'/enrollments')return await reply(200,[{studentId:student,batchId:batch,status:'Active'}]);
    if(u.pathname===base+'/sessions')return await reply(200,[{id:'begin',batchId:batch,teacherId:teacher,startUtc:'2026-10-31T19:00:00Z',deliveryMode:'Online',roomName:'https://meeting.example.invalid/begin',status:'Scheduled'},{id:'end',batchId:batch,teacherId:teacher,startUtc:'2026-11-30T18:00:00Z',deliveryMode:'Online',roomName:'https://meeting.example.invalid/end',status:'Scheduled'},{id:'cancel',batchId:batch,teacherId:teacher,startUtc:'2026-11-01T06:00:00Z',deliveryMode:'Online',roomName:'https://meeting.example.invalid/cancel',status:'Cancelled'}]);
    if(u.pathname===base+'/events'||u.pathname===base+'/makeup-classes')return await reply(200,[]);
    if(u.pathname===base+'/academic-governance')return await reply(200,{years:[{id:'year',name:'Synthetic year',startDate:'2026-01-01',endDate:'2026-12-31',isCurrent:true,isClosed:false}],terms:[]});
    if(u.pathname===base+'/holidays')return await reply(200,[{id:'holiday',name:'Synthetic holiday',holidayDate:'2026-01-01'}]);
    if(u.pathname===base+'/course-modules')return await reply(200,state.modules);
    if(u.pathname===base+'/students/'+student+'/fee-arrangements/admission-fee')return await reply(200,{amount:0,dueDate:null});
    if(u.pathname===base+'/students/'+student+'/fee-arrangements')return await reply(200,state.fees);
    throw Error('Unexpected API path '+u.pathname);
    async function reply(status,body){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(body)).toString('base64')},sid);}
   }catch(e){if(state.closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
   await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
   const clock=route==='calendar'?`const OriginalDate=Date;globalThis.Date=class extends OriginalDate{constructor(...args){super(...(args.length?args:['2026-10-31T18:45:00Z']));}static now(){return OriginalDate.parse('2026-10-31T18:45:00Z');}};`:'';
   await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){${clock}localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(config.width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
   const evaluate=async expr=>{const v=await cdp.send('Runtime.evaluate',{expression:expr,awaitPromise:true,returnByValue:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expr=>{const end=Date.now()+7000;while(Date.now()<end){if(await evaluate('Boolean('+expr+')'))return;await sleep(40);}throw Error('DOM wait failed: '+expr);};
   const fill=async(expr,value)=>{await evaluate(`(${expr}).focus()`);await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'a',code:'KeyA',windowsVirtualKeyCode:65,modifiers:2},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'a',code:'KeyA',windowsVirtualKeyCode:65},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'Backspace',code:'Backspace',windowsVirtualKeyCode:8},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'Backspace',code:'Backspace',windowsVirtualKeyCode:8},sid);if(value)await cdp.send('Input.insertText',{text:value},sid);await wait(`(${expr}).value===${JSON.stringify(value)}`);};
   await cdp.send('Page.navigate',{url:origin+'/'+route},sid);return{sid,state,evaluate,wait,fill,close:()=>{state.closing=true;return cdp.send('Target.closeTarget',{targetId});}};
  }
  async function run(route,name,config,check){let p;try{p=await page(route,config);await check(p);results.push({route,case:name,...config,pass:true,writes:p.state.writes});}catch(e){results.push({route,case:name,...config,pass:false,error:e.message,writes:p?.state.writes||[]});}finally{if(p)await p.close();}}
  for(const zone of ['Asia/Kolkata','UTC','Pacific/Honolulu','Pacific/Kiritimati'])for(const width of [320,1440])await run('calendar','India-month-and-cancelled',{zone,width},async p=>{
   await p.wait(`document.querySelectorAll('.calendar-event').length>=3`);a.equal(await p.evaluate(`document.querySelector('.workspace-calendar-panel-header h2').textContent`),'November 2026');
   for(const [id,day] of [['begin','1'],['end','30']]){a.equal(await p.evaluate(`document.querySelector('a[href="https://meeting.example.invalid/${id}"]')?.closest('.calendar-day').querySelector('.calendar-date').textContent`),day);}
   await p.wait(`document.querySelectorAll('#calendar-month-agenda-list > li').length===3`);
   a.equal(await p.evaluate(`Boolean(document.querySelector('a[href="https://meeting.example.invalid/cancel"]'))`),false);
   a.equal(await p.evaluate(`Array.from(document.querySelectorAll('.calendar-event')).some(e=>e.textContent.includes('Cancelled')&&e.tagName==='DIV')`),true);
   await p.evaluate(`document.querySelector('[aria-label="Previous month"]').click()`);await p.wait(`document.querySelector('.workspace-calendar-panel-header h2')?.textContent==='October 2026'`);a.equal(await p.evaluate(`document.querySelectorAll('#calendar-month-agenda-list > li').length`),0);
   await p.evaluate(`document.querySelector('[aria-label="Next month"]').click()`);await p.wait(`document.querySelectorAll('#calendar-month-agenda-list > li').length===3`);a.equal(p.state.writes.length,0);
  });
  for(const zone of ['Asia/Kolkata','UTC','Pacific/Honolulu','Pacific/Kiritimati'])for(const route of ['academic-periods','holidays'])await run(route,'civil-date',{zone,width:1280},async p=>{
   await p.wait(`document.querySelector('main')?.textContent.includes(${JSON.stringify(route==='holidays'?'Synthetic holiday':'Synthetic year')})`);const t=await p.evaluate(`document.querySelector('main').textContent`);a.match(t,/01-Jan-2026|1 Jan 2026/);a.doesNotMatch(t,/02-Jan-2026|2 Jan 2026/);a.equal(p.state.writes.length,0);
  });
  for(const width of [320,1440])for(const sequence of ['','0','1.5','2'])await run('curriculum','sequence-'+sequence,{width},async p=>{
   await p.wait(`document.querySelector('[name="course"]')?.value===${JSON.stringify(course)}`);await p.fill(`document.querySelector('input[placeholder="Module or chapter"]')`,'Synthetic module');await p.fill(`document.querySelector('input[type="number"]')`,sequence);
   a.equal(await p.evaluate(`document.querySelector('input[type="number"]').checkValidity()`),sequence==='2');await p.evaluate(`document.querySelector('form').requestSubmit()`);
   if(sequence==='2'){for(let i=0;i<100&&!p.state.writes.length;i++)await sleep(20);a.equal(p.state.writes.length,1);a.equal(p.state.writes[0].body.sequence,2);}else{await sleep(80);a.equal(p.state.writes.length,0);}
  });
  for(const width of [320,1440])for(const amount of ['1.50','0.50','0.01'])await run('student-fees','fraction-'+amount,{width},async p=>{
   await p.wait(`document.querySelector('input[placeholder="Subject, e.g. Piano"]')&&!document.querySelector('input[placeholder="Subject, e.g. Piano"]').disabled`);
   await p.fill(`document.querySelector('input[placeholder="Subject, e.g. Piano"]')`,'Synthetic subject');const field=`document.querySelector('input[placeholder="Subject, e.g. Piano"]').closest('form').querySelector('input[type="number"]')`;await p.fill(field,amount);a.equal(await p.evaluate(`(${field}).checkValidity()`),true);await p.evaluate(`document.querySelector('input[placeholder="Subject, e.g. Piano"]').closest('form').requestSubmit()`);
   await p.wait(`document.querySelector('input[placeholder="Subject, e.g. Piano"]').value===''`);a.equal(p.state.writes.length,1);a.equal(p.state.writes[0].body.amount,Number(amount));
  });
  a.equal(results.length,30);a.equal(results.filter(v=>!v.pass).length,0,'Every native academic batch case must pass');a.deepEqual(errors,[]);a.equal(requests.filter(r=>/penta|model|inference/.test(r.path)).length,0);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile){try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c10-academic-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;failure??='Owned cleanup failed';}}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,apiOrigin,sandboxDisabled:false,browserSpawnRequested:Boolean(child),ownedProfileRemoved:!cleanupError,cleanupError,results,errors,requests,scope:'Real Chromium DOM/keyboard/forms with explicit timezones and synthetic API/clock fixtures. Not Identity/SQL/physical-device/critical acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('30 native academic/calendar/input cases PASS; synthetic APIs only, normal sandbox, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
