const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_PRACTICE_IDENTITY_SOURCE_SNAPSHOT.json')),prior=JSON.parse(read('QA/REPORTS/PHASE_2B_SUBMISSION_IDENTITY_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.evidence,...snapshot.binaries,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
const intentional=['apps/api/Controllers/PracticeLogsController.cs','QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1','QA/00_QA_README.md','QA/REPORTS/PHASE_2_START_PLAN.md','QA/ISSUES/INDEX.md','QA/03_TEST_MATRIX.md','QA/ISSUES/BUG-DATA-0043.md'];
for(const x of [...prior.sources,...prior.evidence,...prior.binaries,...prior.normalBinaries])if(!intentional.includes(x.file))assert.equal(sha(x.file),x.sha256,'Accepted predecessor preserved '+x.file);
assert.deepEqual(snapshot.normalBinaries,prior.normalBinaries);
for(const x of snapshot.beforeSources.filter(x=>prior.sources.some(y=>y.file===x.file)))assert.equal(x.sha256,prior.sources.find(y=>y.file===x.file).sha256,'Accepted starting source '+x.file);
const baseline=read('QA/EVIDENCE/logs/phase-2b-practice-identity-sql-baseline.log'),log=read('QA/EVIDENCE/logs/phase-2b-practice-identity-sql.log');
assert.match(baseline,/BASELINE PASS:6 learner\/boundary\/lifecycle bypasses/);assert.equal([...baseline.matchAll(/^PRACTICEIDENTITY CASE .+ PASS\.$/gm)].length,6);
for(const mode of ['Missing','Foreign','Inactive','TooManyMinutes','FutureDate','lifecycle-overpost'])assert.ok(baseline.includes('CASE baseline-'+mode+' PASS.'));
assert.match(log,/REGRESSION PASS:37 cases/);assert.equal([...log.matchAll(/^PRACTICEIDENTITY CASE .+ PASS\.$/gm)].length,37);
for(const mode of ['Missing','Foreign','Inactive','Empty','NegativeMinutes','ZeroMinutes','TooManyMinutes','FutureDate','LongFocus','LongNotes'])assert.ok(log.includes('CASE reject-'+mode+' PASS.'));
for(const label of ['anonymous-denied','foreign-actor-denied','teacher-denied',...Array.from({length:7},(_,i)=>'binding-invalid-'+(13+i)),...['minimum-null','maximum-text','empty-text','whitespace','same-day-repeat','omitted-date','null-date','oldest-date'].map(x=>x+'-server-lifecycle'),...['trimmed','null','empty'].map(x=>'review-'+x+'-feedback'),'foreign-row-404','missing-row-404','anonymous-review-denied','foreign-actor-review-denied','teacher-review-denied','complete-scoped-ordered-readback-and-repeated-day-preservation'])assert.ok(log.includes('CASE '+label+' PASS.'),label);
for(const output of [baseline,log]){
 assert.match(output,/QA PracticeIdentity exit=0/);assert.match(output,/application migrations=82, identity migrations=7/);assert.match(output,/Negative cleanup check refused a mismatched database marker/);
 assert.match(output,/routes=313, controller method\/routes=302, framework Identity method\/routes=10, SHA256=562F95AD3C4514C384CCC11317E91174D0BAB7203E6AA7F162FCD74E8BE81A99/);
 const id=output.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(output.match(/^QA SQL port=(\d+)/m)[1]);
 assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);
 assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
 assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-practice-identity-backend-baseline.log'),/Failed:\s+12, Passed:\s+7, Skipped:\s+0, Total:\s+19/);
assert.match(read('QA/EVIDENCE/practice-identity/practice-identity-baseline.trx'),/<Counters total="19" executed="19" passed="7" failed="12"/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-practice-identity-suite.log'),/Failed:\s+0, Passed:\s+875, Skipped:\s+0, Total:\s+875/);
const trx=read('QA/EVIDENCE/practice-identity/practice-identity-suite.trx');assert.match(trx,/<Counters total="875" executed="875" passed="875"/);
assert.equal([...trx.matchAll(/<UnitTestResult [^>]*testName="AcademyDesk\.Api\.Tests\.PracticeLogCreationTests\./g)].length,26);
assert.equal([...trx.matchAll(/<UnitTestResult [^>]*testName="AcademyDesk\.Api\.Tests\.PracticeLogCreationTests\.Mvc_record_validation/g)].length,7);
for(const suffix of ['sql-build-baseline','sql-build'])assert.match(read(`QA/EVIDENCE/logs/phase-2b-practice-identity-${suffix}.log`),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.deepEqual(snapshot.checks,{backendTotal:875,newBackendCases:26,mvcValidationCases:7,baselineBackendCases:19,baselineBackendFailures:12,httpSqlCases:37,baselineHttpBypasses:6,frontend:'UNCHANGED',closure:'OPEN',browserDevice:'NOT RUN',devServices:'UNCHANGED'});
assert.match(read('QA/ISSUES/BUG-DATA-0043.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_PRACTICE_IDENTITY_REPAIR.md','QA/ISSUES/BUG-DATA-0043.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
assert.equal(cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS Practice identity:6 real HTTP baseline bypasses/12 controller failures;875 backend (26 new including7 MVC),37 real Identity/global-filter/HTTP-SQL checks. Active scoped learner, server lifecycle, optional fields, repeated days, separate review, auth/audits and fresh SQL verified. Accepted predecessor/frontend/evidence/binaries/normal assemblies/schema/routes and both owned cleanups preserved. No dev/Azure/deploy; browser/policy/linked/critical/release gates OPEN.');
