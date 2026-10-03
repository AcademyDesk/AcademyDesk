const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_MAKEUP_LOCATION_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.binaries,...snapshot.evidence,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
for(const name of ['LEAVE_IDENTITY','SCHEDULE_DEFAULTS','CALENDAR_DETAILS','ATTENDANCE_NOTES','ASSESSMENT_OPTIONS']){
 const prior=JSON.parse(read(`QA/REPORTS/PHASE_2B_${name}_SOURCE_SNAPSHOT.json`));
 for(const x of [...prior.sources,...(prior.unchangedSources??[])].filter(x=>x.file.startsWith('apps/')&&!['apps/web/src/app/calendar/page.tsx','apps/web/src/app/schedule/page.tsx'].includes(x.file)))assert.equal(sha(x.file),x.sha256,'Prior source '+x.file);
 for(const x of snapshot.normalBinaries)assert.equal(x.sha256,prior.normalBinaries.find(y=>y.file===x.file).sha256,'Normal assembly changed');
}
const prior=JSON.parse(read('QA/REPORTS/PHASE_2B_LEAVE_IDENTITY_SOURCE_SNAPSHOT.json'));assert.equal(sha('apps/web/src/app/schedule/page.tsx'),prior.sources.find(x=>x.file==='apps/web/src/app/schedule/page.tsx').sha256);
const suite=read('QA/EVIDENCE/logs/phase-2b-makeup-location-suite.log');assert.match(suite,/Failed:\s+0, Passed:\s+516, Skipped:\s+0, Total:\s+516/);
const trx=read('QA/EVIDENCE/makeup-location/makeup-location-suite.trx');assert.match(trx,/<Counters total="516" executed="516" passed="516"/);assert.equal([...trx.matchAll(/<UnitTestResult [^>]*testName="AcademyDesk\.Api\.Tests\.MakeupLocationTests\./g)].length,39);
const ui=read('QA/EVIDENCE/logs/phase-2b-makeup-location-ui.log');assert.match(ui,/tests 53[\s\S]*pass 53[\s\S]*fail 0/);assert.match(ui,/Actual directory renders captured HTTP\/SQL mode-specific locations/);assert.match(ui,/Captured make-up SQL fixture reaches actual calendar location projections/);
const log=read('QA/EVIDENCE/logs/phase-2b-makeup-location-sql.log'),cases=[...log.matchAll(/^MAKEUPLOCATION CASE (.+) PASS\.$/gm)].map(x=>x[1]);assert.equal(cases.length,50);assert.equal(new Set(cases).size,50);
for(const label of ['inherited-batch-teacher-fallback','existing-makeup-manage-grant','no-owned-upcoming-scheduled-class','foreign-student','foreign-batch','foreign-teacher','missing-manual-date','foreign-route','anonymous','teacher-no-grant','custom-role-no-grant','owned-list-response-fresh-SQL','foreign-owned-list-preserved-row'])assert.ok(cases.includes(label),label);
const fixture=JSON.parse(log.match(/^MAKEUPLOCATION FIXTURE (.+)$/m)[1]);assert.equal(fixture.makeups.length,20);
for(const row of fixture.makeups){assert.ok(['Offline','Online','Hybrid'].includes(row.deliveryMode));if(row.deliveryMode==='Offline')assert.equal(row.meetingLink,null);else{assert.equal(row.venue,null);assert.equal(row.meetingLink,'https://meeting.example.invalid/makeup');}}
assert.match(log,/MAKEUPLOCATION REGRESSION PASS:50 cases/);assert.match(log,/QA MakeupLocation exit=0/);
assert.match(log,/routes=312, controller method\/routes=301, framework Identity method\/routes=10, SHA256=74CB2F226EBAFF8881F6CF80D6F648310A78B07D79A0A6D9E04D4ED858544FF3/);
assert.match(log,/PASS: application migrations=81, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);assert.match(log,/Negative cleanup check refused a mismatched database marker/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-makeup-location-sql-build.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);assert.match(read('QA/EVIDENCE/logs/phase-2b-makeup-location-typecheck.log'),/TypeScript PASS/);assert.match(read('QA/EVIDENCE/logs/phase-2b-makeup-location-lint.log'),/0 errors, 1 warning/);
for(const name of ['phase-2b-makeup-location-sql.log','phase-2b-makeup-location-sql-attempt1.log']){
 const text=read('QA/EVIDENCE/logs/'+name),id=text.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(text.match(/^QA SQL port=(\d+)/m)[1]);
 assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
 assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
}
assert.equal(snapshot.checks.backendTotal,516);assert.equal(snapshot.checks.newBackendCases,39);assert.equal(snapshot.checks.httpSqlCases,50);assert.equal(snapshot.checks.controlledTsxCases,53);assert.equal(snapshot.checks.closure,'OPEN');
assert.match(read('QA/ISSUES/BUG-DATA-0039.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_MAKEUP_LOCATION_REPAIR.md','QA/ISSUES/BUG-DATA-0039.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:'pipe'});
console.log('PASS Makeup location bounded repair:516 backend (39 new),50 real HTTP/SQL,53 actual controlled TSX (22 new/31 reused including captured HTTP fixtures), relevant locations only and queued notices, preserved prior sources/normal assemblies/owned cleanup. Browser/status/concurrency/audit rollback/critical release OPEN.');
