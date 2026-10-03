const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto'), cp = require('node:child_process'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..'), read = file => fs.readFileSync(path.join(root, file), 'utf8').replace(/^\uFEFF/, ''), sha = file => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex');
const prior = JSON.parse(read('QA/REPORTS/PHASE_2B_CERTIFICATE_UI_SOURCE_SNAPSHOT.json'));
const current = JSON.parse(read('QA/REPORTS/PHASE_2B_CERTIFICATE_SQL_PREPARATION_SOURCE_SNAPSHOT.json'));
const intentional = ['QA/00_QA_README.md', 'QA/REPORTS/PHASE_2_START_PLAN.md', 'QA/ISSUES/INDEX.md', 'QA/03_TEST_MATRIX.md', 'QA/ISSUES/BUG-DATA-0046.md', 'QA/tools/SqlHarness/Program.cs', 'QA/tools/SqlHarness/Run-ReconciledPayment.ps1'];
assert.equal(cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), current.commit);
for (const entry of [...current.sources, ...current.evidence, ...current.binaries, ...current.normalBinaries]) assert.equal(sha(entry.file), entry.sha256, entry.file);
for (const entry of [...prior.sources, ...prior.evidence, ...prior.binaries, ...prior.normalBinaries]) if (!intentional.includes(entry.file)) assert.equal(sha(entry.file), entry.sha256, 'Accepted predecessor retained: ' + entry.file);
assert.deepEqual(current.normalBinaries, prior.normalBinaries);
assert.equal(current.beforeSources.length, intentional.length);
for (const entry of current.beforeSources) assert.equal(entry.sha256, prior.sources.find(x => x.file === entry.file).sha256, entry.file);
const checks = current.checks;
assert.equal(checks.buildExit, 0); assert.equal(checks.plannedCases, 62); assert.equal(checks.plannedWrites, 17); assert.equal(checks.runtimePassed, 0);
assert.equal(checks.certificateHttpSql, 'NOT RUN / DOCKER UNAVAILABLE'); assert.equal(checks.backendRerun, false); assert.equal(checks.browserDevice, 'NOT RUN'); assert.equal(checks.closure, 'OPEN');
const first = read('QA/EVIDENCE/logs/phase-2b-certificate-sql-build.log'); assert.match(first, /NETSDK1004/);
for (const file of ['QA/EVIDENCE/logs/phase-2b-certificate-sql-build-final.log', 'QA/EVIDENCE/logs/phase-2b-certificate-sql-build-ready.log']) { assert.match(read(file), /Build succeeded/); assert.match(read(file), /0 Warning\(s\)/); assert.match(read(file), /0 Error\(s\)/); }
const attempt = read('QA/EVIDENCE/logs/phase-2b-certificate-sql.log'); assert.match(attempt, /dockerDesktopLinuxEngine/); assert.match(attempt, /cannot find the file specified/);
assert.doesNotMatch(attempt, /CERTIFICATE CASE|CERTIFICATE REGRESSION PASS|HTTP controls PASS|QA CertificateEnrollment run=/);
const harness = read('QA/tools/SqlHarness/CertificateEnrollmentRegression.cs');
assert.match(harness, /body.GetValueOrDefault\("notes"\)/); assert.match(harness, /CertificateIssued/); assert.match(harness, /POST Certificates/); assert.match(harness, /JsonElement.DeepEquals\(before, after\)/);
assert.match(read('QA/tools/SqlHarness/Program.cs'), /if \(auditMode == "--audit-certificate-enrollment"\) \{ await VerifyCertificateEnrollmentAsync/);
const wrapper = read('QA/tools/SqlHarness/Run-ReconciledPayment.ps1'); assert.match(wrapper, /'CertificateEnrollment' \{ 'certificate-enrollment-sql' \}/); assert.match(wrapper, /COMPLIANCE\|CERTIFICATE/);
assert.match(read('QA/ISSUES/BUG-DATA-0046.md'), /\| Status \| OPEN \|/);
assert.match(read('QA/REPORTS/PHASE_2B_CERTIFICATE_SQL_PREPARATION.md'), /Expected17 successful issuances and45 no-write reads\/denials are intentions,not results/);
for (const file of ['QA/REPORTS/PHASE_2B_CERTIFICATE_SQL_PREPARATION.md', 'QA/ISSUES/BUG-DATA-0046.md']) for (const match of read(file).matchAll(/\]\(([^)]+)\)/g)) { const target = match[1].split('#')[0]; if (target && !/^https?:/.test(target)) assert.ok(fs.existsSync(path.resolve(root, path.dirname(file), target)), target); }
const diff = cp.spawnSync('git', ['diff', '--check'], { cwd: root, encoding: 'utf8' }); assert.equal(diff.status, 0, diff.stdout.slice(0, 500));
console.log('PASS preparation consistency only: guarded62-case certificate HTTP/SQL harness built;Docker provisioning blocked,zero runtime passes. Historical product/UI/tests/evidence/binaries retained. Issue/Phase2B/release OPEN;resume same SQL slice once Docker runs,Sol High. No dev DB/services/Azure/commit/deploy.');
