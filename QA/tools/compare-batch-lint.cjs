// Compare existing findings without suppressing lint or claiming a clean gate.
const fs = require('node:fs'), cp = require('node:child_process'), path = require('node:path'), crypto = require('node:crypto');
const { ESLint } = require('../../apps/web/node_modules/eslint');
const root = path.resolve(__dirname, '../..'), web = path.join(root, 'apps/web');
const sha = value => crypto.createHash('sha256').update(value).digest('hex');
(async () => {
  const eslint = new ESLint({ cwd: web }), before = [];
  const baseline = JSON.parse(fs.readFileSync(path.join(root, 'QA/REPORTS/PHASE_2B_BATCH_PRESERVATION_BEFORE_SOURCE_SNAPSHOT.json')));
  const files = ['src/app/batches/page.tsx', 'src/app/teachers/page.tsx'];
  for (const file of files) {
    const source = cp.execFileSync('git', ['show', 'HEAD:apps/web/' + file], { cwd: root, encoding: 'utf8' });
    const expected = baseline.records.find(x => x.file === 'apps/web/' + file).sha256;
    if (sha(source) !== expected && sha(source.replace(/\r?\n/g, '\r\n')) !== expected) throw Error('HEAD does not match pre-edit source: ' + file);
    before.push(...await eslint.lintText(source, { filePath: file }));
  }
  const after = await eslint.lintFiles([...files, 'src/lib/batch-update.ts']);
  const norm = rows => rows.flatMap(x => x.messages.map(m => ({
    ruleId: m.ruleId, severity: m.severity,
    // React lint appends absolute source excerpts with shifted line numbers.
    // Retain rule/reason and the offending loader, not incidental line offsets.
    message: m.message.split('\n\nD:')[0], trigger: m.message.match(/void (load\w+)\(/)?.[1] ?? null,
  }))).sort((a, b) => JSON.stringify(a).localeCompare(JSON.stringify(b)));
  const summary = rows => ({ errors: rows.reduce((s, x) => s + x.errorCount, 0), warnings: rows.reduce((s, x) => s + x.warningCount, 0) });
  const sameDiagnostics = JSON.stringify(norm(before)) === JSON.stringify(norm(after));
  console.log(JSON.stringify({ before: summary(before), after: summary(after), sameDiagnostics,
    helper: summary(after.filter(x => x.filePath.endsWith('batch-update.ts'))), beforeMessages: norm(before), afterMessages: norm(after) }, null, 2));
  if (!sameDiagnostics) process.exitCode = 1;
})().catch(error => { console.error(error.message); process.exitCode = 1; });
