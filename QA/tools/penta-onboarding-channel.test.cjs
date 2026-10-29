// Synthetic client boundary plus owned loopback HTTP, not SQL/model/browser acceptance.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const http = require('node:http');
const ts = require('../../apps/web/node_modules/typescript');
const file = path.resolve(__dirname, '../../apps/web/src/lib/penta-onboarding-channel.ts');
const code = ts.transpileModule(fs.readFileSync(file, 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText;
const ids = ['11111111-1111-4111-8111-111111111111', '22222222-2222-4222-8222-222222222222',
  '33333333-3333-4333-8333-333333333333', '44444444-4444-4444-8444-444444444444'];
const binding = () => ({ academyId: ids[0], actorId: ids[1], conversationId: ids[2], draftId: ids[3],
  contextVersion: 1, authorityVersion: 1, expectedVersion: 2, draftRevision: 1, payloadHash: 'a'.repeat(64) });
const later = () => { let resolve; const promise = new Promise(r => { resolve = r; }); return { promise, resolve }; };
function fixture(b, requestId, kind = 'clarify', updated = true) {
  const revision = b.draftRevision + (updated ? 1 : 0);
  const op = `onboarding:${b.draftId.replaceAll('-', '')}:${revision}`;
  return { conversationId: b.conversationId, requestId, version: b.expectedVersion + 1,
    draftId: b.draftId, draftRevision: revision, kind,
    status: updated ? 'draft_updated' : kind === 'clarify' ? 'clarification_required' : kind,
    snapshot: { version: 'operational-draft-0.3.0', availability: 'ready',
      scope: { tenant_id: b.academyId, actor_id: b.actorId, conversation_id: b.conversationId,
        connection_id: `academydesk-onboarding:${b.draftId.replaceAll('-', '')}`,
        authority_version: b.authorityVersion, context_version: b.contextVersion },
      draft: { ref: b.draftId, revision, tool_key: 'AcademyDesk.StudentOnboarding.Create',
        status: { host_prepare: 'awaiting_approval', request_pause: 'paused', request_cancel: 'cancelled' }[kind] ?? 'collecting',
        operation_ref: kind === 'host_prepare' ? op : null, fields: { private_note: 'must not escape' } } },
    operations: kind === 'host_prepare' ? [{ ref: op, request_ref: requestId, draft_ref: b.draftId,
      draft_revision: revision, tool_key: 'AcademyDesk.StudentOnboarding.Create', tool_revision: 1,
      approval_ref: null, status: 'awaiting_approval' }] : [], questions: kind === 'clarify' ? ['student_name'] : [], canExecute: false };
}
function harness(request, origin = 'http://127.0.0.1:1') {
  let token = 'synthetic-session', loginEpoch = 'synthetic-original-login';
  const listeners = new Map();
  const window = { location: { pathname: '/penta' }, addEventListener(name, fn) { if (!listeners.has(name)) listeners.set(name, new Set()); listeners.get(name).add(fn); },
    removeEventListener(name, fn) { listeners.get(name)?.delete(fn); } };
  const api = { apiUrl: origin, portalSignOutEvent: 'sign-out', portalSessionChangedEvent:'session-changed',portalSessionEpoch:()=>loginEpoch,portalWorkspaceForPath(p) {
    return p.startsWith('/teacher') ? 'Teacher' : p.startsWith('/portal') ? 'Portal' : p.startsWith('/platform') ? 'Platform' : 'AcademyAdmin';
  }, portalAccessToken(workspace) {
    assert.equal(workspace, 'AcademyAdmin'); return token;
  }, isPortalSignOutEvent(event, workspace) { return event.workspace === workspace; } };
  const module = { exports: {} };
  vm.runInNewContext(code, { module, exports: module.exports, require(name) { assert.equal(name, './api'); return api; },
    window, fetch: request, crypto, AbortController, TextDecoder, Uint8Array });
  return { create: module.exports.createOnboardingTurnChannel, setToken(value) { token = value; },
    replaceEpoch() { loginEpoch = 'synthetic-replacement-login'; },
    setPath(value) { window.location.pathname = value; },
    emit(name, event) { for (const fn of listeners.get(name) ?? []) fn(event); },
    listenerCount: () => [...listeners.values()].reduce((n, set) => n + set.size, 0) };
}
const json = (value, status = 201, headers = {}) => new Response(JSON.stringify(value), {
  status, headers: { 'Content-Type': 'application/json', ...headers },
});
const results = [];
async function test(name, run) {
  try { await run(); results.push({ name, pass: true }); }
  catch (error) { results.push({ name, pass: false, type: error.name }); console.error(`FAIL ${name}: ${error.name}`); }
}
(async () => {
  for (const [kind, updated] of [['host_prepare', true], ['clarify', true], ['clarify', false],
    ['unsupported', false], ['request_pause', true], ['request_resume', true], ['request_cancel', true],
    ['unavailable', false], ['invalid_response', false]]) {
    await test(`bound ${kind}/${updated} never reports domain success`, async () => {
      const b = binding(), calls = [];
      const h = harness(async (url, init) => { calls.push({ url, init }); return json(fixture(b, JSON.parse(init.body).requestId, kind, updated)); });
      const channel = h.create(b);
      try {
        const output = await channel.send('Prepare a draft, not an execution.');
        assert.equal(output.kind, kind); assert.equal(output.domainEffect, false); assert.equal(output.canExecute, false);
        assert.equal(output.snapshot, undefined); assert.equal(output.operations, undefined);
        assert.equal(JSON.stringify(output).includes('must not escape'), false);
        assert.equal(calls.length, 1); assert.equal(calls[0].init.redirect, 'error');
        assert.equal(calls[0].init.cache, 'no-store'); assert.equal(calls[0].init.credentials, 'omit');
        assert.deepEqual(Object.keys(JSON.parse(calls[0].init.body)).sort(),
          ['requestId', 'expectedVersion', 'draftRevision', 'payloadHash', 'text'].sort());
        await assert.rejects(channel.send('Try again')); assert.equal(calls.length, 1);
      } finally { channel.dispose(); }
      assert.equal(h.listenerCount(), 0);
    });
  }
  const corruptions = {
    'foreign conversation': v => { v.conversationId = ids[0]; },
    'foreign request': v => { v.requestId = ids[0]; },
    'stale version': v => { v.version--; },
    'foreign draft': v => { v.draftId = ids[0]; },
    'wrong revision': v => { v.draftRevision++; },
    'execution grant': v => { v.canExecute = true; },
    'fabricated completion': v => { v.kind = 'succeeded'; v.status = 'succeeded'; },
    'foreign academy': v => { v.snapshot.scope.tenant_id = ids[1]; },
    'foreign actor': v => { v.snapshot.scope.actor_id = ids[0]; },
    'foreign connection': v => { v.snapshot.scope.connection_id = 'other'; },
    'foreign snapshot conversation': v => { v.snapshot.scope.conversation_id = ids[0]; },
    'changed authority': v => { v.snapshot.scope.authority_version++; },
    'changed context': v => { v.snapshot.scope.context_version++; },
    'unavailable snapshot': v => { v.snapshot.availability = 'denied'; },
    'missing snapshot': v => { v.snapshot = null; },
    'wrong snapshot draft': v => { v.snapshot.draft.ref = ids[0]; },
    'wrong snapshot revision': v => { v.snapshot.draft.revision++; },
    'wrong tool': v => { v.snapshot.draft.tool_key = 'GrantPermission'; },
    'non-prepare operation': v => { v.operations.push({}); },
    'duplicate questions': v => { v.questions.push(v.questions[0]); },
    'unknown kind prototype': v => { v.kind = '__proto__'; },
  };
  for (const [name, change] of Object.entries(corruptions)) await test(`reject ${name}`, async () => {
    const b = binding(); let calls = 0;
    const h = harness(async (_, init) => { calls++; const v = fixture(b, JSON.parse(init.body).requestId); change(v); return json(v); });
    const channel = h.create(b);
    try { await assert.rejects(channel.send('Draft')); await assert.rejects(channel.send('Again')); assert.equal(calls, 1); }
    finally { channel.dispose(); }
  });
  for (const status of [400, 401, 403, 404, 409, 410, 429, 500, 503]) await test(`HTTP ${status} never refreshes/replays`, async () => {
    let calls = 0; const h = harness(async () => { calls++; return json({ message: 'private backend diagnostic' }, status); });
    const channel = h.create(binding());
    try { await assert.rejects(channel.send('Draft'), error => !error.message.includes('private backend'));
      await assert.rejects(channel.send('Again')); assert.equal(calls, 1); assert.ok(channel.requestId); }
    finally { channel.dispose(); }
  });
  for (const [name, change] of Object.entries({
    'wrong operation reference': v => { v.operations[0].ref = 'other'; },
    'wrong operation request': v => { v.operations[0].request_ref = ids[0]; },
    'wrong operation draft': v => { v.operations[0].draft_ref = ids[0]; },
    'wrong operation revision': v => { v.operations[0].draft_revision++; },
    'wrong operation tool': v => { v.operations[0].tool_key = 'GrantRole'; },
    'wrong operation tool revision': v => { v.operations[0].tool_revision++; },
    'unsolicited approval': v => { v.operations[0].approval_ref = 'fabricated'; },
    'operation already succeeded': v => { v.operations[0].status = 'succeeded'; },
    'snapshot operation mismatch': v => { v.snapshot.draft.operation_ref = 'other'; },
    'snapshot status mismatch': v => { v.snapshot.draft.status = 'approved'; },
  })) await test(`reject ${name}`, async () => {
    const b = binding(), h = harness(async (_, init) => {
      const v = fixture(b, JSON.parse(init.body).requestId, 'host_prepare'); change(v); return json(v);
    });
    const channel = h.create(b);
    try { await assert.rejects(channel.send('Draft')); } finally { channel.dispose(); }
  });
  for (const [name, change] of Object.entries({
    'foreign pending request': v => { v.requestId = ids[0]; },
    'foreign pending conversation': v => { v.conversationId = ids[0]; },
    'stale pending version': v => { v.version--; },
    'pending with execution grant': v => { v.canExecute = true; },
    'pending with snapshot': v => { v.snapshot = {}; },
    'false pending success': v => { v.status = 'succeeded'; },
  })) await test(`reject ${name}`, async () => {
    const b = binding(), h = harness(async (url, init) => {
      if (init.method === 'POST') throw Error();
      const v = { conversationId: b.conversationId, requestId: url.match(/claims\/([^/]+)\/result$/)[1],
        version: 3, status: 'pending', canExecute: false }; change(v); return json(v, 202);
    });
    const channel = h.create(b);
    try { await assert.rejects(channel.send('Draft')); await assert.rejects(channel.reconcile()); }
    finally { channel.dispose(); }
  });
  for (const scenario of ['malformed JSON', 'oversized header', 'oversized stream', 'wrong content type'])
    await test(`reject ${scenario}`, async () => {
      const h = harness(async () => scenario === 'malformed JSON' ? new Response('{', { status: 201, headers: { 'Content-Type': 'application/json' } }) :
        scenario === 'wrong content type' ? new Response('{}', { status: 201 }) :
        json({ text: scenario === 'oversized stream' ? 'x'.repeat(65537) : '' }, 201,
          scenario === 'oversized header' ? { 'Content-Length': '65537' } : {}));
      const channel = h.create(binding());
      try { await assert.rejects(channel.send('Draft')); await assert.rejects(channel.send('Again')); }
      finally { channel.dispose(); }
    });
  await test('transport interruption reconciles same ID with GET only', async () => {
    const b = binding(), calls = []; let step = 0;
    const h = harness(async (url, init) => {
      calls.push({ url, method: init.method });
      if (++step === 1) throw new Error('private socket failure');
      const requestId = url.match(/claims\/([^/]+)\/result$/)[1];
      return step === 2 ? json({ conversationId: b.conversationId, requestId, version: 3, status: 'pending', canExecute: false }, 202) :
        json(fixture(b, requestId), 200);
    });
    const channel = h.create(b);
    try {
      await assert.rejects(channel.reconcile()); assert.equal(calls.length, 0);
      await assert.rejects(channel.send('Draft')); const admitted = channel.requestId;
      assert.equal((await channel.reconcile()).kind, 'pending');
      assert.equal((await channel.reconcile()).status, 'draft_updated');
      assert.equal(channel.requestId, admitted); assert.deepEqual(calls.map(c => c.method), ['POST', 'GET', 'GET']);
      await assert.rejects(channel.send('Again')); assert.equal(calls.length, 3);
    } finally { channel.dispose(); }
  });
  for (const scenario of ['abort', 'new token', 'local logout', 'storage logout', 'dispose'])
    await test(`late result discarded after ${scenario}`, async () => {
      const b = binding(), entered = later(), release = later(); let calls = 0;
      const h = harness(async (_, init) => { calls++; entered.resolve(); await release.promise;
        return json(fixture(b, JSON.parse(init.body).requestId)); });
      const channel = h.create(b), abort = new AbortController();
      try {
        const output = channel.send('Draft', abort.signal); await entered.promise;
        await assert.rejects(channel.send('Double click')); assert.equal(calls, 1);
        if (scenario === 'abort') abort.abort();
        if (scenario === 'new token') h.setToken('replacement-session');
        if (scenario === 'local logout') { h.emit('sign-out', { detail: 'AcademyAdmin' }); h.setToken('synthetic-session'); }
        if (scenario === 'storage logout') { h.emit('storage', { workspace: 'AcademyAdmin' }); h.setToken('synthetic-session'); }
        if (scenario === 'dispose') channel.dispose();
        release.resolve(); await assert.rejects(output); await assert.rejects(channel.send('Again')); assert.equal(calls, 1);
      } finally { channel.dispose(); }
    });
  await test('invalid input and pre-abort dispatch nothing', async () => {
    let calls = 0; const h = harness(async () => { calls++; throw Error(); });
    for (const change of [b => { b.academyId = '../foreign'; }, b => { b.draftId = '0'.repeat(36); },
      b => { b.expectedVersion = 100; }, b => { b.draftRevision = 0; }, b => { b.payloadHash = 'bad'; }]) {
      const b = binding(); change(b); assert.throws(() => h.create(b));
    }
    const channel = h.create(binding()), abort = new AbortController(); abort.abort();
    try { for (const text of ['', ' ', 'x'.repeat(2001), 'bad\u0000']) await assert.rejects(channel.send(text));
      await assert.rejects(channel.send('Draft', abort.signal)); assert.equal(calls, 0); assert.equal(channel.requestId, null); }
    finally { channel.dispose(); }
  });
  await test('other portal logout does not borrow or invalidate Admin credentials', async () => {
    const b = binding(), h = harness(async (_, init) => json(fixture(b, JSON.parse(init.body).requestId)));
    const channel = h.create(b);
    try {
      h.emit('sign-out', { detail: 'Teacher' }); h.emit('storage', { workspace: 'Teacher' });
      assert.equal((await channel.send('Draft')).status, 'draft_updated');
    } finally { channel.dispose(); }
  });
  await test('caller cannot mutate a pinned binding while request is in flight', async () => {
    const b = binding(), original = binding(), entered = later(), release = later();
    const h = harness(async (_, init) => { entered.resolve(); await release.promise;
      const input = JSON.parse(init.body); assert.equal(input.payloadHash, original.payloadHash);
      return json(fixture(original, input.requestId)); });
    const channel = h.create(b);
    try {
      const sent = channel.send('Draft'); await entered.promise;
      b.academyId = ids[1]; b.expectedVersion++; b.payloadHash = 'b'.repeat(64); release.resolve();
      assert.equal((await sent).version, original.expectedVersion + 1);
    } finally { channel.dispose(); }
  });
  await test('host pending cursor hydrates GET-only recovery after reload', async () => {
    const b = { ...binding(), pendingRequestId: ids[0] }, calls = [];
    const h = harness(async (url, init) => { calls.push({ url, method: init.method });
      return json(fixture(b, b.pendingRequestId), 200); });
    const channel = h.create(b);
    try {
      await assert.rejects(channel.send('Must not replay')); assert.equal(calls.length, 0);
      assert.equal((await channel.reconcile()).requestId, b.pendingRequestId);
      assert.deepEqual(calls.map(c => c.method), ['GET']);
      assert.equal(calls[0].url.endsWith(`/claims/${b.pendingRequestId}/result`), true);
      assert.throws(() => h.create({ ...binding(), pendingRequestId: '../other' }));
    } finally { channel.dispose(); }
  });
  await test('other portal cannot borrow an existing Admin token', async () => {
    let calls = 0; const h = harness(async () => { calls++; throw Error(); });
    for (const p of ['/teacher', '/portal/penta', '/platform/penta']) {
      h.setPath(p); assert.throws(() => h.create(binding()));
    }
    assert.equal(calls, 0); assert.equal(h.listenerCount(), 0);
  });
  await test('navigation to another portal discards late Admin response', async () => {
    const b = binding(), entered = later(), release = later();
    const h = harness(async (_, init) => { entered.resolve(); await release.promise;
      return json(fixture(b, JSON.parse(init.body).requestId)); });
    const channel = h.create(b);
    try {
      const output = channel.send('Draft'); await entered.promise; h.setPath('/teacher');
      release.resolve(); await assert.rejects(output); await assert.rejects(channel.reconcile());
    } finally { channel.dispose(); }
  });
  await test('native HTTP uses one POST then bound status GET', async () => {
    const b = binding(), calls = []; let admitted;
    const server = http.createServer(async (req, res) => {
      const chunks = []; for await (const chunk of req) chunks.push(chunk);
      calls.push({ method: req.method, path: req.url, authenticated: req.headers.authorization === 'Bearer synthetic-session' });
      res.setHeader('Content-Type', 'application/json');
      if (req.method === 'POST') { admitted = JSON.parse(Buffer.concat(chunks)).requestId; res.statusCode = 503; res.end('{}'); }
      else { res.statusCode = 200; res.end(JSON.stringify(fixture(b, admitted))); }
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const h = harness(fetch, `http://127.0.0.1:${server.address().port}`), channel = h.create(b);
    try {
      await assert.rejects(channel.send('Draft')); assert.equal((await channel.reconcile()).status, 'draft_updated');
      assert.deepEqual(calls.map(c => c.method), ['POST', 'GET']); assert.ok(calls.every(c => c.authenticated));
      assert.equal(calls[1].path.endsWith(`/claims/${admitted}/result`), true);
    } finally { channel.dispose(); await new Promise(resolve => server.close(resolve)); }
  });
  await test('missed identical-token replacement blocks draft send',async()=>{
    let calls=0;const h=harness(async()=>{calls++;throw Error('Unexpected request');}),channel=h.create(binding());
    h.replaceEpoch();await assert.rejects(channel.send('Draft'));await assert.rejects(channel.reconcile());assert.equal(calls,0);channel.dispose();assert.equal(h.listenerCount(),0);
  });
  await test('late identical-token replacement withholds turn result and recovery',async()=>{
    const b=binding(),entered=later(),release=later();let calls=0;
    const h=harness(async(_,init)=>{calls++;entered.resolve();await release.promise;return json(fixture(b,JSON.parse(init.body).requestId));}),channel=h.create(b);
    const pending=channel.send('Draft');await entered.promise;h.replaceEpoch();release.resolve();await assert.rejects(pending);await assert.rejects(channel.reconcile());assert.equal(calls,1);channel.dispose();
  });
  await test('same-document replacement aborts a pending turn without resubmitting',async()=>{
    const b=binding(),entered=later(),release=later();let calls=0,signal;
    const h=harness(async(_,init)=>{calls++;signal=init.signal;entered.resolve();await release.promise;return json(fixture(b,JSON.parse(init.body).requestId));}),channel=h.create(b);
    const pending=channel.send('Draft');await entered.promise;h.replaceEpoch();h.emit('session-changed',{detail:'AcademyAdmin'});assert.equal(signal.aborted,true);
    release.resolve();await assert.rejects(pending);await assert.rejects(channel.reconcile());assert.equal(calls,1);channel.dispose();assert.equal(h.listenerCount(),0);
  });
  await test('another workspace replacement leaves Admin draft usable',async()=>{
    const b=binding();let calls=0;const h=harness(async(_,init)=>{calls++;return json(fixture(b,JSON.parse(init.body).requestId));}),channel=h.create(b);
    h.emit('session-changed',{detail:'Teacher'});assert.equal((await channel.send('Draft')).kind,'clarify');assert.equal(calls,1);channel.dispose();
  });
  const report = { kind: 'synthetic-client-and-loopback', passed: results.filter(r => r.pass).length,
    failed: results.filter(r => !r.pass).length, total: results.length, modelInference: false,
    sqlAcceptance: false, browserAcceptance: false, observations: results };
  console.log(JSON.stringify(report));
  const out = process.env.QA_PENTA_CHANNEL_OUTPUT;
  if (out) { assert.ok(!fs.existsSync(out), 'Preserve previous receipts'); fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, JSON.stringify(report, null, 2)); }
  if (report.failed) process.exitCode = 1;
})().catch(error => { console.error(`Channel test runner failed: ${error.name}`); process.exitCode = 1; });
