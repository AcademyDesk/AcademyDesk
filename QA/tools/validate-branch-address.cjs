// Bounded repair checkpoint; old audit evidence remains historical.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os'), crypto = require('node:crypto');
const cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..'), read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_BRANCH_ADDRESS_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + current.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
assert.equal(new Set(current.records.map(x => x.file)).size, current.records.length);
for (const row of [...current.records, ...current.assemblies]) assert.equal(sha(row.file), row.sha256, row.file);
assert.deepEqual(prior.records.filter(x => sha(x.file) !== x.sha256).map(x => x.file).sort(), current.changedFromPrevious.slice().sort());
const old = new Set(prior.records.map(x => x.file));
assert.deepEqual(current.records.filter(x => !old.has(x.file)).map(x => x.file).sort(), current.added.slice().sort());
assert.deepEqual(current.beforeApplication, JSON.parse(read('QA/REPORTS/PHASE_2B_BRANCH_ADDRESS_BEFORE_SOURCE_SNAPSHOT.json')).records);
for (const row of current.beforeApplication) assert.notEqual(sha(row.file), row.sha256);
console.log(`PASS HEAD, ${current.records.length} source captures, changed/added delta, before application and final assembly hashes.`);
for (const stage of ['before-sql', 'final-run1', 'final-run2']) {
  const log = read(`QA/EVIDENCE/logs/phase-2b-branch-address-${stage}.log`), baseline = stage === 'before-sql';
  assert.match(log, baseline ? /BRANCHADDRESS BASELINE REPRODUCED: 3/ : /BRANCHADDRESS REGRESSION PASS: 13/);
  assert.equal((log.match(/BRANCHADDRESS CASE /g) || []).length, baseline ? 3 : 13);
  for (const action of ['edit', 'deactivate', 'reactivate'])
    for (const value of baseline ? ['populated'] : ['populated', 'null'])
      assert.ok(log.includes(`CASE ${baseline ? 'baseline-loss' : 'preserved'}/${action}/${value} PASS.`));
  if (!baseline) for (const rejected of ['blank-name', 'missing-branch', 'foreign-actor', 'foreign-route', 'teacher', 'anonymous'])
    assert.ok(log.includes(`rejected-no-write/${rejected} PASS.`));
  assert.match(log, /routes=311, controller method\/routes=300, framework Identity method\/routes=10, SHA256=EFEF7739163F3DF5FF695E0F09747B9CD9734C7CFB6055D9B22A1FA6713B5D35/);
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7/);
  assert.match(log, /QA BranchAddress(?:Baseline)? exit=0/);
  const id = log.match(/^QA BranchAddress(?:Baseline)? run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1];
  const port = Number(log.match(/^QA SQL port=(\d+)/m)[1]);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' });
  assert.equal(inspect.status, 1); assert.match(inspect.stderr, /No such (object|container)/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false);
  assert.equal(cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -eq ${port}).Count`], { encoding: 'utf8' }).trim(), '0');
  console.log(`PASS ${stage}: SQL/HTTP evidence; owned container/root/listener absent (${id}/${port}).`);
}
// Initial fixture collision is retained/excluded, not treated as a passing baseline.
const initial = 'bb78130e49474b1aa0aa3dbd3694e543';
assert.equal(cp.spawnSync('docker', ['inspect', `academydesk-qa-${initial}`], { encoding: 'utf8' }).status, 1);
assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', initial)), false);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-address-initial-cleanup.log'), /synthetic data only; evidence retained/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-address-before-client.log'), /tests 6[\s\S]*pass 0[\s\S]*fail 6/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-address-client.log'), /tests 94[\s\S]*pass 94[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-address-api-tests.log'), /Failed:\s+0, Passed:\s+231, Skipped:\s+0, Total:\s+231/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-address-final-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-address-typecheck.log'), /PASS frontend TypeScript --noEmit --incremental false; exit=0/);
assert.equal(read('QA/EVIDENCE/logs/phase-2b-branch-address-lint.log').trim(), 'PASS targeted Branches ESLint; exit=0.');
for (const file of ['QA/REPORTS/PHASE_2B_BRANCH_ADDRESS_REPAIR.md', 'QA/ISSUES/BUG-DATA-0025.md'])
  for (const m of read(file).matchAll(/\]\(([^)]+)\)/g)) { const target = m[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue; assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); }
assert.match(read('QA/ISSUES/BUG-DATA-0025.md'), /\| Status \| OPEN \|/);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS 231 API/94 controlled client, build/typecheck/lint, links and OPEN issue; no browser/phase/release/commit/deployment claim.');
