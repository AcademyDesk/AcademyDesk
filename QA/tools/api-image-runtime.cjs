'use strict';
// New candidate only: isolated Production boot and no-database fail-closed smoke.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os');
const{execFileSync}=require('node:child_process');const a=require('node:assert/strict');const{inputs}=require('./api-image-build.cjs');
const root=path.resolve(__dirname,'../..');
function status(raw){const match=/^HTTP\/1\.[01] (\d{3})\b/.exec(raw);a.ok(match,'Expected actual HTTP status line.');return Number(match[1]);}
function patchedPkcs(productVersion){const match=/^10\.0\.(\d+)(?:\+[0-9a-f]+)?$/.exec(productVersion);return !!match&&Number(match[1])>=13;}
function docker(args){return execFileSync('docker',args,{windowsHide:true,encoding:'utf8',timeout:15000,stdio:['ignore','pipe','pipe']});}
function exchange(name,url,headers=''){
 a.match(url,/^\/[A-Za-z0-9_./-]+$/);a.ok(headers===''||headers==='Authorization: Bearer synthetic-invalid\r\n');
 const request=`GET ${url} HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n${headers}\r\n`;
 const encoded=Buffer.from(request).toString('base64');
 return docker(['exec',name,'/bin/bash','-c',`exec 3<>/dev/tcp/127.0.0.1/8080; printf '%s' '${encoded}' | base64 -d >&3; cat <&3`]);
}
async function run(){
 const build=process.argv[2];a.match(build,/^[0-9a-f]{32}$/);a.equal(process.argv.length,3);
 a.ok(os.freemem()>=3*1024**3,'3GiB guard: runtime not started.');
 const receipt=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-'+build,'result.json'),'utf8'));
 a.equal(receipt.passed,true);a.equal(receipt.exitCode,0);a.deepEqual(inputs(),receipt.sourceFiles,'Current API source differs from candidate.');a.match(receipt.imageId,/^sha256:[0-9a-f]{64}$/);
 const image=JSON.parse(docker(['image','inspect',receipt.imageId]))[0];a.equal(image.Config.Labels['academydesk.qa.run'],build);
 const run=crypto.randomUUID().replaceAll('-',''),name='academydesk-image-runtime-'+run,evidence=path.join(root,'QA/EVIDENCE/api-image-runtime-'+run);fs.mkdirSync(evidence,{recursive:true});let started=false;
 const result={run,build,imageId:receipt.imageId,passed:false,checks:[],productionApproved:false,authenticatedSqlAcceptance:false,loadedPkcsAcceptance:false};
 try{
  docker(['run','--detach','--pull=never','--name',name,'--label','academydesk.qa.run='+run,'--network','none','--read-only','--memory','512m','--cpus','1','--pids-limit','128','--cap-drop','ALL','--security-opt','no-new-privileges',
   '--tmpfs','/tmp:rw,noexec,nosuid,size=33554432','--tmpfs','/app/wwwroot:rw,noexec,nosuid,size=1048576','--tmpfs','/root/.aspnet/DataProtection-Keys:rw,noexec,nosuid,size=1048576',
   '-e','ASPNETCORE_ENVIRONMENT=Production','-e','Database__ApplyMigrationsOnStartup=false','-e','Bootstrap__PlatformOwnerEmail=','-e','Bootstrap__PlatformOwnerPassword=','-e','MediaStorage__Enabled=false',
   '-e','ConnectionStrings__DefaultConnection=Server=127.0.0.1,1;Database=AcademyDesk_QA_Unavailable;Integrated Security=true;Encrypt=false;Connect Timeout=1;Connect Retry Count=0',receipt.imageId]);started=true;
  const readyBy=Date.now()+60000;let health;
  while(Date.now()<readyBy){try{health=exchange(name,'/health');break;}catch{await new Promise(resolve=>setTimeout(resolve,500));}}
  a.equal(status(health||''),503,'No database must not report healthy.');const checks=result.checks;checks.push({path:'/health',status:503,scope:'startup with deliberately unreachable local database'});
  for(const url of['/api/auth/session','/api/academies','/api/academies/11111111-1111-4111-8111-111111111111/payments','/api/class-media/11111111-1111-4111-8111-111111111111/content','/api/academies/11111111-1111-4111-8111-111111111111/penta/chat/health']){
   a.equal(status(exchange(name,url)),401,'Anonymous denied '+url);checks.push({path:url,status:401});
  }
  a.equal(status(exchange(name,'/api/auth/session','Authorization: Bearer synthetic-invalid\r\n')),401);checks.push({path:'/api/auth/session',invalidBearer:true,status:401});
  a.equal(status(exchange(name,'/openapi/v1.json')),404);checks.push({path:'/openapi/v1.json',status:404,scope:'Production schema disabled'});
  const began=Date.now();a.equal(status(exchange(name,'/health')),503);const healthMilliseconds=Date.now()-began;a.ok(healthMilliseconds<7000,'Unavailable DB health within5s application bound plus2s CLI overhead');checks.push({path:'/health',status:503,healthMilliseconds});
  const state=JSON.parse(docker(['inspect',name]))[0];a.equal(state.State.Running,true);a.equal(state.State.OOMKilled,false);a.equal(state.HostConfig.NetworkMode,'none');a.equal(state.HostConfig.ReadonlyRootfs,true);a.equal(state.Mounts.filter(m=>m.Type==='bind'||m.Type==='volume').length,0);
  result.versions=docker(['exec',name,'dotnet','--list-runtimes']).trim().split(/\r?\n/);
  const deps=JSON.parse(docker(['exec',name,'cat','/app/AcademyDesk.Api.deps.json']));result.pkcsLibraries=Object.keys(deps.libraries).filter(key=>key.startsWith('System.Security.Cryptography.Pkcs/'));
  // Publish legitimately omits an equal/newer shared-framework asset from app.deps.
  // Inspect every shipped PKCS binary rather than inferring a patch from package presence.
  const binaries=docker(['exec',name,'find','/app','/usr/share/dotnet/shared','-name','System.Security.Cryptography.Pkcs.dll','-type','f']).trim().split(/\r?\n/).filter(Boolean);
  a.ok(binaries.length>0&&binaries.length<=4,'Review unexpected PKCS image layout.');result.shippedPkcs=[];
  for(const [index,binary] of binaries.entries()){
   a.match(binary,/^(?:\/app|\/usr\/share\/dotnet\/shared\/Microsoft\.(?:AspNetCore|NETCore)\.App\/10\.0\.\d+)\/System\.Security\.Cryptography\.Pkcs\.dll$/);
   const local=path.join(evidence,'pkcs-'+index+'.dll');docker(['cp',name+':'+binary,local]);
   const command="[System.Diagnostics.FileVersionInfo]::GetVersionInfo('"+local.replaceAll("'","''")+"').ProductVersion";
   const productVersion=execFileSync('powershell.exe',['-NoProfile','-NonInteractive','-EncodedCommand',Buffer.from(command,'utf16le').toString('base64')],{windowsHide:true,encoding:'utf8',timeout:15000}).trim();
   result.shippedPkcs.push({path:binary,productVersion,sha256:crypto.createHash('sha256').update(fs.readFileSync(local)).digest('hex')});
   a.ok(patchedPkcs(productVersion),'Shipped PKCS must include net10.0.13 security fixes.');
  }
  Object.assign(result,{passed:true,network:'none',readOnlyRoot:true,persistentMounts:0});
 }catch(error){result.failure=String(error.message).slice(0,500);throw error;}
 finally{
  try{if(started){const state=JSON.parse(docker(['inspect',name]))[0];a.equal(state.Config.Labels['academydesk.qa.run'],run);a.equal(state.Image,receipt.imageId);docker(['stop','--timeout','5',name]);docker(['rm',name]);result.ownedContainerRemoved=true;console.log('Exact owned runtime removed='+name+';no images/volumes/other containers removed.');}}
  catch(error){result.passed=false;result.cleanupFailed=true;throw error;}
  finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({run,build,passed:result.passed,checks:result.checks.length,versions:result.versions,evidence}));}
 }
}
module.exports={status,patchedPkcs};if(require.main===module)run().catch(error=>{console.error(error.message);process.exitCode=1;});
