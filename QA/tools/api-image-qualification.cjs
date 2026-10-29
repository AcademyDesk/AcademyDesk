'use strict';
// Read-only reconciliation of pinned accepted receipts. Never closes a release gate.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),build='0a5f8d2c708a40a994dd97e9f6d12425',image='sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70';
const records=[
 {scope:'role-tenant',run:'b9bc968afc9a489996b7ea6e609f7ebf',window:'183b84e66ffb466896995345acd737dc',checks:30},
 {scope:'finance',run:'8dc5a0e28710446aa4b921da05c32aba',window:'3d620b14f31549729c198ff46bf7a5fe',checks:56},
 {scope:'reconstructed-baseline-upgrade',run:'95c2d4659dee4fddad2a89b8d24740e5',window:'ea8bcc6c36184470b55bd96740d1f9de',checks:31},
 {scope:'disposable-backup-restore',run:'95548d27905e4b9ca15e1f2667b0a848',window:'d2b9cb39a15c4937ad5b96bd83e66fb3',checks:47},
 {scope:'encrypted-key-replacement',run:'c2216a4cdadc44839f7d4b24d08db5d5',window:'2dbd11707e2a450ba33641a7445eef12',checks:42},
 {scope:'encrypted-key-replacement',run:'dece640e5cb84f759762a37e61470ed3',window:'c40200f150e34fcea13a5b200e87631d',checks:45,keyBackup:true},
 {scope:'combined-database-encrypted-key-recovery',run:'b2b836568715499d961502604c3d31a8',window:'f8dcc41044f0447fa723fc8bcc86515d',checks:63,keyBackup:true},
 {scope:'encrypted-key-startup-faults',run:'c41ff369762049a3902cc70a89c16b29',window:'4b840cd6ccf94ec4b63f9cbad2d4fcb9',checks:51,keyBackup:true},
 {scope:'encrypted-key-certificate-rotation',run:'e00668b29a4f4dca9a355493e1fccd55',window:'51e97721be7545ef8500a1ce32b404ff',checks:56,keyBackup:true},
 {scope:'approved-financial-policies',run:'ccf3598f267840e8a7c6ae2f44df36be',window:'332e68905d7e4e29ac4175b71f931cae',checks:57},
 {scope:'native-payment-browser',run:'294dd8d8dbe34109a54f80e0f59c2d1c',window:'2df977e56b5d467ea0beaaa378439399',checks:45},
 {scope:'native-audit-faults',run:'acc60573df364b189dd871b62936a466',window:'d576b3eac4d74cfb82c2948f042af0b7',checks:42},
 {scope:'native-finance-transitions',run:'a2547d69a5df45839648e46df13ecdfb',window:'07872588c8b94d6b8f85c2497b8f6574',checks:158},
 {scope:'native-role-entry-browser',run:'c731f49de72a413e9d483a21357b5416',window:'07b5b7696e1f4ed0baed5d7ad4704eb0',checks:32}
];
function qualifyTransitions(p){
 const groups=require('./api-image-transitions.cjs').groups;
 a.equal(p.passed,true);a.equal(p.originalHarnessUnchanged,true);a.equal(p.rateLimiterModified,false);a.equal(p.policy,'Existing FP1/FP3 only');a.deepEqual(p.expectedGroups,groups);a.deepEqual(p.groups.map(row=>row.name),groups);
 a.deepEqual(p.originalRegressionSha256,{
  'CollectionRaceRegression.cs':'e64150799a7292d49d015e08d0ec722d9b16c7ec5cfefea27d9297d82df2c85e',
  'Run-ReconciledPayment.ps1':'dcba02fcf46ebb26f4f2ae57204e972b3deba973080f7abfdca93e3c51655106',
  'ApprovalRaceRegression.cs':'6c053a6067fbdd6799e9a018cca9e55f2913c7214211f5aa32c5f37a72727026',
  'PaymentVoidRaceRegression.cs':'0c3dd4f6fde5420f9569f2a7e6ba69ddc9912a04b228d4f9b36e032101d495cb'
 });
 let paymentRows=0;
 for(const group of p.groups){
  a.deepEqual(group.races.map(row=>row.attempt),[1,2,3,4,5]);
  for(const row of group.races){
   a.equal(row.guarded,true);a.equal(row.bothReadyBeforeRelease,true);a.ok(row.statuses.every(status=>[200,201,400].includes(status)));a.equal(row.collected+row.adjusted+row.balance,1000);a.ok(row.balance>=0);paymentRows+=row.rows;
   if(group.name==='adjusted-collection'){a.deepEqual([...row.statuses].sort((x,y)=>x-y),[201,400]);a.equal(row.rows,2);a.equal(row.collected,800);a.equal(row.adjusted,200);a.equal(row.balance,0);a.equal(row.status,'Paid');}
   else if(group.name==='approval-payment'){
    if(row.statuses[0]===200){a.deepEqual(row.statuses,[200,400]);a.deepEqual([row.rows,row.collected,row.adjusted,row.balance,row.status],[1,600,200,200,'PartiallyPaid']);}
    else{a.deepEqual(row.statuses,[400,201]);a.deepEqual([row.rows,row.collected,row.adjusted,row.balance,row.status],[2,1000,0,0,'Paid']);}
   }else{
    const adjusted=row.attempt%2===0?200:0;a.equal(row.adjusted,adjusted);
    if(group.name==='two-voids'){a.deepEqual(row.statuses,[200,200]);a.deepEqual([row.rows,row.collected,row.balance,row.status],[2,0,1000-adjusted,'Overdue']);}
    else if(group.name==='create-void'){a.deepEqual(row.statuses,[200,201]);a.deepEqual([row.rows,row.collected,row.balance,row.status],[2,400-adjusted,600,'PartiallyPaid']);}
    else{a.ok([200,400].includes(row.statuses[0]));a.equal(row.statuses[1],200);a.deepEqual([row.rows,row.collected,row.balance,row.status],[1,0,1000-adjusted,'Overdue']);}
   }
  }
 }
 a.equal(p.totalPaymentRows,paymentRows);return{pairs:25,paymentRows};
}
function qualifyEntryBrowser(p,web={build:'bad444b6c61e45f680ed62f76d35dcf4',files:628}){
 a.equal(p.passed,true);a.equal(p.phase,'complete');a.equal(p.exportBuild,web.build);a.equal(p.verifiedExportFiles,web.files);
 for(const flag of['apiResponseMocks','tokenInjection','modelActivated','physicalDevicesTested'])a.equal(p[flag],false);
 a.equal(p.domainStateUnchanged,true);a.deepEqual(Object.keys(p.domainTableHashes).sort(),['Students','Teachers','Invoices','Payments','FinanceAdjustments','PayrollProfiles','PayrollPayouts','AuditLogs'].sort());for(const digest of Object.values(p.domainTableHashes))a.match(digest,/^[A-F0-9]{64}$/);
 const identities=[['Platform','/platform/ai','/api/platform/overview'],['AcademyAdmin','/penta','/api/academies'],['Teacher','/teacher/ai','/api/teacher/me'],['Portal','/portal/ai','/api/portal/me']];
 a.deepEqual(p.cases.map(row=>[row.role,row.width,row.theme]),identities.flatMap(([role])=>[390,1440].flatMap(width=>['light','dark'].map(theme=>[role,width,theme]))));
 for(const row of p.cases){a.equal(row.aiLanding,identities.find(([role])=>role===row.role)[1]);a.equal(row.aiTheme,row.theme);a.equal(row.manualTheme,row.theme);a.ok(Number.isInteger(row.journeyRequestCount)&&row.journeyRequestCount>0);for(const flag of['conversationUnavailableTruthful','manualUsable','realCoreRead','noHorizontalOverflow'])a.equal(row[flag],true);a.equal(row.pageErrors,0);}
 a.deepEqual(p.authenticatedRoles.map(row=>row.role),identities.map(([role])=>role));
 for(const [index,identity]of identities.entries()){
  const requests=p.authenticatedRoles[index].requests;
  for(const [method,path]of[['POST','/api/auth/login'],['GET','/api/auth/session'],['GET',identity[2]]])a.ok(requests.some(row=>row.method===method&&row.path===path&&row.status===200));
  a.equal(requests.some(row=>row.status===429||row.status>=500),false);a.equal(requests.some(row=>row.method!=='GET'&&!(row.method==='POST'&&['/api/auth/login','/api/auth/refresh'].includes(row.path))),false);
 }
 return{journeys:16,roles:4};
}
function qualify(record,sql,window,log){
 a.equal(sql.run,record.run);
 const knownLegacyMode=sql.run==='b9bc968afc9a489996b7ea6e609f7ebf'&&record.scope==='role-tenant'&&sql.mode===undefined;
 if(knownLegacyMode)for(const extra of['finance','upgrade','recovery','keyRecovery'])a.equal(sql[extra],undefined);else a.equal(sql.mode,record.scope);
 a.equal(sql.build,build);a.equal(sql.imageId,image);a.equal(sql.passed,true);a.equal(sql.sourceUnchanged,true);a.equal(sql.productionApproved,false);a.equal(sql.credentialsRetained,false);a.equal(sql.externalInference,false);a.equal(sql.ownedResourcesRemoved,true);a.equal(sql.inventoryPreserved,true);a.deepEqual(sql.cleanupErrors,[]);a.equal(sql.checks.length,record.checks);
 a.equal(sql.expectedMigrations.AcademyDeskDbContext.length,102);a.equal(sql.expectedMigrations.IdentityDbContext.length,7);a.deepEqual(sql.appliedMigrations,[...sql.expectedMigrations.AcademyDeskDbContext,...sql.expectedMigrations.IdentityDbContext].sort());
 a.equal(sql.checks.some(row=>row.status===429||row.status>=500),false);
 a.equal(window.run,record.window);a.equal(window.passed,true);a.equal(window.runtimeExit,0);a.equal(window.runtimePassed,true);a.equal(window.restorationPassed,true);a.equal(window.inventoryPreserved,true);a.equal(window.restartInvocations,0);a.equal(window.productionApproved,false);a.ok(window.admissionFreeBytes>=3*1024**3);a.deepEqual(window.before,window.after);a.deepEqual(window.restorationErrors,[]);
 a.deepEqual(window.services.map(row=>row.name).sort(),['pentaaimodels-api-1','pentaaimodels-inference-1']);a.equal(window.restored.length,2);
 for(const service of window.services){const restored=window.restored.find(row=>row.name===service.name);a.equal(restored.id,service.id);a.equal(restored.image,service.image);a.equal(restored.running,true);a.ok(restored.health===null||restored.health==='healthy');}
 const summaries=log.split(/\r?\n/).filter(Boolean).map(line=>{try{return JSON.parse(line);}catch{return null;}}).filter(Boolean);
 a.ok(summaries.some(row=>row.run===record.run&&row.passed===true&&row.ownedResourcesRemoved===true&&row.inventoryPreserved===true),'Service window must contain the exact successful child run.');
 if(record.scope==='finance'){
  const f=sql.finance;a.deepEqual(f.sequential,{invoice:1000,reconciled:600,balance:400,excess500Status:400,rows:1});a.equal(f.races.length,5);a.equal(f.paymentSuccessAudits,11);
  for(const row of f.races){a.deepEqual(row.statuses,[201,400]);a.equal(row.bothReadyBeforeRelease,true);a.equal(row.rejectionMessage,true);a.equal(row.collected,1000);a.equal(row.rows,2);a.equal(row.reconciled,1);a.equal(row.completed,1);a.equal(row.status,'Paid');a.equal(row.adjusted,0);}
 }
 if(record.scope==='reconstructed-baseline-upgrade'){
  const u=sql.upgrade;a.equal(u.baselineApplication,82);a.equal(u.baselineIdentity,7);a.equal(u.pendingApplication,20);a.equal(u.pendingIdentity,0);a.equal(u.rowsPreserved,4);a.match(u.baselineDataSha256,/^[A-F0-9]{64}$/);a.equal(u.afterDataSha256,u.baselineDataSha256);a.equal(u.reconstructedSchema,true);a.equal(u.originalReleaseBinaryTested,false);
 }
 if(['disposable-backup-restore','combined-database-encrypted-key-recovery'].includes(record.scope)){
  const r=sql.recovery;a.equal(r.passed,true);a.equal(r.phase,'complete');a.equal(r.syntheticOnly,true);a.equal(r.backupExported,false);a.equal(r.productionBackupTested,false);a.equal(r.keyringRecoveryTested,false);a.equal(r.migrationHistories,109);a.equal(Object.keys(r.tablesBefore).length,8);a.deepEqual(r.tablesAfter,r.tablesBefore);
 }
 if(['encrypted-key-replacement','combined-database-encrypted-key-recovery','encrypted-key-startup-faults','encrypted-key-certificate-rotation'].includes(record.scope)){
  const k=sql.keyRecovery;a.equal(k.passed,true);a.equal(k.phase,'complete');a.ok(k.encryptedBefore.length>0);a.deepEqual(k.encryptedAfter,k.encryptedBefore);a.equal(k.syntheticOnly,true);a.equal(k.productionStorageTested,false);a.equal(k.rotationTested,false);a.equal(k.sharedReplicaConcurrencyTested,false);a.equal(sql.syntheticKeyMaterialRemoved,true);
  if(record.keyBackup)a.equal(k.separateSyntheticKeyBackupTested,true);
 }
 if(record.scope==='combined-database-encrypted-key-recovery')a.equal(sql.jointSyntheticDatabaseAndKeyRecovery,true);
 if(record.scope==='encrypted-key-startup-faults'){
  const k=sql.keyConfiguration;a.equal(k.passed,true);a.equal(k.syntheticOnly,true);a.deepEqual(k.cases.map(row=>row.name),require('./api-image-key-config.cjs').cases.map(row=>row.name));
  for(const row of k.cases){a.equal(row.exited,true);a.ok(Number.isInteger(row.exitCode)&&row.exitCode!==0);a.equal(row.notOom,true);a.equal(row.safeConfigurationFailure,true);}
 }
 if(record.scope==='encrypted-key-certificate-rotation'){
  const k=sql.keyRotation;a.equal(k.passed,true);a.equal(k.syntheticOnly,true);a.equal(k.productionRotationApproved,false);a.match(k.oldKeyId,/^[0-9a-f-]{36}$/);a.match(k.newKeyId,/^[0-9a-f-]{36}$/);a.notEqual(k.newKeyId,k.oldKeyId);a.match(sql.keyMaintenanceBuild,/^[0-9a-f]{32}$/);
 }
 if(record.scope==='approved-financial-policies'){
  const p=sql.policies;a.equal(p.passed,true);a.equal(p.noWriteGuards,15);a.deepEqual(p.approvedProductRules,['FP1','FP2','FP3']);a.equal(p.accountingTaxPayrollReviewComplete,false);a.equal(p.reductionWithoutSettlement,true);a.equal(p.noCashDueModels,2);a.equal(p.voidedTerminal,true);a.deepEqual(p.statements.map(row=>row.model),['Monthly','SessionBlock']);for(const row of p.statements){a.equal(Object.keys(row.checks).length,11);a.ok(Object.values(row.checks).every(value=>value===true));}
 }
 if(record.scope==='native-payment-browser'){
  const p=sql.paymentBrowser;a.equal(p.passed,true);a.equal(p.phase,'complete');a.equal(p.exportBuild,record.web?.build||'bad444b6c61e45f680ed62f76d35dcf4');a.equal(p.verifiedExportFiles,record.web?.files||628);a.equal(p.apiResponseMocks,false);a.equal(p.tokenInjection,false);a.equal(p.physicalDevicesTested,false);
  a.deepEqual(p.cases.map(row=>[row.width,row.theme,row.submittedStatus]),[[390,'light',201],[390,'dark',400],[1440,'light',201],[1440,'dark',400]]);
  for(const row of p.cases){a.deepEqual(row.sql,{rows:2,collected:1000,status:'Paid',balance:0});a.equal(row.visiblePickerCollapsed,true);a.equal(row.noHorizontalOverflow,true);a.equal(row.pageErrors,0);a.ok(row.requests.some(request=>request.path==='/api/auth/login'&&request.method==='POST'&&request.status===200));a.ok(row.requests.some(request=>request.path==='/api/auth/session'&&request.method==='GET'&&request.status===200));const writes=row.requests.filter(request=>request.method==='POST'&&/^\/api\/academies\/[a-f0-9-]{36}\/payments$/.test(request.path));a.equal(writes.length,1);a.equal(writes[0].status,row.submittedStatus);a.equal(row.requests.some(request=>request.status===429||request.status>=500),false);}
 }
 if(record.scope==='native-audit-faults'){
  const p=sql.auditFaults;a.equal(p.passed,true);a.equal(p.syntheticOnly,true);a.equal(p.faultEnabled,true);a.equal(p.faultRemoved,true);a.equal(p.applicationRulesChanged,false);a.equal(p.protectedTableCount,12);a.equal(p.allFaultsNoWrite,true);a.equal(p.recoveredStudentIdentityLinkVerified,true);
  const names=['payment-create','adjustment-approval','payment-void','payroll-payout','onboarding'];a.deepEqual(p.cases.map(row=>row.name),names);for(const row of p.cases){a.equal(row.status,500);a.equal(row.domainIdentityAuditUnchanged,true);}a.deepEqual(p.recovery.map(row=>row.name),names);a.deepEqual(p.recovery.map(row=>row.status),[201,200,200,201,200]);a.ok(p.recovery.every(row=>row.successAudits===1));a.deepEqual(sql.syntheticCounts,{academies:2,identities:6,payments:2});
 }
 if(record.scope==='native-finance-transitions'){
  const transitions=qualifyTransitions(sql.transitions);a.deepEqual(sql.syntheticCounts,{academies:2,identities:5,payments:transitions.paymentRows});
 }
 if(record.scope==='native-role-entry-browser'){
  qualifyEntryBrowser(sql.entryBrowser,record.web);a.deepEqual(sql.syntheticCounts,{academies:2,identities:5,payments:0});
 }
 if(record.scope==='native-report-browser'){require('./api-image-report-browser.cjs').qualifyReportProof(sql.reportBrowser,record.web);a.deepEqual(sql.syntheticCounts,{academies:2,identities:5,payments:4});}
 if(record.scope==='native-expense-browser'){require('./api-image-expense-browser.cjs').verifyProof(sql.expenseBrowser,record.web);a.deepEqual(sql.syntheticCounts,{academies:2,identities:5,payments:0});a.deepEqual(sql.expenseRejectionGuards,['sub-cent','overflow','zero','foreign branch'].map(label=>({label,status:400,noWriteOrAudit:true})));a.equal(sql.expenseRows,4);}
 if(record.scope==='native-teacher-onboarding-browser'){require('./api-image-teacher-browser.cjs').verifyProof(sql.teacherBrowser,record.web);a.deepEqual(sql.syntheticCounts,{academies:2,identities:5,payments:0});a.equal(sql.teacherRows,4);}
 if(record.scope==='native-student-onboarding-browser'){require('./api-image-student-browser.cjs').verifyProof(sql.studentBrowser,record.web);a.deepEqual(sql.syntheticCounts,{academies:2,identities:5,payments:0});a.equal(sql.studentRows,4);a.equal(sql.guardianRows,2);a.equal(sql.parentLinkRows,2);}
 if(record.scope==='native-enrollment-browser'){require('./api-image-enrollment-browser.cjs').verifyProof(sql.enrollmentBrowser,record.web);a.deepEqual(sql.syntheticCounts,{academies:2,identities:5,payments:0});a.equal(sql.enrollmentRows,4);}
 return{scope:record.scope,run:record.run,window:record.window,checks:record.checks,receiptAccepted:true,scopeDeclaration:knownLegacyMode?'known pinned role-tenant receipt predates mode field':'explicit mode field'};
}
function selectRecords(apiOnly){return apiOnly?records.filter(record=>!['native-payment-browser','native-role-entry-browser'].includes(record.scope)):records;}
function run(){
 const apiOnly=process.argv[2]==='--api-only';a.ok(process.argv.length===2||apiOnly&&process.argv.length===3);a.equal(process.env.QA_WEB_CANDIDATE,undefined,'Use the explicit candidate qualifier for new frontend evidence.');const receipt=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-'+build+'/result.json')));a.equal(receipt.passed,true);a.equal(receipt.imageId,image);a.deepEqual(require('./api-image-build.cjs').inputs(),receipt.sourceFiles);if(!apiOnly)require('./api-image-payment-browser.cjs').verifyExport();
 const qualified=selectRecords(apiOnly).map(record=>{
  const sqlFile=path.join(root,'QA/EVIDENCE/api-image-sql-'+record.run+'/result.json'),windowFile=path.join(root,'QA/EVIDENCE/api-runtime-window-'+record.window+'/result.json'),sqlBytes=fs.readFileSync(sqlFile),windowBytes=fs.readFileSync(windowFile),log=fs.readFileSync(path.join(path.dirname(windowFile),'runtime.log'),'utf8');
  const sql=JSON.parse(sqlBytes),accepted=qualify(record,sql,JSON.parse(windowBytes),log);
  const screenshots=record.scope==='native-role-entry-browser'?sql.entryBrowser.cases.flatMap(row=>['entry','manual'].map(prefix=>{
   const name=`${prefix}-${row.role}-${row.width}-${row.theme}.png`,bytes=fs.readFileSync(path.join(path.dirname(sqlFile),name));a.ok(bytes.length>=24&&bytes.length<10*1024**2);a.equal(bytes.subarray(0,8).toString('hex'),'89504e470d0a1a0a');return{name,sha256:crypto.createHash('sha256').update(bytes).digest('hex')};
  })):undefined;
  return{...accepted,...(screenshots?{screenshots}:{}),sqlReceiptSha256:crypto.createHash('sha256').update(sqlBytes).digest('hex'),windowReceiptSha256:crypto.createHash('sha256').update(windowBytes).digest('hex')};
 });
 return{build,imageId:image,currentApplicationSourcesMatch:true,currentFrontendSourcesVerified:!apiOnly,scope:apiOnly?'API-only: explicitly excludes two historical web-dependent receipts':'API plus original pinned frontend',qualified,productionApproved:false,enterpriseTestingComplete:false,limitations:['No entire issue or broad package closed by this read-only reconciliation.','Local synthetic combined recovery is not production storage or disaster-recovery approval.','Original historical binary provenance, production storage/ACLs/rotation/full recovery and rollback remain open.','Critical linked E2E, physical devices, security/privacy review and private-model qualification remain open.','No commit, push, main/Mini write, deployment or credentials exposed.']};
}
module.exports={qualify,qualifyTransitions,qualifyEntryBrowser,selectRecords,records,build,image};if(require.main===module){try{console.log(JSON.stringify(run(),null,2));}catch{console.error('Pinned qualification failed; do not count the receipts as current acceptance. Values withheld.');process.exitCode=1;}}
