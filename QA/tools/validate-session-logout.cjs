// Evidence consistency, not an application/native suite or security release gate.
const fs=require('node:fs'),p=require('node:path'),c=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=p.resolve(__dirname,'../..'),read=f=>fs.readFileSync(p.join(root,f),'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n'),json=f=>JSON.parse(read(f)),sha=f=>c.createHash('sha256').update(fs.readFileSync(p.join(root,f))).digest('hex');
const before=json('QA/EVIDENCE/session-logout-before.json'),receipt=json('QA/EVIDENCE/session-logout-source-receipt.json');
const pins=new Map(before.predecessors.flatMap(f=>{const x=json(f);return x.retainedBefore||x.current;}).map(x=>[x.file,x])),allowed=new Set(before.files.map(x=>x.file));let retained=0;
for(const[file,item]of pins){if(allowed.has(file))continue;a.equal(sha(file),item.sha256,'Immutable predecessor '+file);retained++;}
a.equal(pins.size,979);a.equal(retained,975);a.equal(receipt.predecessorPins,pins.size);a.equal(receipt.immutablePredecessorPins,retained);
for(const item of before.files){a.equal(sha(item.backup),item.sha256,item.backup);if(pins.has(item.file))a.equal(pins.get(item.file).sha256,item.sha256,item.file);}
for(const item of receipt.current)a.equal(sha(item.file),item.sha256,item.file);
const log=n=>read('QA/EVIDENCE/logs/phase-2b-session-logout-'+n+'.log');
a.match(log('baseline'),/tests 18/);a.match(log('baseline'),/pass 8/);a.match(log('baseline'),/fail 10/);
a.match(log('handlers'),/tests 75/);a.match(log('handlers'),/pass 75/);a.match(log('handlers'),/fail 0/);
a.equal(log('types').trim(),'');a.equal(receipt.typesExitCode,0);a.equal(log('lint-final').trim(),'');a.equal(receipt.lintExitCode,0);
a.match(log('lint'),/couldn't find an eslint.config/);a.match(log('browser-initial'),/Timeout 30000ms/);a.match(json('QA/EVIDENCE/session-logout-browser-incomplete.json').error,/SyntheticTeacher/);
a.match(log('browser'),/SUMMARY \{"checks":5,"accepted":true,"nativeBackend":false\}/);
const observed=json('QA/EVIDENCE/session-logout-browser-observations.json');a.equal(observed.checks.length,5);a.equal(new Set(observed.checks).size,5);a.equal(observed.errors.length,0);a.equal(observed.serverRevocationAccepted,false);a.match(observed.transport,/controlled/);
a.ok(observed.events.some(x=>x.key==='academydesk.accessToken.AcademyAdmin'&&x.removed));a.ok(observed.controlled.some(x=>x.path==='/api/auth/login'&&x.method==='POST'));
for(const id of ['real-storage-event-admin-logout-gates-peer-preserves-teacher','peer-signin-link-full-load-returns-ready-dashboard','mobile390-peer-logout-message-action-no-horizontal-overflow','teacher-own-logout-does-not-gate-admin-workspace'])a.ok(observed.checks.includes(id));
for(const id of ['desktop-peer','mobile-peer']){a.match(read('QA/EVIDENCE/session-logout-'+id+'.txt'),/You signed out of this workspace in another tab/);a.ok(fs.statSync(p.join(root,'QA/EVIDENCE/session-logout-'+id+'.png')).size>0);}
const cleanup=json('QA/EVIDENCE/session-logout-cleanup.json');a.equal(cleanup.listenersRemaining,0);a.equal(cleanup.webStopped,true);a.equal(cleanup.webCopyRetained,true);a.equal(cleanup.newSqlResources,'NONE');a.equal(cleanup.previousFixtures,'UNCHANGED');a.equal(cleanup.normalDevServices,'UNCHANGED');
const report='QA/REPORTS/PHASE_2B_SESSION_LOGOUT.md';for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))a.ok(fs.existsSync(p.resolve(root,p.dirname(report),link[1])),link[1]);
for(const[k,v]of Object.entries({applicationChanged:true,productScope:'API client helper and workspace frame only',azure:'UNCHANGED',commitPushDeploy:'NOT DONE',priorNativeSuitesRerun:false,broaderSessionAcceptance:'OPEN',newControlledChecks:18,relevantExistingHelperChecks:57,controlledChecksAccepted:75,browserChecksAccepted:5,nativeBackendAcceptance:false,serverLogoutRevocationAcceptance:false,cleanupComplete:true}))a.equal(receipt[k],v,k);
a.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),receipt.commit);a.equal(receipt.commit,before.commit);a.equal(cp.spawnSync('git',['-c','core.safecrlf=false','diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log(`PASS successor consistency:${retained} immutable predecessor pins;18 new+57 relevant helper regressions,5 controlled-transport browser/storage-event checks;owned QA web stopped,no SQL/Azure/deployment. Native server logout,cross-tab locking/pre-event/frozen/broader session/release OPEN.`);
