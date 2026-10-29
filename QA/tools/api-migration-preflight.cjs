'use strict';
// Read-only source/artifact continuity; never invent a baseline image provenance.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const{execFileSync}=require('node:child_process'),a=require('node:assert/strict'),{inputs}=require('./api-image-build.cjs'),{migrations}=require('./api-image-sql.cjs');
const root=path.resolve(__dirname,'../..'),baseline='f7af512f884ca5fe8ac3ddb287c852492592e709';
const recordedImage='sha256:dd092f9786df2e248f625080fbd000ab7f7f9a1fd773f6f83ab3b44ad6759d9c';
function parseMigrationLines(text){const files=new Map();for(const line of text.split(/\r?\n/).filter(Boolean)){const match=/^[0-9a-f]{40}:([^:]+):(.*)$/.exec(line);a.ok(match);files.set(match[1],(files.get(match[1])||'')+'\n'+match[2]);}return migrations([...files.values()]);}
function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.equal(process.argv.length,2);
 const git=args=>execFileSync('git',args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:15000,stdio:['ignore','pipe','pipe']});
 a.equal(git(['rev-parse',baseline+'^{commit}']).trim(),baseline);
 const prior=parseMigrationLines(git(['grep','-F','-e','[Migration("','-e','[DbContext(typeof(',baseline,'--','apps/api/Migrations']));
 const source=inputs(),current=migrations(source.filter(file=>file.path.includes('/Migrations/')&&file.path.endsWith('.cs')).map(file=>fs.readFileSync(path.join(root,file.path),'utf8'))),pending={};
 for(const context of Object.keys(prior)){a.deepEqual(current[context].slice(0,prior[context].length),prior[context],'Baseline migrations must remain exact ordered prefix.');pending[context]=current[context].filter(id=>!prior[context].includes(id));}
 const modified=git(['diff','--diff-filter=MD','--name-only',baseline,'--','apps/api/Migrations']).trim().split(/\r?\n/).filter(Boolean),publishedChanges=modified.filter(file=>!file.endsWith('ModelSnapshot.cs'));a.deepEqual(publishedChanges,[],'Published migration body/designer changed or deleted; review before upgrade.');
 function inspect(reference){try{const row=JSON.parse(execFileSync('docker',['image','inspect',reference],{windowsHide:true,encoding:'utf8',timeout:15000,stdio:['ignore','pipe','ignore']}))[0];return{imageId:row.Id,created:row.Created,revision:row.Config.Labels?.['org.opencontainers.image.revision']||null};}catch{return null;}}
 const actual=inspect('academydesk-qa-api:docker-runtime-f7af512'),recorded=inspect(recordedImage),run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/api-migration-preflight-'+run);fs.mkdirSync(evidence,{recursive:true});
 const result={run,baseline,prior,current,pending,publishedMigrationChanges:publishedChanges,snapshotChanges:modified,sourceContinuity:true,recordedImage,recordedImageAvailable:!!recorded,cachedTag:actual,status:recorded?'REVIEW_BASELINE_SOURCE_BINDING':'BLOCKED_BASELINE_ARTIFACT',upgradeRuntimePassed:false,productionApproved:false,mutation:false};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({run,status:result.status,baselineApplication:prior.AcademyDeskDbContext.length,baselineIdentity:prior.IdentityDbContext.length,pendingApplication:pending.AcademyDeskDbContext.length,pendingIdentity:pending.IdentityDbContext.length,sourceContinuity:true,evidence}));
}
module.exports={parseMigrationLines,run};if(require.main===module)try{run();}catch(error){console.error(error.message);process.exitCode=1;}
