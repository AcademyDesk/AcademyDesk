'use strict';
// Current export linked to the unchanged native Access Review authority/audit cases.
const fs=require('node:fs'),path=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),read=p=>fs.readFileSync(path.join(root,'QA/EVIDENCE',p)),json=p=>JSON.parse(read(p)),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function run(){
 a.equal(process.argv.length,4);const[sqlId,windowId]=process.argv.slice(2);for(const id of[sqlId,windowId])a.match(id,/^[0-9a-f]{32}$/);
 const ui=require('./compact-theme-qualification.cjs').qualify(),apiId='52c9676e11d24cf9863a123d178021a1',api=json('api-image-'+apiId+'/result.json');
 a.equal(api.passed,true);a.equal(api.standardSourceDockerfileValidated,false);a.equal(api.networkDownloads,false);a.deepEqual(require('./api-image-build.cjs').inputs(),api.sourceFiles);
 const build=json('admission-build-window-d9858a86005d46b5a66947b34ed73ee3/result.json');
 require('./admission-repair-qualification.cjs').qualifyLocalWindow(build,api.imageId);a.equal(build.build,apiId);
 const attributes=/<Counters\s+([^>]+)\/>/.exec(read('admission-build-window-d9858a86005d46b5a66947b34ed73ee3/admission.trx').toString())?.[1];a.ok(attributes);
 const counters=Object.fromEntries([...attributes.matchAll(/([A-Za-z]+)="(\d+)"/g)].map(m=>[m[1],Number(m[2])]));
 for(const k of['total','executed','passed'])a.equal(counters[k],2982);for(const k of['failed','error','timeout','aborted','notExecuted'])a.equal(counters[k],0);
 const payload=json('api-image-artifacts-30b5219ebdd54f11b9b2b4c33f96ea14/result.json');
 for(const k of['passed','sourceUnchanged','ownedContainerRemoved'])a.equal(payload[k],true);a.equal(payload.appStarted,false);a.equal(payload.build,apiId);a.equal(payload.imageId,api.imageId);a.equal(payload.files,65);a.equal(payload.bytes,api.publishedBytes);
 a.deepEqual([...payload.manifest].sort((x,y)=>x.path.localeCompare(y.path)),[...api.manifest].sort((x,y)=>x.path.localeCompare(y.path)));
 const sql=json('api-image-sql-'+sqlId+'/result.json'),w=json('api-runtime-window-'+windowId+'/result.json');
 a.equal(sql.run,sqlId);a.equal(w.run,windowId);a.equal(sql.mode,'native-access-review-browser');a.equal(sql.build,apiId);a.equal(sql.imageId,api.imageId);
 for(const k of['passed','sourceUnchanged','ownedResourcesRemoved','inventoryPreserved'])a.equal(sql[k],true);a.deepEqual(sql.cleanupErrors,[]);
 for(const k of['productionApproved','externalInference','credentialsRetained'])a.equal(sql[k],false);
 a.equal(sql.checks.length,53);a.ok(sql.checks.every(c=>c.status!==429&&c.status<500));
 a.deepEqual(sql.checks.filter(c=>c.label==='Access Review same Staff bearer enrollment read').map(c=>c.status),[403,403,200,200,403,403,200,403,403,200,403,403,200,403]);
 a.deepEqual(sql.syntheticCounts,{academies:2,identities:6,payments:0});a.equal(sql.expectedMigrations.AcademyDeskDbContext.length,102);a.equal(sql.expectedMigrations.IdentityDbContext.length,7);a.deepEqual(sql.appliedMigrations,Object.values(sql.expectedMigrations).flat().sort());
 a.equal(sql.admissionReproduction.passed,true);a.equal(sql.admissionReproduction.observedStatus,409);a.deepEqual(sql.admissionReproduction.before,sql.admissionReproduction.after);
 require('./admission-repair-qualification.cjs').qualifyWindow(w,sql,read('api-runtime-window-'+windowId+'/runtime.log').toString());require('./compact-theme-qualification.cjs').resources(w);
 const f=sql.accessReviewBrowser;require('./api-image-access-review-browser.cjs').proof(f);a.equal(f.exportBuild,ui.webBuild);a.equal(f.verifiedExportFiles,627);
 const writes=f.requests.filter(r=>r.path.includes('/access-grants')&&['POST','PATCH'].includes(r.method));a.equal(writes.length,10);a.equal(writes.filter(r=>r.method==='POST'&&r.status===200).length,4);a.equal(writes.filter(r=>r.method==='PATCH'&&r.status===204).length,4);a.equal(writes.filter(r=>r.status===500&&r.expectedAuditFault).length,2);
 const screenshots=f.cases.map(c=>{const bytes=read('api-image-sql-'+sqlId+'/access-review-'+c.width+'-'+c.theme+'.png');a.equal(bytes.subarray(0,8).toString('hex'),'89504e470d0a1a0a');return{width:c.width,theme:c.theme,sha256:hash(bytes)};});
 console.log(JSON.stringify({passed:true,scope:'Current shared-control export linked to native grant/revoke authority and audit recovery',apiBuild:apiId,webBuild:ui.webBuild,handlerChecks:63,retainedApiTests:2982,fullApiSuiteRepeated:false,exportFiles:627,verifiedImageFiles:65,syntheticKeyboardThemeJourneys:8,nativeHttpChecks:53,actualBrowserJourneys:4,newGrants:4,revokedGrants:4,successAudits:8,auditFaultRollbackControls:2,sameStaffBearer:true,sqlSha256:hash(read('api-image-sql-'+sqlId+'/result.json')),windowSha256:hash(read('api-runtime-window-'+windowId+'/result.json')),screenshots,physicalDevicesTested:false,standardSourceDockerfileValidated:false,enterpriseComplete:false,productionApproved:false},null,2));
}
if(require.main===module)try{run();}catch(error){console.error(JSON.stringify({passed:false,scope:'Current shared-control native proof incomplete or changed',type:error.name,sourceLines:[...String(error.stack||'').matchAll(/(?:shared-control-native-qualification|compact-theme-qualification|admission-repair-qualification)\.cjs:(\d+):(\d+)/g)].map(m=>({line:Number(m[1]),column:Number(m[2])}))}));process.exitCode=1;}
