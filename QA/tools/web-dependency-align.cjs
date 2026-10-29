'use strict';
// Recoverable worktree-only replacement of the dependency junction. No npm install,
// deletion, main write, config edit, Docker mutation or network request.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const{inventory,copyFiles,verifyFiles,sourcePins,memoryGuard}=require('./default-web-build.cjs');
const root=path.resolve(__dirname,'../..'),web=path.join(root,'apps/web'),main='D:\\AcademyDesk',expectedTarget=path.join(main,'apps/web/node_modules');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
function childOf(base,file){const relative=path.relative(base,file);a.ok(relative&&!relative.startsWith('..')&&!path.isAbsolute(relative),'Owned target must remain inside its exact workspace.');return file;}
function mainState(){const git=args=>execFileSync('git',['-C',main,...args],{encoding:'utf8',windowsHide:true,stdio:['ignore','pipe','pipe']}).trim();a.equal(git(['diff','--name-only']),'');a.equal(git(['diff','--cached','--name-only']),'');return{head:git(['rev-parse','HEAD']),package:hash(fs.readFileSync(path.join(main,'apps/web/package.json'))),lock:hash(fs.readFileSync(path.join(main,'apps/web/package-lock.json'))),next:hash(fs.readFileSync(path.join(expectedTarget,'next/package.json')))};}
function completeInventory(directory){const files=inventory(directory);let count=0;function walk(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){a.equal(entry.isSymbolicLink(),false);if(entry.isDirectory())walk(path.join(dir,entry.name));else{a.equal(entry.isFile(),true);count++;}}}walk(directory);a.equal(count,files.length,'Do not silently omit dependency files.');return files;}
async function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.equal(process.argv[2],'--align');a.equal(process.argv.length,4);memoryGuard();
 const candidate=require('./web-dependency-candidate.cjs').open(process.argv[3]);a.equal(candidate.worktreeState,'exact-candidate');require('./api-image-payment-browser.cjs').verifyExport(candidate.result.run);
 const destination=childOf(web,path.join(web,'node_modules'));a.equal(fs.lstatSync(destination).isSymbolicLink(),true,'Only replace the known junction, never an existing physical dependency directory.');a.equal(fs.realpathSync(destination).toLowerCase(),expectedTarget.toLowerCase());
 const source=childOf(candidate.app,path.join(candidate.app,'node_modules'));a.equal(fs.realpathSync(source).toLowerCase(),source.toLowerCase());
 const files=completeInventory(source),bytes=files.reduce((sum,row)=>sum+row.bytes,0);a.ok(Number(fs.statfsSync('D:/').bavail)*Number(fs.statfsSync('D:/').bsize)>5*1024**3);
 const run=crypto.randomUUID().replaceAll('-',''),stage=childOf(path.join(root,'.build-check'),path.join(root,'.build-check/web-dependency-alignment',run)),prepared=childOf(stage,path.join(stage,'node_modules')),saved=childOf(stage,path.join(stage,'original-main-junction')),rejected=childOf(stage,path.join(stage,'rejected-node_modules')),evidence=path.join(root,'QA/EVIDENCE/web-dependency-alignment-'+run),before=mainState(),pins=sourcePins(web);
 fs.mkdirSync(prepared,{recursive:true});fs.mkdirSync(evidence,{recursive:true});const result={run,candidate:candidate.result.run,source,destination,savedJunction:saved,files:files.length,bytes,passed:false,mainBefore:before,noDownloads:true,noInstall:true,noDockerMutation:true,productionApproved:false},file=path.join(evidence,'result.json'),save=()=>fs.writeFileSync(file,JSON.stringify(result,null,2));save();
 let junctionSaved=false,installed=false;
 try{
  console.log(JSON.stringify({run,phase:'copy-complete-candidate-dependencies',files:files.length,bytes}));copyFiles(files,source,prepared);await verifyFiles(files,source,prepared);a.equal(completeInventory(prepared).length,files.length);memoryGuard();a.deepEqual(sourcePins(web),pins);a.deepEqual(mainState(),before);
  // Rename only the checked junction itself, never its target. Keep it recoverable.
  a.equal(fs.lstatSync(destination).isSymbolicLink(),true);a.equal(fs.realpathSync(destination).toLowerCase(),expectedTarget.toLowerCase());fs.renameSync(destination,saved);junctionSaved=true;
  fs.renameSync(prepared,destination);installed=true;a.equal(fs.lstatSync(destination).isSymbolicLink(),false);a.equal(fs.realpathSync(destination).toLowerCase(),destination.toLowerCase());await verifyFiles(files,source,destination);a.equal(completeInventory(destination).length,files.length);
  for(const[name,version]of[['next','16.4.1'],['eslint-config-next','16.4.1'],['sharp','0.35.5'],['source-map-js','1.2.2']])a.equal(JSON.parse(fs.readFileSync(path.join(destination,name,'package.json'))).version,version);
  a.deepEqual(sourcePins(web),pins);a.deepEqual(mainState(),before);result.mainAfter=mainState();result.fullDependencyCopyVerified=true;result.physicalWorktreeDependencies=true;result.passed=true;
 }catch(error){
  result.failure='Alignment failed; retain owned copy and restore original junction without touching its target.';
  if(junctionSaved){try{if(installed)fs.renameSync(destination,rejected);fs.renameSync(saved,destination);result.originalJunctionRestored=fs.lstatSync(destination).isSymbolicLink()&&fs.realpathSync(destination).toLowerCase()===expectedTarget.toLowerCase();}catch{result.originalJunctionRestored=false;}}
  process.exitCode=1;
 }finally{try{a.deepEqual(mainState(),before);a.deepEqual(sourcePins(web),pins);result.mainAndApplicationUnchanged=true;}catch{result.mainAndApplicationUnchanged=false;result.passed=false;process.exitCode=1;}save();console.log(JSON.stringify({run,passed:result.passed,mainAndApplicationUnchanged:result.mainAndApplicationUnchanged,originalJunctionRestored:result.originalJunctionRestored,evidence}));}
}
module.exports={childOf,completeInventory};if(require.main===module)run().catch(()=>{console.error('Alignment preflight rejected; no dependency directory swap started.');process.exitCode=1;});
