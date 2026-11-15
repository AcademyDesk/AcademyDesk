const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const ts = require(process.env.QA_WEB_TYPESCRIPT || '../../apps/web/node_modules/typescript');

function load(name, imports = {}) {
  const source = fs.readFileSync('apps/web/src/lib/' + name + '.ts', 'utf8'), module = { exports: {} };
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } });
  new Function('require', 'module', 'exports', outputText)(name => {
    if (!(name in imports)) throw Error('Unexpected dependency ' + name);
    return imports[name];
  }, module, module.exports);
  return module.exports;
}
const channel = load('penta-onboarding-channel', { './api': {} });
const academic = load('penta-enrollment-assistant');
const creation = load('penta-enrollment-creation', { './penta-onboarding-channel': channel, './penta-enrollment-assistant': academic });
const lib = load('penta-class-scheduling', { './penta-onboarding-channel': channel, './penta-enrollment-creation': creation, './penta-enrollment-assistant': academic });
const id = n => String(n).padStart(8, '0') + '-1111-4111-8111-111111111111';
const academy = id(1), batch = id(2), conversation = id(3), startRequestId = id(4), requestId = id(5);
const key = { startRequestId, requestId, conversationId: conversation };
const completeTime = '2027-01-01T10:00:00Z';
const slot = { start: '2027-02-01T10:00', end: '2027-02-01T11:00', mode: 'InPerson', location: 'Studio P' };
const now = () => Date.parse(completeTime);
const createTool = 'AcademyDesk.ClassSession.Create', moveTool = 'AcademyDesk.ClassSession.Reschedule';
const batchChoice = { sourceId: batch, name: 'Synthetic Piano', teacherId: null, branchId: null, sourcePath: '/sessions' };
const lookup = { capability: 'executor', tool: 'class-scheduling.batch-options.v1', asOfUtc: completeTime, academyId: academy, maxRows: 10, hasMore: false, rows: [batchChoice] };
const plan = { id: '00000000-0000-0000-0000-000000000000', batchId: batch, teacherId: null, branchId: null, startUtc: slot.start + ':00Z',
  endUtc: slot.end + ':00Z', deliveryMode: slot.mode, roomName: slot.location, status: 'Scheduled' };
const receipt = { receiptId: id(8), auditId: id(9), classSessionId: id(10), completedAtUtc: completeTime };
function draft(status = 'Prepared', overrides = {}) {
  return { requestId, revision: 1, contextVersion: 0, expiresAtUtc: '2027-01-01T10:10:00Z', plan, followsBatchTeacher: true,
    payloadHash: 'a'.repeat(64), status, approvalId: status === 'Prepared' ? null : id(7),
    receipt: status === 'Succeeded' ? receipt : null, ...overrides };
}
class Memory {
  data = new Map();
  getItem(key) { return this.data.get(key) ?? null; }
  setItem(key, value) { this.data.set(key, value); }
  removeItem(key) { this.data.delete(key); }
}
function fixture(options = {}) {
  const calls = [], memory = new Memory();
  let saved, view;
  const transport = {
    academyId: academy, epoch: 'current', current: () => true, signal: new AbortController().signal, invalidate() {},
    async send(path, method, body) {
      calls.push({ path, method, body });
      const response = (value, status = 200) => Response.json(value, { status, headers: { 'cache-control': options.cached ? 'public' : 'no-store' } });
      if (path === '/readiness') return response({ protocol: 'class-scheduling-browser.v1', requiresApproval: true, tools: [createTool, moveTool] });
      if (path.startsWith('/batches?')) return response(lookup);
      if (path.startsWith('/conversations/') && !path.includes('/drafts/')) return response({ conversationId: conversation, version: 0, expiresAtUtc: draft().expiresAtUtc }, 201);
      if (path.endsWith('/approve')) {
        saved = draft('Authorized', options.changed ? { plan: { ...plan, roomName: 'Different room' } } : {});
        return response(saved);
      }
      if (path.endsWith('/execute')) {
        saved = draft('Succeeded');
        if (options.lost) throw Error('Response lost');
        return response(saved, 201);
      }
      if (method === 'PUT') { saved = draft(); return response(saved, 201); }
      return response(saved);
    }
  };
  const newDriver = () => {
    const ids = [startRequestId, requestId];
    return lib.createClassScheduling(transport, () => memory, next => { view = next; }, () => ids.shift(), now);
  };
  const driver = newDriver();
  async function prepare() {
    await driver.initialise();
    await driver.search('create', 'Piano');
    await driver.prepare(view.matches.rows[0], slot);
  }
  return { driver, memory, calls, prepare, newDriver, view: () => view };
}

