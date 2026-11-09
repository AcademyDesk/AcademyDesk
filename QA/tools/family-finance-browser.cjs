// Family currency, finance permission and invoice history in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/family-finance-browser');
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

async function main() {
 const results=[],errors=[],requests=[];let child,socket,cdp,profile,started=false,passed=false,failure=null;
 const selected=process.env.QA_CURRENCY_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,student='11111111-1111-4111-8111-111111111111',base='/api/portal/students/'+student;
 const invoice=(extra={})=>({id:'usd',invoiceNumber:'USD-DUE',balance:70.15,currency:'USD',dueDate:'2026-11-08',status:'Issued',...extra});
 const variants=['mixedStudent','mixedParent','usd','empty','cancelled','cancelledZero','missing','legacy','parentDenied','staleDenied','staleGrant','filters'];
 try {
  if(!executable||!fs.existsSync(path.join(out,'portal.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c23-family-'));
  const downloads=fs.mkdtempSync(path.join(evidence,'downloads-'));
  child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  await cdp.send('Browser.setDownloadBehavior',{behavior:'allow',downloadPath:downloads});
  for(const width of [320,1440])for(const variant of variants){
   const parent=['mixedParent','parentDenied','staleDenied','staleGrant'].includes(variant),denied=['parentDenied','staleDenied'].includes(variant);
   let rows=[invoice()];
   if(['mixedStudent','mixedParent','filters'].includes(variant))rows.push(invoice({id:'inr',invoiceNumber:'INR-DUE',currency:'INR',balance:200.25}),invoice({id:'cancel',invoiceNumber:'CANCELLED-HISTORY',status:'cancelled',balance:900}),invoice({id:'paid',invoiceNumber:'PAID-HISTORY',status:'Paid',balance:0,dueDate:'2026-10-01'}));
   if(variant==='empty'||denied)rows=[];
   if(variant==='cancelled')rows=[invoice({status:'CANCELLED',balance:900})];
   if(variant==='cancelledZero')rows=[invoice({status:'Cancelled',balance:0})];
   if(variant==='missing')delete rows[0].currency;
   if(variant==='legacy')rows[0].currency='bad unit';
   const d={name:'Synthetic Student',firstName:'Synthetic',lastName:'Student',canViewFinance:!denied,invoices:rows,schedule:[],assignments:[],attendanceSummary:{total:2,present:1,late:0},practiceSummary:{totalMinutes:15,logCount:1},practiceLogs:[],music:[],resources:[],assessmentResults:[],certificates:[],classHistory:[],cycleProgress:[]};
   const me={role:parent?'Parent':'Student',displayName:'Synthetic user',studentId:student,parentId:parent?'parent':undefined,children:parent?[{id:student,name:d.name,canViewAcademicProgress:true,canViewFinance:variant==='staleGrant'?false:variant==='staleDenied'?true:!denied,canViewDocuments:true,canManageLeave:false}]:undefined};
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false,downloadCount=0;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out: '+expression);};
   const metric=()=>evaluate(`Array.from(document.querySelectorAll('.learner-kpi')).find(n=>n.querySelector('span')?.textContent==='Fee balance')?.querySelector('b')?.textContent`);
   const rowLabels=()=>evaluate(`Array.from(document.querySelectorAll('.learner-download-row b')).map(n=>n.textContent)`);
   const setStatus=value=>evaluate(`(()=>{const n=document.querySelector('[aria-label="Payment status"]');n.value=${JSON.stringify(value)};n.dispatchEvent(new Event('change',{bubbles:true}));})()`);
   try {
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
    cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try {
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});
     requests.push({width,variant,method:request.method,path:url.pathname});a.equal(request.method,'GET','Finance browsing sends no mutation');a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');let data;
     if(url.pathname==='/api/portal/me')data=me;
     else if(url.pathname==='/api/auth/session')data={workspace:'Portal',roles:[parent?'Guardian':'Student'],displayName:'Synthetic user',isPlatformOwner:false};
     else if(url.pathname===base)data=d;
     else if([base+'/leave-requests','/api/portal/notifications','/api/portal/announcements'].includes(url.pathname))data=[];
     else if(url.pathname===base+'/invoices/usd/download'){
      a.equal(denied,false);downloadCount++;
      return reply(200,'<!doctype html><title>Synthetic invoice fixture</title><p>USD 70.15</p>','text/html');
     }else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data,contentType='application/json'){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:contentType},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, OPTIONS'}],body:status===204?'':Buffer.from(contentType==='application/json'?JSON.stringify(data):data).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);fs.writeFileSync(path.join(evidence,'interception-errors.json'),JSON.stringify(errors,null,2));await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.Portal','synthetic-access');localStorage.setItem('academydesk.refreshToken.Portal','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.Portal','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/portal'},sid);await wait(`Boolean(document.querySelector('.learner-kpis'))`);
    if(denied)a.equal(await metric(),'Finance access unavailable');
    else if(['empty','cancelled','cancelledZero'].includes(variant))a.equal(await metric(),'0.00');
    else if(variant==='missing')a.equal(await metric(),'Currency details unavailable');
    else if(variant==='legacy')a.match(await metric(),/BAD UNIT 70\.15/);
    else if(['mixedStudent','mixedParent','filters'].includes(variant))a.match(await metric(),/INR.*200\.25.*USD.*70\.15/);
    else a.match(await metric(),/USD.*70\.15/);
    if(variant==='mixedParent'){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,'mixed-parent-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    await evaluate(`Array.from(document.querySelectorAll('nav button')).find(n=>n.textContent.endsWith('More')).click()`);await wait(`document.body.textContent.includes('Fees and payments')`);
    if(denied){a.equal(await evaluate(`Boolean(document.querySelector('[aria-label="Payment status"]'))`),false);a.match(await evaluate('document.body.textContent'),/Finance details are not available/);a.equal(await evaluate(`document.querySelectorAll('.learner-download-row').length`),0);}
    else {
     a.equal((await rowLabels()).length,rows.length);
     if(['cancelled','cancelledZero'].includes(variant))a.match((await rowLabels())[0],/Cancelled/);
     if(variant==='filters'){
      const all=await rowLabels();a.match(all[2],/CANCELLED-HISTORY.*Cancelled/);a.doesNotMatch(all[2],/900/);
      await setStatus('Outstanding');await wait(`document.querySelectorAll('.learner-download-row').length===2`);a.deepEqual((await rowLabels()).map(x=>x.split(' · ')[0]),['USD-DUE','INR-DUE']);
      await setStatus('Paid');await wait(`document.querySelectorAll('.learner-download-row').length===1`);a.match((await rowLabels())[0],/^PAID-HISTORY/);
      await evaluate(`(()=>{const n=document.querySelector('[aria-label="Payment cycle"]');Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(n,'2026-11');n.dispatchEvent(new Event('input',{bubbles:true}));n.dispatchEvent(new Event('change',{bubbles:true}));})()`);await wait(`document.querySelectorAll('.learner-download-row').length===0`);
      await evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Clear').click()`);await wait(`document.querySelectorAll('.learner-download-row').length===4`);
     }
     if(['mixedStudent','mixedParent'].includes(variant)){
      const scenarioDownloads=path.join(downloads,width+'-'+variant);fs.mkdirSync(scenarioDownloads);
      await cdp.send('Browser.setDownloadBehavior',{behavior:'allow',downloadPath:scenarioDownloads});
      const before=fs.readdirSync(scenarioDownloads).filter(x=>!x.endsWith('.crdownload'));
      await evaluate(`document.querySelector('.learner-download-row button').click()`);
      const end=Date.now()+7000;let added=[];while(Date.now()<end){added=fs.readdirSync(scenarioDownloads).filter(x=>!x.endsWith('.crdownload')&&!before.includes(x));if(added.length)break;await sleep(30);}a.equal(added.length,1);a.match(fs.readFileSync(path.join(scenarioDownloads,added[0]),'utf8'),/Synthetic invoice fixture.*USD 70\.15/);a.equal(downloadCount,1);
     }
    }
    a.equal(await evaluate(`localStorage.getItem('academydesk.accessToken.Portal')`),'synthetic-access');results.push({width,theme:width===320?'light':'dark',variant,parent,pass:true,downloadCount});
   }catch(e){results.push({width,variant,pass:false,error:e.message,downloadCount});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,24);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c23-family-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium with synthetic family API data and HTML download fixture. API tests separately validate real server HTML. No SQL, real Identity, provider, device or production acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('24 native family finance scenarios PASS; synthetic APIs only, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
