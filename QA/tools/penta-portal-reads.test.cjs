const assert = require('node:assert/strict');
const fs = require('node:fs');
const { test } = require('node:test');
const ts = require(process.env.QA_WEB_TYPESCRIPT || '../../apps/web/node_modules/typescript');

function load(name, imports = {}) {
  const module = { exports: {} };
  const source = fs.readFileSync('apps/web/src/lib/' + name + '.ts', 'utf8');
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 }
  });
  new Function('require', 'module', 'exports', outputText)(name => {
    if (!(name in imports)) throw Error('Unknown dependency ' + name);
    return imports[name];
  }, module, module.exports);
  return module.exports;
}
const lib = load('penta-portal-reads', { './penta-enrollment-assistant': load('penta-enrollment-assistant') });
const id = n => String(n).padStart(8, '0') + '-1111-4111-8111-111111111111';
const academy = id(1), mira = { id: id(2), name: 'Mira Learner', academic: true, finance: true };
const leo = { id: id(3), name: 'Leo Learner', academic: true, finance: false };
const list = { academyId: academy, protocol: 'portal-reads.v1', learners: [mira, leo], hasMore: false };
const prompt = { kind: 'next-class', learnerName: null };

test('known class, due work and balance requests route to approved read kinds', () => {
  assert.deepEqual(lib.portalPrompt('When is my next class?'), prompt);
  assert.deepEqual(lib.portalPrompt('What homework is due for Mira?'), { kind: 'due-work', learnerName: 'Mira' });
  assert.deepEqual(lib.portalPrompt('How much do I owe?'), { kind: 'outstanding-fees', learnerName: null });
  for (const text of ['Show due work for ', 'Show due work for Mira for Leo', 'Tell me the class then delete it', 'Show my fees\nfor Mira'])
    assert.equal(lib.portalPrompt(text), null);
});

test('linked options reject cross-academy responses, duplicate IDs, and extra personal data', () => {
  assert.equal(lib.portalLearners(list, academy).learners.length, 2);
  assert.throws(() => lib.portalLearners(list, id(99)));
  assert.throws(() => lib.portalLearners({ ...list, learners: [mira, mira] }, academy));
  assert.throws(() => lib.portalLearners({ ...list, learners: [{ ...mira, phone: 'synthetic' }] }, academy));
});

test('a selected learner must still be linked, with no automatic fallback to a sibling', () => {
  assert.equal(lib.resolvePortalLearner(prompt, [mira, leo], false, null), undefined);
  assert.equal(lib.resolvePortalLearner(prompt, [mira, leo], false, mira.id), mira);
  assert.equal(lib.resolvePortalLearner(prompt, [leo], false, mira.id), undefined);
  assert.equal(lib.resolvePortalLearner({ ...prompt, learnerName: 'Learner' }, [mira, leo], false, null), undefined);
  assert.equal(lib.resolvePortalLearner({ ...prompt, learnerName: 'Mira' }, [mira, leo], false, null), mira);
  assert.equal(lib.resolvePortalLearner({ ...prompt, learnerName: 'Mira' }, [mira], true, null), undefined);
});

test('read results bind to learner, academy, kind and fixed Manual source', () => {
  const row = { batchName: 'Piano', startUtc: '2026-11-20T09:00:00Z', endUtc: '2026-11-20T10:00:00Z', deliveryMode: 'InPerson' };
  const answer = { academyId: academy, studentId: mira.id, kind: 'next-class', asOfUtc: '2026-11-15T09:00:00Z',
    rows: [row], sourcePath: '/portal' };
  assert.equal(lib.portalAnswer(answer, academy, mira.id, answer.kind).rows[0].batchName, 'Piano');
  for (const patch of [{ academyId: id(99) }, { studentId: leo.id }, { kind: 'due-work' }, { sourcePath: 'https://example.invalid' },
    { rows: [{ ...row, meetingLink: 'https://example.invalid' }] }])
    assert.throws(() => lib.portalAnswer({ ...answer, ...patch }, academy, mira.id, answer.kind));
});

test('portal invoice answers preserve exact amounts and require a valid currency', () => {
  const row = { invoiceNumber: 'SYNTHETIC', currency: 'USD', balance: '9999999999999999.99', dueDate: '2026-12-01' };
  const answer = { academyId: academy, studentId: mira.id, kind: 'outstanding-fees', asOfUtc: '2026-11-15T09:00:00Z',
    rows: [row], sourcePath: '/portal' };
  assert.equal(lib.portalAnswer(answer, academy, mira.id, answer.kind).rows[0].balance, row.balance);
  for (const patch of [{ balance: Number(row.balance) }, { currency: 'USD/INR' }, { dueDate: '2026-02-30' }])
    assert.throws(() => lib.portalAnswer({ ...answer, rows: [{ ...row, ...patch }] }, academy, mira.id, answer.kind));
});
