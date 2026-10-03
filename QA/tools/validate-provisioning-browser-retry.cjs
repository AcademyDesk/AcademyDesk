// Preservation and evidence consistency only, not a rerun of product tests.
const fs=require('node:fs'),p=require('node:path'),c=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=p.resolve(__dirname,'../..'),read=f=>fs.readFileSync(p.join(root,f),'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n'),sha=f=>c.createHash('sha256').update(fs.readFileSync(p.join(root,f))).digest('hex');
const before=JSON.parse(read('QA/EVIDENCE/provisioning-browser-retry-before.json')),receipt=JSON.parse(read('QA/EVIDENCE/provisioning-browser-retry-source-receipt.json'));
const allowed=['QA/tools/SqlHarness/BrowserProvisioningHost.cs','QA/ISSUES/BUG-DATA-0011.md','QA/ISSUES/BUG-AUTH-REFRESH-001.md','QA/03_TEST_MATRIX.md','QA/REPORTS/PHASE_2_START_PLAN.md'];
a.equal(before.retainedBefore.length,857);a.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),before.commit);
for(const e of before.retainedBefore)if(!allowed.includes(e.file))a.equal(sha(e.file),e.sha256,e.file);
for(const e of receipt.current)a.equal(sha(e.file),e.sha256,e.file);
const hostSnapshot='QA/EVIDENCE/provisioning-browser-retry-host-before.cs';a.equal(sha(hostSnapshot),before.retainedBefore.find(e=>e.file==='QA/tools/SqlHarness/BrowserProvisioningHost.cs').sha256);
a.equal(read('QA/tools/SqlHarness/BrowserProvisioningHost.cs'),read(hostSnapshot).replace('IsActive = true, DisplayName = "Synthetic UI Owner"','IsActive = true, IsPlatformOwner = true, DisplayName = "Synthetic UI Owner"'));
const prefix='QA/EVIDENCE/provisioning-browser-retry-',snapshot=n=>read(prefix+n+'.txt');
for(const [n,re] of [['student-success',/status: Created student portal account/],['teacher-success',/status: Created teacher portal account/],['duplicate',/alert: Username[\s\S]*already taken. No changes were saved.[\s\S]*Synthetic duplicate draft/],['academy-success',/Total academies 3[\s\S]*Synthetic UI Academy[\s\S]*status: Academy and Academy Admin created/]])a.match(snapshot(n),re);
a.doesNotMatch(snapshot('academy-success'),/- dialog "Onboard academy"/);
for(const [n,path,identity] of [['student-routing','portal','Isolated-A Fixture'],['student-preserved','portal','Isolated-A Fixture'],['teacher-routing','teacher','Synthetic Teacher'],['teacher-preserved','teacher','Synthetic Teacher'],['admin-routing','dashboard','qa-ui-admin@example.invalid'],['owner-routing','platform','Synthetic UI Owner']]){a.ok(snapshot(n).includes('http://127.0.0.1:49151/'+path));a.ok(snapshot(n).includes(identity));}
const log=n=>read('QA/EVIDENCE/logs/phase-2b-provisioning-browser-retry-'+n+'.log');
const host=log('sql');a.match(host,/PROVISIONUI READY run=c375f6326a7d4c8ba8c02909b7c5cec7/);a.match(host,/routes=313/);a.doesNotMatch(host,/PROVISIONUI SQL PASS|QA BrowserProvisioning exit=0/);
a.deepEqual([...host.matchAll(/PROVISIONUI HTTP POST \/api\/academies\/[0-9a-f-]+\/portal-accounts (\d+)/g)].map(x=>+x[1]),[200,400,200]);
a.match(host,/PROVISIONUI HTTP POST \/api\/platform\/academies 201/);
a.match(log('fixture-correction'),/Msg 1934/);a.match(log('fixture-correction-final'),/Msg 8169/);a.match(log('fixture-correction-verified'),/FIXTURE PASS owner flag0->1;exactly one owned synthetic identity/);
a.match(log('independent-sql'),/INDEPENDENT SQL PASS exactly three browser-created users;expected roles\/academy associations and student\/teacher typed links;one unique onboarded academy/);
const cleanup=JSON.parse(read(prefix+'cleanup.json'));a.equal(cleanup.containerStopped,true);a.equal(cleanup.containerRetained,true);a.equal(cleanup.rootRetained,true);a.equal(cleanup.listenersRemaining,0);
const report='QA/REPORTS/PHASE_2B_PROVISIONING_BROWSER_RETRY.md';for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))a.ok(fs.existsSync(p.resolve(root,p.dirname(report),link[1])),link[1]);
for(const [k,v] of Object.entries({applicationChanged:false,azure:'UNCHANGED',commitPushDeploy:'NOT DONE',closure:'OPEN',screenshots:0,priorSuitesRerun:false,interruptedHostAccepted:false,independentSqlAccepted:true,cleanupComplete:false}))a.equal(receipt[k],v,k);
a.equal(cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}).status,0);
console.log('PASS provisioning-browser consistency:actual success/reset/duplicate draft/new-account routing/workspace preservation;independent SQL3 accounts/one academy;857 predecessor pins,product/HEAD/Azure unchanged. Interrupted host not fully accepted;stopped disposable fixture retained,cleanup/Phase2B/release OPEN.');
