'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict');
function resolveExport(root,id=process.env.QA_WEB_CANDIDATE){
 if(process.env.QA_GUARDIAN_UI)return require('./guardian-context-ui-export.cjs').verify(process.env.QA_GUARDIAN_UI).web;
 if(process.env.QA_ZOD_CANDIDATE)return require('./zod-security-candidate.cjs').verify(process.env.QA_ZOD_CANDIDATE).web;
 if(!id)return require('./api-image-payment-browser.cjs').verifyExport(false).web;
 a.match(id,/^[a-f0-9]{32}$/);const context=require('./web-dependency-candidate.cjs').open(id);a.equal(context.result.validated,true);a.deepEqual(require('./default-web-build.cjs').sourcePins(context.app),context.result.candidateSourcePins);
 const out=fs.realpathSync(path.join(context.app,'out'));a.equal(out.toLowerCase(),path.join(context.app,'out').toLowerCase());
 a.ok(context.result.exportManifest.length>500);
 for(const row of context.result.exportManifest){a.ok(!row.path.includes('..')&&!path.isAbsolute(row.path));const bytes=fs.readFileSync(path.join(out,row.path));a.equal(crypto.createHash('sha256').update(bytes).digest('hex'),row.sha256);}
 return out;
}
module.exports={resolveExport};
