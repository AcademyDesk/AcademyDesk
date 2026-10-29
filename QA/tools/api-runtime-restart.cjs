'use strict';
// One explicitly approved restart, preserving exact inventories and prior services.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os');
const{execFileSync,spawn}=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
function docker(args){return execFileSync('docker',args,{windowsHide:true,encoding:'utf8',timeout:15000,stdio:['ignore','pipe','pipe']}).trim();}
function inventory(){return{containers:docker(['ps','-aq','--no-trunc']).split(/\r?\n/).filter(Boolean).sort(),volumes:docker(['volume','ls','-q']).split(/\r?\n/).filter(Boolean).sort(),images:docker(['image','ls','-aq','--no-trunc']).split(/\r?\n/).filter(Boolean).filter((id,i,all)=>all.indexOf(id)===i).sort()};}
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
async function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.deepEqual(process.argv.slice(2),['--approved-once-22oct']);
 const expected=['pentaaimodels-api-1','pentaaimodels-inference-1'].sort();
 const names=docker(['ps','--format','{{.Names}}']).split(/\r?\n/).filter(Boolean).sort();a.deepEqual(names,expected,'Unexpected running services: stop for review.');
 const before=inventory(),services=names.map(name=>{const row=JSON.parse(docker(['inspect',name]))[0];return{name,id:row.Id,image:row.Image,health:row.State.Health?.Status||null};});
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/api-runtime-restart-'+run);fs.mkdirSync(evidence,{recursive:true});
 const result={run,approval:'explicit one-time restart and guarded API runtime retest',before,services,passed:false,restartInvocations:0,productionApproved:false};
 const save=()=>fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));save();
 try{
  result.restartInvocations=1;save();console.log(JSON.stringify({run,evidence,restartInvocations:1}));
  const child=spawn('docker',['desktop','restart','--timeout','180'],{windowsHide:true,stdio:['ignore','pipe','pipe']});
  const log=fs.createWriteStream(path.join(evidence,'restart.log'),{flags:'wx'});child.stdout.pipe(log,{end:false});child.stderr.pipe(log,{end:false});
  result.restartExit=await new Promise((resolve,reject)=>{child.on('error',reject);child.on('close',resolve);});await new Promise(resolve=>log.end(resolve));
  const deadline=Date.now()+180000;let ready=false;
  while(Date.now()<deadline){try{docker(['info','--format','{{.OSType}}']);ready=true;break;}catch{await sleep(2000);}}
  a.ok(ready,'Docker did not recover; no second restart permitted.');
  for(const service of services){const row=JSON.parse(docker(['inspect',service.id]))[0];a.equal(row.Image,service.image);if(!row.State.Running)docker(['start',service.id]);}
  const healthyBy=Date.now()+90000;let states;
  while(Date.now()<healthyBy){states=services.map(service=>{const row=JSON.parse(docker(['inspect',service.id]))[0];return{name:service.name,id:row.Id,image:row.Image,running:row.State.Running,health:row.State.Health?.Status||null};});if(states.every(row=>row.running&&(!row.health||row.health==='healthy')))break;await sleep(2000);}
  a.ok(states.every(row=>row.running&&(!row.health||row.health==='healthy')),'Original services not restored healthy.');
  result.after=inventory();a.deepEqual(result.after,before,'Docker inventory changed during restart.');a.deepEqual(docker(['ps','--format','{{.Names}}']).split(/\r?\n/).filter(Boolean).sort(),expected);
  Object.assign(result,{passed:true,restored:states,freeMemoryBytes:os.freemem()});
 }catch(error){result.failure=String(error.message).slice(0,500);throw error;}
 finally{save();console.log(JSON.stringify({run,passed:result.passed,freeMemoryGiB:result.freeMemoryBytes/1024**3,evidence}));}
}
run().catch(error=>{console.error(error.message);process.exitCode=1;});
