const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_SCHEDULE_DEFAULTS_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.binaries,...snapshot.evidence,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
for(const previousName of ['CALENDAR_DETAILS','ATTENDANCE_NOTES','ASSESSMENT_OPTIONS']) {
 const previous=JSON.parse(read(`QA/REPORTS/PHASE_2B_${previousName}_SOURCE_SNAPSHOT.json`));
 for(const x of previous.sources.filter(x=>x.file.startsWith('apps/')&&x.file!=='apps/web/src/app/schedule/page.tsx'))assert.equal(sha(x.file),x.sha256,'Prior application source changed outside Schedule');
 for(const x of (previous.unchangedSources??[]).filter(x=>x.file!=='apps/web/src/app/schedule/page.tsx'))assert.equal(sha(x.file),x.sha256,'Prior Teacher/CSS source changed');
 for(const x of snapshot.normalBinaries)assert.equal(x.sha256,previous.normalBinaries.find(y=>y.file===x.file).sha256,'Normal development assembly changed');
}
assert.equal(cp.execFileSync('git',['diff','--','apps/api/Controllers/ClassSessionsController.cs'],{cwd:root,encoding:'utf8'}),'','Session API changed');
const ui=read('QA/EVIDENCE/logs/phase-2b-schedule-defaults-ui.log');assert.match(ui,/tests 18[\s\S]*pass 18[\s\S]*fail 0/);
const fixtures=[...ui.matchAll(/^SCHEDULEDEFAULTS PAYLOAD (.+)$/gm)].map(x=>JSON.parse(x[1]));assert.equal(fixtures.length,12);assert.equal(new Set(fixtures.map(x=>x.label)).size,12);
for(const x of fixtures){assert.equal(x.payload.teacherId,x.expected.teacherId);assert.equal(x.payload.branchId,x.expected.branchId);assert.match(x.payload.startUtc,/T04:30:00\.000Z$/);assert.equal(new Date(x.payload.endUtc)-new Date(x.payload.startUtc),3600000);}
const log=read('QA/EVIDENCE/logs/phase-2b-schedule-defaults-sql.log');assert.equal([...log.matchAll(/^SCHEDULEDEFAULTS CASE .+ PASS\.$/gm)].length,18);assert.match(log,/SCHEDULEDEFAULTS REGRESSION PASS:18 cases/);assert.match(log,/QA ScheduleDefaults exit=0/);
for(const label of [...fixtures.map(x=>x.label),'null-override-inherits-existing-contract','foreign-batch','foreign-teacher','foreign-branch','foreign-route','anonymous'])assert.ok(log.includes('SCHEDULEDEFAULTS CASE '+label+' PASS.'),label);
assert.match(log,/routes=312, controller method\/routes=301, framework Identity method\/routes=10, SHA256=74CB2F226EBAFF8881F6CF80D6F648310A78B07D79A0A6D9E04D4ED858544FF3/);
assert.match(log,/PASS: application migrations=81, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);assert.match(log,/Negative cleanup check refused a mismatched database marker/);
const id=log.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(log.match(/^QA SQL port=(\d+)/m)[1]);
assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
assert.match(read('QA/EVIDENCE/logs/phase-2b-schedule-defaults-typecheck.log'),/TypeScript PASS/);assert.match(read('QA/EVIDENCE/logs/phase-2b-schedule-defaults-sql-build.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
for(const f of ['phase-2b-schedule-defaults-baseline-lint.log','phase-2b-schedule-defaults-lint.log']){const lint=read('QA/EVIDENCE/logs/'+f);assert.match(lint,/Schedule lint PASS: exit=0/);assert.match(lint,/0 errors, 1 warning/);}
assert.equal(snapshot.checks.typeScriptExitCode,0);assert.equal(snapshot.checks.lintExitCode,0);assert.match(read('QA/ISSUES/BUG-DATA-0037.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_SCHEDULE_DEFAULTS_REPAIR.md','QA/ISSUES/BUG-DATA-0037.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:'pipe'});
console.log('PASS Schedule defaults repair:18 actual TSX handlers,18 real Identity/HTTP/fresh SQL cases with twelve emitted payloads, TypeScript/build/lint exit0 (1 inherited warning), owned cleanup and prior application/normal assembly preservation. Browser/device/critical acceptance OPEN.');
