'use strict';
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), path = require('node:path');
const cp = require('node:child_process');
const { counters, publicApi, writeManifest, verify, source, MINIMUM_API_TESTS } = require('./ci-release-artifacts.cjs');
const sourceSha = 'a'.repeat(40), alternate = 'b'.repeat(40), origin = 'https://api.academydesk.example';
const parent = path.resolve(__dirname, '../../.build-check');
fs.mkdirSync(parent, { recursive: true });
const owned = fs.mkdtempSync(path.join(parent, 'ci-artifact-guards-'));
let number = 0;
function directory() { const folder = path.join(owned, String(++number)); fs.mkdirSync(folder); return folder; }
function trx(changes = {}, outcomes) {
  const attrs = { total: MINIMUM_API_TESTS, executed: MINIMUM_API_TESTS, passed: MINIMUM_API_TESTS,
    failed: 0, error: 0, timeout: 0, aborted: 0, notExecuted: 0, notRunnable: 0, ...changes };
  const results = outcomes ?? Array.from({ length: attrs.total }, () => 'Passed');
  return `<TestRun><Results>${results.map(outcome => `<UnitTestResult outcome="${outcome}" />`).join('')}</Results><ResultSummary><Counters ${Object.entries(attrs).map(([key, value]) => `${key}="${value}"`).join(' ')} /></ResultSummary></TestRun>`;
}
function fixture(kind = 'web') {
  const folder = directory();
  if (kind === 'web') { fs.mkdirSync(path.join(folder, 'out')); fs.writeFileSync(path.join(folder, 'out/index.html'), '<html>synthetic artifact guard fixture</html>'); }
  else fs.writeFileSync(path.join(folder, 'api-image.tar.gz'), 'synthetic archive bytes; not a real image');
  writeManifest(folder, kind, sourceSha, kind === 'web' ? { apiOrigin: origin } : { trx: trx() });
  return folder;
}
function change(folder, mutate) {
  const file = path.join(folder, 'manifest.json'), value = JSON.parse(fs.readFileSync(file, 'utf8'));
  mutate(value); fs.writeFileSync(file, JSON.stringify(value));
}
test('passing TRX reconciles with all existing tests and no skips', () => assert.deepEqual(counters(trx()), { total: MINIMUM_API_TESTS, passed: MINIMUM_API_TESTS, skipped: 0 }));
test('the previous 2995-test baseline cannot replace the expanded teacher-service suite', () =>
  assert.throws(() => counters(trx({ total: 2995, executed: 2995, passed: 2995 }))));
test('the previous 3040-test baseline cannot replace the protected teacher workflow suite', () =>
  assert.throws(() => counters(trx({ total: 3040, executed: 3040, passed: 3040 }))));
test('the previous 3049-test baseline cannot replace the teacher Core suite', () =>
  assert.throws(() => counters(trx({ total: 3049, executed: 3049, passed: 3049 }))));

test('the previous 3065-test baseline cannot replace the teacher conversation suite', () =>
  assert.throws(() => counters(trx({ total: 3065, executed: 3065, passed: 3065 }))));

test('the previous 3089-test baseline cannot replace the teacher startup suite', () =>
  assert.throws(() => counters(trx({ total: 3089, executed: 3089, passed: 3089 }))));

test('the previous 3106-test baseline cannot replace the enrollment shared-service suite', () =>
  assert.throws(() => counters(trx({ total: 3106, executed: 3106, passed: 3106 }))));
for (const [name, changes] of Object.entries({ filtered: { total: 1, executed: 1, passed: 1 }, failed: { passed: MINIMUM_API_TESTS - 1, failed: 1 }, skipped: { executed: MINIMUM_API_TESTS - 1, passed: MINIMUM_API_TESTS - 1, notExecuted: 1 }, aborted: { aborted: 1 }, timed_out: { timeout: 1 } }))
  test(`TRX ${name} cannot produce an accepted image manifest`, () => assert.throws(() => counters(trx(changes))));
