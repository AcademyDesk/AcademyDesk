const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const read = f => fs.readFileSync(path.join(root, f), 'utf8').replace(/^\uFEFF/, '');
const digest = value => crypto.createHash('sha256').update(value).digest('hex');
const sha = f => digest(fs.readFileSync(path.join(root, f)));
const priorFile = 'QA/REPORTS/PHASE_2B_CERTIFICATE_PRINT_UI_SOURCE_SNAPSHOT.json';
const prior = JSON.parse(read(priorFile));
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_CERTIFICATE_BROWSER_SOURCE_SNAPSHOT.json'));
const intentional = ['apps/web/src/app/globals.css', 'QA/00_QA_README.md', 'QA/REPORTS/PHASE_2_START_PLAN.md', 'QA/ISSUES/INDEX.md', 'QA/03_TEST_MATRIX.md', 'QA/ISSUES/BUG-FUNC-0032.md'];
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], {cwd:root,encoding:'utf8'}).trim(), current.commit);
for (const group of ['sources', 'evidence', 'binaries', 'normalBinaries']) {
  assert.equal(new Set(current[group].map(x => x.file)).size, current[group].length, group);
  for (const entry of current[group]) assert.equal(sha(entry.file), entry.sha256, entry.file);
  for (const entry of prior[group]) if (!intentional.includes(entry.file)) assert.equal(sha(entry.file), entry.sha256, 'Accepted predecessor retained: ' + entry.file);
}
assert.deepEqual(current.binaries, prior.binaries); assert.deepEqual(current.normalBinaries, prior.normalBinaries);
assert.deepEqual(current.beforeSources.map(x => x.file).sort(), intentional.slice().sort());
for (const entry of current.beforeSources) assert.equal(entry.sha256, prior.sources.find(x => x.file === entry.file).sha256);
assert.ok(current.evidence.some(x => x.file === priorFile));
const css = read('apps/web/src/app/globals.css');
const addedRule = /^\.certificates-admin \.certificate-branding input\[type="file"\]\.sr-only \{[^\r\n]*\}\r?\n/gm;
assert.equal([...css.matchAll(addedRule)].length, 1);
assert.equal(digest(css.replace(addedRule, '')), prior.sources.find(x => x.file === 'apps/web/src/app/globals.css').sha256, 'Only one scoped certificate CSS rule added');
assert.match(css, /padding: 0 !important; border: 0; overflow: hidden; clip: rect\(0, 0, 0, 0\); clip-path: inset\(50%\)/);
const checks = JSON.parse(read('QA/EVIDENCE/certificate-browser-verification.json'));
assert.deepEqual(current.checks, checks);
assert.deepEqual(checks.controlledTsx, {print:50,eligibility:48,compliance:45,total:143,exitCode:0});
assert.equal(checks.pdfOutput, 'NOT SAVED OR INSPECTED'); assert.equal(checks.physicalMobile, 'NOT RUN');
assert.equal(checks.backendRerun, false); assert.equal(checks.certificateSqlRerun, false);
assert.equal(checks.closure, 'OPEN'); assert.equal(checks.azure, 'UNCHANGED'); assert.equal(checks.commitPushDeploy, 'NOT DONE');
const observations = JSON.parse(read('QA/EVIDENCE/certificate-browser-observations.json'));
assert.equal(observations.facts.length, 25); assert.equal(observations.facts.filter(x => x.passed).length, 24);
assert.deepEqual(observations.facts.filter(x => !x.passed).map(x => x.check), ['final-390-mobile-layout']);
assert.deepEqual(observations.finalConsoleErrorsWarnings, []);
const fact = name => { const x = observations.facts.find(x => x.check === name); assert.ok(x?.passed, name); return x; };
for (const name of ['draft-guard-real-DOM', 'saved-select-collapses', 'official-real-DOM-identity-and-draft-isolation', 'fresh-revocation-blocks-cached-selection', 'fresh-replacement-blocks-cached-selection', 'failed-preprint-read-preserves-draft-and-unlocks', 'rejected-issue-clears-old-print-selection-no-false-success', 'confirmed-synthetic-issue-selects-returned-ID-not-first-record', 'actual-date-year-month-choice-collapses', 'real-DOM-synthetic-print-throw-restores-preview-and-retry', 'real-DOM-simulated-afterprint-restores-preview-not-native-proof']) fact(name);
const mobile = fact('final-390-mobile-dropdown-and-no-overflow').details;
assert.equal(mobile.viewport, 390); assert.equal(mobile.viewportHeight, 844); assert.ok(mobile.documentWidth <= 390); assert.equal(mobile.logoWidth, 1); assert.equal(mobile.layerHit, 'listbox'); assert.ok(mobile.gap > 0 && mobile.gap < 10); assert.ok(mobile.menu.right <= 390);
const clean = fact('clean-reload-390-mobile-layout').geometry;
assert.equal(clean.width, 390); assert.ok(clean.documentWidth <= 390); assert.equal(clean.logo.width, 1); assert.equal(clean.logo.height, 1); assert.match(clean.text, /CERT-SYNTHETIC-001/);
const desktop = fact('settled-desktop-dropdown-adjacent-and-layered').details;
assert.equal(desktop.viewport, 1280); assert.equal(desktop.layerHit, 'listbox'); assert.ok(desktop.gap > 0 && desktop.gap < 10); assert.ok(Math.abs(desktop.trigger.left - desktop.menu.left) < 2);
const desktopOverflow = fact('desktop-hidden-logo-no-page-overflow').geometry;
assert.equal(desktopOverflow.width, 1280); assert.ok(desktopOverflow.documentWidth <= 1280); assert.equal(desktopOverflow.logo.width, 1);
const isolatedPrint = fact('clean-reload-actual-controls-draft-isolation-at-print').text;
assert.match(isolatedPrint, /Saved learner/i); assert.match(isolatedPrint, /Saved programme/); assert.match(isolatedPrint, /CERT-SYNTHETIC-001/); assert.doesNotMatch(isolatedPrint, /LATER UNSAVED|Other Learner|Other programme/);
const independent = fact('real-DOM-independent-no-notes-saved-print-does-not-use-draft').details;
assert.equal(independent.state, 'issued'); assert.match(independent.text, /Independent programme/); assert.doesNotMatch(independent.text, /LATER UNSAVED|Other programme|Saved recognition/);
const transport = JSON.parse(read('QA/EVIDENCE/certificate-browser-synthetic-transport.json'));
assert.equal(transport.requests.length, 101); assert.equal(transport.prints.length, 6);
assert.ok(transport.prints.some(x => x.mode === 'native')); assert.ok(transport.prints.some(x => x.mode === 'throw'));
assert.ok(transport.prints.some(x => x.event === 'afterprint' && x.mode === 'capture'));
for (const capture of transport.prints.filter(x => x.text)) {
  assert.equal(capture.state, 'issued'); assert.doesNotMatch(capture.text, /LATER UNSAVED|Other Learner/);
  if (capture.mode === 'native') { assert.match(capture.text, /Certificate: CERT-SYNTHETIC-NEW-1/); assert.match(capture.text, /Verification: VERIFY-SYNTHETIC-NEW-1/); }
  else { assert.match(capture.text, /Certificate: CERT-SYNTHETIC-001/); assert.match(capture.text, /Verification: VERIFY-SYNTHETIC-001/); }
}
const cleanup = JSON.parse(read('QA/EVIDENCE/certificate-browser-cleanup.json'));
assert.equal(cleanup.listenersRemaining, 0); assert.equal(cleanup.viewport, 'RESET'); assert.equal(cleanup.browserTab, 'CLOSED'); assert.equal(cleanup.normalDevServices, 'UNCHANGED');
for (const entry of cleanup.copiedProductMatches) assert.equal(entry.sourceSha256, entry.copySha256, entry.file);
const log = read('QA/EVIDENCE/logs/phase-2b-certificate-browser-regression.log');
for (const [label, count] of Object.entries({tests:143,pass:143,fail:0,cancelled:0,skipped:0,todo:0})) assert.match(log, new RegExp('(?:^|\\n)[^\\r\\n]*' + label + ' ' + count + '(?:\\r?\\n|$)'));
const report = read('QA/REPORTS/PHASE_2B_CERTIFICATE_BROWSER_CHECK.md');
assert.match(report, /not 24 independent/); assert.match(report, /No PDF was saved/); assert.match(report, /ThemeProvider error/); assert.match(report, /one retained QA expectation mismatch/);
assert.match(read('QA/ISSUES/BUG-FUNC-0032.md'), /\| Status \| OPEN \|/);
for (const file of ['QA/REPORTS/PHASE_2B_CERTIFICATE_BROWSER_CHECK.md', 'QA/ISSUES/BUG-FUNC-0032.md']) for (const match of read(file).matchAll(/\]\(([^)]+)\)/g)) {
  const target = match[1].split('#')[0]; if (target && !/^https?:/.test(target)) assert.ok(fs.existsSync(path.resolve(root, path.dirname(file), target)), target);
}
const diff = cp.spawnSync('git', ['diff', '--check'], {cwd:root,encoding:'utf8'}); assert.equal(diff.status, 0, diff.stdout.slice(0,500));
console.log('PASS certificate browser evidence: scoped overflow repair, actual synthetic DOM/390/1280 checks, 143 controlled regressions; one retained corrected QA expectation. Product TSX/API/schema/shared controls, predecessor evidence, 96 QA/two normal binaries preserved. PDF/physical-device/full-portal/issue/Phase2B/release OPEN; owned QA cleaned; Azure/commit/deploy unchanged. Next same certificate PDF/layout/device verification, Sol High.');
