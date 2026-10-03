// Evidence consistency/preservation only, not additional runtime business cases.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n'),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const before=JSON.parse(read('QA/EVIDENCE/guardian-flags-before.json')),receipt=JSON.parse(read('QA/EVIDENCE/guardian-flags-source-receipt.json'));
const previous=JSON.parse(read(before.predecessor));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),previous.commit);
const changed=['apps/api/Controllers/PortalController.cs','QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1','QA/ISSUES/BUG-SEC-0005.md','QA/03_TEST_MATRIX.md','QA/REPORTS/PHASE_2_START_PLAN.md'];
for(const e of before.retainedBefore)if(!changed.includes(e.file))assert.equal(sha(e.file),e.sha256,'Accepted work retained: '+e.file);
for(const e of receipt.current)assert.equal(sha(e.file),e.sha256,e.file);
for(const [copy,original] of [['PortalController.cs','apps/api/Controllers/PortalController.cs'],['Program.cs','QA/tools/SqlHarness/Program.cs'],['Run-ReconciledPayment.ps1','QA/tools/SqlHarness/Run-ReconciledPayment.ps1']])assert.equal(sha('QA/EVIDENCE/guardian-flags-before/'+copy),before.retainedBefore.find(x=>x.file===original).sha256);
const old=read('QA/EVIDENCE/guardian-flags-before/PortalController.cs');
const history='            historyResources.Where(resource => resource.ClassSessionId == x.Session.Id)\n                .Select(resource => new PortalClassResource(resource.Title, resource.Description, resource.Type, resource.Url)).ToList()))';
const guarded='            canViewDocuments ? historyResources.Where(resource => resource.ClassSessionId == x.Session.Id)\n                .Select(resource => new PortalClassResource(resource.Title, resource.Description, resource.Type, resource.Url)).ToList() : []))';
const method='    public async Task<IActionResult> DownloadInvoice(Guid studentId, Guid invoiceId, CancellationToken token)\n    {\n        var user = await users.GetUserAsync(User);\n        if (user?.AcademyId is null || !await CanAccessStudent(user, studentId, token)) return Forbid();';
const guard=method.replace(')) return Forbid();',')\n            || (user.GuardianId.HasValue && (await ParentAccess(user, studentId, token))?.CanViewFinance != true)) return Forbid();');
assert.ok(old.includes(history)&&old.includes(method));
assert.equal(read('apps/api/Controllers/PortalController.cs'),old.replace(history,guarded).replace(method,guard),'Only invoice guard and nested resources changed; certificate and all other product code retained');
const oldProgram=read('QA/EVIDENCE/guardian-flags-before/Program.cs');
assert.equal(read('QA/tools/SqlHarness/Program.cs'),oldProgram.replace('args[3] != "--audit-certificate-family"','args[3] != "--audit-guardian-flags" && args[3] != "--audit-certificate-family"').replace('            if (auditMode == "--audit-certificate-family") { await VerifyCertificateFamilyAsync(factory, client); return; }','            if (auditMode == "--audit-certificate-family") { await VerifyCertificateFamilyAsync(factory, client); return; }\n            if (auditMode == "--audit-guardian-flags") { await VerifyGuardianFlagsAsync(factory, client); return; }'));
const expectedRunner=read('QA/EVIDENCE/guardian-flags-before/Run-ReconciledPayment.ps1').split('\n').map(line=>{
 if(line.includes('ValidateSet')||line.includes(' -in @'))return line.replace("'CertificateFamily'","'GuardianFlags','CertificateFamily'");
 if(line.includes('qaArtifact = switch'))return line.replace("'CertificateFamily' {","'GuardianFlags' { 'guardian-flags-sql-baseline' }; 'CertificateFamily' {");
 if(line.includes('CertificateFamily')&&line.includes('qaSwitch ='))return line+"\n    if ($Module -eq 'GuardianFlags') { $qaSwitch = '--audit-guardian-flags' }";
 if(line.includes('QA_CERTIFICATE_FAMILY_BASELINE'))return line+"\n            if ($Module -eq 'GuardianFlags' -and $env:QA_GUARDIAN_FLAGS_BASELINE -ne '1') { $qaArtifact = 'guardian-flags-sql-final' }";
 return line.replace('CERTFAMILY)','CERTFAMILY|GUARDIANFLAGS)');
}).join('\n');assert.equal(read('QA/tools/SqlHarness/Run-ReconciledPayment.ps1'),expectedRunner);
const parse=file=>read(file).split('\n').filter(x=>x.startsWith('GUARDIANFLAGS EVIDENCE ')).map(x=>JSON.parse(x.slice('GUARDIANFLAGS EVIDENCE '.length)));
const baseline=parse('QA/EVIDENCE/logs/phase-2b-guardian-flags-baseline.log');assert.equal(baseline.length,2);assert.ok(baseline.every(x=>x.status===200));
assert.match(baseline[0].response,/QA-FLAGS-OWN/);const leak=JSON.parse(baseline[1].response);assert.equal(leak.invoices.length,0);assert.equal(leak.resources.length,0);assert.match(JSON.stringify(leak.classHistory),/QA-FLAGS-PRIVATE/);
const finalFile='QA/EVIDENCE/logs/phase-2b-guardian-flags-final.log',final=parse(finalFile);assert.equal(final.length,55);assert.equal(new Set(final.map(x=>x.label)).size,55);assert.equal(read(finalFile).split('\n').filter(x=>/^GUARDIANFLAGS CASE .* PASS\.$/.test(x)).length,55);
assert.equal(final.filter(x=>x.status===200).length,19);assert.equal(final.filter(x=>x.status===401).length,2);assert.equal(final.filter(x=>x.status===403).length,31);assert.equal(final.filter(x=>x.status===404).length,3);
for(const e of [...baseline,...final]) {assert.equal(e.beforeDigest,e.afterDigest,e.label);assert.equal(crypto.createHash('sha256').update(e.response).digest('hex').toUpperCase(),e.bodyDigest,e.label);}
for(const f of [0,1])for(const d of [0,1])for(const a of [0,1]) {
 const key=`F${f}D${d}A${a}`,find=suffix=>{const e=final.find(x=>x.label===key+suffix);assert.ok(e);return e;};
 assert.equal(find('-invoice').status,f?200:403);assert.equal(find('-certificate').status,d?200:403);
 const detail=JSON.parse(find('-details').response);assert.equal(detail.invoices.length,f);assert.equal(detail.resources.length,d?3:0);assert.equal(detail.certificates.length,d);assert.equal(detail.classHistory.length,a);
 if(a){assert.equal(detail.classHistory[0].resources.length,d?2:0);assert.equal(detail.classHistory[0].attendanceStatus,'Present');assert.equal(detail.classHistory[0].status,'Completed');}
 if(!d)assert.doesNotMatch(find('-details').response,/https:\/\/example.invalid\/qa\/|QA-FLAGS-CERT/);
}
for(const file of ['phase-2b-guardian-flags-baseline.log','phase-2b-guardian-flags-final.log']) {
 const log=read('QA/EVIDENCE/logs/'+file);assert.match(log,/QA GuardianFlags exit=0/);assert.match(log,/Negative cleanup check refused a mismatched database marker/);assert.match(log,/application migrations=82, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);assert.doesNotMatch(log,/Unhandled exception|Regression failed/);
}
for(const file of ['phase-2b-guardian-flags-baseline-build.log','phase-2b-guardian-flags-final-build.log']){const log=read('QA/EVIDENCE/logs/'+file);assert.match(log,/Build succeeded/);assert.match(log,/0 Warning\(s\)/);assert.match(log,/0 Error\(s\)/);}
assert.match(read('QA/EVIDENCE/logs/phase-2b-guardian-flags-backend.log'),/Failed:\s+0, Passed:\s+1019, Skipped:\s+0, Total:\s+1019/);
assert.match(read('QA/EVIDENCE/guardian-flags-test-results/guardian-flags.trx'),/<Counters total="1019" executed="1019" passed="1019"/);
const cleanup=JSON.parse(read('QA/EVIDENCE/guardian-flags-cleanup.json'));assert.equal(cleanup.runs.length,2);assert.equal(cleanup.listenersRemaining,0);assert.ok(cleanup.runs.every(x=>x.containerAbsent&&x.rootAbsent));
const report='QA/REPORTS/PHASE_2B_GUARDIAN_FLAGS_REPAIR.md';for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))assert.ok(fs.existsSync(path.resolve(root,path.dirname(report),link[1])),link[1]);
assert.match(read('QA/ISSUES/BUG-SEC-0005.md'),/\| Status \| OPEN \|/);
for(const id of ['SECURITY-GUARDIAN-002','FAMILY-API-008','FAMILY-API-010'])assert.match(read('QA/03_TEST_MATRIX.md').split('\n').find(x=>x.includes(id)),/PARTIAL PASS \/ OPEN/);
assert.equal(receipt.closure,'OPEN');assert.equal(receipt.azure,'UNCHANGED');assert.equal(receipt.commitPushDeploy,'NOT DONE');
assert.equal(cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS guardian permission consistency: two real baseline disclosures;55 real Identity/HTTP-SQL cases;1019 existing backend rerun;two narrow product edits;accepted evidence/source/normal binaries retained;owned cleanup. Phase2B/release OPEN;Azure unchanged.');
