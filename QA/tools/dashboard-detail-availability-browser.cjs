// Dashboard detail availability in real Chromium with controlled independent API faults.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/dashboard-detail-availability-browser');
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
 function dataset(variant){return{
  dashboard:{activeStudents:1,activeTeachers:1,activeCourses:1,activeBatches:1,openLeads:0,attendanceRecordsLast30Days:2,presentAttendanceLast30Days:1,classesToday:1,
    byCurrency:[{currency:'USD',grossBilled:100.55,approvedAdjustments:.05,collected:20.1,outstanding:80.4,reconciled:20.1}],
    todaySchedule:[{id:'schedule',batchName:'Primary schedule',teacherName:'Synthetic Teacher',startUtc:'2026-11-08T10:00:00Z',status:'Scheduled'}],recentActivity:[]},
  students:variant==='empty'?[]:[{id:'s',firstName:'Synthetic',lastName:'Student',isActive:true}],
  invoices:variant==='empty'?[]:[{id:'i',studentId:'s',invoiceNumber:'USD-DUE',balance:80.4,currency:'USD',status:'Issued'}],
  payroll:variant==='empty'?[]:[{id:'p',workerName:'Synthetic Staff',periodLabel:'October',netAmount:999.55,currency:'EUR',status:'Approved'}],
  sessions:variant==='empty'?[]:[{id:'ss'}],attendance:[{studentId:'s',status:'Present'},{studentId:'s',status:'Absent'}]};}
 try{
  if(!executable||!fs.existsSync(path.join(out,'dashboard.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c21-dashboard-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const variant of ['valid','empty','invoices403','payroll503','students403','sessions503','attendance503','both503','transportinvoice','malformedpayroll','pendinginvoice','session503','reload']){const route='dashboard';let invoiceFailures=0,release;
   const{targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+7000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out');};
   const card=label=>`Array.from(document.querySelectorAll('.enterprise-kpi-grid > button')).find(n=>n.querySelector('span')?.textContent===${JSON.stringify(label)})`;
   const metric=label=>evaluate(card(label)+`?.querySelector('strong')?.textContent`);
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});a.equal(request.method,'GET');requests.push({method:request.method,path:url.pathname});let data;
     if(url.pathname==='/api/auth/session'){if(variant==='session503')return reply(503,{});data={workspace:'AcademyAdmin',academyId:academy,roles:['AcademyAdmin'],displayName:'Synthetic admin',isPlatformOwner:false};}
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Finance"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname.startsWith(base+'/')){
      const key=url.pathname.endsWith('/attendance')?'attendance':url.pathname.endsWith('/payroll/payouts')?'payroll':url.pathname.slice((base+'/').length);
      a.ok(['dashboard','students','invoices','payroll','sessions','attendance'].includes(key),'Unexpected API '+key);
      if(variant==='invoices403'&&key==='invoices'||variant==='students403'&&key==='students')return reply(403,{});
      if(variant==='payroll503'&&key==='payroll'||variant==='sessions503'&&key==='sessions'||variant==='attendance503'&&key==='attendance'||variant==='both503'&&['invoices','payroll'].includes(key)||variant==='reload'&&key==='invoices'&&invoiceFailures++===0)return reply(503,{});
      if(variant==='transportinvoice'&&key==='invoices')return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionReset'},sid);
      if(variant==='malformedpayroll'&&key==='payroll')return reply(200,{});
      data=dataset(variant)[key];
      if(variant==='pendinginvoice'&&key==='invoices'){release=()=>reply(200,data);return;}
     }
     else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/'+route},sid);
    if(variant==='session503'){
     await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be loaded')`);
     a.equal(await evaluate(`Boolean(document.querySelector('[aria-label="Academy operating indicators"]'))`),false);
     results.push({width,route,variant,pass:true,primaryGated:true});continue;
    }
    await wait(`${card('Active students')}?.querySelector('strong')?.textContent==='1'`);
    a.match(await metric('Outstanding fees'),/USD.*80.40/);a.equal(await metric('Attendance rate'),'50%');
    if(variant==='pendinginvoice'){
     const end=Date.now()+7000;while(!release&&Date.now()<end)await sleep(25);a.equal(typeof release,'function');
     await evaluate(card('Outstanding fees')+'.click()');await wait(`document.querySelector('[role="dialog"]')?.textContent.includes('Loading details')`);
     a.doesNotMatch(await evaluate(`document.querySelector('[role="dialog"]').textContent`),/No student fees|could not be loaded/);
     await release();await wait(`document.querySelector('[role="dialog"]')?.textContent.includes('USD-DUE')`);
     await evaluate(`document.querySelector('[aria-label="Close details"]').click()`);await wait(`!document.querySelector('[role="dialog"]')`);
    }else if(variant!=='valid'&&variant!=='empty')await wait(`document.body.textContent.includes('Some dashboard details could not be loaded')`);
    else await wait(`!Array.from(document.querySelectorAll('button')).some(n=>n.textContent==='Reload details')`);
    const open=async label=>{await evaluate(card(label)+'.click()');await wait(`Boolean(document.querySelector('[role="dialog"]'))`);return evaluate(`document.querySelector('[role="dialog"]').textContent`);};
    const close=async()=>{await evaluate(`document.querySelector('[aria-label="Close details"]').click()`);await wait(`!document.querySelector('[role="dialog"]')`);};
    const finance=await open('Outstanding fees');
    if(['invoices403','both503','transportinvoice','reload'].includes(variant)){a.match(finance,/Student fee details could not be loaded/);a.doesNotMatch(finance,/No student fees are outstanding/);}
    else if(variant==='empty')a.match(finance,/No student fees are outstanding/);else a.match(finance,/USD-DUE.*USD.*80.40 due/);
    if(['payroll503','both503','malformedpayroll'].includes(variant)){a.match(finance,/Payroll details could not be loaded/);a.doesNotMatch(finance,/No payroll records are listed/);}
    else if(variant==='empty')a.match(finance,/No payroll records are listed/);else a.match(finance,/EUR.*999.55/);
    if(variant==='students403')a.match(finance,/Student names could not be loaded/);
    if(variant==='invoices403'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,'invoice-unavailable-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    await close();
    const students=await open('Active students');if(variant==='students403'){a.match(students,/Student details could not be loaded/);a.doesNotMatch(students,/No active students/);}else if(variant==='empty')a.match(students,/No active students/);else a.match(students,/Synthetic Student/);await close();
    const attendance=await open('Attendance rate');if(['students403','sessions503','attendance503'].includes(variant)){a.match(attendance,/Attendance details could not be loaded/);a.doesNotMatch(attendance,/No attendance recorded|100%/);}else if(variant==='empty')a.match(attendance,/No active students/);else a.match(attendance,/50%/);await close();
    const classes=await open('Classes today');a.match(classes,/Primary schedule/);await close();
    if(variant==='reload'){
     await evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Reload details').click()`);
     await wait(`Boolean(${card('Active students')})&&!Array.from(document.querySelectorAll('button')).some(n=>n.textContent==='Reload details')`);
     a.match(await open('Outstanding fees'),/USD-DUE.*USD.*80.40 due/);await close();a.equal(invoiceFailures,2);
    }
    a.equal(await evaluate(`localStorage.getItem('academydesk.accessToken.AcademyAdmin')`),'synthetic-access');
    results.push({width,route,variant,pass:true,feeSummary:await metric('Outstanding fees'),dialogsVerified:4+(variant==='reload'||variant==='pendinginvoice'?1:0)});
   }catch(e){results.push({width,route,variant,pass:false,error:e.message});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,26);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c21-dashboard-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium Dashboard independent optional failures, loading and read-only recovery with synthetic APIs; no live Identity/SQL/device/FX qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('26 native dashboard detail availability scenarios PASS; zero API writes, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
