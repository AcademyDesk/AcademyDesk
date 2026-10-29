'use strict';
// Offline script generation from hash-verified exact published candidate DLLs.
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),crypto=require('node:crypto');
const{execFileSync}=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
function prepare(){
 a.ok(os.freemem()>=3*1024**3,'3GiB guard: migration script compiler not started.');
 const preflight=JSON.parse(execFileSync(process.execPath,[path.join(root,'QA/tools/api-migration-preflight.cjs')],{windowsHide:true,encoding:'utf8',timeout:20000}));
 const prior=JSON.parse(fs.readFileSync(path.join(preflight.evidence,'result.json'),'utf8'));a.equal(prior.sourceContinuity,true);
 const artifact=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-artifacts-d98dcbc285f94ec39dbc72f44b9e8e43/result.json'),'utf8'));a.equal(artifact.passed,true);a.equal(artifact.build,'0a5f8d2c708a40a994dd97e9f6d12425');
 const candidate=path.join(root,'.build-check/api-artifacts/d98dcbc285f94ec39dbc72f44b9e8e43/app');for(const file of artifact.manifest)a.equal(hash(fs.readFileSync(path.join(candidate,file.path))),file.sha256);
 const run=crypto.randomUUID().replaceAll('-',''),work=path.join(root,'.build-check/migration-scripts',run),empty=path.join(work,'empty-feed'),output=path.join(work,'sql'),evidence=path.join(root,'QA/EVIDENCE/api-migration-scripts-'+run);for(const dir of[empty,output,evidence])fs.mkdirSync(dir,{recursive:true});
 const log=path.join(evidence,'build.log'),fd=fs.openSync(log,'wx');
 try{execFileSync('dotnet',['build',path.join(root,'QA/tools/MigrationScript/MigrationScript.csproj'),'--artifacts-path',path.join(work,'artifacts'),'--source',empty,'-p:NuGetAudit=false','-p:CandidateDirectory='+candidate,'-p:UseSharedCompilation=false','--nologo'],{cwd:root,windowsHide:true,timeout:120000,stdio:['ignore',fd,fd]});}finally{fs.closeSync(fd);}
 const executable=path.join(work,'artifacts/bin/MigrationScript/debug/MigrationScript.dll'),app=prior.prior.AcademyDeskDbContext.at(-1),identity=prior.prior.IdentityDbContext.at(-1);
 const generateFd=fs.openSync(path.join(evidence,'generate.log'),'wx');
 try{execFileSync('dotnet',[executable,output,app,identity],{cwd:root,windowsHide:true,timeout:30000,stdio:['ignore',generateFd,generateFd]});}finally{fs.closeSync(generateFd);}
 const scripts=['baseline-application.sql','baseline-identity.sql'].map(file=>{const bytes=fs.readFileSync(path.join(output,file));return{file,bytes:bytes.length,sha256:hash(bytes)};});
 const generatorDirectory=path.dirname(executable),generatorManifest=fs.readdirSync(generatorDirectory,{withFileTypes:true}).filter(file=>file.isFile()).map(file=>({file:file.name,sha256:hash(fs.readFileSync(path.join(generatorDirectory,file.name)))}));
 const result={run,preflight:preflight.run,candidateImageId:artifact.imageId,candidateBuild:artifact.build,baseline:prior.baseline,prior:prior.prior,current:prior.current,pending:prior.pending,sourceContinuity:true,networkAcquisition:false,generatedFrom:'exact candidate assembly unchanged historical migrations; reconstructed baseline schema, not old release binary',scripts,output,generatorDirectory,generatorManifest};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));return result;
}
module.exports={prepare};if(require.main===module)try{console.log(JSON.stringify(prepare()));}catch{console.error('Migration script preparation failed; consult owned build log; no database connected.');process.exitCode=1;}
