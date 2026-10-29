'use strict';
// Reconciles real receipts without rerunning the enterprise suite or granting release authority.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const exec=(binary,args,cwd=root)=>cp.execFileSync(binary,args,{cwd,windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();
async function main(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.ok(process.argv.length===3||process.argv.length===4);const run=process.argv[2];a.match(run||'',/^[a-f0-9]{32}$/);
 const folder=path.join(root,'QA/EVIDENCE/teacher-creation-window-'+run),parent=JSON.parse(fs.readFileSync(path.join(folder,'result.json')));
 for(const key of['passed','validationComplete','sourceUnchanged','restorationPassed','inventoryPreserved'])a.equal(parent[key],true,key);
 a.equal(parent.modelLoaded,false);a.equal(parent.downloaded,false);a.equal(parent.productionApproved,false);a.ok(parent.steps.every(x=>x.exitCode===0&&!x.timedOut));
 a.deepEqual(require('./api-image-build.cjs').inputs(),parent.sourcePins);for(const pin of parent.validationPins)a.equal(hash(path.join(root,pin.path)),pin.sha256,'Changed validation source: '+pin.path);
 const tests=require('./ci-release-artifacts.cjs').counters(fs.readFileSync(path.join(folder,'api.trx'),'utf8'));a.deepEqual(tests,{total:3106,passed:3106,skipped:0});
 const sql=parent.sqlRun;a.match(sql,/^[a-f0-9]{32}$/);
 const chat=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/penta-teacher-conversation-sql-'+sql+'/result.json')));
 a.deepEqual(chat,parent.conversation);a.equal(chat.checks,43);for(const key of['createdTeachers','createdAccounts','executionAudits'])a.equal(chat[key],0);
 const startFolder=path.join(root,'QA/EVIDENCE/penta-teacher-start-sql-'+sql),start=JSON.parse(fs.readFileSync(path.join(startFolder,'result.json')));
 a.deepEqual(start,parent.start);a.equal(start.passed,true);a.equal(start.checks,46);a.equal(start.startSessionsCreated,1);a.deepEqual(start.startStatuses,[200,201]);
 a.equal(start.createdTeachers,1);a.equal(start.createdAccounts,0);a.equal(start.executionAudits,1);a.equal(start.plannerCalls,2);a.equal(start.syntheticInterpreter,true);
 for(const key of['modelInference','toolRegistered','productionApproved'])a.equal(start[key],false);
 a.equal(chat.syntheticInterpreter,true);a.equal(chat.modelInference,false);a.equal(chat.toolRegistered,false);a.equal(chat.productionApproved,false);
 a.equal(parent.core.checks,21);a.equal(parent.workflow.checks,30);a.deepEqual(parent.workflow.simultaneousStatuses,[200,201]);a.deepEqual(parent.workflow.duplicateStatuses,[201,409]);a.equal(parent.native.checks,15);
 const coreFolder=path.join(root,'QA/EVIDENCE/penta-teacher-core-sql-'+sql),shared=JSON.parse(fs.readFileSync(path.join(coreFolder,'shared-contract-check.json')));
 a.equal(shared.passed,true);a.equal(shared.checks,11);for(const key of['modelLoaded','inference','liveHostMutation','miniSourceChanged'])a.equal(shared[key],false);
 let frontend=null,frontendFolder=null;
 if(process.argv[3]){
  a.match(process.argv[3],/^[a-f0-9]{32}$/);frontendFolder=path.join(root,'QA/EVIDENCE/teacher-frontend-'+process.argv[3]);frontend=JSON.parse(fs.readFileSync(path.join(frontendFolder,'result.json')));
  for(const key of['passed','validationComplete','sourceUnchanged','restorationPassed','inventoryPreserved'])a.equal(frontend[key],true,'Frontend '+key);
  for(const key of['productionApproved','modelLoaded','modelInference','downloaded'])a.equal(frontend[key],false);
  a.equal(frontend.clientChecks,52);a.equal(frontend.startClientChecks,61);a.ok(frontend.steps.every(s=>s.exitCode===0&&!s.timedOut));
  a.deepEqual(frontend.apiSourcePins,parent.sourcePins);a.deepEqual(frontend.before,parent.before);for(const pin of frontend.sourcePins)a.equal(hash(path.join(root,pin.path)),pin.sha256);
  const client=JSON.parse(fs.readFileSync(path.join(frontendFolder,'start-transport.json')));a.equal(client.passed,61);a.equal(client.failed,0);a.equal(client.synthetic,true);a.equal(client.sqlAcceptance,false);a.equal(client.modelInference,false);
  a.ok(client.checks.every(c=>c.pass===true));
 }
 a.equal(hash(path.join(coreFolder,'contract-samples.json')),shared.sampleSha256);for(const pin of shared.sharedSourcePins)a.equal(hash(pin.path),pin.sha256);
 const log=fs.readFileSync(path.join(folder,'sql.log'),'utf8');a.match(log,/AVAILABILITY all 45 SQL\/Identity\/HTTP cases/);a.match(log,/run-owned database and login removed/);
 a.equal(exec('docker',['ps','-aq','--filter','label=academydesk.qa.run='+sql]),'');
 for(const row of parent.restored){const actual=JSON.parse(exec('docker',['inspect',row.id]))[0];a.equal(actual.Id,row.id);a.equal(actual.Image,row.image);a.equal(actual.State.Running,true);if(actual.State.Health)a.equal(actual.State.Health.Status,'healthy');}
 a.equal((await fetch('http://127.0.0.1:8000/health',{signal:AbortSignal.timeout(10000)})).status,200);a.ok(!fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')));
 const inventory=Object.fromEntries([['containers',['ps','-aq','--no-trunc']],['volumes',['volume','ls','-q']],['networks',['network','ls','-q','--no-trunc']],['images',['image','ls','-aq','--no-trunc']]].map(([k,args])=>[k,[...new Set(exec('docker',args).split(/\r?\n/).filter(Boolean))].sort()]));a.deepEqual(inventory,parent.before);
 a.equal(exec('git',['branch','--show-current']),'codex/penta-search');a.equal(exec('git',['rev-parse','HEAD']),'b1f8cb6f4209da7fbd14f8089fcb6792438684c4');
 const mainRoot='D:\\AcademyDesk';a.equal(exec('git',['rev-parse','HEAD'],mainRoot),'f7af512f884ca5fe8ac3ddb287c852492592e709');
 a.equal(crypto.createHash('sha256').update(cp.execFileSync('git',['status','--porcelain'],{cwd:mainRoot,windowsHide:true,stdio:['ignore','pipe','pipe']})).digest('hex'),'75d60408556517b8cf32f57b34baa18a3c557c9fb01cae407f47964db9079f6f');
 const evidence=path.join(root,'QA/EVIDENCE/penta-teacher-start-qualified-'+crypto.randomUUID().replaceAll('-',''));fs.mkdirSync(evidence);
 const guards=cp.spawnSync(process.execPath,['--test','--test-reporter=tap','QA/tools/ci-release-artifacts.test.cjs','QA/tools/ci-workflow.test.cjs'],{cwd:root,windowsHide:true,encoding:'utf8',timeout:30000});a.equal(guards.status,0);a.match(guards.stdout,/# tests 35/);a.match(guards.stdout,/# fail 0/);fs.writeFileSync(path.join(evidence,'ci-guards.tap'),guards.stdout+guards.stderr);
 const files=[path.join(startFolder,'result.json'),...['result.json','api.trx','sql.log'].map(p=>path.join(folder,p)),path.join(root,'QA/EVIDENCE/penta-teacher-conversation-sql-'+sql+'/result.json'),...['result.json','contract-samples.json','shared-contract-check.json'].map(p=>path.join(coreFolder,p)),path.join(evidence,'ci-guards.tap'),...(frontendFolder?['result.json','start-transport.json','frontend-types.log','frontend-lint.log'].map(p=>path.join(frontendFolder,p)):[])];
 const result={passed:true,run,sqlRun:sql,tests,startupSqlChecks:start.checks,conversationSqlChecks:chat.checks,coreSqlChecks:21,workflowSqlChecks:30,serviceSqlChecks:15,availabilityCases:45,sharedContractSdkChecks:11,ciGuardChecks:35,
  apiSourcePins:parent.sourcePins,validationPins:parent.validationPins,sourceUnchanged:true,mainUnchanged:true,servicesRestored:true,inventoryPreserved:true,ownedSqlCleanup:true,leaseReleased:true,
  modelLoaded:false,modelInference:false,toolRegistered:false,productionApproved:false,syntheticInterpreter:true,startSessionsCreated:1,createdTeachers:1,createdAccounts:0,executionAudits:1,
  frontendRun:frontend?.run||null,frontendSourcePins:frontend?.sourcePins||[],frontendStartupTransportQualified:!!frontend,startClientChecks:frontend?61:0,retainedOperationClientChecks:frontend?52:0,
  frontendConversationQualified:false,qualifiedModelConnection:false,qualifierSha256:hash(__filename),evidencePins:files.map(p=>({path:path.relative(root,p).replaceAll('\\','/'),sha256:hash(p)}))};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({passed:true,evidence:path.relative(root,evidence),tests,startupSqlChecks:start.checks,conversationSqlChecks:chat.checks,sharedContractSdkChecks:11,ciGuardChecks:35,frontendStartupTransportQualified:!!frontend,startClientChecks:frontend?61:0,productionApproved:false}));
}
main().catch(e=>{console.error('Teacher startup qualifier refused: '+String(e.message).slice(0,180));process.exitCode=1;});
