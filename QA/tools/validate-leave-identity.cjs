const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_LEAVE_IDENTITY_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.binaries,...snapshot.evidence,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
for(const name of ['SCHEDULE_DEFAULTS','CALENDAR_DETAILS','ATTENDANCE_NOTES','ASSESSMENT_OPTIONS']) {
 const prior=JSON.parse(read(`QA/REPORTS/PHASE_2B_${name}_SOURCE_SNAPSHOT.json`));
 for(const x of [...prior.sources,...(prior.unchangedSources??[])].filter(x=>x.file.startsWith('apps/')&&x.file!=='apps/web/src/app/schedule/page.tsx'))assert.equal(sha(x.file),x.sha256,'Prior application source '+x.file);
 for(const x of snapshot.normalBinaries)assert.equal(x.sha256,prior.normalBinaries.find(y=>y.file===x.file).sha256,'Normal assembly changed');
}
const prior=JSON.parse(read('QA/REPORTS/PHASE_2B_SCHEDULE_DEFAULTS_SOURCE_SNAPSHOT.json'));assert.equal(sha('apps/web/src/app/schedule/page.tsx'),prior.sources.find(x=>x.file==='apps/web/src/app/schedule/page.tsx').sha256);
const suite=read('QA/EVIDENCE/logs/phase-2b-leave-identity-suite.log');assert.match(suite,/Failed:\s+0, Passed:\s+477, Skipped:\s+0, Total:\s+477/);
const trx=read('QA/EVIDENCE/leave-identity/leave-identity-suite.trx');assert.match(trx,/<Counters total="477" executed="477" passed="477"/);assert.equal([...trx.matchAll(/<UnitTestResult [^>]*testName="AcademyDesk\.Api\.Tests\.LeaveIdentityTests\./g)].length,51);
const log=read('QA/EVIDENCE/logs/phase-2b-leave-identity-sql.log');const cases=[...log.matchAll(/^LEAVEIDENTITY CASE (.+) PASS\.$/gm)].map(x=>x[1]);assert.equal(cases.length,63);assert.equal(new Set(cases).size,63);
for(const label of ['student-foreign-extra','teacher-foreign-extra','student-wrong-entity','teacher-wrong-entity','student-empty-extra','teacher-empty-extra','missing-type-binding','numeric-type-binding','missing-reason-binding','reversed-dates','foreign-route','foreign-actor-own-route','anonymous','teacher-existing-access','operations-existing-access','foreign-read-route','owned-list-canonical-readback','foreign-owned-list-preserved-decision'])assert.ok(cases.includes(label),label);
assert.equal(cases.filter(x=>x.startsWith('valid-')).length,16);assert.equal(cases.filter(x=>x.startsWith('unsupported-')).length,14);
assert.match(log,/LEAVEIDENTITY REGRESSION PASS:63 cases/);assert.match(log,/QA LeaveIdentity exit=0/);
assert.match(log,/routes=312, controller method\/routes=301, framework Identity method\/routes=10, SHA256=74CB2F226EBAFF8881F6CF80D6F648310A78B07D79A0A6D9E04D4ED858544FF3/);
assert.match(log,/PASS: application migrations=81, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);assert.match(log,/Negative cleanup check refused a mismatched database marker/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-leave-identity-sql-build.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
const id=log.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(log.match(/^QA SQL port=(\d+)/m)[1]);
assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
assert.equal(snapshot.checks.backendTotal,477);assert.equal(snapshot.checks.newBackendCases,51);assert.equal(snapshot.checks.httpSqlCases,63);assert.equal(snapshot.checks.closure,'OPEN');
assert.match(read('QA/ISSUES/BUG-DATA-0038.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_LEAVE_IDENTITY_REPAIR.md','QA/ISSUES/BUG-DATA-0038.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:'pipe'});
console.log('PASS Leave identity bounded repair:477 backend tests (51 new),63 real Identity/model-binding/HTTP/fresh SQL cases, unchanged inactive policy/access/empty success body, prior application sources and normal assemblies preserved, owned cleanup. Browser/Decide/audit rollback/critical release OPEN.');
