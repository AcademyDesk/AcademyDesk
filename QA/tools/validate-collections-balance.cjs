// Bounded checkpoint/source/evidence validation. Historical validators remain historical.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os'), crypto = require('node:crypto');
const cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..'), read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const snapshot = JSON.parse(read('QA/REPORTS/PHASE_2B_COLLECTIONS_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + snapshot.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), snapshot.commit);
assert.equal(snapshot.records.length, 119); assert.equal(new Set(snapshot.records.map(x => x.file)).size, 119);
for (const record of [...snapshot.records, ...snapshot.assemblies]) assert.equal(sha(record.file), record.sha256, record.file);
assert.deepEqual(prior.records.filter(x => sha(x.file) !== x.sha256).map(x => x.file).sort(), snapshot.changedFromPrevious.slice().sort());
assert.equal(snapshot.changedFromPrevious.length, 5);
const oldFiles = new Set(prior.records.map(x => x.file));
assert.deepEqual(snapshot.records.filter(x => !oldFiles.has(x.file)).map(x => x.file).sort(), snapshot.added.slice().sort());
assert.equal(snapshot.added.length, 5);
assert.deepEqual(snapshot.beforeApplication, JSON.parse(read('QA/REPORTS/PHASE_2B_COLLECTIONS_BEFORE_SOURCE_SNAPSHOT.json')).records);
console.log('PASS HEAD, 119 source captures, five changed/five added, before application hashes and final assemblies.');
for (const stage of ['baseline', 'final-run1', 'final-run2']) {
  const log = read(`QA/EVIDENCE/logs/phase-2b-collections-${stage}.log`), baseline = stage === 'baseline';
  if (baseline) assert.match(log, /COLLECTIONS BASELINE REPRODUCED: gross=1000; canonical remaining=500; collection balance missing; zero-balance Issued invoice included=True/);
  else {
    assert.equal((log.match(/^COLLECTIONS CASE .* PASS:/gm) || []).length, 30);
    assert.match(log, /COLLECTIONS REGRESSION PASS: 30 bounded cases/);
    for (const fixture of ['original', 'mixed', 'unpaid', 'void-only', 'cent', 'USD', 'pending-adjustment', 'rejected-adjustment'])
      assert.match(log, new RegExp(`COLLECTIONS CASE fixture/${fixture} PASS: visible=True;`));
    for (const fixture of ['settled-issued', 'fully-adjusted-issued', 'overpaid-issued', 'paid', 'cancelled', 'today', 'future', 'foreign'])
      assert.match(log, new RegExp(`COLLECTIONS CASE fixture/${fixture} PASS: visible=False;`));
    for (const target of ['settled-issued', 'fully-adjusted-issued', 'overpaid-issued', 'paid', 'cancelled', 'foreign', 'missing'])
      assert.ok(log.includes(`COLLECTIONS CASE follow-up/${target} PASS: rejected; captured state unchanged.`));
    assert.match(log, /follow-up\/cent PASS: one own linked task\/audit; financial state unchanged/);
    assert.match(log, /FinanceUser\/queue PASS: HTTP=200/); assert.match(log, /FinanceUser\/foreign PASS: HTTP=403/); assert.match(log, /anonymous PASS: HTTP=401/);
  }
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7/);
  assert.match(log, /QA Collections(?:Baseline)? exit=0/);
  assert.match(log, /routes=310, controller method\/routes=299, framework Identity method\/routes=10, SHA256=EB8940A5BC276EBA1E4273A2DAB59629964475CB2C58486244B8DB4F873E55BC/);
  const id = log.match(/^QA Collections(?:Baseline)? run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1];
  const port = Number(log.match(/^QA SQL port=(\d+)/m)[1]);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' });
  assert.equal(inspect.status, 1); assert.match(inspect.stderr, /No such (object|container)/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false);
  assert.equal(cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -eq ${port}).Count`], { encoding: 'utf8' }).trim(), '0');
  console.log(`PASS ${stage}: actual SQL/HTTP evidence and owned container/root/port absent (${id}/${port}).`);
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-collections-before-client.log'), /tests 11[\s\S]*pass 0[\s\S]*fail 11/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-collections-client.log'), /tests 33[\s\S]*pass 33[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-collections-api-tests.log'), /Failed:\s+0, Passed:\s+215, Skipped:\s+0, Total:\s+215/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-collections-final-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-collections-typecheck.log'), /PASS frontend TypeScript --noEmit --incremental false; exit=0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-collections-lint.log'), /0 errors, 3 warnings/);
for (const file of ['QA/REPORTS/PHASE_2B_COLLECTIONS_BALANCE_REPAIR.md', 'QA/ISSUES/BUG-DATA-0017.md'])
  for (const m of read(file).matchAll(/\]\(([^)]+)\)/g)) { const target = m[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue;
    assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); }
assert.match(read('QA/ISSUES/BUG-DATA-0017.md'), /\| Status \| OPEN \|/);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS source/build/typecheck/lint, 215 API/33 controlled client, report links and OPEN issue; no commit/deployment.');
