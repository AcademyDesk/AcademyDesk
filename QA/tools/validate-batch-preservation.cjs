// Verify this bounded checkpoint without closing existing lint/browser/release gates.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os'), crypto = require('node:crypto');
const cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..'), read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_BATCH_PRESERVATION_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + current.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
assert.equal(new Set(current.records.map(x => x.file)).size, current.records.length);
for (const row of [...current.records, ...current.assemblies]) assert.equal(sha(row.file), row.sha256, row.file);
assert.deepEqual(prior.records.filter(x => sha(x.file) !== x.sha256).map(x => x.file).sort(), current.changedFromPrevious.slice().sort());
const old = new Set(prior.records.map(x => x.file));
assert.deepEqual(current.records.filter(x => !old.has(x.file)).map(x => x.file).sort(), current.added.slice().sort());
assert.deepEqual(current.beforeApplication, JSON.parse(read('QA/REPORTS/PHASE_2B_BATCH_PRESERVATION_BEFORE_SOURCE_SNAPSHOT.json')).records);
for (const row of current.beforeApplication) assert.notEqual(sha(row.file), row.sha256);
console.log(`PASS HEAD, ${current.records.length} captures, precise changed/added delta, pre-edit hashes and final assemblies.`);
const finalIds = [];
for (const stage of ['before-sql', 'final-run1', 'final-run2']) {
  const log = read(`QA/EVIDENCE/logs/phase-2b-batch-preservation-${stage}.log`), baseline = stage === 'before-sql';
  assert.match(log, baseline ? /BATCHPRESERVE BASELINE REPRODUCED: 13/ : /BATCHPRESERVE REGRESSION PASS: 27/);
  assert.equal((log.match(/BATCHPRESERVE CASE /g) || []).length, baseline ? 13 : 27);
  assert.ok(log.includes('inherited-request-properties/explicit-full-roundtrip PASS.'));
  for (const mode of baseline ? ['InPerson', 'Online', 'Hybrid'] : ['InPerson', 'Online', 'Hybrid', 'null-optionals'])
    for (const action of ['edit', 'deactivate', 'reactivate', 'assign']) {
      const prefix = baseline ? action === 'assign' && mode !== 'InPerson' ? 'baseline-rejected' : 'baseline-loss' : 'preserved';
      assert.ok(log.includes(`CASE ${prefix}/${mode}/${action} PASS.`));
    }
  if (!baseline) {
    assert.ok(log.includes('explicit-null-clear/current-replacement-contract PASS.'));
    for (const rejection of ['missing-meeting-link/Online', 'missing-meeting-link/Hybrid', 'invalid-meeting-json', 'foreign-course', 'foreign-actor', 'teacher', 'anonymous', 'missing-batch', 'foreign-route'])
      assert.ok(log.includes(`rejected-no-write/${rejection} PASS.`));
  }
  assert.match(log, /routes=311, controller method\/routes=300, framework Identity method\/routes=10, SHA256=EFEF7739163F3DF5FF695E0F09747B9CD9734C7CFB6055D9B22A1FA6713B5D35/);
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7/);
  assert.match(log, /QA BatchPreservation(?:Baseline)? exit=0/);
  const id = log.match(/^QA BatchPreservation(?:Baseline)? run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1];
  const port = Number(log.match(/^QA SQL port=(\d+)/m)[1]); finalIds.push(id);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' });
  assert.equal(inspect.status, 1); assert.match(inspect.stderr, /No such (object|container)/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false);
  assert.equal(cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -eq ${port}).Count`], { encoding: 'utf8' }).trim(), '0');
  console.log(`PASS ${stage}: real SQL/HTTP controls and owned container/root/listener absent (${id}/${port}).`);
}
assert.equal(new Set(finalIds).size, 3);
for (const id of ['2721f9308c9f452fae069d0956a44ca6', '1853a53a3cb2487aab7f7bc9d29f3910']) {
  assert.equal(cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' }).status, 1);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false);
  assert.ok(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-exploratory-cleanup.log').includes(id));
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-before-sql-camelcase.log'), /SEEDREJECTION:.*Select a valid class time/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-before-client.log'), /tests 16[\s\S]*pass 0[\s\S]*fail 16/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-before-api-tests.log'), /Failed:\s+6, Passed:\s+0, Skipped:\s+0, Total:\s+6/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-api-tests.log'), /Failed:\s+0, Passed:\s+237, Skipped:\s+0, Total:\s+237/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-client.log'), /tests 110[\s\S]*pass 110[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-final-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-typecheck.log'), /PASS frontend TypeScript --noEmit --incremental false; exit=0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-lint.log'), /2 errors, 4 warnings/);
const lint = JSON.parse(read('QA/EVIDENCE/logs/phase-2b-batch-preservation-lint-comparison.json'));
assert.deepEqual(lint.before, { errors: 2, warnings: 4 }); assert.deepEqual(lint.after, lint.before);
assert.equal(lint.sameDiagnostics, true); assert.deepEqual(lint.beforeMessages, lint.afterMessages);
assert.deepEqual(lint.helper, { errors: 0, warnings: 0 });
for (const file of ['QA/REPORTS/PHASE_2B_BATCH_PRESERVATION_REPAIR.md', 'QA/ISSUES/BUG-DATA-0031.md', 'QA/ISSUES/BUG-FUNC-0019.md'])
  for (const m of read(file).matchAll(/\]\(([^)]+)\)/g)) { const target = m[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue; assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); }
for (const id of ['BUG-DATA-0031', 'BUG-FUNC-0019', 'BUG-FUNC-0004']) assert.match(read('QA/ISSUES/' + id + '.md'), /\| Status \| OPEN \|/);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS bounded 237 API/110 controlled frontend/two SQL x27; build/TypeScript; unchanged existing lint FAIL recorded. Browser/phase/release/commit/deployment not certified.');
