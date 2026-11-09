// Invoice documents and guarded required reads in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn, execFileSync } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/invoice-document-browser');
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
 const variants=['Classic','Modern','Minimal','Formal','cancelled','missingBalance','legacy','missingCurrency','empty','readRetry','denied','mixed'];
 try{
  if(!executable||!fs.existsSync(path.join(out,'invoices.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c24-invoices-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const variant of variants){
   const bill={id:'i',studentId:'s',invoiceNumber:'C24-USD',totalAmount:100.55,adjustedAmount:.05,paidAmount:30.35,balance:70.15,currency:'USD',issuedDate:'2026-11-01',dueDate:'2026-11-08',status:'PartiallyPaid'};
   if(variant==='cancelled')bill.status='cancelled';if(variant==='missingBalance')delete bill.balance;if(variant==='legacy')bill.currency='bad unit';if(variant==='missingCurrency')delete bill.currency;
   const rows=variant==='empty'?[]:[bill];if(variant==='mixed')rows.push({...bill,id:'inr',invoiceNumber:'C24-INR',currency:'INR',totalAmount:200.25,balance:200.25,adjustedAmount:0,paidAmount:0});
   const theme=['Classic','Modern','Minimal','Formal'].includes(variant)?variant:'Classic';let settingsReads=0,releaseSettings,pdfPages=null;
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out: '+expression);};
   const click=label=>evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent===${JSON.stringify(label)}).click()`);
   const amount=label=>evaluate(`Array.from(document.querySelectorAll('.invoice-paper-total')).find(n=>n.querySelector('span')?.textContent===${JSON.stringify(label)})?.querySelector('b')?.textContent`);
   const blocked=async()=>{a.equal(await evaluate(`document.querySelector('fieldset').disabled`),true);a.equal(await evaluate(`Boolean(document.querySelector('.invoices-kpis,.invoices-directory,.invoice-paper'))`),false);await evaluate(`document.querySelector('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}))`);};
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});
     requests.push({width,variant,method:request.method,path:url.pathname});a.equal(request.method,'GET','Document/read checks must send no writes');a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');let data;
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['FinanceUser'],displayName:'Synthetic Finance',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic Academy',legalName:'Synthetic Legal Academy',enabledModulesJson:'["Core","Finance"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname===base+'/invoices/student-options')data=[{id:'s',firstName:'Synthetic',lastName:'Learner',email:'billing@example.invalid'}];
     else if(url.pathname===base+'/fee-plans')data=[];
     else if(url.pathname===base+'/invoices')data=rows;
     else if(url.pathname===base+'/invoices/document-settings'){
      settingsReads++;if(variant==='denied')return reply(403,{});if(variant==='readRetry'&&settingsReads===1){releaseSettings=()=>reply(503,{});return;}
      data={taxRegistrationNumber:'TEST-TAX',taxLabel:'GST',invoiceTemplateKey:theme,invoiceAuthorityName:'Synthetic Signatory',invoiceAuthorityTitle:'Finance',invoiceLogoUrl:null,invoiceSignatureUrl:null};
     }else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);fs.writeFileSync(path.join(evidence,'interception-errors.json'),JSON.stringify(errors,null,2));await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/invoices'},sid);
    if(variant==='denied'){await wait(`document.querySelector('[role="alert"]')?.textContent.includes('does not have access')`);await blocked();a.doesNotMatch(await evaluate('document.body.textContent'),/49142|5092|localhost/);results.push({width,variant,pass:true,settingsReads,pdfPages});continue;}
    if(variant==='readRetry'){const end=Date.now()+7000;while(!releaseSettings&&Date.now()<end)await sleep(30);a.equal(typeof releaseSettings,'function');await blocked();await releaseSettings();await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be loaded')`);await blocked();await click('Try again');}
    await wait(`Boolean(document.querySelector('.invoices-kpis'))`);a.equal(await evaluate(`document.querySelector('fieldset').disabled`),false);
    if(variant==='empty'){a.match(await evaluate(`document.querySelector('.invoices-kpis').textContent`),/0\.00/);a.doesNotMatch(await evaluate(`document.querySelector('.invoices-kpis').textContent`),/INR|₹/);a.equal(await evaluate(`document.querySelectorAll('.invoices-directory tbody tr').length`),0);results.push({width,variant,pass:true,settingsReads,pdfPages});continue;}
    if(variant==='mixed')a.match(await evaluate(`document.querySelector('.invoices-kpis').textContent`),/INR.*200\.25.*USD.*100\.55/);
    await click('Preview / Print');await wait(`Boolean(document.querySelector('.invoice-paper'))`);
    a.equal(await evaluate(`document.querySelector('.invoice-paper').classList.contains('invoice-theme-'+${JSON.stringify(theme.toLowerCase())})`),true);
    if(variant==='cancelled'){a.match(await amount('Amount due'),/0\.00/);a.match(await amount('Balance before cancellation'),/70\.15/);}
    else if(variant==='missingBalance')a.equal(await amount('Amount due'),'Balance unavailable');
    else if(variant==='missingCurrency')a.equal(await amount('Amount due'),'Currency details unavailable');
    else if(variant==='legacy')a.equal(await amount('Amount due'),'BAD UNIT 70.15');
    else a.match(await amount('Amount due'),/70\.15/);
    if(width===320){
     const geometry=await evaluate(`(()=>{const paper=document.querySelector('.invoice-paper'),table=paper.querySelector('table'),p=paper.getBoundingClientRect(),t=table.getBoundingClientRect();return{paperOverflow:paper.scrollWidth-paper.clientWidth,tableLeft:t.left,paperLeft:p.left,tableRight:t.right,paperRight:p.right};})()`);
     a.ok(geometry.paperOverflow<=1,'Mobile invoice must not clip its document content: '+JSON.stringify(geometry));a.ok(geometry.tableLeft>=geometry.paperLeft&&geometry.tableRight<=geometry.paperRight,'Mobile billed table must fit the invoice');
    }
    if(['Classic','Modern','Minimal','Formal','cancelled'].includes(variant)){
     await cdp.send('Emulation.setEmulatedMedia',{media:'print'},sid);
     a.equal(await evaluate(`getComputedStyle(document.querySelector('.invoice-paper')).visibility`),'visible');a.equal(await evaluate(`getComputedStyle(document.querySelector('.invoice-paper-actions')).display`),'none');
     const pdf=await cdp.send('Page.printToPDF',{printBackground:true,paperWidth:8.27,paperHeight:11.69,marginTop:.25,marginBottom:.25,marginLeft:.25,marginRight:.25},sid);
     const pdfPath=path.join(evidence,'invoice-'+width+'-'+variant+'.pdf');fs.writeFileSync(pdfPath,Buffer.from(pdf.data,'base64'));
     const content=execFileSync('pdftotext',['-layout',pdfPath,'-'],{encoding:'utf8'});fs.writeFileSync(pdfPath+'.txt',content);
     a.match(content,/Synthetic Legal Academy/);a.match(content,/C24-USD/);a.match(content,/Approved adjustments\s+.*0\.05/);a.match(content,/Collected\s+.*30\.35/);a.match(content,variant==='cancelled'?/Amount due\s+.*0\.00/:/Amount due\s+.*70\.15/);a.doesNotMatch(content,/Issue invoice|New invoice|Preview \/ Print|Print \/ Save PDF/);
     const info=execFileSync('pdfinfo',[pdfPath],{encoding:'utf8'});pdfPages=Number(info.match(/^Pages:\s+(\d+)/m)?.[1]);a.equal(pdfPages,1,'One invoice should print on one A4 page');
     await cdp.send('Emulation.setEmulatedMedia',{media:'screen'},sid);
     await evaluate(`document.querySelector('.invoice-paper').scrollIntoView({block:'start'})`);const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,'invoice-'+width+'-'+variant+'.png'),Buffer.from(shot.data,'base64'));
    }
    await click('Close preview');await wait(`!document.querySelector('.invoice-paper')`);a.equal(await evaluate(`localStorage.getItem('academydesk.accessToken.AcademyAdmin')`),'synthetic-access');results.push({width,variant,pass:true,settingsReads,pdfPages});
   }catch(e){results.push({width,variant,pass:false,error:e.message,settingsReads,pdfPages});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,24);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c24-invoices-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium invoice UI and real A4 PDF bytes, synthetic API responses only. No live Identity/SQL/provider/device/printer or production acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('24 native invoice scenarios and 10 single-page A4 PDFs PASS; synthetic APIs only, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
