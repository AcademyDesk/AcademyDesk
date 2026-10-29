'use strict';
// One bounded local image attempt, with a minimal, secret-screened source context.
// No push, existing-image replacement, credentials, SQL or model activation.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os');
const {spawn,execFileSync}=require('node:child_process');const a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),api=path.join(root,'apps/api');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
function buildNetwork(args){a.ok(args.length===0||(args.length===1&&args[0]==='--approved-project-restore'),'Unknown acquisition/build option.');return args.length?'default':'none';}
function inspectBases(dockerfile,lookup=reference=>{
 try{return execFileSync('docker',['image','inspect',reference,'--format','{{.Id}}'],{windowsHide:true,encoding:'utf8',stdio:['ignore','pipe','ignore']}).trim();}catch{return null;}
}){
 const references=[...dockerfile.matchAll(/^FROM\s+(\S+)(?:\s+AS\s+\S+)?\s*$/gmi)].map(match=>match[1]);
 a.equal(references.length,2,'Review changed Dockerfile stages before running this packet.');
 return references.map(reference=>{a.ok(!reference.includes('$'),'Unresolved base image reference refused.');const imageId=lookup(reference);return{reference,imageId,available:typeof imageId==='string'&&/^sha256:[0-9a-f]{64}$/.test(imageId)};});
}
const skip=name=>['bin','obj','node_modules','.git','uploads'].includes(name)||/^\.env(?:\.|$)/i.test(name)||/^appsettings\..*local\.json$/i.test(name)||/^(?:secrets|nuget)\.config$/i.test(name)||/^secrets\.json$/i.test(name)||/\.(?:pfx|pem|key|p12|log|user|tmp)$/i.test(name);
function inlineRisk(text){
 if(/-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/.test(text))return true;
 if(/\b(?:Password|ClientSecret|AccountKey|AccessToken)\s*=\s*"[^"\r\n]+"/i.test(text))return true;
 return [...text.matchAll(/"((?:\\.|[^"\\\r\n])*)"/g)].some(match=>/(?:Password|Pwd|AccountKey|SharedAccessSignature)\s*=\s*[^;\s]+/i.test(match[1]));
}
function configurationRisk(value,key=''){
 if(Array.isArray(value))return value.some(item=>configurationRisk(item,key));
 if(value&&typeof value==='object')return Object.entries(value).some(([name,item])=>configurationRisk(item,name));
 if(typeof value!=='string'||!value.trim())return false;
 return /(?:password|clientsecret|accountkey|accesskey|privatekey|connectionstring|token|credential|sharedaccesssignature)/i.test(key)||/(?:Password|Pwd|AccountKey|SharedAccessSignature)\s*=/i.test(value)||/-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/.test(value);
}
function parseConfiguration(bytes,file){try{return JSON.parse(bytes.toString('utf8').replace(/^\uFEFF/,''));}catch{throw Error('Configuration JSON could not be parsed in '+file+'; contents withheld.');}}
function inputs(){const files=[];function walk(dir){for(const item of fs.readdirSync(dir,{withFileTypes:true})){
 if(skip(item.name))continue;const file=path.join(dir,item.name);a.ok(!item.isSymbolicLink(),'Build inputs cannot follow links.');
 if(item.isDirectory()){walk(file);continue;}a.ok(item.isFile());
 a.ok(/\.(cs|csproj|json|http)$/.test(item.name)||item.name==='Dockerfile','Unexpected API build input type; review the path before building.');
 const bytes=fs.readFileSync(file);a.ok(bytes.length<=10*1024**2,'Individual build input exceeds10MiB.');
 if(item.name.endsWith('.json'))a.ok(!configurationRisk(parseConfiguration(bytes,path.relative(root,file))),'Probable secret/configuration risk in '+path.relative(root,file)+'; value withheld.');
 if(/\.(cs|http|csproj)$/.test(item.name))a.ok(!inlineRisk(bytes.toString('utf8')),'Probable inline credential in '+path.relative(root,file)+'; value withheld.');
 files.push({path:path.relative(root,file).replaceAll('\\','/'),sha256:hash(bytes),bytes:bytes.length});
 }}walk(api);a.ok(files.length<5000);a.ok(files.reduce((sum,file)=>sum+file.bytes,0)<64*1024**2);return files;}
