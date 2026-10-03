// Bounded graph repair evidence, not whole-module/release acceptance.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const s=JSON.parse(read('QA/REPORTS/PHASE_2B_PREREQUISITE_SOURCE_SNAPSHOT.json')),p=JSON.parse(read('QA/REPORTS/'+s.priorSnapshot));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),s.commit);
assert.equal(s.records.length,163);assert.equal(new Set(s.records.map(x=>x.file)).size,163);
for(const row of [...s.records,...s.assemblies,...s.testAssemblies,...s.runtimeAssemblies,...s.historicalAssemblies])assert.equal(sha(row.file),row.sha256,row.file);
assert.deepEqual(s.assemblies,p.assemblies);
const changed=p.records.filter(x=>sha(x.file)!==x.sha256).map(x=>x.file).sort();assert.deepEqual(changed,s.changedFromPrevious.slice().sort());
assert.deepEqual(changed,['QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1'].sort());
const old=new Set(p.records.map(x=>x.file));assert.deepEqual(s.records.filter(x=>!old.has(x.file)).map(x=>x.file).sort(),s.added.slice().sort());
assert.equal(s.controllerBefore.sha256,'aed066886c50f7a413a614d2dff6e965bf9e75ab34e9495b482c462bddb94b39');
assert.notEqual(sha(s.controllerBefore.file),s.controllerBefore.sha256);
const trx=read('QA/EVIDENCE/prerequisites/prerequisite-suite.trx'),results=[...trx.matchAll(/<UnitTestResult\b[^>]*\boutcome="([^"]+)"/g)].map(x=>x[1]);
assert.equal(results.length,341);assert.ok(results.every(x=>x==='Passed'));
assert.equal([...trx.matchAll(/<UnitTestResult\b[^>]*testName="AcademyDesk.Api.Tests.CoursePrerequisiteTests\./g)].length,19);
assert.match(read('QA/EVIDENCE/logs/phase-2b-prerequisite-suite.log'),/Passed!\s+- Failed:\s+0, Passed:\s+341, Skipped:\s+0, Total:\s+341/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-prerequisite-baseline.log'),/Failed!\s+- Failed:\s+6, Passed:\s+13, Skipped:\s+0, Total:\s+19/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-prerequisite-sql-build-final.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
const ids=[];
function Absent(id,port){
 const inspect=cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'});assert.equal(inspect.status,1);assert.match(inspect.stderr,/no such/i);
 assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
 assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
}
for(const n of [1,2]){
 const log=read(`QA/EVIDENCE/logs/phase-2b-prerequisite-sql-final-run${n}.log`);
 assert.equal([...log.matchAll(/^PREREQUISITE CASE .+ PASS\.$/gm)].length,32);
 assert.match(log,/PREREQUISITE REGRESSION PASS:32 cases/);assert.match(log,/QA CoursePrerequisites exit=0/);
 assert.equal([...log.matchAll(/^PREREQUISITE SQL application-lock WAIT=2 observed before release\.$/gm)].length,3);
 for(const name of ['concurrent-reciprocal','concurrent-duplicate','concurrent-three-cycle','other-academy-lock-independent','lock-timeout503-no-write','audit-fault500-rollback','post-fault-lock-released','platform-own-transaction','platform-cycle-rejected'])assert.ok(log.includes('PREREQUISITE CASE '+name),name);
 assert.match(log,/AUDIT FAULT enabled/);assert.match(log,/AUDIT FAULT removed/);
 assert.match(log,/HTTP controls PASS/);assert.match(log,/Tenant controls PASS/);assert.match(log,/Runtime inventory PASS: routes=311, controller method\/routes=300, framework Identity method\/routes=10, SHA256=EFEF7739163F3DF5FF695E0F09747B9CD9734C7CFB6055D9B22A1FA6713B5D35/);
 assert.match(log,/Negative cleanup check refused a mismatched database marker/);assert.match(log,/PASS: application migrations=80, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);
 const id=log.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(log.match(/^QA SQL port=(\d+)/m)[1]);ids.push(id);Absent(id,port);
 console.log(`PASS final run${n}:32 cases;3 deterministic SQL waiting pairs; owned container/root/listener absent.`);
}
assert.equal(new Set(ids).size,2);Absent('33b3681a755b48b3a468a1456fd61c80',61699);
assert.match(read('QA/EVIDENCE/logs/phase-2b-prerequisite-sql-run1.log'),/first-edge: expected 200, actual 403/);
assert.match(read('QA/ISSUES/BUG-DATA-0029.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_PREREQUISITE_REPAIR.md','QA/ISSUES/BUG-DATA-0029.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const t=m[1].split('#')[0];if(t&&!/^https?:/.test(t))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),t)),t);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:['ignore','pipe','pipe']});
console.log('PASS163 source captures:157 prior unchanged,2 QA dispatcher/wrapper changes,4 first captures including1 repaired application controller. Normal/historical binaries unchanged, isolated final tests/runtime pinned. Baseline5 cycle failures+1 boundary expectation retained; backend341/341 and two SQL32 runs PASS; initial fixture403 excluded and cleaned. Frontend/browser/device/legacy-cycle repair/all-linked/critical/phase/release remain unverified. No dev DB/Azure/commit/push.');
