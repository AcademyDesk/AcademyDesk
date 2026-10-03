// Actual uploader event handlers with explicit React-hook/media/storage doubles.
// No real DOM, microphone, codec, upload, or browser-permission certification.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const ts = require('../../apps/web/node_modules/typescript');
const source = fs.readFileSync(require('node:path').resolve(__dirname, '../../apps/web/src/components/teacher-material-uploader.tsx'), 'utf8');
const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
function deferred() { let resolve, reject; const promise = new Promise((a,b) => { resolve=a; reject=b; }); return {promise,resolve,reject}; }
function fixture(options={}) {
  let cursor=0, mounted=true, lateUpdates=0, permissionCalls=0; const slots=[], cleanups=[], effects=[];
  const permission=deferred(), instances=[], tracks=[{stopped:0,stop(){this.stopped++;}}];
  const uploadCalls=[]; let historyRefreshes=0, uploadAttempts=0;
  const stream={getTracks:()=>tracks};
  class Recorder {
    static isTypeSupported(type) { return type===(options.mime || 'audio/webm;codecs=opus'); }
    constructor(input, config) { if(options.constructorFailure) throw Error('constructor failed'); this.stream=input; this.mimeType=config?.mimeType || 'audio/mp4'; this.state='inactive'; instances.push(this); }
    start(interval) { if(options.startFailure) throw Error('start failed'); this.interval=interval; this.state='recording'; }
    pause(){this.state='paused';} resume(){this.state='recording';}
    emit(data=new Blob(['synthetic audio'],{type:this.mimeType})){this.ondataavailable?.({data});}
    stop(){if(this.state==='inactive') throw Error('duplicate stop'); this.state='inactive'; this.stopCalls=(this.stopCalls||0)+1; if(!options.deferredStop)this.onstop?.();}
    finishStop(){assert.equal(this.state,'inactive');this.onstop?.();}
  }
  const React={
    useState(initial){const i=cursor++; if(!slots[i]) slots[i]={value:initial}; return [slots[i].value,value=>{if(!mounted)lateUpdates++; slots[i].value=typeof value==='function'?value(slots[i].value):value;}];},
    useRef(value){const i=cursor++; if(!slots[i])slots[i]={current:value}; return slots[i];},
    useEffect(callback,deps){const i=cursor++; if(!slots[i]){slots[i]={deps}; effects.push(()=>{cleanups.push(callback());});}}
  };
  const jsx=(type,props)=>({type,props:props||{}}), module={exports:{}};
  const upload={ MAX_MEDIA_BYTES:2*1024**3, pendingUpload:()=>null, forgetUpload:()=>{}, recordingExtension:type=>type.startsWith('audio/mp4')?'m4a':type.startsWith('audio/ogg')?'ogg':'webm', prepareUpload:()=>{throw Error('Unexpected upload');}, runMediaUpload:()=>{throw Error('Unexpected upload');} };
  if(options.allowUpload){
    upload.prepareUpload=async(file,scope,title,description)=>{const job={fileName:file.name,length:file.size,title:title.trim()||file.name,description:description.trim(),scope};uploadCalls.push({stage:'prepare',file,job});return job;};
    upload.runMediaUpload=async(file,job,config)=>{uploadCalls.push({stage:'run',file,job});uploadAttempts++;if(options.failFirstUpload&&uploadAttempts===1)throw Error('Synthetic storage interruption');config.onProgress(file.size,'Confirming upload');};
  }
  const context={module,exports:module.exports,require:id=>id==='react'?React:id==='react/jsx-runtime'?{jsx,jsxs:jsx}:id==='@/lib/class-media-upload'?upload:id==='@/lib/private-material'?{PREVIEW_BYTES:32*1024**2,previewMime:()=>null}:(()=>{throw Error(id);})(), navigator:options.unsupported?{}:{mediaDevices:{getUserMedia(){permissionCalls++;return permission.promise;}}},MediaRecorder:options.unsupported?undefined:Recorder,Blob,File,DOMException,AbortController,URL,Date,Promise,Error,localStorage:{},window:{confirm:()=>true}};
  vm.runInNewContext(compiled, context);
  const scope={ownerId:'synthetic-owner',batchId:'synthetic-batch',studentId:null,classSessionId:null};
  let tree;
  function render(){cursor=0;tree=module.exports.TeacherMaterialUploader({scope,onUploaded:async()=>{historyRefreshes++;}});while(effects.length)effects.shift()();return tree;}
  function nodes(node=tree){if(!node)return [];if(Array.isArray(node))return node.flatMap(n=>nodes(n));if(typeof node!=='object')return [];return [node,...nodes(node.props?.children ?? null)];}
  function text(node){if(node==null||typeof node==='boolean')return '';if(Array.isArray(node))return node.map(text).join('');if(typeof node!=='object')return String(node);return text(node.props?.children);}
  function button(name){render();const value=nodes().find(n=>n.type==='button'&&text(n)===name);assert.ok(value,'Missing button '+name);return value;}
  function input(name){render();return nodes().find(n=>n.type==='input'&&n.props.name===name);}
  render();
  return {permission,stream,tracks,instances,button,input,render,nodes,text,uploadCalls,get historyRefreshes(){return historyRefreshes;},async submit(){render();tree.props.onSubmit({preventDefault(){}});await new Promise(resolve=>setImmediate(resolve));},select(file){render();nodes().find(n=>n.type==='input'&&n.props.type==='file').props.onChange({target:{files:[file]}});render();},unmount(){mounted=false;cleanups.forEach(fn=>fn?.());},get lateUpdates(){return lateUpdates;},get permissionCalls(){return permissionCalls;}};
}
async function settle(){await Promise.resolve();await Promise.resolve();}
test('pending microphone request locks competing file/upload actions and shows waiting guidance',async()=>{
  const f=fixture();await settle();f.select(new File(['existing'],'existing.wav'));
  void f.button('Record audio').props.onClick();await settle();f.render();
  assert.equal(f.button('Confirm upload').props.disabled,true);
  assert.equal(f.nodes().find(n=>n.type==='input'&&n.props.type==='file').props.disabled,true);
  assert.match(f.text(f.render()),/Waiting for microphone permission/);
  f.permission.reject(new DOMException('synthetic denial','NotAllowedError'));await settle();
  assert.match(f.text(f.render()),/Microphone access/);assert.equal(f.button('Confirm upload').props.disabled,false);
});
test('permission denial retains selected file and optional metadata and permits a fresh request',async()=>{
  const f=fixture();await settle();f.select(new File(['existing'],'existing.wav'));f.input('title').props.onChange({target:{value:'Keep title'}});
  void f.button('Record audio').props.onClick();f.permission.reject(new DOMException('synthetic denial','NotAllowedError'));await settle();
  const output=f.text(f.render());assert.match(output,/existing.wav/);assert.match(output,/Microphone access/);assert.equal(f.input('title').props.value,'Keep title');
  assert.equal(f.button('Record audio').props.disabled,false);assert.equal(f.button('Confirm upload').props.disabled,false);
  void f.button('Record audio').props.onClick();await settle();assert.equal(f.permissionCalls,2);
});
test('start, pause, resume and stop produce reviewable audio with actual recorder MIME and release tracks',async()=>{
  const f=fixture({mime:'audio/mp4'});await settle();void f.button('Record audio').props.onClick();f.permission.resolve(f.stream);await settle();
  const r=f.instances[0];assert.equal(r.state,'recording');assert.equal(r.interval,1000);assert.equal(f.button('Confirm upload').props.disabled,true);
  f.button('Pause recording').props.onClick();assert.equal(r.state,'paused');f.button('Resume recording').props.onClick();assert.equal(r.state,'recording');
  r.emit();f.button('Stop recording').props.onClick();assert.equal(r.state,'inactive');assert.equal(f.tracks[0].stopped,1);
  assert.match(f.text(f.render()),/class-recording-\d+\.m4a/);assert.match(f.text(f.render()),/Recording ready/);assert.equal(f.button('Confirm upload').props.disabled,false);
});
test('empty recording stops tracks and does not enable upload',async()=>{
  const f=fixture();await settle();void f.button('Record audio').props.onClick();f.permission.resolve(f.stream);await settle();f.button('Stop recording').props.onClick();
  assert.match(f.text(f.render()),/No audio was recorded/);assert.equal(f.button('Confirm upload').props.disabled,true);assert.equal(f.tracks[0].stopped,1);
});
test('unmount stops active recorder and suppresses callbacks/state changes',async()=>{
  const f=fixture();await settle();void f.button('Record audio').props.onClick();f.permission.resolve(f.stream);await settle();f.instances[0].emit();f.unmount();
  assert.equal(f.instances[0].state,'inactive');assert.equal(f.tracks[0].stopped,1);assert.equal(f.lateUpdates,0);
});
test('late microphone grant after unmount releases tracks without creating recorder',async()=>{
  const f=fixture();await settle();void f.button('Record audio').props.onClick();f.unmount();f.permission.resolve(f.stream);await settle();
  assert.equal(f.instances.length,0);assert.equal(f.tracks[0].stopped,1);assert.equal(f.lateUpdates,0);
});
for(const failure of ['constructorFailure','startFailure'])test(failure+' releases microphone and allows retry',async()=>{
  const f=fixture({[failure]:true});await settle();void f.button('Record audio').props.onClick();f.permission.resolve(f.stream);await settle();
  assert.match(f.text(f.render()),/Microphone access/);assert.equal(f.tracks[0].stopped,1);assert.equal(f.button('Record audio').props.disabled,false);assert.equal(f.button('Confirm upload').props.disabled,true);
});
test('unsupported recording explains saved-file fallback without requesting microphone',async()=>{
  const f=fixture({unsupported:true});await settle();f.button('Record audio').props.onClick();await settle();assert.match(f.text(f.render()),/Recording is not supported/);assert.equal(f.permissionCalls,0);
});
test('cancel pending microphone unlocks retained file and discards late grant without recording',async()=>{
  const f=fixture();await settle();f.select(new File(['existing'],'existing.wav'));f.input('title').props.onChange({target:{value:'Keep title'}});
  void f.button('Record audio').props.onClick();await settle();f.button('Cancel microphone request').props.onClick();
  assert.equal(f.button('Confirm upload').props.disabled,false);assert.equal(f.nodes().find(n=>n.type==='input'&&n.props.type==='file').props.disabled,false);
  assert.equal(f.input('title').props.value,'Keep title');assert.match(f.text(f.render()),/existing.wav/);assert.match(f.text(f.render()),/cancelled/);
  const record=f.button('Record audio');assert.equal(record.props.disabled,true);record.props.onClick();assert.equal(f.permissionCalls,1);
  f.permission.resolve(f.stream);await settle();assert.equal(f.instances.length,0);assert.equal(f.tracks[0].stopped,1);
  assert.equal(f.button('Record audio').props.disabled,false);assert.match(f.text(f.render()),/existing.wav/);
});
test('late denial after cancel preserves cancellation guidance and upload availability',async()=>{
  const f=fixture();await settle();f.select(new File(['existing'],'existing.wav'));void f.button('Record audio').props.onClick();await settle();f.button('Cancel microphone request').props.onClick();
  f.permission.reject(new DOMException('synthetic late denial','NotAllowedError'));await settle();assert.match(f.text(f.render()),/cancelled/);assert.equal(f.button('Confirm upload').props.disabled,false);assert.equal(f.button('Record audio').props.disabled,false);
});
test('Remove file clears native picker value so the same original file can be reselected',async()=>{
  const f=fixture();await settle();f.select(new File(['existing'],'existing.wav'));
  const picker=f.nodes().find(n=>n.type==='input'&&n.props.type==='file');picker.props.ref.current={value:'C:\\fakepath\\existing.wav'};
  f.button('Remove file').props.onClick();assert.equal(picker.props.ref.current.value,'');assert.equal(f.button('Confirm upload').props.disabled,true);
});
test('successful recording start clears native picker value but denial does not',async()=>{
  const f=fixture();await settle();f.select(new File(['existing'],'existing.wav'));const picker=f.nodes().find(n=>n.type==='input'&&n.props.type==='file');picker.props.ref.current={value:'C:\\fakepath\\existing.wav'};
  void f.button('Record audio').props.onClick();assert.equal(picker.props.ref.current.value,'C:\\fakepath\\existing.wav');f.permission.resolve(f.stream);await settle();assert.equal(picker.props.ref.current.value,'');f.button('Stop recording').props.onClick();
});

