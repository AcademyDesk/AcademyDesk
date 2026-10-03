// Current-source + bounded evidence/cleanup validation; not a repeat audit.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os');
const crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..'), read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_GOVERNANCE_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + current.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
assert.equal(current.records.length, 114);
for (const record of current.records) assert.equal(sha(record.file), record.sha256, record.file);
assert.deepEqual(prior.records.filter(r => sha(r.file) !== r.sha256).map(r => r.file).sort(), current.changedFromPrevious.slice().sort());
assert.equal(current.changedFromPrevious.length, 3);
const old = new Set(prior.records.map(r => r.file));
assert.deepEqual(current.records.filter(r => !old.has(r.file)).map(r => r.file).sort(), current.added.slice().sort());
assert.equal(current.added.length, 5);
for (const record of current.assemblies) assert.equal(sha(record.file), record.sha256, record.file);
const before = JSON.parse(read('QA/REPORTS/PHASE_2B_GOVERNANCE_BEFORE_SOURCE_SNAPSHOT.json'));
assert.equal(before.records.length, 2);
for (const r of before.records) assert.equal(current.beforeApplication.find(x => x.file === r.file).sha256, r.sha256);
console.log('PASS HEAD, 114 source hashes, three changed prior captures/five additional captures, before application capture and final assemblies.');
for (const stage of ['preliminary', 'final1', 'final2']) {
  const file = stage === 'preliminary' ? 'QA/EVIDENCE/logs/phase-2b-governance-run1.log' : `QA/EVIDENCE/logs/phase-2b-governance-final-run${stage.slice(-1)}.log`;
  const log = read(file); assert.equal((log.match(/^GOVERNANCE CASE .* PASS:/gm) || []).length, 82, file);
  assert.match(log, /GOVERNANCE REGRESSION PASS: 82 primary cases/);
  for (const actor of ['AcademyAdmin', 'Owner', 'FinanceUser', 'QA-GovFinance']) {
    assert.ok(log.includes(`GOVERNANCE CASE ${actor}/list PASS: HTTP=200; captured state unchanged; exact nine-field own Collections list.`));
    assert.ok(log.includes(`GOVERNANCE CASE ${actor}/edit PASS: HTTP=200; only stage/promise changed; assignment/link/status preserved; one actor/route audit.`));
  }
  for (const actor of ['Teacher', 'Student', 'FrontDesk', 'Operations', 'QA-GovStudents', 'QA-GovNone'])
    for (const verb of ['list', 'edit']) assert.ok(log.includes(`GOVERNANCE CASE ${actor}/${verb} PASS: HTTP=403; captured state unchanged.`));
  for (const label of ['foreign-task', 'non-collections', 'missing-task']) assert.ok(log.includes(`GOVERNANCE CASE ${label} PASS: HTTP=404; captured state unchanged.`));
  for (const label of ['FinanceUser/generic-list-denied', 'FinanceUser/generic-edit-denied', 'FinanceUser/foreign-route-GET', 'FinanceUser/foreign-route-PATCH', 'grant/absent-read', 'grant/absent-edit', 'grant/wrong-academy-read', 'grant/wrong-academy-edit', 'grant/expired-read', 'grant/expired-edit', 'grant/revoked-read', 'grant/revoked-edit', 'FinanceUser/Controls-disabled-read', 'FinanceUser/Controls-disabled-edit', 'inactive-academy/read', 'inactive-academy/edit', 'inactive-user/read', 'inactive-user/edit'])
    assert.ok(log.includes(`GOVERNANCE CASE ${label} PASS: HTTP=403; captured state unchanged.`));
  for (const label of ['optional-null', 'optional-omitted-and-overposting', 'completed-preserved', 'cancelled-preserved', 'audit-recovery-edit'])
    assert.ok(log.includes(`GOVERNANCE CASE ${label} PASS: HTTP=200; only stage/promise changed; assignment/link/status preserved; one actor/route audit.`));
  for (const label of ['audit-fault-edit', 'audit-fault-create']) assert.ok(log.includes(`GOVERNANCE CASE ${label} PASS: HTTP=500; captured state unchanged.`));
  assert.match(log, /GOVERNANCE CASE audit-recovery-create PASS: HTTP=200; one own invoice-linked task and audit persisted/);
  assert.equal((log.match(/^AUDIT FAULT enabled:/gm) || []).length, 1);
  assert.equal((log.match(/^AUDIT FAULT removed:/gm) || []).length, 1);
  assert.match(log, /routes=310, controller method\/routes=299, framework Identity method\/routes=10, SHA256=EB8940A5BC276EBA1E4273A2DAB59629964475CB2C58486244B8DB4F873E55BC/);
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7/); assert.match(log, /QA Governance exit=0/);
  const id = log.match(/^QA Governance run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1], port = Number(log.match(/^QA SQL port=(\d+)/m)[1]);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' });
  assert.equal(inspect.status, 1); assert.match(inspect.stderr, /No such (object|container)/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false);
  assert.equal(cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -eq ${port}).Count`], { encoding: 'utf8' }).trim(), '0');
  console.log(`PASS ${stage}: 82 cases; exact owned container/root/port absent (${id}/${port}).`);
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-governance-before-client-corrected.log'), /tests 9[\s\S]*pass 1[\s\S]*fail 8/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-governance-client.log'), /tests 22[\s\S]*pass 22[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-governance-api-tests.log'), /Failed:\s+0, Passed:\s+201, Skipped:\s+0, Total:\s+201/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-governance-final-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.equal(read('QA/EVIDENCE/logs/phase-2b-governance-typecheck.log').trim(), 'PASS frontend TypeScript --noEmit --incremental false; exit=0.');
assert.match(read('QA/EVIDENCE/logs/phase-2b-governance-lint.log'), /0 errors, 3 warnings/);
let links = 0;
for (const file of ['QA/REPORTS/PHASE_2B_GOVERNANCE_ACCESS_REPAIR.md', 'QA/ISSUES/BUG-FUNC-0006.md', 'QA/ISSUES/BUG-FUNC-0003.md'])
  for (const match of read(file).matchAll(/\]\(([^)]+)\)/g)) {
    const target = match[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue;
    assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); links++;
  }
assert.match(read('QA/ISSUES/BUG-FUNC-0006.md'), /\| Status \| OPEN \|/);
assert.match(read('QA/ISSUES/BUG-FUNC-0003.md'), /\| Status \| OPEN \|/);
console.log(`PASS baseline/final client, API 201/201, typecheck/build/lint evidence, ${links} report/issue links; issues remain OPEN.`);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS git diff --check; no commit/push/cloud operation.');
