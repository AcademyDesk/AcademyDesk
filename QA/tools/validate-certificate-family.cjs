// Consistency and preservation; not an additional runtime business-test count.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const before=JSON.parse(read('QA/EVIDENCE/certificate-family-before.json')),receipt=JSON.parse(read('QA/EVIDENCE/certificate-family-source-receipt.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),before.commit);assert.equal(receipt.commit,before.commit);
assert.equal(before.retainedBefore.length,495);
for(const e of before.retainedBefore)if(!before.allowedChanges.includes(e.file))assert.equal(sha(e.file),e.sha256,'Accepted predecessor retained: '+e.file);
for(const e of receipt.current)assert.equal(sha(e.file),e.sha256,e.file);
for(const [copy,original] of [['QA/EVIDENCE/certificate-family-portal-before.cs.txt','apps/api/Controllers/PortalController.cs'],['QA/EVIDENCE/certificate-family-harness-before.cs.txt','QA/tools/SqlHarness/Program.cs'],['QA/EVIDENCE/certificate-family-runner-before.ps1.txt','QA/tools/SqlHarness/Run-ReconciledPayment.ps1']])assert.equal(sha(copy),before.retainedBefore.find(x=>x.file===original).sha256,copy);
const oldPortal=read('QA/EVIDENCE/certificate-family-portal-before.cs.txt'),portal=read('apps/api/Controllers/PortalController.cs');
const start='    public async Task<IActionResult> DownloadCertificate(',end='    [HttpGet("students/{studentId:guid}/leave-requests")]';
function outside(s){const a=s.indexOf(start),b=s.indexOf(end,a);assert.ok(a>=0&&b>a);return s.slice(0,a)+'REPAIRED METHOD\n'+s.slice(b);}
assert.equal(outside(portal),outside(oldPortal),'Only DownloadCertificate product method changed');
assert.match(portal.slice(portal.indexOf(start),portal.indexOf(end)),/CanViewDocuments != true/);assert.match(portal.slice(portal.indexOf(start),portal.indexOf(end)),/student.FirstName.*student.LastName/);
const parse=f=>read(f).split(/\r?\n/).filter(x=>x.startsWith('CERTFAMILY EVIDENCE ')).map(x=>JSON.parse(x.slice(20)));
const baselineLog=read('QA/EVIDENCE/logs/phase-2b-certificate-family-baseline-resumed.log'),baseline=parse('QA/EVIDENCE/logs/phase-2b-certificate-family-baseline-resumed.log');
assert.equal(baseline.length,2);assert.equal(baseline[0].status,200);assert.match(baseline[0].response,/Account parent1 &lt;not learner&gt;/);assert.doesNotMatch(baseline[0].response,/<h2>Learner/);
assert.equal(baseline[1].status,200);assert.match(baseline[1].response,/QA-FAMILY-ISSUED-0/);for(const row of baseline)assert.equal(row.beforeDigest,row.afterDigest);
assert.match(baselineLog,/no passing repair cases/);
const finalLog=read('QA/EVIDENCE/logs/phase-2b-certificate-family-final.log'),rows=parse('QA/EVIDENCE/logs/phase-2b-certificate-family-final.log');
assert.equal(rows.length,25);assert.equal(new Set(rows.map(x=>x.label)).size,25);
assert.match(finalLog,/CERTFAMILY REGRESSION PASS: 25 cases/);assert.equal(finalLog.split(/\r?\n/).filter(x=>x.startsWith('CERTFAMILY CASE ')).length,25);
assert.deepEqual(rows.reduce((a,x)=>(a[x.status]=(a[x.status]||0)+1,a),{}),{200:7,401:1,403:13,404:4});
for(const row of rows){assert.equal(row.beforeDigest,row.afterDigest,row.label);assert.match(row.beforeDigest,/^[0-9A-F]{64}$/);assert.equal(row.bodyDigest,crypto.createHash('sha256').update(row.response).digest('hex').toUpperCase());
 if(row.status===200){assert.equal(row.type,'text/html');assert.match(row.file,/^QA-FAMILY-ISSUED-\d\.html$/);assert.match(row.response,/<h2>Learner &lt;&amp; O&#39;Neil<\/h2>/);assert.match(row.response,/Award &lt;script&gt; &amp; &quot;quoted&quot;/);assert.match(row.response,/29 Feb 2020/);assert.doesNotMatch(row.response,/Account |<script>/);}else{assert.notEqual(row.type,'text/html');assert.doesNotMatch(row.response,/This certifies that/);}
}
for(const label of ['identity-learner','identity-parent1','identity-parent2','other-parent-still-permitted','documents-only-permission','status-case-issued','status-case-iSsUeD'])assert.equal(rows.find(x=>x.label===label).status,200);
for(const label of ['deny-restricted','permission-revoked-after-login','link-revoked-after-login','portal-flag-off','inactive-child-learner','inactive-child-parent1'])assert.equal(rows.find(x=>x.label===label).status,403);
for(const log of [baselineLog,finalLog]){assert.match(log,/QA CertificateFamily exit=0/);assert.match(log,/Negative cleanup check refused a mismatched database marker/);assert.match(log,/application migrations=82, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);assert.doesNotMatch(log,/Unhandled exception|Regression failed/);}
for(const file of ['phase-2b-certificate-family-baseline-build.log','phase-2b-certificate-family-final-build.log']){const log=read('QA/EVIDENCE/logs/'+file);assert.match(log,/Build succeeded/);assert.match(log,/0 Warning\(s\)/);assert.match(log,/0 Error\(s\)/);}
assert.match(read('QA/EVIDENCE/logs/phase-2b-certificate-family-backend.log'),/Failed:\s+0, Passed:\s+1019, Skipped:\s+0, Total:\s+1019/);
assert.match(read('QA/EVIDENCE/certificate-family-test-results/certificate-family.trx'),/<Counters total="1019" executed="1019" passed="1019"/);
const launch=JSON.parse(read('QA/EVIDENCE/certificate-family-initial-launch-cleanup.json'));assert.equal(launch.endpointTestsStarted,false);assert.equal(launch.ownedContainerRemoved,true);
assert.match(launch.failure,/harness DLL absent/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-certificate-family-baseline.log'),/QA CertificateFamily run=10ba384a18e542ffb221dda8b913f56e/);
const cleanup=JSON.parse(read('QA/EVIDENCE/certificate-family-cleanup.json'));assert.equal(cleanup.listenersRemaining,0);assert.equal(cleanup.runs.length,3);assert.ok(cleanup.runs.every(x=>x.containerAbsent&&x.rootAbsent));
for(const f of ['QA/ISSUES/BUG-DATA-0014.md','QA/ISSUES/BUG-SEC-0005.md'])assert.match(read(f),/\| Status \| OPEN \|/);
const report='QA/REPORTS/PHASE_2B_CERTIFICATE_FAMILY_DOWNLOAD_REPAIR.md';for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))assert.ok(fs.existsSync(path.resolve(root,path.dirname(report),link[1])),link[1]);
for(const id of ['SECURITY-GUARDIAN-002','FAMILY-DOCUMENT-001','FAMILY-API-011'])assert.match(read('QA/03_TEST_MATRIX.md').split(/\r?\n/).find(x=>x.includes(id)),/PARTIAL PASS \/ OPEN/);
assert.equal(receipt.closure,'OPEN');assert.equal(receipt.azure,'UNCHANGED');assert.equal(receipt.commitPushDeploy,'NOT DONE');
assert.notEqual(sha('.build-check/certificate-family-sql/bin/AcademyDesk.Api/debug/AcademyDesk.Api.dll'),sha('.build-check/certificate-family-sql-final/bin/AcademyDesk.Api/debug/AcademyDesk.Api.dll'));
const diff=cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'});assert.equal(diff.status,0,diff.stdout.slice(0,400));
console.log('PASS family certificate consistency: two accepted defects reproduced,25 real auth/HTTP-SQL unchanged-snapshot cases,1019 existing backend rerun,method-only repair,prior source/evidence/binary preservation and exact owned cleanup. HTML not PDF/device/full-portal/critical acceptance; issues/Phase2B/release OPEN; Azure unchanged.');