// Real 32-MiB byte boundary, controlled recorder events; not codec/device evidence.
const recorderLimit=32*1024**2;
async function recordingFixture(options={}){
  const f=fixture(options);await settle();void f.button('Record audio').props.onClick();f.permission.resolve(f.stream);await settle();return f;
}
test('one byte below recorder threshold does not auto-stop or upload',async()=>{
  const f=await recordingFixture();const r=f.instances[0];r.emit(new Blob([new Uint8Array(recorderLimit-1)],{type:r.mimeType}));
  assert.equal(r.state,'recording');assert.equal(r.stopCalls,undefined);assert.equal(f.button('Confirm upload').props.disabled,true);assert.equal(f.uploadCalls.length,0);
  f.unmount();assert.equal(f.tracks[0].stopped,1);
});
test('exact recorder threshold stops once, retains metadata and waits for explicit confirmation',async()=>{
  const f=await recordingFixture();f.input('title').props.onChange({target:{value:'Keep limit title'}});const r=f.instances[0];
  r.emit(new Blob([new Uint8Array(recorderLimit-1)],{type:r.mimeType}));r.emit(new Blob(['x'],{type:r.mimeType}));
  assert.equal(r.stopCalls,1);assert.equal(f.tracks[0].stopped,1);assert.match(f.text(f.render()),/Recording stopped at the browser memory limit/);
  assert.equal(f.input('title').props.value,'Keep limit title');assert.equal(f.button('Confirm upload').props.disabled,false);assert.equal(f.uploadCalls.length,0);assert.equal(f.button('Record audio').props.disabled,false);
});
test('deferred limit stop includes final recorder chunk without duplicate stop and offers large-file fallback',async()=>{
  const f=await recordingFixture({deferredStop:true});const r=f.instances[0];r.emit(new Blob([new Uint8Array(recorderLimit)],{type:r.mimeType}));
  assert.equal(r.stopCalls,1);assert.equal(f.button('Confirm upload').props.disabled,true);assert.equal(f.tracks[0].stopped,0);
  r.emit(new Blob(['tail'],{type:r.mimeType}));r.finishStop();assert.equal(r.stopCalls,1);assert.equal(f.tracks[0].stopped,1);
  assert.match(f.text(f.render()),/Files over 32 MB are not previewed here/);assert.match(f.text(f.render()),/browser memory limit/);assert.equal(f.button('Confirm upload').props.disabled,false);assert.equal(f.uploadCalls.length,0);
});
test('unmount while limit stop is pending releases tracks and suppresses queued callbacks',async()=>{
  const f=await recordingFixture({deferredStop:true});const r=f.instances[0];r.emit(new Blob([new Uint8Array(recorderLimit)],{type:r.mimeType}));f.unmount();
  r.emit(new Blob(['late'],{type:r.mimeType}));r.finishStop();assert.equal(r.stopCalls,1);assert.equal(f.tracks[0].stopped,1);assert.equal(f.lateUpdates,0);assert.equal(f.uploadCalls.length,0);
});
test('threshold recording survives failed upload and explicit retry preserves exact bytes and clears on success',async()=>{
  const f=await recordingFixture({deferredStop:true,allowUpload:true,failFirstUpload:true,mime:'audio/mp4'});const r=f.instances[0];
  f.input('title').props.onChange({target:{value:'Limit title'}});f.nodes().find(n=>n.type==='textarea').props.onChange({target:{value:'Limit comment'}});
  r.emit(new Blob([new Uint8Array(recorderLimit)],{type:r.mimeType}));r.emit(new Blob(['tail'],{type:r.mimeType}));r.finishStop();
  assert.equal(f.uploadCalls.length,0);await f.submit();assert.match(f.text(f.render()),/Synthetic storage interruption/);assert.equal(f.button('Resume upload').props.disabled,false);assert.equal(f.historyRefreshes,0);
  const original=f.uploadCalls[0].file;assert.equal(original.size,recorderLimit+4);assert.equal(original.type,'audio/mp4');assert.match(original.name,/\.m4a$/);assert.equal(await original.slice(-4).text(),'tail');
  await f.submit();assert.equal(f.uploadCalls.length,4);assert.equal(f.uploadCalls[2].file,original);assert.equal(f.uploadCalls[3].file,original);assert.equal(f.uploadCalls[3].job.title,'Limit title');assert.equal(f.uploadCalls[3].job.description,'Limit comment');
  assert.equal(f.historyRefreshes,1);assert.match(f.text(f.render()),/Class material uploaded successfully/);assert.equal(f.input('title').props.value,'');assert.equal(f.nodes().find(n=>n.type==='textarea').props.value,'');assert.equal(f.button('Confirm upload').props.disabled,true);
});

