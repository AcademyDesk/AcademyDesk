'use strict';
// Read-only inspection of the successful candidate; the container is never started.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const{execFileSync}=require('node:child_process'),a=require('node:assert/strict');
const{inputs,configurationRisk,parseConfiguration,inlineRisk}=require('./api-image-build.cjs');
const root=path.resolve(__dirname,'../..');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
function manifestRisk(value){
 // Dependency names such as Microsoft.IdentityModel.Tokens.Jwt are metadata,
 // not credential keys. Only exempt validated version-valued dependency maps.
 const copy=structuredClone(value);
 for(const target of Object.values(copy.targets||{}))for(const library of Object.values(target)){
  if(!library.dependencies)continue;
  const versions=Object.values(library.dependencies);
  if(!versions.every(version=>typeof version==='string'&&/^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$/.test(version)))return true;
  delete library.dependencies;
 }
 return configurationRisk(copy);
}
function artifactRisk(relative,bytes){
 const segments=relative.replaceAll('\\','/').split('/');
 if(segments.some(name=>['.git','.vs','.idea','.vscode','node_modules','.next','obj','bin','uploads','qa','publishprofiles'].includes(name.toLowerCase())||/^\.env(?:\.|$)/i.test(name)||/^appsettings\..*local\.json$/i.test(name)||/^(?:secrets\.json|nuget\.config)$/i.test(name)||/\.(?:pfx|p12|pem|key|publishsettings|pubxml|user|log|tmp)$/i.test(name)))return 'local/private artifact';
 if(/\.(?:cs|csproj|http)$/i.test(relative))return 'unnecessary source/development input';
 if(/\.json$/i.test(relative)){
  const value=parseConfiguration(bytes,relative),risk=relative==='AcademyDesk.Api.deps.json'?manifestRisk(value):configurationRisk(value);
  return risk?'nonempty credential configuration':null;
 }
 if(/\.(?:config|xml|txt|ya?ml)$/i.test(relative)&&inlineRisk(bytes.toString('utf8')))return 'inline credential/private key heuristic';
 return null;
}
function scan(directory){
 const manifest=[];let total=0;
 function walk(current){for(const entry of fs.readdirSync(current,{withFileTypes:true})){
  const file=path.join(current,entry.name),relative=path.relative(directory,file).replaceAll('\\','/');
  a.ok(!entry.isSymbolicLink(),'Published payload contains a link; path review required.');
  if(entry.isDirectory()){walk(file);continue;}a.ok(entry.isFile());
  const stat=fs.statSync(file);a.ok(stat.size<=64*1024**2,'Published file exceeds64MiB inspection bound.');
  total+=stat.size;a.ok(total<=512*1024**2&&manifest.length<5000,'Published payload exceeds inspection bound.');
  const bytes=fs.readFileSync(file),risk=artifactRisk(relative,bytes);a.ok(!risk,'Image app artifact risk: '+relative+' / '+risk+'; values withheld.');
  manifest.push({path:relative,bytes:bytes.length,sha256:hash(bytes)});
 }}walk(directory);
 for(const required of['AcademyDesk.Api.dll','AcademyDesk.Api.deps.json','AcademyDesk.Api.runtimeconfig.json','appsettings.json'])a.ok(manifest.some(item=>item.path===required),'Missing published application file: '+required);
 return{manifest,total};
}
function docker(args){return execFileSync('docker',args,{windowsHide:true,encoding:'utf8',timeout:60000,stdio:['ignore','pipe','pipe']});}
function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');const build=process.argv[2];a.match(build,/^[0-9a-f]{32}$/);a.equal(process.argv.length,3);
 const space=fs.statfsSync('D:/');a.ok(Number(space.bavail)*Number(space.bsize)>2*1024**3,'2GiB D: extraction space guard.');
 const receipt=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-'+build,'result.json'),'utf8'));
 a.equal(receipt.passed,true);a.equal(receipt.exitCode,0);a.deepEqual(inputs(),receipt.sourceFiles);a.match(receipt.imageId,/^sha256:[0-9a-f]{64}$/);
 const image=JSON.parse(docker(['image','inspect',receipt.imageId]))[0];a.equal(image.Config.Labels['academydesk.qa.run'],build);
 a.deepEqual(image.Config.Entrypoint,['dotnet','AcademyDesk.Api.dll']);a.equal(image.Config.WorkingDir,'/app');
 for(const env of image.Config.Env||[]){const split=env.indexOf('=');a.ok(split>0);a.ok(!configurationRisk({[env.slice(0,split)]:env.slice(split+1)}),'Nonempty credential image environment; values withheld.');}
 const run=crypto.randomUUID().replaceAll('-',''),name='academydesk-image-artifacts-'+run,evidence=path.join(root,'QA/EVIDENCE/api-image-artifacts-'+run),copy=path.join(root,'.build-check/api-artifacts',run,'app');
 fs.mkdirSync(evidence,{recursive:true});fs.mkdirSync(copy,{recursive:true});
 const result={run,build,imageId:receipt.imageId,passed:false,appStarted:false,network:'none',scope:'published /app payload and image config only; heuristic, not whole-image vulnerability/secret certification',productionApproved:false};let created=false;
 try{
  docker(['create','--pull=never','--name',name,'--label','academydesk.qa.run='+run,'--network','none','--read-only','--entrypoint','/bin/true',receipt.imageId]);created=true;
  let state=JSON.parse(docker(['inspect',name]))[0];a.equal(state.State.Running,false);a.equal(state.State.Status,'created');a.equal(state.HostConfig.NetworkMode,'none');
  docker(['cp',name+':/app/.',copy]);const payload=scan(copy);
  a.deepEqual(inputs(),receipt.sourceFiles,'API source changed during image artifact inspection.');
  Object.assign(result,{passed:true,files:payload.manifest.length,bytes:payload.total,manifest:payload.manifest,sourceUnchanged:true,credentialEnvironment:false,entrypoint:image.Config.Entrypoint,workingDirectory:image.Config.WorkingDir});
 }catch(error){result.failure=String(error.message).slice(0,500);throw error;}
 finally{
  try{if(created){const state=JSON.parse(docker(['inspect',name]))[0];a.equal(state.Config.Labels['academydesk.qa.run'],run);a.equal(state.Image,receipt.imageId);a.equal(state.State.Running,false);docker(['rm',name]);result.ownedContainerRemoved=true;}}
  catch(error){result.passed=false;result.cleanupFailed=true;throw error;}
  finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({run,build,passed:result.passed,files:result.files,bytes:result.bytes,ownedContainerRemoved:result.ownedContainerRemoved,evidence}));}
 }
}
module.exports={artifactRisk};if(require.main===module)try{run();}catch(error){console.error(error.message);process.exitCode=1;}
