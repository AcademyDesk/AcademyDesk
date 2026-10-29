// Synthetic host responses / exact vendored validator. No SQL/model/browser acceptance.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const a = require('node:assert/strict'), crypto = require('node:crypto');
const ts = require('../../apps/web/node_modules/typescript');
const root = path.resolve(__dirname, '../..');
const conversation = '11111111-1111-4111-8111-111111111111', draft = '22222222-2222-4222-8222-222222222222';
const academy = '33333333-3333-4333-8333-333333333333', actor = '44444444-4444-4444-8444-444444444444';
const epoch = '55555555-5555-4555-8555-555555555555';
const tool = 'AcademyDesk.StudentOnboarding.Create';
function fixture() {
  const fields = { StudentFirstName: { kind: 'text', value: 'SYNTHETIC-REVIEW-ONLY' } };
  const canonicalPayload = JSON.stringify({ tool_key: tool, tool_revision: 1, fields });
  const snapshot = { version: 'operational-draft-0.3.0', scope: { tenant_id: academy, actor_id: actor,
    conversation_id: conversation, connection_id: `academydesk-onboarding:${draft.replaceAll('-', '')}`,
    authority_version: 1, context_version: 1 }, as_of: '2026-10-18T10:00:00Z', catalog_revision: 1,
    tools: [{ key: tool, revision: 1, family: 'AI-04', capability: ['Executor'], description: 'Synthetic host draft',
      effect: 'write', fields: [{ key: 'StudentFirstName', kind: 'text', label: 'Student first name',
        required: true, clearable: false, choices: [], columns: [], max_length: 120 }],
      preparation_required: true, approval_required: true, max_items: 1 }],
    draft: { ref: draft, revision: 1, tool_key: tool, fields, status: 'collecting', operation_ref: null },
    evidence: [], availability: 'ready' };
  return { account: { academyId: academy, roles: ['AcademyAdmin'], isPlatformOwner: false,
      workspace: 'AcademyAdmin', displayName: 'Synthetic reviewer' },
    current: { conversationId: conversation, version: 1, currentDraftId: draft, draftRevision: 1,
      pendingRequestId: null, status: 'selected', snapshot, canExecute: false },
    view: { id: draft, sessionId: conversation, contextVersion: 0, revision: 1, status: 'AwaitingReview',
      expiresAtUtc: '2099-10-18T12:00:00Z', canonicalPayload,
      payloadHash: crypto.createHash('sha256').update(canonicalPayload).digest('hex'), readyForReview: false, canExecute: false } };
}
function harness(f = fixture(), options = {}) {
  let token = 'synthetic-review-session', loginEpoch = 'synthetic-original-login'; const calls = [], modules = new Map();
  const window = Object.assign(new EventTarget(), { location: { pathname: '/penta/onboarding' } });
  const api = { apiUrl: 'http://127.0.0.1:1', portalAccessToken(workspace) { a.equal(workspace, 'AcademyAdmin'); return token; },
    portalSessionEpoch(workspace) { a.equal(workspace, 'AcademyAdmin'); return loginEpoch; },
    portalSessionChangedEvent:'academydesk:portal-session-changed',
    portalWorkspaceForPath: p => p.startsWith('/teacher') ? 'Teacher' : 'AcademyAdmin',
    portalSignOutEvent:'academydesk:portal-sign-out', isPortalSignOutEvent:event => event.key === 'academydesk.accessToken.AcademyAdmin' && event.newValue === null };
  function load(file) {
    file = path.resolve(file);
    if (modules.has(file)) return modules.get(file).exports;
    if (file.endsWith('.json')) return JSON.parse(fs.readFileSync(file, 'utf8'));
    const mod = { exports: {} }; modules.set(file, mod);
    const src = fs.readFileSync(file, 'utf8');
    const js = ts.transpileModule(src, { compilerOptions: { module: ts.ModuleKind.CommonJS,
      target: ts.ScriptTarget.ES2022, esModuleInterop: true, resolveJsonModule: true } }).outputText;
    vm.runInNewContext(js, { module: mod, exports: mod.exports, require(name) {
      if (name === './api') return api;
      let target = path.resolve(path.dirname(file), name); if (!path.extname(target)) target += '.ts';
      a.ok(target.startsWith(path.join(root, 'apps/web/src') + path.sep)); return load(target);
    }, window, Date: options.Date ?? Date, crypto: crypto.webcrypto, TextEncoder, TextDecoder, Uint8Array, AbortController,
      fetch: async () => { throw Error('Unexpected default fetch'); } });
    return mod.exports;
  }
  const lib = load(path.join(root, 'apps/web/src/lib/penta-onboarding-review.ts'));
  async function request(url, init) {
    calls.push({ url, init }); const number = calls.length;
    if (options.onRequest) await options.onRequest(number, { replaceToken: () => { token = 'replacement'; }, replaceEpoch: () => { loginEpoch = 'synthetic-replacement-login'; }, window });
    if (options.respond) return options.respond(number, url, init);
    let status = options.statusAt === number ? options.status : 200;
    let value = number === 1 ? f.account : number === 3 ? f.view : f.current;
    if (number === 4 && options.after) value = options.after;
    if (options.rawAt === number) return new Response('{', { status, headers: { 'Content-Type': 'application/json' } });
    return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } });
  }
  return { lib, f, calls, window, request, replaceEpoch: () => { loginEpoch = 'synthetic-replacement-login'; }, setEpoch: value => { loginEpoch = value; }, operation: () => load(path.join(root, 'apps/web/src/lib/penta-onboarding-operation.ts')),
    dialogue: () => load(path.join(root, 'apps/web/src/lib/penta-onboarding-dialogue.ts')), run: (signal = new AbortController().signal) =>
    lib.loadOnboardingReview(conversation, draft, epoch, 1, signal, request) };
}
const observations = [];
async function check(name, run) { try { await run(); observations.push({ name, pass: true }); }
  catch (e) { observations.push({ name, pass: false, type: e.name }); console.error(`FAIL ${name}: ${e.name}`); } }
