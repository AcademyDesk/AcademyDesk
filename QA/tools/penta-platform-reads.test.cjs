const assert = require('node:assert/strict');
const fs = require('node:fs');
const { test } = require('node:test');
const ts = require(process.env.QA_WEB_TYPESCRIPT || '../../apps/web/node_modules/typescript');

function load(name, imports = {}) {
  const module = { exports: {} };
  const source = fs.readFileSync('apps/web/src/lib/' + name + '.ts', 'utf8');
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } });
  new Function('require', 'module', 'exports', outputText)(name => {
    if (!(name in imports)) throw Error('Unknown dependency ' + name);
    return imports[name];
  }, module, module.exports);
  return module.exports;
}
const lib = load('penta-platform-reads', { './penta-enrollment-assistant': load('penta-enrollment-assistant') });
const answer = { scope: 'PlatformOwner', kind: 'support', asOfUtc: '2026-11-15T09:00:00Z',
  sourcePath: '/platform/control', metrics: { open: 5, highOrUrgent: 2 } };

test('recognize verified platform read families and refuse private and write prompts', () => {
  assert.equal(lib.platformPrompt('Show system status?'), 'health');
  assert.equal(lib.platformPrompt('Show academy onboarding'), 'onboarding');
  assert.equal(lib.platformPrompt('How many support cases are open?'), 'support');
  for (const text of ['Close a support case', 'Show learner fees', 'Show system status\nand enable maintenance'])
    assert.equal(lib.platformPrompt(text), null);
});

test('platform readiness must have the platform-owner scope', () => {
  assert.equal(lib.platformReady({ protocol: 'platform-reads.v1', scope: 'PlatformOwner' }), true);
  assert.equal(lib.platformReady({ protocol: 'platform-reads.v1', scope: 'AcademyAdmin' }), false);
});

test('only validated aggregate counts and the fixed Manual source appear', () => {
  assert.deepEqual(lib.platformAnswer(answer, 'support').items, [['Open support cases', 5], ['High or urgent', 2]]);
  for (const patch of [{ scope: 'AcademyAdmin' }, { kind: 'onboarding' }, { sourcePath: 'https://example.invalid' },
    { metrics: { ...answer.metrics, subject: 'private' } }, { metrics: { open: 1, highOrUrgent: 2 } }])
    assert.throws(() => lib.platformAnswer({ ...answer, ...patch }, 'support'));
});
