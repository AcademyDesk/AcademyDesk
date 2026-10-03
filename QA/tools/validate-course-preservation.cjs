const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname,'../..'), read = file => fs.readFileSync(path.join(root,file),'utf8').replace(/^\uFEFF/,''), sha = file => crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex');
const snapshot = JSON.parse(read('QA/REPORTS/PHASE_2B_COURSE_PRESERVATION_SOURCE_SNAPSHOT.json')), prior = JSON.parse(read('QA/REPORTS/'+snapshot.priorSnapshot));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
assert.equal(snapshot.records.length,155); assert.equal(new Set(snapshot.records.map(x=>x.file)).size,155);
for (const row of [...snapshot.records,...snapshot.assemblies]) assert.equal(sha(row.file),row.sha256,row.file);
assert.deepEqual(snapshot.assemblies,prior.assemblies);
assert.deepEqual(prior.records.filter(x=>sha(x.file)!==x.sha256).map(x=>x.file).sort(),snapshot.changedFromPrevious.slice().sort());
assert.deepEqual(snapshot.changedFromPrevious.slice().sort(),['apps/web/src/app/courses/page.tsx','QA/tools/course-feedback.test.cjs'].sort());
const old = new Set(prior.records.map(x=>x.file)); assert.deepEqual(snapshot.records.filter(x=>!old.has(x.file)).map(x=>x.file).sort(),snapshot.added.slice().sort());
assert.deepEqual(snapshot.added.slice().sort(),['apps/web/src/lib/course-update.ts','QA/tools/course-preservation.test.cjs','QA/tools/validate-course-preservation.cjs','apps/api/Controllers/CoursesController.cs','apps/api/Domain/Entities/ProgramCourse.cs'].sort());
const before = JSON.parse(read('QA/EVIDENCE/logs/phase-2b-course-preservation-before-lint.json'))[0], after = JSON.parse(read('QA/EVIDENCE/logs/phase-2b-course-preservation-after-lint.json'));
assert.equal(crypto.createHash('sha256').update(before.source).digest('hex'),prior.records.find(x=>x.file==='apps/web/src/app/courses/page.tsx').sha256);
assert.equal(crypto.createHash('sha256').update(after[0].source).digest('hex'),sha('apps/web/src/app/courses/page.tsx'));
const norm = row => row.messages.map(m=>({rule:m.ruleId,severity:m.severity,message:m.message.split('\n\nD:')[0],trigger:m.message.match(/void (load\w+)\(/)?.[1]??null}));
assert.deepEqual(norm(after[0]),norm(before)); assert.equal(after[0].errorCount,1); assert.equal(after[0].warningCount,1); assert.equal(after[1].messages.length,0);
assert.match(read('QA/EVIDENCE/logs/phase-2b-course-preservation-before-client.log'),/tests 24[\s\S]*pass 3[\s\S]*fail 21/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-course-preservation-targeted.log'),/tests 73[\s\S]*pass 73[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-course-preservation-client.log'),/tests 233[\s\S]*pass 233[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-course-preservation-typecheck.log'),/PASS frontend TypeScript --noEmit --incremental false; exit=0/);
// Static contract completeness, distinct from HTTP/SQL execution.
const controller = read('apps/api/Controllers/CoursesController.cs');
const args = controller.match(/public abstract record CourseRequest\(([^)]+)\)/)[1];
const keys = args.split(',').map(x=>{ const k=x.trim().split(/\s+/).at(-1); return k[0].toLowerCase()+k.slice(1); }).concat('isActive').sort();
const ts = require('../../apps/web/node_modules/typescript'), mod={exports:{}};
new Function('module','exports',ts.transpileModule(read('apps/web/src/lib/course-update.ts'),{compilerOptions:{module:ts.ModuleKind.CommonJS}}).outputText)(mod,mod.exports);
assert.deepEqual(Object.keys(mod.exports.courseUpdatePayload({})).sort(),keys);
assert.match(read('QA/ISSUES/BUG-DATA-0028.md'),/\| Status \| OPEN \|/);
for (const file of ['QA/REPORTS/PHASE_2B_COURSE_PRESERVATION_REPAIR.md','QA/ISSUES/BUG-DATA-0028.md'])
  for(const m of read(file).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(file),target)),target);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:['ignore','pipe','pipe']});
console.log('PASS155 captures;2 changed/148 prior unchanged/5 first captures; backend assemblies unchanged. Baseline24 (3pass/21fail); Course73/73, combined233/233, TypeScript. Full PUT keys match current DTO statically. Course lint unchanged1error/1warning, helper clean. No HTTP/SQL/browser/device/release/commit/deployment acceptance.');