test('prompt separates schedule from reschedule and refuses unrelated instructions', () => {
  assert.deepEqual(lib.schedulingPrompt('Schedule a class for Piano'), { kind: 'create', query: 'Piano' });
  assert.deepEqual(lib.schedulingPrompt('Reschedule class for Piano'), { kind: 'move', query: 'Piano' });
  assert.throws(() => lib.schedulingPrompt('Approve and execute the schedule'));
});
test('UTC and mode-specific fields reject past, invalid, and unsafe values', () => {
  assert.equal(lib.schedulingSlot(slot, now()).startUtc, plan.startUtc);
  assert.equal(lib.schedulingSlot({ ...slot, mode: 'Hybrid', location: 'https://meeting.example.invalid/a' }, now()).deliveryMode, 'Hybrid');
  for (const patch of [{ start: '2027-02-30T10:00' }, { end: '2027-01-01T08:00' }, { mode: 'Hybrid', location: '' },
    { mode: 'Online', location: 'http://meeting.example.invalid' }, { mode: 'Online', location: 'https://user@meeting.example.invalid' }])
    assert.throws(() => lib.schedulingSlot({ ...slot, ...patch }, now()));
});
test('lookup is tenant-bound, bounded and rejects extra fields or duplicate sources', () => {
  assert.equal(lib.schedulingLookup(lookup, academy, 'create').rows[0].name, batchChoice.name);
  for (const patch of [{ academyId: id(99) }, { maxRows: 11 }, { rows: [batchChoice, batchChoice] },
    { rows: [{ ...batchChoice, joiningUrl: 'https://private.example.invalid' }] }])
    assert.throws(() => lib.schedulingLookup({ ...lookup, ...patch }, academy, 'create'));
});
test('exact draft rejects forged binding, malformed review or cross-session receipt', () => {
  assert.equal(lib.schedulingDraft(draft(), key).status, 'Prepared');
  for (const patch of [{ requestId: id(42) }, { revision: 2 }, { contextVersion: 1 }, { expiresAtUtc: '2027-01-02T00:00:00' },
    { payloadHash: 'A'.repeat(64) }, { plan: { ...plan, roomName: '<test>\n' } }, { plan: { ...plan, id: id(20) } }])
    assert.throws(() => lib.schedulingDraft(draft('Prepared', patch), key));
  const source = { ...plan, id: id(20) };
  assert.throws(() => lib.schedulingDraft(draft('Succeeded', { source, plan: { ...plan, id: source.id } }), key));
});
test('readiness and lookup use only GET, and a cached response never opens controls', async () => {
  const f = fixture({ cached: true });
  await f.driver.initialise();
  await f.driver.search('create', 'Piano');
  assert.equal(f.view().ready, false);
  assert.deepEqual(f.calls.map(r => r.method), ['GET']);
});
test('prepare posts a typed request and refuses execute until exact separate approval', async () => {
  const f = fixture();
  await f.prepare();
  const review = f.view().draft;
  assert.equal(review.status, 'Prepared');
  assert.equal(f.calls.at(-1).body.payload.fields.RoomName.value, 'Studio P');
  await f.driver.advance(review, 'execute');
  assert.equal(f.calls.filter(r => r.method === 'POST').length, 0);
  await f.driver.advance(review, 'approve');
  assert.equal(f.calls.at(-1).body.intent, 'ApproveClassSessionSchedule');
  await f.driver.advance(f.view().draft, 'execute');
  assert.equal(f.view().phase, 'succeeded');
  assert.deepEqual(f.calls.filter(r => r.method === 'POST').map(r => r.path.split('/').pop()), ['approve', 'execute']);
});
test('a changed approval cannot be executed', async () => {
  const f = fixture({ changed: true });
  await f.prepare();
  await f.driver.advance(f.view().draft, 'approve');
  assert.equal(f.view().phase, 'unknown');
  assert.equal(f.view().draft, null);
  await f.driver.recover();
  assert.equal(f.view().draft, null);
  assert.equal(f.calls.filter(r => r.path.endsWith('/execute')).length, 0);
});
test('committed lost result recovers after reload with GET and only opaque storage', async () => {
  const f = fixture({ lost: true });
  await f.prepare();
  await f.driver.advance(f.view().draft, 'approve');
  await f.driver.advance(f.view().draft, 'execute');
  assert.equal(f.view().phase, 'unknown');
  const serialized = JSON.parse(f.memory.getItem(lib.schedulingPointer));
  assert.deepEqual(serialized, { ...key, academyId: academy, epoch: 'current' });
  const before = f.calls.length;
  f.driver.dispose();
  await f.newDriver().initialise();
  assert.equal(f.view().draft.receipt.receiptId, receipt.receiptId);
  assert.deepEqual(f.calls.slice(before).map(r => r.method), ['GET', 'GET']);
});
test('a stale academy or login epoch invalidates the recovery pointer', async () => {
  const f = fixture();
  await f.prepare();
  assert.equal(lib.readSchedulingPointer(f.memory, academy, 'stale'), null);
  assert.equal(f.memory.getItem(lib.schedulingPointer), null);
});
