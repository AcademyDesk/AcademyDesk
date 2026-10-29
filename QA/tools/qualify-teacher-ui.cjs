'use strict';
// Read-only final linkage. Native UI/SQL receipts are bounded acceptance for this
// screen, not inference qualification, all portals/devices or Azure release.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const command=(binary,args,cwd=root)=>cp.execFileSync(binary,args,{cwd,windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();
async function main(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.equal(process.argv.length,4);const[frontRun,browserRun]=process.argv.slice(2);for(const id of[frontRun,browserRun])a.match(id||'',/^[a-f0-9]{32}$/);
 const frontPath=path.join(root,'QA/EVIDENCE/teacher-frontend-'+frontRun),browserPath=path.join(root,'QA/EVIDENCE/teacher-browser-window-'+browserRun);
 const front=JSON.parse(fs.readFileSync(path.join(frontPath,'result.json'))),browser=JSON.parse(fs.readFileSync(path.join(browserPath,'result.json')));
 for(const current of[front,browser]){
  for(const key of['passed','validationComplete','restorationPassed','inventoryPreserved','sourceUnchanged'])a.equal(current[key],true,key);
  a.equal(current.modelLoaded,false);a.equal(current.downloaded,false);a.equal(current.productionApproved,false);a.ok(current.steps.every(s=>s.exitCode===0&&!s.timedOut));
 }
 a.equal(front.webBuild,browser.webBuild);a.equal(front.clientChecks,52);for(const pin of front.sourcePins)a.equal(hash(path.join(root,pin.path)),pin.sha256);
 a.deepEqual(require('./api-image-build.cjs').inputs(),browser.apiSourcePins);a.deepEqual(front.apiSourcePins,browser.apiSourcePins);for(const pin of browser.harnessPins)a.equal(hash(path.join(root,pin.path)),pin.sha256);
 const build=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/default-web-build-'+browser.webBuild+'/result.json')));a.equal(build.passed,true);a.equal(build.downloaded,false);a.equal(build.installed,false);a.equal(build.sourceUnchanged,true);
 a.deepEqual(require('./default-web-build.cjs').sourcePins(path.join(root,'apps/web')),build.sourcePins);a.deepEqual(build.sourcePins,browser.sourcePins);
 const exported=path.join(root,'.build-check/default-web-build',browser.webBuild,'app/out'),files=require('./default-web-build.cjs').inventory(exported).map(({file,bytes})=>({path:path.relative(exported,file).replaceAll('\\','/'),bytes,sha256:hash(file)}));a.deepEqual(files,browser.exportPins);
 const corePath=path.join(root,'QA/EVIDENCE/penta-teacher-core-qualified-c8ed13746d9c4cfbbcdfa2b4632f9cc2/result.json'),core=JSON.parse(fs.readFileSync(corePath));a.equal(core.passed,true);a.deepEqual(core.apiSourcePins,browser.apiSourcePins);a.deepEqual(core.tests,{total:3065,passed:3065,skipped:0});a.equal(core.sharedContractSdkChecks,11);a.equal(core.ciGuardChecks,33);
 for(const pin of core.evidencePins)a.equal(hash(path.join(root,pin.path)),pin.sha256);
 const nativePath=path.join(root,'QA/EVIDENCE/penta-teacher-live-'+browser.sqlRun),native=JSON.parse(fs.readFileSync(path.join(nativePath,'result.json'))),post=JSON.parse(fs.readFileSync(path.join(nativePath,'sql-postconditions.json')));
 a.equal(native.passed,true);a.equal(native.sqlBacked,true);a.equal(native.syntheticApiResponses,false);a.equal(native.modelInference,false);a.equal(native.webBuild,browser.webBuild);a.equal(native.checks.length,6);
 a.deepEqual(native.checks.map(x=>x.name),['light-mobile','dark-desktop','lost-response','cancelled','teacher','unavailable']);
 for(const check of native.checks){a.equal(check.passed,true);a.deepEqual(check.errors,[]);a.ok(check.calls.every(c=>c.status!==429));}
 a.equal(native.checks.find(c=>c.name==='lost-response').lostResponseAfterCommit,true);
 for(const key of['passed','unrelatedStateUnchanged'])a.equal(post[key],true);a.equal(post.createdTeachers,3);a.equal(post.executionAudits,3);a.equal(post.succeededDrafts,3);a.equal(post.cancelledDrafts,1);a.equal(post.accountChanges,false);a.equal(post.modelInference,false);a.equal(post.actualHttpRequests,browser.actualHttpRequests);
 const log=fs.readFileSync(path.join(browserPath,'sql-browser.log'),'utf8');a.match(log,/PENTATEACHERCORE all 21 real SQL\/HTTP/);a.match(log,/PENTATEACHER all 30 real SQL\/HTTP/);a.match(log,/TEACHERCREATION all 15 real SQL\/HTTP/);a.match(log,/AVAILABILITY all 45 SQL\/Identity\/HTTP/);a.match(log,/run-owned database and login removed/);
 a.equal(command('docker',['ps','-aq','--filter','label=academydesk.qa.run='+browser.sqlRun]),'');
 for(const row of browser.restored){const current=JSON.parse(command('docker',['inspect',row.id]))[0];a.equal(current.Image,row.image);a.equal(current.State.Running,true);if(current.State.Health)a.equal(current.State.Health.Status,'healthy');}
 a.equal((await fetch('http://127.0.0.1:8000/health',{signal:AbortSignal.timeout(10000)})).status,200);
 const inventory=Object.fromEntries([['containers',['ps','-aq','--no-trunc']],['volumes',['volume','ls','-q']],['networks',['network','ls','-q','--no-trunc']],['images',['image','ls','-aq','--no-trunc']]].map(([key,args])=>[key,[...new Set(command('docker',args).split(/\r?\n/).filter(Boolean))].sort()]));a.deepEqual(inventory,browser.before);a.deepEqual(inventory,front.before);a.ok(!fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')));
 const mainRoot='D:\\AcademyDesk';a.equal(command('git',['rev-parse','HEAD'],mainRoot),'f7af512f884ca5fe8ac3ddb287c852492592e709');const mainState=cp.execFileSync('git',['status','--porcelain'],{cwd:mainRoot,windowsHide:true,stdio:['ignore','pipe','pipe']});a.equal(crypto.createHash('sha256').update(mainState).digest('hex'),'75d60408556517b8cf32f57b34baa18a3c557c9fb01cae407f47964db9079f6f');
 a.equal(command('git',['branch','--show-current']),'codex/penta-search');a.equal(command('git',['rev-parse','HEAD']),'b1f8cb6f4209da7fbd14f8089fcb6792438684c4');
 const screenshots=fs.readdirSync(nativePath).filter(f=>f.endsWith('.png')).sort();a.equal(screenshots.length,8);
 const receiptFiles=[corePath,path.join(frontPath,'result.json'),path.join(browserPath,'result.json'),path.join(browserPath,'sql-browser.log'),path.join(nativePath,'result.json'),path.join(nativePath,'sql-postconditions.json'),...screenshots.map(p=>path.join(nativePath,p))];
 const out=path.join(root,'QA/EVIDENCE/penta-teacher-ui-qualified-'+crypto.randomUUID().replaceAll('-',''));fs.mkdirSync(out);
 const result={passed:true,frontRun,browserRun,webBuild:browser.webBuild,sqlRun:browser.sqlRun,apiTests:3065,coreSqlChecks:21,workflowSqlChecks:30,serviceSqlChecks:15,availabilityCases:45,sharedSdkChecks:11,clientChecks:52,ciGuardChecks:33,browserJourneys:6,actualHttpRequests:post.actualHttpRequests,createdTeachers:3,executionAudits:3,staticPages:build.staticPages,exportFiles:files.length,screenshots,
  screenshotHumanReview:'PENDING',physicalDevices:'NOT RUN',modelLoaded:false,modelInference:false,productionApproved:false,mainUnchanged:true,ownedCleanup:true,servicesRestored:true,inventoryPreserved:true,leaseReleased:true,sourceUnchanged:true,
  qualifierSha256:hash(__filename),evidencePins:receiptFiles.map(p=>({path:path.relative(root,p).replaceAll('\\','/'),sha256:hash(p)}))};
 fs.writeFileSync(path.join(out,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({passed:true,evidence:path.relative(root,out),apiTests:3065,browserJourneys:6,actualHttpRequests:post.actualHttpRequests,exportFiles:files.length,screenshotReview:'PENDING',productionApproved:false}));
}
main().catch(e=>{console.error('Teacher UI qualification refused: '+String(e.message).slice(0,180));process.exitCode=1;});
