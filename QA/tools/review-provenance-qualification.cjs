'use strict';
const fs=require('node:fs'),path=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),apiId='3368e36621f942bfac353b8ac92824da',baselineApi='52c9676e11d24cf9863a123d178021a1',buildWindow='c79968927c2a4ab085830a58e7075220',baselineSql='90c6a162794c44b0b100f1569ab612fc',baselineWindow='39a3d8718f764732af258c87af9be85e';
const read=p=>fs.readFileSync(path.join(root,'QA/EVIDENCE',p)),json=p=>JSON.parse(read(p)),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function fixture(sql,guard){
 a.equal(sql.passed,true);a.equal(sql.build,guard?apiId:baselineApi);a.equal(sql.mode,guard?'native-review-provenance-guard':'native-review-provenance-baseline');
 for(const k of['sourceUnchanged','ownedResourcesRemoved','inventoryPreserved'])a.equal(sql[k],true);a.deepEqual(sql.cleanupErrors,[]);
 for(const k of['productionApproved','externalInference','credentialsRetained'])a.equal(sql[k],false);
 a.ok(sql.checks.every(c=>c.status!==429&&c.status<500));a.deepEqual(sql.syntheticCounts,{academies:2,identities:5,payments:0});
 a.equal(sql.expectedMigrations.AcademyDeskDbContext.length,102);a.equal(sql.expectedMigrations.IdentityDbContext.length,7);a.deepEqual(sql.appliedMigrations,Object.values(sql.expectedMigrations).flat().sort());
 a.equal(sql.admissionReproduction.passed,true);a.equal(sql.admissionReproduction.observedStatus,409);a.deepEqual(sql.admissionReproduction.before,sql.admissionReproduction.after);
 const f=sql.reviewProvenance;for(const k of['complete','faultRemoved','refusalsNoWrite'])a.equal(f[k],true);
 a.equal(f.guardExpected,guard);a.equal(f.protectionAccepted,guard);a.equal(f.missingProvenanceReproduced,!guard);a.equal(f.auditRollbackReproduced,!guard);a.equal(f.concurrentSignoffsTested,false);
 a.deepEqual(f.cases.map(c=>c.label),guard?['owner-no-followup','owner-followup','platform-followup','owner-recovery','platform-recovery']:['owner-no-followup','owner-followup','platform-followup']);
 for(const c of f.cases){a.equal(c.status,200);for(const k of['serverActorRecorded','exactTaskCorrelation','spoofIgnored'])a.equal(c[k],guard);a.equal(c.successAudits,guard?2:c.label.startsWith('platform')?0:1);}
 a.deepEqual(f.faults.map(c=>c.label),guard?['owner-generic-audit','platform-generic-audit','owner-domain-audit','followup-insert']:['owner-generic-audit','platform-generic-audit']);
 for(const c of f.faults){a.equal(c.status,guard||!c.label.startsWith('platform')?500:200);a.equal(c.allTablesUnchanged,guard);a.equal(c.noSuccessAudit,true);a.equal(c.unrelatedStateUnchanged,true);}
 a.deepEqual(f.finalCounts,guard?{reviews:5,tasks:3,audits:10}:{reviews:5,tasks:4,audits:2});if(guard)a.equal(f.overlongNoWrite,true);
 for(const[label,status]of[['absent entitlement',403],['foreign signoff denied',403],['teacher signoff denied',403],['student signoff denied',403],['anonymous signoff denied',401]])a.equal(sql.checks.find(c=>c.label==='review provenance '+label)?.status,status);
 return f;
}
function run(){
 a.equal(process.argv.length,4);const[sqlId,windowId]=process.argv.slice(2);for(const id of[sqlId,windowId])a.match(id,/^[0-9a-f]{32}$/);
 const api=json('api-image-'+apiId+'/result.json'),prior=json('api-image-'+baselineApi+'/result.json');
 a.equal(api.passed,true);a.equal(api.sourceUnchanged,true);a.equal(api.standardSourceDockerfileValidated,false);a.equal(api.networkDownloads,false);
 a.deepEqual(require('./api-image-build.cjs').inputs(),api.sourceFiles);const pins=new Map(prior.sourceFiles.map(f=>[f.path,JSON.stringify(f)]));
 a.equal(api.sourceFiles.length,pins.size);a.deepEqual(api.sourceFiles.filter(f=>JSON.stringify(f)!==pins.get(f.path)).map(f=>f.path),['apps/api/Controllers/AccessReviewsController.cs']);
 const bw=json('admission-build-window-'+buildWindow+'/result.json');require('./admission-repair-qualification.cjs').qualifyLocalWindow(bw,api.imageId);a.equal(bw.build,apiId);a.deepEqual(bw.steps.map(s=>s.name),['units','image-build']);
 const attributes=/<Counters\s+([^>]+)\/>/.exec(read('admission-build-window-'+buildWindow+'/admission.trx').toString())?.[1];a.ok(attributes);const counts=Object.fromEntries([...attributes.matchAll(/([A-Za-z]+)="(\d+)"/g)].map(m=>[m[1],Number(m[2])]));
 for(const k of['total','executed','passed'])a.equal(counts[k],2995);for(const k of['failed','error','timeout','aborted','notExecuted'])a.equal(counts[k],0);
 const baseline=json('api-image-sql-'+baselineSql+'/result.json'),oldWindow=json('api-runtime-window-'+baselineWindow+'/result.json');fixture(baseline,false);a.equal(baseline.imageId,prior.imageId);a.equal(baseline.checks.length,46);require('./admission-repair-qualification.cjs').qualifyWindow(oldWindow,baseline,read('api-runtime-window-'+baselineWindow+'/runtime.log').toString());
 const sql=json('api-image-sql-'+sqlId+'/result.json'),w=json('api-runtime-window-'+windowId+'/result.json');a.equal(sql.run,sqlId);a.equal(w.run,windowId);const f=fixture(sql,true);a.equal(sql.imageId,api.imageId);
 require('./admission-repair-qualification.cjs').qualifyWindow(w,sql,read('api-runtime-window-'+windowId+'/runtime.log').toString());
 a.equal(w.artifactsBeforeSql,true);a.match(w.artifactsRun,/^[0-9a-f]{32}$/);const payload=json('api-image-artifacts-'+w.artifactsRun+'/result.json');
 for(const k of['passed','sourceUnchanged','ownedContainerRemoved'])a.equal(payload[k],true);a.equal(payload.appStarted,false);a.equal(payload.build,apiId);a.equal(payload.imageId,api.imageId);a.equal(payload.files,65);a.equal(payload.bytes,api.publishedBytes);a.deepEqual([...payload.manifest].sort((x,y)=>x.path.localeCompare(y.path)),[...api.manifest].sort((x,y)=>x.path.localeCompare(y.path)));
 a.equal(fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')),false);
 console.log(JSON.stringify({passed:true,scope:'Access-review server provenance and domain/generic audit atomicity',apiBuild:apiId,imageId:api.imageId,sourceFiles:api.sourceFiles.length,publishedImageFiles:65,apiTests:2995,newControllerCases:13,nativeHttpChecks:sql.checks.length,baselineHttpChecks:46,normalAndRecoverySignoffs:f.cases.length,faultRollbackControls:f.faults.length,newReviewRows:5,linkedTasks:3,successAudits:10,spoofedBodyIdentityIgnored:true,tenantRoleModuleDenialsPreserved:true,sqlSha256:hash(read('api-image-sql-'+sqlId+'/result.json')),windowSha256:hash(read('api-runtime-window-'+windowId+'/result.json')),baselineSha256:hash(read('api-image-sql-'+baselineSql+'/result.json')),concurrentSignoffsTested:false,liveSignoffBrowserTested:false,physicalDevicesTested:false,standardSourceDockerfileValidated:false,enterpriseComplete:false,productionApproved:false},null,2));
}
if(require.main===module)try{run();}catch(error){console.error(JSON.stringify({passed:false,scope:'Review provenance proof incomplete or changed',type:error.name,sourceLines:[...String(error.stack||'').matchAll(/(?:review-provenance-qualification|admission-repair-qualification)\.cjs:(\d+):(\d+)/g)].map(m=>({line:Number(m[1]),column:Number(m[2])}))}));process.exitCode=1;}
