// Actual helper code with synthetic HTTP streams; no live storage/SQL acceptance.
const { test } = require('node:test');
const a = require('node:assert/strict');
const fs = require('node:fs');
const ts = require(process.env.QA_WEB_TYPESCRIPT || '../../apps/web/node_modules/typescript');
const source = ts.transpileModule(fs.readFileSync('apps/web/src/lib/private-material.ts', 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 }
}).outputText;
const origin = 'https://academy.example.invalid';
const path = '/api/class-media/33333333-3333-3333-3333-333333333333/content';
const ticketPath = '/api/class-material-downloads/tickets';
const ticket = { ticket: 'SyntheticScopedCredential', expiresInSeconds: 60 };
const bytes = new Uint8Array([1, 2, 3]);
function load(api = async () => { throw Error('Unexpected HTTP'); }) {
  const module = { exports: {} };
  new Function('require', 'module', 'exports', source)(name => {
    a.equal(name, './api'); return { apiUrl: origin, academyApi: api };
  }, module, module.exports);
  return module.exports;
}
const lib = load();
const signal = () => new AbortController().signal;
const json = (value = ticket, options = {}) => new Response(JSON.stringify(value), options);
function trackedResponse({ status = 200, headers = {}, value = bytes } = {}) {
  let cancelled = 0;
  const response = new Response(new ReadableStream({ start(c) { c.enqueue(value); }, pull(c) { c.close(); }, cancel() { cancelled++; } }, { highWaterMark: 0 }), { status, headers });
  return { response, cancelled: () => cancelled };
}

for (const length of [NaN, Infinity, -Infinity, 0, -1, 0.5, 2_000_000_001, '3'])
  test('invalid declared byte length fails before transport: ' + String(length), async () => {
    let calls = 0;
    await a.rejects(lib.boundedMaterialBlob(path, length, 10, signal(), async () => { calls++; return new Response(bytes); }));
    a.equal(calls, 0);
  });
for (const limit of [NaN, Infinity, -Infinity, 0, -1, 3.5, lib.BUFFERED_DOWNLOAD_BYTES + 1, '10'])
  test('invalid memory budget fails before transport: ' + String(limit), async () => {
    let calls = 0;
    await a.rejects(lib.boundedMaterialBlob(path, 3, limit, signal(), async () => { calls++; return new Response(bytes); }));
    a.equal(calls, 0);
  });
for (const kind of ['info', 'blob', 'ticket']) test('pre-aborted ' + kind + ' makes no HTTP request', async () => {
  const abort = new AbortController(); abort.abort(); let calls = 0;
  const api = async () => { calls++; return kind === 'info' ? new Response(null, { headers: { 'Content-Length': '3' } }) : kind === 'ticket' ? json() : new Response(bytes); };
  const client = load(api);
  await a.rejects(kind === 'info' ? client.materialInfo(path, abort.signal) : kind === 'ticket' ? client.nativeMaterialTicket(path, abort.signal) : client.boundedMaterialBlob(path, 3, 10, abort.signal));
  a.equal(calls, 0);
});
for (const length of ['', '0', '-3', '3.5', '1e2', '+3', '03', '0x10', '3bytes', '2000000001'])
  test('HEAD rejects noncanonical or invalid Content-Length ' + JSON.stringify(length), async () => {
    const client = load(async () => new Response(null, { headers: { 'Content-Length': length } }));
    await a.rejects(client.materialInfo(path, signal()));
  });
for (const kind of ['info', 'blob', 'ticket']) for (const partial of ['status', 'header'])
  test(kind + ' refuses an unsolicited partial response via ' + partial, async () => {
    const item = trackedResponse({ status: partial === 'status' ? 206 : 200,
      headers: { 'Content-Length': '3', ...(partial === 'header' ? { 'Content-Range': 'bytes 0-2/100' } : {}) },
      value: kind === 'ticket' ? new TextEncoder().encode(JSON.stringify(ticket)) : bytes });
    const client = load(async () => item.response);
    await a.rejects(kind === 'info' ? client.materialInfo(path, signal()) : kind === 'ticket' ? client.nativeMaterialTicket(path, signal()) : client.boundedMaterialBlob(path, 3, 10, signal()));
    a.equal(item.cancelled(), 1);
  });
