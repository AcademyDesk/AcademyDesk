// Consistency checks only; do not count these as additional business tests.
const fs=require('node:fs'),path=require('node:path'),c=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n'),sha=f=>c.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const before=JSON.parse(read('QA/EVIDENCE/platform-provisioning-before.json')),receipt=JSON.parse(read('QA/EVIDENCE/platform-provisioning-source-receipt.json'));
a.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),before.commit);a.equal(before.retainedBefore.length,662);
const allowed=['apps/api/Controllers/PlatformAcademiesController.cs','QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1','QA/ISSUES/BUG-DATA-0011.md','QA/03_TEST_MATRIX.md','QA/REPORTS/PHASE_2_START_PLAN.md'];
for(const e of before.retainedBefore)if(!allowed.includes(e.file))a.equal(sha(e.file),e.sha256,e.file);
for(const e of receipt.current)a.equal(sha(e.file),e.sha256,e.file);
for(const [copy,original] of [['PlatformAcademiesController.cs','apps/api/Controllers/PlatformAcademiesController.cs'],['Program.cs','QA/tools/SqlHarness/Program.cs'],['Run-ReconciledPayment.ps1','QA/tools/SqlHarness/Run-ReconciledPayment.ps1']])a.equal(sha('QA/EVIDENCE/platform-provisioning-before/'+copy),before.retainedBefore.find(x=>x.file===original).sha256);
const old=read('QA/EVIDENCE/platform-provisioning-before/PlatformAcademiesController.cs');
const originalRole='        if (!await roles.RoleExistsAsync("AcademyAdmin")) await roles.CreateAsync(new ApplicationRole { Name = "AcademyAdmin" });';
const boundary=`        // This platform route has no academyId, so the academy action filter cannot
        // own its boundary. Keep academy, Identity role/user and success audit atomic.
        var identityConnection = AcademyIdentityTransaction.Validate(db, identityDb);
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await using var identityTransaction = await AcademyIdentityTransaction.EnlistAsync(db, identityDb, transaction, identityConnection, token);
        if (!await roles.RoleExistsAsync("AcademyAdmin"))
        {
            var roleResult = await roles.CreateAsync(new ApplicationRole { Name = "AcademyAdmin" });
            if (!roleResult.Succeeded) return BadRequest(new { message = "The Academy Admin role could not be created. No changes were saved." });
        }`;
