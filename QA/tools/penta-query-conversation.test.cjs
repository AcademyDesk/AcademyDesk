const { test } = require('node:test');
const a = require('node:assert/strict');
const fs = require('node:fs');
const ts = require(process.env.QA_WEB_TYPESCRIPT || '../../apps/web/node_modules/typescript');
function load(name, imports = {}) {
  const m = { exports: {} };
  const js = ts.transpileModule(fs.readFileSync('apps/web/src/lib/' + name + '.ts', 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
  new Function('require', 'module', 'exports', js)(key => { if (!(key in imports)) throw Error(key); return imports[key]; }, m, m.exports);
  return m.exports;
}
const channel = load('penta-onboarding-channel', { './api': {} });
const contract = load('penta-chat-contract');
const lib = load('penta-query-conversation', { './penta-onboarding-channel': channel, './penta-chat-contract': contract });
const id = n => String(n).padStart(8, '0') + '-1111-4111-8111-111111111111';
const academy = id(1), conversation = id(2), record = id(3), expiry = '2030-01-01T00:00:00Z';
function receipt(requestId, expectedVersion, text, entity = 'teacher') {
  return { conversationId: conversation, requestId, version: expectedVersion + 1, kind: 'RESULT', message: 'Verified record',
    context: { entity, filters: {}, resultIds: [record] }, result: { entity, records: [{ id: record, label: 'Synthetic record', code: null,
      active: true, subjects: [], sourcePath: entity === 'teacher' ? `/teacher-profile?teacherId=${record}` : entity === 'student' ? `/student-management?studentId=${record}` : '/batch-setup' }],
      hasMore: false, asOfUtc: '2026-11-24T01:00:00Z' }, inputText: text, protocol: lib.queryProtocol };
}
function fixture(options = {}) {
  let view, current = true, seq = 10;
  const map = options.map || new Map(), requests = [], receipts = options.receipts || new Map();
  const store = { getItem: k => map.get(k) ?? null, setItem: (k, v) => { if (options.storageFail) throw Error('storage unavailable'); map.set(k, v); }, removeItem: k => map.delete(k) };
  const response = value => Response.json(value, { status: 200, headers: { 'cache-control': 'no-store' } });
  const driver = lib.createQueryConversation({ academyId: academy, epoch: 'synthetic-epoch', signal: new AbortController().signal,
    current: () => current, invalidate: () => { current = false; }, send: async (url, method, body) => {
      requests.push({ url, method, body });
      if (url === '/health') return response({ protocol: lib.queryProtocol, readOnly: true, status: options.down ? 'Unavailable' : 'Available',
        tools: ['teacher', 'student', 'batch'].flatMap(entity => ['search', 'open'].map(kind => ({ key: `AcademyDesk.${entity}.${kind}`, entity, kind, readOnly: true, fields: entity === 'teacher' ? ['name', 'subject', 'status'] : ['name', 'status'] }))) });
      if (url === '/conversations') return Response.json({ conversationId: conversation, version: 0, expiresAtUtc: expiry }, { status: 201, headers: { 'cache-control': 'no-store' } });
      if (method === 'POST') {
        const value = receipt(body.requestId, body.expectedVersion, body.text, options.entity || 'teacher'); receipts.set(body.requestId, structuredClone(value));
        if (options.lost) throw Error('reply lost');
        if (options.tamper) options.tamper(value);
        return Response.json(value, { status: 201, headers: { 'cache-control': 'no-store' } });
      }
      if (url.includes('/turns/')) return response(receipts.get(url.split('/').at(-1)));
      return response({ conversationId: conversation, version: receipts.size, expiresAtUtc: expiry, turns: [...receipts.values()], pendingRequestId: null, hasMore: false });
    },
  }, () => store, value => { view = value; }, () => id(seq++));
  return { driver, map, receipts, requests, get view() { return view; }, change: () => { current = false; } };
}

test('queries negotiate real typed catalog and never POST before explicit start/send', async () => {
  const f = fixture(); await f.driver.initialise(); a.equal(f.view.phase, 'ready'); a.deepEqual(f.view.entities, ['teacher', 'student', 'batch']);
  a.equal(f.requests.filter(x => x.method === 'POST').length, 0);
});
for (const entity of ['teacher', 'student', 'batch']) test(`${entity} result keeps the source entity and matching Manual destination`, async () => {
  const f = fixture({ entity }); await f.driver.initialise(); await f.driver.start(); await f.driver.send('Find records');
  a.equal(f.view.phase, 'idle'); a.equal(f.view.turns[0].context.entity, entity); a.equal(f.view.turns[0].result.entity, entity);
});
test('conversation preserves multiple authoritative turns and reloads history with GET only', async () => {
  const f = fixture(); await f.driver.initialise(); await f.driver.start(); await f.driver.send('Show teachers'); await f.driver.send('Only piano');
  a.equal(f.view.turns.length, 2);
  const reload = fixture({ map: f.map, receipts: f.receipts, down: true }); await reload.driver.initialise();
  a.equal(reload.view.turns.length, 2); a.equal(reload.view.available, false); a.equal(reload.requests.filter(x => x.method === 'POST').length, 0);
  a.deepEqual(reload.view.turns.map(x => x.inputText), ['Show teachers', 'Only piano']);
});
test('lost response recovers same durable receipt without another planner dispatch', async () => {
  const f = fixture({ lost: true }); await f.driver.initialise(); await f.driver.start(); await f.driver.send('Show teachers');
  a.equal(f.view.phase, 'unknown'); const posts = f.requests.filter(x => x.method === 'POST').length;
  await f.driver.recover(); a.equal(f.view.phase, 'idle'); a.equal(f.view.turns[0].context.entity, 'teacher');
  a.equal(f.requests.filter(x => x.method === 'POST').length, posts);
});
test('private storage keeps only scoped recovery IDs and cannot send if persistence fails', async () => {
  const f = fixture(); await f.driver.initialise(); await f.driver.start(); await f.driver.send('Private prompt');
  const value = f.map.get(lib.queryPointerKey); a.doesNotMatch(value, /Private prompt|Synthetic record|Bearer|filters/); a.equal(Object.keys(JSON.parse(value)).length, 6);
  const blocked = fixture({ storageFail: true }); await blocked.driver.initialise(); await blocked.driver.start(); await blocked.driver.send('Do not send');
  a.equal(blocked.requests.filter(x => x.method === 'POST' && x.url.includes('/turns')).length, 0);
});
for (const [label, tamper] of [
  ['wrong domain', v => { v.context.entity = 'student'; }],
  ['unsafe source', v => { v.result.records[0].sourcePath = 'javascript:bad'; }],
  ['extra private data', v => { v.result.records[0].salary = 'private'; }],
  ['invented result ID', v => { v.context.resultIds = [id(99)]; }],
  ['wrong request', v => { v.requestId = id(99); }],
  ['wrong session', v => { v.conversationId = id(99); }],
  ['unknown protocol', v => { v.protocol = 'other'; }],
  ['financial filters', v => { v.context.filters.balance_status = 'Pending'; }],
]) test(`reject ${label} while retaining GET-only recovery`, async () => {
  const f = fixture({ tamper }); await f.driver.initialise(); await f.driver.start(); await f.driver.send('Show teachers');
  a.equal(f.view.phase, 'unknown'); a.equal(f.view.turns.length, 0); await f.driver.recover(); a.equal(f.view.phase, 'idle');
});
test('foreign epoch recovery pointers do not leak prior conversation history', async () => {
  const map = new Map([[lib.queryPointerKey, JSON.stringify({ epoch: 'other', academyId: academy, conversationId: conversation,
    requestId: id(10), expectedVersion: 0, expiresAtUtc: expiry })]]);
  const f = fixture({ map }); await f.driver.initialise(); a.equal(f.view.phase, 'ready'); a.equal(f.requests.length, 1); a.equal(map.size, 0);
});
