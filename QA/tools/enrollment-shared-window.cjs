'use strict';
// Existing approved bounded SQL/API windows; no downloads, model or Azure work.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os'),a=require('node:assert/strict');
const{execFileSync,spawn}=require('node:child_process');
const root=path.resolve(__dirname,'../..'),sleep=ms=>new Promise(r=>setTimeout(r,ms));
const originals=[['pentaaimodels-api-1','978357d1b07fc4e52fddd2530f4c485a1bbe34858a199de4e752bda621131278','sha256:c7eb72ae4cc71208006b57e32122165f4f45b4e9c88d709b8ce05d28ceed6bdf'],['pentaaimodels-inference-1','c06a0392290ce1b9198ff49e2bf6d1e9d4c2891b88e115b40e0329dda580aca2','sha256:559ac229adefe0f7e2d4e32f5222f26927b6bb8ba44b9db08e2544afebf41984']];
function docker(args){return execFileSync('docker',args,{windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();}
function inventory(){return Object.fromEntries([['containers',['ps','-aq','--no-trunc']],['volumes',['volume','ls','-q']],['networks',['network','ls','-q','--no-trunc']],['images',['image','ls','-aq','--no-trunc']]].map(([key,args])=>[key,[...new Set(docker(args).split(/\r?\n/).filter(Boolean))].sort()]));}
async function guard(){if(global.gc)global.gc();const until=Date.now()+45000;while(os.freemem()<3*1024**3&&Date.now()<until)await sleep(2000);a.ok(os.freemem()>=3*1024**3,'Unchanged3GiB free RAM guard refused next step.');}
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
async function work(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');
 const reuseArg=process.argv[3],reuseApi=reuseArg?.startsWith('--reuse-api=')?reuseArg.slice(12):null;
 a.deepEqual(process.argv.slice(2),['--validate',...(reuseApi?['--reuse-api='+reuseApi]:[])]);if(reuseApi)a.match(reuseApi,/^[0-9a-f]{32}$/);
 a.deepEqual(docker(['ps','--format','{{.Names}}']).split(/\r?\n/).sort(),originals.map(s=>s[0]).sort());
 for(const[name,id,image]of originals){const row=JSON.parse(docker(['inspect',id]))[0];a.equal(row.Name,'/'+name);a.equal(row.Image,image);a.equal(row.State.Running,true);}
 docker(['image','inspect','mcr.microsoft.com/mssql/server:2022-latest']);
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/enrollment-shared-window-'+run);fs.mkdirSync(evidence,{recursive:true});
 const pins=require('./api-image-build.cjs').inputs(),before=inventory(),result={run,passed:false,productionApproved:false,downloaded:false,modelLoaded:false,sourcePins:pins,before,steps:[],services:originals};
 const validationFiles=['QA/tools/enrollment-shared-window.cjs','QA/tools/SqlHarness/EnrollmentMutationRegression.cs','QA/tools/SqlHarness/EnrollmentLifecycleRegression.cs',
  'QA/tools/SqlHarness/Program.cs','QA/tools/SqlHarness/Run-ReconciledPayment.ps1','tests/AcademyDesk.Api.Tests/EnrollmentMutationServiceTests.cs',
  'tests/AcademyDesk.Api.Tests/PentaEnrollmentFieldsTests.cs','tests/AcademyDesk.Api.Tests/EnrollmentAdmissionRulesTests.cs',
  'tests/AcademyDesk.Api.Tests/Infrastructure/QaApiFactory.cs','QA/tools/ci-release-artifacts.cjs'];
 const validationPins=()=>validationFiles.map(p=>({path:p,sha256:hash(path.join(root,p))}));result.validationPins=validationPins();
 const save=()=>fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));let stopped=false;save();console.log(JSON.stringify({run,evidence,phase:'approved-local-validation-window'}));
 async function child(name,args,timeout,extra={},cwd=root){
  await guard();console.log(JSON.stringify({run,phase:name}));const log=fs.createWriteStream(path.join(evidence,name+'.log'),{flags:'wx'});let tail='';
  const binary=name==='sql'?'powershell':name.startsWith('frontend-')?process.execPath:'dotnet';const p=spawn(binary,args,{cwd,windowsHide:true,env:{...process.env,DOTNET_CLI_TELEMETRY_OPTOUT:'1',DOTNET_NOLOGO:'1',DOTNET_GCHeapHardLimit:'0x60000000',...extra},stdio:['ignore','pipe','pipe']});
  p.stdout.on('data',b=>tail=(tail+b.toString()).slice(-(1024**2)));p.stdout.pipe(log,{end:false});p.stderr.pipe(log,{end:false});let timedOut=false;
  const timer=setTimeout(()=>{if(p.exitCode===null){timedOut=true;execFileSync('taskkill.exe',['/PID',String(p.pid),'/T','/F'],{windowsHide:true,stdio:'ignore'});}},timeout);
  const code=await new Promise((resolve,reject)=>{p.on('error',reject);p.on('close',resolve);}).finally(()=>clearTimeout(timer));await new Promise(r=>log.end(r));result.steps.push({name,exitCode:code,timedOut});save();a.equal(timedOut,false);a.equal(code,0,'Owned enrollment validation failed: '+name+'; private logs retained.');a.deepEqual(require('./api-image-build.cjs').inputs(),pins);a.deepEqual(validationPins(),result.validationPins);return tail;
 }
 try{
  stopped=true;for(const[,id,image]of originals){a.equal(JSON.parse(docker(['inspect',id]))[0].Image,image);docker(['stop','--time','15',id]);}
  await guard();result.admissionFreeBytes=os.freemem();save();
  if(reuseApi){
   const priorFolder=path.join(root,'QA/EVIDENCE/enrollment-shared-window-'+reuseApi),prior=JSON.parse(fs.readFileSync(path.join(priorFolder,'result.json')));
   a.ok(prior.steps.some(s=>s.name==='api-tests'&&s.exitCode===0&&!s.timedOut));a.deepEqual(prior.sourcePins,pins);
   for(const pin of prior.validationPins.filter(p=>p.path.startsWith('tests/')))a.equal(hash(path.join(root,pin.path)),pin.sha256,'Changed tested source: '+pin.path);
   const originalTrx=path.join(priorFolder,'api.trx'),counts=require('./ci-release-artifacts.cjs').counters(fs.readFileSync(originalTrx,'utf8'));a.deepEqual(counts,{total:3183,passed:3183,skipped:0});
   fs.copyFileSync(originalTrx,path.join(evidence,'api.trx'),fs.constants.COPYFILE_EXCL);result.reusedApiFrom={run:reuseApi,trxSha256:hash(originalTrx),apiSourceUnchanged:true,relevantTestSourcesUnchanged:true};
   result.steps.push({name:'api-tests-reused',exitCode:0,timedOut:false});save();
  }else await child('api-tests',['test','tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj','--no-restore','-p:UseSharedCompilation=false','--logger','trx;LogFileName=api.trx','--results-directory',evidence],600000);
  result.tests=require('./ci-release-artifacts.cjs').counters(fs.readFileSync(path.join(evidence,'api.trx'),'utf8'));save();
  await child('harness-build',['build','QA/tools/SqlHarness/SqlHarness.csproj','--no-restore','--artifacts-path','.build-check/enrollment-lifecycle-sql','-p:UseSharedCompilation=false'],300000);
  const output=await child('sql',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(root,'QA/tools/SqlHarness/Run-ReconciledPayment.ps1'),'-Module','EnrollmentLifecycle'],600000,{QA_ENROLLMENT_SHARED:'1'});
  a.match(output,/PASS: ENROLLMENTLIFE all 28 native SQL\/HTTP cases/);a.match(output,/PASS: ENROLLMENTSHARED all 57 real SQL\/HTTP\/service cases/);
  a.match(output,/PASS: application migrations=\d+, identity migrations=\d+, scoped runtime login verified; run-owned database and login removed\./);
  const sqlRun=/QA EnrollmentLifecycle run=([0-9a-f]{32})/.exec(output)?.[1];a.match(sqlRun||'',/^[0-9a-f]{32}$/);result.sqlRun=sqlRun;
  const current=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/enrollment-shared-sql-'+sqlRun+'/result.json')));
  a.equal(current.passed,true);a.equal(current.checks,57);a.equal(current.capacityRaces,5);a.equal(current.duplicateRaces,5);a.equal(current.transferRaces,5);a.equal(current.modelInference,false);a.equal(current.toolRegistered,false);
  a.ok(current.raceStatuses.every(x=>JSON.stringify(x)==='[201,409]'));result.native=current;result.retainedLifecycleChecks=28;
  result.validationComplete=true;
 }catch(error){result.failure=String(error.message).slice(0,220);}
 finally{
  console.log(JSON.stringify({run,phase:'restore-exact-original-services'}));result.restorationErrors=[];
  if(stopped)for(const[name,id,image]of [...originals].reverse())try{const row=JSON.parse(docker(['inspect',id]))[0];a.equal(row.Image,image);if(!row.State.Running)docker(['start',id]);}catch{result.restorationErrors.push(name);}
  const until=Date.now()+120000;do{result.restored=originals.map(([name,id,image])=>{const row=JSON.parse(docker(['inspect',id]))[0];a.equal(row.Image,image);return{name,id:row.Id,image:row.Image,running:row.State.Running,health:row.State.Health?.Status||null};});if(result.restored.every(s=>s.running&&(!s.health||s.health==='healthy')))break;await sleep(2000);}while(Date.now()<until);
  result.restorationPassed=!result.restorationErrors.length&&result.restored.every(s=>s.running&&(!s.health||s.health==='healthy'));result.after=inventory();
  try{a.deepEqual(before,result.after);result.inventoryPreserved=true;}catch{result.inventoryPreserved=false;}
  result.sourceUnchanged=JSON.stringify(require('./api-image-build.cjs').inputs())===JSON.stringify(pins)&&JSON.stringify(validationPins())===JSON.stringify(result.validationPins);
  result.passed=!!(result.validationComplete&&result.restorationPassed&&result.inventoryPreserved&&result.sourceUnchanged);save();
  console.log(JSON.stringify({run,passed:result.passed,tests:result.tests,sqlRun:result.sqlRun,restorationPassed:result.restorationPassed,inventoryPreserved:result.inventoryPreserved,failure:result.failure,evidence}));if(!result.passed)process.exitCode=1;
 }
}
require('./local-validation-lease.cjs').withLease(path.join(root,'.build-check'),'enrollment-shared',work).catch(()=>{console.error('Enrollment validation preflight refused; no new authority.');process.exitCode=1;});
