'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{qualify,records,build,image}=require('./api-image-qualification.cjs');
function transitionFixture(){
 const originalRegressionSha256={
  'CollectionRaceRegression.cs':'e64150799a7292d49d015e08d0ec722d9b16c7ec5cfefea27d9297d82df2c85e',
  'Run-ReconciledPayment.ps1':'dcba02fcf46ebb26f4f2ae57204e972b3deba973080f7abfdca93e3c51655106',
  'ApprovalRaceRegression.cs':'6c053a6067fbdd6799e9a018cca9e55f2913c7214211f5aa32c5f37a72727026',
  'PaymentVoidRaceRegression.cs':'0c3dd4f6fde5420f9569f2a7e6ba69ddc9912a04b228d4f9b36e032101d495cb'
 };
 const names=require('./api-image-transitions.cjs').groups,p={passed:true,originalHarnessUnchanged:true,rateLimiterModified:false,policy:'Existing FP1/FP3 only',originalRegressionSha256,expectedGroups:names,groups:names.map(name=>({name,races:Array.from({length:5},(_,index)=>{
  const attempt=index+1,adjusted=name==='adjusted-collection'?200:name==='approval-payment'?0:attempt%2===0?200:0;
  const [statuses,rows,collected,status]=name==='adjusted-collection'?[[201,400],2,800,'Paid']:name==='approval-payment'?[[400,201],2,1000,'Paid']:name==='two-voids'?[[200,200],2,0,'Overdue']:name==='create-void'?[[200,201],2,400-adjusted,'PartiallyPaid']:[[400,200],1,0,'Overdue'];
  return{attempt,statuses,rows,collected,adjusted,status,balance:1000-adjusted-collected,guarded:true,bothReadyBeforeRelease:true};
 })}))};p.totalPaymentRows=p.groups.flatMap(g=>g.races).reduce((sum,row)=>sum+row.rows,0);return p;
}
function entryFixture(){
 const identities=[['Platform','/platform/ai','/api/platform/overview'],['AcademyAdmin','/penta','/api/academies'],['Teacher','/teacher/ai','/api/teacher/me'],['Portal','/portal/ai','/api/portal/me']];
 return{passed:true,phase:'complete',exportBuild:'bad444b6c61e45f680ed62f76d35dcf4',verifiedExportFiles:628,apiResponseMocks:false,tokenInjection:false,modelActivated:false,physicalDevicesTested:false,domainStateUnchanged:true,domainTableHashes:Object.fromEntries(['Students','Teachers','Invoices','Payments','FinanceAdjustments','PayrollProfiles','PayrollPayouts','AuditLogs'].map(table=>[table,'A'.repeat(64)])),cases:identities.flatMap(([role,aiLanding])=>[390,1440].flatMap(width=>['light','dark'].map(theme=>({role,width,theme,aiTheme:theme,manualTheme:theme,journeyRequestCount:3,aiLanding,conversationUnavailableTruthful:true,manualUsable:true,realCoreRead:true,noHorizontalOverflow:true,pageErrors:0})))),authenticatedRoles:identities.map(([role,,core])=>({role,requests:[{method:'POST',path:'/api/auth/login',status:200},{method:'GET',path:'/api/auth/session',status:200},{method:'GET',path:core,status:200}]}))};
}
// Portable synthetic fixtures: unit tests never require private, untracked evidence.
function fixture(record=records[0]){
 const expectedMigrations={AcademyDeskDbContext:Array.from({length:102},(_,i)=>'app'+i),IdentityDbContext:Array.from({length:7},(_,i)=>'identity'+i)};
 const sql={run:record.run,build,imageId:image,mode:record.scope,passed:true,sourceUnchanged:true,productionApproved:false,credentialsRetained:false,externalInference:false,ownedResourcesRemoved:true,inventoryPreserved:true,cleanupErrors:[],checks:Array.from({length:record.checks},()=>({status:200})),expectedMigrations,appliedMigrations:Object.values(expectedMigrations).flat().sort()};
 const services=['pentaaimodels-api-1','pentaaimodels-inference-1'].map((name,i)=>({name,id:'synthetic-container-'+i,image:'synthetic-image-'+i}));
 const before={containers:services.map(row=>row.id),volumes:['synthetic-original-volume'],networks:['synthetic-network'],images:[image]};
 const window={run:record.window,passed:true,runtimeExit:0,runtimePassed:true,restorationPassed:true,inventoryPreserved:true,restartInvocations:0,productionApproved:false,admissionFreeBytes:4*1024**3,before,after:structuredClone(before),restorationErrors:[],services,restored:services.map(row=>({...row,running:true,health:row.name.includes('inference')?'healthy':null}))};
 if(record.scope==='finance')sql.finance={sequential:{invoice:1000,reconciled:600,balance:400,excess500Status:400,rows:1},paymentSuccessAudits:11,races:Array.from({length:5},()=>({statuses:[201,400],bothReadyBeforeRelease:true,rejectionMessage:true,collected:1000,rows:2,reconciled:1,completed:1,status:'Paid',adjusted:0}))};
 if(record.scope==='reconstructed-baseline-upgrade')sql.upgrade={baselineApplication:82,baselineIdentity:7,pendingApplication:20,pendingIdentity:0,rowsPreserved:4,baselineDataSha256:'A'.repeat(64),afterDataSha256:'A'.repeat(64),reconstructedSchema:true,originalReleaseBinaryTested:false};
 if(['disposable-backup-restore','combined-database-encrypted-key-recovery'].includes(record.scope)){const tables=Object.fromEntries(['Academies','Students','Invoices','Payments','AspNetUsers','AspNetUserRoles','__EFMigrationsHistory','AuditLogs'].map(name=>[name,{RowCount:1,Digest:'B'.repeat(64)}]));sql.recovery={passed:true,phase:'complete',syntheticOnly:true,backupExported:false,productionBackupTested:false,keyringRecoveryTested:false,migrationHistories:109,tablesBefore:tables,tablesAfter:structuredClone(tables)};}
 if(['encrypted-key-replacement','combined-database-encrypted-key-recovery','encrypted-key-startup-faults','encrypted-key-certificate-rotation'].includes(record.scope)){const keys=[{name:'key-aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa.xml',sha256:'c'.repeat(64)}];sql.keyRecovery={passed:true,phase:'complete',encryptedBefore:keys,encryptedAfter:structuredClone(keys),syntheticOnly:true,productionStorageTested:false,rotationTested:false,sharedReplicaConcurrencyTested:false,separateSyntheticKeyBackupTested:!!record.keyBackup};sql.syntheticKeyMaterialRemoved=true;}
 if(record.scope==='combined-database-encrypted-key-recovery')sql.jointSyntheticDatabaseAndKeyRecovery=true;
 if(record.scope==='encrypted-key-startup-faults')sql.keyConfiguration={passed:true,syntheticOnly:true,cases:require('./api-image-key-config.cjs').cases.map(row=>({name:row.name,exited:true,exitCode:134,notOom:true,safeConfigurationFailure:true}))};
 if(record.scope==='encrypted-key-certificate-rotation'){sql.keyRotation={passed:true,syntheticOnly:true,productionRotationApproved:false,oldKeyId:'00000001-0000-0000-0000-000000000000',newKeyId:'00000002-0000-0000-0000-000000000000'};sql.keyMaintenanceBuild='a'.repeat(32);}
 if(record.scope==='approved-financial-policies')sql.policies={passed:true,noWriteGuards:15,approvedProductRules:['FP1','FP2','FP3'],accountingTaxPayrollReviewComplete:false,reductionWithoutSettlement:true,noCashDueModels:2,voidedTerminal:true,statements:['Monthly','SessionBlock'].map(model=>({model,checks:Object.fromEntries(['status','method','paidAtNull','referenceNull','gross','deductions','net','workerId','recordedIdentity','exactNet','settlement'].map(key=>[key,true]))}))};
 if(record.scope==='native-payment-browser')sql.paymentBrowser={passed:true,phase:'complete',exportBuild:'bad444b6c61e45f680ed62f76d35dcf4',verifiedExportFiles:628,apiResponseMocks:false,tokenInjection:false,physicalDevicesTested:false,cases:[[390,'light',201],[390,'dark',400],[1440,'light',201],[1440,'dark',400]].map(([width,theme,submittedStatus])=>({width,theme,submittedStatus,sql:{rows:2,collected:1000,status:'Paid',balance:0},visiblePickerCollapsed:true,noHorizontalOverflow:true,pageErrors:0,requests:[{path:'/api/auth/login',method:'POST',status:200},{path:'/api/auth/session',method:'GET',status:200},{path:'/api/academies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/payments',method:'POST',status:submittedStatus}]}))};
 if(record.scope==='native-audit-faults'){const names=['payment-create','adjustment-approval','payment-void','payroll-payout','onboarding'];sql.auditFaults={passed:true,syntheticOnly:true,faultEnabled:true,faultRemoved:true,applicationRulesChanged:false,protectedTableCount:12,allFaultsNoWrite:true,recoveredStudentIdentityLinkVerified:true,cases:names.map(name=>({name,status:500,domainIdentityAuditUnchanged:true})),recovery:names.map((name,i)=>({name,status:[201,200,200,201,200][i],successAudits:1}))};sql.syntheticCounts={academies:2,identities:6,payments:2};}
 if(record.scope==='native-finance-transitions'){sql.transitions=transitionFixture();sql.syntheticCounts={academies:2,identities:5,payments:sql.transitions.totalPaymentRows};}
 if(record.scope==='native-role-entry-browser'){sql.entryBrowser=entryFixture();sql.syntheticCounts={academies:2,identities:5,payments:0};}
 return{record,sql,window,log:JSON.stringify({run:record.run,passed:true,ownedResourcesRemoved:true,inventoryPreserved:true})};
}
function check(f){return qualify(f.record,f.sql,f.window,f.log);}
test('all pinned current-image receipts require matching successful cleanup and restoration evidence',()=>{for(const record of records)a.equal(check(fixture(record)).receiptAccepted,true);});
test('wrong image, failed run, missing SQL histories, leaking values and mismatched window cannot qualify',()=>{
 const mutations=[f=>f.sql.passed=false,f=>f.sql.imageId='sha256:'+'0'.repeat(64),f=>f.sql.sourceUnchanged=false,f=>f.sql.credentialsRetained=true,f=>f.sql.appliedMigrations.pop(),f=>f.sql.checks.push({status:429}),f=>f.window.runtimePassed=false,f=>f.window.restorationPassed=false,f=>f.window.admissionFreeBytes=2*1024**3,f=>f.window.restored[0].id='replacement-container',f=>f.window.after.volumes.pop(),f=>f.log='{"run":"other","passed":true}'];
 for(const mutate of mutations){const f=fixture();mutate(f);a.throws(()=>check(f));}
});
test('narrow finance, upgrade, backup and encrypted-key evidence cannot be silently widened',()=>{
 for(const [scope,mutate] of[['finance',f=>f.sql.finance.races[0].collected=1400],['reconstructed-baseline-upgrade',f=>f.sql.upgrade.afterDataSha256='0'.repeat(64)],['disposable-backup-restore',f=>f.sql.recovery.tablesAfter.Payments.RowCount++],['encrypted-key-replacement',f=>f.sql.syntheticKeyMaterialRemoved=false]]){const f=fixture(records.find(row=>row.scope===scope));mutate(f);a.throws(()=>check(f));}
});
test('only the exact pinned legacy role receipt may omit its later-added mode field',()=>{
 const f=fixture();delete f.sql.mode;a.match(check(f).scopeDeclaration,/predates mode field/);f.sql.finance={};a.throws(()=>check(f));
 const later=fixture(records[1]);delete later.sql.mode;a.throws(()=>check(later));
});
test('the additional encrypted-key backup receipt requires its exact backup assertion',()=>{const f=fixture(records.find(row=>row.keyBackup));f.sql.keyRecovery.separateSyntheticKeyBackupTested=false;a.throws(()=>check(f));});
test('combined receipt needs both complete data and key evidence plus the joint execution marker',()=>{
 for(const mutate of[f=>delete f.sql.recovery,f=>f.sql.keyRecovery.passed=false,f=>f.sql.jointSyntheticDatabaseAndKeyRecovery=false]){const f=fixture(records.find(row=>row.scope==='combined-database-encrypted-key-recovery'));mutate(f);a.throws(()=>check(f));}
});
test('configuration receipt rejects silent fallback, OOM or incomplete startup failures',()=>{
 for(const mutate of[f=>f.sql.keyConfiguration.cases[0].exitCode=0,f=>f.sql.keyConfiguration.cases[0].notOom=false,f=>f.sql.keyConfiguration.cases.pop()]){const f=fixture(records.find(row=>row.scope==='encrypted-key-startup-faults'));mutate(f);a.throws(()=>check(f));}
});
test('rotation cannot qualify from cached old-key writes or missing compiler identity',()=>{
 for(const mutate of[f=>f.sql.keyRotation.newKeyId=f.sql.keyRotation.oldKeyId,f=>f.sql.keyMaintenanceBuild=undefined,f=>f.sql.keyRotation.passed=false]){const f=fixture(records.find(row=>row.scope==='encrypted-key-certificate-rotation'));mutate(f);a.throws(()=>check(f));}
});
test('financial policy receipt requires all no-write guards and exact zero-net invariants',()=>{
 for(const mutate of[f=>f.sql.policies.noWriteGuards=14,f=>f.sql.policies.statements[0].checks.paidAtNull=false,f=>f.sql.policies.voidedTerminal=false]){const f=fixture(records.find(row=>row.scope==='approved-financial-policies'));mutate(f);a.throws(()=>check(f));}
});
test('native browser receipt needs all viewports/themes, real login/submits and exact SQL without mocks or rate limits',()=>{
 for(const mutate of[f=>f.sql.paymentBrowser.cases.pop(),f=>f.sql.paymentBrowser.apiResponseMocks=true,f=>f.sql.paymentBrowser.tokenInjection=true,f=>f.sql.paymentBrowser.cases[0].sql.rows=3,f=>f.sql.paymentBrowser.cases[1].submittedStatus=201,f=>f.sql.paymentBrowser.cases[0].requests.pop(),f=>f.sql.paymentBrowser.cases[0].requests.push({method:'GET',path:'/api/other',status:429}),f=>f.sql.paymentBrowser.cases[0].pageErrors=1]){const f=fixture(records.find(row=>row.scope==='native-payment-browser'));mutate(f);a.throws(()=>check(f));}
});
test('required-audit proof needs all five real failures, unchanged domain/Identity and audited linked recovery',()=>{
 for(const mutate of[f=>f.sql.auditFaults.cases.pop(),f=>f.sql.auditFaults.cases[0].status=201,f=>f.sql.auditFaults.cases[0].domainIdentityAuditUnchanged=false,f=>f.sql.auditFaults.faultRemoved=false,f=>f.sql.auditFaults.recovery[1].successAudits=2,f=>f.sql.auditFaults.recoveredStudentIdentityLinkVerified=false,f=>f.sql.syntheticCounts.identities=5]){const f=fixture(records.find(row=>row.scope==='native-audit-faults'));mutate(f);a.throws(()=>check(f));}
});
test('native transition qualification requires all25barrier pairs and exact gross/reduced ledger outcomes',()=>{
 const{qualifyTransitions}=require('./api-image-qualification.cjs');a.equal(qualifyTransitions(transitionFixture()).pairs,25);
 const approvedFirst=transitionFixture();Object.assign(approvedFirst.groups[1].races[0],{statuses:[200,400],rows:1,collected:600,adjusted:200,balance:200,status:'PartiallyPaid'});approvedFirst.totalPaymentRows--;a.equal(qualifyTransitions(approvedFirst).paymentRows,44);
 const reconciledFirst=transitionFixture();reconciledFirst.groups[4].races[0].statuses=[200,200];a.equal(qualifyTransitions(reconciledFirst).pairs,25);
 for(const mutate of[p=>p.groups.pop(),p=>p.groups[0].races.pop(),p=>p.groups[0].races[0].statuses=[201,201],p=>p.groups[1].races[0].statuses=[200,201],p=>p.groups[2].races[0].balance=0,p=>p.groups[3].races[0].bothReadyBeforeRelease=false,p=>p.groups[4].races[0].statuses=[429,200],p=>p.rateLimiterModified=true,p=>p.originalRegressionSha256['ApprovalRaceRegression.cs']='changed',p=>p.totalPaymentRows++]){const p=transitionFixture();mutate(p);a.throws(()=>qualifyTransitions(p));}
});
test('pinned transition receipt requires complete pairs and independently matching persisted row count',()=>{
 for(const mutate of[f=>delete f.sql.transitions,f=>f.sql.transitions.passed=false,f=>f.sql.transitions.groups[3].races.pop(),f=>f.sql.syntheticCounts.payments++]){const f=fixture(records.find(row=>row.scope==='native-finance-transitions'));mutate(f);a.throws(()=>check(f));}
});
test('role-entry qualification requires16distinct journeys, actual themes, private login/read evidence and no domain writes',()=>{
 const{qualifyEntryBrowser}=require('./api-image-qualification.cjs');a.deepEqual(qualifyEntryBrowser(entryFixture()),{journeys:16,roles:4});
 for(const mutate of[p=>p.cases.pop(),p=>p.cases[0].role='Guardian',p=>p.cases[0].manualTheme='dark',p=>p.cases[0].journeyRequestCount=0,p=>p.cases[0].realCoreRead=false,p=>p.authenticatedRoles[0].requests.pop(),p=>p.authenticatedRoles[1].requests[0].status=401,p=>p.authenticatedRoles[2].requests.push({method:'POST',path:'/api/academies/owned/payments',status:201}),p=>p.authenticatedRoles[3].requests.push({method:'GET',path:'/api/other',status:429}),p=>delete p.domainTableHashes.AuditLogs,p=>p.domainStateUnchanged=false,p=>p.modelActivated=true,p=>p.tokenInjection=true]){const p=entryFixture();mutate(p);a.throws(()=>qualifyEntryBrowser(p));}
});
test('pinned role-entry receipt requires full native browser evidence and unchanged no-payment fixture counts',()=>{
 for(const mutate of[f=>delete f.sql.entryBrowser,f=>f.sql.entryBrowser.passed=false,f=>f.sql.entryBrowser.cases.pop(),f=>f.sql.syntheticCounts.payments=1]){const f=fixture(records.find(row=>row.scope==='native-role-entry-browser'));mutate(f);a.throws(()=>check(f));}
});

test('API-only qualification excludes exactly the two frontend-dependent historical records',()=>{
 const{selectRecords}=require('./api-image-qualification.cjs');a.equal(selectRecords(false).length,14);const rows=selectRecords(true);a.equal(rows.length,12);a.equal(rows.some(row=>['native-payment-browser','native-role-entry-browser'].includes(row.scope)),false);a.ok(rows.some(row=>row.scope==='native-finance-transitions'));
});

test('new browser receipts need explicit matching new export identity without weakening real-result assertions',()=>{
 const web={build:'a'.repeat(32),files:627};
 for(const scope of['native-payment-browser','native-role-entry-browser']){
  const f=fixture(records.find(row=>row.scope===scope));f.record={...f.record,web};const p=f.sql.paymentBrowser||f.sql.entryBrowser;
  a.throws(()=>check(f));p.exportBuild=web.build;p.verifiedExportFiles=web.files;a.equal(check(f).receiptAccepted,true);
  if(scope==='native-payment-browser')p.cases[0].sql.collected=1400;else p.cases[0].realCoreRead=false;a.throws(()=>check(f));
 }
});
