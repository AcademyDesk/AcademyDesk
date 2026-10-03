// Evidence/preservation consistency, not another business-test count.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,'');
const json=f=>JSON.parse(read(f));
const sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const receipt=json('QA/EVIDENCE/certificate-font-source-receipt.json'),appearance=json(receipt.predecessor),accepted=json(appearance.predecessor);
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),receipt.commit);
assert.deepEqual(receipt.intentionalBefore.map(x=>x.file).sort(),['apps/web/src/app/certificates/page.tsx','QA/ISSUES/BUG-FUNC-0032.md'].sort());
for(const e of receipt.intentionalBefore)assert.equal(e.sha256,appearance.current.find(x=>x.file===e.file).sha256);
for(const e of appearance.current)if(!receipt.intentionalBefore.some(x=>x.file===e.file))assert.equal(sha(e.file),e.sha256,'Appearance retained: '+e.file);
for(const group of ['sources','evidence','binaries','normalBinaries'])for(const e of accepted[group])if(!appearance.intentionalBefore.some(x=>x.file===e.file))assert.equal(sha(e.file),e.sha256,'Accepted predecessor retained: '+e.file);
for(const e of receipt.current)assert.equal(sha(e.file),e.sha256,e.file);
for(const e of receipt.copiedProduct)assert.equal(sha(e.file),sha(e.copy),e.file+' QA copy mismatch');
for(const e of receipt.fontSource)assert.equal(sha(e.file),e.sha256,e.file);
const transport=json('QA/EVIDENCE/certificate-font-transport.json'),baseline=json('QA/EVIDENCE/certificate-font-corrected-baseline.json');
assert.equal(transport.prints.length,4);assert.equal(transport.fontRequests.length,5);
assert.deepEqual(baseline.prints[0],transport.prints[0]);
assert.equal(transport.prints[0].fontStatus,'loading');assert.equal(transport.prints[0].faces[0].status,'loading');
assert.ok(transport.prints[0].at<transport.fontRequests[0].servedAt);
for(const request of transport.fontRequests){assert.equal(request.bytes,29288);assert.ok(request.servedAt>=request.requestedAt+request.delay);}
for(const [printIndex,fontIndex] of [[1,1],[2,3],[3,4]]){
  const print=transport.prints[printIndex];assert.equal(print.state,'issued');assert.equal(print.fontStatus,'loaded');
  assert.ok(print.faces.length>0&&print.faces.every(x=>x.status==='loaded'));assert.ok(print.at>=transport.fontRequests[fontIndex].servedAt);
  assert.match(print.text,/CERT-SYNTHETIC-001/);assert.match(print.text,/VERIFY-SYNTHETIC-001/);assert.doesNotMatch(print.text,/PREVIEW/);
}
assert.ok(transport.prints[2].at>=transport.images[0].servedAt);
const combined=transport.prints[3];assert.equal(combined.images[0].complete,true);assert.equal(combined.images[0].naturalWidth,64);
assert.ok(combined.at>=transport.images.find(x=>combined.images[0].src.endsWith(x.path)).servedAt);
assert.ok(!transport.prints.some(x=>x.at>=transport.fontRequests[2].requestedAt&&x.at<transport.fontRequests[3].requestedAt),'Cancelled readiness never triggers output');
const failed=json('QA/EVIDENCE/certificate-font-fixture-failed-attempt.json');assert.equal(failed.prints[1].faces[0].status,'error');
assert.notEqual(sha('QA/tools/certificate-font-fixture.cjs'),sha('QA/EVIDENCE/certificate-font-fixture-first-attempt.cjs'));
const observations=json('QA/EVIDENCE/certificate-font-observations.json');assert.equal(observations.facts.length,8);
const fact=name=>{const f=observations.facts.find(x=>x.check===name);assert.ok(f,name);return f;};
assert.match(observations.facts[0].fixtureOutcome,/not a successful custom-font test/);
assert.equal(fact('corrected-fixture-repaired-preparation-waits').dom.fontStatus,'loading');
const cancelled=fact('cancel-font-wait-restores-preview').dom;assert.equal(cancelled.state,'preview');assert.equal(cancelled.printDisabled,false);assert.match(cancelled.text,/PREVIEW/);
const pending=fact('combined-logo-font-preparation-pending').dom;assert.equal(pending.fontStatus,'loading');assert.equal(pending.images[0].complete,false);
const ready=fact('combined-font-logo-ready-at-print').dom;assert.equal(ready.fontStatus,'loaded');assert.equal(ready.images[0].naturalWidth,64);
for(const f of observations.facts.filter(x=>x.geometry)){
  const g=f.geometry;assert.equal(g.fontStatus,'loaded');assert.match(g.font,/QA Certificate Geist/);assert.equal(g.state,'preview');
  assert.ok(g.documentWidth<=g.width&&g.article.left>=0&&g.article.right<=g.width);assert.equal(g.images[0].complete,true);assert.equal(g.images[0].naturalWidth,64);
  assert.match(g.text,/CERT-SYNTHETIC-001/);assert.match(g.text,/VERIFY-SYNTHETIC-001/);
  for(const e of g.fields)assert.ok(e.left>=g.article.left-1&&e.right<=g.article.right+1,f.check+' '+e.tag);
}
assert.deepEqual(observations.facts.filter(x=>x.geometry).map(x=>x.geometry.width).sort((a,b)=>a-b),[390,1280]);
const log=read('QA/EVIDENCE/logs/phase-2b-certificate-font-regression.log');assert.match(log,/tests 66/);assert.match(log,/pass 66/);assert.match(log,/fail 0/);
const checks=read('QA/EVIDENCE/logs/phase-2b-certificate-font-static-checks.log').trim().split(/\r?\n/).map(JSON.parse);
assert.deepEqual(checks.map(x=>[x.check,x.exitCode]),[['typecheck',0],['lint',0]]);assert.match(checks[1].stdout,/0 errors, 2 warnings/);
const cleanup=json('QA/EVIDENCE/certificate-font-cleanup.json');assert.equal(cleanup.listenersRemaining,0);assert.equal(cleanup.viewport,'RESET');assert.equal(cleanup.browserTab,'CLOSED');assert.equal(cleanup.previousFailedApiStopped,true);
assert.equal(receipt.closure,'OPEN');assert.equal(receipt.azure,'UNCHANGED');assert.equal(receipt.commitPushDeploy,'NOT DONE');
assert.match(read('QA/ISSUES/BUG-FUNC-0032.md'),/\| Status \| OPEN \|/);
assert.match(read('apps/web/src/lib/certificate-font.ts'),/timeoutMs = 10000/);assert.match(read('apps/web/src/app/certificates/page.tsx'),/await waitForCertificateFonts\(document.fonts, controller.signal\)/);
const report='QA/REPORTS/PHASE_2B_CERTIFICATE_FONT_CHECK.md';for(const link of read(report).matchAll(/\]\(([^)]+)\)/g))if(!link[1].startsWith('https://'))assert.ok(fs.existsSync(path.resolve(root,path.dirname(report),link[1])),link[1]);
const diff=cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'});assert.equal(diff.status,0,diff.stdout.slice(0,400));
console.log('PASS certificate font consistency: valid delayed-font race retained, ready/cancel/combined captures, excluded fixture failure, 66 controlled checks, static checks, 390/1280 DOM layout, predecessor/binary preservation and owned QA cleanup. Not PDF/device/full-portal acceptance; issue/Phase2B/release OPEN; Azure unchanged.');