test('GET verifies its declared size against the previously checked HEAD size', async () => {
  const item = trackedResponse({ headers: { 'Content-Length': '4' } });
  await a.rejects(lib.boundedMaterialBlob(path, 3, 10, signal(), async () => item.response));
  a.equal(item.cancelled(), 1);
});
test('a response from another source is refused and cancelled', async () => {
  for (const url of ['https://foreign.example.invalid' + path, origin + '/api/auth/session', origin + path + '?token=private']) {
    const item = trackedResponse(); Object.defineProperty(item.response, 'url', { value: url });
    await a.rejects(lib.boundedMaterialBlob(path, 3, 10, signal(), async () => item.response)); a.equal(item.cancelled(), 1);
  }
});
test('a followed redirect is refused even when its final URL matches', async () => {
  const item = trackedResponse(); Object.defineProperty(item.response, 'redirected', { value: true });
  await a.rejects(lib.boundedMaterialBlob(path, 3, 10, signal(), async () => item.response)); a.equal(item.cancelled(), 1);
});
for (const status of [401, 403, 404, 429, 500, 503]) test('HTTP ' + status + ' discards its body and retains the appropriate error', async () => {
  const item = trackedResponse({ status });
  await a.rejects(lib.requireMaterialResponse(item.response), status === 401 ? /Sign in/ : status === 403 ? /cannot access/ : status === 503 ? /storage/ : /file|Wait/);
  a.equal(item.cancelled(), 1);
});
test('cancellation after HEAD transport cannot return metadata', async () => {
  const abort = new AbortController();
  const client = load(async () => { abort.abort(); return new Response(null, { headers: { 'Content-Length': '3' } }); });
  await a.rejects(client.materialInfo(path, abort.signal), { name: 'AbortError' });
});
test('cancellation during response validation cannot return HEAD metadata', async () => {
  const abort = new AbortController();
  const client = load(async () => {
    queueMicrotask(() => queueMicrotask(() => abort.abort()));
    return new Response(null, { headers: { 'Content-Length': '3' } });
  });
  await a.rejects(client.materialInfo(path, abort.signal), { name: 'AbortError' });
});
test('cancellation on final EOF cannot return a completed Blob', async () => {
  const abort = new AbortController();
  const stream = new ReadableStream({ start(c) { c.enqueue(bytes); }, pull(c) { c.close(); abort.abort(); } }, { highWaterMark: 0 });
  await a.rejects(lib.boundedMaterialBlob(path, 3, 10, abort.signal, async () => new Response(stream)), { name: 'AbortError' });
});
test('cancellation while awaiting a chunk promptly releases the reader', async () => {
  const abort = new AbortController(); let controller, ready;
  const waiting = new Promise(resolve => { ready = resolve; });
  const stream = new ReadableStream({ start(c) { controller = c; }, pull() { ready(); } }, { highWaterMark: 0 });
  const pending = lib.boundedMaterialBlob(path, 3, 10, abort.signal, async () => new Response(stream)).then(() => 'success', e => e.name);
  await waiting; abort.abort();
  const observed = await Promise.race([pending, new Promise(resolve => setTimeout(() => resolve('pending'), 50))]);
  try { controller.close(); } catch { /* already cancelled */ }
  await pending;
  a.equal(observed, 'AbortError'); a.equal(stream.locked, false);
});
for (const declared of [false, true]) test('ticket JSON is byte-bounded ' + (declared ? 'before reading advertised overflow' : 'while streaming'), async () => {
  const value = ' '.repeat(8192) + JSON.stringify(ticket);
  const item = trackedResponse({ value: new TextEncoder().encode(value), headers: declared ? { 'Content-Length': String(value.length) } : {} });
  await a.rejects(lib.nativeMaterialTicket(path, signal(), async () => item.response)); a.equal(item.cancelled(), 1);
});
for (const key of ['actorId', 'academyId', 'accessToken']) test('ticket envelope rejects unrequested ' + key, async () =>
  a.rejects(lib.nativeMaterialTicket(path, signal(), async () => json({ ...ticket, [key]: 'synthetic-private' }))));
