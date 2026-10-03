const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_TEMPLATE_STATE_SOURCE_SNAPSHOT.json')),prior=JSON.parse(read('QA/REPORTS/PHASE_2B_PREFERENCE_ACCESS_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.evidence,...snapshot.binaries,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
const intentional=['apps/api/Controllers/NotificationsController.cs','apps/web/src/app/communications/page.tsx','QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1','QA/00_QA_README.md','QA/REPORTS/PHASE_2_START_PLAN.md','QA/ISSUES/INDEX.md','QA/03_TEST_MATRIX.md'];
for(const x of [...prior.sources,...prior.evidence,...prior.binaries,...prior.normalBinaries])if(!intentional.includes(x.file))assert.equal(sha(x.file),x.sha256,'Accepted predecessor preserved '+x.file);
assert.deepEqual(snapshot.normalBinaries,prior.normalBinaries);
for(const x of snapshot.beforeSources.filter(x=>prior.sources.some(y=>y.file===x.file)))assert.equal(x.sha256,prior.sources.find(y=>y.file===x.file).sha256,'Accepted starting source '+x.file);
const log=read('QA/EVIDENCE/logs/phase-2b-template-state-sql.log'),baseline=read('QA/EVIDENCE/logs/phase-2b-template-state-sql-baseline.log');
assert.match(baseline,/TEMPLATESTATE BASELINE PASS:2 Disabled\/active templates created through HTTP and incorrectly accepted/);
assert.equal([...baseline.matchAll(/^TEMPLATESTATE CASE .+ PASS\.$/gm)].length,2);
assert.match(log,/TEMPLATESTATE REGRESSION PASS:42 cases/);assert.equal([...log.matchAll(/^TEMPLATESTATE CASE .+ PASS\.$/gm)].length,42);
for(const channel of ['Email','WhatsApp']){
 for(const status of ['Draft','Approved','Disabled'])for(const active of ['True','False'])assert.ok(log.includes(`CASE ${channel}-${status}-active-${active} PASS.`));
 for(const label of ['legacy-dIsAbLeD','legacy-Disabled','before-disable','committed-disable-stale-id','explicit-reactivation','committed-inactive-stale-id','starter-draft-usable','starter-disabled','approved-not-provider-approved','synthetic-connected-still-queued'])assert.ok(log.includes(`CASE ${channel}-${label} PASS.`));
}
for(const label of ['starter-catalogue-and-add','repeat-starters-do-not-reactivate','foreign-template','missing-template','teacher-denied','foreign-actor-denied','foreign-route-denied','anonymous-denied','owned-list-retains-disabled-for-editing','foreign-list-preserved'])assert.ok(log.includes(`CASE ${label} PASS.`));
for(const output of [baseline,log]){
 assert.match(output,/QA TemplateState exit=0/);assert.match(output,/application migrations=82, identity migrations=7/);assert.match(output,/Negative cleanup check refused a mismatched database marker/);
 assert.match(output,/routes=313, controller method\/routes=302, framework Identity method\/routes=10, SHA256=562F95AD3C4514C384CCC11317E91174D0BAB7203E6AA7F162FCD74E8BE81A99/);
 const id=output.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(output.match(/^QA SQL port=(\d+)/m)[1]);
 assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
 assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
}
assert.match(read('QA/EVIDENCE/logs/phase-2b-template-state-backend-baseline.log'),/Failed:\s+8, Passed:\s+16, Skipped:\s+0, Total:\s+24/);
assert.match(read('QA/EVIDENCE/template-state/template-state-baseline.trx'),/<Counters total="24" executed="24" passed="16" failed="8"/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-template-state-suite.log'),/Failed:\s+0, Passed:\s+633, Skipped:\s+0, Total:\s+633/);
const trx=read('QA/EVIDENCE/template-state/template-state-suite.trx');assert.match(trx,/<Counters total="633" executed="633" passed="633"/);assert.equal([...trx.matchAll(/<UnitTestResult [^>]*testName="AcademyDesk\.Api\.Tests\.NotificationTemplateStateTests\./g)].length,24);
assert.match(read('QA/EVIDENCE/logs/phase-2b-template-state-ui-baseline.log'),/tests 26[\s\S]*pass 6[\s\S]*fail 20/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-template-state-ui.log'),/tests 26[\s\S]*pass 26[\s\S]*fail 0/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-template-state-channel-regression.log'),/tests 50[\s\S]*pass 50[\s\S]*fail 0/);
const payloads=f=>read(f).split(/\r?\n/).filter(x=>x.startsWith('CHANNELCHOICE PAYLOAD ')).sort();assert.equal(payloads('QA/EVIDENCE/logs/phase-2b-template-state-channel-regression.log').length,22);assert.deepEqual(payloads('QA/EVIDENCE/logs/phase-2b-template-state-channel-regression.log'),payloads('QA/EVIDENCE/logs/phase-2b-communication-channel-ui.log'));
assert.match(read('QA/EVIDENCE/logs/phase-2b-template-state-typecheck.log'),/TypeScript PASS/);
for(const suffix of ['sql-build-baseline','sql-build'])assert.match(read(`QA/EVIDENCE/logs/phase-2b-template-state-${suffix}.log`),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-template-state-lint.log'),/0 errors, 1 warning/);
assert.deepEqual(snapshot.checks,{backendTotal:633,newBackendCases:24,controlledTsxCases:26,previousChannelCases:50,unchangedPayloads:22,httpSqlCases:42,baselineHttpDefects:2,baselineBackendFailures:8,baselineUiFailures:20,lint:'0 errors/1 inherited warning',closure:'OPEN',browserDevice:'NOT RUN',devServices:'UNCHANGED'});
assert.match(read('QA/ISSUES/BUG-FUNC-0029.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_TEMPLATE_STATE_REPAIR.md','QA/ISSUES/BUG-FUNC-0029.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
assert.equal(cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS Template state:2 real HTTP baseline defects;633 backend (24 new),26 controlled TSX/50 preserved channel cases/22 identical payloads,42 real Identity/global-filter/HTTP-SQL cases. Disabled overrides active; Draft/Approved preserved, committed disable rechecked; no-write rejection/full summaries/actor audits/scope/consent preserved. Predecessor evidence/schema/normal binaries/owned cleanup verified; browser/race/critical OPEN; no dev/Azure/outbound/deploy.');
