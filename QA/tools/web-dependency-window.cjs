'use strict';
// Owner-approved frontend-only validation extension, exact restoration finally.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os'),a=require('node:assert/strict');
const{execFileSync,spawn}=require('node:child_process');
const root=path.resolve(__dirname,'../..'),expected=['pentaaimodels-api-1','pentaaimodels-inference-1'].sort(),sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function docker(args){return execFileSync('docker',args,{encoding:'utf8',windowsHide:true,timeout:30000,stdio:['ignore','pipe','pipe']}).trim();}
function lines(args){return docker(args).split(/\r?\n/).filter(Boolean).sort();}
function inventory(){return{containers:lines(['ps','-aq','--no-trunc']),volumes:lines(['volume','ls','-q']),networks:lines(['network','ls','-q','--no-trunc']),images:[...new Set(lines(['image','ls','-aq','--no-trunc']))].sort()};}
const witnessImage='sha256:add1449de3e17847da8baba6eddbb71c4d1289b7ec7ae7bec25fb37ce830fc2a';
function verifyWitness(row,run,name){
 a.equal(row.Id.length,64);a.equal(row.Name,'/'+name);a.equal(row.Image,witnessImage);a.equal(row.Config.Labels['academydesk.qa.run'],run);a.equal(row.Config.User,'101:101');a.equal(row.State.Running,true);a.equal(row.HostConfig.NetworkMode,'none');a.equal(row.HostConfig.ReadonlyRootfs,true);a.equal(row.HostConfig.Privileged,false);a.equal(row.HostConfig.Memory,16*1024**2);a.equal(row.HostConfig.NanoCpus,100000000);a.equal(row.HostConfig.PidsLimit,8);a.deepEqual(row.HostConfig.CapDrop,['ALL']);a.ok(row.HostConfig.SecurityOpt.includes('no-new-privileges'));a.equal(row.Mounts.length,0);a.deepEqual(row.Config.Entrypoint,['/bin/sleep']);a.deepEqual(row.Config.Cmd,['3000']);for(const bindings of Object.values(row.NetworkSettings.Ports||{}))a.ok(!bindings||bindings.length===0);return true;
}
async function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');const feedback=process.argv[2]==='--approved-finance-feedback-cache-stop-restore-23oct',runtime=process.argv[2]==='--approved-frontend-runtime-cache-stop-restore-23oct',resume=process.argv[2]==='--approved-frontend-resume-stop-restore-23oct',cache=feedback||runtime||process.argv[2]==='--approved-frontend-align-cache-stop-restore-23oct',align=!runtime&&!feedback&&(cache||process.argv[2]==='--approved-frontend-align-stop-restore-23oct');a.ok(resume||align?process.argv.length===4:process.argv.length===3&&(runtime||feedback||process.argv[2]==='--approved-frontend-stop-restore-23oct'));if(resume||align)a.match(process.argv[3],/^[a-f0-9]{32}$/);a.deepEqual(lines(['ps','--format','{{.Names}}']),expected);
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/web-dependency-window-'+run);fs.mkdirSync(evidence,{recursive:true});
 const services=expected.map(name=>{const row=JSON.parse(docker(['inspect',name]))[0];return{name,id:row.Id,image:row.Image};}),result={run,approval:'explicit owner23October bounded frontend install/build/browser window; stop only two original services, exact restore immediately even on failure',scope:align?'worktree-only verified dependency copy/alignment; no reinstall or rebuild':'isolated frontend validation',services,before:inventory(),passed:false,productionApproved:false,restartInvocations:0,steps:[]};
 const file=path.join(evidence,'result.json'),save=()=>fs.writeFileSync(file,JSON.stringify(result,null,2));let witnessCreated=false;const witnessName='academydesk-frontend-witness-'+run;save();
 async function child(name,tool,args,accepted=[0]){
  const stream=fs.createWriteStream(path.join(evidence,name+'.log'),{flags:'wx'}),p=spawn(process.execPath,[path.join(root,'QA/tools',tool),...args],{cwd:root,env:{...process.env,...(name.startsWith('browser-')?{QA_WEB_CANDIDATE:result.candidateRun}:{})},windowsHide:true,stdio:['ignore','pipe','pipe']});let output='';p.stdout.on('data',bytes=>{output+=bytes.toString();if(output.length>1024**2)output=output.slice(-(1024**2));});p.stdout.pipe(stream,{end:false});p.stderr.pipe(stream,{end:false});
  let timedOut=false;const timeout=setTimeout(()=>{if(p.exitCode===null){timedOut=true;execFileSync('taskkill.exe',['/PID',String(p.pid),'/T','/F'],{windowsHide:true,stdio:'ignore'});}},name.startsWith('browser-')?600000:1200000);
  const code=await new Promise((resolve,reject)=>{p.on('error',reject);p.on('close',resolve);}).finally(()=>clearTimeout(timeout));await new Promise(resolve=>stream.end(resolve));result.steps.push({name,exitCode:code,timedOut});save();a.equal(timedOut,false);a.ok(accepted.includes(code),'Frontend validation child failed; retain evidence.');return output;
 }
 try{
  console.log(JSON.stringify({run,evidence,phase:'stop-two-original-services'}));
  for(const service of services){a.equal(JSON.parse(docker(['inspect',service.id]))[0].Image,service.image);docker(['stop','--timeout','15',service.id]);}
  if(cache){
   result.cacheApproval='Owner23October asked to free safe RAM and continue; one clean-cache release after approved services stop; no restart/persistent setting/process kill';result.hostFreeBeforeCache=os.freemem();save();
   a.deepEqual(lines(['ps','--format','{{.Names}}']),[]);for(const service of services){const row=JSON.parse(docker(['inspect',service.id]))[0];a.equal(row.Image,service.image);a.equal(row.State.Running,false);}
   execFileSync('wsl',['-d','docker-desktop','-u','root','--','sync'],{windowsHide:true,timeout:30000,stdio:'ignore'});execFileSync('wsl',['-d','docker-desktop','-u','root','--','sysctl','-w','vm.drop_caches=3'],{windowsHide:true,timeout:30000,stdio:'ignore'});result.cleanCacheDropCompleted=true;save();
   for(let attempt=0;attempt<6&&os.freemem()<3*1024**3;attempt++)await sleep(5000);
  }
  result.admissionFreeBytes=os.freemem();save();a.ok(result.admissionFreeBytes>=3*1024**3,'Unchanged3GiB guard blocked; restoring without validation.');
  if(feedback){
   a.equal(JSON.parse(docker(['image','inspect',witnessImage]))[0].Id,witnessImage);docker(['create','--pull=never','--name',witnessName,'--label','academydesk.qa.run='+run,'--network','none','--read-only','--user','101:101','--memory','16m','--cpus','0.1','--pids-limit','8','--cap-drop','ALL','--security-opt','no-new-privileges','--entrypoint','/bin/sleep',witnessImage,'3000']);witnessCreated=true;docker(['start',witnessName]);verifyWitness(JSON.parse(docker(['inspect',witnessName]))[0],run,witnessName);result.ownedIdleWitness={image:witnessImage,network:'none',memoryBytes:16*1024**2,purpose:'Keep an owned bounded container running during the approved frontend window; no claim the older bridge drift cause is proven.'};save();
   result.candidateRun='293e5beba439453794e6c1e68adc2b71';result.feedbackRuns=[];save();
   for(const[tool,count,prefix]of[['expenses-feedback-browser.cjs',54,'expenses-feedback-browser-'],['invoice-payment-safety-browser.cjs',84,'invoice-payment-safety-'],['payroll-recovery-browser.cjs',96,'payroll-recovery-'],['finance-governance-recovery-browser.cjs',144,'finance-governance-recovery-']]){
    require('./default-web-build.cjs').memoryGuard();const verified=require('./api-image-payment-browser.cjs').verifyExport(result.candidateRun);a.equal(verified.verifiedExportFiles,627);console.log(JSON.stringify({run,phase:'browser-'+tool,expected:count}));const output=await child('browser-'+tool,tool,[]),summary=JSON.parse(output.trim().split(/\r?\n/).at(-1));a.equal(summary.passed,count);a.equal(path.dirname(summary.evidence),path.join(root,'QA/EVIDENCE'));a.ok(path.basename(summary.evidence).startsWith(prefix));a.match(path.basename(summary.evidence).slice(prefix.length),/^\d+$/);const receipt=fs.readFileSync(path.join(summary.evidence,'result.json')),proof=JSON.parse(receipt);a.equal(proof.checks.length,count);require('./api-image-payment-browser.cjs').verifyExport(result.candidateRun);result.feedbackRuns.push({tool,passed:count,evidence:path.relative(root,summary.evidence).replaceAll('\\','/'),receiptSha256:crypto.createHash('sha256').update(receipt).digest('hex'),exportRelative:verified.exportRelative,synthetic:true,nativeSql:false});save();
   }
   a.equal(result.feedbackRuns.reduce((sum,row)=>sum+row.passed,0),378);result.validationComplete=true;
  }else if(runtime){
   const output=await child('static-runtime','web-container-runtime.cjs',['--approved-originals-stopped']),summary=JSON.parse(output.trim().split(/\r?\n/).at(-1));a.equal(summary.passed,true);a.equal(summary.ownedResourcesRemoved,true);a.equal(summary.inventoryPreserved,true);a.equal(summary.originalServicesUnchanged,true);result.staticRuntimeRun=summary.run;result.validationComplete=true;
  }else if(align){
   result.candidateRun=process.argv[3];save();const output=await child('alignment','web-dependency-align.cjs',['--align',process.argv[3]]),summary=JSON.parse(output.trim().split(/\r?\n/).at(-1));a.equal(summary.passed,true);a.equal(summary.mainAndApplicationUnchanged,true);result.alignmentRun=summary.run;result.validationComplete=true;
  }else{
  const prepared=resume?{run:process.argv[3],stage:require('./web-dependency-candidate.cjs').open(process.argv[3]).stage}:JSON.parse((await child('prepare','web-dependency-candidate.cjs',['--prepare'])).trim().split(/\r?\n/).at(-1));a.match(prepared.run,/^[a-f0-9]{32}$/);result.candidateRun=prepared.run;save();console.log(JSON.stringify({run,candidateRun:prepared.run,app:path.join(prepared.stage,'app'),phase:'await-reviewed-manifest-patch',deadlineSeconds:90}));
  const helper=require('./web-dependency-candidate.cjs'),deadline=Date.now()+90000;let patched=false;
  while(Date.now()<deadline){try{const context=helper.open(prepared.run);helper.validateManifest(context.result.originalManifest,JSON.parse(fs.readFileSync(path.join(context.app,'package.json'))));patched=true;break;}catch{await sleep(500);}}
  a.ok(patched,'Reviewed manifest patch missing; restore rather than install unreviewed inputs.');
  for(const mode of resume?['--validate']:['--resolve','--install','--validate']){console.log(JSON.stringify({run,candidateRun:prepared.run,phase:mode}));await child(mode.slice(2),'web-dependency-candidate.cjs',[mode,prepared.run]);}
  console.log(JSON.stringify({run,candidateRun:prepared.run,phase:'candidate-advisories'}));
  const auditText=await child('advisories','web-package-advisories.cjs',['--candidate',prepared.run],[0,1]),audit=JSON.parse(auditText.trim().split(/\r?\n/).at(-1));a.equal(audit.completed,true);result.advisoryRun=audit.run;result.remainingAdvisoryMatches=audit.findingCount;
  result.browserRuns=[];
  for(const tool of['login-challenge-browser.cjs','session-resume-browser.cjs','admin-manual-foundation-browser.cjs','teacher-manual-foundation-browser.cjs','student-manual-foundation-browser.cjs']){
   a.ok(os.freemem()>=3*1024**3,'Unchanged3GiB guard blocked browser.');console.log(JSON.stringify({run,candidateRun:prepared.run,phase:'browser-'+tool}));const output=await child('browser-'+tool,tool,[]),summary=JSON.parse(output.trim().split(/\r?\n/).at(-1));a.ok(summary.passed>0);result.browserRuns.push({tool,...summary});save();
  }
  result.validationComplete=true;
  }
 }catch(error){result.failure=/^(Unchanged3GiB|Reviewed manifest)/.test(error.message)?error.message:'Frontend validation failed; values withheld, inspect owned logs.';}
 finally{
  if(witnessCreated){try{const row=JSON.parse(docker(['inspect',witnessName]))[0];a.equal(row.Name,'/'+witnessName);a.equal(row.Image,witnessImage);a.equal(row.Config.Labels['academydesk.qa.run'],run);try{verifyWitness(row,run,witnessName);result.witnessEndVerified=true;}catch{result.witnessEndVerified=false;}docker(['rm','-f',witnessName]);result.ownedWitnessRemoved=true;}catch{result.ownedWitnessRemoved=false;}}
  console.log(JSON.stringify({run,phase:'restore-exact-original-services'}));result.restorationErrors=[];
  for(const service of [...services].reverse()){try{const row=JSON.parse(docker(['inspect',service.id]))[0];a.equal(row.Image,service.image);if(!row.State.Running)docker(['start',service.id]);}catch{result.restorationErrors.push(service.name);}}
  const deadline=Date.now()+120000;
  do{result.restored=services.map(service=>{const row=JSON.parse(docker(['inspect',service.id]))[0];return{name:service.name,id:row.Id,image:row.Image,running:row.State.Running,health:row.State.Health?.Status||null};});if(result.restored.every(row=>row.running&&(!row.health||row.health==='healthy')))break;await sleep(2000);}while(Date.now()<deadline);
  result.restorationPassed=result.restorationErrors.length===0&&result.restored.every(row=>row.running&&(!row.health||row.health==='healthy'));
  result.after=inventory();try{a.deepEqual(result.after,result.before);a.deepEqual(lines(['ps','--format','{{.Names}}']),expected);result.inventoryPreserved=true;}catch{result.inventoryPreserved=false;}
  result.passed=!!(result.validationComplete&&result.restorationPassed&&result.inventoryPreserved&&(!witnessCreated||(result.ownedWitnessRemoved&&result.witnessEndVerified)));save();console.log(JSON.stringify({run,candidateRun:result.candidateRun,passed:result.passed,remainingAdvisoryMatches:result.remainingAdvisoryMatches,restorationPassed:result.restorationPassed,inventoryPreserved:result.inventoryPreserved,evidence}));if(!result.passed)process.exitCode=1;
 }
}
module.exports={verifyWitness,witnessImage};if(require.main===module)require('./local-validation-lease.cjs').withLease(path.join(root,'.build-check'),'frontend',run).catch(()=>{console.error('Frontend window preflight failed; no authority expansion.');process.exitCode=1;});
