const { test } = require('node:test');
const a = require('node:assert/strict');
const fs = require('node:fs');
const ts = require(process.env.QA_WEB_TYPESCRIPT || '../../apps/web/node_modules/typescript');
function load(name, imports = {}) {
  const module = { exports: {} };
  const source = fs.readFileSync('apps/web/src/lib/' + name + '.ts', 'utf8');
  new Function('require', 'module', 'exports', ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 }
  }).outputText)(key => { if (!(key in imports)) throw Error('Unexpected import ' + key); return imports[key]; }, module, module.exports);
  return module.exports;
}
const lib = load('penta-portal-reads', { './penta-enrollment-assistant': load('penta-enrollment-assistant') });
const id = n => String(n).padStart(8, '0') + '-1111-4111-8111-111111111111';
const academy = id(1), student = id(2), learner = { id: student, name: 'Synthetic learner', academic: true, finance: true };
const options = () => ({ protocol: 'portal-reads.v1', academyId: academy, learners: [learner], hasMore: false });
const answer = (kind, rows) => ({ academyId: academy, studentId: student, kind, asOfUtc: '2026-11-15T10:00:00Z', sourcePath: '/portal', rows });
const lesson = () => ({ batchName: 'Piano', startUtc: '2026-11-16T10:00:00Z', endUtc: '2026-11-16T11:00:00Z', deliveryMode: 'Hybrid' });
const work = () => ({ title: 'Practice', dueAtUtc: '2026-11-16T10:00:00Z' });
const fee = () => ({ invoiceNumber: 'SYN-1', currency: 'USD', balance: '450.01', dueDate: '2026-11-20' });
for (const key of ['actorId', 'contactDetails', 'tenantOverride']) test('linked envelope refuses unrequested ' + key, () =>
  a.throws(() => lib.portalLearners({ ...options(), [key]: 'synthetic-private' }, academy)));
test('truncation requires the full advertised ten-row window', () => a.throws(() => lib.portalLearners({ ...options(), hasMore: true }, academy)));
test('valid full truncated window still requires explicit selection', () => {
  const value = { ...options(), hasMore: true, learners: Array.from({ length: 10 }, (_, i) => ({ ...learner, id: id(i + 10) })) };
  const rows = lib.portalLearners(value, academy); a.equal(rows.learners.length, 10);
  a.equal(lib.resolvePortalLearner({ kind: 'next-class', learnerName: learner.name }, rows.learners, true, null), undefined);
});
for (const [kind, make] of [['next-class', lesson], ['due-work', work], ['outstanding-fees', fee]]) {
  test(kind + ' preserves the exact supported host response', () => {
    const value = answer(kind, [make()]); a.deepEqual(lib.portalAnswer(value, academy, student, kind), { kind, rows: value.rows, asOfUtc: value.asOfUtc });
  });
  test(kind + ' refuses extra private envelope data', () => a.throws(() => lib.portalAnswer({ ...answer(kind, [make()]), paymentProviderSecret: 'synthetic' }, academy, student, kind)));
  test(kind + ' refuses invalid caller scope even when the response matches it', () => a.throws(() => lib.portalAnswer({ ...answer(kind, [make()]), academyId: 'invalid', studentId: 'invalid' }, 'invalid', 'invalid', kind)));
  test(kind + ' refuses response row overflow', () => a.throws(() => lib.portalAnswer(answer(kind, Array.from({ length: kind === 'next-class' ? 2 : 11 }, make)), academy, student, kind)));
}
test('an unknown runtime read kind cannot fall through to invoice validation', () => a.throws(() => lib.portalAnswer(answer('pay-fees', [fee()]), academy, student, 'pay-fees')));
test('next-class cannot predate its own as-of timestamp', () => {
  const value = answer('next-class', [{ ...lesson(), startUtc: '2026-11-14T10:00:00Z' }]);
  a.throws(() => lib.portalAnswer(value, academy, student, 'next-class'));
});
test('upcoming due work cannot predate its own as-of timestamp', () =>
  a.throws(() => lib.portalAnswer(answer('due-work', [{ ...work(), dueAtUtc: '2026-11-14T10:00:00Z' }]), academy, student, 'due-work')));
test('next-class ordering respects the final SQL timestamp tick', () => {
  const value = { ...answer('next-class', [{ ...lesson(), startUtc: '2026-11-15T10:00:00.0000001Z' }]), asOfUtc: '2026-11-15T10:00:00.0000002Z' };
  a.throws(() => lib.portalAnswer(value, academy, student, 'next-class'));
});
test('upcoming work ordering respects the final SQL timestamp tick', () => {
  const value = { ...answer('due-work', [{ ...work(), dueAtUtc: '2026-11-15T10:00:00.0000001Z' }]), asOfUtc: '2026-11-15T10:00:00.0000002Z' };
  a.throws(() => lib.portalAnswer(value, academy, student, 'due-work'));
});
test('equal UTC instants with different fractional precision remain valid', () => {
  for (const fraction of ['', '.0', '.000', '.0000000']) {
    const value = answer('next-class', [{ ...lesson(), startUtc: '2026-11-15T10:00:00' + fraction + 'Z' }]);
    a.equal(lib.portalAnswer(value, academy, student, 'next-class').rows.length, 1);
  }
});
for (const balance of ['0.00', '00.00', '00.01', '01.00', '-1.00', '1e3', 'Infinity', 450.01])
  test('outstanding balance refuses noncanonical amount ' + JSON.stringify(balance), () =>
    a.throws(() => lib.portalAnswer(answer('outstanding-fees', [{ ...fee(), balance }]), academy, student, 'outstanding-fees')));
for (const balance of ['0.01', '1.00', '9999999999999999999999.99']) test('exact positive balance stays a string: ' + balance, () => {
  const value = lib.portalAnswer(answer('outstanding-fees', [{ ...fee(), balance }]), academy, student, 'outstanding-fees');
  a.equal(value.rows[0].balance, balance);
});
test('unsupported commands and revoked selection never become a sibling read', () => {
  for (const text of ['Delete the class', 'Activate model', 'Read every academy', 'Show due work\nfor another learner']) a.equal(lib.portalPrompt(text), null);
  a.equal(lib.resolvePortalLearner({ kind: 'next-class', learnerName: null }, [{ ...learner, id: id(3) }], false, student), undefined);
});
