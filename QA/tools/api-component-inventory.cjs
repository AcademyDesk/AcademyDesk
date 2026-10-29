'use strict';
// Offline /app component inventory, not a whole-image SBOM or vulnerability scan.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),{build,image}=require('./api-image-qualification.cjs');
const artifactRun='d98dcbc285f94ec39dbc72f44b9e8e43';
const digest=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
function safeRelative(relative){a.equal(typeof relative,'string');a.ok(relative.length>0&&relative.length<=512&&!relative.includes('\\')&&!/[\x00-\x1f\x7f]/.test(relative));a.ok(!path.posix.isAbsolute(relative));a.ok(relative.split('/').every(part=>part!==''&&part!=='.'&&part!=='..'&&!part.includes(':')));return relative;}
function componentId(id){const match=/^([A-Za-z0-9_.-]+)\/(\d+\.\d+\.\d+(?:[-+][A-Za-z0-9_.-]+)?)$/.exec(id);a.ok(match,'Invalid dependency identity; values withheld.');return{name:match[1],version:match[2]};}
function inventory(deps,manifest){
 a.equal(typeof deps.runtimeTarget?.name,'string');a.deepEqual(Object.keys(deps.targets),[deps.runtimeTarget.name]);
 const target=deps.targets[deps.runtimeTarget.name],ids=Object.keys(target).sort();a.deepEqual(ids,Object.keys(deps.libraries).sort());
 const files=new Map();for(const file of manifest){safeRelative(file.path);a.match(file.sha256,/^[a-f0-9]{64}$/);a.ok(Number.isSafeInteger(file.bytes)&&file.bytes>=0);a.ok(!files.has(file.path));files.set(file.path,file);}
 const components=ids.map(id=>{
  const identity=componentId(id),library=deps.libraries[id],value=target[id];a.ok(['project','package'].includes(library.type));a.ok(Object.keys(value).every(key=>['dependencies','runtime','resources','runtimeTargets'].includes(key)),'Unknown target declaration requires review; do not omit it.');
  const dependencies=Object.entries(value.dependencies||{}).map(([name,version])=>{const dependency=name+'/'+version;componentId(dependency);a.ok(Object.hasOwn(target,dependency),'Unresolved dependency; values withheld.');return dependency;}).sort();
  if(library.type==='package'){a.match(library.sha512,/^sha512-[A-Za-z0-9+/]{86}==$/);a.equal(Buffer.from(library.sha512.slice(7),'base64').length,64);}
  const assets=[];
  for(const kind of['runtime','resources','runtimeTargets'])for(const [declared,metadata]of Object.entries(value[kind]||{})){
   safeRelative(declared);if(kind==='resources')a.match(metadata.locale,/^[A-Za-z0-9-]+$/);
   const published=kind==='runtimeTargets'?declared:kind==='resources'?metadata.locale+'/'+path.posix.basename(declared):path.posix.basename(declared);
   const file=files.get(published);a.ok(file,'Declared runtime asset absent from verified publish manifest; values withheld.');assets.push({kind,declared,published,bytes:file.bytes,sha256:file.sha256});
  }
  return{id,...identity,type:library.type,declaredPackageIntegrity:library.type==='package'?library.sha512:null,packageArchiveVerified:false,licenseReview:'NOT REVIEWED',vulnerabilityReview:'NOT SCANNED',dependencies,assets:assets.sort((x,y)=>x.published.localeCompare(y.published))};
 });
 const projects=components.filter(row=>row.type==='project');a.equal(projects.length,1);a.equal(projects[0].name,'AcademyDesk.Api');
 const reachable=new Set(),byId=new Map(components.map(row=>[row.id,row]));function visit(id){if(reachable.has(id))return;reachable.add(id);for(const dependency of byId.get(id).dependencies)visit(dependency);}visit(projects[0].id);
 a.equal(reachable.size,components.length,'Unreachable runtime components require review.');
 return{schema:'academy-published-component-inventory/1',runtimeTarget:deps.runtimeTarget.name,root:projects[0].id,componentCount:components.length,packageCount:components.filter(row=>row.type==='package').length,declaredAssetCount:components.reduce((sum,row)=>sum+row.assets.length,0),components,wholeImageSbomComplete:false,licenseReviewComplete:false,vulnerabilityScanComplete:false,productionApproved:false,limitations:['Published application dependency graph only; not base OS, shared framework, SDK, frontend or build tooling.','NuGet integrity is declared manifest metadata; no package archive acquisition or verification.','No licence, advisory, exploitability or legal review performed.']};
}
function run(){
 a.equal(process.argv.length,2);a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');
 const buildReceipt=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-'+build+'/result.json')));a.equal(buildReceipt.passed,true);a.equal(buildReceipt.imageId,image);a.deepEqual(require('./api-image-build.cjs').inputs(),buildReceipt.sourceFiles);
 const receiptFile=path.join(root,'QA/EVIDENCE/api-image-artifacts-'+artifactRun+'/result.json'),receiptBytes=fs.readFileSync(receiptFile),receipt=JSON.parse(receiptBytes);
 a.equal(receipt.passed,true);a.equal(receipt.build,build);a.equal(receipt.imageId,image);a.equal(receipt.ownedContainerRemoved,true);a.equal(receipt.sourceUnchanged,true);a.equal(receipt.appStarted,false);a.equal(receipt.files,63);a.equal(receipt.manifest.length,63);
 const directory=path.join(root,'.build-check/api-artifacts',artifactRun,'app');
 for(const file of receipt.manifest){safeRelative(file.path);const absolute=path.join(directory,...file.path.split('/'));a.equal(fs.lstatSync(absolute).isSymbolicLink(),false);a.equal(fs.statSync(absolute).size,file.bytes);a.equal(digest(fs.readFileSync(absolute)),file.sha256,'Published file hash changed; values withheld.');}
 const depsFile=path.join(directory,'AcademyDesk.Api.deps.json'),depsBytes=fs.readFileSync(depsFile),result=inventory(JSON.parse(depsBytes),receipt.manifest);
 a.deepEqual(require('./api-image-build.cjs').inputs(),buildReceipt.sourceFiles,'API source changed during offline inventory.');
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/api-component-inventory-'+run);fs.mkdirSync(evidence,{recursive:true});
 Object.assign(result,{run,build,imageId:image,artifactRun,artifactReceiptSha256:digest(receiptBytes),dependencyManifestSha256:digest(depsBytes),verifiedPublishedFiles:63,sourceUnchanged:true,networkUsed:false,dockerMutation:false});
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({run,evidence,componentCount:result.componentCount,packageCount:result.packageCount,declaredAssetCount:result.declaredAssetCount,verifiedPublishedFiles:63,wholeImageSbomComplete:false,productionApproved:false}));
}
module.exports={inventory,safeRelative};if(require.main===module)try{run();}catch{console.error('Offline component inventory failed; values withheld.');process.exitCode=1;}
