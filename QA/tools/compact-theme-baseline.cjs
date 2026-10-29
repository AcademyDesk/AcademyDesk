'use strict';
// Successful diagnostic means the expected defect was reproduced, never UI acceptance.
const fs=require('node:fs'),path=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto'),{spawnSync}=require('node:child_process');
const root=path.resolve(__dirname,'../..'),ui='2e6a10b2410142f183d546379a04d7cf';
function run(){
 a.equal(process.argv.length,3);require('./default-web-build.cjs').memoryGuard();
 const web=require('./shared-select-ui-export.cjs').verify(ui);
 const p=spawnSync(process.execPath,[path.join(root,'QA/tools/compact-theme-browser.cjs'),ui,'--baseline'],{cwd:root,windowsHide:true,encoding:'utf8',timeout:120000,maxBuffer:1024**2});
 a.equal(p.error,undefined);a.equal(p.status,1);
 const output=JSON.parse(p.stdout.trim().split(/\r?\n/).at(-1));a.equal(output.passed,false);a.match(output.run,/^[0-9a-f]{32}$/);
 const browser=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/compact-theme-browser-'+output.run+'/result.json')));
 a.deepEqual([browser.failedCase.width,browser.failedCase.theme,browser.failedCase.stage,browser.failedCase.type],[320,'light','theme paint and hit-target containment','AssertionError']);
 a.equal(browser.cases.length,0);a.ok(browser.lastThemeGeometry.rows.some(r=>!r.textFits));
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/compact-theme-baseline-'+run);
 fs.mkdirSync(evidence,{recursive:true});const result={run,sourceUi:ui,webBuild:web.build,browserRun:browser.run,passed:true,diagnosticOnly:true,uiAccepted:false,expectedDefectReproduced:true,themeGeometry:browser.lastThemeGeometry,productionApproved:false};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));
 console.log(JSON.stringify({...result,evidence}));
}
if(require.main===module)try{run();}catch(error){console.error(JSON.stringify({passed:false,scope:'compact-theme-baseline-preflight',type:error.name}));process.exitCode=1;}
