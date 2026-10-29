'use strict';
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
function prepare(){
 a.ok(os.freemem()>=3*1024**3,'3GiB guard: synthetic key maintenance compiler not started.');
 const run=crypto.randomUUID().replaceAll('-',''),work=path.join(root,'.build-check/key-maintenance',run),empty=path.join(work,'empty-feed'),evidence=path.join(root,'QA/EVIDENCE/api-key-maintenance-'+run);for(const dir of[empty,evidence])fs.mkdirSync(dir,{recursive:true});
 const source=['Program.cs','KeyMaintenance.csproj'].map(file=>({file,sha256:hash(fs.readFileSync(path.join(root,'QA/tools/KeyMaintenance',file)))})),fd=fs.openSync(path.join(evidence,'build.log'),'wx');
 try{execFileSync('dotnet',['build',path.join(root,'QA/tools/KeyMaintenance/KeyMaintenance.csproj'),'--artifacts-path',path.join(work,'artifacts'),'--source',empty,'-p:NuGetAudit=false','-p:UseSharedCompilation=false','--nologo'],{cwd:root,windowsHide:true,timeout:120000,stdio:['ignore',fd,fd]});}finally{fs.closeSync(fd);}
 const output=path.join(work,'artifacts/bin/KeyMaintenance/debug'),manifest=['KeyMaintenance.dll','KeyMaintenance.deps.json','KeyMaintenance.runtimeconfig.json'].map(file=>({file,sha256:hash(fs.readFileSync(path.join(output,file)))}));
 for(const file of source)a.equal(hash(fs.readFileSync(path.join(root,'QA/tools/KeyMaintenance',file.file))),file.sha256);
 const result={run,output,manifest,source,networkAcquisition:false,applicationBuild:false,syntheticOnly:true};fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));return result;
}
module.exports={prepare};
