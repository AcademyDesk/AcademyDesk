'use strict';
const fs=require('node:fs'),path=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),uiId='ac009cf087bf4114a0a8e4e58ae179cf',windowId='402c768e52504bdcb1632607ab99bef7';
const read=p=>fs.readFileSync(path.join(root,'QA/EVIDENCE',p)),json=p=>JSON.parse(read(p)),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function resources(w){
 for(const k of['passed','runtimePassed','restorationPassed','inventoryPreserved'])a.equal(w[k],true);
 a.equal(w.productionApproved,false);a.equal(w.restartInvocations,0);a.equal(w.runtimeExit,0);a.equal(w.failure,undefined);
 a.ok(w.admissionFreeBytes>=3*1024**3);a.deepEqual(w.before,w.after);a.deepEqual(w.restorationErrors,[]);
 a.deepEqual(w.services.map(s=>s.name).sort(),['pentaaimodels-api-1','pentaaimodels-inference-1']);a.equal(w.restored.length,2);
 for(const s of w.services){const r=w.restored.find(r=>r.name===s.name);a.equal(r.id,s.id);a.equal(r.image,s.image);a.equal(r.running,true);a.ok(r.health===null||r.health==='healthy');}
}
function qualify(){
 const ui=json('compact-theme-ui-'+uiId+'/result.json'),web=require('./compact-theme-ui-export.cjs').verify(uiId);
 a.equal(ui.passed,true);a.equal(ui.handlerChecks,63);a.equal(ui.failure,undefined);
 a.deepEqual(ui.steps.map(s=>s.name),['handlers','typecheck','targeted-lint','build','browser']);a.ok(ui.steps.every(s=>s.exitCode===0&&s.timedOut===false));
 const prior=json('shared-select-ui-2e6a10b2410142f183d546379a04d7cf/result.json'),pins=new Map(prior.sourcePins.map(p=>[p.path,p.sha256]));
 a.equal(ui.sourcePins.length,pins.size);a.deepEqual(ui.sourcePins.filter(p=>p.sha256!==pins.get(p.path)).map(p=>p.path),['src/components/manual-workspace.module.css']);
 a.match(ui.browserRun,/^[0-9a-f]{32}$/);const browserPath='compact-theme-browser-'+ui.browserRun,b=json(browserPath+'/result.json');
 a.equal(b.passed,true);a.equal(b.ui,uiId);a.equal(b.webBuild,web.build);a.equal(b.verifiedExportFiles,627);
 for(const k of['apiResponseMocks','syntheticToken'])a.equal(b[k],true);
 for(const k of['actualSql','serverAuthorizationTested','physicalDevicesTested','productionApproved'])a.equal(b[k],false);
 a.equal(b.failure,undefined);a.equal(b.failedCase,undefined);
 a.deepEqual(b.cases.map(c=>[c.width,c.theme]),[320,390,768,1440].flatMap(w=>[[w,'light'],[w,'dark']]));
 const screenshots=b.cases.map(c=>{a.equal(c.options,6);a.equal(c.pageErrors,0);for(const k of['themeControlsContained','themeKeyboardSwitch','selectedFocus','arrowHomeEnd','typeAhead','enterAndSpace','tabAndReverseTab','escapeRestoresFocus','pointerParity','topLayerContained','noImplicitChange','noWrites'])a.equal(c[k],true);const bytes=read(browserPath+'/select-'+c.width+'-'+c.theme+'.png');a.equal(bytes.subarray(0,8).toString('hex'),'89504e470d0a1a0a');a.equal(bytes.readUInt32BE(16),c.width);a.equal(bytes.readUInt32BE(20),900);return{width:c.width,theme:c.theme,sha256:hash(bytes)};});
 const baseline=json('compact-theme-baseline-a32b3e11b7fc49b98b2ce6eb492c09cf/result.json');
 a.equal(baseline.passed,true);a.equal(baseline.expectedDefectReproduced,true);a.equal(baseline.diagnosticOnly,true);a.equal(baseline.uiAccepted,false);a.ok(baseline.themeGeometry.rows.some(r=>!r.textFits));
 resources(json('api-runtime-window-9a3ad34d73df4a22a5fd6babb9dfaf22/result.json'));
 const w=json('api-runtime-window-'+windowId+'/result.json');resources(w);a.equal(w.uiRun,uiId);a.equal(w.webBuild,web.build);
 const output=JSON.parse(read('api-runtime-window-'+windowId+'/runtime.log').toString().trim());a.equal(output.passed,true);a.equal(output.run,uiId);a.equal(output.webBuild,web.build);a.equal(output.exportFiles,627);
 a.equal(fs.existsSync(path.join(root,'.build-check/qa-validation-window.lock')),false);
 return{passed:true,scope:'Admin compact-theme paint containment and retained shared-select keyboard',uiRun:uiId,webBuild:web.build,windowRun:windowId,browserRun:b.run,handlerChecks:63,exportFiles:627,syntheticBrowserJourneys:8,screenshots,browserSha256:hash(read(browserPath+'/result.json')),windowSha256:hash(read('api-runtime-window-'+windowId+'/result.json')),serverAuthorizationTested:false,physicalDevicesTested:false,enterpriseComplete:false,productionApproved:false};
}
module.exports={qualify,resources};
if(require.main===module)try{console.log(JSON.stringify(qualify(),null,2));}catch(error){console.error(JSON.stringify({passed:false,scope:'compact theme proof incomplete or changed',type:error.name,sourceLines:[...String(error.stack||'').matchAll(/compact-theme-qualification\.cjs:(\d+):(\d+)/g)].map(m=>({line:Number(m[1]),column:Number(m[2])}))}));process.exitCode=1;}