test('invalid ticket JSON never exposes its contents in the error', async () => {
  await a.rejects(lib.nativeMaterialTicket(path, signal(), async () => new Response('SyntheticPrivateCredential')), e => {
    a.doesNotMatch(e.message, /SyntheticPrivateCredential/); return true;
  });
});
test('valid HEAD metadata keeps Unicode filenames and bounded sanitization', async () => {
  const client = load(async (url, init) => {
    a.equal(url, path); a.equal(init.method, 'HEAD'); a.equal(init.redirect, 'error'); a.equal(init.cache, 'no-store');
    return new Response(null, { headers: { 'Content-Length': '3', 'Content-Disposition': "attachment; filename*=UTF-8''%E0%AE%AA%E0%AE%BE%E0%AE%9F%E0%AE%AE%E0%AF%8D.mov" } });
  });
  const info = await client.materialInfo(path, signal()); a.equal(info.length, 3); a.equal(info.filename, 'பாடம்.mov');
});
test('valid complete streamed bytes retain exact content and release the lock', async () => {
  const response = new Response(bytes, { headers: { 'Content-Length': '3' } });
  Object.defineProperty(response, 'url', { value: origin + path });
  const value = await lib.boundedMaterialBlob(path, 3, 10, signal(), async (url, init) => {
    a.equal(url, path); a.equal(init.redirect, 'error'); a.equal(init.cache, 'no-store'); return response;
  });
  a.deepEqual(new Uint8Array(await value.arrayBuffer()), bytes); a.equal(response.body.locked, false);
});
test('chunked file responses remain supported without a Content-Length header', async () => {
  const value = await lib.boundedMaterialBlob(path, 3, 10, signal(), async () => new Response(bytes)); a.equal(value.size, 3);
});
test('valid ticket remains an authenticated POST to the fixed endpoint', async () => {
  const value = await lib.nativeMaterialTicket(path, signal(), async (url, init) => {
    a.equal(url, ticketPath); a.equal(init.method, 'POST'); a.deepEqual(JSON.parse(init.body), { path });
    a.equal(init.redirect, 'error'); a.equal(init.cache, 'no-store'); return json();
  });
  a.equal(value, ticket.ticket);
});
test('maximum valid ticket length remains supported without trimming or rewriting', async () => {
  const value = { ...ticket, ticket: 'a'.repeat(3000) };
  a.equal(await lib.nativeMaterialTicket(path, signal(), async () => json(value)), value.ticket);
});
test('decompressed ticket response uses the decoded byte limit rather than compressed Content-Length', async () => {
  a.equal(await lib.nativeMaterialTicket(path, signal(), async () => json(ticket, { headers: { 'Content-Encoding': 'gzip', 'Content-Length': '12' } })), ticket.ticket);
});
test('oversize and truncated files still fail rather than claim completion', async () => {
  await a.rejects(lib.boundedMaterialBlob(path, 2, 10, signal(), async () => new Response(bytes)), /exceeded/);
  await a.rejects(lib.boundedMaterialBlob(path, 4, 10, signal(), async () => new Response(bytes)), /interrupted/);
});
test('malformed private paths are refused before fetching', async () => {
  for (const url of [path + '?token=secret', '//foreign.invalid' + path, '/api/auth/session']) {
    let calls = 0; await a.rejects(lib.nativeMaterialTicket(url, signal(), async () => { calls++; return json(); })); a.equal(calls, 0);
  }
});
