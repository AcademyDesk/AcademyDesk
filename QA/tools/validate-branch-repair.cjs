// Bounded branch-repair evidence validation. No application/SQL mutation.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os');
const crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_BRANCH_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + current.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
assert.equal(current.records.length, 103);
for (const record of current.records) assert.equal(sha(record.file), record.sha256, record.file);
assert.deepEqual(prior.records.filter(r => sha(r.file) !== r.sha256).map(r => r.file).sort(), current.changedFromPrevious.slice().sort());
assert.equal(current.changedFromPrevious.length, 5);
const oldPaths = new Set(prior.records.map(r => r.file));
assert.deepEqual(current.records.filter(r => !oldPaths.has(r.file)).map(r => r.file).sort(), current.added.slice().sort());
const before = JSON.parse(read('QA/REPORTS/PHASE_2B_BRANCH_BEFORE_SOURCE_SNAPSHOT.json'));
for (const record of before.records) assert.equal(prior.records.find(r => r.file === record.file).sha256, record.sha256);
console.log('PASS HEAD, 103 current hashes, five exact prior changes, three additions, two before-source hashes.');
for (const stage of ['baseline', 'fixed']) for (const run of [1, 2]) {
  const file = `QA/EVIDENCE/logs/phase-2b-branch-${stage}-run${run}.log`;
  const log = read(file);
  assert.equal((log.match(/^BRANCH CASE /gm) || []).length, 22, file);
  assert.equal((log.match(/^BRANCH CASE .* REPRODUCED:/gm) || []).length, stage === 'baseline' ? 8 : 0, file);
  assert.equal((log.match(/^LINKED CASE branch\//gm) || []).length, 22, file);
  for (const kind of ['students', 'teachers']) for (const label of ['foreign', 'missing', 'empty-guid', 'foreign-inactive'])
    assert.match(log, new RegExp(`BRANCH CASE ${kind}/${label} ${stage === 'baseline' ? 'REPRODUCED: HTTP=200' : 'PASS: HTTP=400'}`));
  assert.equal((log.match(/^BRANCH CASE .*\/malformed PASS: HTTP=400/gm) || []).length, 2);
  if (stage === 'fixed') {
    assert.equal((log.match(/^LINKED CASE (?!branch\/).* PASS:/gm) || []).length, 26, file);
    assert.equal((log.match(/^LINKED FAULT attempts=/gm) || []).length, 4, file);
    assert.equal((log.match(/^LINKED FAULT removed:/gm) || []).length, 4, file);
    assert.equal((log.match(/^LINKED ACCESS .* PASS:/gm) || []).length, 2, file);
    assert.equal((log.match(/^BRANCH CASE .* PASS:/gm) || []).length, 22, file);
  }
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7/);
  assert.match(log, /QA Branch(?:Baseline|Fixed) exit=0/);
  const id = log.match(/^QA Branch(?:Baseline|Fixed) run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1];
  const port = Number(log.match(/^QA SQL port=(\d+)/m)[1]);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' });
  assert.equal(inspect.status, 1, 'Current exact container must be absent'); assert.match(inspect.stderr, /No such (object|container)/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false, 'Owned host root must be absent');
  assert.equal(cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -eq ${port}).Count`], { encoding: 'utf8' }).trim(), '0');
  console.log(`PASS ${stage} ${run}: 22 branch cases${stage === 'fixed' ? ' +26 linked regression' : ', eight invalid assignments reproduced'}; owned container/root/port absent (${id}/${port}).`);
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-api-tests.log'), /Failed:\s+0, Passed:\s+184, Skipped:\s+0, Total:\s+184/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
let links = 0;
for (const file of ['QA/REPORTS/PHASE_2B_PEOPLE_BRANCH_REPAIR.md', 'QA/ISSUES/BUG-DATA-0051.md'])
  for (const match of read(file).matchAll(/\]\(([^)]+)\)/g)) {
    const target = match[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue;
    assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); links++;
  }
assert.match(read('QA/ISSUES/BUG-DATA-0051.md'), /\| Status \| OPEN \|/);
assert.match(read('QA/ISSUES/BUG-DATA-0051.md'), /RUNTIME-REPRODUCED/);
console.log(`PASS API 184/184, build, ${links} report/issue links; issue/phase remain OPEN.`);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS git diff --check; no commit, push or cloud operation performed.');
