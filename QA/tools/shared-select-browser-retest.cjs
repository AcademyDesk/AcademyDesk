'use strict';
// Browser-only successor. The earlier UI envelope remains FAIL; no build repeated.
const fs=require('node:fs'),path=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto'),{spawnSync}=require('node:child_process');
const root=path.resolve(__dirname,'../..'),ui='2e6a10b2410142f183d546379a04d7cf';
function run(){
 a.equal(process.argv.length,3);
 const api=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/api-image-'+process.argv[2]+'/result.json')));
 a.deepEqual(require('./api-image-build.cjs').inputs(),api.sourceFiles);
 require('./default-web-build.cjs').memoryGuard();
 const prior=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/shared-select-ui-'+ui+'/result.json')));
 a.equal(prior.passed,false);a.equal(prior.exportReady,true);
 a.equal(prior.failure,'Shared select validation failed: browser\n\n1 !== 0\n');
 a.deepEqual(prior.steps.map(s=>s.name),['handlers','typecheck','targeted-lint','build','browser']);
 a.equal(prior.steps[4].exitCode,1);a.equal(prior.steps[4].timedOut,false);
 const web=require('./shared-select-ui-export.cjs').verify(ui),run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/shared-select-retest-'+run);
 fs.mkdirSync(evidence,{recursive:true});
 const result={run,sourceUi:ui,webBuild:web.build,passed:false,buildRepeated:false,applicationChanged:false,productionApproved:false};
 try{
  const p=spawnSync(process.execPath,[path.join(root,'QA/tools/shared-select-keyboard-browser.cjs'),ui],{cwd:root,windowsHide:true,encoding:'utf8',timeout:300000,maxBuffer:1024**2});
  fs.writeFileSync(path.join(evidence,'browser.log'),(p.stdout||'')+(p.stderr||''));
  result.exitCode=p.status;result.timedOut=p.error?.code==='ETIMEDOUT';a.equal(p.error,undefined);a.equal(p.status,0);
  const b=JSON.parse(p.stdout.trim().split(/\r?\n/).at(-1));a.equal(b.passed,true);a.equal(b.journeys,8);
  result.browserRun=b.run;require('./shared-select-ui-export.cjs').verify(ui);result.passed=true;
 }catch(error){result.failure='Browser-only successor failed; retain screenshot and source.';result.type=error.name;process.exitCode=1;}
 finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({run,sourceUi:ui,webBuild:web.build,browserRun:result.browserRun,passed:result.passed,evidence,productionApproved:false}));}
}
if(require.main===module)try{run();}catch(error){console.error(JSON.stringify({passed:false,scope:'shared-select-browser-retest-preflight',type:error.name}));process.exitCode=1;}
