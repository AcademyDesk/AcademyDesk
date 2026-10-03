// Receipt/evidence consistency only, not additional business test counts.
const fs=require('node:fs'),path=require('node:path'),c=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n'),sha=f=>c.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const before=JSON.parse(read('QA/EVIDENCE/platform-billing-before.json')),receipt=JSON.parse(read('QA/EVIDENCE/platform-billing-source-receipt.json'));
a.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),before.commit);
const allowed=['apps/api/Controllers/PlatformControlController.cs','QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1','QA/ISSUES/BUG-DATA-0024.md','QA/03_TEST_MATRIX.md','QA/REPORTS/PHASE_2_START_PLAN.md'];
for(const e of before.retainedBefore)if(!allowed.includes(e.file))a.equal(sha(e.file),e.sha256,e.file);
for(const e of receipt.current)a.equal(sha(e.file),e.sha256,e.file);
for(const [copy,original] of [['PlatformControlController.cs','apps/api/Controllers/PlatformControlController.cs'],['Program.cs','QA/tools/SqlHarness/Program.cs'],['Run-ReconciledPayment.ps1','QA/tools/SqlHarness/Run-ReconciledPayment.ps1']])a.equal(sha('QA/EVIDENCE/platform-billing-before/'+copy),before.retainedBefore.find(x=>x.file===original).sha256);
const old=read('QA/EVIDENCE/platform-billing-before/PlatformControlController.cs');
const oldBalance='OutstandingBilling = await invoices.Where(x => x.Status == "Issued" || x.Status == "Overdue").SumAsync(x => (decimal?)x.Amount, token) ?? 0,';
const newBalance='OutstandingBilling = await invoices.Where(x => x.Status == "Issued" || x.Status == "Overdue" || x.Status == "Payment submitted").SumAsync(x => (decimal?)x.Amount, token) ?? 0,';
a.ok(old.includes(oldBalance));a.equal(read('apps/api/Controllers/PlatformControlController.cs'),old.replace(oldBalance,newBalance),'One outstanding predicate only; all writes/auth/contracts/deletion guard retained');
const oldProgram=read('QA/EVIDENCE/platform-billing-before/Program.cs');a.equal(read('QA/tools/SqlHarness/Program.cs'),oldProgram.replace('args[3] != "--audit-activity-deletion"','args[3] != "--audit-platform-billing" && args[3] != "--audit-activity-deletion"').replace('            if (auditMode == "--audit-activity-deletion") { await VerifyActivityDeletionAsync(factory, client); return; }','            if (auditMode == "--audit-activity-deletion") { await VerifyActivityDeletionAsync(factory, client); return; }\n            if (auditMode == "--audit-platform-billing") { await VerifyPlatformBillingAsync(factory, client); return; }'));
const expectedRunner=read('QA/EVIDENCE/platform-billing-before/Run-ReconciledPayment.ps1').split('\n').map(line=>{
 if(line.includes('ValidateSet')||line.includes(' -in @'))return line.replace("'ActivityDeletion'","'PlatformBilling','ActivityDeletion'");
 if(line.includes('qaArtifact = switch'))return line.replace("'ActivityDeletion' {","'PlatformBilling' { 'platform-billing-sql-baseline' }; 'ActivityDeletion' {");
 if(line.includes('ActivityDeletion')&&line.includes('qaSwitch ='))return line+"\n    if ($Module -eq 'PlatformBilling') { $qaSwitch = '--audit-platform-billing' }";
 if(line.includes('QA_ACTIVITY_DELETION_BASELINE'))return line+"\n            if ($Module -eq 'PlatformBilling' -and $env:QA_PLATFORM_BILLING_BASELINE -ne '1') { $qaArtifact = 'platform-billing-sql-final' }";
 return line.replace('ACTIVITYDELETE)','ACTIVITYDELETE|PLATFORMBILLING)');
}).join('\n');a.equal(read('QA/tools/SqlHarness/Run-ReconciledPayment.ps1'),expectedRunner);
const prefix='PLATFORMBILLING EVIDENCE ',parse=f=>read(f).split('\n').filter(x=>x.startsWith(prefix)).map(x=>JSON.parse(x.slice(prefix.length)));
const baselineFile='QA/EVIDENCE/logs/phase-2b-platform-billing-baseline.log',finalFile='QA/EVIDENCE/logs/phase-2b-platform-billing-final.log',baseline=parse(baselineFile),final=parse(finalFile);
a.equal(baseline.length,5);const gap=baseline.find(x=>x.label==='baseline-submitted-gap');a.ok(gap);a.equal(gap.billed,1000);a.equal(gap.collected,0);a.equal(gap.outstanding,0);a.equal(baseline.filter(x=>x.mutation!=='none').length,3);
a.equal(final.length,51);a.equal(new Set(final.map(x=>x.label)).size,51);a.equal(read(finalFile).split('\n').filter(x=>/^PLATFORMBILLING CASE .* PASS\.$/.test(x)).length,51);
for(const [status,count] of [[200,27],[201,1],[400,6],[403,13],[401,2],[404,2]])a.equal(final.filter(x=>x.status===status).length,count);
for(const e of [...baseline,...final]){
 a.equal(e.exactSnapshotVerified,true);a.equal(c.createHash('sha256').update(e.response).digest('hex').toUpperCase(),e.bodyDigest);
 if(e.mutation==='none'){a.equal(e.beforeDigest,e.afterDigest,e.label);a.equal(e.addedAudit,null);}else{a.notEqual(e.beforeDigest,e.afterDigest,e.label);a.ok(e.addedAudit);}
 if(e.billed!==null){const body=JSON.parse(e.response);a.equal(body.totalBilled,e.billed);a.equal(body.collectedBilling,e.collected);a.equal(body.outstandingBilling,e.outstanding);a.equal(body.overdueInvoices,e.overdue);if(e!==gap)a.equal(Math.round(e.billed*100),Math.round((e.collected+e.outstanding)*100));}
}
const writes=final.filter(x=>x.mutation!=='none');a.equal(writes.length,11);a.equal(new Set(writes.map(x=>x.addedAudit)).size,11);a.equal(writes.filter(x=>x.mutation==='create').length,1);a.equal(writes.filter(x=>x.mutation==='status').length,5);a.equal(writes.filter(x=>x.mutation==='submit').length,5);
a.equal(final.filter(x=>x.mutation==='none').length,40);a.equal(final.filter(x=>x.status>=400).length,23);
for(const label of ['submitted-still-outstanding','repeated-claim-not-collected','empty-reference-unpaid','overdue-submission-unpaid','draft-claim-is-unpaid']){const e=final.find(x=>x.label===label);a.equal(e.outstanding,1000);a.equal(e.collected,0);}
const aggregate=final.find(x=>x.label==='two-tenant-decimal-zero-aggregate');a.equal(aggregate.billed,1011.10);a.equal(aggregate.collected,4.44);a.equal(aggregate.outstanding,1006.66);a.equal(aggregate.overdue,1);
for(const file of [baselineFile,finalFile]){const log=read(file);a.match(log,/QA PlatformBilling exit=0/);a.match(log,/Negative cleanup check refused a mismatched database marker/);a.match(log,/application migrations=82, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);a.doesNotMatch(log,/Unhandled exception|Regression failed/);}
for(const file of ['baseline-build','final-build']){const log=read('QA/EVIDENCE/logs/phase-2b-platform-billing-'+file+'.log');a.match(log,/Build succeeded/);a.match(log,/0 Warning\(s\)/);a.match(log,/0 Error\(s\)/);}
a.match(read('QA/EVIDENCE/logs/phase-2b-platform-billing-backend.log'),/Failed:\s+0, Passed:\s+1019, Skipped:\s+0, Total:\s+1019/);a.match(read('QA/EVIDENCE/platform-billing-test-results/platform-billing.trx'),/<Counters total="1019" executed="1019" passed="1019"/);
const cleanup=JSON.parse(read('QA/EVIDENCE/platform-billing-cleanup.json'));a.equal(cleanup.runs.length,2);a.equal(cleanup.listenersRemaining,0);a.ok(cleanup.runs.every(x=>x.containerAbsent&&x.rootAbsent));
a.match(read('QA/ISSUES/BUG-DATA-0024.md'),/\| Status \| OPEN \|/);for(const id of ['PLATFORM-BALANCE-001','PLATFORM-API-002','PLATFORM-API-010'])a.match(read('QA/03_TEST_MATRIX.md').split('\n').find(x=>x.includes(id)),/PARTIAL PASS \/ OPEN/);
const report='QA/REPORTS/PHASE_2B_PLATFORM_BILLING_REPAIR.md';for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))a.ok(fs.existsSync(path.resolve(root,path.dirname(report),link[1])),link[1]);
a.equal(receipt.closure,'OPEN');a.equal(receipt.azure,'UNCHANGED');a.equal(receipt.commitPushDeploy,'NOT DONE');a.equal(cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS platform billing consistency: real baseline unverified deficit;51 HTTP-SQL cases;1019 existing backend rerun;one balance-predicate repair;11 exact invoice/audit writes and40 unchanged reads/denials;accepted source/evidence/binaries retained;owned cleanup. Wider gates OPEN;Azure unchanged.');