test('active recorder error keeps interruption warning, original metadata and explicit upload',async()=>{
  const f=await recordingFixture({allowUpload:true,mime:'audio/mp4'});const r=f.instances[0];
  f.input('title').props.onChange({target:{value:'Interrupted title'}});f.nodes().find(n=>n.type==='textarea').props.onChange({target:{value:'Interrupted comment'}});
  r.emit(new Blob(['captured'],{type:r.mimeType}));r.onerror();
  assert.equal(r.stopCalls,1);assert.equal(f.tracks[0].stopped,1);assert.match(f.text(f.render()),/Recording was interrupted/);assert.doesNotMatch(f.text(f.render()),/Recording ready/);
  assert.equal(f.button('Confirm upload').props.disabled,false);assert.equal(f.button('Record audio').props.disabled,false);assert.equal(f.uploadCalls.length,0);
  await f.submit();assert.equal(await f.uploadCalls[0].file.text(),'captured');assert.equal(f.uploadCalls[0].job.title,'Interrupted title');assert.equal(f.uploadCalls[0].job.description,'Interrupted comment');assert.equal(f.historyRefreshes,1);assert.match(f.text(f.render()),/uploaded successfully/);
});
test('empty recorder error explains interruption and saved-file fallback without enabling upload',async()=>{
  const f=await recordingFixture();const r=f.instances[0];r.onerror();assert.match(f.text(f.render()),/Recording was interrupted/);assert.match(f.text(f.render()),/no audio|No audio/);assert.match(f.text(f.render()),/saved file/);assert.equal(f.button('Confirm upload').props.disabled,true);assert.equal(f.button('Record audio').props.disabled,false);assert.equal(f.tracks[0].stopped,1);
});
test('native inactive-before-error keeps final data and warning through queued stop without duplicate stop',async()=>{
  const f=await recordingFixture({deferredStop:true,mime:'audio/mp4',allowUpload:true});const r=f.instances[0];r.emit(new Blob(['first'],{type:r.mimeType}));r.state='inactive';r.onerror();
  assert.equal(r.stopCalls,undefined);assert.equal(f.button('Confirm upload').props.disabled,true);assert.ok(f.tracks[0].stopped>0);
  r.emit(new Blob(['tail'],{type:r.mimeType}));r.finishStop();assert.match(f.text(f.render()),/Recording was interrupted/);assert.equal(f.button('Confirm upload').props.disabled,false);assert.equal(r.stopCalls,undefined);assert.equal(f.uploadCalls.length,0);await f.submit();assert.equal(await f.uploadCalls[0].file.text(),'firsttail');
});
test('paused recorder error resets paused state and retains interruption warning',async()=>{
  const f=await recordingFixture();const r=f.instances[0];r.emit();f.button('Pause recording').props.onClick();r.onerror();assert.equal(r.stopCalls,1);assert.match(f.text(f.render()),/Recording was interrupted/);assert.doesNotMatch(f.text(f.render()),/Recording paused/);assert.equal(f.button('Confirm upload').props.disabled,false);assert.equal(f.tracks[0].stopped,1);
});
test('unmount detaches error handler and queued callbacks cannot update the departed page',async()=>{
  const f=await recordingFixture({deferredStop:true});const r=f.instances[0];const queuedError=r.onerror, queuedStop=r.onstop;f.unmount();assert.equal(r.onerror,null);queuedError();queuedStop();assert.equal(f.lateUpdates,0);assert.equal(f.uploadCalls.length,0);
});
test('error during pending memory-limit stop preserves interruption warning over ordinary limit guidance',async()=>{
  const f=await recordingFixture({deferredStop:true});const r=f.instances[0];r.emit(new Blob([new Uint8Array(recorderLimit)],{type:r.mimeType}));r.onerror();r.emit(new Blob(['tail'],{type:r.mimeType}));r.finishStop();assert.equal(r.stopCalls,1);assert.match(f.text(f.render()),/Recording was interrupted/);assert.equal(f.button('Confirm upload').props.disabled,false);assert.equal(f.uploadCalls.length,0);
});
