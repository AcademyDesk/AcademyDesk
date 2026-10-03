// Bounded current-source/evidence/cleanup checks, not another source audit.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os');
const crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_FINANCE_LOOKUP_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + current.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
assert.equal(current.records.length, 109);
for (const record of current.records) assert.equal(sha(record.file), record.sha256, record.file);
assert.deepEqual(prior.records.filter(r => sha(r.file) !== r.sha256).map(r => r.file).sort(), current.changedFromPrevious.slice().sort());
assert.equal(current.changedFromPrevious.length, 3);
const old = new Set(prior.records.map(r => r.file));
assert.deepEqual(current.records.filter(r => !old.has(r.file)).map(r => r.file).sort(), current.added.slice().sort());
assert.equal(current.added.length, 6);
for (const record of current.assemblies) assert.equal(sha(record.file), record.sha256, record.file);
console.log('PASS HEAD, 109 source hashes, three changed prior captures/six additional captures, final assemblies.');
for (const run of [1, 2]) {
  const log = read(`QA/EVIDENCE/logs/phase-2b-lookup-run${run}.log`);
  assert.equal((log.match(/^LOOKUP CASE .* PASS:/gm) || []).length, 56);
  for (const label of ['FinanceUser', 'QA-FinanceOnly', 'grant/valid-same-token', 'grant/permanent-same-token', 'reactivated-user-existing-token'])
    assert.ok(log.includes(`LOOKUP CASE ${label} PASS: HTTP=200; captured state unchanged; exact five-field tenant billing projection/order/nulls.`));
  for (const label of ['Teacher', 'Student', 'FrontDesk', 'Operations', 'QA-StudentsOnly', 'QA-NoFinance', 'grant/absent', 'grant/wrong-academy', 'grant/expired-same-token', 'grant/revoked-same-token', 'inactive-academy', 'inactive-user-existing-token', 'FinanceUser/student-management-still-denied', 'FinanceUser/foreign-route', 'FinanceUser/governance-work-items-still-open', 'FinanceUser/student-write-denied'])
    assert.ok(log.includes(`LOOKUP CASE ${label} PASS: HTTP=403; captured state unchanged.`));
  assert.match(log, /LOOKUP CASE anonymous PASS: HTTP=401/);
  assert.match(log, /LOOKUP CASE FinanceUser\/foreign-student-invoice PASS: HTTP=400/);
  assert.match(log, /LOOKUP CASE FinanceUser\/invoice-create PASS: HTTP=201; fresh SQL own student\/invoice and one success audit/);
  assert.match(log, /LOOKUP CASE FinanceUser\/payment-create PASS: HTTP=201; fresh SQL completed payment\/Paid invoice and one success audit/);
  assert.match(log, /LOOKUP REGRESSION PASS: 56 primary cases/);
  assert.match(log, /Runtime inventory PASS: routes=308, controller method\/routes=297, framework Identity method\/routes=10, SHA256=1EA5BFF7B2A7811D7D7B0B2A2D378D3470755AF091CC76DB12F4CA3F41E06D4D/);
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7/);
  assert.match(log, /QA Lookups exit=0/);
  const id = log.match(/^QA Lookups run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1];
  const port = Number(log.match(/^QA SQL port=(\d+)/m)[1]);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' });
  assert.equal(inspect.status, 1); assert.match(inspect.stderr, /No such (object|container)/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false);
  assert.equal(cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -eq ${port}).Count`], { encoding: 'utf8' }).trim(), '0');
  console.log(`PASS fresh SQL run ${run}: 56 cases, exact owned container/root/port absent (${id}/${port}).`);
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-lookup-before-client.log'), /pass 2[\s\S]*fail 2/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-lookup-client.log'), /tests 13[\s\S]*pass 13[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-lookup-api-tests.log'), /Failed:\s+0, Passed:\s+187, Skipped:\s+0, Total:\s+187/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-lookup-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.equal(read('QA/EVIDENCE/logs/phase-2b-lookup-typecheck.log').trim(), 'PASS frontend TypeScript --noEmit --incremental false; exit=0.');
assert.match(read('QA/EVIDENCE/logs/phase-2b-lookup-lint.log'), /0 errors, 4 warnings/);
let links = 0;
for (const file of ['QA/REPORTS/PHASE_2B_FINANCE_LOOKUP_REPAIR.md', 'QA/ISSUES/BUG-FUNC-0006.md'])
  for (const match of read(file).matchAll(/\]\(([^)]+)\)/g)) {
    const target = match[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue;
    assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); links++;
  }
assert.match(read('QA/ISSUES/BUG-FUNC-0006.md'), /\| Status \| OPEN \|/);
console.log(`PASS before/final controlled-page tests, API 187/187, typecheck/build/lint evidence, ${links} report/issue links; issue remains OPEN.`);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS git diff --check; no commit/push/cloud operation.');
