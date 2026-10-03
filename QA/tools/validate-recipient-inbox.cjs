const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_RECIPIENT_INBOX_SOURCE_SNAPSHOT.json')),prior=JSON.parse(read('QA/REPORTS/PHASE_2B_COMMUNICATION_CHANNEL_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.evidence,...snapshot.binaries,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
const changed=['apps/api/Data/AcademyDeskDbContext.cs','QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1'];
for(const x of [...prior.sources,...prior.evidence,...prior.normalBinaries])if(!changed.includes(x.file))assert.equal(sha(x.file),x.sha256,'Prior repair preserved '+x.file);
assert.deepEqual(snapshot.normalBinaries,prior.normalBinaries);
for(const x of snapshot.beforeSources.filter(x=>prior.sources.some(y=>y.file===x.file)))assert.equal(x.sha256,prior.sources.find(y=>y.file===x.file).sha256,'Starting source matches accepted checkpoint');
const log=read('QA/EVIDENCE/logs/phase-2b-inbox-sql.log'),baseline=read('QA/EVIDENCE/logs/phase-2b-inbox-sql-baseline.log');
assert.match(baseline,/BASELINE:9 future\/cancelled\/blocked\/undelivered\/unknown rows exposed/);assert.match(baseline,/cancelled delivery state overwritten to Read; reproduced2 defects/);
assert.equal([...log.matchAll(/^INBOXLIFE CASE .+ PASS\.$/gm)].length,80);
for(const type of ['Student','Teacher','Guardian','Parent'])for(const part of ['Queued-InApp-True','Sent-Email-True','Cancelled-InApp-False','BlockedConsent-Email-False','AwaitingConnection-WhatsApp-False','Failed-Email-False','Queued-Email-False','RetryRequested-InApp-False','Sent-Unknown-False'])assert.ok(log.includes(`hidden-${type}-${part} PASS.`));
for(const part of ['foreign-academy-id','wrong-recipient','unknown-id','anonymous','admin-no-person','repeat-idempotent','concurrent-first-read-idempotent','future-becomes-due','cancelled-after-read','cancellation-retains-receipt-not-delivery-overwrite'])assert.ok(log.includes(`CASE ${part} PASS.`));
assert.match(log,/INBOXLIFE REGRESSION PASS:80 cases/);assert.match(log,/INBOXLIFE MIGRATION PASS:upgrade preserves existing notification\/legacy Read/);
for(const output of [log,baseline]){
 assert.match(output,/QA InboxLifecycle exit=0/);assert.match(output,/application migrations=82, identity migrations=7/);assert.match(output,/Negative cleanup check refused a mismatched database marker/);assert.match(output,/SHA256=74CB2F226EBAFF8881F6CF80D6F648310A78B07D79A0A6D9E04D4ED858544FF3/);
 const id=output.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(output.match(/^QA SQL port=(\d+)/m)[1]);
 assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
 assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-inbox-ui-baseline.log'),/tests 14[\s\S]*pass 1[\s\S]*fail 13/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-inbox-ui.log'),/tests 14[\s\S]*pass 14[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-inbox-suite.log'),/Failed:\s+0, Passed:\s+593, Skipped:\s+0, Total:\s+593/);
const trx=read('QA/EVIDENCE/inbox/inbox-suite.trx');assert.match(trx,/<Counters total="593" executed="593" passed="593"/);assert.equal([...trx.matchAll(/<UnitTestResult [^>]*testName="AcademyDesk\.Api\.Tests\.RecipientNotificationVisibilityTests\./g)].length,36);
assert.match(read('QA/EVIDENCE/logs/phase-2b-inbox-typecheck.log'),/TypeScript PASS/);assert.match(read('QA/EVIDENCE/logs/phase-2b-inbox-lint.log'),/0 errors, 2 warnings/);assert.match(read('QA/EVIDENCE/logs/phase-2b-inbox-sql-build.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);assert.match(read('QA/EVIDENCE/logs/phase-2b-inbox-model.log'),/No changes have been made to the model since the last migration/);
assert.deepEqual(snapshot.checks,{backendTotal:593,newBackendCases:36,controlledTsxCases:14,httpSqlCases:80,baselineSqlDefects:2,baselineUiFailures:13,closure:'OPEN',browserDevice:'NOT RUN',normalDevMigration:'NOT RUN'});
assert.match(read('QA/ISSUES/BUG-FUNC-0027.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_RECIPIENT_INBOX_REPAIR.md','QA/ISSUES/BUG-FUNC-0027.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
assert.equal(cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS Recipient inbox:2 API baseline defects/13 UI failures;593 backend (36 new),14 controlled TSX,80 real Identity-HTTP-SQL; migration upgrade/down/reapply, due/status/tenant gates, separate account receipts, concurrent first reads/no delivery mutation. Prior sources/evidence/normal assemblies and both owned cleanups verified. Normal dev DB/services/Azure/deploy untouched; browser/lifecycle/critical/release OPEN.');
