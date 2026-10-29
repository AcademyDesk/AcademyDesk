// Test-only signals, never application injection or native failure certification.
const assert=require('node:assert/strict'),path=require('node:path');
exports.observe=()=>{
 const gum=navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices),Native=window.MediaRecorder,NativeFile=window.File;
 window.__qaRecovery={hold:false,pending:null,held:null,recorders:[],events:[],files:[]};
 window.File=class extends NativeFile{constructor(...args){super(...args);if(this.name.startsWith('class-recording-'))window.__qaRecovery.files.push(this);}};
 navigator.mediaDevices.getUserMedia=async constraints=>{
  const q=window.__qaRecovery;if(!q.hold)return gum(constraints);
  const stream=await gum(constraints);q.held=stream;
  return new Promise((resolve,reject)=>{q.pending={resolve,reject};});
 };
 window.MediaRecorder=class extends Native{
  constructor(...args){super(...args);const q=window.__qaRecovery;q.recorders.push(this);
   const entry={sizes:[],blobs:[],stops:0,stopCalls:0};q.events.push(entry);
   this.addEventListener('dataavailable',event=>{entry.sizes.push(event.data.size);if(event.data.size)entry.blobs.push(event.data);});
   this.addEventListener('stop',()=>entry.stops++);
   const stop=this.stop.bind(this);this.stop=()=>{entry.stopCalls++;return stop();};
  }
 };
};
exports.verify=async({page,form,bytes,fileName,evidence,width,theme})=>{
 const status=text=>form.getByRole('status').filter({hasText:text});
 const ended=()=>page.evaluate(()=>window.__qaStreams.every(s=>s.getTracks().every(t=>t.readyState==='ended')));
 const original=async()=>{
  assert.equal(await form.getByLabel('Choose class attachment').evaluate(e=>e.files[0]?.name),fileName);
  assert.deepEqual(await form.getByLabel('Choose class attachment').evaluate(async e=>Array.from(new Uint8Array(await e.files[0].arrayBuffer()))),Array.from(bytes));
  assert.equal(await form.locator('[name=title]').inputValue(),'Native permission retained title');
  assert.equal(await form.locator('[name=description]').inputValue(),'Native permission retained comment');
 };
 // Native grant is held at a test-only Promise boundary, not an OS prompt.
 for(const resolution of ['grant','reject']){
  await page.evaluate(()=>{window.__qaRecovery.hold=true;});
  await form.getByRole('button',{name:'Record audio',exact:true}).click();
  await page.waitForFunction(()=>!!window.__qaRecovery.pending);
  assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isDisabled(),true);
  await form.getByRole('button',{name:'Cancel microphone request',exact:true}).click();
  await status('Microphone request cancelled.').waitFor();await original();
  assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isEnabled(),true);
  assert.equal(await form.getByLabel('Choose class attachment').isEnabled(),true);
  assert.equal(await form.getByRole('button',{name:'Record audio',exact:true}).isDisabled(),true);
  assert.equal(await page.evaluate(()=>window.__qaRecovery.recorders.length),0);
  await page.evaluate(resolution=>{const q=window.__qaRecovery;q.hold=false;const pending=q.pending;q.pending=null;
   if(resolution==='grant')pending.resolve(q.held);else{q.held.getTracks().forEach(t=>t.stop());pending.reject(new DOMException('Synthetic cancellation rejection','NotAllowedError'));}
  },resolution);
  await page.waitForFunction(()=>window.__qaRecovery.held.getTracks().every(t=>t.readyState==='ended'));
  assert.equal(await form.getByRole('button',{name:'Record audio',exact:true}).isEnabled(),true);
  await original();await status('Microphone request cancelled.').waitFor();
  assert.equal(await page.evaluate(()=>window.__qaRecovery.recorders.length),0);
 }
 // Controlled error event; native stop/data tail and real tracks are retained.
 await form.getByRole('button',{name:'Record audio',exact:true}).click();await form.getByRole('button',{name:'Pause recording',exact:true}).waitFor();
 await page.waitForTimeout(1250);
 await page.evaluate(()=>window.__qaRecovery.recorders[0].dispatchEvent(new Event('error')));
 await status('Recording was interrupted. Review the captured audio').waitFor();assert.equal(await ended(),true);
 const interrupted=await page.evaluate(()=>{const q=window.__qaRecovery,r=q.recorders[0],e=q.events[0];return {state:r.state,sizes:e.sizes,stops:e.stops,stopCalls:e.stopCalls,fileSize:q.files[0].size};});
 assert.equal(interrupted.state,'inactive');assert.equal(interrupted.stops,1);assert.equal(interrupted.stopCalls,1);assert.ok(interrupted.sizes.reduce((a,b)=>a+b,0)>0);
 assert.equal(interrupted.fileSize,interrupted.sizes.reduce((a,b)=>a+b,0));
 assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isEnabled(),true);
 assert.equal(await form.locator('[name=title]').inputValue(),'Native permission retained title');
 assert.equal(await form.locator('[name=description]').inputValue(),'Native permission retained comment');
 await page.screenshot({path:path.join(evidence,`${width}-${theme}-interrupted.png`),fullPage:true});
 await form.getByRole('button',{name:'Remove file',exact:true}).click();
 // Deliver the actual boundary as a controlled BlobEvent before the first
 // native timeslice. This inert payload is not decodable audio/long capture.
 await form.getByRole('button',{name:'Record audio',exact:true}).click();await form.getByRole('button',{name:'Pause recording',exact:true}).waitFor();
 const injected=await page.evaluate(()=>{const q=window.__qaRecovery,r=q.recorders[1];const before=q.events[1].sizes.reduce((a,b)=>a+b,0);const size=32*1024*1024-before;if(size<=0)throw Error('Unexpected native data size');
  r.dispatchEvent(new BlobEvent('dataavailable',{data:new Blob([new Uint8Array(size)],{type:r.mimeType})}));return {before,size};});
 await status('Recording stopped at the browser memory limit.').waitFor();assert.equal(await ended(),true);
 const limited=await page.evaluate(async()=>{const q=window.__qaRecovery,r=q.recorders[1],e=q.events[1],file=q.files[1];
  const hash=async blob=>Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',await blob.arrayBuffer()))).map(b=>b.toString(16).padStart(2,'0')).join('');
  return {state:r.state,mime:r.mimeType,sizes:e.sizes,stops:e.stops,stopCalls:e.stopCalls,fileSize:file.size,fileType:file.type,fileName:file.name,fileHead:Array.from(new Uint8Array(await file.slice(0,4).arrayBuffer())),fileHash:await hash(file),chunksHash:await hash(new Blob(e.blobs))};});
 assert.equal(limited.state,'inactive');assert.equal(limited.stops,1);assert.equal(limited.stopCalls,1);
 assert.equal(injected.before+injected.size,32*1024*1024);
 const total=limited.sizes.reduce((a,b)=>a+b,0);assert.ok(total>=32*1024*1024);assert.ok(limited.sizes.length>=2,'Native final data event was lost');
 assert.equal(limited.fileSize,total);assert.equal(limited.fileType,limited.mime);assert.equal(limited.fileHash,limited.chunksHash);assert.match(limited.fileName,/^class-recording-\d+\.webm$/);
 const preview=form.locator('audio');
 // A nonempty native tail pushes the preview over32MiB: UI omits it. At the
 // exact boundary, inert zero-headed data correctly fails format sniffing;
 // only a real preceding native WebM header qualifies for a preview.
 if(total>32*1024*1024){await form.getByText('Files over 32 MB are not previewed here.',{exact:false}).waitFor();assert.equal(await preview.count(),0);}
 else if(limited.fileHead.join(',')==='26,69,223,163'){await preview.waitFor();assert.equal(await preview.evaluate(async e=>(await (await fetch(e.src)).blob()).size),total);}
 else{assert.deepEqual(limited.fileHead,[0,0,0,0]);await form.getByText('If this device cannot preview the format,',{exact:false}).waitFor();assert.equal(await preview.count(),0);}
 assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isEnabled(),true);
 assert.equal(await form.locator('[name=title]').inputValue(),'Native permission retained title');
 assert.equal(await form.locator('[name=description]').inputValue(),'Native permission retained comment');
 await page.screenshot({path:path.join(evidence,`${width}-${theme}-limit.png`),fullPage:true});
 await form.getByRole('button',{name:'Remove file',exact:true}).click();assert.equal(await form.locator('.teacher-file-preview').count(),0);
 assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isDisabled(),true);
 assert.equal(await form.getByRole('button',{name:'Record audio',exact:true}).isEnabled(),true);
 assert.equal(await ended(),true);
 return {cases:4,cancellation:'controlled held native grant and controlled rejection; retained exact original bytes/details; no recorder',interruption:{signal:'controlled error event',...interrupted},limit:{signal:'controlled32MiB BlobEvent plus native stop tail',...injected,totalBytes:total,...limited},upload:'NOT RUN; no auto-upload',physicalDevices:'NOT RUN'};
};
