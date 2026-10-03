// Validate only this checkpoint; earlier accepted audits/captures remain historical.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os'), crypto = require('node:crypto');
const cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..'), read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const snapshot = JSON.parse(read('QA/REPORTS/PHASE_2B_INVOICE_SETTINGS_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + snapshot.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), snapshot.commit);
assert.equal(snapshot.records.length, 123); assert.equal(new Set(snapshot.records.map(x => x.file)).size, 123);
for (const record of [...snapshot.records, ...snapshot.assemblies]) assert.equal(sha(record.file), record.sha256, record.file);
assert.deepEqual(prior.records.filter(x => sha(x.file) !== x.sha256).map(x => x.file).sort(), snapshot.changedFromPrevious.slice().sort());
assert.equal(snapshot.changedFromPrevious.length, 4);
const old = new Set(prior.records.map(x => x.file));
assert.deepEqual(snapshot.records.filter(x => !old.has(x.file)).map(x => x.file).sort(), snapshot.added.slice().sort());
assert.equal(snapshot.added.length, 4);
assert.deepEqual(snapshot.beforeApplication, JSON.parse(read('QA/REPORTS/PHASE_2B_INVOICE_SETTINGS_BEFORE_SOURCE_SNAPSHOT.json')).records);
console.log('PASS HEAD, 123 source captures, four changed/four added, before application hashes and final assemblies.');
let finalInventory;
for (const stage of ['baseline', 'final-run1', 'final-run2']) {
  const log = read(`QA/EVIDENCE/logs/phase-2b-invoice-settings-${stage}.log`), baseline = stage === 'baseline';
  if (baseline) {
    assert.equal((log.match(/^INVOICESETTINGS CASE .* PASS:/gm) || []).length, 6);
    assert.match(log, /INVOICESETTINGS BASELINE REPRODUCED: Finance-only invoice prerequisites 200; required Governance settings 403; new invoice route absent 404/);
  } else {
    assert.equal((log.match(/^INVOICESETTINGS CASE .* PASS:/gm) || []).length, 34);
    assert.match(log, /INVOICESETTINGS REGRESSION PASS: 34 bounded cases/);
    for (const actor of ['AcademyAdmin', 'Owner', 'FinanceUser', 'QA-InvoiceFinance']) assert.ok(log.includes(`INVOICESETTINGS CASE ${actor} PASS: HTTP=200; captured state unchanged; exact seven-field own/default invoice projection.`));
    for (const actor of ['Teacher', 'Student', 'FrontDesk', 'Operations', 'QA-InvoiceStudents', 'QA-InvoiceNone']) assert.ok(log.includes(`INVOICESETTINGS CASE ${actor} PASS: HTTP=403; captured state unchanged.`));
    for (const method of ['POST', 'PUT', 'PATCH', 'DELETE']) assert.ok(log.includes(`INVOICESETTINGS CASE read-only/${method} PASS: HTTP=405; captured state unchanged.`));
    for (const label of ['Finance-only/governance-read-denied', 'Finance-only/governance-write-denied', 'FinanceUser/foreign-route', 'Admin/foreign-route', 'inactive-academy', 'inactive-user'])
      assert.ok(log.includes(`INVOICESETTINGS CASE ${label} PASS: HTTP=403; captured state unchanged.`));
    assert.match(log, /no-row\/defaults PASS: HTTP=200; captured state unchanged; exact seven-field/);
    for (const theme of ['Classic', 'Modern', 'Minimal', 'Formal']) assert.ok(log.includes(`INVOICESETTINGS CASE template/${theme} PASS: HTTP=200; captured state unchanged; exact seven-field`));
    assert.ok(log.includes('modules/[] PASS: HTTP=403')); assert.ok(log.includes('modules/["FinanceControls"] PASS: HTTP=403'));
    assert.ok(log.includes('modules/["Finance","FinanceControls"] PASS: HTTP=200')); assert.match(log, /both-modules\/governance-retained PASS: HTTP=200/);
    assert.match(log, /anonymous PASS: HTTP=401/);
    const inventory = log.match(/routes=311, controller method\/routes=300, framework Identity method\/routes=10, SHA256=[A-F0-9]{64}/)[0];
    if (finalInventory) assert.equal(inventory, finalInventory); else finalInventory = inventory;
  }
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7/); assert.match(log, /QA InvoiceSettings(?:Baseline)? exit=0/);
  const id = log.match(/^QA InvoiceSettings(?:Baseline)? run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1], port = Number(log.match(/^QA SQL port=(\d+)/m)[1]);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' }); assert.equal(inspect.status, 1); assert.match(inspect.stderr, /No such (object|container)/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false);
  assert.equal(cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -eq ${port}).Count`], { encoding: 'utf8' }).trim(), '0');
  console.log(`PASS ${stage}: actual SQL/HTTP evidence and owned container/root/port absent (${id}/${port}).`);
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-invoice-settings-before-client.log'), /tests 10[\s\S]*pass 0[\s\S]*fail 10/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-invoice-settings-client.log'), /tests 43[\s\S]*pass 43[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-invoice-settings-api-tests.log'), /Failed:\s+0, Passed:\s+222, Skipped:\s+0, Total:\s+222/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-invoice-settings-final-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-invoice-settings-typecheck.log'), /PASS frontend TypeScript --noEmit --incremental false; exit=0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-invoice-settings-lint.log'), /0 errors, 3 warnings/);
for (const file of ['QA/REPORTS/PHASE_2B_INVOICE_SETTINGS_REPAIR.md', 'QA/ISSUES/BUG-FUNC-0006.md'])
  for (const m of read(file).matchAll(/\]\(([^)]+)\)/g)) { const target = m[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue; assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); }
assert.match(read('QA/ISSUES/BUG-FUNC-0006.md'), /\| Status \| OPEN \|/);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS build/typecheck/lint, 222 API/43 controlled client, links and OPEN issue; no commit/deployment.');
