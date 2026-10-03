// Validate bounded evidence and cleanup, not global browser/release acceptance.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os'), crypto = require('node:crypto');
const cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..'), read = f => fs.readFileSync(path.join(root, f), 'utf8').replace(/^\uFEFF/, '');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const snapshot = JSON.parse(read('QA/REPORTS/PHASE_2B_BATCH_TIMES_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + snapshot.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), snapshot.commit);
assert.equal(new Set(snapshot.records.map(x => x.file)).size, snapshot.records.length);
for (const row of [...snapshot.records, ...snapshot.assemblies]) assert.equal(sha(row.file), row.sha256, row.file);
assert.deepEqual(prior.records.filter(x => sha(x.file) !== x.sha256).map(x => x.file).sort(), snapshot.changedFromPrevious.slice().sort());
const old = new Set(prior.records.map(x => x.file));
assert.deepEqual(snapshot.records.filter(x => !old.has(x.file)).map(x => x.file).sort(), snapshot.added.slice().sort());
const before = JSON.parse(read('QA/REPORTS/PHASE_2B_BATCH_TIMES_BEFORE_SOURCE_SNAPSHOT.json'));
assert.deepEqual(snapshot.beforeApplication, before.records);
for (const row of before.records) assert.equal(prior.records.find(x => x.file === row.file).sha256, row.sha256);
const cases = ['Pascal','camel','mixed','optional-null','optional-omitted','optional-blank','null-entry','mixed-null-entry',
  'empty-array','JSON-null','malformed','object','scalar','string','empty-entry','missing-time','null-day','null-time','number-time','invalid-day','invalid-time'];
const runs = [];
for (const stage of ['before-sql', 'final-run1', 'final-run2']) {
  const log = read(`QA/EVIDENCE/logs/phase-2b-batch-times-${stage}.log`), baseline = stage === 'before-sql';
  assert.match(log, baseline ? /BATCHTIMES BASELINE REPRODUCED: 48/ : /BATCHTIMES REGRESSION PASS: 48/);
  assert.equal((log.match(/BATCHTIMES CASE /g) || []).length, 48);
  for (const method of ['POST', 'PUT']) for (const label of cases) {
    const valid = ['Pascal','camel','mixed','optional-null','optional-omitted','optional-blank'].includes(label);
    const code = baseline && ['null-entry','mixed-null-entry'].includes(label) ? 500 :
      baseline && ['camel','mixed'].includes(label) ? 400 : valid ? method === 'POST' ? 201 : 200 : 400;
    assert.ok(log.includes(`CASE ${method}/${label} HTTP${code} PASS.`));
  }
  for (const method of ['POST','PUT']) for (const actor of ['foreign','teacher','anonymous'])
    assert.ok(log.includes(`CASE ${method}/${actor} HTTP${actor === 'anonymous' ? 401 : 403} PASS.`));
  assert.match(log, /routes=311, controller method\/routes=300, framework Identity method\/routes=10, SHA256=EFEF7739163F3DF5FF695E0F09747B9CD9734C7CFB6055D9B22A1FA6713B5D35/);
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);
  assert.match(log, /QA BatchTimes(?:Baseline)? exit=0/);
  const id = log.match(/^QA BatchTimes(?:Baseline)? run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1];
  const port = Number(log.match(/^QA SQL port=(\d+)/m)[1]); runs.push(id);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' });
  assert.equal(inspect.status, 1); assert.match(inspect.stderr, /no such/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false);
  const listener = cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`], { encoding: 'utf8' }).trim();
  assert.equal(listener, '0');
  console.log(`PASS ${stage}: 48 exact cases, route/migration/ownership controls, owned container/root/listener absent.`);
}
assert.equal(new Set(runs).size, 3);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-times-before-client.log'), /tests 6[\s\S]*pass 3[\s\S]*fail 3/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-times-before-api-tests.log'), /Failed:\s+8, Passed:\s+32, Skipped:\s+0, Total:\s+40/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-times-api-tests.log'), /Failed:\s+0, Passed:\s+277, Skipped:\s+0, Total:\s+277/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-times-client.log'), /tests 116[\s\S]*pass 116[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-times-final-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-batch-times-typecheck.log'), /PASS frontend TypeScript --noEmit --incremental false; exit=0/);
const lint = JSON.parse(read('QA/EVIDENCE/logs/phase-2b-batch-times-lint-comparison.json'));
assert.deepEqual(lint.before, { errors: 2, warnings: 3 }); assert.deepEqual(lint.after, lint.before);
assert.equal(lint.sameDiagnostics, true); assert.deepEqual(lint.beforeMessages, lint.afterMessages);
for (const id of ['BUG-FUNC-0019', 'BUG-API-0007']) assert.match(read('QA/ISSUES/' + id + '.md'), /\| Status \| OPEN \|/);
for (const file of ['QA/REPORTS/PHASE_2B_BATCH_TIMES_REPAIR.md','QA/ISSUES/BUG-FUNC-0019.md','QA/ISSUES/BUG-API-0007.md'])
  for (const m of read(file).matchAll(/\]\(([^)]+)\)/g)) { const target = m[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue; assert.ok(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); }
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore','pipe','pipe'] });
console.log(`PASS HEAD/${snapshot.records.length} source captures/precise delta/assemblies, API277/frontend116/SQL twice48/build/TypeScript. Existing lint FAIL retained; browser/phase/release/deployment not certified.`);
