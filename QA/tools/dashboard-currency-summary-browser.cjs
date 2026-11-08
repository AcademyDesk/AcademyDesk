// Dashboard currency rendering and fee/payroll detail isolation in Chromium; synthetic APIs only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/dashboard-currency-summary-browser');
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
 const selected=process.env.QA_CURRENCY_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 const totals=(currency,changes={})=>({currency,grossBilled:1000,approvedAdjustments:200,collected:400,outstanding:400,reconciled:100,...changes});
 function summary(variant){
  const meta={activeStudents:2,activeTeachers:1,activeCourses:1,activeBatches:1,openLeads:0,attendanceRecordsLast30Days:2,presentAttendanceLast30Days:1,classesToday:0,todaySchedule:[],recentActivity:[]};
  if(variant==='legacy')return{...meta,outstandingBalance:9999};
  if(variant==='empty')return{...meta,byCurrency:[]};
  if(variant==='usd'||variant==='unknown-detail')return{...meta,byCurrency:[totals('USD',{outstanding:70.15})]};
  if(variant==='malformed')return{...meta,byCurrency:[totals('USD',{outstanding:null})]};
  return{...meta,outstandingBalance:9999,byCurrency:[totals('INR',{outstanding:400.25}),totals('USD',{outstanding:70.15})]};
 }
 function invoices(variant){return variant==='empty'?[]:[
  {id:'due',studentId:'s',invoiceNumber:'FEE-DUE',totalAmount:100,adjustedAmount:19.85,paidAmount:10,balance:70.15,currency:variant==='unknown-detail'?undefined:'USD',status:'Issued'},
  {id:'cancelled',studentId:'s',invoiceNumber:'FEE-CANCELLED',totalAmount:500,paidAmount:0,balance:500,currency:'INR',status:'cancelled'},
  {id:'overpaid',studentId:'s',invoiceNumber:'FEE-OVERPAID',totalAmount:100,paidAmount:200,balance:0,currency:'USD',status:'Paid'}];}
 function payroll(variant){return variant==='empty'?[]:[
  {id:'due',workerName:'Synthetic Staff',periodLabel:'October',netAmount:999.55,currency:variant==='unknown-detail'?undefined:'EUR',status:'Approved'},
  {id:'paid',workerName:'Paid worker',periodLabel:'October',netAmount:10000,currency:'INR',status:'Paid'}];}
 try{
  if(!executable||!fs.existsSync(path.join(out,'dashboard.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c19-dashboard-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const variant of ['mixed','usd','empty','legacy','malformed','unknown-detail','unavailable']){const route='dashboard';
   const{targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+7000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out');};
   const card=label=>`Array.from(document.querySelectorAll('.enterprise-kpi-grid > button')).find(n=>n.querySelector('span')?.textContent===${JSON.stringify(label)})`;
   const metric=label=>evaluate(card(label)+`?.querySelector('strong')?.textContent`);
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});a.equal(request.method,'GET');requests.push({method:request.method,path:url.pathname});let data;
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['AcademyAdmin'],displayName:'Synthetic admin',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Finance","FinanceControls"]'}];
     else if(url.pathname===base+'/dashboard'){if(variant==='unavailable')return reply(503,{});data=summary(variant);}
     else if(url.pathname===base+'/students')data=[{id:'s',firstName:'Synthetic',lastName:'Student',isActive:true}];
     else if(url.pathname===base+'/invoices')data=invoices(variant);
     else if(url.pathname===base+'/payroll/payouts')data=payroll(variant);
     else if(url.pathname==='/api/portal/announcements'||url.pathname===base+'/sessions')data=[];
     else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/'+route},sid);
    if(variant==='unavailable'){
     await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be loaded')`);
     a.equal(await evaluate(`Boolean(document.querySelector('[aria-label="Academy operating indicators"]'))`),false);
     a.equal(await evaluate(`Array.from(document.querySelectorAll('button')).some(n=>n.textContent==='Try again')`),true);
     results.push({width,route,variant,pass:true,recoveryGated:true});continue;
    }
    await wait(`${card('Active students')}?.querySelector('strong')?.textContent==='2'`);
    const value=await metric('Outstanding fees');
    if(variant==='mixed')a.match(value,/INR.*400\.25.*USD.*70\.15/);
    else if(variant==='usd'||variant==='unknown-detail'){a.match(value,/USD.*70\.15/);a.doesNotMatch(value,/INR|₹/);}
    else if(variant==='empty')a.equal(value,'0.00');
    else a.equal(value,'Currency details unavailable');
    a.doesNotMatch(value,/999|NaN/);
    await evaluate(card('Outstanding fees')+'.click()');await wait(`Boolean(document.querySelector('[role="dialog"]'))`);
    const detail=await evaluate(`document.querySelector('[role="dialog"]').textContent`);
    a.match(detail,/Student fee payments/);a.match(detail,/Payroll records not marked Paid/);
    if(variant==='empty'){a.match(detail,/No student fees are outstanding/);a.match(detail,/No payroll records are listed/);}
    else{a.match(detail,/FEE-DUE/);a.doesNotMatch(detail,/FEE-CANCELLED|FEE-OVERPAID|Paid worker/);
     if(variant==='unknown-detail')a.equal((detail.match(/Currency details unavailable/g)||[]).length,2);
     else{a.match(detail,/USD.*70\.15 due/);a.match(detail,/EUR.*999\.55.*Approved/);}
    }
    await evaluate(`document.querySelector('[aria-label="Close details"]').click()`);await wait(`!document.querySelector('[role="dialog"]')`);
    a.equal(await metric('Attendance rate'),'50%');a.equal(await metric('Active students'),'2');
    results.push({width,route,variant,pass:true,value,detailVerified:true});
   }catch(e){results.push({width,route,variant,pass:false,error:e.message});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});}
  }
  a.equal(results.length,14);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c19-dashboard-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium dashboard fees and separate payroll details with synthetic APIs; no live Identity/SQL/device/FX qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('14 native dashboard currency scenarios PASS; zero API writes, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
