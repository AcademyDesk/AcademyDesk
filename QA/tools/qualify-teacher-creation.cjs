'use strict';
// Reconcile actual retained receipts/current source/live restoration. This never
// runs a model, changes Docker or turns local host checks into release approval.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const digest=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
function command(binary,args,cwd=root){return cp.execFileSync(binary,args,{cwd,windowsHide:true,timeout:30000,encoding:'utf8',stdio:['ignore','pipe','pipe']}).trim();}
async function qualify(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');const[run,frontendRun]=process.argv.slice(2);a.ok([3,4].includes(process.argv.length));a.match(run||'',/^[0-9a-f]{32}$/);
 if(frontendRun)a.match(frontendRun,/^[0-9a-f]{32}$/);
 const folder=path.join(root,'QA/EVIDENCE/teacher-creation-window-'+run),parent=JSON.parse(fs.readFileSync(path.join(folder,'result.json')));
 for(const key of['passed','validationComplete','restorationPassed','inventoryPreserved','sourceUnchanged'])a.equal(parent[key],true,key);
 a.equal(parent.productionApproved,false);a.equal(parent.modelLoaded,false);a.equal(parent.downloaded,false);a.match(parent.sqlRun,/^[0-9a-f]{32}$/);
 a.deepEqual(require('./api-image-build.cjs').inputs(),parent.sourcePins);a.ok(parent.validationPins.length>=7);
 const frontendFolder=frontendRun?path.join(root,'QA/EVIDENCE/teacher-frontend-'+frontendRun):folder;
 const front=frontendRun?JSON.parse(fs.readFileSync(path.join(frontendFolder,'result.json'))):parent;
 const replaced=new Set(frontendRun?['apps/web/src/lib/penta-teacher-operation.ts','QA/tools/penta-teacher-operation.test.cjs']:[]);
 let archivedRunner=false;
 for(const pin of parent.validationPins)if(!replaced.has(pin.path)){
  if(pin.path==='QA/tools/teacher-creation-window.cjs' && digest(path.join(root,pin.path))!==pin.sha256){
   a.equal(digest(path.join(folder,'executed-runner.cjs')),pin.sha256,'Executed backend runner archive mismatch');archivedRunner=true;
  }else a.equal(digest(path.join(root,pin.path)),pin.sha256,'Changed backend or retained validation input: '+pin.path);
 }
 if(frontendRun){
  for(const key of['passed','validationComplete','restorationPassed','inventoryPreserved','sourceUnchanged'])a.equal(front[key],true,'frontend '+key);
  a.deepEqual(front.apiSourcePins,parent.sourcePins);a.equal(front.modelLoaded,false);a.equal(front.modelInference,false);a.equal(front.downloaded,false);a.equal(front.productionApproved,false);
  for(const pin of front.sourcePins)a.equal(digest(path.join(root,pin.path)),pin.sha256,'Changed final frontend input: '+pin.path);
  for(const file of replaced)a.ok(front.sourcePins.some(pin=>pin.path===file));
 }
 const tests=require('./ci-release-artifacts.cjs').counters(fs.readFileSync(path.join(folder,'api.trx'),'utf8'));a.deepEqual(tests,parent.tests);a.ok(tests.total>=3049);
 a.ok(parent.steps.every(s=>s.exitCode===0&&!s.timedOut));
 const nativePath=path.join(root,'QA/EVIDENCE/teacher-creation-sql-'+parent.sqlRun+'/result.json');
 const native=JSON.parse(fs.readFileSync(nativePath));a.equal(native.passed,true);a.equal(native.checks,15);a.equal(native.created,4);a.equal(native.accountsCreated,0);a.equal(native.modelInference,false);a.equal(native.newAiToolRegistered,false);
 const workflowPath=path.join(root,'QA/EVIDENCE/penta-teacher-workflow-sql-'+parent.sqlRun+'/result.json');const workflow=JSON.parse(fs.readFileSync(workflowPath));
 a.deepEqual(workflow,parent.workflow);a.equal(workflow.passed,true);a.equal(workflow.checks,30);a.equal(workflow.createdTeachers,3);a.equal(workflow.executionAudits,3);a.equal(workflow.accountsCreated,0);a.equal(workflow.modelInference,false);a.equal(workflow.toolRegistered,false);
 a.deepEqual(workflow.simultaneousStatuses,[200,201]);a.deepEqual(workflow.duplicateStatuses,[201,409]);
 const sql=fs.readFileSync(path.join(folder,'sql.log'),'utf8');a.match(sql,/AVAILABILITY all 45 SQL\/Identity\/HTTP cases/);a.match(sql,/run-owned database and login removed/);a.match(sql,/QA TeacherAvailability exit=0/);
 const clientChecks=frontendRun?52:parent.frontendChecks;a.ok(clientChecks>=50);a.equal(frontendRun?front.clientChecks:parent.frontendChecks,clientChecks);
 for(const name of['frontend-checks','frontend-types','frontend-lint'])a.ok(front.steps.some(s=>s.name===name&&s.exitCode===0&&!s.timedOut));
 const client=fs.readFileSync(path.join(frontendFolder,'frontend-checks.log'),'utf8');a.match(client,new RegExp('# tests '+clientChecks+'\\b'));a.match(client,/# fail 0/);
 for(const row of parent.restored){const actual=JSON.parse(command('docker',['inspect',row.id]))[0];a.equal(actual.Id,row.id);a.equal(actual.Name,'/'+row.name);a.equal(actual.Image,row.image);a.equal(actual.State.Running,true);if(actual.State.Health)a.equal(actual.State.Health.Status,'healthy');}
 a.equal((await fetch('http://127.0.0.1:8000/health',{signal:AbortSignal.timeout(10000)})).status,200);
 a.equal(command('docker',['ps','-aq','--filter','label=academydesk.qa.run='+parent.sqlRun]),'');
 const inventory=Object.fromEntries([['containers',['ps','-aq','--no-trunc']],['volumes',['volume','ls','-q']],['networks',['network','ls','-q','--no-trunc']],['images',['image','ls','-aq','--no-trunc']]].map(([key,args])=>[key,[...new Set(command('docker',args).split(/\r?\n/).filter(Boolean))].sort()]));
 a.deepEqual(inventory,parent.before);a.deepEqual(inventory,front.before);a.ok(!fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')));
 a.equal(command('git',['branch','--show-current']),'codex/penta-search');a.equal(command('git',['rev-parse','HEAD']),'b1f8cb6f4209da7fbd14f8089fcb6792438684c4');
 const main='D:\\AcademyDesk';a.equal(command('git',['rev-parse','HEAD'],main),'f7af512f884ca5fe8ac3ddb287c852492592e709');
 const mainState=cp.execFileSync('git',['status','--porcelain'],{cwd:main,windowsHide:true,stdio:['ignore','pipe','pipe']});a.equal(crypto.createHash('sha256').update(mainState).digest('hex'),'75d60408556517b8cf32f57b34baa18a3c557c9fb01cae407f47964db9079f6f');
 const resultFolder=path.join(root,'QA/EVIDENCE/penta-teacher-qualified-'+crypto.randomUUID().replaceAll('-',''));fs.mkdirSync(resultFolder);
 const ci=cp.spawnSync(process.execPath,['--test','--test-reporter=tap','QA/tools/ci-release-artifacts.test.cjs','QA/tools/ci-workflow.test.cjs'],{cwd:root,windowsHide:true,timeout:30000,encoding:'utf8'});
 fs.writeFileSync(path.join(resultFolder,'ci-guards.tap'),ci.stdout+ci.stderr);a.equal(ci.status,0);a.match(ci.stdout,/# tests 32/);a.match(ci.stdout,/# fail 0/);
 const receipts=['result.json','api.trx','sql.log'].map(p=>path.join(folder,p)).concat(['result.json','frontend-checks.log','frontend-types.log','frontend-lint.log'].map(p=>path.join(frontendFolder,p)),[nativePath,workflowPath,path.join(resultFolder,'ci-guards.tap')]);
 if(archivedRunner)receipts.push(path.join(folder,'executed-runner.cjs'));
 const result={passed:true,run,frontendRun:frontendRun||null,sqlRun:parent.sqlRun,tests,teacherServiceSqlChecks:15,retainedAvailabilityCases:45,workflowSqlChecks:30,clientChecks,ciGuardChecks:32,
  simultaneousStatuses:workflow.simultaneousStatuses,distinctDraftDuplicateStatuses:workflow.duplicateStatuses,typecheck:true,lint:true,ownedCleanup:true,servicesRestored:true,inventoryPreserved:true,leaseReleased:true,mainUnchanged:true,
  modelLoaded:false,modelInference:false,toolRegistered:false,visibleUiAcceptance:false,githubCiExecuted:false,productionApproved:false,
  qualifierSha256:digest(__filename),backendRunnerArchived:archivedRunner,currentRunnerSha256:digest(path.join(root,'QA/tools/teacher-creation-window.cjs')),
  evidencePins:receipts.map(file=>({path:path.relative(root,file).replaceAll('\\','/'),sha256:digest(file)})),validationPins:parent.validationPins.filter(p=>!replaced.has(p.path)),frontendPins:frontendRun?front.sourcePins:[],
  guardPins:['QA/tools/ci-release-artifacts.test.cjs','QA/tools/ci-workflow.test.cjs','.github/workflows/verify.yml','.github/workflows/deploy-azure.yml'].map(p=>({path:p,sha256:digest(path.join(root,p))}))};
 fs.writeFileSync(path.join(resultFolder,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({passed:true,evidence:path.relative(root,resultFolder),tests,workflowSqlChecks:30,clientChecks,ciGuardChecks:32,mainUnchanged:true,productionApproved:false}));
}
qualify().catch(error=>{console.error('Teacher source/evidence qualification refused: '+String(error.message).slice(0,180));process.exitCode=1;});
