'use strict';
// Owner23Oct:free safe RAM and continue. One clean-cache release in the exact
// docker-desktop distro. No process termination, restart, persistent setting,
// driver/model change, container/volume/image deletion or host memory guard change.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os'),a=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const root=path.resolve(__dirname,'../..'),expected=['pentaaimodels-api-1','pentaaimodels-inference-1'].sort();
const call=(cmd,args)=>execFileSync(cmd,args,{encoding:'utf8',windowsHide:true,timeout:30000,stdio:['ignore','pipe','pipe']}).trim();
function lines(args){return call('docker',args).split(/\r?\n/).filter(Boolean).sort();}
function inventory(){return{containers:lines(['ps','-aq','--no-trunc']),volumes:lines(['volume','ls','-q']),networks:lines(['network','ls','-q','--no-trunc']),images:[...new Set(lines(['image','ls','-aq','--no-trunc']))].sort()};}
function meminfo(){const text=call('wsl',['-d','docker-desktop','-u','root','--','cat','/proc/meminfo']);return Object.fromEntries(text.split(/\r?\n/).map(line=>/^(MemTotal|MemFree|MemAvailable|Buffers|Cached|Dirty|Writeback|SReclaimable|Mlocked):\s+(\d+) kB$/.exec(line)).filter(Boolean).map(row=>[row[1],Number(row[2])*1024]));}
function services(){return JSON.parse(call('docker',['inspect',...expected])).map(row=>({name:row.Name.slice(1),id:row.Id,image:row.Image,running:row.State.Running,health:row.State.Health?.Status||null}));}
async function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.deepEqual(process.argv.slice(2),['--owner-approved-clean-cache-23oct']);a.deepEqual(lines(['ps','--format','{{.Names}}']),expected);const before=inventory(),original=services();a.ok(original.every(row=>row.running&&(!row.health||row.health==='healthy')));
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/dev-cache-reclaim-'+run);fs.mkdirSync(evidence,{recursive:true});const result={run,approval:'Owner explicitly requested freeing as much safe RAM as possible and continuing on23October',hostFreeBefore:os.freemem(),guestBefore:meminfo(),before,original,passed:false,restartInvocations:0,processTerminations:0,persistentSettingsChanged:false,productionApproved:false};
 try{call('wsl',['-d','docker-desktop','-u','root','--','sync']);result.syncCompleted=true;call('wsl',['-d','docker-desktop','-u','root','--','sysctl','-w','vm.drop_caches=3']);result.cleanCacheDropCompleted=true;result.guestAfter=meminfo();result.samples=[{atUtc:new Date().toISOString(),freeBytes:os.freemem()}];
  // Allow the existing VM memory-reclamation mechanism a bounded opportunity;
  // do not enable/change it or imply guest-free memory equals host-free memory.
  for(let i=0;i<6&&os.freemem()<3*1024**3;i++){await new Promise(resolve=>setTimeout(resolve,5000));result.samples.push({atUtc:new Date().toISOString(),freeBytes:os.freemem()});}
  result.after=inventory();a.deepEqual(result.after,before);result.restored=services();a.deepEqual(result.restored,original);result.inventoryPreserved=true;result.servicesUnchanged=true;result.hostFreeAfter=os.freemem();result.hostGuardPass=result.hostFreeAfter>=3*1024**3;result.passed=true;
 }catch{result.failure='Clean-cache reclamation incomplete; no clean/inventory/guard success inferred.';process.exitCode=1;}
 finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({run,passed:result.passed,hostFreeBeforeGiB:result.hostFreeBefore/1024**3,hostFreeAfterGiB:result.hostFreeAfter===undefined?null:result.hostFreeAfter/1024**3,hostGuardPass:result.hostGuardPass,guestCacheBefore:result.guestBefore.Cached,guestCacheAfter:result.guestAfter?.Cached,servicesUnchanged:result.servicesUnchanged,inventoryPreserved:result.inventoryPreserved,evidence}));}
}
if(require.main===module)run().catch(()=>{console.error('Cache-reclamation preflight failed; no restart/settings/process or container change permitted.');process.exitCode=1;});