test('TRX individual outcomes must reconcile even if aggregate claims PASS', () => assert.throws(() => counters(trx({}, Array.from({ length: MINIMUM_API_TESTS }, (_, i) => i ? 'Passed' : 'Failed')))));
test('TRX duplicate counters and external entities reject', () => {
  assert.throws(() => counters(trx().replace('</ResultSummary>', '<Counters /></ResultSummary>')));
  assert.throws(() => counters('<!DOCTYPE TestRun [<!ENTITY x SYSTEM "file:///private">]>' + trx()));
  assert.throws(() => counters(trx().replace('failed="0"', 'failed="0" failed="0"')));
});
test('API origin rejects credentials, scripts, loopback and appended routes', () => {
  for (const url of ['http://api.example', 'https://user:secret@api.example', 'https://127.0.0.1', 'https://10.2.3.4', 'https://192.168.1.2', 'https://172.20.0.1', 'https://api.example/api', 'https://api.example?token=private', 'javascript:alert(1)']) assert.throws(() => publicApi(url));
  assert.equal(publicApi(origin + '/'), origin);
});
for (const kind of ['api', 'web']) test(`${kind} content/source binding round-trips without granting production approval`, () => {
  const result = verify(fixture(kind), kind, sourceSha, origin);
  assert.equal(result.stagingApproved, false); assert.equal(result.productionApproved, false);
});
test('wrong source revision, kind and API origin reject before promotion', () => {
  const folder = fixture();
  assert.throws(() => verify(folder, 'web', alternate, origin));
  assert.throws(() => verify(folder, 'api', sourceSha, origin));
  assert.throws(() => verify(folder, 'web', sourceSha, 'https://other.academydesk.example'));
});
test('changed payload and extra payload files reject', () => {
  const changed = fixture(); fs.appendFileSync(path.join(changed, 'out/index.html'), 'changed');
  assert.throws(() => verify(changed, 'web', sourceSha, origin));
  const extra = fixture(); fs.writeFileSync(path.join(extra, 'unexpected.txt'), 'unexpected');
  assert.throws(() => verify(extra, 'web', sourceSha, origin));
});
test('missing files and a missing entry page reject', () => {
  const folder = directory(); fs.mkdirSync(path.join(folder, 'out')); fs.writeFileSync(path.join(folder, 'out/other.html'), 'no entry');
  assert.throws(() => writeManifest(folder, 'web', sourceSha, { apiOrigin: origin }));
  const incomplete = fixture(); fs.renameSync(path.join(incomplete, 'out/index.html'), path.join(incomplete, 'out/renamed.html'));
  assert.throws(() => verify(incomplete, 'web', sourceSha, origin));
});
test('altered hashes, duplicate rows and escalated approval claims reject', () => {
  for (const mutate of [value => { value.files[0].sha256 = '0'.repeat(64); }, value => value.files.push(value.files[0]), value => { value.productionApproved = true; }, value => { value.stagingApproved = true; }, value => { value.modelQualified = true; }]) {
    const folder = fixture(); change(folder, mutate); assert.throws(() => verify(folder, 'web', sourceSha, origin));
  }
});
test('invalid/fractional API count and fake skipped manifest reject', () => {
  for (const mutate of [value => { value.tests.total = MINIMUM_API_TESTS + 0.5; }, value => { value.tests.skipped = 1; }, value => { value.tests.passed--; }]) {
    const folder = fixture('api'); change(folder, mutate); assert.throws(() => verify(folder, 'api', sourceSha, origin));
  }
});
test('PR placeholder origin cannot be promoted', () => {
  const folder = directory(); fs.mkdirSync(path.join(folder, 'out')); fs.writeFileSync(path.join(folder, 'out/index.html'), 'fixture');
  writeManifest(folder, 'web', sourceSha, { apiOrigin: 'https://api.example.invalid' });
  assert.throws(() => verify(folder, 'web', sourceSha, 'https://api.example.invalid'));
});
test('an existing manifest is never overwritten', () => assert.throws(() => writeManifest(fixture(), 'web', sourceSha, { apiOrigin: origin })));
test('a directory junction inside a payload rejects without following its target', () => {
  const folder = fixture(), outside = directory();
  fs.symlinkSync(outside, path.join(folder, 'out/linked'), process.platform === 'win32' ? 'junction' : 'dir');
  assert.throws(() => verify(folder, 'web', sourceSha, origin), /links/);
});
test('source identity rejects a different HEAD or tracked/untracked source edits', t => {
  for (const [head, status] of [[alternate, ''], [sourceSha, ' M apps/api/Program.cs\n'], [sourceSha, '?? apps/api/Unexpected.cs\n']]) {
    const stub = t.mock.method(cp, 'execFileSync', (binary, args) => args[0] === 'rev-parse' ? `${head}\n` :
      status.startsWith('??') && !args.includes('--untracked-files=all') ? '' : status);
    assert.throws(() => source(sourceSha)); stub.mock.restore();
  }
});
test('source identity accepts only its exact clean checkout', t => {
  t.mock.method(cp, 'execFileSync', (binary, args) => args[0] === 'rev-parse' ? `${sourceSha}\n` : '');
  assert.doesNotThrow(() => source(sourceSha)); assert.throws(() => source('main'));
});
