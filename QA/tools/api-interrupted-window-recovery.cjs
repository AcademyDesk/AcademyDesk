'use strict';
// Resume cleanup only for the exact interrupted window; never resume its tests.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const root=path.resolve(__dirname,'../..'),windowRun='a326e2dd9336442fa32e4fb1d83ed1a1',sqlRun='1469cc724ac941baab4ee8696197ba81';
const docker=args=>execFileSync('docker',args,{encoding:'utf8',windowsHide:true,timeout:30000,stdio:['ignore','pipe','pipe']}).trim();
const inspect=name=>JSON.parse(docker(['inspect',name]))[0],sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function inventory(){return{containers:docker(['ps','-aq','--no-trunc']).split(/\r?\n/).filter(Boolean).sort(),volumes:docker(['volume','ls','-q']).split(/\r?\n/).filter(Boolean).sort(),networks:docker(['network','ls','-q','--no-trunc']).split(/\r?\n/).filter(Boolean).sort(),images:[...new Set(docker(['image','ls','-aq','--no-trunc']).split(/\r?\n/).filter(Boolean))].sort()};}
async function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.deepEqual(process.argv.slice(2),['--recover-exact-interrupted-window']);
 const windowBytes=fs.readFileSync(path.join(root,'QA/EVIDENCE/api-runtime-window-'+windowRun+'/result.json')),sqlBytes=fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-sql-'+sqlRun+'/result.json')),window=JSON.parse(windowBytes),sql=JSON.parse(sqlBytes);
 a.equal(window.run,windowRun);a.equal(sql.run,sqlRun);a.equal(window.stopStarted,true);a.equal(window.passed,false);a.equal(sql.passed,false);a.equal(window.restored,undefined);a.equal(sql.ownedResourcesRemoved,undefined);
 a.deepEqual(docker(['ps','--format','{{.Names}}']).split(/\r?\n/).filter(Boolean),[]);
 const prefix='academydesk-image-sql-'+sqlRun,targets=[{name:prefix+'-api',image:sql.imageId},{name:prefix+'-db',image:sql.sqlImageId}];
 const states=targets.map(target=>{const row=inspect(target.name);a.equal(row.Config.Labels['academydesk.qa.run'],sqlRun);a.equal(row.Image,target.image);a.equal(row.State.Running,false);a.equal(window.before.containers.includes(row.Id),false);return{name:target.name,id:row.Id,image:row.Image,exitCode:row.State.ExitCode,oom:row.State.OOMKilled,finishedAt:row.State.FinishedAt};});
 for(const service of window.services){const row=inspect(service.id);a.equal(row.Id,service.id);a.equal(row.Image,service.image);a.equal(row.Name,'/'+service.name);}
 const network=JSON.parse(docker(['network','inspect',prefix]))[0];a.equal(network.Labels['academydesk.qa.run'],sqlRun);a.equal(Object.keys(network.Containers||{}).length,0);a.equal(window.before.networks.includes(network.Id),false);
 const id=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/interrupted-window-recovery-'+id);fs.mkdirSync(evidence,{recursive:true});
 const result={run:id,interruptedWindow:windowRun,interruptedSql:sqlRun,originalWindowSha256:crypto.createHash('sha256').update(windowBytes).digest('hex'),originalSqlSha256:crypto.createHash('sha256').update(sqlBytes).digest('hex'),observedOwnedStates:states,interruptedTestAccepted:false,passed:false,productionApproved:false,removed:[],restored:[]};
 const save=()=>fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));save();
 try{for(const target of states){const row=inspect(target.id);a.equal(row.State.Running,false);a.equal(row.Config.Labels['academydesk.qa.run'],sqlRun);docker(['rm',target.id]);result.removed.push(target.name);}docker(['network','rm',network.Id]);result.removed.push(prefix);}
 catch{result.cleanupFailed=true;}
 finally{
  for(const service of [...window.services].reverse()){const row=inspect(service.id);a.equal(row.Image,service.image);if(!row.State.Running)docker(['start',service.id]);}
  const deadline=Date.now()+120000;while(Date.now()<deadline){result.restored=window.services.map(service=>{const row=inspect(service.id);return{name:service.name,id:row.Id,image:row.Image,running:row.State.Running,health:row.State.Health?.Status||null};});if(result.restored.every(row=>row.running&&(!row.health||row.health==='healthy')))break;await sleep(2000);}
  result.after=inventory();result.inventoryPreserved=JSON.stringify(result.after)===JSON.stringify(window.before);result.restorationPassed=result.restored.every(row=>row.running&&(!row.health||row.health==='healthy'));result.originalReceiptsUnchanged=fs.readFileSync(path.join(root,'QA/EVIDENCE/api-runtime-window-'+windowRun+'/result.json')).equals(windowBytes)&&fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-sql-'+sqlRun+'/result.json')).equals(sqlBytes);
  result.passed=!result.cleanupFailed&&result.inventoryPreserved&&result.restorationPassed&&result.originalReceiptsUnchanged;save();console.log(JSON.stringify({run:id,passed:result.passed,inventoryPreserved:result.inventoryPreserved,restorationPassed:result.restorationPassed,interruptedTestAccepted:false,evidence}));if(!result.passed)process.exitCode=1;
 }
}
run().catch(()=>{console.error('Exact interrupted-window recovery failed; inspect bounded metadata, no values emitted.');process.exitCode=1;});
