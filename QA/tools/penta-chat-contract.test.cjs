// Synthetic frontend contract tests. No HTTP, inference, SQL or customer data.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../..');
const baseline = process.argv.includes('--baseline');
let guard;
if (baseline) {
  const source = require('node:child_process').execFileSync('git', ['show', 'd3c4ccfd6ea8344a73e390e866b43f512cb51e17:apps/web/src/components/penta/penta-chat-workspace.tsx'], { cwd: root, encoding: 'utf8' });
  const body = source.match(/function validReceipt\(value: Receipt, session: Session, requestId: string\) \{([\s\S]*?)\n\}/);
  assert.ok(body, 'Original receipt guard must be present for baseline');
  guard = vm.runInNewContext(`const guid=/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i; (function(value, session, requestId){${body[1]}})`);
} else {
  const ts = require(path.join(root, 'apps/web/node_modules/typescript'));
  const source = fs.readFileSync(path.join(root, 'apps/web/src/lib/penta-chat-contract.ts'), 'utf8');
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
  const context = { exports: {}, Intl, Date };
  vm.runInNewContext(compiled, context);
  guard = context.exports.validPentaReceipt;
  global.contract = context.exports;
}
const conversationId = '11111111-1111-4111-8111-111111111111';
const requestId = '22222222-2222-4222-8222-222222222222';
const studentId = '33333333-3333-4333-8333-333333333333';
const session = { conversationId, version: 0, expiresAtUtc: '2099-10-08T10:00:00Z' };
const fixture = () => ({ conversationId, requestId, version: 1, kind: 'RESULT', message: 'Synthetic verified read.', capability: 'executor', provider: 'PENTA Mini', protocol: '0.1',
  context: { filters: { balance_status: 'Pending' }, sort_by: null, current_learner_id: null, current_result_ids: [studentId] },
  result: { count: 1, hasMore: false, rows: [{ sourceId: studentId, displayName: 'Synthetic learner', recordCode: null, subjects: ['Piano'], balances: [{ currency: 'INR', outstanding: 400 }], sourcePath: `/student-management?studentId=${studentId}` }], asOfUtc: '2026-10-08T10:00:00Z', source: 'Synthetic ledger', balanceScope: 'Synthetic student fees.' } });
const cases = [];
const add = (name, expected, mutate) => cases.push({ name, expected, value: mutate ? mutate(fixture()) : fixture() });
const change = (name, mutate) => add(name, false, value => { mutate(value); return value; });
add('valid source result', true);
add('valid empty result', true, v => { v.result.count = 0; v.result.rows = []; v.context.current_result_ids = []; return v; });
add('valid ambiguity with source rows', true, v => { v.kind = 'CLARIFICATION_REQUIRED'; return v; });
for (const kind of ['ERROR', 'REFUSAL', 'UNSUPPORTED', 'TEXT_RESPONSE', 'CLARIFICATION_REQUIRED', 'APPROVAL_REQUIRED'])
  add(`valid ${kind} without source rows`, true, v => { v.kind = kind; v.result = null; return v; });
