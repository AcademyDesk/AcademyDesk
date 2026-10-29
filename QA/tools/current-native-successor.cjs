'use strict';
// Compact read-only entry point to the existing exact receipt qualification.
// No tests are replaced, no failed envelope waived, no issue closed.
const path=require('node:path'),a=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const ids=[
 '5cf31ac559164848b493d9f69330067c','9a06d7e7c0464cb78458a630ca7c84e3',
 'cd76de157ec744dfa81d72f045bfffdb','2e0dc820a98e4fab800ca3f08790044c',
 'a78ec590dfd946648b7f0384ff1c2ec5','2b0615b22b824f3ab614eab4f818722f',
 'db412a4d58424333a01f73fc0d5e3575','7520f723e5204994806c9b8903a1893a',
 '5a18723b54ec4fde948e09aa4aa966d6','bf1b3ec39d6d4688b4966a1a0e0992a8',
 '6375e819f5a847bcbd378e1b22e17a19','4eed1898f83342c583e5684beca1fb08',
 '3a58c2f1f1234d2cac12f1d3741cab37','2e396cf0951042139badab5e5b812200'
];
function summarize(r){
 a.equal(r.currentNativeReceiptsVerified,19);a.equal(r.candidate,'293e5beba439453794e6c1e68adc2b71');a.equal(r.exportFiles,627);a.equal(r.apiOnly.length,12);a.equal(r.native.length,7);
 for(const key of['worktreeAligned','mainUnchanged','fullInventoryPreserved','apiSourceMatches','historicalDriftRetained'])a.equal(r[key],true);
 for(const key of['historicalFrontendEnvelopeAccepted','securityApproved','productionApproved','enterpriseTestingComplete','physicalDevicesTested','liveAIQualified'])a.equal(r[key],false);a.equal(r.remainingAdvisoryMatches,1);
 a.deepEqual(r.native.map(x=>[x.run,x.window]),Array.from({length:7},(_,i)=>ids.slice(i*2,i*2+2)));for(const record of [...r.apiOnly,...r.native])a.equal(record.receiptAccepted,true);
 const screenshots=r.native.flatMap(x=>x.screenshots),csv=r.native.flatMap(x=>x.csv||[]);a.equal(screenshots.length,56);a.equal(csv.length,4);for(const artifact of[...screenshots,...csv])a.match(artifact.sha256,/^[0-9a-f]{64}$/);
 return{passed:true,scope:'Current pinned native slices only; not full enterprise or production acceptance',candidate:r.candidate,currentNativeReceiptsVerified:19,exportFiles:627,screenshotHashes:56,csvHashes:4,remainingAdvisoryMatches:1,failedHistoricalEnvelopeRetained:true,productionApproved:false,enterpriseTestingComplete:false,physicalDevicesTested:false,liveAIQualified:false};
}
if(require.main===module){try{a.equal(process.argv.length,2);const root=path.resolve(__dirname,'../..');a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');const bytes=execFileSync(process.execPath,[path.join(__dirname,'web-dependency-qualification.cjs'),'--native-enrollment-successor',...ids],{cwd:root,encoding:'utf8',windowsHide:true,timeout:60000,maxBuffer:2*1024**2,stdio:['ignore','pipe','pipe']});console.log(JSON.stringify(summarize(JSON.parse(bytes)),null,2));}catch{console.error('Current native qualification failed/incomplete; receipts retained and production NOT approved.');process.exitCode=1;}}
module.exports={ids,summarize};
