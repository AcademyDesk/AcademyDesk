// Batched financial UI in real Chromium. Synthetic APIs only; not live Identity/SQL/device acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/finance-batch-browser');
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
 const academy='11111111-1111-4111-8111-111111111111',student='22222222-2222-4222-8222-222222222222',teacher='33333333-3333-4333-8333-333333333333',invoice='44444444-4444-4444-8444-444444444444',pay='55555555-5555-4555-8555-555555555555',pr='66666666-6666-4666-8666-666666666666';
 const base='/api/academies/'+academy,routes={payment:'payments',invoice:'invoices',profile:'payroll',payout:'payroll',reconcile:'finance-reconciliation'};
 const money=(n,c='INR')=>new Intl.NumberFormat('en-IN',{style:'currency',currency:c}).format(n);
 const defer=()=>{let release;return{promise:new Promise(r=>release=r),release:()=>release()};};
 try{
  const selected=process.env.QA_FINANCE_API_ORIGIN||'http://127.0.0.1:49142',url=new URL(selected);
  if(url.protocol!=='http:'||url.hostname!=='127.0.0.1'||!url.port||Number(url.port)<1024||selected!==url.origin||url.username||url.password)throw Error('Only a canonical unprivileged HTTP127.0.0.1origin is allowed.');apiOrigin=url.origin;
  if(!executable||!fs.existsSync(path.join(out,'payments.html')))throw Error('Use installed Chromium and the exact current export.');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c9-finance-'));
  child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe'],windowsHide:true});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',e=>{clearTimeout(timer);reject(e);});child.once('exit',()=>{clearTimeout(timer);reject(Error('Browser exited before debugger'));});child.stderr.on('data',data=>{log+=data;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  async function page(kind,config={}){
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});
   await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width:config.width||1280,height:1000,deviceScaleFactor:1,mobile:false},sid);
   const state={writes:[],gets:0,commits:0,hold:null,reject:false,transport:false,commitOnLoss:true,failRead:false,
    invoices:[{id:invoice,invoiceNumber:'QA-I',studentId:student,totalAmount:1000,balance:config.balance??400,adjustedAmount:config.adjusted??0,currency:'INR',issuedDate:'2026-11-01',dueDate:'2026-12-01',status:(config.balance===0?'Paid':'PartiallyPaid')},...(config.mixed?[{id:'usd',invoiceNumber:'QA-USD',studentId:student,totalAmount:100,balance:80,currency:'USD',issuedDate:'2026-11-01',dueDate:'2026-12-01',status:'PartiallyPaid'}]:[])],
    payments:[{id:'reconciled',invoiceId:invoice,amount:600,currency:'INR',status:'Reconciled',method:'UPI',paidAtUtc:'2026-11-01T10:00:00Z',reconciliationReference:'Existing evidence'},
     {id:pay,invoiceId:invoice,amount:25.5,currency:'INR',status:'Completed',method:'Cash',paidAtUtc:'2026-11-01T11:00:00Z'},
     {id:'voided',invoiceId:invoice,amount:75,currency:'INR',status:'Voided',method:'Cash',paidAtUtc:'2026-11-01T12:00:00Z'},...(config.mixed?[{id:'usd',invoiceId:'usd',amount:20,currency:'USD',status:'Completed',method:'Cash',paidAtUtc:'2026-11-01T11:00:00Z'}]:[])],
    profiles:[{id:pr,workerType:'Teacher',workerName:'Synthetic Teacher',paymentModel:'Monthly',monthlyAmount:1000,effectiveFrom:'2026-01-01',isActive:true}],
    payouts:[{id:'historic',payrollProfileId:pr,workerName:'Original saved name',workerType:'Teacher',payslipNumber:'PS-ORIGINAL',periodLabel:'October 2026',grossAmount:1000,deductions:100,netAmount:900,currency:'INR',status:'Paid',paymentMethod:'BankTransfer',paidAtUtc:'2026-11-01T10:00:00Z'},...(config.mixed?[{id:'usd',workerName:'USD saved name',workerType:'Teacher',periodLabel:'October 2026',netAmount:50,currency:'USD',paidAtUtc:'2026-11-01T10:00:00Z'}]:[])]};
   cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
   cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{
    try{
     const u=new URL(request.url);if(u.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);if(u.origin!==apiOrigin)throw Error('Unexpected external origin');if(request.method==='OPTIONS')return await reply(204,{});
     requests.push({method:request.method,path:u.pathname});
     if(request.method!=='GET'){
      const expected={payment:base+'/payments',invoice:base+'/invoices',profile:base+'/payroll/profiles',payout:base+'/payroll/payouts',reconcile:base+'/payments/'+pay+'/reconcile'}[kind];a.equal(u.pathname,expected);a.equal(request.method,kind==='reconcile'?'PATCH':'POST');
      const body=JSON.parse(request.postData);state.writes.push({path:u.pathname,method:request.method,body});if(state.hold)await state.hold;
      if(state.reject)return await reply(400,{message:'Synthetic rejected write'});
      if(kind==='payout'&&(!Number.isFinite(body.deductions)||body.deductions<0||body.deductions>=1000))return await reply(400,{message:'Positive net payout required'});
      if(!state.transport||state.commitOnLoss){
       state.commits++;
       if(kind==='payment'){const row=state.invoices.find(i=>i.id===body.invoiceId);a.ok(row);a.ok(body.amount>0&&body.amount<=row.balance);row.balance-=body.amount;row.status=row.balance===0?'Paid':'PartiallyPaid';state.payments.push({id:'saved-'+state.commits,...body,currency:row.currency,status:'Completed',paidAtUtc:'2026-11-07T00:00:00Z'});}
       if(kind==='invoice')state.invoices.push({id:'saved-'+state.commits,...body,invoiceNumber:'QA-NEW',totalAmount:body.amount,balance:body.amount,currency:'INR',dueDate:body.dueDate||'2026-12-01',status:'Issued'});
       if(kind==='profile')state.profiles.push({id:'saved-'+state.commits,...body});
       if(kind==='payout')state.payouts.push({id:'saved-'+state.commits,...body,workerType:'Teacher',workerName:'Synthetic Teacher',payslipNumber:'PS-NEW',grossAmount:1000,netAmount:1000-body.deductions,currency:'INR',status:'Paid',paidAtUtc:'2026-11-07T00:00:00Z'});
       if(kind==='reconcile'){a.ok(body.reference.trim());state.payments.find(p=>p.id===pay).status='Reconciled';state.payments.find(p=>p.id===pay).reconciliationReference=body.reference;}
      }
      if(state.transport)return await cdp.send('Fetch.failRequest',{requestId,errorReason:'Failed'},sid);return await reply(200,{id:'saved',...body});
     }
     state.gets++;if(state.failRead&&state.commits&&u.pathname.startsWith(base+'/'))return await reply(503,{});
     if(u.pathname==='/api/academies')return await reply(200,[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Finance","Teachers"]'}]);
     if(u.pathname==='/api/auth/session')return await reply(200,{workspace:'AcademyAdmin',academyId:academy,displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false});
     if(u.pathname==='/api/portal/announcements')return await reply(200,[]);
     if(u.pathname===base+'/invoices/student-options')return await reply(200,[{id:student,firstName:'Synthetic',lastName:'Learner'}]);
     if(u.pathname===base+'/invoices/document-settings')return await reply(200,{invoiceTemplateKey:'Classic',taxLabel:'GST'});
     if(u.pathname===base+'/fee-plans')return await reply(200,[]);
     if(u.pathname===base+'/invoices')return await reply(200,state.invoices);
     if(u.pathname===base+'/payments')return await reply(200,state.payments);
     if(u.pathname===base+'/teachers')return await reply(200,[{id:teacher,firstName:'Synthetic',lastName:'Teacher'}]);
     if(u.pathname===base+'/staff')return await reply(200,[]);
     if(u.pathname===base+'/payroll/profiles')return await reply(200,state.profiles);
     if(u.pathname===base+'/payroll/payouts')return await reply(200,state.payouts);
     throw Error('Unexpected API path '+u.pathname);
     async function reply(status,body){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, PATCH, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(body)).toString('base64')},sid);}
    }catch(e){if(state.closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}
   });
   await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(config.theme||'light')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
   const evaluate=async expr=>{const v=await cdp.send('Runtime.evaluate',{expression:expr,awaitPromise:true,returnByValue:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expr=>{for(let i=0;i<90;i++){if(await evaluate('Boolean('+expr+')'))return;await sleep(40);}throw Error('DOM wait failed: '+expr);};
   const form=`document.querySelectorAll('form')[${kind==='payout'?1:0}]`;
   const nativeSelect=async(expr,value)=>evaluate(`(()=>{const e=${expr};Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype,'value').set.call(e,${JSON.stringify(value)});e.dispatchEvent(new Event('change',{bubbles:true}));})()`);
   const fill=async(expr,value)=>{await evaluate(`(${expr}).focus()`);await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'a',code:'KeyA',windowsVirtualKeyCode:65,modifiers:2},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'a',code:'KeyA',windowsVirtualKeyCode:65},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'Backspace',code:'Backspace',windowsVirtualKeyCode:8},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'Backspace',code:'Backspace',windowsVirtualKeyCode:8},sid);if(value)await cdp.send('Input.insertText',{text:value},sid);await wait(`(${expr}).value===${JSON.stringify(value)}`);};
   const choose=async(name,label)=>{await evaluate(`document.querySelector('[name="${name}"]').parentElement.querySelector('button').click()`);await wait(`Array.from(document.querySelectorAll('[role="option"]')).some(b=>b.textContent===${JSON.stringify(label)})`);await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).find(b=>b.textContent===${JSON.stringify(label)}).click()`);};
   const notice=()=>evaluate(`document.querySelector(${JSON.stringify({payment:'.payments-standard > .enterprise-settings-empty',invoice:'.invoices-notice',profile:'.payroll-notice',payout:'.payroll-notice',reconcile:'.reconciliation-notice'}[kind])})?.textContent||''`);
   const ready=async()=>wait(kind==='payment'?`document.querySelector('.payments-directory')?.textContent.includes('QA-I')`:kind==='invoice'?`document.querySelector('.invoices-directory')?.textContent.includes('QA-I')`:kind==='reconcile'?`document.querySelector('.reconciliation-ledger')?.textContent.includes('QA-I')`:`document.querySelector('.payroll-register')?.textContent.includes('Original saved name')`);
   const setup=async()=>{
    await ready();if(kind==='payment'){if((config.balance??400)>0){await nativeSelect(`${form}.querySelector('select')`,invoice);await wait(`${form}.querySelector('input[type="number"]').value===${JSON.stringify(String(config.balance??400))}`);}}
    if(kind==='invoice')await fill(`${form}.querySelector('input[type="number"]')`,'100');
    if(kind==='profile'){await choose('workerId','Synthetic Teacher');await evaluate(`document.querySelector('[name="effectiveFrom"]').parentElement.querySelector('button').click()`);await wait(`document.querySelector('.standard-date-popover')`);await evaluate(`Array.from(document.querySelectorAll('.standard-date-popover button')).find(b=>b.textContent==='Today').click()`);await fill(`${form}.querySelector('[name="monthlyAmount"]')`,'1000');}
    if(kind==='payout'){await choose('payrollProfileId','Synthetic Teacher · Monthly '+money(1000));await fill(`${form}.querySelector('[name="periodLabel"]')`,'November 2026');await fill(`${form}.querySelector('[name="deductions"]')`,config.deductions??'1');}
    if(kind==='reconcile'){await evaluate(`document.querySelector('.reconciliation-link').click()`);await wait(`document.querySelector('.reconciliation-form')`);await fill(`${form}.querySelector('input')`,'Synthetic evidence');}
   };
   await cdp.send('Page.navigate',{url:origin+'/'+routes[kind]},sid);await setup();
   return{sid,state,form,fill,evaluate,wait,ready,notice,submit:()=>evaluate(`${form}.requestSubmit()`),close:()=>{state.closing=true;return cdp.send('Target.closeTarget',{targetId});}};
  }
  async function run(kind,name,config,check){let p;try{p=await page(kind,config);await check(p);results.push({kind,case:name,...config,pass:true,writes:p.state.writes});}catch(e){results.push({kind,case:name,...config,pass:false,error:e.message,writes:p?.state.writes||[]});}finally{if(p)await p.close();}}
  for(const kind of ['payment','invoice','profile','payout','reconcile'])for(const scenario of ['success','pending','rejected','uncertain','refresh-failure'])await run(kind,scenario,{},async p=>{
   const gate=defer();try{
    if(scenario==='pending')p.state.hold=gate.promise;if(scenario==='rejected')p.state.reject=true;if(scenario==='uncertain')p.state.transport=true;if(scenario==='refresh-failure')p.state.failRead=true;
    await p.submit();for(let i=0;i<100&&!p.state.writes.length;i++)await sleep(20);a.equal(p.state.writes.length,1);
    if(scenario==='pending'){await p.submit();a.equal(p.state.writes.length,1,'No duplicate event submission');a.equal(await p.evaluate(`${p.form}.querySelector('input')?.matches(':disabled')||${p.form}.querySelector('select')?.matches(':disabled')`),true);gate.release();}
    if(scenario==='uncertain'){await p.wait(`document.querySelector('main')?.textContent.includes('could not be confirmed')`);await p.submit();a.equal(p.state.writes.length,1);a.equal(p.state.commits,1);a.equal(await p.evaluate(`Boolean(Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Reload financial records'))`),true);const before=p.state.gets;p.state.transport=false;await p.evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Reload financial records').click()`);for(let i=0;i<100&&p.state.gets<=before;i++)await sleep(30);a.ok(p.state.gets>before,'Recovery reload must read current records');await p.wait(`Boolean(document.querySelector('main'))&&!Array.from(document.querySelectorAll('main > p')).some(e=>/Loading/i.test(e.textContent))`);a.equal(p.state.writes.length,1,'Recovery navigation must not replay the financial mutation');}
    else if(scenario==='refresh-failure'){await p.wait(`document.querySelector('main')?.textContent.includes('could not be refreshed')`);a.match(await p.notice(),/saved|recorded|issued|reconciled/i);a.equal(p.state.commits,1);a.equal(p.state.writes.length,1);}
    else if(scenario==='rejected'){await p.wait(`document.querySelector('main')?.textContent.includes('could not be saved')||document.querySelector('main')?.textContent.includes('could not be recorded')`);a.equal(p.state.commits,0);a.equal(p.state.writes.length,1);a.equal(await p.evaluate(`${p.form}.querySelector('input')?.value!==''`),true);}
    else{await p.wait(`document.querySelector('main')?.textContent.includes(${JSON.stringify({payment:'Payment recorded.',invoice:'Invoice issued.',profile:'Payout profile saved.',payout:'Payout recorded and payslip generated.',reconcile:'Payment reconciled.'}[kind])})`);a.match(await p.notice(),/saved|recorded|issued|reconciled/i);a.equal(p.state.commits,1);}
   }finally{gate.release();}
  });
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const input of [{name:'reconciled',balance:400},{name:'adjusted',balance:200,adjusted:200},{name:'settled',balance:0},{name:'cents',balance:0.01}])await run('payment',input.name,{...input,width,theme},async p=>{
   const total=await p.evaluate(`Array.from(document.querySelectorAll('.payments-kpis article')).find(e=>e.textContent.includes('Collected'))?.querySelector('strong').textContent`);a.equal(total,money(625.5));
   const amount=await p.evaluate(`${p.form}.querySelector('input[type="number"]').value`);a.equal(amount,input.balance===0?'':String(input.balance));
   a.equal(await p.evaluate(`Boolean(${p.form}.querySelector('option[value="${invoice}"]'))`),input.balance>0);
   if(input.balance>0)a.equal(await p.evaluate(`${p.form}.querySelector('input[type="number"]').max`),String(input.balance));
   a.equal(await p.evaluate('document.documentElement.scrollWidth<=innerWidth+1'),true);a.equal(await p.evaluate('document.documentElement.dataset.theme'),theme);a.equal(p.state.writes.length,0);
  });
  for(const kind of ['payment','invoice','payout','reconcile'])await run(kind,'mixed-currencies',{mixed:true},async p=>{const cls={payment:'.payments-kpis',invoice:'.invoices-kpis',payout:'.payroll-kpis',reconcile:'.reconciliation-kpis'}[kind],t=await p.evaluate(`document.querySelector('${cls}').textContent`);a.match(t,/USD/);a.ok(t.includes(money({payment:625.5,invoice:1000,payout:900,reconcile:25.5}[kind])));a.ok(!t.includes(money({payment:645.5,invoice:1100,payout:950,reconcile:45.5}[kind])));a.equal(p.state.writes.length,0);});
  for(const deductions of ['1000','1001','999.99'])await run('payout','net-'+deductions,{deductions},async p=>{await p.submit();if(deductions==='999.99'){await p.wait(`document.querySelector('.payroll-notice')?.textContent.includes('Payout recorded')`);a.equal(p.state.writes.length,1);a.ok(Math.abs(p.state.payouts.at(-1).netAmount-0.01)<1e-9);}else{await p.wait(`document.querySelector('.payroll-notice')?.textContent.includes('less than gross')`);a.equal(p.state.writes.length,0);}});
  await run('payment','uncommitted-loss',{},async p=>{p.state.transport=true;p.state.commitOnLoss=false;await p.submit();await p.wait(`document.querySelector('main')?.textContent.includes('could not be confirmed')`);await p.submit();a.equal(p.state.writes.length,1);a.equal(p.state.commits,0);});
  a.equal(results.length,49);a.equal(results.filter(r=>!r.pass).length,0,'Every batched native case must pass');a.deepEqual(errors,[]);a.equal(requests.filter(r=>/penta|model|inference/.test(r.path)).length,0);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}
  if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile){try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c9-finance-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned profile cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;failure??='Owned cleanup failed';}}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,apiOrigin,sandboxDisabled:false,browserSpawnRequested:Boolean(child),ownedProfileRemoved:!cleanupError,cleanupError,results,errors,requests,scope:'Actual Chromium DOM/keyboard/form validation; all APIs synthetic. Not real financial Identity/SQL/concurrency/device acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('49 native financial batch cases PASS; synthetic APIs only, sandbox enabled, owned profile removed.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
