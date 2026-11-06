// Current compensation UI in real Chromium. Synthetic APIs only; not live payroll/SQL/device acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/compensation-browser');
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
 const runtimeErrors=[],results=[],requests=[];let child,socket,cdp,profile,passed=false,failure=null,apiOrigin,serverStarted=false;
 const fields=['monthlySalary','standardHourlyRate','beginnerHourlyRate','intermediateHourlyRate','advancedHourlyRate'];
 const labels={monthlySalary:'Monthly salary',standardHourlyRate:'Standard hourly rate',beginnerHourlyRate:'Beginner hourly rate (optional)',intermediateHourlyRate:'Intermediate hourly rate (optional)',advancedHourlyRate:'Advanced hourly rate (optional)'};
 const academy='11111111-1111-4111-8111-111111111111',teacher='22222222-2222-4222-8222-222222222222',other='33333333-3333-4333-8333-333333333333';
 const endpoint=(id)=>`/api/academies/${academy}/teachers/${id}/compensation`;
 const sandboxDisabled=false;
 try{
  const selected=process.env.QA_COMPENSATION_API_ORIGIN||'http://127.0.0.1:49142',url=new URL(selected);
  if(url.protocol!=='http:'||url.hostname!=='127.0.0.1'||!url.port||Number(url.port)<1024||selected!==url.origin||url.username||url.password)throw Error('Only an explicit unprivileged HTTP127.0.0.1origin is accepted.');
  apiOrigin=url.origin;
  if(!executable||!fs.existsSync(path.join(out,'teacher-payments.html')))throw Error('Select installed Chromium and build the current static export first.');
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));serverStarted=true;
  const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c7-compensation-'));
  child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe'],windowsHide:true});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Chromium startup timeout')),15000);child.once('error',e=>{clearTimeout(timer);reject(e);});child.once('exit',()=>{clearTimeout(timer);reject(Error('Chromium exited before debugger startup; inspect local startup log.'));});child.stderr.on('data',data=>{log+=data;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((resolve,reject)=>{socket.addEventListener('open',resolve,{once:true});socket.addEventListener('error',reject,{once:true});});cdp=new CDP(socket);
  const canonical=(value)=>Object.fromEntries([['model',value.model],...fields.map(f=>[f,value[f]??null]),['effectiveFrom',value.effectiveFrom??null]]);
  async function page(config){
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'});const {sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});
   await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width:config.width||1280,height:900,deviceScaleFactor:1,mobile:false},sid);
   const state={stored:{[teacher]:canonical(config),[other]:canonical({model:'Monthly',monthlySalary:77})},writes:[],otherGets:0,failOther:!!config.failOther,holdOther:null,holdSave:null};
   cdp.on(sid,'Runtime.exceptionThrown',e=>runtimeErrors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));
   cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')runtimeErrors.push(e.args.map(a=>a.value??a.description??'').join(' '));});
   cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{
    try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);
     if(url.origin!==apiOrigin)throw Error('Unexpected external request');
     if(request.method==='OPTIONS')return await reply(204,{});
     requests.push({method:request.method,path:url.pathname});
     const selectedId=url.pathname===endpoint(teacher)?teacher:url.pathname===endpoint(other)?other:null;
     if(request.method==='PUT'){
      a.ok(selectedId);const body=JSON.parse(request.postData);state.writes.push({path:url.pathname,body});
      a.ok(['Monthly','Hourly'].includes(body.model));a.ok(fields.every(f=>body[f]===null||(typeof body[f]==='number'&&Number.isFinite(body[f])&&body[f]>=0)));
      a.notEqual(body[body.model==='Monthly'?'monthlySalary':'standardHourlyRate'],null);
      state.stored[selectedId]=structuredClone(body);if(state.holdSave)await state.holdSave;
      return await reply(200,state.stored[selectedId]);
     }
     a.equal(request.method,'GET','No other mutation is permitted');
     if(selectedId){if(selectedId===other){state.otherGets++;if(state.holdOther)await state.holdOther;if(state.failOther)return await reply(503,{message:'Synthetic unavailable teacher'});}return await reply(200,state.stored[selectedId]);}
     if(url.pathname==='/api/academies')return await reply(200,[{id:academy,name:'Synthetic academy',enabledModulesJson:'["Core","Teachers","Operations"]'}]);
     if(url.pathname==='/api/auth/session')return await reply(200,{workspace:'AcademyAdmin',academyId:academy,displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false});
     if(url.pathname==='/api/portal/announcements')return await reply(200,[]);
     if(url.pathname===`/api/academies/${academy}/teachers`)return await reply(200,[{id:teacher,firstName:'Zero',lastName:'Teacher',isActive:true},{id:other,firstName:'Other',lastName:'Teacher',isActive:true}]);
     throw Error('Unexpected API path '+url.pathname);
     async function reply(status,body){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, PUT, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(body)).toString('base64')},sid);}
    }catch(e){runtimeErrors.push(e.message);await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}
   });
   await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
   await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(config.theme||'light')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
   const evaluate=async expression=>{const r=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true},sid);if(r.exceptionDetails)throw Error(r.exceptionDetails.exception?.description||r.exceptionDetails.text);return r.result.value;};
   const wait=async expression=>{for(let i=0;i<120;i++){if(await evaluate(expression))return;await sleep(50);}throw Error('DOM wait failed: '+expression);};
   const input=(field)=>`Array.from(document.querySelectorAll('label')).find(e=>e.textContent.trim()===${JSON.stringify(labels[field])})?.querySelector('input')`;
   const ready=async(value,id=teacher)=>{const field=value.model==='Monthly'?'monthlySalary':'standardHourlyRate';await wait(`document.querySelector('[name="payment-teacher"]')?.value===${JSON.stringify(id)}&&${input(field)}?.value===${JSON.stringify(String(value[field]))}&&Array.from(document.querySelectorAll('button')).some(b=>b.textContent==='Save payment details'&&!b.disabled)`);};
   const clickSave=()=>evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Save payment details').click()`);
   const select=async(id)=>{await evaluate(`document.querySelector('[name="payment-teacher"]').parentElement.querySelector('button').click()`);await wait(`Array.from(document.querySelectorAll('[role="option"]')).some(b=>b.textContent===${JSON.stringify(id===teacher?'Zero Teacher':'Other Teacher')})`);await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).find(b=>b.textContent===${JSON.stringify(id===teacher?'Zero Teacher':'Other Teacher')}).click()`);await wait(`document.querySelector('[name="payment-teacher"]')?.value===${JSON.stringify(id)}`);};
   const fill=async(field,value)=>{await evaluate(`${input(field)}.focus()`);await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'a',code:'KeyA',windowsVirtualKeyCode:65,modifiers:2},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'a',code:'KeyA',windowsVirtualKeyCode:65},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'Backspace',code:'Backspace',windowsVirtualKeyCode:8},sid);await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'Backspace',code:'Backspace',windowsVirtualKeyCode:8},sid);if(value)await cdp.send('Input.insertText',{text:value},sid);await wait(`${input(field)}?.value===${JSON.stringify(value)}`);};
   const reload=async()=>{await cdp.send('Page.reload',{ignoreCache:true},sid);};
   await cdp.send('Page.navigate',{url:origin+'/teacher-payments'},sid);
   return{sid,state,evaluate,wait,input,ready,fill,clickSave,select,reload,close:()=>cdp.send('Target.closeTarget',{targetId})};
  }
  const matrix=[{name:'monthly-zero',model:'Monthly',monthlySalary:0},{name:'hourly-zero',model:'Hourly',standardHourlyRate:0},{name:'hourly-bands-zero',model:'Hourly',standardHourlyRate:10,beginnerHourlyRate:0,intermediateHourlyRate:0,advancedHourlyRate:0},{name:'positive',model:'Hourly',standardHourlyRate:125.5,beginnerHourlyRate:10.25,intermediateHourlyRate:20.5,advancedHourlyRate:30.75,effectiveFrom:'2024-02-29'},{name:'typed-zero',model:'Monthly',monthlySalary:22,edit:'0'},{name:'optional-clear',model:'Hourly',standardHourlyRate:10,beginnerHourlyRate:0,clear:true},{name:'monthly-negative',model:'Monthly',monthlySalary:10,edit:'-1',invalid:true},{name:'hourly-negative',model:'Hourly',standardHourlyRate:10,edit:'-1',invalid:true},{name:'monthly-required',model:'Monthly',monthlySalary:10,edit:'',invalid:true},{name:'hourly-required',model:'Hourly',standardHourlyRate:10,edit:'',invalid:true}];
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const item of matrix){
   const p=await page({...item,width,theme});try{
    await p.ready(item);const primary=item.model==='Monthly'?'monthlySalary':'standardHourlyRate';const expected=canonical(item);if(item.edit!==undefined&&!item.invalid)expected[primary]=Number(item.edit);if(item.clear)expected.beginnerHourlyRate=null;
    if(item.edit!==undefined)await p.fill(primary,item.edit);if(item.clear)await p.fill('beginnerHourlyRate','');await p.clickSave();
    if(item.invalid){a.equal(await p.evaluate(`${p.input(primary)}.checkValidity()`),false);await sleep(80);a.equal(p.state.writes.length,0);a.deepEqual(p.state.stored[teacher],canonical(item));}
    else{await p.wait(`document.querySelector('.teacher-payments-message')?.textContent.includes('Teacher payment details saved.')`);a.deepEqual(p.state.stored[teacher],expected);await p.reload();await p.ready(expected);await p.clickSave();await p.wait(`document.querySelector('.teacher-payments-message')?.textContent.includes('Teacher payment details saved.')`);a.equal(p.state.writes.length,2);for(const w of p.state.writes)a.deepEqual(w.body,expected);for(const f of item.model==='Monthly'?['monthlySalary']:fields.slice(1))a.equal(await p.evaluate(`${p.input(f)}?.value`),expected[f]===null?'':String(expected[f]));}
    a.equal(await p.evaluate('document.documentElement.scrollWidth<=innerWidth+1'),true,'No horizontal viewport overflow');a.equal(await p.evaluate('document.documentElement.dataset.theme'),theme);
    if((width===320&&theme==='light'&&item.name==='monthly-zero')||(width===1440&&theme==='dark'&&item.name==='positive')){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},p.sid);fs.writeFileSync(path.join(evidence,`${width}-${theme}-${item.name}.png`),Buffer.from(shot.data,'base64'));}
    results.push({case:item.name,width,theme,pass:true,writes:p.state.writes});
   }catch(e){results.push({case:item.name,width,theme,pass:false,error:e.message,writes:p.state.writes});}finally{await p.close();}
  }
  const deferred=()=>{let release;return{promise:new Promise(r=>release=r),release:()=>release()};};
  for(const kind of ['pending-read','reversed-read','failed-read','late-save']){
   const p=await page({model:'Monthly',monthlySalary:0});const gate=deferred();try{
    await p.ready({model:'Monthly',monthlySalary:0});
    if(kind==='late-save'){p.state.holdSave=gate.promise;await p.clickSave();for(let i=0;i<120&&p.state.writes.length===0;i++)await sleep(25);a.equal(p.state.writes.length,1);await p.select(other);await p.ready({model:'Monthly',monthlySalary:77},other);gate.release();await sleep(150);a.equal(await p.evaluate(`${p.input('monthlySalary')}.value`),'77');a.equal(await p.evaluate(`Boolean(document.querySelector('.teacher-payments-message')?.textContent.includes('saved'))`),false);a.equal(p.state.writes[0].path,endpoint(teacher));a.equal(p.state.stored[other].monthlySalary,77);}
    else{
     if(kind==='failed-read')p.state.failOther=true;else p.state.holdOther=gate.promise;
     await p.select(other);for(let i=0;i<120&&p.state.otherGets===0;i++)await sleep(25);a.ok(p.state.otherGets>0);
     await p.wait(`Array.from(document.querySelectorAll('button')).some(b=>b.textContent==='Save payment details'&&b.disabled)`);a.notEqual(await p.evaluate(`${p.input('monthlySalary')}?.value`),'0','Old teacher draft must be withheld');await p.clickSave();a.equal(p.state.writes.length,0);
     if(kind==='reversed-read'){await p.select(teacher);await p.ready({model:'Monthly',monthlySalary:0});gate.release();await sleep(150);a.equal(await p.evaluate(`${p.input('monthlySalary')}.value`),'0');}
     else if(kind==='failed-read'){await p.wait(`Array.from(document.querySelectorAll('button')).some(b=>b.textContent==='Retry loading payment details')`);p.state.failOther=false;await p.evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Retry loading payment details').click()`);await p.ready({model:'Monthly',monthlySalary:77},other);}
     else{gate.release();await p.ready({model:'Monthly',monthlySalary:77},other);}
    }
    results.push({case:kind,pass:true,writes:p.state.writes});
   }catch(e){results.push({case:kind,pass:false,error:e.message,writes:p.state.writes});}finally{gate.release();await p.close();}
  }
  a.deepEqual(runtimeErrors,[]);a.equal(requests.filter(r=>/penta|model|inference/.test(r.path)).length,0);a.equal(results.length,44);a.equal(results.filter(r=>!r.pass).length,0,'Every browser case must pass');passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}
  if(serverStarted){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;
  if(profile){try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c7-compensation-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned profile cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;failure??='Owned profile cleanup incomplete';}}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,apiOrigin,sandboxDisabled,browserSpawnRequested:Boolean(child),ownedProfileRemoved:!cleanupError,cleanupError,results,runtimeErrors,requests,scope:'Real Chromium keyboard/DOM/HTML validation; synthetic API state, not real Identity/SQL/payroll/devices.'},null,2));
  if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('44 actual Chromium compensation cases PASS; default sandbox, owned cleanup, synthetic APIs only.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
