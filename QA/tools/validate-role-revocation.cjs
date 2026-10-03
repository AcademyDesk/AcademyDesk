// Diagnostic consistency only; reproduced policy gaps remain OPEN.
const fs=require('node:fs'),p=require('node:path'),c=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=p.resolve(__dirname,'../..'),read=f=>fs.readFileSync(p.join(root,f),'utf8').replace(/^\uFEFF/,''),json=f=>JSON.parse(read(f)),sha=f=>c.createHash('sha256').update(fs.readFileSync(p.join(root,f))).digest('hex');
const before=json('QA/EVIDENCE/role-revocation-before.json'),receipt=json('QA/EVIDENCE/role-revocation-source-receipt.json');
const pins=new Map(before.predecessors.flatMap(f=>{const x=json(f);return x.retainedBefore||x.current;}).map(x=>[x.file,x])),allowed=new Set(before.files.map(x=>x.file));let retained=0;
for(const [file,x]of pins){if(allowed.has(file))continue;a.equal(sha(file),x.sha256,file);retained++;}
a.equal(pins.size,1007);a.equal(retained,1003);
for(const x of before.files){a.equal(sha(x.backup),x.sha256,x.backup);if(pins.has(x.file))a.equal(pins.get(x.file).sha256,x.sha256,x.file);}
for(const x of receipt.current)a.equal(sha(x.file),x.sha256,x.file);
const observed=json('QA/EVIDENCE/role-revocation-observations.json');
a.equal(observed.native,true);a.equal(observed.diagnosticOnly,true);a.equal(observed.productChanged,false);a.equal(observed.runs.length,2);a.equal(new Set(observed.runs.map(x=>x.run)).size,2);
for(const run of observed.runs){
 a.equal(run.cases.length,37);a.equal(run.cases.filter(x=>x.accepted).length,35);
 a.deepEqual(run.cases.filter(x=>!x.accepted).map(x=>x.id).sort(),['operations-only-expected-finance-denial','role-response-matches-persisted-memberships']);
 a.deepEqual(run.summary,{requests:30,observations:37,acceptedControls:35,policyGaps:2,unchangedSnapshots:18,diagnosticOnly:true,nativeIdentitySql:true});
 a.match(read(run.file),/exit=0/);
}
const cleanup=json('QA/EVIDENCE/role-revocation-cleanup.json');a.equal(cleanup.runs.length,2);
for(const x of cleanup.runs){a.equal(x.rootAbsent,true);a.equal(x.containerAbsent,true);a.equal(x.listenersRemaining,0);a.ok(observed.runs.some(y=>y.run===x.run&&y.port===x.port));}
a.equal(cleanup.previousFixtures,'UNCHANGED');a.equal(cleanup.normalDevDatabaseAndServices,'UNCHANGED');
a.match(read('QA/EVIDENCE/logs/phase-2b-role-revocation-build.log'),/0 Error\(s\)/);
const report='QA/REPORTS/PHASE_2B_ROLE_REVOCATION_DIAGNOSTIC.md';for(const x of read(report).matchAll(/\]\(([^)]+)\)/g))a.ok(fs.existsSync(p.resolve(root,p.dirname(report),x[1])),x[1]);
for(const[k,v]of Object.entries({applicationChanged:false,diagnosticOnly:true,roleReplacementAcceptance:'OPEN',azure:'UNCHANGED',commitPushDeploy:'NOT DONE',priorSuitesRerun:false,browser:'NOT RUN',cleanupComplete:true}))a.equal(receipt[k],v,k);
a.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),before.commit);a.equal(receipt.commit,before.commit);a.equal(cp.spawnSync('git',['-c','core.safecrlf=false','diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS diagnostic consistency:1003 immutable predecessor pins;70 accepted controls and4 failure observations of2 repeated policy gaps;no application/Azure changes;role replacement OPEN.');
