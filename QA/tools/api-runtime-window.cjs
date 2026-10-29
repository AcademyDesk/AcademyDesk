'use strict';
// Explicitly approved temporary stop of only two existing services; always restore.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os');
const{execFileSync,spawn}=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),expected=['pentaaimodels-api-1','pentaaimodels-inference-1'].sort();
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function docker(args){return execFileSync('docker',args,{windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();}
function inventory(){return{containers:docker(['ps','-aq','--no-trunc']).split(/\r?\n/).filter(Boolean).sort(),volumes:docker(['volume','ls','-q']).split(/\r?\n/).filter(Boolean).sort(),networks:docker(['network','ls','-q','--no-trunc']).split(/\r?\n/).filter(Boolean).sort(),images:[...new Set(docker(['image','ls','-aq','--no-trunc']).split(/\r?\n/).filter(Boolean))].sort()};}
async function run(){
 const reviewSignoffNative=process.argv[2]==='--approved-review-signoff-browser-cache-stop-restore-26oct';
 if(reviewSignoffNative){a.equal(process.argv.length,5);a.match(process.argv[4],/^[0-9a-f]{32}$/);process.env.QA_REVIEW_SIGNOFF_UI=process.argv[4];process.argv.splice(4,1);process.argv[2]='--approved-review-provenance-guard-cache-stop-restore-25oct';}
 const reviewSignoffUi=process.argv[2]==='--approved-review-signoff-ui-cache-stop-restore-26oct';
 if(reviewSignoffUi)process.argv[2]='--approved-staff-ui-cache-stop-restore-25oct';
 const reviewProvenanceBaseline=process.argv[2]==='--approved-review-provenance-baseline-cache-stop-restore-25oct',reviewProvenanceGuard=process.argv[2]==='--approved-review-provenance-guard-cache-stop-restore-25oct';
 if(reviewProvenanceBaseline||reviewProvenanceGuard)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const materialScope=process.argv[2]==='--approved-material-scope-ui-cache-stop-restore-25oct';
 if(materialScope)process.argv[2]='--approved-staff-ui-cache-stop-restore-25oct';
 const compactThemeNative=process.argv[2]==='--approved-compact-theme-access-browser-cache-stop-restore-25oct';
 if(compactThemeNative)process.argv[2]='--approved-access-review-browser-sql-only-cache-stop-restore-25oct';
 const compactTheme=process.argv[2]==='--approved-compact-theme-ui-cache-stop-restore-25oct';
 if(compactTheme)process.argv[2]='--approved-staff-ui-cache-stop-restore-25oct';
 const compactThemeBaseline=process.argv[2]==='--approved-compact-theme-baseline-cache-stop-restore-25oct';
 if(compactThemeBaseline)process.argv[2]='--approved-staff-ui-cache-stop-restore-25oct';
 const selectRetest=process.argv[2]==='--approved-shared-select-browser-retest-cache-stop-restore-25oct';
 if(selectRetest)process.argv[2]='--approved-staff-ui-cache-stop-restore-25oct';
 const selectKeyboard=process.argv[2]==='--approved-shared-select-ui-cache-stop-restore-25oct';
 if(selectKeyboard)process.argv[2]='--approved-staff-ui-cache-stop-restore-25oct';
 const accessExpiry=process.argv[2]==='--approved-access-expiry-ui-cache-stop-restore-25oct';
 if(accessExpiry)process.argv[2]='--approved-staff-ui-cache-stop-restore-25oct';
 const staffLayout=process.argv[2]==='--approved-staff-layout-ui-cache-stop-restore-25oct';
 if(staffLayout)process.argv[2]='--approved-staff-ui-cache-stop-restore-25oct';
 const staffSqlOnly=process.argv[2]==='--approved-staff-browser-sql-only-cache-stop-restore-25oct';
 if(staffSqlOnly)process.argv[2]='--approved-staff-browser-cache-stop-restore-25oct';
 const staffUi=process.argv[2]==='--approved-staff-ui-cache-stop-restore-25oct',staffBrowser=process.argv[2]==='--approved-staff-browser-cache-stop-restore-25oct';
 if(staffUi)process.argv[2]='--approved-guardian-context-ui-cache-stop-restore-24oct';
 if(staffBrowser)process.argv[2]='--approved-guardian-context-browser-cache-stop-restore-24oct';
 const staffAuditBaseline=process.argv[2]==='--approved-staff-audit-baseline-cache-stop-restore-25oct',staffAuditGuard=process.argv[2]==='--approved-staff-audit-guard-cache-stop-restore-25oct';
 if(staffAuditBaseline||staffAuditGuard)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const accessSqlOnly=process.argv[2]==='--approved-access-review-browser-sql-only-cache-stop-restore-25oct';
 if(accessSqlOnly)process.argv[2]='--approved-access-review-browser-cache-stop-restore-25oct';
 const accessUi=process.argv[2]==='--approved-access-review-ui-cache-stop-restore-25oct',accessBrowser=process.argv[2]==='--approved-access-review-browser-cache-stop-restore-25oct';
 if(accessUi)process.argv[2]='--approved-guardian-context-ui-cache-stop-restore-24oct';
 if(accessBrowser)process.argv[2]='--approved-guardian-context-browser-cache-stop-restore-24oct';
 const roleAuditBaseline=process.argv[2]==='--approved-role-audit-baseline-cache-stop-restore-25oct',roleAuditGuard=process.argv[2]==='--approved-role-audit-guard-cache-stop-restore-25oct';
 if(roleAuditBaseline||roleAuditGuard)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const payrollBrowser=process.argv[2]==='--approved-payroll-period-browser-cache-stop-restore-25oct';
 if(payrollBrowser)process.argv[2]='--approved-guardian-context-browser-cache-stop-restore-24oct';
 const payrollPeriodSqlOnly=process.argv[2]==='--approved-payroll-period-sql-only-cache-stop-restore-25oct';
 if(payrollPeriodSqlOnly)process.argv[2]='--approved-payroll-period-guard-cache-stop-restore-25oct';
 const payrollPeriodBaseline=process.argv[2]==='--approved-payroll-period-baseline-cache-stop-restore-25oct',payrollPeriodGuard=process.argv[2]==='--approved-payroll-period-guard-cache-stop-restore-25oct';
 if(payrollPeriodBaseline||payrollPeriodGuard)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const grantAuditNormal=process.argv[2]==='--approved-grant-audit-normal-proof-cache-stop-restore-25oct';
 if(grantAuditNormal)process.argv[2]='--approved-effective-grants-cache-stop-restore-24oct';
 const grantAuditBaseline=process.argv[2]==='--approved-grant-audit-baseline-cache-stop-restore-25oct',grantAuditGuard=process.argv[2]==='--approved-grant-audit-guard-cache-stop-restore-25oct';
 if(grantAuditBaseline||grantAuditGuard)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const effectiveGrants=process.argv[2]==='--approved-effective-grants-cache-stop-restore-24oct';
 if(effectiveGrants)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const guardianBrowser=process.argv[2]==='--approved-guardian-context-browser-cache-stop-restore-24oct';let guardianUiSource;
 if(guardianBrowser){a.equal(process.argv.length,5);guardianUiSource=process.argv[4];a.match(guardianUiSource,/^[0-9a-f]{32}$/);process.argv.splice(4,1);process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';}
 const guardianUi=process.argv[2]==='--approved-guardian-context-ui-cache-stop-restore-24oct';
 if(guardianUi)process.argv[2]='--approved-enrollment-conflict-ui-cache-stop-restore-24oct';
 const zodAlign=process.argv[2]==='--approved-zod-align-cache-stop-restore-24oct';
 if(zodAlign)process.argv[2]='--approved-zod-security-cache-stop-restore-24oct';
 const zodSecurity=process.argv[2]==='--approved-zod-security-cache-stop-restore-24oct';
 if(zodSecurity)process.argv[2]='--approved-enrollment-conflict-ui-cache-stop-restore-24oct';
 const admissionAuthority=process.argv[2]==='--approved-admission-authority-cache-stop-restore-24oct';
 if(admissionAuthority)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const admissionConfiguration=process.argv[2]==='--approved-admission-configuration-cache-stop-restore-24oct';
 if(admissionConfiguration)process.argv[2]='--approved-admission-lifecycle-cache-stop-restore-24oct';
 const zodEnrollmentBrowser=process.argv[2]==='--approved-zod-enrollment-browser-cache-stop-restore-24oct';
 const enrollmentUiBrowser=zodEnrollmentBrowser||process.argv[2]==='--approved-enrollment-conflict-browser-cache-stop-restore-24oct';
 let enrollmentUiSource;
 if(enrollmentUiBrowser){a.equal(process.argv.length,5);enrollmentUiSource=process.argv[4];a.match(enrollmentUiSource,/^[0-9a-f]{32}$/);process.argv.splice(4,1);process.argv[2]='--approved-admission-browser-cache-stop-restore-23oct';}
 const enrollmentUi=process.argv[2]==='--approved-enrollment-conflict-ui-cache-stop-restore-24oct';
 if(enrollmentUi)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const admissionLifecycle=process.argv[2]==='--approved-admission-lifecycle-cache-stop-restore-24oct';
 if(admissionLifecycle)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const policyPayload=process.argv[2]==='--approved-policy-payload-cache-stop-restore-24oct';
 if(policyPayload)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const admissionPolicy=process.argv[2]==='--approved-admission-policy-cache-stop-restore-24oct';
 if(admissionPolicy)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 const rulesBrowser=process.argv[2]==='--approved-admission-rules-browser-cache-stop-restore-23oct';
 if(rulesBrowser)process.argv[2]='--approved-admission-browser-cache-stop-restore-23oct';
 const admissionRules=process.argv[2]==='--approved-admission-rules-cache-stop-restore-23oct';
 if(admissionRules)process.argv[2]='--approved-admission-repair-cache-stop-restore-23oct';
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');const admissionBrowser=process.argv[2]==='--approved-admission-browser-cache-stop-restore-23oct',admissionRepair=process.argv[2]==='--approved-admission-repair-cache-stop-restore-23oct';a.equal(process.argv.length,admissionRepair||admissionBrowser?4:3);const candidateBuild=admissionRepair||admissionBrowser?process.argv[3]:'0a5f8d2c708a40a994dd97e9f6d12425';a.match(candidateBuild,/^[0-9a-f]{32}$/);
 const admissionReproduction=process.argv[2]==='--approved-admission-reproduction-cache-stop-restore-23oct',enrollmentBrowser=admissionBrowser||process.argv[2]==='--approved-enrollment-browser-cache-stop-restore-23oct',studentBrowser=process.argv[2]==='--approved-student-browser-cache-stop-restore-23oct',teacherBrowser=process.argv[2]==='--approved-teacher-browser-cache-stop-restore-23oct',expenseBrowser=process.argv[2]==='--approved-expense-browser-cache-stop-restore-23oct',reportBrowser=process.argv[2]==='--approved-report-browser-cache-stop-restore-23oct',cache=admissionRepair||admissionReproduction||enrollmentBrowser||studentBrowser||teacherBrowser||expenseBrowser||reportBrowser||['--approved-payment-browser-cache-stop-restore-23oct','--approved-entry-browser-cache-stop-restore-23oct'].includes(process.argv[2]);
 const finance=process.argv[2]==='--approved-finance-stop-restore-22oct',transitionVoids=process.argv[2]==='--approved-transition-voids-stop-restore-22oct',transitions=transitionVoids||process.argv[2]==='--approved-transitions-stop-restore-22oct',entryBrowser=process.argv[2]==='--approved-entry-browser-stop-restore-22oct'||process.argv[2]==='--approved-entry-browser-cache-stop-restore-23oct',paymentBrowser=process.argv[2]==='--approved-payment-browser-stop-restore-22oct'||process.argv[2]==='--approved-payment-browser-cache-stop-restore-23oct',auditFaults=process.argv[2]==='--approved-audit-faults-stop-restore-22oct',policies=process.argv[2]==='--approved-policies-stop-restore-22oct',upgrade=process.argv[2]==='--approved-upgrade-stop-restore-22oct',recovery=process.argv[2]==='--approved-recovery-stop-restore-22oct',keys=process.argv[2]==='--approved-keys-stop-restore-22oct',fullRecovery=process.argv[2]==='--approved-full-recovery-stop-restore-22oct',keyFaults=process.argv[2]==='--approved-key-faults-stop-restore-22oct',keyRotation=process.argv[2]==='--approved-key-rotation-stop-restore-22oct',sql=admissionRepair||admissionReproduction||enrollmentBrowser||studentBrowser||teacherBrowser||expenseBrowser||reportBrowser||finance||transitions||entryBrowser||paymentBrowser||auditFaults||policies||upgrade||recovery||keys||fullRecovery||keyFaults||keyRotation||process.argv[2]==='--approved-sql-stop-restore-22oct';a.ok(sql||process.argv[2]==='--approved-stop-restore-22oct','Explicit approved service window required.');
const helper=reviewSignoffUi?'review-signoff-ui-check.cjs':materialScope?'material-scope-ui-check.cjs':compactTheme?'compact-theme-ui-check.cjs':compactThemeBaseline?'compact-theme-baseline.cjs':selectRetest?'shared-select-browser-retest.cjs':selectKeyboard?'shared-select-ui-check.cjs':accessExpiry?'access-expiry-ui-check.cjs':staffLayout?'staff-layout-ui-check.cjs':staffUi?'staff-ui-check.cjs':accessUi?'access-review-ui-check.cjs':guardianUi?'guardian-context-ui-check.cjs':zodAlign?'zod-security-align.cjs':zodSecurity?'zod-security-candidate.cjs':enrollmentUi?'enrollment-conflict-ui-check.cjs':policyPayload?'api-image-artifacts.cjs':(sql||expenseBrowser)?'api-image-sql.cjs':'api-image-runtime.cjs';
 a.deepEqual(docker(['ps','--format','{{.Names}}']).split(/\r?\n/).filter(Boolean).sort(),expected);
 const before=inventory(),services=expected.map(name=>{const row=JSON.parse(docker(['inspect',name]))[0];return{name,id:row.Id,image:row.Image};});
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/api-runtime-window-'+run);fs.mkdirSync(evidence,{recursive:true});
 const result={run,approval:zodSecurity?'Owner24October explicitly approved isolated Zod4.6.6 package acquisition/install/build/tests on D:, original app preserved, same3GiB guard/two-service restore; no main/GitHub/Azure':sql?'explicit bounded local SQL/API validation windows, one guarded test at a time, immediate exact restoration even on failure; no restart/download/prune/model/production/Azure':'explicit temporary two-service stop, guarded API smoke, immediate exact restoration',helper,before,services,passed:false,restartInvocations:0,productionApproved:false,stopStarted:false};
 const save=()=>fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));save();
 let witnessCreated=false;const witnessName='academydesk-enrollment-witness-'+run,witness=require('./web-dependency-window.cjs');
 try{
  result.stopStarted=true;save();console.log(JSON.stringify({run,evidence,phase:'temporary-service-stop'}));
  for(const service of services){const row=JSON.parse(docker(['inspect',service.id]))[0];a.equal(row.Image,service.image);docker(['stop','--timeout','15',service.id]);}
  if(cache){
   result.cacheApproval='Owner23October explicitly requested freeing safe RAM and continuing; one clean-cache drop after only original two services stop';result.hostFreeBeforeCache=os.freemem();save();a.equal(docker(['ps','--format','{{.Names}}']),'');
   execFileSync('wsl',['-d','docker-desktop','-u','root','--','sync'],{windowsHide:true,timeout:30000,stdio:'ignore'});execFileSync('wsl',['-d','docker-desktop','-u','root','--','sysctl','-w','vm.drop_caches=3'],{windowsHide:true,timeout:30000,stdio:'ignore'});result.cleanCacheDropCompleted=true;save();
  }
  const memoryBy=Date.now()+30000;let free=os.freemem();
  while(free<3*1024**3&&Date.now()<memoryBy){await sleep(2000);free=os.freemem();}
  result.admissionFreeBytes=free;save();a.ok(free>=3*1024**3,'3GiB guard still blocked after temporary stop; restoring without testing.');
  if(enrollmentUi){
   a.equal(JSON.parse(docker(['image','inspect',witness.witnessImage]))[0].Id,witness.witnessImage);
   docker(['create','--pull=never','--name',witnessName,'--label','academydesk.qa.run='+run,'--network','none','--read-only','--user','101:101','--memory','16m','--cpus','0.1','--pids-limit','8','--cap-drop','ALL','--security-opt','no-new-privileges','--entrypoint','/bin/sleep',witness.witnessImage,'3000']);witnessCreated=true;
   docker(['start',witnessName]);witness.verifyWitness(JSON.parse(docker(['inspect',witnessName]))[0],run,witnessName);
   result.ownedIdleWitness={image:witness.witnessImage,network:'none',memoryBytes:16*1024**2,purpose:'Reuse existing bounded idle safeguard; previous bridge drift retained, cause not proven.'};save();
  }
  console.log(JSON.stringify({run,phase:enrollmentUi?'guarded-frontend-candidate':sql?'guarded-sql-integration':'guarded-api-smoke',freeMemoryGiB:free/1024**3}));
  if((reviewProvenanceGuard||grantAuditNormal||payrollPeriodGuard||accessBrowser||staffBrowser)&&!payrollPeriodSqlOnly&&!accessSqlOnly&&!staffSqlOnly){
   // Inspect before SQL allocates memory; retain any earlier post-SQL refusal.
   a.ok(os.freemem()>=3*1024**3,'Unchanged3GiB guard blocked pre-SQL image inspection.');
   const inspection=require('node:child_process').spawnSync(process.execPath,[path.join(root,'QA/tools/api-image-artifacts.cjs'),candidateBuild],{cwd:root,windowsHide:true,encoding:'utf8',timeout:120000,maxBuffer:1024**2});
   fs.writeFileSync(path.join(evidence,'artifacts.log'),(inspection.stdout||'')+(inspection.stderr||''));
   a.equal(inspection.error,undefined);a.equal(inspection.status,0);const proof=JSON.parse(inspection.stdout.trim().split(/\r?\n/).at(-1));a.equal(proof.passed,true);a.equal(proof.ownedContainerRemoved,true);a.equal(proof.build,candidateBuild);result.artifactsRun=proof.run;result.artifactsBeforeSql=true;save();
  }
  const log=fs.createWriteStream(path.join(evidence,'runtime.log'),{flags:'wx'});
const browserEnvironment=compactThemeNative?{QA_COMPACT_THEME_UI:guardianUiSource,QA_STAFF_UI:'',QA_ACCESS_REVIEW_UI:'',QA_GUARDIAN_UI:'',QA_ZOD_CANDIDATE:'',QA_ENROLLMENT_UI:'',QA_WEB_CANDIDATE:''}:staffBrowser?{QA_STAFF_UI:guardianUiSource,QA_ACCESS_REVIEW_UI:'',QA_GUARDIAN_UI:'',QA_ZOD_CANDIDATE:'',QA_ENROLLMENT_UI:'',QA_WEB_CANDIDATE:''}:accessBrowser?{QA_ACCESS_REVIEW_UI:guardianUiSource,QA_GUARDIAN_UI:'',QA_ZOD_CANDIDATE:'',QA_ENROLLMENT_UI:'',QA_WEB_CANDIDATE:''}:guardianBrowser?{QA_GUARDIAN_UI:guardianUiSource,QA_ZOD_CANDIDATE:'',QA_ENROLLMENT_UI:'',QA_WEB_CANDIDATE:''}:zodEnrollmentBrowser?{QA_ZOD_CANDIDATE:enrollmentUiSource,QA_ENROLLMENT_UI:'',QA_WEB_CANDIDATE:''}:enrollmentUiBrowser?{QA_ENROLLMENT_UI:enrollmentUiSource,QA_ZOD_CANDIDATE:'',QA_WEB_CANDIDATE:''}:{QA_ZOD_CANDIDATE:'',QA_ENROLLMENT_UI:'',QA_WEB_CANDIDATE:'293e5beba439453794e6c1e68adc2b71'};
const child=spawn(process.execPath,[path.join(root,'QA/tools',helper),candidateBuild,...(enrollmentUi||policyPayload?[]:reviewProvenanceBaseline?['--review-provenance-baseline']:reviewProvenanceGuard?['--review-provenance-guard']:staffBrowser?['--staff-browser']:staffAuditBaseline?['--staff-audit-baseline']:staffAuditGuard?['--staff-audit-guard']:accessBrowser?['--access-review-browser']:roleAuditBaseline?['--role-audit-baseline']:roleAuditGuard?['--role-audit-guard']:payrollBrowser?['--payroll-period-browser']:payrollPeriodBaseline?['--payroll-period-baseline']:payrollPeriodGuard?['--payroll-period-guard']:grantAuditBaseline?['--grant-audit-baseline']:grantAuditGuard?['--grant-audit-guard']:effectiveGrants?['--effective-grants']:guardianBrowser?['--guardian-context']:admissionAuthority?['--admission-authority']:admissionConfiguration?['--admission-configuration']:admissionLifecycle?['--admission-lifecycle']:admissionPolicy?['--admission-policy']:admissionRules?['--admission-rules']:admissionRepair?['--admission-repair']:admissionReproduction?['--admission-reproduction']:enrollmentBrowser?['--enrollment-browser']:studentBrowser?['--student-browser']:teacherBrowser?['--teacher-browser']:expenseBrowser?['--expense-browser']:reportBrowser?['--report-browser']:finance?['--finance']:transitions?[transitionVoids?'--transition-voids':'--transitions']:entryBrowser?['--entry-browser']:paymentBrowser?['--payment-browser']:auditFaults?['--audit-faults']:policies?['--policies']:upgrade?['--upgrade']:recovery?['--recovery']:keys?['--keys']:fullRecovery?['--full-recovery']:keyFaults?['--key-faults']:keyRotation?['--key-rotation']:[])],{cwd:root,...(guardianBrowser||enrollmentBrowser||studentBrowser||teacherBrowser||reportBrowser||expenseBrowser?{env:{...process.env,QA_STAFF_UI:'',QA_ACCESS_REVIEW_UI:'',...browserEnvironment}}:{}),windowsHide:true,stdio:['ignore','pipe','pipe']});child.stdout.pipe(log,{end:false});child.stderr.pipe(log,{end:false});
  result.runtimeExit=await new Promise((resolve,reject)=>{child.on('error',reject);child.on('close',resolve);});await new Promise(resolve=>log.end(resolve));
  a.equal(result.runtimeExit,0,'Approved API test failed; retain runtime log and restore services.');result.runtimePassed=true;
  if(enrollmentUi){const proof=JSON.parse(fs.readFileSync(path.join(evidence,'runtime.log'),'utf8').trim().split(/\r?\n/).at(-1));a.equal(proof.passed,true);result.uiRun=proof.run;result.webBuild=proof.webBuild;result.scope='offline frontend feedback validation; approved bounded frontend window, no SQL/model/network acquisition';save();}
  if(zodSecurity){result.zodRun=result.uiRun;result.scope='Owner24Oct approved isolated public Zod package acquisition/install/build/browser checks; working app preserved';save();}
  if(zodAlign){result.zodRun=candidateBuild;result.alignmentRun=result.uiRun;result.scope='Exact validated candidate worktree-only alignment after accepted candidate/native proof; previous physical dependencies retained, no install/download/main changes';save();}
  if(accessUi){result.accessReviewUiRun=result.uiRun;result.scope='Access Review grant/revoke feedback and recovery, offline existing dependencies only';save();}
  if(staffUi){result.staffUiRun=result.uiRun;result.scope='Staff create/role/offboard feedback/recovery, offline existing dependencies';save();}
  if(guardianUi&&!accessUi&&!staffUi){result.guardianUiRun=result.uiRun;result.scope='Guardian selected-child frontend read-state repair, offline existing dependencies, no backend/model/package changes';save();}
  if(policyPayload){const proof=JSON.parse(fs.readFileSync(path.join(evidence,'runtime.log'),'utf8').trim().split(/\r?\n/).at(-1));a.equal(proof.passed,true);a.equal(proof.ownedContainerRemoved,true);result.artifactsRun=proof.run;result.scope='unstarted image payload only; no SQL/API/browser execution';save();}
  if(rulesBrowser||admissionPolicy||grantAuditGuard){
   a.ok(os.freemem()>=3*1024**3,'Unchanged3GiB guard blocked actual-image payload inspection.');
   const inspection=require('node:child_process').spawnSync(process.execPath,[path.join(root,'QA/tools/api-image-artifacts.cjs'),candidateBuild],{cwd:root,windowsHide:true,encoding:'utf8',timeout:120000,maxBuffer:1024**2});
   fs.writeFileSync(path.join(evidence,'artifacts.log'),(inspection.stdout||'')+(inspection.stderr||''));
   a.equal(inspection.error,undefined);a.equal(inspection.status,0);const proof=JSON.parse(inspection.stdout.trim().split(/\r?\n/).at(-1));a.equal(proof.passed,true);a.equal(proof.ownedContainerRemoved,true);result.artifactsRun=proof.run;save();
  }
 }catch(error){result.failure=String(error.message).slice(0,500);}
 finally{
  if(result.stopStarted){
   console.log(JSON.stringify({run,phase:'restore-original-services'}));const restorationErrors=[];
   for(const service of [...services].reverse()){try{const row=JSON.parse(docker(['inspect',service.id]))[0];a.equal(row.Image,service.image);if(!row.State.Running)docker(['start',service.id]);}catch{restorationErrors.push(service.name+' could not be restored');}}
   const readyBy=Date.now()+120000;let states=[];
   while(Date.now()<readyBy){states=services.map(service=>{const row=JSON.parse(docker(['inspect',service.id]))[0];return{name:service.name,id:row.Id,image:row.Image,running:row.State.Running,health:row.State.Health?.Status||null};});if(states.every(row=>row.running&&(!row.health||row.health==='healthy')))break;await sleep(2000);}
   result.restored=states;result.restorationErrors=restorationErrors;result.restorationPassed=!restorationErrors.length&&states.every(row=>row.running&&(!row.health||row.health==='healthy'));
  }
  if(witnessCreated){try{const row=JSON.parse(docker(['inspect',witnessName]))[0];a.equal(row.Image,witness.witnessImage);a.equal(row.Name,'/'+witnessName);a.equal(row.Config.Labels['academydesk.qa.run'],run);try{witness.verifyWitness(row,run,witnessName);result.witnessEndVerified=true;}catch{result.witnessEndVerified=false;}docker(['rm','-f',witnessName]);result.ownedWitnessRemoved=true;}catch{result.ownedWitnessRemoved=false;}}
  try{result.after=inventory();a.deepEqual(result.after,before);a.deepEqual(docker(['ps','--format','{{.Names}}']).split(/\r?\n/).filter(Boolean).sort(),expected);result.inventoryPreserved=true;}catch{result.inventoryPreserved=false;}
  result.passed=!!(!result.failure&&result.runtimePassed&&(!enrollmentUi||result.uiRun)&&(!(rulesBrowser||admissionPolicy||policyPayload)||result.artifactsRun)&&result.restorationPassed&&result.inventoryPreserved&&(!witnessCreated||(result.ownedWitnessRemoved&&result.witnessEndVerified)));save();console.log(JSON.stringify({run,passed:result.passed,runtimeExit:result.runtimeExit,restorationPassed:result.restorationPassed,inventoryPreserved:result.inventoryPreserved,evidence}));if(!result.passed)process.exitCode=1;
 }
}
require('./local-validation-lease.cjs').withLease(path.join(root,'.build-check'),'api-runtime',run).catch(error=>{console.error(error.message);process.exitCode=1;});