add('null receipt', false, () => null);
add('array receipt', false, () => []);
change('null result row', v => { v.result.rows = [null]; });
change('null balance', v => { v.result.rows[0].balances = [null]; });
change('missing filters', v => { delete v.context.filters; });
change('null filters', v => { v.context.filters = null; });
change('non-string filter', v => { v.context.filters.subject = {}; });
change('unknown filter', v => { v.context.filters.sql = 'invalid'; });
change('unsupported balance filter', v => { v.context.filters.balance_status = 'Any'; });
change('unsupported sort', v => { v.context.sort_by = 'random'; });
change('invalid current IDs', v => { v.context.current_result_ids = ['REC-617']; });
change('selected ID absent from results', v => { v.context.current_learner_id = requestId; });
change('context row identity mismatch', v => { v.context.current_result_ids = []; });
change('missing result for RESULT', v => { v.result = null; });
change('source rows on ERROR', v => { v.kind = 'ERROR'; });
change('non-boolean pagination', v => { v.result.hasMore = 'false'; });
change('count below displayed rows', v => { v.result.count = 0; });
change('inconsistent pagination', v => { v.result.hasMore = true; });
change('missing source', v => { delete v.result.source; });
change('missing scope', v => { delete v.result.balanceScope; });
change('duplicate currency', v => { v.result.rows[0].balances.push({ currency: 'INR', outstanding: 10 }); });
change('wrong capability', v => { v.capability = 'twin'; });
change('wrong conversation', v => { v.conversationId = requestId; });
change('wrong request', v => { v.requestId = conversationId; });
change('wrong version', v => { v.version = 2; });
change('wrong protocol', v => { v.protocol = 'other'; });
change('wrong provider', v => { v.provider = 'other'; });
change('unsafe source link', v => { v.result.rows[0].sourcePath = 'https://example.invalid'; });
change('negative amount', v => { v.result.rows[0].balances[0].outstanding = -1; });
change('non-finite amount', v => { v.result.rows[0].balances[0].outstanding = Infinity; });
change('invalid timestamp', v => { v.result.asOfUtc = 'not a date'; });
change('duplicate row', v => { v.result.rows.push(v.result.rows[0]); });
let passed = 0, failed = 0;
const observations = [];
for (const test of cases) {
  try { assert.equal(Boolean(guard(test.value, session, requestId, 'executor')), test.expected); passed++; observations.push({ name: test.name, pass: true }); }
  catch (error) { failed++; observations.push({ name: test.name, pass: false, error: error.name }); console.log(`FAIL ${test.name}: ${error.name}`); }
}
if (!baseline) {
  const { validPentaSession, validPentaContext, validPentaAccount, validPentaHealth } = global.contract;
  const tests = [
    ['valid session', () => validPentaSession(session), true],
    ['null session', () => validPentaSession(null), false],
    ['missing expiry', () => validPentaSession({ conversationId, version: 0 }), false],
    ['invalid expiry', () => validPentaSession({ ...session, expiresAtUtc: 'bad' }), false],
    ['non-initial version', () => validPentaSession({ ...session, version: 1 }), false],
    ['valid academy', () => validPentaContext({ academyId: conversationId, name: 'Synthetic academy', timeZone: 'Asia/Kolkata' }, conversationId), true],
    ['invalid timezone', () => validPentaContext({ academyId: conversationId, name: 'Synthetic academy', timeZone: 'Invalid/Zone' }, conversationId), false],
    ['wrong academy', () => validPentaContext({ academyId: requestId, name: 'Synthetic academy', timeZone: 'UTC' }, conversationId), false],
    ['null context', () => validPentaContext(null, conversationId), false],
    ['valid account', () => validPentaAccount({ academyId: conversationId, isPlatformOwner: false, roles: ['AcademyAdmin'] }), true],
    ['malformed roles', () => validPentaAccount({ academyId: conversationId, roles: 'Owner' }), false],
    ['platform owner excluded', () => validPentaAccount({ academyId: conversationId, isPlatformOwner: true, roles: ['Owner'] }), false],
    ['student excluded', () => validPentaAccount({ academyId: conversationId, roles: ['Student'] }), false],
    ['valid health', () => validPentaHealth({ status: 'Available', provider: 'PENTA Mini', protocol: '0.1', readOnly: true }), true],
    ['null health', () => validPentaHealth(null), false],
    ['wrong health protocol', () => validPentaHealth({ status: 'Available', provider: 'PENTA Mini', protocol: 'wrong', readOnly: true }), false],
  ];
  for (const [name, run, expected] of tests) {
    try { assert.equal(Boolean(run()), expected); passed++; observations.push({ name, pass: true }); }
    catch (error) { failed++; observations.push({ name, pass: false, error: error.name }); console.log(`FAIL ${name}: ${error.name}`); }
  }
}
const summary = { baseline, passed, failed, total: passed + failed };
console.log(JSON.stringify(summary));
const out = process.env.QA_PENTA_CONTRACT_OUTPUT;
if (out) { assert.ok(!fs.existsSync(out), 'Do not overwrite prior evidence'); fs.mkdirSync(path.dirname(out), { recursive: true }); fs.writeFileSync(out, JSON.stringify({ ...summary, observations }, null, 2)); }
if (failed) process.exitCode = 1;
