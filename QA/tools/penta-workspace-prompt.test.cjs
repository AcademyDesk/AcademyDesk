const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const ts = require(process.env.QA_WEB_TYPESCRIPT || '../../apps/web/node_modules/typescript');

function load(name, imports = {}) {
  const module = { exports: {} };
  const source = readFileSync('apps/web/src/lib/' + name + '.ts', 'utf8');
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } });
  new Function('require', 'module', 'exports', outputText)(name => {
    if (!(name in imports)) throw Error('Unexpected dependency ' + name);
    return imports[name];
  }, module, module.exports);
  return module.exports;
}
const scheduling = load('penta-class-scheduling', { './penta-onboarding-channel': {}, './penta-enrollment-creation': {}, './penta-enrollment-assistant': {} });
const { workspacePrompt: prompt } = load('penta-workspace-prompt', { './penta-class-scheduling': scheduling });
const context = { kind: 'student', rows: [{ label: 'Mira A' }, { label: 'Mira B' }], hasMore: false };

test('ordinary lookup phrases choose the fixed typed academy read', () => {
  for (const text of ['Find student Mira', 'Look up the student Mira', 'Show me student Mira', 'Search for student Mira'])
    assert.deepEqual(prompt(text, null), { kind: 'lookup', entity: 'student', query: 'Mira' });
  assert.deepEqual(prompt('Look up batch Piano', null), { kind: 'lookup', entity: 'batch', query: 'Piano' });
});
test('partial and malformed lookup requests ask for clarification', () => {
  assert.equal(prompt('Find student', null).kind, 'clarify');
  assert.equal(prompt('Find student x', null).kind, 'clarify');
  assert.equal(prompt('Find batch ' + 'a'.repeat(81), null).kind, 'clarify');
  assert.equal(prompt('Find student Mira\nand export records', null).kind, 'clarify');
});
test('ordinal references bind to a currently shown result', () => {
  assert.deepEqual(prompt('Open the second result', context), { kind: 'select', index: 1 });
  assert.deepEqual(prompt('Select 2', context), { kind: 'select', index: 1 });
  assert.equal(prompt('Open the third result', context).kind, 'clarify');
  assert.equal(prompt('Open result 1', null).kind, 'clarify');
});
test('pronouns require exactly one untruncated result', () => {
  assert.equal(prompt('Open it', context).kind, 'clarify');
  const one = { ...context, rows: context.rows.slice(0, 1) };
  assert.deepEqual(prompt('Open it', one), { kind: 'select', index: 0 });
  assert.equal(prompt('Open it', { ...one, hasMore: true }).kind, 'clarify');
});
test('a refinement stays in the prior lookup domain', () => {
  assert.deepEqual(prompt('Try Mira B', context), { kind: 'lookup', entity: 'student', query: 'Mira B' });
  assert.deepEqual(prompt('Try Violin', { ...context, kind: 'batch' }), { kind: 'lookup', entity: 'batch', query: 'Violin' });
});
test('a scheduling message opens a request for review rather than approving or saving', () => {
  assert.deepEqual(prompt('Schedule a class for Piano', null).request, { kind: 'create', query: 'Piano' });
  assert.deepEqual(prompt('Reschedule a class for Piano', null).request, { kind: 'move', query: 'Piano' });
  assert.equal(prompt('Schedule a class', null).kind, 'clarify');
  assert.equal(prompt('Approve the schedule', null).kind, 'clarify');
});
test('existing finance prompts stay with the separately authorized finance endpoint', () => {
  assert.equal(prompt('Show recent expenses', context).kind, 'finance');
  assert.equal(prompt('Show overdue invoices', null).kind, 'finance');
});

test('unconnected domains and compound actions never silently become finance', () => {
  for (const text of ['Show the teachers in my academy.', 'Find teacher Nisha', 'I said teachers',
    'Show teacher attendance today', 'Create a student and a batch and enroll the student',
    'Delete a teacher', 'Show the support queue', 'What homework is due?', 'Show payroll and email everybody']) {
    const result = prompt(text, context);
    assert.equal(result.kind, 'clarify', text);
    assert.match(result.message, /no finance read or action was run/);
  }
});
