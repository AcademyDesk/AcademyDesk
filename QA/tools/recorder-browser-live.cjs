// Real exported Teacher UI, native Chromium permission/MediaRecorder APIs.
// CDP permissions are explicit; fake audio device prevents host microphone capture.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api,run,mode]=process.argv.slice(2),recovery=mode==='recovery',upload=mode==='upload',target=new URL(api);assert.ok(mode===undefined||recovery||upload);assert.equal(target.hostname,'127.0.0.1');assert.equal(target.protocol,'http:');assert.ok(target.port);assert.match(run,/^[a-f0-9]{32}$/);
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE',upload?'recorder-upload':recovery?'recorder-recovery':'recorder-browser',run);fs.mkdirSync(evidence,{recursive:true});
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':({'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser,phase='launch',activePage;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',args:['--use-fake-device-for-media-stream']});
  const auth=await browser.newContext();const login=await auth.request.post(api+'/api/auth/login',{data:{email:'qa-teacher-a@example.invalid',password:'Synthetic!39Ab'}});assert.equal(login.status(),200);const token=(await login.json()).accessToken;await auth.close();
  for(const width of [320,1440])for(const theme of ['light','dark']){
   phase=`${width}-${theme} setup`;const context=await browser.newContext({viewport:{width:1440,height:900}}),page=await context.newPage(),errors=[],calls=[];activePage=page;
   await context.addInitScript(({theme,token})=>{
    localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.Teacher',token);
    // Transparent observer; original native method determines outcome and tracks.
    window.__qaStreams=[];const native=navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);
    navigator.mediaDevices.getUserMedia=async constraints=>{const stream=await native(constraints);window.__qaStreams.push(stream);return stream;};
   },{theme,token});
   if(recovery)await context.addInitScript(require('./recorder-recovery-cases.cjs').observe);
   page.on('pageerror',e=>errors.push(e.name));page.on('console',m=>{if(m.type()==='error')errors.push('console error');});
   await context.route('**/*',async r=>{const req=r.request(),url=new URL(req.url());try{
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     if(!upload)assert.equal(req.method(),'GET','Recording must not auto-upload');
     else assert.ok(req.method()==='GET'||req.method()==='HEAD'||(req.method()==='POST'&&/^\/api\/teacher\/media-uploads(?:\/[a-f0-9-]+\/complete)?$/.test(url.pathname))||(req.method()==='PUT'&&/^\/api\/teacher\/media-uploads\/[a-f0-9-]+\/chunks\/0$/.test(url.pathname)),'Unexpected UI write');
     const response=await r.fetch({url:api+url.pathname+url.search});assert.equal(response.status(),req.method()==='POST'&&url.pathname==='/api/teacher/media-uploads'?201:200);calls.push({method:req.method(),path:url.pathname,status:response.status()});
     return r.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type','access-control-expose-headers':'Content-Length,Content-Disposition,Content-Range'}});
    }
    if(url.origin===origin||['data:','blob:'].includes(url.protocol))return r.continue();errors.push('external request');return r.abort('blockedbyclient');
   }catch(e){errors.push('route '+url.pathname+' '+e.name);try{await r.abort('failed');}catch{errors.push('route teardown');}}});
   const cdp=await context.newCDPSession(page),{targetInfo}=await cdp.send('Target.getTargetInfo');assert.ok(targetInfo.browserContextId);
   const permission=async setting=>{await cdp.send('Browser.setPermission',{permission:{name:'microphone'},setting,origin,browserContextId:targetInfo.browserContextId});};
   await page.goto(origin+'/teacher');await page.getByRole('heading',{name:'Welcome, Synthetic',exact:true}).waitFor();
   await page.locator('.teacher-portal-nav').getByRole('button',{name:/Classroom/}).click();
   const choose=page.getByRole('button',{name:'Class',exact:true});await choose.click();assert.equal(await page.getByRole('option').count(),2);await page.getByRole('option').nth(1).click();
   const form=page.locator('.teacher-upload-form');await form.waitFor();await page.waitForLoadState('networkidle');await page.setViewportSize({width,height:900});
   // Open classroom via desktop navigation first: mobile section navigation is
   // not accepted by this component gate. All recorder steps use target width.
   const bytes=Buffer.from('QA synthetic saved-file fallback, not audio'),fileName='denial-fallback.bin';
   await form.getByLabel('Choose class attachment').setInputFiles({name:fileName,mimeType:'application/octet-stream',buffer:bytes});
   await form.locator('[name=title]').fill('Native permission retained title');await form.locator('[name=description]').fill('Native permission retained comment');
   if(upload){
    phase=`${width}-${theme} linked recording upload`;await permission('granted');assert.equal(await page.evaluate(async()=> (await navigator.permissions.query({name:'microphone'})).state),'granted');
    const result=await require('./recorder-upload-cases.cjs').verify({page,form,calls,api,token,evidence,width,theme});
    await page.waitForLoadState('networkidle');await context.unrouteAll({behavior:'wait'});assert.deepEqual(errors,[]);checks.push({width,theme,...result,requests:calls.length});await context.close();activePage=null;
    console.log(`BLOB RECORDER UPLOAD BROWSER PASS ${width}px ${theme}: native fake-device recording -> explicit201/chunk200/complete200 -> refreshed history/native readback/hash/playback/denied controls.`);continue;
   }
   if(recovery){
    phase=`${width}-${theme} controlled recovery`;await permission('granted');assert.equal(await page.evaluate(async()=> (await navigator.permissions.query({name:'microphone'})).state),'granted');
    const result=await require('./recorder-recovery-cases.cjs').verify({page,form,bytes,fileName,evidence,width,theme});
    await page.waitForLoadState('networkidle');await context.unrouteAll({behavior:'wait'});assert.deepEqual(errors,[]);assert.ok(calls.length>0);
    checks.push({width,theme,...result,requests:calls.length});await context.close();activePage=null;
    console.log(`BLOB RECORDER RECOVERY BROWSER PASS ${width}px ${theme}: controlled late grant/rejection cancellation; interrupted native stop; exact32MiB plus native tail; no upload.`);continue;
   }
   phase=`${width}-${theme} native denial`;await permission('denied');assert.equal(await page.evaluate(async()=> (await navigator.permissions.query({name:'microphone'})).state),'denied');
   await form.getByRole('button',{name:'Record audio',exact:true}).click();await form.getByRole('status').filter({hasText:'Microphone access could not be started.'}).waitFor();
   assert.equal(await form.locator('[name=title]').inputValue(),'Native permission retained title');assert.equal(await form.locator('[name=description]').inputValue(),'Native permission retained comment');
   assert.equal(await form.getByLabel('Choose class attachment').evaluate(e=>e.files[0].name),fileName);assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isEnabled(),true);assert.equal(await page.evaluate(()=>window.__qaStreams.length),0);
   await page.screenshot({path:path.join(evidence,`${width}-${theme}-denied.png`),fullPage:true});
   phase=`${width}-${theme} native recording`;await permission('granted');assert.equal(await page.evaluate(async()=> (await navigator.permissions.query({name:'microphone'})).state),'granted');
   await form.getByRole('button',{name:'Record audio',exact:true}).click();await form.getByRole('button',{name:'Pause recording',exact:true}).waitFor();
   assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isDisabled(),true);assert.equal(await form.getByLabel('Choose class attachment').isDisabled(),true);
   await page.waitForTimeout(1250);await form.getByRole('button',{name:'Pause recording',exact:true}).click();await form.getByRole('button',{name:'Resume recording',exact:true}).waitFor();
   await form.getByRole('button',{name:'Resume recording',exact:true}).click();await page.waitForTimeout(1250);await form.getByRole('button',{name:'Stop recording',exact:true}).click();
   await form.getByRole('status').filter({hasText:'Recording ready. Review it, then confirm upload.'}).waitFor();
   assert.equal(await page.evaluate(()=>window.__qaStreams.length===1&&window.__qaStreams.every(s=>s.getTracks().every(t=>t.readyState==='ended'))),true);
   phase=`${width}-${theme} native playback`;const media=form.locator('video,audio');await media.waitFor();await media.scrollIntoViewIfNeeded();
   const played=await media.evaluate(async e=>{await e.play();for(let i=0;i<20&&e.currentTime<=0;i++)await new Promise(r=>setTimeout(r,250));const result={time:e.currentTime,ready:e.readyState,error:e.error?.code??null,paused:e.paused,duration:e.duration};e.pause();return result;});assert.ok(played.time>0,'Native playback clock did not advance: '+JSON.stringify(played));assert.ok(played.ready>=2,'Native playback not ready: '+JSON.stringify(played));assert.equal(played.error,null);
   const recorded=await media.evaluate(async e=>{const b=await (await fetch(e.src)).arrayBuffer();return {length:b.byteLength,head:Array.from(new Uint8Array(b).slice(0,4))};});assert.ok(recorded.length>0);assert.deepEqual(recorded.head,[26,69,223,163]);
   assert.equal(await form.locator('[name=title]').inputValue(),'Native permission retained title');assert.equal(await form.locator('[name=description]').inputValue(),'Native permission retained comment');assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isEnabled(),true);
   await form.getByRole('button',{name:'Remove file',exact:true}).click();assert.equal(await form.getByLabel('Choose class attachment').evaluate(e=>e.files.length),0);
   await form.getByRole('button',{name:'Record audio',exact:true}).click();await form.getByRole('button',{name:'Pause recording',exact:true}).waitFor();
   phase=`${width}-${theme} native unmount`;await choose.click();await page.getByRole('option',{name:'Select class',exact:true}).click();await form.waitFor({state:'detached'});
   assert.equal(await page.evaluate(()=>window.__qaStreams.length===2&&window.__qaStreams.every(s=>s.getTracks().every(t=>t.readyState==='ended'))),true);
   await page.waitForLoadState('networkidle');await context.unrouteAll({behavior:'wait'});assert.deepEqual(errors,[]);assert.ok(calls.length>0);checks.push({width,theme,permission:'native denied -> granted',recording:'fake-device native start/pause/resume/stop/playback/unmount',recordedBytes:recorded.length,requests:calls.length});await context.close();activePage=null;
   console.log(`BLOB RECORDER BROWSER PASS ${width}px ${theme}: native denial retained file/details; native fake-device recovery/playback/track cleanup; no upload.`);
  }
  const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');assert.equal(checks.length,4);fs.writeFileSync(path.join(evidence,'receipt.json'),JSON.stringify({classification:upload?'native fake-device recording/explicit real upload/exported Teacher history/readback playback/Student API/denied API probes/real SQL-private Azurite; desktop navigation setup; physical devices/Student UI/native Download NOT RUN':recovery?'controlled Promise scheduling/error/data signals in real exported UI; native fake-device streams/MediaRecorder final stop events; real read-only SQL/API; human prompt/native error/long-duration limit/physical devices/upload NOT RUN':'native CDP-configured browser permission; synthetic audio device; real read-only SQL/API; desktop navigation setup; human prompt/cancel/physical devices NOT RUN',teacherExportSha256:sha(path.join(web,'teacher.html')),uploaderSourceSha256:sha(path.join(root,'apps/web/src/components/teacher-material-uploader.tsx')),checks},null,2));console.log('BLOB RECORDER RECEIPT '+evidence);
 }catch(e){if(activePage&&!activePage.isClosed()){await activePage.screenshot({path:path.join(evidence,'failed.png'),fullPage:true}).catch(()=>{});}throw new Error('Phase '+phase+'; '+e.name+': '+String(e.message).split(/\r?\n/)[0]);}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error('Recorder browser failed: '+e.name+': '+String(e.message).split(/\r?\n/)[0].replace(/Bearer\s+\S+/gi,'Bearer [REDACTED]')+'; request logs/stacks omitted.');process.exitCode=1;});
