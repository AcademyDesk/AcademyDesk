// Controlled frontend checkpoint only; browser/device and release gates stay open.
const fs = require('node:fs'), path = require('node:path'), cp = require('node:child_process'), crypto = require('node:crypto'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8').replace(/^\uFEFF/, '');
const sha = file => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex');
const snapshot = JSON.parse(read('QA/REPORTS/PHASE_2B_COURSE_FEEDBACK_SOURCE_SNAPSHOT.json'));
const prior = JSON.parse(read('QA/REPORTS/' + snapshot.priorSnapshot));
assert.equal(cp.execFileSync('git', ['rev-parse','HEAD'], { cwd: root, encoding: 'utf8' }).trim(), snapshot.commit);
assert.equal(snapshot.records.length, 150);
assert.equal(new Set(snapshot.records.map(x => x.file)).size, snapshot.records.length);
for (const row of [...snapshot.records, ...snapshot.assemblies]) assert.equal(sha(row.file), row.sha256, row.file);
assert.deepEqual(prior.records.filter(x => sha(x.file) !== x.sha256).map(x => x.file).sort(), snapshot.changedFromPrevious.slice().sort());
assert.deepEqual(snapshot.changedFromPrevious, []);
assert.deepEqual(snapshot.assemblies, prior.assemblies);
const old = new Set(prior.records.map(x => x.file));
assert.deepEqual(snapshot.records.filter(x => !old.has(x.file)).map(x => x.file).sort(), snapshot.added.slice().sort());
assert.deepEqual(snapshot.added.slice().sort(), ['apps/web/src/app/courses/page.tsx','QA/tools/course-feedback.test.cjs','QA/tools/validate-course-feedback.cjs'].sort());
const before = JSON.parse(read('QA/REPORTS/PHASE_2B_COURSE_FEEDBACK_BEFORE_SOURCE_SNAPSHOT.json')).records;
assert.deepEqual(snapshot.beforeApplication, before);
assert.equal(before[0].file, 'apps/web/src/app/courses/page.tsx');
assert.equal(old.has(before[0].file), false); // First bounded capture; pre-edit lint independently captures original source.
assert.match(read('QA/EVIDENCE/logs/phase-2b-course-feedback-before-client.log'), /tests 15[\s\S]*pass 10[\s\S]*fail 5/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-course-feedback-targeted.log'), /tests 49[\s\S]*pass 49[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-course-feedback-client.log'), /tests 209[\s\S]*pass 209[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-course-feedback-typecheck.log'), /PASS frontend TypeScript --noEmit --incremental false; exit=0/);
const lintBefore = JSON.parse(read('QA/EVIDENCE/logs/phase-2b-course-feedback-before-lint.json'));
const lintAfter = JSON.parse(read('QA/EVIDENCE/logs/phase-2b-course-feedback-after-lint.json'));
assert.equal(crypto.createHash('sha256').update(lintBefore[0].source).digest('hex'), before[0].sha256);
assert.equal(crypto.createHash('sha256').update(lintAfter[0].source).digest('hex'), sha(before[0].file));
const norm = rows => rows.flatMap(x => x.messages.map(m => ({ ruleId: m.ruleId, severity: m.severity,
  message: m.message.split('\n\nD:')[0], trigger: m.message.match(/void (load\w+)\(/)?.[1] ?? null }))).sort((a,b) => JSON.stringify(a).localeCompare(JSON.stringify(b)));
// Additional asynchronous readback path removes only the old loadCourses diagnostic; remaining gate still fails.
assert.deepEqual(norm(lintAfter), norm(lintBefore).filter(x => !(x.ruleId === 'react-hooks/set-state-in-effect' && x.trigger === 'loadCourses')));
assert.equal(lintBefore[0].errorCount, 2); assert.equal(lintBefore[0].warningCount, 1);
assert.equal(lintAfter[0].errorCount, 1); assert.equal(lintAfter[0].warningCount, 1);
for (const issue of ['BUG-FUNC-0003','BUG-DATA-0028']) assert.match(read(`QA/ISSUES/${issue}.md`), /\| Status \| OPEN \|/);
for (const file of ['QA/REPORTS/PHASE_2B_COURSE_FEEDBACK_REPAIR.md','QA/ISSUES/BUG-FUNC-0003.md'])
  for (const m of read(file).matchAll(/\]\(([^)]+)\)/g)) { const target = m[1].split('#')[0]; if (!target || /^https?:/.test(target)) continue; assert.ok(fs.existsSync(path.resolve(root, path.dirname(file), target)), `${file}: ${target}`); }
cp.execFileSync('git', ['diff','--check'], { cwd: root, stdio: ['ignore','pipe','pipe'] });
console.log('PASS 150 source captures; all147 prior captures and backend assemblies unchanged;3 first captures. Baseline15 (10pass/5fail), Course49/49, combined209/209, TypeScript. Lint2->1error/1warning, no new diagnostics; gate FAIL. No API/SQL/browser/phase/commit/deployment acceptance. Course configuration-loss BUG-DATA-0028 remains OPEN.');
