'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{cases,verifyFailure,verifyConfiguration}=require('./api-image-key-config.cjs');
const good={Running:false,OOMKilled:false,ExitCode:134},log='Security:DataProtection:Certificate requires valid material. No fallback is permitted.';
test('misconfiguration requires real nonzero exit, expected boundary, no OOM and no echoed configuration values',()=>{
 a.equal(verifyFailure(good,log,'Certificate',['private-sentinel']).safeConfigurationFailure,true);
 for(const state of[{...good,Running:true},{...good,OOMKilled:true},{...good,ExitCode:0}])a.throws(()=>verifyFailure(state,log,'Certificate',[]));
 a.throws(()=>verifyFailure(good,log+'private-sentinel','Certificate',['private-sentinel']));a.throws(()=>verifyFailure(good,'unrelated startup crash','Certificate',[]));
});
test('all five startup failures and healthy restoration must finish before acceptance',async()=>{
 const result={checks:[]},names=[];let restored=false;
 await verifyConfiguration({result,probe:async row=>{names.push(row.name);return verifyFailure(good,log.replace(':Certificate',':'+row.field),row.field,[]);},restore:async()=>{restored=true;}});
 a.equal(result.keyConfiguration.passed,true);a.equal(result.checks.length,5);a.deepEqual(names,cases.map(row=>row.name));a.equal(restored,true);
 const failed={checks:[]};await a.rejects(verifyConfiguration({result:failed,probe:async()=>{throw Error('fixture failed');},restore:async()=>{}}));a.equal(failed.keyConfiguration.passed,false);
});
