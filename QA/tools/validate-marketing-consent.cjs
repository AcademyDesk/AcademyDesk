const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_MARKETING_CONSENT_SOURCE_SNAPSHOT.json')),prior=JSON.parse(read('QA/REPORTS/PHASE_2B_TEMPLATE_STATE_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.evidence,...snapshot.binaries,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
const intentional=['apps/api/Controllers/NotificationsController.cs','QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1','QA/00_QA_README.md','QA/REPORTS/PHASE_2_START_PLAN.md','QA/ISSUES/INDEX.md','QA/03_TEST_MATRIX.md'];
for(const x of [...prior.sources,...prior.evidence,...prior.binaries,...prior.normalBinaries])if(!intentional.includes(x.file))assert.equal(sha(x.file),x.sha256,'Accepted predecessor preserved '+x.file);
assert.deepEqual(snapshot.normalBinaries,prior.normalBinaries);
for(const x of snapshot.beforeSources.filter(x=>prior.sources.some(y=>y.file===x.file)))assert.equal(x.sha256,prior.sources.find(y=>y.file===x.file).sha256,'Accepted starting source '+x.file);
const baseline=read('QA/EVIDENCE/logs/phase-2b-marketing-consent-sql-baseline.log'),log=read('QA/EVIDENCE/logs/phase-2b-marketing-consent-sql.log');
assert.match(baseline,/MARKETING BASELINE PASS:6 opt-out bypasses reproduced/);assert.equal([...baseline.matchAll(/^MARKETING CASE .+ PASS\.$/gm)].length,6);
assert.match(log,/MARKETING REGRESSION PASS:190 cases/);assert.equal([...log.matchAll(/^MARKETING CASE .+ PASS\.$/gm)].length,190);
for(const channel of ['Email','WhatsApp']){
 for(const category of ['Marketing','Utility','Authentication','Transactional'])for(const consent of ['True','False'])for(const marketing of ['True','False'])for(const connection of ['Missing','Disabled','Unsecured','Ready'])assert.ok(log.includes(`CASE ${channel}-${category}-consent-${consent}-marketing-${marketing}-${connection} PASS.`));
 for(const consent of ['True','False'])for(const marketing of ['True','False'])assert.ok(log.includes(`CASE ${channel}-Guardian-consent-${consent}-marketing-${marketing} PASS.`));
 for(const target of ['Queued','RetryRequested']){
  for(const label of ['committed-marketing-opt-out','committed-channel-opt-out','utility-requeue'])assert.ok(log.includes(`CASE ${channel}-${label}-${target} PASS.`));
  for(const state of ['Disabled','Inactive','Foreign'])assert.ok(log.includes(`CASE ${channel}-template-${state}-${target} PASS.`));
 }
 for(const label of ['preference-Missing','preference-Foreign','preference-OtherType','legacy-mArKeTiNg','legacy-Marketing','legacy-template-channel','cancel-still-allowed','manual-not-classified-as-marketing','utility-control'])assert.ok(log.includes(`CASE ${channel}-${label} PASS.`));
}
for(const label of ['InApp-retry-policy-unchanged','anonymous-denied','foreign-actor-denied','teacher-denied','owned-full-readback','foreign-full-readback'])assert.ok(log.includes(`CASE ${label} PASS.`));
for(const output of [baseline,log]){
 assert.match(output,/QA MarketingConsent exit=0/);assert.match(output,/application migrations=82, identity migrations=7/);assert.match(output,/Negative cleanup check refused a mismatched database marker/);
 assert.match(output,/routes=313, controller method\/routes=302, framework Identity method\/routes=10, SHA256=562F95AD3C4514C384CCC11317E91174D0BAB7203E6AA7F162FCD74E8BE81A99/);
 const id=output.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(output.match(/^QA SQL port=(\d+)/m)[1]);
 assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
 assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-marketing-consent-backend-baseline.log'),/Failed:\s+40, Passed:\s+138, Skipped:\s+0, Total:\s+178/);
assert.match(read('QA/EVIDENCE/marketing-consent/marketing-consent-baseline.trx'),/<Counters total="178" executed="178" passed="138" failed="40"/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-marketing-consent-suite.log'),/Failed:\s+0, Passed:\s+813, Skipped:\s+0, Total:\s+813/);
const trx=read('QA/EVIDENCE/marketing-consent/marketing-consent-suite.trx');assert.match(trx,/<Counters total="813" executed="813" passed="813"/);assert.equal([...trx.matchAll(/<UnitTestResult [^>]*testName="AcademyDesk\.Api\.Tests\.MarketingConsentTests\./g)].length,180);
assert.match(read('QA/EVIDENCE/logs/phase-2b-marketing-consent-suite-initial.log'),/Passed:\s+811/);
for(const suffix of ['sql-build-baseline','sql-build'])assert.match(read(`QA/EVIDENCE/logs/phase-2b-marketing-consent-${suffix}.log`),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.deepEqual(snapshot.checks,{backendTotal:813,newBackendCases:180,baselineBackendCases:178,baselineBackendFailures:40,httpSqlCases:190,creationMatrixCases:128,baselineHttpBypasses:6,frontend:'UNCHANGED',closure:'OPEN',browserDevice:'NOT RUN',devServices:'UNCHANGED'});
assert.match(read('QA/ISSUES/BUG-SEC-0010.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_MARKETING_CONSENT_REPAIR.md','QA/ISSUES/BUG-SEC-0010.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
assert.equal(cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS Marketing consent:6 real HTTP baseline bypasses/40 controller failures;813 backend (180 new),190 real Identity/global-filter/HTTP-SQL cases including128 creation combinations and committed opt-out Queue/Retry no-write guards. Full stored/returned rows, consent precedence, settings/audits/scope preserved. Accepted frontend/prior sources/evidence/binaries/schema/owned cleanup verified. No dev/Azure/outbound/deploy; browser/races/category snapshot/future dispatcher/critical OPEN.');
