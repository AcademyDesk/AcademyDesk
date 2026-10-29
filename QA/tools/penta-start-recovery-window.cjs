'use strict';
// Existing owner-approved bounded frontend/SQL windows; exact service restoration.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os'),a=require('node:assert/strict');
const {execFileSync,spawn}=require('node:child_process');
const root=path.resolve(__dirname,'../..'),web=path.join(root,'apps/web'),helper=require('./default-web-build.cjs');
const expected=['pentaaimodels-api-1','pentaaimodels-inference-1'].sort(),sleep=ms=>new Promise(r=>setTimeout(r,ms));
function docker(args){return execFileSync('docker',args,{windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();}
function inventory(){return Object.fromEntries([['containers',['ps','-aq','--no-trunc']],['volumes',['volume','ls','-q']],['networks',['network','ls','-q','--no-trunc']],['images',['image','ls','-aq','--no-trunc']]].map(([key,args])=>[key,[...new Set(docker(args).split(/\r?\n/).filter(Boolean))].sort()]));}
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
async function memory(){const until=Date.now()+45000;while(os.freemem()<3*1024**3&&Date.now()<until)await sleep(2000);helper.memoryGuard();}
function exportProof(build){
 a.match(build,/^[a-f0-9]{32}$/);const proof=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/default-web-build-'+build+'/result.json')));
 a.equal(proof.passed,true);a.equal(proof.downloaded,false);a.equal(proof.installed,false);a.equal(proof.sourceUnchanged,true);a.deepEqual(helper.sourcePins(web),proof.sourcePins);
 return path.join(root,'.build-check/default-web-build',build,'app/out');
}
async function work(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');const sql=process.argv[2]==='--sql',resume=process.argv[2]==='--frontend-resume';
 a.ok(sql||resume?process.argv.length===4:process.argv.length===3&&process.argv[2]==='--frontend');
 const sourcePins=helper.sourcePins(web),apiPins=require('./api-image-build.cjs').inputs();
 const accepted=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-3368e36621f942bfac353b8ac92824da/result.json')));
 a.deepEqual(apiPins,accepted.sourceFiles,'Reuse the accepted API only if actual domain source is unchanged.');
 let exported;if(sql)exported=exportProof(process.argv[3]);
 a.deepEqual(docker(['ps','--format','{{.Names}}']).split(/\r?\n/).filter(Boolean).sort(),expected);
 if(sql)docker(['image','inspect','mcr.microsoft.com/mssql/server:2022-latest']); // no implicit acquisition
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/penta-start-recovery-window-'+run);fs.mkdirSync(evidence,{recursive:true});
 const before=inventory(),services=expected.map(name=>{const row=JSON.parse(docker(['inspect',name]))[0];return{name,id:row.Id,image:row.Image};});
 const result={run,scope:sql?'retained onboarding SQL regression plus actual reload browser':'focused start checks, types/lint/fresh static export',passed:false,productionApproved:false,modelLoaded:false,downloaded:false,before,services,sourcePins,apiSourceUnchanged:true,steps:[]};
 if(resume){
  a.match(process.argv[3],/^[a-f0-9]{32}$/);const previous=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/penta-start-recovery-window-'+process.argv[3]+'/result.json')));
  a.equal(previous.passed,false);a.equal(previous.restorationPassed,true);a.equal(previous.inventoryPreserved,true);a.deepEqual(previous.sourcePins,sourcePins);
  a.equal(previous.startChecks,58);a.deepEqual(previous.steps.slice(0,2).map(s=>({name:s.name,exitCode:s.exitCode,timedOut:s.timedOut})),[{name:'start-regressions',exitCode:0,timedOut:false},{name:'typecheck',exitCode:0,timedOut:false}]);
  result.reusedChecksFrom=process.argv[3];result.steps=previous.steps.slice(0,2);result.startChecks=58;
 }
 const save=()=>fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));let stopping=false;save();
 async function child(name,exe,args,cwd,timeout,extra={}){
  await memory();console.log(JSON.stringify({run,phase:name}));
  const stream=fs.createWriteStream(path.join(evidence,name+'.log'),{flags:'wx'});let tail='';
  const p=spawn(exe,args,{cwd,windowsHide:true,env:{...helper.buildEnvironment(process.env),NODE_OPTIONS:'--max-old-space-size=1536',DOTNET_CLI_TELEMETRY_OPTOUT:'1',DOTNET_GCHeapHardLimit:'0x60000000',...extra},stdio:['ignore','pipe','pipe']});
  p.stdout.on('data',b=>tail=(tail+b.toString()).slice(-(1024**2)));p.stdout.pipe(stream,{end:false});p.stderr.pipe(stream,{end:false});let timedOut=false;
  const timer=setTimeout(()=>{if(p.exitCode===null){timedOut=true;execFileSync('taskkill.exe',['/PID',String(p.pid),'/T','/F'],{windowsHide:true,stdio:'ignore'});}},timeout);
  const code=await new Promise((resolve,reject)=>{p.on('error',reject);p.on('close',resolve);}).finally(()=>clearTimeout(timer));await new Promise(resolve=>stream.end(resolve));
  result.steps.push({name,exitCode:code,timedOut});save();a.equal(timedOut,false);a.equal(code,0,'Owned validation failed: '+name+'; logs retained.');
  a.deepEqual(helper.sourcePins(web),sourcePins);a.deepEqual(require('./api-image-build.cjs').inputs(),apiPins);return tail;
 }
 try{
  stopping=true;for(const service of services){a.equal(JSON.parse(docker(['inspect',service.id]))[0].Image,service.image);docker(['stop','--time','15',service.id]);}
  await memory();result.admissionFreeBytes=os.freemem();save();
  if(!sql){
   if(!resume){
    const start=await child('start-regressions',process.execPath,[path.join(root,'QA/tools/penta-onboarding-start.test.cjs')],root,120000);a.deepEqual(JSON.parse(start.trim().split(/\r?\n/).at(-1)),{passed:58,failed:0});result.startChecks=58;
    await child('typecheck',process.execPath,[path.join(web,'node_modules/typescript/bin/tsc'),'--noEmit','--incremental','false'],web,180000);
   }
   await child('lint',process.execPath,[path.join(web,'node_modules/eslint/bin/eslint.js'),'src/lib/penta-onboarding-start.ts','src/components/penta/onboarding-start-workspace.tsx','src/app/penta/onboarding/page.tsx','--max-warnings=0'],web,300000);
   const built=JSON.parse((await child('build',process.execPath,[path.join(root,'QA/tools/default-web-build.cjs')],root,900000)).trim().split(/\r?\n/).at(-1));a.equal(built.passed,true);result.webBuild=built.run;
   exported=exportProof(built.run);result.exportManifest=helper.inventory(exported).map(({file,bytes})=>({path:path.relative(exported,file).replaceAll('\\','/'),bytes,sha256:hash(file)}));result.exportFiles=result.exportManifest.length;result.validationComplete=true;
  }else{
   await child('harness-build','dotnet',['build','QA/tools/SqlHarness/SqlHarness.csproj','--no-restore','--artifacts-path','.build-check/onboarding-atomic-sql','-p:UseSharedCompilation=false'],root,300000);
   // Scope script execution to this approved child invocation only; never change
   // machine/user execution policy or any persistent PowerShell setting.
   const output=await child('sql-browser','powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(root,'QA/tools/SqlHarness/Run-ReconciledPayment.ps1'),'-Module','OnboardingAtomic'],root,600000,{QA_ONBOARDING_START_RECOVERY_BROWSER:'1',QA_ONBOARDING_START_EXPORT:process.argv[3]});
   a.match(output,/PASS: PENTAONBOARDINGSTARTBROWSER 5 actual browser\/SQL scenarios/);
   a.match(output,/PASS: application migrations=\d+, identity migrations=\d+, scoped runtime login verified; run-owned database and login removed\./);
   const browserRun=/QA OnboardingAtomic run=([0-9a-f]{32})/.exec(output)?.[1];a.match(browserRun||'',/^[a-f0-9]{32}$/);result.sqlRun=browserRun;
   const browser=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/penta-onboarding-start-live-'+browserRun+'/result.json')));
   a.equal(browser.checks.length,5);a.ok(browser.checks.every(c=>c.passed));a.equal(browser.reloadRecovery,true);a.equal(browser.foreignActorRecoveryDenied,true);a.equal(browser.webBuild,process.argv[3]);a.equal(browser.interpreterInvoked,false);result.validationComplete=true;result.webBuild=process.argv[3];
  }
 }catch(error){result.failure=String(error.message).slice(0,200);}
 finally{
  console.log(JSON.stringify({run,phase:'restore-exact-original-services'}));result.restorationErrors=[];
  if(stopping)for(const service of [...services].reverse())try{const row=JSON.parse(docker(['inspect',service.id]))[0];a.equal(row.Image,service.image);if(!row.State.Running)docker(['start',service.id]);}catch{result.restorationErrors.push(service.name);}
  const until=Date.now()+120000;do{result.restored=services.map(service=>{const row=JSON.parse(docker(['inspect',service.id]))[0];return{id:row.Id,image:row.Image,running:row.State.Running,health:row.State.Health?.Status||null};});if(result.restored.every(s=>s.running&&(!s.health||s.health==='healthy')))break;await sleep(2000);}while(Date.now()<until);
  result.restorationPassed=!result.restorationErrors.length&&result.restored.every(s=>s.running&&(!s.health||s.health==='healthy'));
  result.after=inventory();try{a.deepEqual(result.after,before);result.inventoryPreserved=true;}catch{result.inventoryPreserved=false;}
  result.passed=!!(result.validationComplete&&result.restorationPassed&&result.inventoryPreserved);save();console.log(JSON.stringify({run,passed:result.passed,webBuild:result.webBuild,sqlRun:result.sqlRun,restorationPassed:result.restorationPassed,inventoryPreserved:result.inventoryPreserved,evidence,failure:result.failure}));if(!result.passed)process.exitCode=1;
 }
}
require('./local-validation-lease.cjs').withLease(path.join(root,'.build-check'),'penta-start-recovery',work).catch(()=>{console.error('Recovery validation preflight/lease refused; no new authority.');process.exitCode=1;});
