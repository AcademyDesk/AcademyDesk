'use strict';
// Reconciles real receipts without rerunning the enterprise suite or granting release authority.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const exec=(binary,args,cwd=root)=>cp.execFileSync(binary,args,{cwd,windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();
async function main(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.equal(process.argv.length,3);const run=process.argv[2];a.match(run||'',/^[a-f0-9]{32}$/);
 const folder=path.join(root,'QA/EVIDENCE/enrollment-shared-window-'+run),parent=JSON.parse(fs.readFileSync(path.join(folder,'result.json')));
 for(const key of['passed','validationComplete','sourceUnchanged','restorationPassed','inventoryPreserved'])a.equal(parent[key],true,key);
 a.equal(parent.modelLoaded,false);a.equal(parent.downloaded,false);a.equal(parent.productionApproved,false);a.ok(parent.steps.every(x=>x.exitCode===0&&!x.timedOut));
 a.deepEqual(require('./api-image-build.cjs').inputs(),parent.sourcePins);for(const pin of parent.validationPins)a.equal(hash(path.join(root,pin.path)),pin.sha256,'Changed validation source: '+pin.path);
 const tests=require('./ci-release-artifacts.cjs').counters(fs.readFileSync(path.join(folder,'api.trx'),'utf8'));a.deepEqual(tests,{total:3183,passed:3183,skipped:0});
 let reusedFiles=[];
 if(parent.reusedApiFrom){
  a.match(parent.reusedApiFrom.run,/^[a-f0-9]{32}$/);const priorFolder=path.join(root,'QA/EVIDENCE/enrollment-shared-window-'+parent.reusedApiFrom.run),priorFile=path.join(priorFolder,'result.json'),prior=JSON.parse(fs.readFileSync(priorFile)),trx=path.join(priorFolder,'api.trx');
  a.ok(prior.steps.some(s=>s.name==='api-tests'&&s.exitCode===0&&!s.timedOut));a.deepEqual(prior.sourcePins,parent.sourcePins);
  for(const p of prior.validationPins.filter(p=>p.path.startsWith('tests/')))a.equal(hash(path.join(root,p.path)),p.sha256);
  a.equal(hash(trx),parent.reusedApiFrom.trxSha256);a.equal(hash(path.join(folder,'api.trx')),hash(trx));reusedFiles=[priorFile,trx];
 }
 const sql=parent.sqlRun;a.match(sql,/^[a-f0-9]{32}$/);
 const nativeFile=path.join(root,'QA/EVIDENCE/enrollment-shared-sql-'+sql+'/result.json'),native=JSON.parse(fs.readFileSync(nativeFile));
 a.deepEqual(native,parent.native);a.equal(native.passed,true);a.equal(native.checks,57);a.equal(native.capacityRaces,5);a.equal(native.duplicateRaces,5);a.equal(native.transferRaces,5);
 a.equal(native.sqlBacked,true);a.equal(native.modelInference,false);a.equal(native.toolRegistered,false);a.equal(native.productionApproved,false);a.ok(native.raceStatuses.every(x=>JSON.stringify(x)==='[201,409]'));
 a.equal(parent.retainedLifecycleChecks,28);const log=fs.readFileSync(path.join(folder,'sql.log'),'utf8');a.match(log,/ENROLLMENTLIFE all 28 native SQL\/HTTP cases/);a.match(log,/run-owned database and login removed/);
 a.equal(exec('docker',['ps','-aq','--filter','label=academydesk.qa.run='+sql]),'');
 for(const row of parent.restored){const actual=JSON.parse(exec('docker',['inspect',row.id]))[0];a.equal(actual.Id,row.id);a.equal(actual.Image,row.image);a.equal(actual.State.Running,true);if(actual.State.Health)a.equal(actual.State.Health.Status,'healthy');}
 a.equal((await fetch('http://127.0.0.1:8000/health',{signal:AbortSignal.timeout(10000)})).status,200);a.ok(!fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')));
 const inventory=Object.fromEntries([['containers',['ps','-aq','--no-trunc']],['volumes',['volume','ls','-q']],['networks',['network','ls','-q','--no-trunc']],['images',['image','ls','-aq','--no-trunc']]].map(([k,args])=>[k,[...new Set(exec('docker',args).split(/\r?\n/).filter(Boolean))].sort()]));a.deepEqual(inventory,parent.before);
 a.equal(exec('git',['branch','--show-current']),'codex/penta-search');a.equal(exec('git',['rev-parse','HEAD']),'b1f8cb6f4209da7fbd14f8089fcb6792438684c4');
 const mainRoot='D:\\AcademyDesk';a.equal(exec('git',['rev-parse','HEAD'],mainRoot),'f7af512f884ca5fe8ac3ddb287c852492592e709');
 a.equal(crypto.createHash('sha256').update(cp.execFileSync('git',['status','--porcelain'],{cwd:mainRoot,windowsHide:true,stdio:['ignore','pipe','pipe']})).digest('hex'),'75d60408556517b8cf32f57b34baa18a3c557c9fb01cae407f47964db9079f6f');
 const evidence=path.join(root,'QA/EVIDENCE/enrollment-shared-qualified-'+crypto.randomUUID().replaceAll('-',''));fs.mkdirSync(evidence);
 const guards=cp.spawnSync(process.execPath,['--test','--test-reporter=tap','QA/tools/ci-release-artifacts.test.cjs','QA/tools/ci-workflow.test.cjs'],{cwd:root,windowsHide:true,encoding:'utf8',timeout:30000});a.equal(guards.status,0);a.match(guards.stdout,/# tests 36/);a.match(guards.stdout,/# fail 0/);fs.writeFileSync(path.join(evidence,'ci-guards.tap'),guards.stdout+guards.stderr);
 const files=[...['result.json','api.trx','sql.log'].map(p=>path.join(folder,p)),nativeFile,path.join(evidence,'ci-guards.tap'),...reusedFiles];
 const result={passed:true,run,sqlRun:sql,tests,newSharedSqlChecks:57,retainedLifecycleChecks:28,capacityRaces:5,duplicateRaces:5,transferRaces:5,ciGuardChecks:36,
  apiSourcePins:parent.sourcePins,validationPins:parent.validationPins,reusedApiFrom:parent.reusedApiFrom??null,sourceUnchanged:true,mainUnchanged:true,servicesRestored:true,inventoryPreserved:true,ownedSqlCleanup:true,leaseReleased:true,
  modelLoaded:false,modelInference:false,toolRegistered:false,productionApproved:false,conversationQualified:false,frontendQualified:false,
  qualifierSha256:hash(__filename),evidencePins:files.map(p=>({path:path.relative(root,p).replaceAll('\\','/'),sha256:hash(p)}))};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({passed:true,evidence:path.relative(root,evidence),tests,newSharedSqlChecks:57,retainedLifecycleChecks:28,mixedRaces:15,ciGuardChecks:36,productionApproved:false}));
}
main().catch(e=>{console.error('Enrollment shared-service qualifier refused: '+String(e.message).slice(0,180));process.exitCode=1;});
