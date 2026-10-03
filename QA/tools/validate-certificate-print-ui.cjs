const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8').replace(/^\uFEFF/, '');
const digest = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const sha = file => digest(fs.readFileSync(path.join(root, file)));
const priorFile = 'QA/REPORTS/PHASE_2B_CERTIFICATE_SQL_SOURCE_SNAPSHOT.json';
const prior = JSON.parse(read(priorFile));
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_CERTIFICATE_PRINT_UI_SOURCE_SNAPSHOT.json'));
const intentional = ['apps/web/src/app/certificates/page.tsx', 'apps/web/src/app/globals.css', 'QA/00_QA_README.md', 'QA/REPORTS/PHASE_2_START_PLAN.md', 'QA/ISSUES/INDEX.md', 'QA/03_TEST_MATRIX.md'];
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
for (const group of ['sources', 'evidence', 'binaries', 'normalBinaries']) {
  assert.equal(new Set(current[group].map(x => x.file)).size, current[group].length, group);
  for (const entry of current[group]) assert.equal(sha(entry.file), entry.sha256, entry.file);
  for (const entry of prior[group]) if (!intentional.includes(entry.file)) assert.equal(sha(entry.file), entry.sha256, 'Accepted predecessor retained: ' + entry.file);
}
assert.deepEqual(current.binaries, prior.binaries); assert.deepEqual(current.normalBinaries, prior.normalBinaries);
assert.equal(current.beforeSources.length, intentional.length);
assert.deepEqual(current.beforeSources.map(x => x.file).sort(), intentional.slice().sort());
for (const entry of current.beforeSources) assert.equal(entry.sha256, prior.sources.find(x => x.file === entry.file).sha256, entry.file);
assert.deepEqual(current.beforeExtra, [{ file: 'QA/ISSUES/BUG-FUNC-0032.md', sha256: '4b41144e2e307e7c084d66d66b2740ef75089303b4543fbfbcddcbbe420341a6' }]);
assert.equal(sha('QA/EVIDENCE/certificate-print-issue-original.md.txt'), current.beforeExtra[0].sha256);
assert.ok(current.evidence.some(x => x.file === priorFile));
const css = fs.readFileSync(path.join(root, 'apps/web/src/app/globals.css'), 'utf8');
const extra = /^(?:\.certificates-admin \.certificate-preview\[data-print-state="preview"\]::after|\.certificates-admin \.certificate-print-watermark|\.certificates-admin \.certificate-record-identity) \{[^\r\n]*\}\r?\n/gm;
assert.equal([...css.matchAll(extra)].length, 3);
assert.equal(digest(css.replace(extra, '')), prior.sources.find(x => x.file === 'apps/web/src/app/globals.css').sha256, 'Only three certificate-scoped CSS rules added');
function totals(name, tests, passed, failed) {
  const log = read('QA/EVIDENCE/logs/phase-2b-certificate-print-ui-' + name + '.log');
  for (const [label, count] of Object.entries({ tests, pass: passed, fail: failed, cancelled: 0, skipped: 0, todo: 0 })) assert.match(log, new RegExp('(?:^|\\n)[^\\r\\n]*' + label + ' ' + count + '(?:\\r?\\n|$)'));
}
totals('baseline', 4, 0, 4); totals('initial', 87, 87, 0);
totals('ready', 143, 143, 0); totals('target-ready', 50, 50, 0);
totals('final', 143, 143, 0); totals('final-corrected', 143, 143, 0); totals('target-final', 50, 50, 0);
const initialTypes = read('QA/EVIDENCE/logs/phase-2b-certificate-print-ui-typecheck-initial.log');
assert.equal((initialTypes.match(/error TS\d+:/g) || []).length, 3); assert.match(initialTypes, /exit=2/);
const types = read('QA/EVIDENCE/logs/phase-2b-certificate-print-ui-typecheck-ready.log'); assert.match(types, /--noEmit --incremental false exit=0/); assert.doesNotMatch(types, /error TS\d+/);
const initialLint = read('QA/EVIDENCE/logs/phase-2b-certificate-print-ui-lint-final.log'); assert.match(initialLint, /react-hooks\/set-state-in-effect/); assert.match(initialLint, /1 error, 2 warnings/);
const lint = read('QA/EVIDENCE/logs/phase-2b-certificate-print-ui-lint-ready.log'); assert.match(lint, /0 errors, 2 warnings/); assert.equal((lint.match(/@next\/next\/no-img-element/g) || []).length, 2);
const verification = JSON.parse(read('QA/EVIDENCE/certificate-print-ui-verification.json'));
assert.deepEqual(current.checks, verification);
assert.deepEqual(verification.controlledTsx, { exitCode: 0, print: 50, reusedEligibility: 48, reusedCompliance: 45, total: 143 });
assert.deepEqual(verification.baseline, { tests: 4, passed: 0, failed: 4 });
assert.equal(verification.backendRerun, false); assert.equal(verification.certificateSqlRerun, false);
for (const key of ['browserDevice', 'nativePrintPdf', 'frontendBuild']) assert.equal(verification[key], 'NOT RUN');
assert.equal(verification.closure, 'OPEN'); assert.equal(verification.azure, 'UNCHANGED'); assert.equal(verification.commitPushDeploy, 'NOT DONE');
const page = read('apps/web/src/app/certificates/page.tsx');
assert.match(page, /onClick=\{printCertificate\}/); assert.doesNotMatch(page, /onClick=\{\(\) => window\.print\(\)\}/);
assert.match(page, /data-print-state=\{printView \? "issued" : "preview"\}/);
assert.match(page, /PREVIEW — NOT AN ISSUED PRINT/); assert.match(page, /certificateNumber/); assert.match(page, /verificationCode/);
assert.match(page, /\["certificates", "students", "batches", "certificates\/branding"\]/); assert.match(page, /cache: "no-store"/);
assert.doesNotMatch(page, /toLocaleDateString|eslint-disable/);
assert.match(read('QA/ISSUES/BUG-FUNC-0032.md'), /\| Status \| OPEN \|/);
const reportFile = 'QA/REPORTS/PHASE_2B_CERTIFICATE_PRINT_UI_REPAIR.md';
assert.match(read(reportFile), /50\/50 PASS/); assert.match(read(reportFile), /143\/143 PASS/); assert.match(read(reportFile), /browser\/PDF verification/);
for (const file of [reportFile, 'QA/ISSUES/BUG-FUNC-0032.md']) for (const match of read(file).matchAll(/\]\(([^)]+)\)/g)) {
  const target = match[1].split('#')[0];
  if (target && !/^https?:/.test(target)) assert.ok(fs.existsSync(path.resolve(root, path.dirname(file), target)), target);
}
const diff = cp.spawnSync('git', ['diff', '--check'], { cwd: root, encoding: 'utf8' }); assert.equal(diff.status, 0, diff.stdout.slice(0, 500));
console.log('PASS saved certificate print UI: 50 print + 48 eligibility + 45 compliance controlled TSX checks; TypeScript and target lint pass (2 inherited image warnings). Predecessor API/schema/security/shared controls/harness/binaries/evidence preserved; 1019 backend/62 SQL not rerun. Real browser/PDF/device and issue/Phase2B/release remain OPEN. Next same certificate browser/PDF verification, Sol High; no Azure/commit/deploy.');
