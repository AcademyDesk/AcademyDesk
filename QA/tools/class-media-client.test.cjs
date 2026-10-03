// Protocol/unit tests, NOT a browser or real backend substitute. Uses the actual
// TS modules compiled in memory; transport/storage are explicit test doubles.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript');
const root = path.resolve(__dirname, '../../apps/web/src/lib');
function moduleFrom(name, api) {
  const source = ts.transpileModule(fs.readFileSync(path.join(root, name + '.ts'), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
  const module = { exports: {} }; new Function('require', 'module', 'exports', source)(id => { if (id !== './api') throw Error('Unexpected import'); return api; }, module, module.exports); return module.exports;
}
const apiUrl = 'http://localhost:5092';
const lib = moduleFrom('class-media-upload', { apiUrl, academyApi: () => { throw Error('Test must supply transport'); } });
const material = moduleFrom('private-material', { apiUrl });
test('native ticket uses authenticated POST with a fixed private path, never an account token in a URL', async () => {
  const path = '/api/class-media/33333333-3333-3333-3333-333333333333/content'; let called = false;
  const value = await material.nativeMaterialTicket(path, new AbortController().signal, async (url, init) => {
    called = true; assert.equal(url, '/api/class-material-downloads/tickets'); assert.equal(init.method, 'POST');
    assert.deepEqual(JSON.parse(init.body), { path }); assert.equal(init.redirect, 'error'); assert.equal(init.cache, 'no-store');
    return new Response(JSON.stringify({ ticket: 'SyntheticScopedCredential', expiresInSeconds: 60 }));
  });
  assert.equal(value, 'SyntheticScopedCredential'); assert.equal(called, true);
});
test('native ticket rejects bad paths, malformed replies and cancellation before browser handoff', async () => {
  const path = '/api/class-media/33333333-3333-3333-3333-333333333333/content';
  await assert.rejects(material.nativeMaterialTicket('//foreign.invalid/', new AbortController().signal, () => { throw Error('Should not transport'); }));
  for (const result of [null, { ticket: 'token?query', expiresInSeconds: 60 }, { ticket: 'token', expiresInSeconds: 3600 }])
    await assert.rejects(material.nativeMaterialTicket(path, new AbortController().signal, async () => new Response(JSON.stringify(result))));
  const abort = new AbortController(); abort.abort();
  await assert.rejects(material.nativeMaterialTicket(path, abort.signal, async () => new Response(JSON.stringify({ ticket: 'token', expiresInSeconds: 60 }))));
});
test('native browser handoff posts only scoped credential, clears its form and retains target until cleanup', () => {
  const nodes = []; let submitted;
  const doc = { body: { appendChild: item => nodes.push(item) }, createElement: tag => ({ tag, appendChild(item) { this.child = item; }, remove() { this.removed = true; }, submit() { submitted = { action: this.action, method: this.method, target: this.target, key: this.child.name, value: this.child.value }; } }) };
  const cleanup = material.handoffNativeMaterial('/api/class-media/33333333-3333-3333-3333-333333333333/content', 'SyntheticCredential', doc);
  assert.equal(submitted.method, 'POST'); assert.equal(submitted.action, apiUrl + '/api/class-media/33333333-3333-3333-3333-333333333333/content');
  assert.equal(submitted.key, 'ticket'); assert.equal(submitted.value, 'SyntheticCredential'); assert.equal(nodes[1].child.value, '');
  assert.equal(nodes[1].removed, true); assert.equal(nodes[0].removed, undefined); cleanup(); assert.equal(nodes[0].removed, true);
  assert.throws(() => material.handoffNativeMaterial('//foreign.invalid/', 'SyntheticCredential', doc));
});
test('legacy uploader GUID-N filenames use private actions rather than disappearing as relative external links', () => {
  const path = '/uploads/teacher-materials/11111111111111111111111111111111.mov';
  assert.equal(material.privateMaterialPath(path), path); assert.equal(material.privateMaterialPath('/api/class-media/------------------------------------/content'), null);
});
const ownerId = '11111111-1111-1111-1111-111111111111', batchId = '22222222-2222-2222-2222-222222222222', id = '33333333-3333-3333-3333-333333333333';
const scope = { ownerId, batchId, studentId: null, classSessionId: null };
function storage() { const values = new Map(); return { getItem: k => values.get(k) ?? null, setItem: (k,v) => values.set(k,v), removeItem: k => values.delete(k) }; }
const json = (value, status = 200) => new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } });
function transport(file, options = {}) {
  const uploaded = new Set(); const calls = []; let lost = false; let completed = false; let resources = 0;
  const api = async (url, init) => {
    calls.push({ url, init }); init.signal?.throwIfAborted();
    if (url === '/api/teacher/media-uploads') return options.deniedCreate
      ? json({ message: 'Select an active assigned batch, student and class session where applicable.' }, 400)
      : json({ id, fileName: file.name.trim(), length: file.size, chunkBytes: lib.MEDIA_CHUNK_BYTES });
    if (url.endsWith('/complete')) {
      if (options.completeFailure) return json({ message: 'Synthetic SQL failure' }, 500);
      if (!completed) { completed = true; resources++; }
      if (options.lostCompletion && !lost) { lost = true; throw Error('Synthetic lost completion response after persistence'); }
      return json({ id, url: `/api/class-media/${id}/content` });
    }
    if (url.includes('/chunks/')) {
      if (options.rejectedChunk && !lost) { lost = true; return json({message:'Private media storage is unavailable. Keep your file and retry later.'},503); }
      const index = Number(url.split('/').at(-1)); uploaded.add(index);
      assert.equal(init.headers['Content-Type'], 'application/octet-stream');
      assert.equal(init.body.size, file.slice(index * lib.MEDIA_CHUNK_BYTES, (index + 1) * lib.MEDIA_CHUNK_BYTES).size);
      if (options.lostChunk && !lost) { lost = true; throw Error('Synthetic lost response after persistence'); }
      return json({ message: 'Chunk saved' });
    }
    return json({ uploadedBlocks: [...uploaded] });
  };
  return { api, calls, uploaded, resourceCount: () => resources };
}
test('omitted optional details default and one bounded chunk completes then clears draft', async () => {
  const file = new File(['abc'], 'phone.ogg'); const store = storage(); const job = await lib.prepareUpload(file, scope, '', '', store); const network = transport(file);
  assert.equal(job.title, file.name); assert.equal(job.description, '');
  await lib.runMediaUpload(file, job, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api });
  assert.equal(store.getItem(lib.uploadKey(scope)), null);
  assert.equal(network.calls.filter(x => x.url.includes('/chunks/')).length, 1);
  const body = JSON.parse(network.calls[0].init.body); assert.equal(body.studentId, null); assert.equal(body.classSessionId, null); assert.equal(body.description, null);
  assert.ok(network.calls.every(x => x.init.redirect === 'error' && x.init.cache === 'no-store'));
});
test('lost chunk response resumes same request ID and does not restage persisted bytes', async () => {
  const file = new File([new Uint8Array(lib.MEDIA_CHUNK_BYTES + 37)], 'phone.mov'); const store = storage(); const job = await lib.prepareUpload(file, scope, 'Original', 'Comment', store); const network = transport(file, { lostChunk: true });
  await assert.rejects(lib.runMediaUpload(file, job, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api }), /lost response/);
  const resumed = await lib.prepareUpload(file, scope, 'Different title', 'Different comment', store);
  assert.equal(resumed.requestId, job.requestId); assert.equal(resumed.title, 'Original');
  await lib.runMediaUpload(file, resumed, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api });
  assert.deepEqual(network.calls.filter(x => x.url.includes('/chunks/')).map(x => Number(x.url.split('/').at(-1))), [0,1]);
});
test('middle-byte mutation cannot silently reuse uploaded blocks even if edge samples match', async () => {
  const bytes = new Uint8Array(100000); const file = new File([bytes], 'phone.mov'); const store = storage(); const job = await lib.prepareUpload(file, scope, '', '', store); const network = transport(file, { lostChunk: true });
  await assert.rejects(lib.runMediaUpload(file, job, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api }));
  bytes[50000] = 1; const changed = new File([bytes], file.name);
  const resumed = await lib.prepareUpload(changed, scope, '', '', store);
  await assert.rejects(lib.runMediaUpload(changed, resumed, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api }), /differs from the saved/);
  assert.equal(network.calls.filter(x => x.url.includes('/chunks/')).length, 1);
});
test('completion failure preserves same draft and full chunk hashes for retry', async () => {
  const file = new File(['abc'], 'phone.mp4'); const store = storage(); const job = await lib.prepareUpload(file, scope, '', '', store); const network = transport(file, { completeFailure: true });
  await assert.rejects(lib.runMediaUpload(file, job, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api }), /Synthetic SQL/);
  assert.equal(lib.pendingUpload(scope, store).requestId, job.requestId); assert.ok(lib.pendingUpload(scope, store).hashes['0']);
});
test('lost completion after persistence retains draft and retries same ID without reupload or duplicate resource', async () => {
  const file = new File(['abc'], 'completion.wav'); const store = storage();
  const job = await lib.prepareUpload(file, scope, 'Original title', 'Original comment', store);
  const network = transport(file, { lostCompletion: true });
  await assert.rejects(lib.runMediaUpload(file, job, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api }), /lost completion/);
  assert.equal(network.resourceCount(), 1);
  const saved = lib.pendingUpload(scope, store);
  assert.equal(saved.requestId, job.requestId); assert.equal(saved.title, 'Original title'); assert.equal(saved.description, 'Original comment');
  await lib.runMediaUpload(file, saved, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api });
  assert.equal(network.calls.filter(x => x.url.includes('/chunks/')).length, 1);
  assert.equal(network.calls.filter(x => x.url.endsWith('/complete')).length, 2);
  assert.deepEqual(network.calls.filter(x => x.url === '/api/teacher/media-uploads').map(x => JSON.parse(x.init.body).clientRequestId), [job.requestId,job.requestId]);
  assert.equal(network.resourceCount(), 1); assert.equal(lib.pendingUpload(scope, store), null);
});
test('HTTP 503 chunk rejection retains immutable draft; retry uploads rejected block using same request ID', async () => {
  const file = new File(['abc'], 'retry.wav'); const store = storage();
  const job = await lib.prepareUpload(file, scope, 'Original title', 'Original comment', store);
  const network = transport(file, { rejectedChunk: true });
  await assert.rejects(lib.runMediaUpload(file, job, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api }), /Keep your file and retry later/);
  assert.equal(network.uploaded.size, 0);
  const saved = lib.pendingUpload(scope, store);
  assert.equal(saved.requestId, job.requestId); assert.equal(saved.title, 'Original title'); assert.equal(saved.description, 'Original comment');
  assert.equal(network.calls.some(x => x.url.endsWith('/complete')), false);
  await lib.runMediaUpload(file, saved, { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api });
  const starts = network.calls.filter(x => x.url === '/api/teacher/media-uploads').map(x => JSON.parse(x.init.body).clientRequestId);
  assert.deepEqual(starts, [job.requestId, job.requestId]); assert.equal(network.uploaded.size, 1);
  assert.equal(store.getItem(lib.uploadKey(scope)), null);
});
test('wrong file on reload is rejected before new HTTP', async () => {
  const store = storage(); await lib.prepareUpload(new File(['abc'], 'phone.mov'), scope, '', '', store);
  await assert.rejects(lib.prepareUpload(new File(['xyz'], 'phone.mov'), scope, '', '', store), /original file/);
});
test('revoked create retains original draft; explicit local clear sends no API write or delete', async () => {
  const file = new File([new Uint8Array(lib.MEDIA_CHUNK_BYTES + 1)], 'revoked.bin'); const store = storage();
  const job = await lib.prepareUpload(file, scope, 'Original title', 'Original comment', store);
  const settings = {}; const network = transport(file, settings); const abort = new AbortController();
  await assert.rejects(lib.runMediaUpload(file, job, { storage: store, signal: abort.signal, onProgress() { abort.abort(); }, api: network.api }), { name: 'AbortError' });
  const before = network.calls.length; settings.deniedCreate = true;
  await assert.rejects(lib.runMediaUpload(file, lib.pendingUpload(scope, store), { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api }), /active assigned batch/);
  assert.equal(network.calls.length, before + 1); assert.equal(network.uploaded.size, 1);
  const saved = lib.pendingUpload(scope, store);
  assert.equal(saved.requestId, job.requestId); assert.equal(saved.title, job.title); assert.equal(saved.description, job.description);
  assert.equal(network.calls.some(x => x.url.endsWith('/complete')), false);
  const retainedCalls = network.calls.length; lib.forgetUpload(scope, store);
  assert.equal(lib.pendingUpload(scope, store), null); assert.equal(network.calls.length, retainedCalls); assert.equal(network.uploaded.size, 1);
});
test('pause during a confirmed block preserves draft and resumes without duplicate upload', async () => {
  const file = new File([new Uint8Array(lib.MEDIA_CHUNK_BYTES + 1)], 'phone.mov'); const store = storage(); const job = await lib.prepareUpload(file, scope, '', '', store); const network = transport(file); const abort = new AbortController();
  await assert.rejects(lib.runMediaUpload(file, job, { storage: store, signal: abort.signal, onProgress() { abort.abort(); }, api: network.api }), { name: 'AbortError' });
  assert.ok(lib.pendingUpload(scope, store)); assert.equal(network.uploaded.size, 1);
  await lib.runMediaUpload(file, lib.pendingUpload(scope,store), { storage: store, signal: new AbortController().signal, onProgress() {}, api: network.api });
  assert.deepEqual(network.calls.filter(x => x.url.includes('/chunks/')).map(x => x.url.split('/').at(-1)), ['0','1']);
});
test('server manifest mismatch prevents all chunks', async () => {
  const file = new File(['abc'], 'phone.mov'); const store = storage(); const job = await lib.prepareUpload(file, scope, '', '', store); let calls = 0;
  await assert.rejects(lib.runMediaUpload(file, job, { storage: store, signal: new AbortController().signal, onProgress() {}, api: async () => { calls++; return json({ id, fileName: 'other.mov', length:3, chunkBytes:1 }); } }), /do not match/); assert.equal(calls,1);
});
test('no storage permission, invalid filenames and empty files fail before transport', async () => {
  await assert.rejects(lib.prepareUpload(new File(['x'], 'run.exe '), scope, '', '', storage()), /Executable/);
  await assert.rejects(lib.prepareUpload(new File([], 'file.mov'), scope, '', '', storage()), /nonempty/);
  await assert.rejects(lib.prepareUpload(new File(['x'], 'file.mov'), scope, '', '', { ...storage(), setItem() { throw Error('quota'); } }), /quota/);
});
test('draft identity includes API host, owner, batch and optional recipient scope; corrupt data is not silently replaced', async () => {
  const store = storage(); await lib.prepareUpload(new File(['x'], 'file.mov'), scope, '', '', store);
  assert.equal(lib.pendingUpload({ ...scope, ownerId: id }, store), null); assert.equal(lib.pendingUpload({ ...scope, studentId: id }, store), null);
  store.setItem(lib.uploadKey(scope), '{'); assert.throws(() => lib.pendingUpload(scope,store));
});
test('two owners sharing one browser and teaching scope cannot read or clear each other draft; API origin is isolated', async () => {
  const store = storage(); const secondScope = { ...scope, ownerId: id };
  const first = await lib.prepareUpload(new File(['A'], 'teacher-a.bin'), scope, 'A private title', 'A private comment', store);
  assert.equal(lib.pendingUpload(secondScope, store), null);
  const second = await lib.prepareUpload(new File(['B'], 'teacher-b.bin'), secondScope, 'B title', '', store);
  assert.equal(lib.pendingUpload(scope, store).requestId, first.requestId);
  assert.equal(lib.pendingUpload(secondScope, store).requestId, second.requestId);
  const otherOrigin = moduleFrom('class-media-upload', { apiUrl: 'http://127.0.0.1:49002', academyApi() { throw Error('No transport expected'); } });
  assert.equal(otherOrigin.pendingUpload(scope, store), null);
  lib.forgetUpload(secondScope, store);
  assert.equal(lib.pendingUpload(secondScope, store), null);
  assert.equal(lib.pendingUpload(scope, store).title, 'A private title');
});
test('actual Teacher sign-out clears only Teacher credentials; scoped draft stays recoverable only for its owner', async () => {
  const source = ts.transpileModule(fs.readFileSync(path.join(root,'api.ts'),'utf8'), {compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
  const store = storage(); const job = await lib.prepareUpload(new File(['A'], 'owner-a.bin'), scope, 'Private A', '', store);
  const module = {exports:{}}; let httpCalls = 0;
  new Function('module','exports','window','fetch','process',source)(module,module.exports,
    {localStorage:store,location:{pathname:'/teacher'}}, () => { httpCalls++; throw Error('Sign-out is local, no HTTP expected'); }, {env:{NEXT_PUBLIC_API_URL:apiUrl}});
  module.exports.savePortalTokens('Teacher', 'synthetic-A', 'synthetic-A-refresh');
  module.exports.savePortalTokens('Portal', 'synthetic-family', 'synthetic-family-refresh');
  module.exports.clearPortalTokens();
  assert.equal(module.exports.portalAccessToken(), null);
  assert.equal(store.getItem('academydesk.refreshToken.Teacher'), null);
  assert.equal(module.exports.portalAccessToken('Portal'), 'synthetic-family');
  module.exports.savePortalTokens('Teacher', 'synthetic-B', 'synthetic-B-refresh');
  assert.equal(lib.pendingUpload({ ...scope, ownerId: id }, store), null);
  assert.equal(lib.pendingUpload(scope, store).requestId, job.requestId);
  assert.equal(httpCalls, 0);
});
test('recording extension follows actual selected container, unknown format stays bin', () => {
  assert.equal(lib.recordingExtension('audio/mp4;codecs=mp4a.40.2'),'m4a'); assert.equal(lib.recordingExtension('audio/ogg;codecs=opus'),'ogg'); assert.equal(lib.recordingExtension('audio/webm'),'webm'); assert.equal(lib.recordingExtension('unknown'),'bin');
});
test('private routes cannot redirect bearer requests to arbitrary origins/paths', () => {
  const path = `/api/class-media/${id}/content`; assert.equal(material.privateMaterialPath(path),path); assert.equal(material.privateMaterialPath(apiUrl+path),path);
  for (const url of ['https://evil.invalid'+path, path+'?token=x', '//evil.invalid'+path, '/api/auth/login', 'javascript:alert(1)']) assert.equal(material.privateMaterialPath(url),null);
  assert.equal(material.externalMaterialUrl('javascript:alert(1)'),null);
});
test('safe preview selection uses magic bytes, never filename/MIME for active content', () => {
  assert.equal(material.previewMime(new Uint8Array([255,216,255])), 'image/jpeg');
  for (const text of ['<html>test</html>', '<svg></svg>', '%PDF-1.7', 'document']) assert.equal(material.previewMime(new TextEncoder().encode(text)),null);
});
test('bounded download rejects large content without requesting bytes', async () => {
  let called = false; await assert.rejects(material.boundedMaterialBlob(`/api/class-media/${id}/content`, 2_000_000_000, material.BUFFERED_DOWNLOAD_BYTES, new AbortController().signal, async () => { called=true; }), /too large/); assert.equal(called,false);
});
test('preview boundary permits exactly 32 MiB and directs larger files to the actual Download action before HTTP', async () => {
  const contentPath = `/api/class-media/${id}/content`; const signal = new AbortController().signal;
  let called = 0;
  await assert.rejects(material.boundedMaterialBlob(contentPath, material.PREVIEW_BYTES + 1, material.PREVIEW_BYTES, signal, async () => { called++; }), /Select Download to save the original file/);
  assert.equal(called, 0);
  // Empty transport deliberately rejects length later; reaching it proves inclusive size boundary without a 32 MiB allocation.
  await assert.rejects(material.boundedMaterialBlob(contentPath, material.PREVIEW_BYTES, material.PREVIEW_BYTES, signal, async () => { called++; return new Response(new Uint8Array()); }), /interrupted/);
  assert.equal(called, 1);
  const componentSource = fs.readFileSync(path.resolve(root, '../components/teacher-material-uploader.tsx'), 'utf8');
  assert.match(componentSource, /file\.size > PREVIEW_BYTES \? "Files over 32 MB/);
  assert.doesNotMatch(componentSource, /streaming Save/);
});
test('bounded download compares exact length, cancels oversize/truncated streams', async () => {
  const path = `/api/class-media/${id}/content`; const signal = new AbortController().signal;
  const blob = await material.boundedMaterialBlob(path,3,10,signal,async () => new Response(new Uint8Array([1,2,3]))); assert.equal(blob.size,3);
  await assert.rejects(material.boundedMaterialBlob(path,2,10,signal,async () => new Response(new Uint8Array([1,2,3]))),/exceeded/);
  await assert.rejects(material.boundedMaterialBlob(path,4,10,signal,async () => new Response(new Uint8Array([1,2,3]))),/interrupted/);
});
test('private access denial is not interpreted as a downloaded file', async () => {
  await assert.rejects(material.requireMaterialResponse(new Response(null,{status:401})),/Sign in/);
  await assert.rejects(material.requireMaterialResponse(new Response(null,{status:404})),/no longer/);
});
test('authenticated HEAD verifies size and decodes a Unicode attachment filename', async () => {
  let request;
  const client=moduleFrom('private-material',{apiUrl,academyApi:async(path,init)=>{request={path,init};return new Response(null,{headers:{'Content-Length':'3','Content-Disposition':"attachment; filename*=UTF-8''%E0%AE%AA%E0%AE%BE%E0%AE%9F%E0%AE%AE%E0%AF%8D.mov"}});}});
  const info=await client.materialInfo(`/api/class-media/${id}/content`,new AbortController().signal);
  assert.equal(info.length,3); assert.equal(info.filename,'பாடம்.mov'); assert.equal(request.init.method,'HEAD'); assert.equal(request.init.redirect,'error');
});
test('actual shared transport sends current workspace bearer and refreshes a retryable Blob body', async () => {
  const source = ts.transpileModule(fs.readFileSync(path.join(root,'api.ts'),'utf8'), {compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
  const store=storage(); store.setItem('academydesk.accessToken.Teacher','synthetic-old'); store.setItem('academydesk.refreshToken.Teacher','synthetic-refresh');
  const calls=[]; const module={exports:{}}; const fakeWindow={localStorage:store,location:{pathname:'/teacher'}};
  const fetch=async (url,init)=>{ calls.push({url,init}); if(url.endsWith('/api/auth/refresh')) return json({accessToken:'synthetic-new'}); return calls.length===1?new Response(null,{status:401}):json({message:'saved'}); };
  new Function('module','exports','window','fetch','process',source)(module,module.exports,fakeWindow,fetch,{env:{NEXT_PUBLIC_API_URL:apiUrl}});
  const body=new Blob(['abc']); await module.exports.academyApi('/api/teacher/media-uploads/'+id+'/chunks/0',{method:'PUT',body,headers:{'Content-Type':'application/octet-stream'},redirect:'error'});
  assert.equal(calls[0].init.headers.Authorization,'Bearer synthetic-old'); assert.equal(calls[2].init.headers.Authorization,'Bearer synthetic-new'); assert.equal(calls[2].init.body,body);
  assert.ok(calls.every(call=>!call.url.includes('synthetic-')));
});
test('new client components prerender without accessing browser storage or creating file URLs', () => {
  const React = require('../../apps/web/node_modules/react'); const { renderToStaticMarkup } = require('../../apps/web/node_modules/react-dom/server');
  function component(name) {
    const source = ts.transpileModule(fs.readFileSync(path.resolve(root,'../components',name+'.tsx'),'utf8'), {compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022,jsx:ts.JsxEmit.ReactJSX}}).outputText;
    const module={exports:{}};
    new Function('require','module','exports',source)(id=>{
      if(id==='react') return React; if(id==='react/jsx-runtime') return require('../../apps/web/node_modules/react/jsx-runtime');
      if(id==='@/lib/class-media-upload') return lib; if(id==='@/lib/private-material') return material; if(id==='@/lib/api') return {academyApi(){throw Error('No server HTTP expected');}};
      throw Error('Unexpected component import '+id);
    },module,module.exports); return module.exports;
  }
  const upload=renderToStaticMarkup(React.createElement(component('teacher-material-uploader').TeacherMaterialUploader,{scope,onUploaded:async()=>{}}));
  assert.match(upload,/Attachment title \(optional\)/); assert.match(upload,/Choose class attachment/); assert.match(upload,/2 GB/); assert.match(upload,/disabled=""/);
  const actions=renderToStaticMarkup(React.createElement(component('private-material-actions').PrivateMaterialActions,{url:`/api/class-media/${id}/content`,title:'QA'}));
  assert.match(actions,/Preview/); assert.match(actions,/Download/); assert.doesNotMatch(actions,/Bearer|accessToken/);
});
