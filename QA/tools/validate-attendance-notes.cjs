const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_ATTENDANCE_NOTES_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.binaries,...snapshot.evidence,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
const previous=JSON.parse(read('QA/REPORTS/PHASE_2B_ASSESSMENT_OPTIONS_SOURCE_SNAPSHOT.json'));
for(const x of previous.sources.filter(x=>x.file.startsWith('apps/api/')||x.file==='apps/web/src/app/assessments/page.tsx'))assert.equal(sha(x.file),x.sha256,'Prior application source changed outside attendance slice');
for(const x of snapshot.normalBinaries)assert.equal(x.sha256,previous.normalBinaries.find(y=>y.file===x.file).sha256,'Normal development assembly changed');
assert.equal(cp.execFileSync('git',['diff','--','apps/api/Controllers/AttendanceController.cs'],{cwd:root,encoding:'utf8'}),'','Attendance API was modified');
const ui=read('QA/EVIDENCE/logs/phase-2b-attendance-notes-ui.log');assert.match(ui,/tests 24[\s\S]*pass 24[\s\S]*fail 0/);
const payloads=[...ui.matchAll(/^ATTENDANCE PAYLOAD (.+)$/gm)].map(x=>JSON.parse(x[1]));assert.equal(payloads.length,10);assert.equal(new Set(payloads.map(x=>x.label)).size,10);
for(const x of payloads){assert.equal(x.payload.studentId,'s');assert.equal(x.payload.notes===null?null:x.payload.notes.trim(),x.expected);}
const log=read('QA/EVIDENCE/logs/phase-2b-attendance-notes-sql.log');assert.equal([...log.matchAll(/^ATTENDANCENOTES CASE .+ PASS\.$/gm)].length,16);assert.match(log,/ATTENDANCENOTES REGRESSION PASS:16 cases/);assert.match(log,/QA AttendanceNotes exit=0/);
for(const x of payloads)assert.ok(log.includes('ATTENDANCENOTES CASE '+x.label+' PASS.'),x.label);
for(const label of ['invalid-status','no-active-enrollment','missing-session','foreign-route','anonymous','second-session-independence'])assert.ok(log.includes('ATTENDANCENOTES CASE '+label+' PASS.'),label);
assert.match(log,/routes=312, controller method\/routes=301, framework Identity method\/routes=10, SHA256=74CB2F226EBAFF8881F6CF80D6F648310A78B07D79A0A6D9E04D4ED858544FF3/);
assert.match(log,/PASS: application migrations=81, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);
assert.match(log,/Negative cleanup check refused a mismatched database marker/);
const id=log.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(log.match(/^QA SQL port=(\d+)/m)[1]);
assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
assert.match(read('QA/EVIDENCE/logs/phase-2b-attendance-notes-typecheck.log'),/TypeScript PASS/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-attendance-notes-sql-build.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
for(const f of ['phase-2b-attendance-notes-baseline-lint.log','phase-2b-attendance-notes-lint.log'])assert.match(read('QA/EVIDENCE/logs/'+f),/1 error, 2 warnings/);
assert.equal(snapshot.checks.typeScriptExitCode,0);assert.equal(snapshot.checks.lintExitCode,1);
assert.match(read('QA/ISSUES/BUG-DATA-0035.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_ATTENDANCE_NOTES_REPAIR.md','QA/ISSUES/BUG-DATA-0035.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:'pipe'});
console.log('PASS attendance repair:24 actual TSX handlers,16 real HTTP/SQL cases with ten emitted payloads, TypeScript/build, owned cleanup and prior API/schema/security/normal assembly preservation. Inherited lint1 error/2 warnings; browser/device/critical acceptance OPEN.');
