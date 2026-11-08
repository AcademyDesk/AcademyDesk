// Reports currency totals, modal rows, recovery and real CSV downloads in Chromium; synthetic APIs only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/reports-finance-browser');
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
function fixture() { return {
  students: [{ id: 's', firstName: 'Synthetic', lastName: 'Student', isActive: true }],
  teachers: [{ id: 't', firstName: 'Synthetic', lastName: 'Teacher', isActive: true }],
  batches: [{ id: 'b', name: 'Synthetic batch', capacity: 10 }], enrollments: [{ studentId: 's', batchId: 'b', status: 'Active' }],
  sessions: [{ id: 'ss', batchId: 'b', startUtc: '2026-11-08T12:00:00Z', status: 'Scheduled' }], attendance: [{ studentId: 's', status: 'Present' }, { studentId: 's', status: 'Absent' }],
  invoices: [
    { id: 'usd', studentId: 's', invoiceNumber: 'USD-DUE', totalAmount: 100.55, adjustedAmount: .05, paidAmount: 20.1, balance: 80.4, currency: 'USD', status: 'Issued', dueDate: '2026-11-09' },
    { id: 'over', studentId: 's', invoiceNumber: 'INR-OVER', totalAmount: 100, paidAmount: 150, balance: 0, currency: 'INR', status: 'Paid', dueDate: '2026-11-09' },
    { id: 'inr', studentId: 's', invoiceNumber: 'INR-DUE', totalAmount: 80.15, paidAmount: 0, balance: 80.15, currency: 'INR', status: 'Issued', dueDate: '2026-11-09' },
    { id: 'cancel', studentId: 's', invoiceNumber: 'INR-CANCEL', totalAmount: 500, paidAmount: 0, balance: 500, currency: 'INR', status: 'cancelled', dueDate: '2026-11-09' }],
  payments: [{ invoiceId: 'usd', amount: 20.1, currency: 'USD', status: 'Reconciled' }, { invoiceId: 'over', amount: 150, currency: 'INR', status: 'Completed' }, { amount: 9999, currency: 'INR', status: 'Pending' }, { amount: 9999, currency: 'USD', status: 'Voided' }],
  expenses: [{ description: 'USD expense', amount: 5.05, currency: 'USD', category: 'Supplies', expenseDate: '2026-11-01' }, { description: 'INR expense', amount: 10.10, currency: 'INR', category: 'Rent', expenseDate: '2026-11-01' }],
  payroll: [{ workerName: 'Unpaid worker', periodLabel: 'October', netAmount: 999.55, currency: 'EUR', status: 'Approved' }, { workerName: 'Paid worker', netAmount: 500, currency: 'INR', status: 'Paid' }]
}; }
 function dataFor(variant){
  const data=fixture();data.students[0].firstName='=FORMULA';
  if(variant==='empty')return{};
  if(variant==='usd'){data.invoices=data.invoices.slice(0,1);data.payments=data.payments.slice(0,1);data.expenses=data.expenses.slice(0,1);}
  if(variant==='legacy'){delete data.invoices[0].currency;delete data.payments[0].currency;delete data.expenses[0].currency;}
  if(variant==='negative')data.expenses.push({description:'EUR expense',amount:9.15,currency:'EUR',category:'Other',expenseDate:'2026-11-01'});
  return data;
 }
 try{
  if(!executable||!fs.existsSync(path.join(out,'reports.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c20-reports-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const variant of ['mixed','usd','empty','legacy','negative','unavailable','retry']){const route='reports';let failures=0;
   const{targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+7000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out');};
   const card=label=>`Array.from(document.querySelectorAll('.workspace-reports-kpis > button,.workspace-reports-kpis > article')).find(n=>n.querySelector('span')?.textContent===${JSON.stringify(label)})`;
   const metric=label=>evaluate(card(label)+`?.querySelector('strong')?.textContent`);
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});a.equal(request.method,'GET');requests.push({method:request.method,path:url.pathname});let data;
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['AcademyAdmin'],displayName:'Synthetic admin',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Finance"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname.startsWith(base+'/')){
      const key=url.pathname.endsWith('/attendance')?'attendance':url.pathname.endsWith('/payroll/payouts')?'payroll':url.pathname.slice((base+'/').length);
      a.ok(['students','teachers','batches','enrollments','sessions','invoices','payments','expenses','payroll','attendance'].includes(key),'Unexpected data endpoint '+key);
      if(key==='payments'&&(variant==='unavailable'||variant==='retry'&&failures++===0))return reply(503,{});
      data=dataFor(variant)[key]||[];
     }
     else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/'+route},sid);
    if(variant==='unavailable'||variant==='retry'){
     await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be loaded')`);
     a.equal(await evaluate(`Boolean(document.querySelector('[aria-label="Operational metrics"]'))`),false);
     a.equal(await evaluate(`Array.from(document.querySelectorAll('button')).some(n=>n.textContent.includes('Download CSV'))`),false);
     a.doesNotMatch(await evaluate('document.body.textContent'),/5092|localhost/);
     if(variant==='unavailable'){results.push({width,route,variant,pass:true,recoveryGated:true});continue;}
     await evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Try again').click()`);
    }
    await wait(`Boolean(${card('Collected')}?.querySelector('strong'))`);
    const values={};for(const label of ['Collected','Outstanding','Expenses','Collected less recorded expenses'])values[label]=await metric(label);
    if(variant==='legacy')a.ok(Object.values(values).every(v=>v==='Currency details unavailable'));
    else if(variant==='empty')a.ok(Object.values(values).every(v=>v==='0.00'));
    else if(variant==='usd'){
     a.match(values.Collected,/USD.*20.10/);a.match(values.Outstanding,/USD.*80.40/);a.match(values.Expenses,/USD.*5.05/);a.match(values['Collected less recorded expenses'],/USD.*15.05/);
     a.ok(Object.values(values).every(v=>!v.includes('INR')));
    }else{
     a.match(values.Collected,/INR.*150.00.*USD.*20.10/);a.match(values.Outstanding,/INR.*80.15.*USD.*80.40/);a.match(values.Expenses,/INR.*10.10.*USD.*5.05/);a.match(values['Collected less recorded expenses'],/INR.*139.90.*USD.*15.05/);
     if(variant==='negative')a.match(values['Collected less recorded expenses'],/-EUR.*9.15/);
    }
    a.ok(Object.values(values).every(v=>!v.includes('999')&&!v.includes('NaN')));
    for(const label of ['Collected','Outstanding','Expenses']){
     await evaluate(card(label)+'.click()');await wait(`Boolean(document.querySelector('[role="dialog"]'))`);
     const detail=await evaluate(`document.querySelector('[role="dialog"]').textContent`);
     if(variant!=='empty'){
      if(label==='Collected'){
       if(variant==='legacy')a.match(detail,/Currency details unavailable/);else a.match(detail,/USD.*20.10 paid/);
       a.doesNotMatch(detail,/9999|9,999/);
      }else if(label==='Outstanding'){
       a.match(detail,/Payroll records not marked Paid/);a.match(detail,/EUR.*999.55/);a.doesNotMatch(detail,/INR-CANCEL|INR-OVER|Paid worker|90.45/);
       if(variant==='legacy')a.match(detail,/USD-DUE.*Currency details unavailable/);else a.match(detail,/USD-DUE.*USD.*80.40 due/);
      }else if(variant==='legacy')a.match(detail,/Currency details unavailable/);else a.match(detail,/USD.*5.05/);
     }
     if(variant==='mixed'&&label==='Outstanding'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,'modal-'+width+'.png'),Buffer.from(shot.data,'base64'));}
     await evaluate(`document.querySelector('[aria-label="Close details"]').click()`);await wait(`!document.querySelector('[role="dialog"]')`);
    }
    const selectors=[
     `Array.from(document.querySelectorAll('.workspace-reports-panel')).find(n=>n.querySelector('h2')?.textContent==='Finance export').querySelector('button')`,
     `Array.from(document.querySelectorAll('.workspace-reports-export-card')).find(n=>n.querySelector('h3')?.textContent==='Invoices').querySelector('button')`,
     `Array.from(document.querySelectorAll('.workspace-reports-export-card')).find(n=>n.querySelector('h3')?.textContent==='Expenses').querySelector('button')`];
    const downloads=[];
    for(let i=0;i<selectors.length;i++){
     const directory=path.join(profile,'downloads',width+'-'+variant+'-'+i);fs.mkdirSync(directory,{recursive:true});
     await cdp.send('Browser.setDownloadBehavior',{behavior:'allow',downloadPath:directory});
     await evaluate(selectors[i]+'.click()');
     const filename=i===2?'academydesk-expenses.csv':'academydesk-invoices.csv',file=path.join(directory,filename),end=Date.now()+7000;
     while(!fs.existsSync(file)&&Date.now()<end)await sleep(25);a.ok(fs.existsSync(file),'CSV download completed '+filename);
     const csv=fs.readFileSync(file,'utf8');a.match(csv,i===2?/^"Expense amount","Currency"/:/^"Invoice","Student","Amount","Currency","Due date"/);
     if(variant!=='empty'){
      a.match(csv,variant==='legacy'?/Currency details unavailable/:/"USD"/);
      if(i<2)a.match(csv,/"'=FORMULA Student"/);
     }
     fs.writeFileSync(path.join(evidence,width+'-'+variant+'-'+i+'.csv'),csv);downloads.push(csv);
    }
    a.equal(downloads[0],downloads[1]);a.equal(await metric('Attendance rate'),variant==='empty'?'0%':'50%');a.equal(await metric('Active students'),variant==='empty'?'0':'1');
    results.push({width,route,variant,pass:true,values,modalsVerified:3,csvDownloads:3,recovered:variant==='retry'});
   }catch(e){results.push({width,route,variant,pass:false,error:e.message});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});}
  }
  a.equal(results.length,14);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c20-reports-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium Reports, modal values, recovery and real local CSV downloads with synthetic APIs; no live Identity/SQL/device/FX qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('14 native Reports currency/recovery scenarios and 36 CSV downloads PASS; zero API writes, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
