'use strict';
const fs=require('node:fs'),path=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),uiId='f1d14fe30c3f47a19bd84f674bcd0086',windowId='c1a2bc21a77a4f6c83eb007cea7a9085';
const read=p=>fs.readFileSync(path.join(root,'QA/EVIDENCE',p)),json=p=>JSON.parse(read(p)),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function run(){
 const ui=json('material-scope-ui-'+uiId+'/result.json'),web=require('./material-scope-ui-export.cjs').verify(uiId);
 a.equal(ui.passed,true);a.equal(ui.handlerChecks,75);a.equal(ui.failure,undefined);
 a.deepEqual(ui.steps.map(s=>s.name),['handlers','typecheck','targeted-lint','build','browser']);a.ok(ui.steps.every(s=>s.exitCode===0&&s.timedOut===false));
 const prior=json('compact-theme-ui-ac009cf087bf4114a0a8e4e58ae179cf/result.json'),pins=new Map(prior.sourcePins.map(p=>[p.path,p.sha256]));
 a.equal(ui.sourcePins.length,pins.size);a.deepEqual(ui.sourcePins.filter(p=>p.sha256!==pins.get(p.path)).map(p=>p.path),['src/components/private-material-actions.tsx']);
 const api=json('api-image-'+ui.apiBuild+'/result.json');a.equal(api.passed,true);a.deepEqual(require('./api-image-build.cjs').inputs(),api.sourceFiles);
 a.match(ui.browserRun,/^[0-9a-f]{32}$/);const browserPath='material-scope-browser-'+ui.browserRun,b=json(browserPath+'/result.json');
 a.equal(b.passed,true);a.equal(b.ui,uiId);a.equal(b.webBuild,web.build);a.equal(b.verifiedExportFiles,627);a.equal(b.failure,undefined);a.equal(b.failedCase,undefined);
 for(const k of['apiResponseMocks','syntheticToken'])a.equal(b[k],true);for(const k of['actualSql','serverAuthorizationTested','physicalDevicesTested','productionApproved'])a.equal(b[k],false);
 a.deepEqual(b.cases.map(c=>[c.width,c.theme]),[320,390,768,1440].flatMap(w=>[[w,'light'],[w,'dark']]));
 const screenshots=b.cases.map(c=>{a.equal(c.pageErrors,0);for(const k of['previewAndClose','unmountAbortsPendingRead','noLateByteRequest','freshPreviewWorks','allObjectUrlsReleased','noStalePreviewOrNotice','noWrites','noHorizontalOverflow'])a.equal(c[k],true);const bytes=read(browserPath+'/material-'+c.width+'-'+c.theme+'.png');a.equal(bytes.subarray(0,8).toString('hex'),'89504e470d0a1a0a');a.equal(bytes.readUInt32BE(16),c.width);return{width:c.width,theme:c.theme,sha256:hash(bytes)};});
 const baseline=read('private-material-scope-baseline-settled-20261025.tap').toString();for(const[k,n]of[['tests',9],['pass',1],['fail',8],['skipped',0]])a.match(baseline,new RegExp('^# '+k+' '+n+'$','m'));
 const w=json('api-runtime-window-'+windowId+'/result.json');require('./compact-theme-qualification.cjs').resources(w);a.equal(w.uiRun,uiId);a.equal(w.webBuild,web.build);
 const output=JSON.parse(read('api-runtime-window-'+windowId+'/runtime.log').toString().trim());a.equal(output.passed,true);a.equal(output.run,uiId);a.equal(output.webBuild,web.build);a.equal(output.exportFiles,627);
 a.equal(fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')),false);
 console.log(JSON.stringify({passed:true,scope:'Private material client preview/request lifecycle only',uiRun:uiId,webBuild:web.build,windowRun:windowId,browserRun:b.run,handlerChecks:75,exportFiles:627,syntheticBrowserJourneys:8,screenshots,browserSha256:hash(read(browserPath+'/result.json')),windowSha256:hash(read('api-runtime-window-'+windowId+'/result.json')),serverAuthorizationTested:false,physicalDevicesTested:false,enterpriseComplete:false,productionApproved:false},null,2));
}
if(require.main===module)try{run();}catch(error){console.error(JSON.stringify({passed:false,scope:'Material scope proof incomplete or changed',type:error.name,sourceLines:[...String(error.stack||'').matchAll(/(?:material-scope-qualification|compact-theme-qualification)\.cjs:(\d+):(\d+)/g)].map(m=>({line:Number(m[1]),column:Number(m[2])}))}));process.exitCode=1;}
