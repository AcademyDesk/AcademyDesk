// Consistency checks, not an extra runtime business-test count.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8').replace(/^\uFEFF/, '');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const sha = file => hash(fs.readFileSync(path.join(root, file)));
const receipt = JSON.parse(read('QA/EVIDENCE/certificate-appearance-source-receipt.json'));
const accepted = JSON.parse(read(receipt.predecessor));
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], {cwd:root,encoding:'utf8'}).trim(), receipt.commit);
assert.deepEqual(receipt.intentionalBefore.map(x=>x.file).sort(), ['apps/web/src/app/certificates/page.tsx','apps/web/src/app/globals.css','QA/ISSUES/BUG-FUNC-0032.md'].sort());
for (const entry of receipt.intentionalBefore) assert.equal(entry.sha256, accepted.sources.find(x=>x.file===entry.file).sha256);
for (const group of ['sources','evidence','binaries','normalBinaries']) for (const entry of accepted[group]) {
  if (!receipt.intentionalBefore.some(x=>x.file===entry.file)) assert.equal(sha(entry.file), entry.sha256, 'Accepted predecessor retained: '+entry.file);
}
for (const entry of receipt.current) assert.equal(sha(entry.file),entry.sha256,entry.file);
for (const entry of receipt.copiedProduct) assert.equal(sha(entry.file), sha(entry.copy), entry.file+' QA copy mismatch');
const css=read('apps/web/src/app/globals.css');
const rules=/^\.certificates-admin \.certificate-preview\.theme-(?:music-recital footer(?: span:last-child)?|holiday-eid(?: \.certificate-print-watermark)?) \{[^\r\n]*\}\r?\n/gm;
assert.equal([...css.matchAll(rules)].length,4);
assert.equal(hash(css.replace(rules,'')),accepted.sources.find(x=>x.file==='apps/web/src/app/globals.css').sha256,'Only four scoped theme rules added');
const baseline=JSON.parse(read('QA/EVIDENCE/certificate-appearance-baseline.json'));
assert.equal(baseline.prints.length,1); const initial=baseline.prints[0], initialImage=initial.images[0];
assert.equal(initialImage.complete,false); assert.equal(initialImage.naturalWidth,0);
assert.ok(initial.at < baseline.images.find(x=>initialImage.src.endsWith(x.path)).servedAt);
const transport=JSON.parse(read('QA/EVIDENCE/certificate-appearance-transport.json'));
assert.equal(transport.prints.length,3); const repaired=transport.prints[1];
assert.equal(repaired.state,'issued'); assert.equal(repaired.images[0].complete,true); assert.equal(repaired.images[0].naturalWidth,64);
assert.ok(repaired.at >= transport.images.find(x=>repaired.images[0].src.endsWith(x.path)).servedAt);
assert.equal(transport.prints[2].images.length,0); assert.equal(transport.prints[2].state,'issued');
for(const image of transport.images.filter(x=>x.broken || x.path.includes('delayed-5'))) assert.ok(!transport.prints.some(x=>x.images.some(i=>i.src.endsWith(image.path))),'Broken/cancelled image never printed');
assert.deepEqual(transport.uploads.map(x=>x.accepted),[true,true,false]); assert.equal(transport.uploads[2].type,'text/plain');
const observed=JSON.parse(read('QA/EVIDENCE/certificate-appearance-observations.json'));
assert.equal(observed.facts.length,15);
const fact=name=>{const f=observed.facts.find(x=>x.check===name);assert.ok(f,name);return f;};
const failure=fact('broken-logo-unlocks-watermarked-preview').dom;
assert.equal(failure.state,'preview'); assert.equal(failure.printEnabled,true); assert.match(failure.alerts.join(' '),/Nothing was printed/);
assert.equal(fact('cancel-delayed-preparation').dom.state,'preview');
assert.match(fact('PNG-upload-success-notice').dom.statuses.join(' '),/Academy logo uploaded/);
const rejected=fact('invalid-upload-preserves-logo-and-resets-picker').dom;
assert.equal(rejected.fileInputValue,''); assert.match(rejected.statuses.join(' '),/Upload a PNG/); assert.equal(rejected.brandingImages[0].naturalWidth,64);
for(const f of observed.facts.filter(x=>x.geometry)){
  const g=f.geometry; assert.ok(g.documentWidth<=g.width,f.check+' page overflow'); assert.ok(g.article.left>=0&&g.article.right<=g.width,f.check+' article outside viewport');
  assert.equal(g.state,'preview'); assert.equal(g.image[0].complete,true); assert.equal(g.image[0].naturalWidth,64);
  assert.match(g.text,/Certificate: CERT-SYNTHETIC/); assert.match(g.text,/Verification: VERIFY-SYNTHETIC/);
  for(const e of g.fields)assert.ok(e.left>=g.article.left-1&&e.right<=g.article.right+1,f.check+' '+e.tag+' outside article');
}
for(const width of [320,390,1280]){
  const g=fact('music-recital-'+width+'-after-contrast-repair').geometry;
  const footer=g.fields.find(x=>x.tag==='FOOTER'); assert.equal(footer.background,'rgb(255, 248, 251)');assert.equal(footer.color,'rgb(64, 81, 106)');
}
assert.deepEqual(fact('holiday-eid-390-after-contrast-repair').paint,{backgroundImage:'none',watermarkColor:'rgb(245, 223, 142)'});
const log=read('QA/EVIDENCE/logs/phase-2b-certificate-appearance-regression.log');
assert.match(log,/tests 58/);assert.match(log,/pass 58/);assert.match(log,/fail 0/);
assert.equal(JSON.parse(read('QA/EVIDENCE/logs/phase-2b-certificate-appearance-typescript-result.log').trim()).exitCode,0);
assert.match(read('QA/EVIDENCE/logs/phase-2b-certificate-appearance-lint.log'),/0 errors, 2 warnings/);
const cleanup=JSON.parse(read('QA/EVIDENCE/certificate-appearance-cleanup.json'));
assert.equal(cleanup.listenersRemaining,0);assert.equal(cleanup.viewport,'RESET');assert.equal(cleanup.browserTab,'CLOSED');
assert.equal(receipt.closure,'OPEN');assert.equal(receipt.azure,'UNCHANGED');assert.equal(receipt.commitPushDeploy,'NOT DONE');
assert.match(read('QA/ISSUES/BUG-FUNC-0032.md'),/\| Status \| OPEN \|/);
const report='QA/REPORTS/PHASE_2B_CERTIFICATE_APPEARANCE_CHECK.md';
for(const link of read(report).matchAll(/\]\(([^)]+)\)/g)) assert.ok(fs.existsSync(path.resolve(root,path.dirname(report),link[1])),link[1]);
const diff=cp.spawnSync('git',['diff','--check'],{cwd:root,encoding:'utf8'}); assert.equal(diff.status,0,diff.stdout.slice(0,400));
console.log('PASS certificate appearance consistency: retained race baseline, delayed/broken/cancelled/no-logo DOM checks, scoped theme repairs, 58 controlled checks, TypeScript/lint, predecessor/binary preservation and owned QA cleanup. Not native PDF/device/security acceptance; issue/Phase2B/release OPEN; Azure unchanged.');
