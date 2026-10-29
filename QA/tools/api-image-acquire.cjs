'use strict';
// Explicitly approved22Oct local dependency packet; no general registry updater.
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),crypto=require('node:crypto');
const{spawn,execFileSync}=require('node:child_process');const a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const approved=[
 {reference:'mcr.microsoft.com/dotnet/sdk:10.0',digest:'sha256:747cbade5bfd918a175ac32f8f1d931edec2c7d0d5397a3de72eaef1ac1f66d2'},
 {reference:'mcr.microsoft.com/dotnet/aspnet:10.0',digest:'sha256:8af87eba5e7cdafb1514d7388be287365da97c132509e84dfe252ffd552398ca'},
];
const digestReference=item=>item.reference.replace(/:[^/:]+$/,'')+'@'+item.digest;
function inspect(reference){try{return JSON.parse(execFileSync('docker',['image','inspect',reference],{windowsHide:true,encoding:'utf8',stdio:['ignore','pipe','ignore']}))[0];}catch{return null;}}
async function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.equal(process.argv[2],'--approved-22oct');a.equal(process.argv.length,3);
 a.ok(os.freemem()>=3*1024**3,'3GiB guard: acquisition not started.');
 const settings=JSON.parse(fs.readFileSync('C:/Users/Admin/AppData/Roaming/Docker/settings-store.json','utf8'));
 a.equal(settings.CustomWslDistroDir?.replaceAll('\\','/').toLowerCase(),'d:/dockerdesktopdata/dockerdesktopwsl');
 const disk='D:/DockerDesktopData/DockerDesktopWSL/disk/docker_data.vhdx';a.ok(fs.statSync(disk).isFile());
 const space=fs.statfsSync('D:/');a.ok(Number(space.bavail)*Number(space.bsize)>10*1024**3,'10GiB D: disk guard.');
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/api-base-acquisition-'+run);fs.mkdirSync(evidence,{recursive:true});const checks=[];
 for(const item of approved){
  const pinned=digestReference(item),prior=inspect(item.reference);
  a.ok(!prior||prior.RepoDigests?.includes(pinned),'Existing local base tag differs; no overwrite allowed.');
  const log=fs.createWriteStream(path.join(evidence,item.reference.includes('/sdk:')?'sdk.log':'runtime.log'),{flags:'wx'});
  const child=spawn('docker',['pull','--platform','linux/amd64',pinned],{windowsHide:true,stdio:['ignore','pipe','pipe']});child.stdout.pipe(log,{end:false});child.stderr.pipe(log,{end:false});
  let timedOut=false;const timer=setTimeout(()=>{if(child.exitCode===null&&child.signalCode===null){timedOut=true;child.kill();}},180000);
  const code=await new Promise((resolve,reject)=>{child.on('error',reject);child.on('close',resolve);}).finally(()=>clearTimeout(timer));await new Promise(resolve=>log.end(resolve));
  a.equal(code,0,'Pinned public base pull failed; inspect local acquisition log.');a.equal(timedOut,false);
  const result=inspect(pinned);a.ok(result&&result.RepoDigests?.includes(pinned));a.equal(result.Os,'linux');a.equal(result.Architecture,'amd64');
  if(!prior)execFileSync('docker',['tag',pinned,item.reference],{windowsHide:true,stdio:'ignore'});
  a.equal(inspect(item.reference).Id,result.Id);
  checks.push({reference:item.reference,digest:item.digest,imageId:result.Id,size:result.Size,platform:'linux/amd64',tagCreated:!prior});
  console.log(JSON.stringify(checks.at(-1)));
 }
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({run,passed:true,checks,storage:disk,productionApproved:false,scope:'two approved public base images only; no app build/model/Azure/push'},null,2));console.log(JSON.stringify({run,evidence,passed:checks.length}));
}
module.exports={digestReference};if(require.main===module)run().catch(error=>{console.error(error.message);process.exitCode=1;});
