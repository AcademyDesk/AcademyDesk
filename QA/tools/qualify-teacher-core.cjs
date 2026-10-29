'use strict';
// Read-only reconciliation of actual SQL/HTTP receipts, actual shared SDK checks,
// current source and live restoration. No model load/activation or release grant.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const exec=(binary,args,cwd=root)=>cp.execFileSync(binary,args,{cwd,windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();
async function main(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.equal(process.argv.length,3);const run=process.argv[2];a.match(run||'',/^[a-f0-9]{32}$/);
 const folder=path.join(root,'QA/EVIDENCE/teacher-creation-window-'+run),parent=JSON.parse(fs.readFileSync(path.join(folder,'result.json')));
 for(const key of['passed','validationComplete','sourceUnchanged','restorationPassed','inventoryPreserved'])a.equal(parent[key],true,key);
 a.equal(parent.modelLoaded,false);a.equal(parent.downloaded,false);a.equal(parent.productionApproved,false);a.ok(parent.steps.every(x=>x.exitCode===0&&!x.timedOut));
 a.deepEqual(require('./api-image-build.cjs').inputs(),parent.sourcePins);for(const pin of parent.validationPins)a.equal(hash(path.join(root,pin.path)),pin.sha256,'Changed: '+pin.path);
 const tests=require('./ci-release-artifacts.cjs').counters(fs.readFileSync(path.join(folder,'api.trx'),'utf8'));a.deepEqual(tests,{total:3065,passed:3065,skipped:0});
 const sql=parent.sqlRun;a.match(sql,/^[a-f0-9]{32}$/);const coreFolder=path.join(root,'QA/EVIDENCE/penta-teacher-core-sql-'+sql);
 const core=JSON.parse(fs.readFileSync(path.join(coreFolder,'result.json'))),shared=JSON.parse(fs.readFileSync(path.join(coreFolder,'shared-contract-check.json')));
 a.deepEqual(core,parent.core);a.equal(core.checks,21);a.equal(core.createdTeachers,1);a.equal(core.executionAudits,1);a.deepEqual(core.statuses,[200,201]);a.equal(core.modelInference,false);a.equal(core.toolRegistered,false);
 a.equal(shared.passed,true);a.equal(shared.checks,11);a.equal(shared.modelLoaded,false);a.equal(shared.inference,false);a.equal(shared.liveHostMutation,false);a.equal(shared.miniSourceChanged,false);
 a.equal(hash(path.join(coreFolder,'contract-samples.json')),shared.sampleSha256);for(const pin of shared.sharedSourcePins)a.equal(hash(pin.path),pin.sha256);
 a.equal(parent.workflow.checks,30);a.deepEqual(parent.workflow.simultaneousStatuses,[200,201]);a.deepEqual(parent.workflow.duplicateStatuses,[201,409]);a.equal(parent.native.checks,15);
 const log=fs.readFileSync(path.join(folder,'sql.log'),'utf8');a.match(log,/AVAILABILITY all 45 SQL\/Identity\/HTTP cases/);a.match(log,/run-owned database and login removed/);a.equal(exec('docker',['ps','-aq','--filter','label=academydesk.qa.run='+sql]),'');
 for(const row of parent.restored){const actual=JSON.parse(exec('docker',['inspect',row.id]))[0];a.equal(actual.Id,row.id);a.equal(actual.Image,row.image);a.equal(actual.State.Running,true);if(actual.State.Health)a.equal(actual.State.Health.Status,'healthy');}
 a.equal((await fetch('http://127.0.0.1:8000/health',{signal:AbortSignal.timeout(10000)})).status,200);a.ok(!fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')));
 const inventory=Object.fromEntries([['containers',['ps','-aq','--no-trunc']],['volumes',['volume','ls','-q']],['networks',['network','ls','-q','--no-trunc']],['images',['image','ls','-aq','--no-trunc']]].map(([k,args])=>[k,[...new Set(exec('docker',args).split(/\r?\n/).filter(Boolean))].sort()]));a.deepEqual(inventory,parent.before);
 a.equal(exec('git',['branch','--show-current']),'codex/penta-search');a.equal(exec('git',['rev-parse','HEAD']),'b1f8cb6f4209da7fbd14f8089fcb6792438684c4');
 const mainRoot='D:\\AcademyDesk';a.equal(exec('git',['rev-parse','HEAD'],mainRoot),'f7af512f884ca5fe8ac3ddb287c852492592e709');
 a.equal(crypto.createHash('sha256').update(cp.execFileSync('git',['status','--porcelain'],{cwd:mainRoot,windowsHide:true,stdio:['ignore','pipe','pipe']})).digest('hex'),'75d60408556517b8cf32f57b34baa18a3c557c9fb01cae407f47964db9079f6f');
 const evidence=path.join(root,'QA/EVIDENCE/penta-teacher-core-qualified-'+crypto.randomUUID().replaceAll('-',''));fs.mkdirSync(evidence);
 const guards=cp.spawnSync(process.execPath,['--test','--test-reporter=tap','QA/tools/ci-release-artifacts.test.cjs','QA/tools/ci-workflow.test.cjs'],{cwd:root,windowsHide:true,encoding:'utf8',timeout:30000});a.equal(guards.status,0);a.match(guards.stdout,/# tests 33/);a.match(guards.stdout,/# fail 0/);fs.writeFileSync(path.join(evidence,'ci-guards.tap'),guards.stdout+guards.stderr);
 const files=[...['result.json','api.trx','sql.log'].map(p=>path.join(folder,p)),...['result.json','contract-samples.json','shared-contract-check.json'].map(p=>path.join(coreFolder,p)),path.join(evidence,'ci-guards.tap')];
 const result={passed:true,run,sqlRun:sql,tests,coreSqlChecks:21,workflowSqlChecks:30,serviceSqlChecks:15,availabilityCases:45,sharedContractSdkChecks:11,ciGuardChecks:33,apiSourcePins:parent.sourcePins,validationPins:parent.validationPins,
  sourceUnchanged:true,mainUnchanged:true,servicesRestored:true,inventoryPreserved:true,ownedSqlCleanup:true,leaseReleased:true,modelLoaded:false,modelInference:false,toolRegistered:false,productionApproved:false,
  qualifierSha256:hash(__filename),evidencePins:files.map(p=>({path:path.relative(root,p).replaceAll('\\','/'),sha256:hash(p)}))};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({passed:true,evidence:path.relative(root,evidence),tests,coreSqlChecks:21,sharedContractSdkChecks:11,ciGuardChecks:33,productionApproved:false}));
}
main().catch(e=>{console.error('Teacher Core qualifier refused: '+String(e.message).slice(0,180));process.exitCode=1;});
