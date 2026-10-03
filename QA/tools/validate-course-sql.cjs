// Real bounded HTTP/SQL evidence; no browser or phase/release closure.
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),crypto=require('node:crypto'),cp=require('node:child_process'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=file=>fs.readFileSync(path.join(root,file),'utf8').replace(/^\uFEFF/,''),sha=file=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_COURSE_SQL_SOURCE_SNAPSHOT.json')),prior=JSON.parse(read('QA/REPORTS/'+snapshot.priorSnapshot));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
assert.equal(snapshot.records.length,159);assert.equal(new Set(snapshot.records.map(x=>x.file)).size,159);
for(const row of [...snapshot.records,...snapshot.assemblies,...snapshot.testAssemblies,...snapshot.runtimeAssemblies])assert.equal(sha(row.file),row.sha256,row.file);
assert.deepEqual(snapshot.assemblies,prior.assemblies);assert.deepEqual(snapshot.testAssemblies,prior.testAssemblies);
assert.deepEqual(prior.records.filter(x=>sha(x.file)!==x.sha256).map(x=>x.file).sort(),snapshot.changedFromPrevious.slice().sort());
assert.deepEqual(snapshot.changedFromPrevious.slice().sort(),['QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1'].sort());
const old=new Set(prior.records.map(x=>x.file));assert.deepEqual(snapshot.records.filter(x=>!old.has(x.file)).map(x=>x.file).sort(),snapshot.added.slice().sort());
assert.deepEqual(snapshot.added.slice().sort(),['QA/tools/SqlHarness/CoursePreservationRegression.cs','QA/tools/validate-course-sql.cjs'].sort());
const cases=[];
for(const type of ['Music','Tuition','Coaching'])for(const variant of ['populated','null'])for(const action of ['edit','deactivate','reactivate'])cases.push([`preserved/${type}/${variant}/${action}`,200]);
for(const type of ['Music','Tuition','Coaching'])cases.push(['visible-level-clear/'+type,200]);
cases.push(['explicit-full-null-clear',200]);
for(const label of ['name','type','weekly-low','weekly-high','session-low','session-high','min-low','min-high','max-low','max-high','age-reversed','duplicate-code'])cases.push(['rejected/'+label,400]);
for(const method of ['POST','PUT'])for(const actor of ['foreign','teacher','anonymous'])cases.push([method+'/'+actor,actor==='anonymous'?401:403]);
cases.push(['rejected/missing',404],['rejected/foreign-id',404],['rejected/foreign-route',403]);
for(const mode of ['null','omitted','blank'])cases.push(['create-optionals/'+mode,201]);
cases.push(['legacy-abbreviated-loss-reproduced',200]);assert.equal(cases.length,47);
const runs=[];
for(const run of ['run1','run2']){
  const log=read(`QA/EVIDENCE/logs/phase-2b-course-sql-${run}.log`);
  assert.match(log,/COURSEPRESERVE REGRESSION PASS: 47 cases/);assert.equal((log.match(/COURSEPRESERVE CASE /g)||[]).length,47);
  for(const [label,status]of cases)assert.ok(log.includes(`COURSEPRESERVE CASE ${label} HTTP${status} PASS.`),run+':'+label);
  assert.match(log,/routes=311, controller method\/routes=300, framework Identity method\/routes=10, SHA256=EFEF7739163F3DF5FF695E0F09747B9CD9734C7CFB6055D9B22A1FA6713B5D35/);
  assert.match(log,/HTTP controls PASS/);assert.match(log,/Tenant controls PASS/);
  assert.match(log,/Negative cleanup check refused a mismatched database marker/);
  assert.match(log,/PASS: application migrations=80, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);
  assert.match(log,/QA CoursePreservation exit=0/);
  const id=log.match(/^QA CoursePreservation run=([a-f0-9]{32}) container=academydesk-qa-\1 /m)[1],port=Number(log.match(/^QA SQL port=(\d+)/m)[1]);runs.push(id);
  const inspect=cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'});assert.equal(inspect.status,1);assert.match(inspect.stderr,/no such/i);
  assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
  assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
  console.log(`PASS ${run}:47 exact cases; owned container/root/listener absent.`);
}
assert.equal(new Set(runs).size,2);assert.match(read('QA/EVIDENCE/logs/phase-2b-course-sql-build.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.match(read('QA/ISSUES/BUG-DATA-0028.md'),/\| Status \| OPEN \|/);
for(const file of ['QA/REPORTS/PHASE_2B_COURSE_SQL_CHECK.md','QA/ISSUES/BUG-DATA-0028.md'])for(const m of read(file).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(file),target)),target);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:['ignore','pipe','pipe']});
console.log('PASS159 captures;2 QA dispatcher/wrapper changes +2 new QA files; all155 unaffected prior sources/normal binaries and controller-test binaries unchanged. Isolated runtime build pinned; two SQL47 runs/build PASS. Historical backend322/frontend233/TypeScript not rerun; lint/browser/device/concurrency/critical/issue/phase/release remain open. No Azure/commit/push.');