const originalUser='        if (!result.Succeeded) { db.Academies.Remove(academy); await db.SaveChangesAsync(token); return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); }\n        await users.AddToRoleAsync(admin, "AcademyAdmin");';
const checkedUser='        if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) + " No changes were saved." });\n        var assignment = await users.AddToRoleAsync(admin, "AcademyAdmin");\n        if (!assignment.Succeeded) return BadRequest(new { message = "The Academy Admin role could not be assigned. No changes were saved." });';
a.ok(old.includes(originalRole)&&old.includes(originalUser));
const expected=old.replace('RoleManager<ApplicationRole> roles) : ControllerBase','RoleManager<ApplicationRole> roles, IdentityDbContext identityDb) : ControllerBase').replace(originalRole,boundary).replace(originalUser,checkedUser).replace('        return Created($"/api/platform/academies/{academy.Id}"','        await transaction.CommitAsync(token);\n        return Created($"/api/platform/academies/{academy.Id}"');
a.equal(read('apps/api/Controllers/PlatformAcademiesController.cs'),expected,'Only scoped Identity dependency and atomic checked Onboard provisioning; all prior trial/configuration/status logic retained');
const oldProgram=read('QA/EVIDENCE/platform-provisioning-before/Program.cs');a.equal(read('QA/tools/SqlHarness/Program.cs'),oldProgram.replace('args[3] != "--audit-trial-duration"','args[3] != "--audit-platform-provisioning" && args[3] != "--audit-trial-duration"').replace('            if (auditMode == "--audit-trial-duration") { await VerifyTrialDurationAsync(factory, client); return; }','            if (auditMode == "--audit-trial-duration") { await VerifyTrialDurationAsync(factory, client); return; }\n            if (auditMode == "--audit-platform-provisioning") { await VerifyPlatformProvisioningAsync(factory, client, manifest); return; }'));
const expectedRunner=read('QA/EVIDENCE/platform-provisioning-before/Run-ReconciledPayment.ps1').split('\n').map(line=>{
 if(line.includes('ValidateSet')||line.includes(' -in @'))return line.replace("'TrialDuration'","'PlatformProvisioning','TrialDuration'");
 if(line.includes('qaArtifact = switch'))return line.replace("'TrialDuration' {","'PlatformProvisioning' { 'platform-provisioning-sql' }; 'TrialDuration' {");
 if(line.includes('TrialDuration')&&line.includes('qaSwitch ='))return line+"\n    if ($Module -eq 'PlatformProvisioning') { $qaSwitch = '--audit-platform-provisioning' }";
 return line.replace('TRIALDURATION)','TRIALDURATION|PLATFORMPROVISION)');
}).join('\n');a.equal(read('QA/tools/SqlHarness/Run-ReconciledPayment.ps1'),expectedRunner,'Only selected module dispatch/artifact/log prefix; all cleanup gates retained');
const log=read('QA/EVIDENCE/logs/phase-2b-platform-provisioning-sql.log'),prefix='PLATFORMPROVISION EVIDENCE ',cases=log.split('\n').filter(x=>x.startsWith(prefix)).map(x=>JSON.parse(x.slice(prefix.length)));
a.equal(cases.length,30);a.equal(new Set(cases.map(x=>x.label)).size,30);a.equal(log.split('\n').filter(x=>/^PLATFORMPROVISION CASE .* PASS\.$/.test(x)).length,30);
for(const [status,count] of [[201,7],[400,10],[500,7],[409,2],[403,3],[401,1]])a.equal(cases.filter(x=>x.status===status).length,count);
for(const e of cases){a.equal(e.exactSnapshotVerified,true);a.equal(c.createHash('sha256').update(e.response).digest('hex').toUpperCase(),e.bodyDigest);a.equal(e.Faults,e.mode==='none'?0:1,e.label);if(!e.succeeds){a.equal(e.beforeDigest,e.afterDigest,e.label);a.equal(e.newAcademy,null);a.equal(e.addedAudit,null);}else{a.notEqual(e.beforeDigest,e.afterDigest);a.equal(e.status,201);a.ok(e.newAcademy&&e.addedAudit);a.equal(JSON.parse(e.response).id,e.newAcademy);a.equal(e.roleAdditions,e.label==='missing-role-retry'?1:0);}}
const successes=cases.filter(x=>x.succeeds);a.equal(successes.length,7);a.equal(new Set(successes.map(x=>x.newAcademy)).size,7);a.equal(new Set(successes.map(x=>x.addedAudit)).size,7);a.equal(cases.filter(x=>!x.succeeds).length,23);
for(const group of ['missing-role','existing-role'])for(const mode of ['password-result','user-sql','assignment-result','assignment-sql','audit-sql']){
 const e=cases.find(x=>x.label===group+'-'+mode);a.ok(e);a.equal(e.status,mode.endsWith('-sql')?500:400);if(mode==='assignment-result'){a.equal(e.PersistedUserObserved,true);a.equal(e.PendingMembershipObserved,true);a.match(JSON.parse(e.response).message,/No changes were saved/);}if(group==='existing-role')a.ok(cases.find(x=>x.label===e.label+'-retry').succeeds);
}
for(const [mode,status] of [['role-result',400],['role-sql',500]])a.equal(cases.find(x=>x.label==='missing-role-'+mode).status,status);
for(const e of cases.filter(x=>x.status===500))a.equal(JSON.parse(e.response).message,'An unexpected server error occurred.');
a.match(log,/PLATFORMPROVISION ACCESS PASS: real provisioned-admin login200 and own-academy GET200/);a.match(log,/QA PlatformProvisioning exit=0/);a.match(log,/Negative cleanup check refused a mismatched database marker/);a.match(log,/application migrations=82, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);a.match(log,/Runtime inventory PASS: routes=313/);a.doesNotMatch(log,/Unhandled exception|Regression failed/);
const build=read('QA/EVIDENCE/logs/phase-2b-platform-provisioning-build.log');a.match(build,/Build succeeded/);a.match(build,/0 Warning\(s\)/);a.match(build,/0 Error\(s\)/);
a.match(read('QA/EVIDENCE/logs/phase-2b-platform-provisioning-backend.log'),/Failed:\s+0, Passed:\s+1019, Skipped:\s+0, Total:\s+1019/);a.match(read('QA/EVIDENCE/platform-provisioning-test-results/platform-provisioning.trx'),/<Counters total="1019" executed="1019" passed="1019"/);
const cleanup=JSON.parse(read('QA/EVIDENCE/platform-provisioning-cleanup.json'));a.equal(cleanup.runs.length,1);a.equal(cleanup.listenersRemaining,0);a.ok(cleanup.runs.every(x=>x.containerAbsent&&x.rootAbsent));a.equal(cleanup.runs[0].runId,'04bd2e7d6b6e412787e7c3ba2b85e95b');a.equal(cleanup.runs[0].port,58704);
a.match(read('QA/ISSUES/BUG-DATA-0011.md'),/\| Status \| OPEN \|/);for(const id of ['AUTH-PROVISION-001','PLATFORM-API-007']){const cols=read('QA/03_TEST_MATRIX.md').split('\n').find(x=>x.includes('| '+id+' |')).split('|');a.equal(cols[10].trim(),'PARTIAL PASS / OPEN');a.match(cols[11],/BUG-DATA-0011/);a.match(cols[9],/30 final HTTP-SQL/);}
const report='QA/REPORTS/PHASE_2B_PLATFORM_PROVISIONING_REPAIR.md';for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))a.ok(fs.existsSync(path.resolve(root,path.dirname(report),link[1])),link[1]);
a.match(read(report),/exact read-response academy ID is not independently asserted/);a.equal(receipt.baseline,'ACCEPTED SOURCE ONLY; NO NEW RUNTIME BASELINE');a.equal(receipt.closure,'OPEN');a.equal(receipt.azure,'UNCHANGED');a.equal(receipt.commitPushDeploy,'NOT DONE');a.equal(cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS platform provisioning consistency: accepted source baseline;30 final HTTP-SQL cases;1019 existing backend rerun;role/user/membership/audit failure rollback and retry;7 complete creations/audits,23 no-write rejections;662 accepted entries retained;owned cleanup. Other provisioning endpoints/Phase2B OPEN;Azure unchanged.');
