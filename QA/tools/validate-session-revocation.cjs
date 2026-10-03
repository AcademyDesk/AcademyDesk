// Source/evidence consistency. Does not rerun accepted suites or promote security failures.
const fs=require('node:fs'),p=require('node:path'),c=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=p.resolve(__dirname,'../..'),read=f=>fs.readFileSync(p.join(root,f),'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n'),sha=f=>c.createHash('sha256').update(fs.readFileSync(p.join(root,f))).digest('hex');
const before=JSON.parse(read('QA/EVIDENCE/session-revocation-before.json')),receipt=JSON.parse(read('QA/EVIDENCE/session-revocation-source-receipt.json'));
const allowed=['QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1','QA/ISSUES/BUG-AUTH-REFRESH-001.md','QA/03_TEST_MATRIX.md','QA/REPORTS/PHASE_2_START_PLAN.md'];
a.equal(before.retainedBefore.length,883);a.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),before.commit);
for(const e of before.retainedBefore)if(!allowed.includes(e.file))a.equal(sha(e.file),e.sha256,e.file);
for(const e of receipt.current)a.equal(sha(e.file),e.sha256,e.file);
for(const [source,snapshot] of [['QA/tools/SqlHarness/Program.cs','QA/EVIDENCE/session-revocation-program-before.cs'],['QA/tools/SqlHarness/Run-ReconciledPayment.ps1','QA/EVIDENCE/session-revocation-runner-before.ps1']])a.equal(sha(snapshot),before.retainedBefore.find(x=>x.file===source).sha256);
const oldProgram=read('QA/EVIDENCE/session-revocation-program-before.cs');
a.equal(read('QA/tools/SqlHarness/Program.cs'),oldProgram.replace('args[3] != "--audit-refresh-concurrency"','args[3] != "--audit-session-revocation" && args[3] != "--audit-refresh-concurrency"').replace('            if (auditMode == "--audit-session-refresh")','            if (auditMode == "--audit-session-revocation") { await VerifySessionRevocationAsync(factory, client); return; }\n            if (auditMode == "--audit-session-refresh")'));
const oldRunner=read('QA/EVIDENCE/session-revocation-runner-before.ps1');
a.equal(read('QA/tools/SqlHarness/Run-ReconciledPayment.ps1'),oldRunner.replaceAll("'MakeupLocation','RefreshConcurrency'","'MakeupLocation','SessionRevocation','RefreshConcurrency'").replace("    if ($Module -eq 'SessionRefresh')","    if ($Module -eq 'SessionRevocation') { $qaSwitch = '--audit-session-revocation' }\n    if ($Module -eq 'SessionRefresh')").replace("            if ($Module -eq 'SessionRefresh')","            if ($Module -eq 'SessionRevocation') { $qaArtifact = 'session-revocation-sql' }\n            if ($Module -eq 'SessionRefresh')").replace('PROVISIONUI|SESSIONREFRESH','PROVISIONUI|SESSIONREVOCATION|SESSIONREFRESH'));
const log=name=>read('QA/EVIDENCE/logs/phase-2b-session-revocation-'+name+'.log');
const failures=['disabled-login','disabled-refresh','password-change-old-access','deleted-old-protected-academies'];
const actual={ 'disabled-original-access':403,'disabled-login':200,'disabled-login-issued-access':403,'disabled-refresh':200,'disabled-refresh-issued-access':403,'password-change-old-access':200,'password-change-old-refresh':401,'password-change-old-login':401,'deleted-old-session':401,'deleted-old-refresh':401,'deleted-old-login':401,'deleted-old-protected-academies':200 };
let first;
for(const name of ['sql','repeat-sql']){
  const text=log(name),cases=[...text.matchAll(/^SESSIONREVOCATION CASE (.+)$/gm)].map(x=>JSON.parse(x[1]));
  a.equal(cases.length,30);a.equal(new Set(cases.map(x=>x.id)).size,30);
  a.deepEqual(cases.filter(x=>!x.accepted).map(x=>x.id),failures);
  for(const [id,status] of Object.entries(actual))a.equal(cases.find(x=>x.id===id).actual,status,id);
  if(first)a.deepEqual(cases,first);else first=cases;
  const summary=JSON.parse(text.match(/^SESSIONREVOCATION SUMMARY (.+)$/m)[1]);
  a.deepEqual(summary,{requests:30,cases:30,policyGaps:4,diagnosticComplete:true,acceptance:'FAIL / OPEN',productChanged:false});
  a.match(text,/Runtime inventory PASS: routes=313/);a.match(text,/both resolved DbContexts match exact owned target/);
  a.match(text,/SESSIONREVOCATION SQL stamps: rejected-change unchanged; successful native password change rotated/);
  a.match(text,/SESSIONREVOCATION SQL PASS fresh contexts: original identity\/count\/role\/academy\/typed links\/active flags preserved/);
  a.match(text,/Negative cleanup check refused a mismatched database marker/);
  a.match(text,/PASS: application migrations=82, identity migrations=7/);a.match(text,/QA SessionRevocation exit=0/);
  a.doesNotMatch(text,/Unhandled exception|Regression failed/);
}
a.match(log('build'),/Build succeeded\.[\s\S]*0 Warning\(s\)[\s\S]*0 Error\(s\)/);
const cleanup=JSON.parse(read('QA/EVIDENCE/session-revocation-cleanup.json'));
a.equal(cleanup.runs.length,2);a.notEqual(cleanup.runs[0].run,cleanup.runs[1].run);
const containers=cp.execFileSync('docker',['ps','-a','--format','{{.Names}}'],{encoding:'utf8'}).trim().split(/\r?\n/);
for(const r of cleanup.runs){a.equal(r.containerPresent,false);a.equal(r.rootPresent,false);a.equal(r.listeners,0);a.ok(!fs.existsSync(r.root));a.ok(!containers.includes(r.container));a.ok(log(r===cleanup.runs[0]?'sql':'repeat-sql').includes('run='+r.run));}
a.equal(cleanup.previousBrowserFixture,'stopped and retained; no deletion retry');
const report='QA/REPORTS/PHASE_2B_SESSION_REVOCATION_DIAGNOSTIC.md';
for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))if(!link[1].startsWith('https://'))a.ok(fs.existsSync(p.resolve(root,p.dirname(report),link[1])),link[1]);
for(const [k,v] of Object.entries({applicationChanged:false,azure:'UNCHANGED',commitPushDeploy:'NOT DONE',closure:'OPEN',priorSuitesRerun:false,uniqueNativeCases:30,nativeExecutions:60,policyGapsPerRun:4,securityAcceptance:'FAIL / OPEN',cleanupComplete:true}))a.equal(receipt[k],v,k);
a.equal(cp.spawnSync('git',['-c','core.safecrlf=false','diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS diagnostic evidence consistency:two fresh SQL runs30 native cases each,same four policy failures;883 predecessor pins,product/HEAD preserved;new fixtures cleaned. Security FAIL / OPEN,no product fix/deploy;accepted suites not rerun.');
