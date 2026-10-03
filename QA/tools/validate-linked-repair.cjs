// Scoped evidence validation only. Does not mutate a DB or rerun historical audits.
const fs = require('node:fs'), path = require('node:path'), os = require('node:os');
const crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_LINKED_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + current.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
assert.equal(current.records.length, 100);
for (const record of current.records) assert.equal(sha(record.file), record.sha256, record.file);
assert.deepEqual(prior.records.filter(r => sha(r.file) !== r.sha256).map(r => r.file).sort(), current.changedFromPrevious.slice().sort());
assert.equal(current.changedFromPrevious.length, 8);
const oldPaths = new Set(prior.records.map(r => r.file));
assert.deepEqual(current.records.filter(r => !oldPaths.has(r.file)).map(r => r.file).sort(), current.added.slice().sort());
const before = JSON.parse(read('QA/REPORTS/PHASE_2B_LINKED_BEFORE_SOURCE_SNAPSHOT.json'));
for (const record of before.records) assert.equal(prior.records.find(r => r.file === record.file).sha256, record.sha256);
console.log('PASS HEAD, 100 current hashes, eight exact prior changes, four additions, five before-source hashes.');

for (const stage of ['baseline', 'fixed']) for (const run of [1, 2]) {
  const file = `QA/EVIDENCE/logs/phase-2b-linked-${stage}-run${run}.log`;
  const log = read(file);
  assert.equal((log.match(/^LINKED CASE /gm) || []).length, 26, file);
  assert.equal((log.match(/^LINKED CASE .* REPRODUCED:/gm) || []).length, stage === 'baseline' ? 8 : 0, file);
  assert.equal((log.match(/^LINKED FAULT attempts=/gm) || []).length, 4, file);
  assert.equal((log.match(/^LINKED FAULT removed:/gm) || []).length, 4, file);
  assert.equal((log.match(/^LINKED ACCESS .* PASS:/gm) || []).length, 2, file);
  if (stage === 'fixed') {
    assert.equal((log.match(/^AUDIT CASE .* PASS:/gm) || []).length, 6, file);
    assert.equal((log.match(/^PEOPLE CASE .* PASS:/gm) || []).length, 28, file);
    assert.equal((log.match(/^LINKED CASE .* PASS:/gm) || []).length, 26, file);
  }
  assert.match(log, /Negative cleanup check refused a mismatched database marker/);
  assert.match(log, /PASS: application migrations=80, identity migrations=7/);
  assert.match(log, /QA Linked(?:Baseline|Fixed) exit=0/);
  const id = log.match(/^QA Linked(?:Baseline|Fixed) run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1];
  const port = Number(log.match(/^QA SQL port=(\d+)/m)[1]);
  const inspect = cp.spawnSync('docker', ['inspect', `academydesk-qa-${id}`], { encoding: 'utf8' });
  assert.equal(inspect.status, 1, 'Exact current container must be absent: ' + id);
  assert.match(inspect.stderr, /No such (object|container)/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(), 'AcademyDesk-QA', id)), false, 'Owned host root must be absent');
  const listeners = cp.execFileSync('powershell', ['-NoProfile', '-Command', `@(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -eq ${port}).Count`], { encoding: 'utf8' }).trim();
  assert.equal(listeners, '0', 'Exact ephemeral port must not remain listening');
  console.log(`PASS ${stage} ${run}: 26 linked cases${stage === 'fixed' ? ' +28 create +six finance' : ', eight faults reproduced'}; current owned container/root/port absent (${id}/${port}).`);
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-linked-api-tests.log'), /Failed:\s+0, Passed:\s+178, Skipped:\s+0, Total:\s+178/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-linked-build.log'), /0 Warning\(s\)[\s\S]*0 Error\(s\)/);
let links = 0;
for (const file of ['QA/REPORTS/PHASE_2B_LINKED_PEOPLE_REPAIR.md', 'QA/ISSUES/BUG-DATA-0052.md', 'QA/ISSUES/BUG-API-0002.md'])
  for (const match of read(file).matchAll(/\]\(([^)]+)\)/g)) {
    const target = match[1].split('#')[0];
    if (!target || /^https?:/.test(target)) continue;
    assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); links++;
  }
assert.match(read('QA/ISSUES/BUG-DATA-0052.md'), /\| Status \| OPEN \|/);
assert.match(read('QA/ISSUES/BUG-DATA-0052.md'), /RUNTIME-REPRODUCED/);
assert.match(read('QA/ISSUES/BUG-API-0002.md'), /\| Status \| OPEN \|/);
console.log(`PASS API 178/178, build, ${links} report/issue links; issues remain OPEN.`);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS git diff --check; no commit, push or cloud operation performed.');
