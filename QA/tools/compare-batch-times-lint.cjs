// Retain existing diagnostics; no suppression and no claim of a clean lint gate.
const fs = require('node:fs'), crypto = require('node:crypto'), assert = require('node:assert/strict');
const { ESLint } = require('../../apps/web/node_modules/eslint');
const before = JSON.parse(fs.readFileSync('QA/EVIDENCE/logs/phase-2b-batch-times-before-lint.json', 'utf8').replace(/^\uFEFF/, ''));
const expected = JSON.parse(fs.readFileSync('QA/REPORTS/PHASE_2B_BATCH_TIMES_BEFORE_SOURCE_SNAPSHOT.json')).records.find(x => x.file.endsWith('page.tsx')).sha256;
const hash = text => crypto.createHash('sha256').update(text).digest('hex');
assert.ok([hash(before[0].source), hash(before[0].source.replace(/\r?\n/g, '\r\n'))].includes(expected), 'lint baseline source matches exact pre-edit hash');
const norm = rows => rows.flatMap(x => x.messages.map(m => ({ ruleId: m.ruleId, severity: m.severity,
  message: m.message.split('\n\nD:')[0], trigger: m.message.match(/void (load\w+)\(/)?.[1] ?? null }))).sort((a, b) => JSON.stringify(a).localeCompare(JSON.stringify(b)));
const summary = rows => ({ errors: rows.reduce((s, x) => s + x.errorCount, 0), warnings: rows.reduce((s, x) => s + x.warningCount, 0) });
(async () => {
  const after = await new ESLint({ cwd: require('node:path').resolve('apps/web') }).lintFiles(['src/app/batches/page.tsx']);
  const sameDiagnostics = JSON.stringify(norm(before)) === JSON.stringify(norm(after));
  console.log(JSON.stringify({ before: summary(before), after: summary(after), sameDiagnostics, beforeMessages: norm(before), afterMessages: norm(after) }, null, 2));
  assert.ok(sameDiagnostics);
})().catch(error => { console.error(error.message); process.exitCode = 1; });
