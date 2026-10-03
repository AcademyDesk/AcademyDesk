// Direct-controller/InMemory evidence only, not HTTP/SQL/browser acceptance.
const fs=require('node:fs'), path=require('node:path'), crypto=require('node:crypto'), cp=require('node:child_process'), assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'), read=file=>fs.readFileSync(path.join(root,file),'utf8').replace(/^\uFEFF/,''), sha=file=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_COURSE_CONTRACT_SOURCE_SNAPSHOT.json'));
const prior=JSON.parse(read('QA/REPORTS/'+snapshot.priorSnapshot));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
assert.equal(snapshot.records.length,157); assert.equal(new Set(snapshot.records.map(x=>x.file)).size,157);
for(const row of [...snapshot.records,...snapshot.assemblies,...snapshot.testAssemblies])assert.equal(sha(row.file),row.sha256,row.file);
assert.deepEqual(snapshot.assemblies,prior.assemblies);
assert.deepEqual(snapshot.changedFromPrevious,[]); assert.ok(prior.records.every(x=>sha(x.file)===x.sha256));
const old=new Set(prior.records.map(x=>x.file)); assert.deepEqual(snapshot.records.filter(x=>!old.has(x.file)).map(x=>x.file).sort(),snapshot.added.slice().sort());
assert.deepEqual(snapshot.added.slice().sort(),['tests/AcademyDesk.Api.Tests/CoursePreservationTests.cs','QA/tools/validate-course-contract.cjs'].sort());
for(const [name,count]of [['targeted',45],['suite',322]]){
  assert.match(read(`QA/EVIDENCE/logs/phase-2b-course-contract-${name}.log`),new RegExp(`Passed!\\s+- Failed:\\s+0, Passed:\\s+${count}, Skipped:\\s+0, Total:\\s+${count}`));
  const trx=read(`QA/EVIDENCE/course-contract/course-contract-${name}.trx`);
  const attrs=Object.fromEntries([...trx.match(/<Counters ([^>]+)\/>/)[1].matchAll(/(\w+)="([^"]+)"/g)].map(m=>[m[1],Number(m[2])]));
  assert.equal(attrs.total,count); assert.equal(attrs.passed,count); assert.equal(attrs.failed,0); assert.equal(attrs.notExecuted,0);
  const outcomes=[...trx.matchAll(/<UnitTestResult\b[^>]*\boutcome="([^"]+)"/g)].map(m=>m[1]);
  assert.equal(outcomes.length,count); assert.ok(outcomes.every(x=>x==='Passed'));
}
assert.match(read('QA/ISSUES/BUG-DATA-0028.md'),/\| Status \| OPEN \|/);
for(const file of ['QA/REPORTS/PHASE_2B_COURSE_CONTRACT_CHECK.md','QA/ISSUES/BUG-DATA-0028.md'])
  for(const m of read(file).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(file),target)),target);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:['ignore','pipe','pipe']});
console.log('PASS157 captures; all155 prior sources and normal API/SQL-harness assemblies unchanged;2 QA test/validator additions. Isolated build fingerprints verified. Course controller45/45 and backend suite322/322; TRX counters/outcomes agree. Historical frontend233/TypeScript/lint not rerun. HTTP/SQL/browser/device/critical and BUG-DATA-0028 remain OPEN; no commit/deployment.');
