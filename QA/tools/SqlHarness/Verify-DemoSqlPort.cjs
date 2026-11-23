'use strict';
// Real Docker socket/publication regression, NOT SQL or model acceptance.
const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process'),crypto=require('node:crypto'),net=require('node:net'),os=require('node:os'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../../..'),image=process.argv[2],output=process.argv[3];
a.match(image||'',/^sha256:[a-f0-9]{64}$/,'Pass an existing cached Python runtime image SHA; no pulls.');
const docker=args=>cp.execFileSync('docker',args,{encoding:'utf8',windowsHide:true,timeout:30000}).trim();
const before=docker(['ps','-aq','--no-trunc']).split(/\r?\n/).filter(Boolean).sort();
const imageBefore=JSON.parse(docker(['image','inspect',image]))[0];a.equal(imageBefore.Id,image);
a.ok(os.freemem()>=1024**3,'1GiB admission for this 64MiB socket-only test; no SQL or inference');
function publish(interactive){return cp.execFileSync('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(__dirname,'DemoSqlPort.ps1'),...(interactive?['-InteractiveDemo']:[])],{encoding:'utf8',windowsHide:true}).trim();}
const result={run:crypto.randomUUID().replaceAll('-',''),image,checks:[],sqlEngine:false,inference:false,newDownloads:false};
const check=(condition,name)=>{a.ok(condition,name);result.checks.push(name);};
const name='academydesk-qa-port-'+result.run;let id;
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
async function socket(port){return new Promise((resolve,reject)=>{const s=net.createConnection({host:'127.0.0.1',port});s.setTimeout(3000);s.once('data',data=>{s.destroy();resolve(data.toString());});s.once('error',reject);s.once('timeout',()=>{s.destroy();reject(Error('Owned test socket timeout'));});});}
async function ready(port){const until=Date.now()+15000;do{try{if(await socket(port)==='OK')return;}catch{}await sleep(200);}while(Date.now()<until);throw Error('Owned test socket did not become ready');}
async function main(){try{
 check(publish(false)==='127.0.0.1::1433','ordinary QA dynamic binding retained');
 const fixed=publish(true);a.match(fixed,/^127\.0\.0\.1:[1-9][0-9]{3,4}:1433$/);const port=Number(fixed.split(':')[1]);check(port>1024&&port<65536,'demo gets explicit loopback-only port');
 const script="import socket\ns=socket.socket();s.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1);s.bind(('0.0.0.0',1433));s.listen()\nwhile True:\n c,_=s.accept();c.sendall(b'OK');c.close()\n";
 id=docker(['run','-d','--pull=never','--name',name,'--label','academydesk.qa.port-run='+result.run,'--read-only','--memory','64m','--memory-swap','64m','--cpus','1','--pids-limit','32','--cap-drop','ALL','--security-opt','no-new-privileges','--publish',fixed,image,'python','-B','-c',script]);
 const first=JSON.parse(docker(['inspect',id]))[0];check(first.HostConfig.PortBindings['1433/tcp'][0].HostPort===String(port),'Docker stores the explicit port');check(first.NetworkSettings.Ports['1433/tcp'][0].HostIp==='127.0.0.1','published only on loopback');
 await ready(port);check(true,'owned socket reachable before pause');
 docker(['stop','--timeout','5',id]);docker(['start',id]);
 const restored=JSON.parse(docker(['inspect',id]))[0];check(restored.Id===first.Id&&restored.Image===first.Image,'same container and image after restart');check(restored.NetworkSettings.Ports['1433/tcp'][0].HostPort===String(port),'published port unchanged after restart');await ready(port);check(true,'same address reachable after restart');
 result.validated=true;
}catch(e){result.failure=e.message;}
finally{
 const ids=docker(['ps','-aq','--no-trunc','--filter','name=^/'+name+'$','--filter','label=academydesk.qa.port-run='+result.run]);
 if(ids){const c=JSON.parse(docker(['inspect',ids]))[0];a.equal(c.Name,'/'+name);a.equal(c.Config.Labels['academydesk.qa.port-run'],result.run);a.equal(c.Mounts.length,0);if(c.State.Running)docker(['stop','--timeout','5',c.Id]);docker(['rm',c.Id]);}
 const after=docker(['ps','-aq','--no-trunc']).split(/\r?\n/).filter(Boolean).sort();a.deepEqual(after,before,'Existing Docker containers preserved');a.equal(JSON.parse(docker(['image','inspect',image]))[0].Id,image);
 result.resourcesPreserved=true;result.passed=!!result.validated;result.finishedAt=new Date().toISOString();
 if(output){const file=path.resolve(root,output);a.ok(file.startsWith(path.join(root,'.build-check')+path.sep),'Evidence output must stay in .build-check');fs.writeFileSync(file,JSON.stringify(result,null,2),{flag:'wx'});}
 console.log(JSON.stringify(result));if(!result.passed)process.exitCode=1;
}}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
