// Owner billing currencies in real Chromium; synthetic APIs, not live authority or SQL.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/platform-currency-overview-browser');
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
 const group=(currency,changes={})=>({currency,totalBilled:1250.40,collectedBilling:200.10,outstandingBilling:1050.30,...changes});
 function summary(variant){
  const meta={openSupportCases:3,overdueInvoices:1,recentAudit:[]};
  if(variant==='legacy')return{...meta,totalBilled:1370.56,collectedBilling:220.11,outstandingBilling:1150.45};
  if(variant==='empty')return{...meta,byCurrency:[]};
  if(variant==='usd')return{...meta,byCurrency:[group('USD',{totalBilled:120.70,collectedBilling:20.10,outstandingBilling:100.60})]};
  return{...meta,totalBilled:null,collectedBilling:null,outstandingBilling:null,byCurrency:[group('INR'),group('USD',{totalBilled:120.16,collectedBilling:20.01,outstandingBilling:100.15})]};
 }
 try{
  if(!executable||!fs.existsSync(path.join(out,'platform.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c18-platform-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const variant of ['mixed','usd','empty','legacy','unavailable']){const route='platform';
   const{targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+7000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out');};
   const card=label=>`Array.from(document.querySelectorAll('.platform-kpis > button')).find(n=>n.querySelector('span')?.textContent===${JSON.stringify(label)})`;
   const metric=label=>evaluate(card(label)+`?.querySelector('strong')?.textContent`);
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return reply(204,{});a.equal(request.method,'GET');requests.push({method:request.method,path:url.pathname});let data;
     if(url.pathname==='/api/auth/session')data={workspace:'Platform',roles:['PlatformOwner'],displayName:'Synthetic owner',isPlatformOwner:true};
     else if(url.pathname==='/api/platform/academies')data=[{id:academy,name:'Synthetic academy',countryCode:'IN',timeZone:'Asia/Kolkata',isActive:true,branches:1,students:2}];
     else if(url.pathname==='/api/platform/overview'){if(variant==='unavailable')return reply(503,{});data=summary(variant);}
     else if(url.pathname==='/api/platform/health')data={api:'Operational',database:'Operational'};
     else if(url.pathname==='/api/portal/announcements')data=[];
     else throw Error('Unexpected API path '+url.pathname);
     return reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.Platform','synthetic-access');localStorage.setItem('academydesk.refreshToken.Platform','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.Platform','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+'/'+route},sid);await wait(`Boolean(${card('Platform billed')}?.querySelector('strong'))`);await wait(`${card('Active learners')}?.querySelector('strong')?.textContent==='2'&&!document.body.textContent.includes('Loading academy portfolio')`);
    const values={};for(const label of ['Platform billed','Collected','Outstanding'])values[label]=await metric(label);
    if(variant==='mixed'){for(const value of Object.values(values)){a.match(value,/INR/);a.match(value,/USD/);}a.match(values['Platform billed'],/1,250\.40.*120\.16/);a.match(values.Outstanding,/1,050\.30.*100\.15/);}
    else if(variant==='usd'){a.match(values.Outstanding,/USD.*100\.60/);a.doesNotMatch(values.Outstanding,/INR|₹/);}
    else if(variant==='empty')a.ok(Object.values(values).every(v=>v==='0.00'));
    else a.ok(Object.values(values).every(v=>v==='Currency details unavailable'));
    for(const label of ['Platform billed','Collected','Outstanding']){
     await evaluate(card(label)+'.click()');await wait(`Boolean(document.querySelector('[role="dialog"] #overview-tile-title'))`);
     a.equal(await evaluate(`document.querySelector('[role="dialog"] #overview-tile-title').textContent`),values[label]);
     a.equal(await evaluate(`document.querySelector('[role="dialog"] a').getAttribute('href')`),'/platform/control?tab=Billing');
     await evaluate(`document.querySelector('[aria-label="Close detail"]').click()`);await wait(`!document.querySelector('[role="dialog"]')`);
    }
    a.equal(await metric('Active learners'),'2');a.equal(await metric('Open support'),variant==='unavailable'?'0':'3');
    results.push({width,route,variant,pass:true,values});
   }catch(e){results.push({width,route,variant,pass:false,error:e.message});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});}
  }
  a.equal(results.length,10);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c18-platform-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium owner tiles and detail modals with synthetic APIs; no live Identity/SQL/device/FX qualification.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('10 native owner currency scenarios PASS; zero API writes, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
