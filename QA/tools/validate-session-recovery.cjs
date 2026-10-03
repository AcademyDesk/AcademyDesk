// Artifact consistency only; does not rerun accepted audit/backend/security suites.
const fs=require('node:fs'),p=require('node:path'),c=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=p.resolve(__dirname,'../..'),read=f=>fs.readFileSync(p.join(root,f),'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n'),json=f=>JSON.parse(read(f)),sha=f=>c.createHash('sha256').update(fs.readFileSync(p.join(root,f))).digest('hex');
const before=json('QA/EVIDENCE/session-recovery-before.json'),receipt=json('QA/EVIDENCE/session-recovery-source-receipt.json');
const predecessors=new Map(before.predecessors.flatMap(f=>{const x=json(f);return x.retainedBefore||x.current;}).map(x=>[x.file,x]));
const allowed=new Set(before.files.map(x=>x.file));let retained=0;
for(const [file,item]of predecessors){if(allowed.has(file))continue;a.equal(sha(file),item.sha256,'Immutable predecessor '+file);retained++;}
a.equal(predecessors.size,945);a.equal(retained,942);a.equal(receipt.predecessorPins,predecessors.size);a.equal(receipt.immutablePredecessorPins,retained);
const backups={
 'apps/web/src/app/dashboard/page.tsx':before.dashboardBackup,
 'QA/REPORTS/PHASE_2_START_PLAN.md':'QA/EVIDENCE/session-recovery-phase-plan-before.md',
 'QA/03_TEST_MATRIX.md':'QA/EVIDENCE/session-recovery-matrix-before.md',
 'QA/ISSUES/BUG-AUTH-REVOCATION-001.md':'QA/EVIDENCE/session-recovery-issue-before.md'
};
for(const item of before.files){a.equal(sha(backups[item.file]),item.sha256,backups[item.file]);if(predecessors.has(item.file))a.equal(predecessors.get(item.file).sha256,item.sha256,item.file);}
for(const item of receipt.current)a.equal(sha(item.file),item.sha256,item.file);
a.notEqual(sha('apps/web/src/app/dashboard/page.tsx'),before.files[0].sha256,'Dashboard changed');
const prefix='QA/EVIDENCE/logs/phase-2b-session-recovery-',log=n=>read(prefix+n+'.log');
a.match(log('baseline'),/tests 13/);a.match(log('baseline'),/pass 2/);a.match(log('baseline'),/fail 11/);
a.match(log('baseline-initial'),/pass 0/);a.match(log('baseline-initial'),/fail 13/);
a.match(log('handlers'),/tests 13/);a.match(log('handlers'),/pass 13/);a.match(log('handlers'),/fail 0/);a.equal(log('types').trim(),'');a.equal(receipt.typesExitCode,0);
a.match(log('browser-initial'),/FAILED[\s\S]*ERR_ABORTED/);a.match(log('browser-navigation-attempt'),/FAILED[\s\S]*Timeout 30000ms/);
a.match(json('QA/EVIDENCE/session-recovery-navigation-incomplete.json').error,/locator.click/);
a.match(log('browser'),/SUMMARY \{"checks":6,"accepted":true,"nativeRateLimitAcceptance":false\}/);
const observed=json('QA/EVIDENCE/session-recovery-observations.json');
a.equal(observed.run,'f197585f3eb94984903b10f1b76b03f9');a.equal(observed.checks.length,6);a.equal(new Set(observed.checks.map(x=>x.id)).size,6);
a.equal(observed.checks.filter(x=>x.native).length,4);a.equal(observed.errors.length,0);a.equal(observed.nativeRateLimitAcceptance,false);
for(const id of ['revoked-existing-admin-401-signin-other-workspace-preserved','disabled-existing-admin-reload-403-clear-guidance-no-false-totals','manual-read-retry-native-dashboard-success'])a.ok(observed.checks.find(x=>x.id===id&&x.native));
for(const id of ['controlled429-retry-guidance-preserves-session','mobile390-controlled-error-recovery-visible'])a.ok(observed.checks.find(x=>x.id===id&&!x.native));
a.ok(observed.mocked.length>=2);a.ok(observed.mocked.every(x=>x.path==='/api/academies'&&x.status===429));
a.ok(!observed.events.some(x=>x.status===429&&!x.controlled429));
for(const [path,status]of [['/api/academies',401],['/api/academies',403],['/api/auth/refresh',401],['/api/auth/login',200]])a.ok(observed.events.some(x=>x.label==='user'&&x.path===path&&x.status===status));
a.equal(observed.events.filter(x=>x.label==='owner'&&x.path.endsWith('/reset-password')&&x.method==='POST'&&x.status===200).length,1);
a.equal(observed.events.filter(x=>x.label==='owner'&&x.path.endsWith('/active')&&x.method==='PATCH'&&x.status===200).length,1);
const native=log('sql');a.match(native,/Runtime inventory PASS: routes=313/);a.match(native,/Tenant controls PASS/);
a.equal([...native.matchAll(/SESSIONBROWSER NATIVE/g)].length,4);
a.match(native,/DISABLE SQL PASS inactive identity and exactly one attributed platform audit/);
a.match(native,/FINAL disableVerified=True passwordNativeVerified=False; original user\/role counts preserved/);
a.match(native,/Negative cleanup check refused/);a.match(native,/application migrations=82, identity migrations=7/);a.match(native,/QA BrowserSession exit=0/);
a.equal([...native.matchAll(/SESSIONBROWSER HTTP PATCH \/api\/platform\/admins\/[0-9a-f-]+\/active 200/g)].length,1);
a.equal([...native.matchAll(/SESSIONBROWSER HTTP POST \/api\/platform\/admins\/[0-9a-f-]+\/reset-password 200/g)].length,1);
for(const [id,text]of [['revoked-401','Your session has ended.'],['disabled-403','You do not have access to this workspace.'],['controlled-429','Too many requests.'],['controlled-429-mobile','Try again']]){a.ok(read('QA/EVIDENCE/session-recovery-'+id+'.txt').includes(text),id);a.ok(fs.statSync(p.join(root,'QA/EVIDENCE/session-recovery-'+id+'.png')).size>0,id);}
const cleanup=json('QA/EVIDENCE/session-recovery-cleanup.json');a.equal(cleanup.runId,observed.run);a.equal(cleanup.containerAbsent,true);a.equal(cleanup.rootAbsent,true);a.equal(cleanup.listenersRemaining,0);a.equal(cleanup.webStopped,true);a.equal(cleanup.webCopyRetained,true);
a.ok(!fs.existsSync(cleanup.root));a.ok(!cp.execFileSync('docker',['ps','-a','--format','{{.Names}}'],{encoding:'utf8'}).split(/\r?\n/).includes('academydesk-qa-'+cleanup.runId));
const report='QA/REPORTS/PHASE_2B_SESSION_RECOVERY.md';for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))a.ok(fs.existsSync(p.resolve(root,p.dirname(report),link[1])),link[1]);
for(const[k,v]of Object.entries({applicationChanged:true,productScope:'Dashboard only',azure:'UNCHANGED',commitPushDeploy:'NOT DONE',priorSuitesRerun:false,broaderSessionAcceptance:'OPEN',controlledChecksAccepted:13,browserChecksAccepted:6,nativeBrowserChecks:4,controlled429BrowserChecks:2,nativeRateLimitAcceptance:false,nativeControls:4,cleanupComplete:true}))a.equal(receipt[k],v,k);
a.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),receipt.commit);a.equal(receipt.commit,before.commit);
a.equal(cp.spawnSync('git',['-c','core.safecrlf=false','diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log(`PASS successor consistency:${retained} immutable predecessor pins;dashboard-only repair,13 controlled cases,6 scoped browser checks (4 native/2 controlled429),4 native disable/SQL controls;fixture cleaned. Historical QA failures retained;native limiter/broader session/release OPEN,no deployment.`);