async function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.ok(os.freemem()>=3*1024**3,'3GiB guard: build not started.');
 const network=buildNetwork(process.argv.slice(2));
 const bases=inspectBases(fs.readFileSync(path.join(api,'Dockerfile'),'utf8'));
 if(bases.some(base=>!base.available)){
  const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/api-image-'+run);fs.mkdirSync(evidence,{recursive:true});
  const result={run,passed:false,status:'BLOCKED_LOCAL_BASES',bases,contextCreated:false,buildStarted:false,downloaded:false,pushed:false,productionApproved:false};
  fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({...result,evidence}));process.exitCode=1;return;
 }
 const files=inputs(),run=crypto.randomUUID().replaceAll('-',''),base=path.join(root,'.build-check/api-image',run),context=path.join(base,'context');fs.mkdirSync(context,{recursive:true});
 for(const file of files){const target=path.join(context,file.path);fs.mkdirSync(path.dirname(target),{recursive:true});fs.copyFileSync(path.join(root,file.path),target,fs.constants.COPYFILE_EXCL);a.equal(hash(fs.readFileSync(target)),file.sha256);}
 // Preserve the original Dockerfile exactly. No NuGet cache injection or test skips.
 fs.copyFileSync(path.join(root,'.dockerignore'),path.join(context,'.dockerignore'),fs.constants.COPYFILE_EXCL);
 const evidence=path.join(root,'QA/EVIDENCE/api-image-'+run);fs.mkdirSync(evidence,{recursive:true});
 const tag='academydesk-local-candidate:qa-'+run,log=path.join(evidence,'build.log'),stream=fs.createWriteStream(log,{flags:'wx'});
 a.ok(os.freemem()>=3*1024**3,'3GiB guard: context retained, compiler not started.');
 const args=['build','--pull=false','--network='+network,'--progress=plain','--label','academydesk.qa.run='+run,'--label','academydesk.qa.kind=api-image','--iidfile',path.join(evidence,'image-id.txt'),'-f','apps/api/Dockerfile','-t',tag,context];
 console.log(JSON.stringify({run,tag,evidence,sourceFiles:files.length,sourceBytes:files.reduce((sum,file)=>sum+file.bytes,0),network,approvedProjectRestore:network==='default',push:false,contextSecretHeuristic:'PASS'}));
 const child=spawn('docker',args,{cwd:context,windowsHide:true,stdio:['ignore','pipe','pipe']});child.stdout.pipe(stream,{end:false});child.stderr.pipe(stream,{end:false});
 let timedOut=false;const timer=setTimeout(()=>{if(child.exitCode===null&&child.signalCode===null){timedOut=true;execFileSync('taskkill.exe',['/PID',String(child.pid),'/T','/F'],{windowsHide:true,stdio:'ignore'});}},600000);
 const exitCode=await new Promise((resolve,reject)=>{child.on('error',reject);child.on('close',resolve);}).finally(()=>clearTimeout(timer));await new Promise(resolve=>stream.end(resolve));
 a.deepEqual(inputs(),files,'Original API source changed during build.');
 const idPath=path.join(evidence,'image-id.txt'),imageId=fs.existsSync(idPath)?fs.readFileSync(idPath,'utf8').trim():null;
 const result={run,tag,passed:exitCode===0&&!timedOut,exitCode,timedOut,imageId,bases,sourceFiles:files,sourceUnchanged:true,contextMinimal:true,buildStepNetwork:network,approvedProjectRestore:network==='default',forcePull:false,pushed:false,productionApproved:false};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({...result,sourceFiles:files.length,evidence}));if(!result.passed)process.exitCode=1;
}
module.exports={buildNetwork,inspectBases,configurationRisk,parseConfiguration,inlineRisk,skip,inputs};if(require.main===module)run().catch(error=>{console.error(error.message);process.exitCode=1;});
