// Truthful provider readiness and blocked meeting execution in Chromium; synthetic API persistence only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/provider-readiness-browser');
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
 const selected=process.env.QA_PROVIDER_API_ORIGIN||'http://127.0.0.1:49142',u=new URL(selected);
 if(u.protocol!=='http:'||u.hostname!=='127.0.0.1'||!u.port||Number(u.port)<1024||selected!==u.origin||u.username||u.password)throw Error('Canonical unprivileged loopback API required');
 const apiOrigin=u.origin,academy='11111111-1111-4111-8111-111111111111',base='/api/academies/'+academy;
 const variants=['settings-empty','settings-metadata','settings-recorded','settings-zoom','settings-teams','settings-disabled','settings-partial','settings-denied','settings-save','settings-save-recorded','meetings-google-false','meetings-google-true','meetings-zoom-true','meetings-teams-true','meetings-denied','meetings-retry'];
 try{
  if(!executable||!fs.existsSync(path.join(out,'meeting-links.html')))throw Error('Installed Chromium and exact-source export required');
  await new Promise(r=>server.listen(0,'127.0.0.1',r));started=true;const origin='http://127.0.0.1:'+server.address().port;
  profile=fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()),'c26-providers-'));child=spawn(executable,['--headless','--disable-background-networking','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  const debuggerUrl=await new Promise((resolve,reject)=>{let log='';const timer=setTimeout(()=>reject(Error('Browser startup timeout')),15000);child.once('error',reject);child.once('exit',()=>reject(Error('Browser exited')));child.stderr.on('data',b=>{log+=b;fs.writeFileSync(path.join(evidence,'browser-startup.log'),log);const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});});
  socket=new WebSocket(debuggerUrl);await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});cdp=new CDP(socket);
  for(const width of [320,1440])for(const variant of variants){
   const settingsPage=variant.startsWith('settings-'),route=settingsPage?'/communication-settings':'/meeting-links',provider=variant.includes('zoom')?'Zoom':variant.includes('teams')?'Microsoft365':'GoogleWorkspace';
   const recorded=variant==='settings-recorded'||variant.includes('zoom')||variant.includes('teams')||variant.endsWith('-true')||variant==='settings-save-recorded';
   let rows=['Email','WhatsApp','Meeting'].map(channel=>({channel,provider:channel==='Meeting'?provider:channel==='Email'?'SMTP':'MetaCloudApi',status:variant==='settings-disabled'&&channel==='Meeting'?'Disabled':'Configured',senderName:'Synthetic Organizer',senderAddress:'organizer@example.invalid',replyToAddress:'reply@example.invalid',phoneNumber:'+000000000',externalAccountReference:'synthetic-reference',messagesEnabled:true,hasSecureConnection:channel==='Meeting'?(recorded||variant==='settings-disabled'):variant==='settings-save-recorded'||variant==='settings-partial'&&channel==='Email'}));if(variant==='settings-empty')rows=[];
   let reads=0,puts=0,releaseRead;
   const {targetId}=await cdp.send('Target.createTarget',{url:'about:blank'}),{sessionId:sid}=await cdp.send('Target.attachToTarget',{targetId,flatten:true});let closing=false;
   const evaluate=async expression=>{const v=await cdp.send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true,userGesture:true},sid);if(v.exceptionDetails)throw Error(v.exceptionDetails.exception?.description||v.exceptionDetails.text);return v.result.value;};
   const wait=async expression=>{const end=Date.now()+10000;while(Date.now()<end){if(await evaluate(expression))return;await sleep(30);}throw Error('DOM wait timed out: '+expression);};
   const click=label=>evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent===${JSON.stringify(label)}).click()`);
   const state=channel=>evaluate(`document.querySelector('[data-channel-state="'+${JSON.stringify(channel)}+'"]')?.textContent`);
   try{
    await cdp.send('Page.enable',{},sid);await cdp.send('Runtime.enable',{},sid);await cdp.send('Page.bringToFront',{},sid);await cdp.send('Emulation.setDeviceMetricsOverride',{width,height:1000,deviceScaleFactor:1,mobile:false},sid);
    cdp.on(sid,'Runtime.exceptionThrown',e=>errors.push(e.exceptionDetails.exception?.description||e.exceptionDetails.text));cdp.on(sid,'Runtime.consoleAPICalled',e=>{if(e.type==='error')errors.push(e.args.map(v=>v.value??v.description??'').join(' '));});
    cdp.on(sid,'Fetch.requestPaused',async({requestId,request})=>{try{
     const url=new URL(request.url);if(url.origin===origin)return await cdp.send('Fetch.continueRequest',{requestId},sid);a.equal(url.origin,apiOrigin);if(request.method==='OPTIONS')return await reply(204,{});
     requests.push({width,variant,method:request.method,path:url.pathname});a.equal(Object.entries(request.headers).find(([key])=>key.toLowerCase()==='authorization')?.[1],'Bearer synthetic-access');let data;
     if(request.method==='PUT'){
      a.ok(['settings-save','settings-save-recorded'].includes(variant));a.match(url.pathname,new RegExp('^'+base+'/communication-settings/(Email|WhatsApp|Meeting)$'));puts++;
      const channel=url.pathname.split('/').at(-1),body=JSON.parse(request.postData),expected=rows.find(row=>row.channel===channel);a.deepEqual(Object.keys(body).sort(),['externalAccountReference','messagesEnabled','phoneNumber','provider','replyToAddress','senderAddress','senderName','status']);a.equal(body.replyToAddress,'reply@example.invalid');a.equal(body.phoneNumber,'+000000000');a.equal(body.externalAccountReference,'synthetic-reference');a.equal(body.messagesEnabled,true);a.equal(body.provider,expected.provider);a.ok(!Object.keys(body).some(key=>/token|secret|password|hasSecureConnection/i.test(key)));
      const saved={...expected,...body};rows=rows.filter(row=>row.channel!==channel).concat(saved);return await reply(200,saved);
     }
     a.equal(request.method,'GET','No meeting, OAuth or provider send operation exists');
     if(url.pathname==='/api/auth/session')data={workspace:'AcademyAdmin',academyId:academy,roles:['AcademyAdmin'],displayName:'Synthetic Admin',isPlatformOwner:false};
     else if(url.pathname==='/api/academies')data=[{id:academy,name:'Synthetic Academy',timeZone:'Asia/Kolkata',enabledModulesJson:'["Core","Engagement","Scheduling","MultiBranch","Students","Teachers","Curriculum","Batches"]'}];
     else if(url.pathname==='/api/portal/announcements')data=[];
     else if(url.pathname===base+'/communication-settings'){
      reads++;if(variant.endsWith('-denied'))return await reply(403,{});if(variant==='meetings-retry'&&reads===1){releaseRead=()=>reply(503,{});return;}data=rows;
     }else if([base+'/students',base+'/teachers',base+'/batches',base+'/enrollments'].includes(url.pathname))data=[];
     else throw Error('Unexpected API/provider path '+url.pathname);
     return await reply(200,data);
     async function reply(status,data){await cdp.send('Fetch.fulfillRequest',{requestId,responseCode:status,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:origin},{name:'Access-Control-Allow-Headers',value:'Authorization, Content-Type'},{name:'Access-Control-Allow-Methods',value:'GET, PUT, OPTIONS'}],body:status===204?'':Buffer.from(JSON.stringify(data)).toString('base64')},sid);}
    }catch(e){if(closing&&e.message==='Session with given id not found.')return;errors.push(e.message);fs.writeFileSync(path.join(evidence,'interception-errors.json'),JSON.stringify(errors,null,2));await cdp.send('Fetch.failRequest',{requestId,errorReason:'BlockedByClient'},sid).catch(()=>{});}});
    await cdp.send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},sid);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument',{source:`if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(width===320?'light':'dark')});localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-access');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-refresh');localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-epoch');}`},sid);
    await cdp.send('Page.navigate',{url:origin+route},sid);
    if(settingsPage){
     await wait(`Boolean(document.querySelector('section[aria-label="Provider connection records"]'))`);
     a.equal(await evaluate(`(()=>{const n=document.querySelector('[data-provider-availability]'),r=n.getBoundingClientRect();return getComputedStyle(n).display!=='none'&&r.height>0&&r.top>=0&&r.bottom<=innerHeight;})()`),true,'Provider setup limitation must be visible before editing metadata');
     if(variant==='settings-denied'){await wait(`document.body.textContent.includes('Settings could not be loaded')`);a.equal(await evaluate(`document.querySelectorAll('[data-channel-state]').length`),0);a.match(await evaluate('document.body.textContent'),/Connection records are unavailable/);a.equal(await evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Save email settings').disabled`),true);}
     else{
      await wait(`document.querySelectorAll('[data-channel-state]').length===3`);a.match(await evaluate('document.body.textContent'),/Secure provider setup.*unavailable/);a.doesNotMatch(await evaluate('document.body.textContent'),/connected securely in the next step|Ready to connect/);
      if(variant==='settings-empty')a.match(await state('Meeting'),/No saved provider details/);
      else if(variant==='settings-disabled'){a.match(await state('Meeting'),/Disabled/);a.doesNotMatch(await state('Meeting'),/Connection recorded/);}
      else if(variant==='settings-partial'){a.match(await state('Email'),/Connection recorded/);a.match(await state('Meeting'),/Details saved/);}
      else if(recorded)a.match(await state('Meeting'),/Connection recorded.*verification unavailable/);
      else a.match(await state('Meeting'),/Details saved.*setup unavailable/);
      if(['settings-save','settings-save-recorded'].includes(variant)){
       for(const channel of ['Email','WhatsApp','Meeting']){await click(channel==='Meeting'?'Save meeting provider':'Save '+channel.toLowerCase()+' settings');await wait(`document.querySelector('[data-channel-notice="${channel}"]')?.textContent.startsWith('${channel} settings saved.')`);a.match(await state(channel),variant==='settings-save-recorded'?/Connection recorded.*verification unavailable/:/Details saved.*setup unavailable/);}a.equal(puts,3);
      }else a.equal(puts,0);
     }
    }else{
     await wait(`Boolean(document.body?.textContent.includes('Meeting creation and provider history are unavailable'))`);a.equal(await evaluate(`document.querySelector('.meeting-links-fields .enterprise-action-button').disabled`),true);a.equal(await evaluate(`document.querySelector('.meeting-links-fields .enterprise-action-button').matches(':disabled')`),true);
     if(variant==='meetings-retry'){const end=Date.now()+7000;while(!releaseRead&&Date.now()<end)await sleep(30);a.equal(typeof releaseRead,'function');a.equal(await evaluate(`Boolean(document.querySelector('.meeting-links-metrics'))`),false);await releaseRead();await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be loaded')`);await click('Try again');}
     if(variant==='meetings-denied'){await wait(`document.querySelector('[role="alert"]')?.textContent.includes('could not be loaded')`);a.equal(await evaluate(`Boolean(document.querySelector('.meeting-links-metrics'))`),false);}
     else{
      await wait(`Boolean(document.querySelector('.meeting-links-metrics'))`);
      if(provider!=='GoogleWorkspace'){await evaluate(`document.querySelector('input[name="provider"]').closest('.standard-select-field').querySelector('button').click()`);await wait(`Boolean(document.querySelector('[role="listbox"]'))`);await evaluate(`Array.from(document.querySelectorAll('[role="option"]')).find(n=>n.textContent===${JSON.stringify(provider==='Zoom'?'Zoom':'Microsoft Teams')}).click()`);await wait(`document.querySelector('input[name="provider"]').value===${JSON.stringify(provider)}`);}
      a.match(await evaluate(`document.querySelector('.meeting-provider-status').textContent`),/creation unavailable/);a.doesNotMatch(await evaluate(`document.querySelector('.meeting-provider-status').textContent`),/ connected/);
     }
     await evaluate(`document.querySelector('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}))`);await wait(`document.body.textContent.includes('No invitations were sent')`);a.match(await evaluate(`document.querySelector('.meeting-history').textContent`),/Provider meeting history is unavailable/);a.doesNotMatch(await evaluate(`document.querySelector('.meeting-history').textContent`),/No meeting links have been created|Edit details/);a.equal(await evaluate(`document.querySelectorAll('.meeting-history input,.meeting-history button').length`),0);a.equal(puts,0);
    }
    a.doesNotMatch(await evaluate('document.body.textContent'),/5092|localhost/);
    if(['settings-partial','meetings-google-true'].includes(variant)){const shot=await cdp.send('Page.captureScreenshot',{format:'png'},sid);fs.writeFileSync(path.join(evidence,variant+'-'+width+'.png'),Buffer.from(shot.data,'base64'));}
    results.push({width,variant,pass:true,puts,reads,provider});
   }catch(e){results.push({width,variant,pass:false,error:e.message,puts,reads,provider});}finally{closing=true;await cdp.send('Target.closeTarget',{targetId}).catch(()=>{});console.log(width+' '+variant+': '+(results.at(-1)?.pass?'PASS':'FAIL '+results.at(-1)?.error));}
  }
  a.equal(results.length,32);a.equal(results.filter(x=>!x.pass).length,0);a.deepEqual(errors,[]);passed=true;
 }catch(e){failure=e.message;throw e;}finally{
  if(cdp)await cdp.send('Browser.close').catch(()=>{});socket?.close();if(child){child.kill('SIGTERM');if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(2000)]);}if(started){server.closeAllConnections();await new Promise(r=>server.close(r));}
  let cleanupError=null;if(profile)try{if(path.dirname(profile)!==fs.realpathSync(os.tmpdir())||!/^c26-providers-[A-Za-z0-9]+$/.test(path.basename(profile))||fs.realpathSync(profile)!==profile)throw Error('Refused nonowned cleanup');fs.rmSync(profile,{recursive:true,force:true,maxRetries:3,retryDelay:100});}catch(e){cleanupError=e.message;passed=false;}
  fs.writeFileSync(resultFile,JSON.stringify({status:passed?'PASS':'FAIL',failure,results,errors,requests,sandboxDisabled:false,ownedProfileRemoved:!cleanupError,cleanupError,scope:'Real Chromium, synthetic metadata API writes only; no actual OAuth/provider connection, meetings, invites, live Identity/SQL/device or production acceptance.'},null,2));if(cleanupError)throw Error(cleanupError);
 }
 if(passed)console.log('32 native provider readiness cases PASS; twelve synthetic metadata PUTs, zero meeting/OAuth/invite calls, sandbox enabled, owned cleanup.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
