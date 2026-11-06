// Actual native prompt / DOM / FormData with intercepted synthetic APIs. No live SQL/model/device acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/manual-controls-browser');
const executable = process.env.QA_BROWSER_EXECUTABLE;
if (!executable || !fs.existsSync(path.join(out, 'finance-governance.html'))) throw Error('Select Chromium and build the local static app first.');
const apiOrigin=process.env.QA_MANUAL_BROWSER_API_ORIGIN||'http://127.0.0.1:49142';const apiAddress=new URL(apiOrigin);
a.equal(apiAddress.protocol,'http:');a.equal(apiAddress.hostname,'127.0.0.1');a.ok(Number(apiAddress.port)>=1024);a.equal(apiAddress.origin,apiOrigin);
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
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const origin='http://127.0.0.1:'+server.address().port,profile=fs.mkdtempSync(path.join(os.tmpdir(),'c6-chromium-'));
 const child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{windowsHide:true,stdio:['ignore','ignore','pipe']});
 const academy='11111111-1111-4111-8111-111111111111',adjustment='22222222-2222-4222-8222-222222222222',batch='33333333-3333-4333-8333-333333333333';
 const requests=[],results=[],runtimeErrors=[],dialogs=[];let socket,cdp,log='',passed=false,failure=null;
 try{
  const endpoint=await new Promise((resolve,reject)=>{const timer=setTimeout(()=>reject(Error('Chromium startup timeout')),15000);child.once('error',reject);child.stderr.on('data',data=>{log+=data;const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(endpoint);await new Promise((resolve,reject)=>{socket.addEventListener('open',resolve,{once:true});socket.addEventListener('error',reject,{once:true});});cdp=new CDP(socket);
  async function page(config){
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'});const {sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});
   await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);
   await cdp.send('Emulation.setDeviceMetricsOverride',{width:config.width||1280,height:900,deviceScaleFactor:1,mobile:false},sid);
   await cdp.send('Emulation.setTimezoneOverride',{timezoneId:'Asia/Kolkata'},sid);
   const state={writes:[],status:'PendingApproval',dialogs:[],fixtureCommits:0};
   cdp.on(sid,'Runtime.exceptionThrown',e=>runtimeErrors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
   cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')runtimeErrors.push(e.args.map(a=>a.value??a.description).join(' '));});
   cdp.on(sid,'Page.javascriptDialogOpening',async event=>{
    const row={type:event.type,message:event.message,accepted:config.answer!==null};dialogs.push(row);state.dialogs.push(row);
    try{if(event.type!=='prompt'||!config.finance)throw Error('Unexpected native dialog');await cdp.send('Page.handleJavaScriptDialog',{accept:config.answer!==null,...(config.answer===null?{}:{promptText:config.answer})},sid);}catch(e){runtimeErrors.push(e.message);await cdp.send('Page.handleJavaScriptDialog',{accept:false},sid).catch(()=>{});}
   });
   cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{
    try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);
     if(url.origin!==apiOrigin)throw Error('Unexpected external request');
     if(request.method==='OPTIONS')return await reply(204,{});
     requests.push({method:request.method,path:url.pathname});
     if(request.method!=='GET'){
      const value=JSON.parse(request.postData);state.writes.push({path:url.pathname,method:request.method,body:value});
      if(config.finance){
       a.equal(url.pathname,`/api/academies/${academy}/finance-adjustments/${adjustment}/approval`);a.equal(request.method,'PATCH');
       if(config.denied)return await reply(403,{});
       state.status=value.approve?'Approved':'Rejected';state.fixtureCommits++;return await reply(200,{});
      }
      a.ok(['/api/teacher/assignments','/api/teacher/assessments','/api/teacher/lesson-plans'].includes(url.pathname));a.equal(request.method,'POST');
      if(config.denied)return await reply(403,{});
      if(url.pathname!=='/api/teacher/lesson-plans'&&typeof value.isPublished!=='boolean')return await reply(400,{});
      if(url.pathname==='/api/teacher/assessments'&&(value.scheduledAtUtc===''||typeof value.maxScore!=='number'))return await reply(400,{});
      state.fixtureCommits++;
      if(config.lost)return await cdp.send('Fetch.failRequest',{requestId,errorReason:'ConnectionClosed'},sid);
      return await reply(200,{id:'44444444-4444-4444-8444-444444444444'});
     }
     if(url.pathname==='/api/academies')return await reply(200,[{id:academy,name:'Synthetic academy'}]);
     if(url.pathname==='/api/auth/session')return await reply(200,{workspace:config.finance?'AcademyAdmin':'Teacher',roles:[config.finance?'AcademyAdmin':'Teacher'],isPlatformOwner:false,displayName:'Synthetic operator',academyId:academy});
     if(['/api/portal/announcements','/api/portal/notifications','/api/teacher/payments','/api/teacher/practice-logs','/api/teacher/progress'].includes(url.pathname))return await reply(200,[]);
     if(url.pathname==='/api/teacher/me')return await reply(200,{userId:'55555555-5555-4555-8555-555555555555',firstName:'Synthetic',lastName:'Teacher',batches:[{id:batch,name:'Synthetic batch',capacity:8,deliveryMode:'InPerson'}],sessions:[]});
     if(url.pathname==='/api/teacher/calendar')return await reply(200,{sessions:[],holidays:[]});
     if(url.pathname===`/api/academies/${academy}/finance-adjustments`)return await reply(200,[{id:adjustment,type:'Discount',amount:10,reason:'Synthetic discount',status:state.status}]);
     if([`/api/academies/${academy}/finance-governance/collections`,`/api/academies/${academy}/finance-governance/collection-tasks`].includes(url.pathname))return await reply(200,[]);
     if(url.pathname===`/api/academies/${academy}/finance-governance/settings`)return await reply(200,{taxRegistrationNumber:'',taxLabel:'Tax',taxRatePercent:0,defaultPaymentTermsDays:14,taxInclusivePricing:false,invoiceTemplateKey:'Classic',payslipTemplateKey:'Standard'});
     throw Error('Unexpected API request '+url.pathname);
     async function reply(status,body){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, POST, PATCH, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(body)).toString('base64')},sid);}
    }catch(e){runtimeErrors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}
   });
   await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
   const workspace=config.finance?'AcademyAdmin':'Teacher';
   await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme','light');localStorage.setItem(${JSON.stringify('academydesk.accessToken.'+workspace)},'synthetic-access');localStorage.setItem(${JSON.stringify('academydesk.refreshToken.'+workspace)},'synthetic-refresh');localStorage.setItem(${JSON.stringify('academydesk.sessionEpoch.'+workspace)},'synthetic-epoch');}`},sid);
   const evaluate=async expression=>{const result=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true},sid);if(result.exceptionDetails)throw Error(result.exceptionDetails.exception?.description||result.exceptionDetails.text);return result.result.value;};
   const wait=async expression=>{for(let i=0;i<100;i++){if(await evaluate(expression))return;await sleep(50);}throw Error('DOM wait failed: '+expression);};
   await cdp.send('Page.navigate',{url:origin+(config.finance?'/finance-governance':'/teacher')},sid);
   return {state,evaluate,wait,sid,close:()=>cdp.send('Target.closeTarget',{targetId})};
  }
  for(const config of [{label:'Approve',answer:null},{label:'Reject',answer:null},{label:'Approve',answer:''},{label:'Approve',answer:'Synthetic approval'},{label:'Reject',answer:''},{label:'Reject',answer:'   '},{label:'Reject',answer:'Synthetic rejection'},{label:'Approve',answer:'Synthetic denied',denied:true}]){
   const name='finance '+config.label+' '+JSON.stringify(config.answer)+(config.denied?' denied':'');const p=await page({...config,finance:true});
   try{
    await p.wait(`Array.from(document.querySelectorAll('button')).some(b=>b.textContent==='Approve'&&!b.disabled)`);
    await p.evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent===${JSON.stringify(config.label)}).click()`);
    await p.wait(`!Array.from(document.querySelectorAll('button')).some(b=>b.textContent==='Approve'&&b.disabled)`);await sleep(100);
    a.equal(p.state.dialogs.length,1);a.equal(p.state.dialogs[0].type,'prompt');
    const noWrite=config.answer===null||(config.label==='Reject'&&!config.answer.trim());
    a.equal(p.state.writes.length,noWrite?0:1);
    if(noWrite){a.equal(p.state.status,'PendingApproval');a.equal(p.state.fixtureCommits,0);if(config.answer===null)a.equal(await p.evaluate(`document.body.innerText.includes('A rejection reason is required.')`),false);}
    else{a.deepEqual(p.state.writes[0].body,{approve:config.label==='Approve',notes:config.answer});a.equal(p.state.fixtureCommits,config.denied?0:1);if(config.denied)a.equal(p.state.status,'PendingApproval');}
    results.push({case:name,pass:true,requests:p.state.writes,fixtureCommits:p.state.fixtureCommits});
   }catch(e){results.push({case:name,pass:false,error:e.message,requests:p.state.writes});}finally{await p.close();}
  }
  for(const config of [{kind:'assignments',published:'true'},{kind:'assignments',published:'false'},{kind:'assessments',published:'true',date:'2026-11-07T17:30'},{kind:'assessments',published:'false',date:''},{kind:'lesson-plans'},{kind:'assignments',published:'true',denied:true},{kind:'assessments',published:'false',date:'',lost:true}]){
   const name='teacher '+config.kind+(config.denied?' denied':config.lost?' unconfirmed':' '+(config.published??''));const p=await page({...config,finance:false,width:390});
   try{
    await p.wait(`document.body?.innerText.includes('Welcome, Synthetic')`);
    await p.evaluate(`Array.from(document.querySelectorAll('nav[aria-label="Teacher workspace"] button')).find(b=>b.textContent.includes('Homework')).click()`);
    const formIndex={'assignments':0,'lesson-plans':1,'assessments':2}[config.kind];const form=`document.querySelectorAll('.teacher-homework-panels form')[${formIndex}]`;
    await p.wait(`Boolean(${form}?.querySelector('[aria-label="Assigned batch"]'))`);
    await p.evaluate(`${form}.querySelector('[aria-label="Assigned batch"]').click()`);
    await p.wait(`${form}.querySelectorAll('[role="option"]').length===2`);
    await p.evaluate(`Array.from(${form}.querySelectorAll('[role="option"]')).find(b=>b.textContent==='Synthetic batch').click()`);
    await p.wait(`${form}.querySelector('[name="batchId"]').value===${JSON.stringify(batch)}`);
    await p.evaluate(`{const f=${form};f.querySelector('[name="title"]').value='Synthetic title';${config.published===undefined?'':"f.querySelector('[name=\"isPublished\"]').value="+JSON.stringify(config.published)+';'}${config.kind==='assessments'?"f.querySelector('[name=\"maxScore\"]').value='12.5';f.querySelector('[name=\"scheduledAtUtc\"]').value="+JSON.stringify(config.date)+';':''}f.requestSubmit();}`);
    for(let i=0;i<100&&p.state.writes.length===0;i++)await sleep(25);a.equal(p.state.writes.length,1);
    const value=p.state.writes[0].body;a.equal(value.batchId,batch);a.equal(value.title,'Synthetic title');
    if(config.published!==undefined){a.equal(typeof value.isPublished,'boolean');a.equal(value.isPublished,config.published==='true');}else a.equal(Object.hasOwn(value,'isPublished'),false);
    if(config.kind==='assessments'){a.equal(value.maxScore,12.5);a.equal(value.scheduledAtUtc,config.date?'2026-11-07T12:00:00.000Z':null);}
    if(config.denied||config.lost){await p.wait(`document.body.innerText.includes(${JSON.stringify(config.lost?'could not be confirmed':'could not be saved')})`);a.equal(await p.evaluate(`${form}.querySelector('[name="title"]').value`),'Synthetic title');}
    else{await p.wait(`document.body.innerText.includes('Saved for your assigned batch.')`);a.equal(await p.evaluate(`${form}.querySelector('[name="title"]').value`),'');}
    await sleep(100);a.equal(p.state.writes.length,1,'No automatic retry');
    results.push({case:name,pass:true,requests:p.state.writes,fixtureCommits:p.state.fixtureCommits});
   }catch(e){results.push({case:name,pass:false,error:e.message,requests:p.state.writes});}finally{await p.close();}
  }
  a.equal(requests.filter(r=>/penta|model|inference/.test(r.path)).length,0);a.deepEqual(runtimeErrors,[]);a.equal(results.filter(r=>!r.pass).length,0,'Every native browser case must pass');passed=true;
  console.log(results.length+' actual Chromium Manual-control cases PASS; all APIs intercepted, no live writes/models.');
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(resolve=>child.once('exit',resolve)),sleep(2000)]);
  server.closeAllConnections();await new Promise(resolve=>server.close(resolve));a.equal(path.dirname(path.resolve(profile)),path.resolve(os.tmpdir()));a.ok(path.basename(profile).startsWith('c6-chromium-'));fs.rmSync(profile,{recursive:true,force:true});
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,dialogs,requests,runtimeErrors,scope:'Real browser/native prompts/FormData, synthetic interrupted transport; no Identity/SQL/provider/model/device acceptance.'},null,2));
 }
}
main().catch(e=>{console.error(e);process.exitCode=1;});
