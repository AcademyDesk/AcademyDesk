const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_ASSESSMENT_ROSTER_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.binaries,...snapshot.evidence,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
const prior=JSON.parse(read('QA/REPORTS/PHASE_2B_ASSESSMENT_GRADE_SOURCE_SNAPSHOT.json'));
for(const x of snapshot.normalBinaries)assert.equal(x.sha256,prior.normalBinaries.find(y=>y.file===x.file).sha256,'Normal development assembly changed');
const trx=read('QA/EVIDENCE/assessment-roster/assessment-roster-suite.trx'),results=[...trx.matchAll(/<UnitTestResult\b[^>]*\boutcome="([^"]+)"/g)].map(x=>x[1]);
assert.equal(results.length,408);assert.ok(results.every(x=>x==='Passed'));assert.equal([...trx.matchAll(/testName="AcademyDesk.Api.Tests.AssessmentRosterTests\./g)].length,12);
const log=read('QA/EVIDENCE/logs/phase-2b-assessment-roster-sql-final.log');
assert.equal([...log.matchAll(/^ROSTER CASE .+ PASS\.$/gm)].length,30);assert.match(log,/ROSTER REGRESSION PASS:30 cases/);assert.match(log,/student-active-result-visible PASS/);assert.match(log,/guardian-active-result-visible PASS/);assert.match(log,/QA AssessmentRoster exit=0/);
assert.match(log,/PASS: application migrations=81, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);assert.match(log,/routes=311, controller method\/routes=300, framework Identity method\/routes=10/);
for(const name of ['phase-2b-assessment-roster-sql.log','phase-2b-assessment-roster-sql-development.log','phase-2b-assessment-roster-sql-final.log']) {
    const l=read('QA/EVIDENCE/logs/'+name),id=l.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(l.match(/^QA SQL port=(\d+)/m)[1]);
    assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);
    assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
    assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
}
assert.match(read('QA/ISSUES/BUG-DATA-0034.md'),/\| Status \| OPEN \|/);
assert.match(read('QA/REPORTS/PHASE_2B_ASSESSMENT_ROSTER_REPAIR.md'),/membership-only repair/);
for(const f of ['QA/REPORTS/PHASE_2B_ASSESSMENT_ROSTER_REPAIR.md','QA/ISSUES/BUG-DATA-0034.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)) {
    const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);
}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:'pipe'});
console.log('PASS bounded assessment membership repair: backend408/408 (12 new), final HTTP/SQL30, pinned evidence and owned cleanup. Historical-status policy/matrix, browser/device, access and release gates remain OPEN.');
