// Scoped local closure evidence; no browser/device/release certification.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const s=JSON.parse(read('QA/REPORTS/PHASE_2B_YEAR_CLOSURE_SOURCE_SNAPSHOT.json')),p=JSON.parse(read('QA/REPORTS/'+s.priorSnapshot));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),s.commit);
assert.equal(s.records.length,167);assert.equal(new Set(s.records.map(x=>x.file)).size,167);
for(const x of [...s.records,...s.assemblies,...s.historicalAssemblies,...s.testAssemblies,...s.runtimeAssemblies])assert.equal(sha(x.file),x.sha256,x.file);
assert.deepEqual(s.assemblies,p.assemblies);
assert.deepEqual(p.records.filter(x=>sha(x.file)!==x.sha256).map(x=>x.file).sort(),['QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1'].sort());
assert.deepEqual(s.changedFromPrevious.slice().sort(),['QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1'].sort());
const old=new Set(p.records.map(x=>x.file));assert.deepEqual(s.records.filter(x=>!old.has(x.file)).map(x=>x.file).sort(),s.added.slice().sort());
assert.equal(s.controllerBefore.sha256,'6fe3e25d351dd0b88289863e32058e38a4c64024972cf4cbbd8f1c1776c1c150');assert.notEqual(sha(s.controllerBefore.file),s.controllerBefore.sha256);
const trx=read('QA/EVIDENCE/year-closure/year-closure-suite.trx'),results=[...trx.matchAll(/<UnitTestResult\b[^>]*\boutcome="([^"]+)"/g)].map(x=>x[1]);
assert.equal(results.length,363);assert.ok(results.every(x=>x==='Passed'));assert.equal([...trx.matchAll(/<UnitTestResult\b[^>]*testName="AcademyDesk.Api.Tests.AcademicYearClosureTests\./g)].length,22);
assert.match(read('QA/EVIDENCE/logs/phase-2b-year-closure-suite.log'),/Passed!\s+- Failed:\s+0, Passed:\s+363, Skipped:\s+0, Total:\s+363/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-year-closure-baseline.log'),/Failed!\s+- Failed:\s+5, Passed:\s+17, Skipped:\s+0, Total:\s+22/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-year-closure-sql-verified-build.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
const ids=[];
for(const n of [1,2]){
 const log=read(`QA/EVIDENCE/logs/phase-2b-year-closure-sql-verified-run${n}.log`);
 assert.equal([...log.matchAll(/^YEARCLOSURE CASE .+ PASS\.$/gm)].length,38);assert.match(log,/YEARCLOSURE REGRESSION PASS:38 cases/);assert.match(log,/QA YearClosure exit=0/);
 for(const v of [1,2])assert.equal([...log.matchAll(new RegExp(`^YEARCLOSURE SQL parent-lock WAIT=${v} observed before release\\.$`,'gm'))].length,2);
 for(const label of ['closed-parent','stale-closed-parent409','term-audit-fault500-rollback','close-audit-fault500-rollback','term-after-fault-lock-released','close-after-fault-flags-recovered','platform-term-owned-transaction','platform-close-owned-transaction','platform-closed-parent409'])assert.ok(log.includes('YEARCLOSURE CASE '+label),label);
 assert.match(log,/YEARCLOSURE concurrent launchCloseFirst=False winner=create invariant=PASS/);assert.match(log,/YEARCLOSURE concurrent launchCloseFirst=True winner=close invariant=PASS/);
 assert.match(log,/AUDIT FAULT enabled/);assert.match(log,/AUDIT FAULT removed/);assert.match(log,/HTTP controls PASS/);assert.match(log,/Tenant controls PASS/);
 assert.match(log,/Runtime inventory PASS: routes=311, controller method\/routes=300, framework Identity method\/routes=10, SHA256=EFEF7739163F3DF5FF695E0F09747B9CD9734C7CFB6055D9B22A1FA6713B5D35/);
 assert.match(log,/Negative cleanup check refused a mismatched database marker/);assert.match(log,/PASS: application migrations=80, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);
 const id=log.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(log.match(/^QA SQL port=(\d+)/m)[1]);ids.push(id);
 const inspect=cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'});assert.equal(inspect.status,1);assert.match(inspect.stderr,/no such/i);
 assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
 console.log(`PASS SQL run${n}:38 exact cases; both launch-order winners observed after real SQL WAIT1/2; owned cleanup confirmed.`);
}
assert.equal(new Set(ids).size,2);assert.match(read('QA/ISSUES/BUG-DATA-0030.md'),/\| Status \| OPEN \|/);
const failed=read('QA/EVIDENCE/logs/phase-2b-year-closure-sql-run1.log');assert.match(failed,/InvalidCastException[\s\S]*System.Int16[\s\S]*System.Int32/);
const failedInspect=cp.spawnSync('docker',['inspect','academydesk-qa-98350dbb20774ca7be99aa5596958e56'],{encoding:'utf8'});assert.equal(failedInspect.status,1);assert.match(failedInspect.stderr,/no such/i);
assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA','98350dbb20774ca7be99aa5596958e56')),false);
assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command','@(Get-NetTCPConnection -LocalPort 65290 -State Listen -ErrorAction SilentlyContinue).Count'],{encoding:'utf8'}).trim(),'0');
assert.match(read('QA/EVIDENCE/logs/phase-2b-year-closure-sql-final-run1.log'),/Requests did not wait on the held SQL parent/);
const interruptedInspect=cp.spawnSync('docker',['inspect','academydesk-qa-d53f248916924adcbb83721e0b893fec'],{encoding:'utf8'});assert.equal(interruptedInspect.status,1);assert.match(interruptedInspect.stderr,/no such/i);
assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA','d53f248916924adcbb83721e0b893fec')),false);
assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command','@(Get-NetTCPConnection -LocalPort 49690 -State Listen -ErrorAction SilentlyContinue).Count'],{encoding:'utf8'}).trim(),'0');
for(const f of ['QA/REPORTS/PHASE_2B_YEAR_CLOSURE_REPAIR.md','QA/ISSUES/BUG-DATA-0030.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const t=m[1].split('#')[0];if(t&&!/^https?:/.test(t))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),t)),t);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:['ignore','pipe','pipe']});
console.log('PASS167 source captures:161 prior unchanged,2 QA dispatcher/wrapper changes,4 first captures including1 repaired controller. Normal/historical binaries unchanged; isolated final binaries/TRX pinned. Baseline3 product failures+2 boundary assertions retained; backend363/363 and two SQL38 runs PASS. Frontend/browser/device/other lifecycle/critical/issue/phase/release remain OPEN; no dev DB/Azure/commit/push.');
