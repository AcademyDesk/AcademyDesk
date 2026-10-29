'use strict';
const fs=require('node:fs'),path=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function verify(id){
 a.match(id,/^[0-9a-f]{32}$/);
 const r=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/review-signoff-ui-'+id+'/result.json')));
 a.equal(r.run,id);a.equal(r.exportReady,true);a.equal(r.handlerChecks,41);
 for(const key of ['productionApproved','downloaded'])a.equal(r[key],false);
 a.deepEqual(r.steps.slice(0,4).map(s=>s.name),['handlers','typecheck','targeted-lint','build']);
 a.ok(r.steps.slice(0,4).every(s=>s.exitCode===0&&!s.timedOut));
 a.deepEqual(require('./default-web-build.cjs').sourcePins(path.join(root,'apps/web')),r.sourcePins);
 const b=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/default-web-build-'+r.webBuild+'/result.json')));
 a.equal(b.passed,true);a.equal(b.sourceUnchanged,true);a.equal(b.downloaded,false);a.equal(b.installed,false);a.deepEqual(b.sourcePins,r.sourcePins);
 a.equal(r.exportRelative,'.build-check/default-web-build/'+r.webBuild+'/app/out');
 const out=path.join(root,r.exportRelative);a.equal(fs.realpathSync(out).toLowerCase(),out.toLowerCase());
 a.equal(r.exportManifest.length,627);a.equal(new Set(r.exportManifest.map(f=>f.path)).size,627);
 for(const f of r.exportManifest){a.ok(!path.isAbsolute(f.path)&&!f.path.split('/').includes('..'));const file=fs.realpathSync(path.join(out,f.path));a.ok(file.startsWith(out+path.sep));const bytes=fs.readFileSync(file);a.equal(bytes.length,f.bytes);a.equal(hash(bytes),f.sha256);}
 return{web:out,build:r.webBuild,verifiedExportFiles:627,exportRelative:r.exportRelative};
}
module.exports={verify};
