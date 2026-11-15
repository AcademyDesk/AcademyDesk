const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const ts = require(process.env.QA_WEB_TYPESCRIPT || '../../apps/web/node_modules/typescript');

function load(name, imports = {}) {
  const module = { exports: {} };
  const source = fs.readFileSync('apps/web/src/lib/' + name + '.ts', 'utf8');
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 }
  });
  new Function('require', 'module', 'exports', outputText)(name => {
    if (!(name in imports)) throw Error('Unexpected dependency ' + name);
    return imports[name];
  }, module, module.exports);
  return module.exports;
}
const channel = load('penta-onboarding-channel', { './api': {} });
const enrollment = load('penta-enrollment-assistant');
const lib = load('penta-teacher-attendance', { './penta-onboarding-channel': channel, './penta-enrollment-assistant': enrollment });
const id = n => String(n).padStart(8, '0') + '-1111-4111-8111-111111111111';
const [academy, classId, batchId, learnerId, requestId] = [1, 2, 3, 4, 5].map(id);
const startUtc = '2026-11-15T09:00:00Z', expiresAtUtc = '2026-11-15T13:00:00Z';
const lesson = { id: classId, batchId, batchName: 'Piano rehearsal', startUtc };
const learner = { id: learnerId, name: 'Synthetic learner' };
const classes = { academyId: academy, sessionId: null, rows: [lesson], hasMore: false };
const roster = { academyId: academy, sessionId: classId, rows: [learner], hasMore: false };
const receipt = { id: id(6), auditId: id(7), attendanceId: id(8), markedAtUtc: startUtc, inAppQueued: 1 };
const pointer = { epoch: 'current', academyId: academy, sessionId: classId, requestId };
const draft = { requestId, classSessionId: classId, batchId, sessionStartUtc: startUtc, expiresAtUtc,
  payloadHash: 'b'.repeat(64), status: 'Prepared', receipt: null, change: { studentId: learnerId, status: 'Absent',
    notes: null, inAppMessage: 'Your attendance for the class has been marked Absent.' } };

function fixture({ lost = false, cached = false } = {}) {
  const memory = new Map(), calls = [];
  const storage = { getItem: k => memory.get(k), setItem: (k, v) => memory.set(k, v), removeItem: k => memory.delete(k) };
  let view, outcome;
  const transport = {
    academyId: academy, epoch: pointer.epoch, signal: new AbortController().signal, current: () => true, invalidate() {},
    async send(path, method, body) {
      calls.push({ path, method, body });
      const response = (value, status = 200) => Response.json(value, { status, headers: { 'cache-control': cached ? 'public' : 'no-store' } });
      if (path === '/readiness') return response({ protocol: 'teacher-attendance.v1', academyId: academy, requiresConfirmation: true });
      if (path.startsWith('/sessions?q=')) return response(classes);
      if (path.includes('/roster?')) return response(roster);
      if (path.endsWith('/confirm')) {
        outcome = { ...draft, status: 'Succeeded', receipt };
        if (lost) throw Error('lost confirmation');
        return response(outcome, 201);
      }
      if (method === 'PUT') { outcome = draft; return response(draft, 201); }
      return response(outcome);
    }
  };
  const driver = () => lib.createTeacherAttendance(transport, () => storage, value => { view = value; }, () => requestId,
    () => Date.parse(startUtc));
  const operation = driver();
  async function prepare() {
    await operation.initialise();
    await operation.searchClasses('Piano');
    operation.selectClass(view.classes.rows[0]);
    await operation.searchLearners('Synthetic');
    await operation.prepare(view.roster.rows[0], 'Absent', '');
  }
  return { memory, calls, driver, operation, prepare, view: () => view };
}

test('attendance prompt requires a batch name and never treats consent as a command', () => {
  for (const verb of ['Mark', 'Take', 'Record']) assert.equal(lib.attendancePrompt(verb + ' attendance for Piano'), 'Piano');
  assert.throws(() => lib.attendancePrompt('Yes, confirm the mark'));
  assert.throws(() => lib.attendancePrompt('Mark attendance'));
});

test('drop foreign, wrong-session, duplicate and private option fields', () => {
  assert.equal(lib.attendanceClasses(classes, academy).rows[0].id, classId);
  assert.equal(lib.attendanceRoster(roster, academy, classId).rows[0].id, learnerId);
  assert.throws(() => lib.attendanceRoster(roster, id(55), classId));
  assert.throws(() => lib.attendanceRoster(roster, academy, id(55)));
  assert.throws(() => lib.attendanceRoster({ ...roster, rows: [learner, learner] }, academy, classId));
  assert.throws(() => lib.attendanceRoster({ ...roster, rows: [{ ...learner, email: 'synthetic@example.invalid' }] }, academy, classId));
});

test('attendance replies require exact request, class, digest and queued notice outcome', () => {
  assert.equal(lib.attendanceDraft(draft, pointer).status, 'Prepared');
  for (const patch of [{ requestId: id(99) }, { classSessionId: id(99) }, { payloadHash: 'invalid' },
    { change: { ...draft.change, inAppMessage: 'Already delivered' } },
    { status: 'Succeeded', receipt: { ...receipt, inAppQueued: 2 } }])
    assert.throws(() => lib.attendanceDraft({ ...draft, ...patch }, pointer));
});

test('cached readiness never opens attendance', async () => {
  const f = fixture({ cached: true });
  await f.operation.initialise();
  assert.equal(f.view().ready, false);
  assert.deepEqual(f.calls.map(r => r.method), ['GET']);
});

test('a lost confirmation has one explicit write and GET-only recovery after reload', async () => {
  const f = fixture({ lost: true });
  await f.prepare();
  assert.deepEqual(f.calls.filter(r => r.method !== 'GET').map(r => r.method), ['PUT']);
  assert.deepEqual(JSON.parse(f.memory.get(lib.attendancePointer)), pointer);
  await f.operation.confirm(f.view().draft);
  assert.equal(f.view().phase, 'unknown');
  const before = f.calls.length;
  const post = f.calls.at(-1);
  assert.deepEqual(post.body, { payloadHash: draft.payloadHash, intent: 'ConfirmTeacherAttendanceAndQueueInApp' });
  f.operation.dispose();
  await f.driver().initialise();
  assert.equal(f.view().draft.receipt.id, receipt.id);
  assert.deepEqual(f.calls.slice(before).map(r => r.method), ['GET', 'GET']);
});
