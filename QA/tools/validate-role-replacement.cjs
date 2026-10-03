// Successor consistency, not full security/session/AI/release certification.
const fs=require('node:fs'),p=require('node:path'),c=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=p.resolve(__dirname,'../..'),read=f=>fs.readFileSync(p.join(root,f),'utf8').replace(/^\uFEFF/,''),json=f=>JSON.parse(read(f)),sha=f=>c.createHash('sha256').update(fs.readFileSync(p.join(root,f))).digest('hex');
const before=json('QA/EVIDENCE/role-replacement-before.json'),receipt=json('QA/EVIDENCE/role-replacement-source-receipt.json');
const pins=new Map(before.predecessors.flatMap(f=>{let x=json(f);return x.retainedBefore||x.current;}).map(x=>[x.file,x])),allowed=new Set(before.files.map(x=>x.file));let retained=0;
for(const [file,x]of pins){if(allowed.has(file))continue;a.equal(sha(file),x.sha256,file);retained++;}
a.equal(pins.size,1024);a.equal(retained,1019);a.equal(receipt.predecessorPins,pins.size);a.equal(receipt.immutablePredecessorPins,retained);
for(const x of before.files){a.equal(sha(x.backup),x.sha256,x.backup);if(pins.has(x.file))a.equal(pins.get(x.file).sha256,x.sha256,x.file);}
for(const x of receipt.current)a.equal(sha(x.file),x.sha256,x.file);
const old=json('QA/EVIDENCE/role-revocation-observations.json');a.equal(old.runs.length,2);for(const x of old.runs)a.equal(x.cases.filter(y=>!y.accepted).length,2);
a.match(read('QA/EVIDENCE/logs/phase-2b-role-revocation-consistency-final.log'),/PASS diagnostic consistency/);
const observed=json('QA/EVIDENCE/role-replacement-observations.json');
a.deepEqual(observed.coreSummary,{requests:30,observations:37,acceptedControls:37,policyGaps:0,unchangedSnapshots:18,diagnosticOnly:false,nativeIdentitySql:true});a.equal(observed.coreCases.length,37);a.ok(observed.coreCases.every(x=>x.accepted));
a.deepEqual(observed.extendedSummary,{cases:56,injectedFailures:21,unchangedSnapshots:28,protectedRoles:5,preservedExplicitGrant:true,actors:3,nativeIdentitySql:true,accepted:true});a.equal(observed.extendedCases.length,56);a.equal(observed.preservedGrantControl,true);
for(const actor of ['admin','owner','bypass'])for(const mode of ['remove-result','add-result','stamp-result','remove-sql','add-sql','stamp-sql','audit-sql']){
 const fail=observed.extendedCases.find(x=>x.label===actor+'-'+mode+'-rollback'),retry=observed.extendedCases.find(x=>x.label===actor+'-'+mode+'-retry');
 a.ok(fail&&retry);a.equal(fail.status,500);a.equal(fail.faultHits,1);a.equal(fail.exactSnapshotVerified,true);a.equal(fail.beforeDigest,fail.afterDigest);a.equal(retry.status,200);a.deepEqual(retry.roles,['Operations']);
}
for(const role of ['Owner','AcademyAdmin','Student','Guardian','Teacher']){const x=observed.extendedCases.find(x=>x.label==='preserve-'+role);a.equal(x.status,200);a.ok(x.roles.includes(role));a.ok(x.roles.includes('Operations'));a.equal(x.roles.length,2);}
a.equal(observed.extendedCases.filter(x=>x.exactSnapshotVerified).length,28);
for(const x of observed.extendedCases.filter(x=>x.exactSnapshotVerified))a.equal(x.beforeDigest,x.afterDigest);
const log=read(observed.file);a.match(log,/Negative cleanup check refused/);a.match(log,/application migrations=82, identity migrations=7/);a.match(log,/exit=0/);
const cleanup=json('QA/EVIDENCE/role-replacement-cleanup.json');a.equal(cleanup.run,observed.run);a.equal(cleanup.port,observed.port);a.equal(cleanup.rootAbsent,true);a.equal(cleanup.containerAbsent,true);a.equal(cleanup.listenersRemaining,0);
a.equal(cleanup.normalDevDatabaseAndServices,'UNCHANGED');a.equal(cleanup.previousFixtures,'UNCHANGED');a.equal(cleanup.azure,'UNCHANGED');
a.match(read('QA/EVIDENCE/logs/phase-2b-role-replacement-build.log'),/CS0246/);a.match(read('QA/EVIDENCE/logs/phase-2b-role-replacement-build-final.log'),/0 Error\(s\)/);
a.match(read('QA/EVIDENCE/logs/phase-2b-role-replacement-tests.log'),/Failed:\s+0, Passed:\s+44, Skipped:\s+0, Total:\s+44/);
const report='QA/REPORTS/PHASE_2B_STAFF_ROLE_REPLACEMENT.md';for(const x of read(report).matchAll(/\]\(([^)]+)\)/g))a.ok(fs.existsSync(p.resolve(root,p.dirname(report),x[1])),x[1]);
const register=read('QA/REPORTS/AI_REMEDIATION_REGISTER.md'),rows=register.split(/\r?\n/).filter(x=>/^\| \[BUG-/.test(x));a.equal(rows.length,116);
const count=status=>rows.filter(x=>x.includes('| '+status+' |')).length;a.equal(count('PARTIALLY FIXED'),56);a.equal(count('OPEN'),58);a.equal(count('UNABLE TO VERIFY'),2);
for(const[k,v]of Object.entries({applicationChanged:true,productScope:'Staff.UpdateRole only',azure:'UNCHANGED',commitPushDeploy:'NOT DONE',priorFullSuitesRerun:false,backendTargetedPassed:44,nativeAcceptedObservations:94,forcedRollbackCases:21,broaderSessionAcceptance:'OPEN',releaseAcceptance:'OPEN',aiImplementationStarted:false,cleanupComplete:true}))a.equal(receipt[k],v,k);
a.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),before.commit);a.equal(receipt.commit,before.commit);a.equal(cp.spawnSync('git',['-c','core.safecrlf=false','diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS staff-role successor consistency:1019 immutable pins;44 targeted backend tests;37 core+56 extended+1 grant read native observations;21 rollback/21 retries;owned cleanup;Azure/AI unchanged,broader gates OPEN.');
