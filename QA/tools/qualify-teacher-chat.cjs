'use strict';
// Reconcile actual UI/SQL evidence and retained API source. No inference/release authority.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const run=(binary,args,cwd=root)=>cp.execFileSync(binary,args,{cwd,windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();
async function main(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.equal(process.argv.length,4);const[frontRun,browserRun]=process.argv.slice(2);for(const id of[frontRun,browserRun])a.match(id||'',/^[a-f0-9]{32}$/);
 const frontDir=path.join(root,'QA/EVIDENCE/teacher-frontend-'+frontRun),browserDir=path.join(root,'QA/EVIDENCE/teacher-chat-browser-window-'+browserRun);
 const front=JSON.parse(fs.readFileSync(path.join(frontDir,'result.json'))),native=JSON.parse(fs.readFileSync(path.join(browserDir,'result.json')));
 for(const proof of[front,native]){
  for(const key of['passed','validationComplete','sourceUnchanged','restorationPassed','inventoryPreserved'])a.equal(proof[key],true,key);
  a.ok(proof.steps.every(s=>s.exitCode===0&&!s.timedOut));a.equal(proof.modelLoaded,false);a.equal(proof.downloaded,false);a.equal(proof.productionApproved,false);
 }
 a.equal(front.clientChecks,52);a.equal(front.startClientChecks,61);a.equal(front.dialogueClientChecks,54);a.equal(native.retainedSqlChecks,200);a.equal(native.browserChecks,6);a.equal(native.chatBrowserChecks,5);a.equal(front.webBuild,native.webBuild);
 a.deepEqual(require('./api-image-build.cjs').inputs(),native.apiSourcePins);a.deepEqual(front.apiSourcePins,native.apiSourcePins);
 for(const pin of [...front.sourcePins,...native.harnessPins])a.equal(hash(path.join(root,pin.path)),pin.sha256,pin.path);
 const helper=require('./default-web-build.cjs'),build=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/default-web-build-'+native.webBuild+'/result.json')));
 a.equal(build.passed,true);a.equal(build.downloaded,false);a.deepEqual(helper.sourcePins(path.join(root,'apps/web')),build.sourcePins);a.deepEqual(native.sourcePins,build.sourcePins);
 const exported=path.join(root,'.build-check/default-web-build',native.webBuild,'app/out');
 a.deepEqual(helper.inventory(exported).map(({file,bytes})=>({path:path.relative(exported,file).replaceAll('\\','/'),bytes,sha256:hash(file)})),native.exportPins);
 const baseline=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/penta-teacher-start-qualified-68c078d345284c3a9ecc992e273f7f61/result.json')));
 a.equal(baseline.passed,true);a.deepEqual(baseline.apiSourcePins,native.apiSourcePins);for(const pin of baseline.validationPins.filter(p=>p.path.startsWith('tests/')))a.equal(hash(path.join(root,pin.path)),pin.sha256);
 const trx=path.join(root,'QA/EVIDENCE/teacher-creation-window-'+baseline.run+'/api.trx'),tests=require('./ci-release-artifacts.cjs').counters(fs.readFileSync(trx,'utf8'));a.deepEqual(tests,{total:3106,passed:3106,skipped:0});
 const sql=native.sqlRun,chatDir=path.join(root,'QA/EVIDENCE/penta-teacher-chat-live-'+sql),reviewDir=path.join(root,'QA/EVIDENCE/penta-teacher-live-'+sql),coreDir=path.join(root,'QA/EVIDENCE/penta-teacher-core-sql-'+sql);
 const chat=JSON.parse(fs.readFileSync(path.join(chatDir,'result.json'))),post=JSON.parse(fs.readFileSync(path.join(chatDir,'sql-postconditions.json'))),review=JSON.parse(fs.readFileSync(path.join(reviewDir,'result.json')));
 a.equal(chat.passed,true);a.equal(chat.checks.length,5);a.ok(chat.checks.every(c=>c.passed));a.equal(chat.syntheticApiResponses,false);a.equal(chat.modelInference,false);
 a.equal(post.passed,true);a.equal(post.createdTeachers,1);a.equal(post.createdAccounts,0);a.equal(post.executionAudits,1);a.equal(post.startedSessions,3);a.equal(post.plannerCalls,6);a.equal(post.turnRows,9);a.equal(post.cancelledDrafts,1);a.equal(post.reviewedDrafts,1);a.equal(post.unrelatedStateUnchanged,true);
 a.equal(review.passed,true);a.equal(review.checks.length,6);a.ok(review.checks.every(c=>c.passed));
 const shared=JSON.parse(fs.readFileSync(path.join(coreDir,'shared-contract-check.json')));a.equal(shared.passed,true);a.equal(shared.checks,11);a.equal(shared.modelLoaded,false);a.equal(shared.inference,false);a.equal(shared.miniSourceChanged,false);
 for(const pin of shared.sharedSourcePins)a.equal(hash(pin.path),pin.sha256);a.equal(hash(path.join(coreDir,'contract-samples.json')),shared.sampleSha256);
 a.equal(run('docker',['ps','-aq','--filter','label=academydesk.qa.run='+sql]),'');a.ok(!fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')));
 for(const s of native.restored){const actual=JSON.parse(run('docker',['inspect',s.id]))[0];a.equal(actual.Image,s.image);a.equal(actual.State.Running,true);if(actual.State.Health)a.equal(actual.State.Health.Status,'healthy');}
 a.equal((await fetch('http://127.0.0.1:8000/health',{signal:AbortSignal.timeout(10000)})).status,200);
 a.equal(run('git',['branch','--show-current']),'codex/penta-search');a.equal(run('git',['rev-parse','HEAD']),'b1f8cb6f4209da7fbd14f8089fcb6792438684c4');
 a.equal(run('git',['rev-parse','HEAD'],'D:\\AcademyDesk'),'f7af512f884ca5fe8ac3ddb287c852492592e709');a.equal(crypto.createHash('sha256').update(cp.execFileSync('git',['status','--porcelain'],{cwd:'D:\\AcademyDesk',windowsHide:true})).digest('hex'),'75d60408556517b8cf32f57b34baa18a3c557c9fb01cae407f47964db9079f6f');
 const evidence=path.join(root,'QA/EVIDENCE/penta-teacher-chat-qualified-'+crypto.randomUUID().replaceAll('-',''));fs.mkdirSync(evidence);
 const guards=cp.spawnSync(process.execPath,['--test','--test-reporter=tap','QA/tools/ci-release-artifacts.test.cjs','QA/tools/ci-workflow.test.cjs'],{cwd:root,windowsHide:true,encoding:'utf8',timeout:30000});a.equal(guards.status,0);a.match(guards.stdout,/# tests 35/);a.match(guards.stdout,/# fail 0/);fs.writeFileSync(path.join(evidence,'ci-guards.tap'),guards.stdout+guards.stderr);
 const files=[trx,...['result.json','frontend-dialogue-checks.log','frontend-types.log','frontend-lint.log'].map(p=>path.join(frontDir,p)),...['result.json','sql-browser.log'].map(p=>path.join(browserDir,p)),...['result.json','sql-postconditions.json'].map(p=>path.join(chatDir,p)),path.join(reviewDir,'result.json'),path.join(coreDir,'shared-contract-check.json'),path.join(evidence,'ci-guards.tap')];
 const result={passed:true,frontRun,browserRun,sqlRun:sql,webBuild:native.webBuild,tests,clientChecks:167,retainedSqlChecks:200,chatBrowserChecks:5,retainedReviewBrowserChecks:6,sharedContractSdkChecks:11,ciGuardChecks:35,
  actualChatHttpRequests:post.actualHttpRequests,actualReviewHttpRequests:native.actualHttpRequests,apiSourcePins:native.apiSourcePins,webSourcePins:build.sourcePins,harnessPins:native.harnessPins,frontendSourcePins:front.sourcePins,
  servicesRestored:true,ownedSqlCleanup:true,leaseReleased:true,mainUnchanged:true,modelLoaded:false,modelInference:false,syntheticInterpreter:true,productionApproved:false,naturalLanguageQualified:false,globalIntentRoutingQualified:false,
  screenshotReview:'PENDING',qualifierSha256:hash(__filename),evidencePins:files.map(p=>({path:path.relative(root,p).replaceAll('\\','/'),sha256:hash(p)}))};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({passed:true,evidence:path.relative(root,evidence),tests,clientChecks:167,retainedSqlChecks:200,browserJourneys:11,productionApproved:false}));
}
main().catch(e=>{console.error('Teacher chat qualifier refused: '+String(e.message).slice(0,180));process.exitCode=1;});
