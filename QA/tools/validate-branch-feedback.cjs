// Frontend-only bounded checkpoint. No repeated API/SQL audit or closure claim.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..'), read = f => fs.readFileSync(path.join(root, f), 'utf8');
const sha = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, f))).digest('hex');
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_BRANCH_FEEDBACK_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + current.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
assert.equal(current.records.length, 126); assert.equal(new Set(current.records.map(x => x.file)).size, 126);
assert.deepEqual(current.changedFromPrevious, []);
for (const record of [...current.records, ...current.assemblies, ...prior.records]) assert.equal(sha(record.file), record.sha256, record.file);
assert.deepEqual(current.assemblies, prior.assemblies);
const old = new Set(prior.records.map(x => x.file));
assert.deepEqual(current.records.filter(x => !old.has(x.file)).map(x => x.file).sort(), current.added.slice().sort());
assert.equal(current.added.length, 3);
assert.deepEqual(current.beforeApplication, JSON.parse(read('QA/REPORTS/PHASE_2B_BRANCH_FEEDBACK_BEFORE_SOURCE_SNAPSHOT.json')).records);
assert.notEqual(sha(current.beforeApplication[0].file), current.beforeApplication[0].sha256);
console.log('PASS HEAD, 126 source captures; all 123 prior captures and API/harness assemblies unchanged; only Branches application file added/changed.');
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-feedback-before-client.log'), /tests 15[\s\S]*pass 8[\s\S]*fail 7/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-feedback-client.log'), /tests 88[\s\S]*pass 88[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-branch-feedback-typecheck.log'), /PASS frontend TypeScript --noEmit --incremental false; exit=0/);
assert.equal(read('QA/EVIDENCE/logs/phase-2b-branch-feedback-lint.log').trim(), 'PASS targeted Branches ESLint; exit=0.');
for (const file of ['QA/REPORTS/PHASE_2B_BRANCH_FEEDBACK_REPAIR.md', 'QA/ISSUES/BUG-FUNC-0003.md'])
  for (const m of read(file).matchAll(/\]\(([^)]+)\)/g)) { const target = m[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue; assert(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); }
assert.match(read('QA/ISSUES/BUG-FUNC-0003.md'), /\| Status \| OPEN \|/);
assert.match(read('QA/ISSUES/BUG-DATA-0025.md'), /\| Status \| OPEN \|/);
cp.execFileSync('git', ['diff', '--check'], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
console.log('PASS controlled baseline/current 88 checks, TypeScript, lint, report links and OPEN issues; no new backend/SQL/commit/deployment work.');