module.exports = { fixture, harness, conversation, draft, academy, actor, epoch };
if (require.main === module) (async () => {
  await check('all twelve immutable vendor files match their pinned normalized hashes', async () => {
    const base = path.join(root, 'apps/web/src/vendor/penta-operational');
    const upstream = JSON.parse(fs.readFileSync(path.join(base, 'UPSTREAM.json'), 'utf8'));
    a.equal(upstream.files.length, 12);
    for (const row of upstream.files) {
      const content = fs.readFileSync(path.join(base, row.path), 'utf8').replaceAll('\r\n', '\n');
      a.equal(crypto.createHash('sha256').update(content).digest('hex'), row.normalizedSha256);
    }
    a.equal(upstream.activation, false);
  });
  await check('four GETs create passive validated shared projection with no usable actions', async () => {
    const h = harness(), p = await h.run();
    a.equal(p.host.draft.fields.StudentFirstName.value, 'SYNTHETIC-REVIEW-ONLY');
    a.ok(Object.values(p.controls).every(v => v === false)); a.ok(p.capabilities.every(c => c.available === false));
    a.equal(p.models.length, 0); a.equal(p.operation, null); a.equal(p.receipt, null);
    a.equal(h.calls.length, 4); a.ok(h.calls.every(c => c.init.method === 'GET' && c.init.cache === 'no-store' && c.init.redirect === 'error' && c.init.credentials === 'omit'));
    a.ok(h.calls.slice(1).every(c => c.url.includes(`/academies/${academy}/`)));
    a.equal('email' in p, false);
  });
  const changes = {
    'Teacher account': f => { f.account.roles = ['Teacher']; },
    'Student account': f => { f.account.roles = ['Student']; },
    'Platform owner': f => { f.account.isPlatformOwner = true; },
    'wrong workspace': f => { f.account.workspace = 'Teacher'; },
    'unknown academy': f => { f.account.academyId = '../other'; },
    'foreign current conversation': f => { f.current.conversationId = actor; },
    'foreign selected draft': f => { f.current.currentDraftId = actor; },
    'pending current': f => { f.current.status = 'pending'; f.current.pendingRequestId = epoch; f.current.snapshot = null; },
    'current grants execute': f => { f.current.canExecute = true; },
    'foreign draft view': f => { f.view.id = actor; },
    'foreign draft session': f => { f.view.sessionId = actor; },
    'stale draft view revision': f => { f.view.revision++; },
    'draft view grants execute': f => { f.view.canExecute = true; },
    'expired draft': f => { f.view.expiresAtUtc = '2000-01-01T00:00:00Z'; },
    'wrong digest': f => { f.view.payloadHash = 'a'.repeat(64); },
    'malformed canonical JSON': f => { f.view.canonicalPayload = '{'; },
    'foreign tenant snapshot': f => { f.current.snapshot.scope.tenant_id = actor; },
    'wrong connection': f => { f.current.snapshot.scope.connection_id = 'foreign'; },
    'wrong context': f => { f.current.snapshot.scope.context_version++; },
    'wrong authority': f => { f.current.snapshot.scope.authority_version++; },
    'wrong snapshot draft revision': f => { f.current.snapshot.draft.revision++; },
    'unknown tool': f => { f.current.snapshot.tools[0].key = 'GrantRole'; },
    'approval safeguard absent': f => { f.current.snapshot.tools[0].approval_required = false; },
    'foreign family': f => { f.current.snapshot.tools[0].family = 'AI-07'; },
    'unknown field': f => { f.current.snapshot.draft.fields.Unknown = { kind: 'text', value: 'bad' }; },
    'overlong field': f => { f.current.snapshot.draft.fields.StudentFirstName.value = 'x'.repeat(121); },
    'unknown snapshot property': f => { f.current.snapshot.permissions = ['owner']; },
    'succeeded masquerades as review': f => { f.view.status = 'Succeeded'; },
  };
  for (const [name, mutate] of Object.entries(changes)) await check(`withhold ${name}`, async () => {
    const f = fixture(); mutate(f); const h = harness(f); await a.rejects(h.run()); a.ok(h.calls.every(c => c.init.method === 'GET'));
  });
  await check('medical input never enters review projection even with a valid digest', async () => {
    const f = fixture(), payload = JSON.parse(f.view.canonicalPayload);
    payload.fields.MedicalOrAccessibilityNotes = { kind: 'text', value: 'SYNTHETIC-SENSITIVE' };
    f.view.canonicalPayload = JSON.stringify(payload); f.view.payloadHash = crypto.createHash('sha256').update(f.view.canonicalPayload).digest('hex');
    const h = harness(f); await a.rejects(h.run()); a.equal(h.calls.length, 3);
  });
  for (const key of ['version', 'draftRevision', 'scope', 'tools', 'fields']) await check(`fence changed second current ${key}`, async () => {
    const f = fixture(), after = structuredClone(f.current);
    if (key === 'version' || key === 'draftRevision') after[key]++;
    if (key === 'scope') after.snapshot.scope.actor_id = epoch;
    if (key === 'tools') after.snapshot.tools[0].description = 'Changed';
    if (key === 'fields') after.snapshot.draft.fields.StudentFirstName.value = 'Changed';
    await a.rejects(harness(f, { after }).run());
  });
  for (const status of [401, 403, 404, 409, 410, 429, 503]) await check(`withhold HTTP ${status} without retry`, async () => {
    const h = harness(fixture(), { statusAt: 2, status }); await a.rejects(h.run()); a.equal(h.calls.length, 2);
  });
  await check('changed session cannot adopt an old response', async () => {
    const h = harness(fixture(), { onRequest: async (n, api) => { if (n === 2) api.replaceToken(); } });
    await a.rejects(h.run()); a.equal(h.calls.length, 2);
  });
  await check('cross-portal navigation discards response', async () => {
    const h = harness(fixture(), { onRequest: async (n, api) => { if (n === 2) api.window.location.pathname = '/teacher'; } });
    await a.rejects(h.run()); a.equal(h.calls.length, 2);
  });
  await check('aborted bootstrap cannot expose a projection', async () => {
    const abort = new AbortController(); const h = harness(fixture(), { onRequest: async n => { if (n === 3) abort.abort(); } });
    await a.rejects(h.run(abort.signal)); a.equal(h.calls.length, 3);
  });
  await check('invalid URL binding dispatches nothing', async () => {
    const h = harness(); await a.rejects(h.lib.loadOnboardingReview('../other', draft, epoch, 1, new AbortController().signal)); a.equal(h.calls.length, 0);
  });
  await check('malformed response is withheld', async () => { await a.rejects(harness(fixture(), { rawAt: 2 }).run()); });
  const report = { kind: 'synthetic-passive-review', passed: observations.filter(x => x.pass).length,
    failed: observations.filter(x => !x.pass).length, total: observations.length,
    modelInference: false, sqlAcceptance: false, browserAcceptance: false, observations };
  console.log(JSON.stringify(report));
  const output = process.env.QA_PENTA_REVIEW_OUTPUT;
  if (output) { a.ok(!fs.existsSync(output)); fs.mkdirSync(path.dirname(output), { recursive: true }); fs.writeFileSync(output, JSON.stringify(report, null, 2)); }
  if (report.failed) process.exitCode = 1;
})().catch(e => { console.error(`Review test runner failed: ${e.name}`); process.exitCode = 1; });
