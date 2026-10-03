/* Evidence locator only. Never converts declaration coverage into semantic approval. */
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { reconcileCoverage, resolveControl } = require('./coverage.cjs');
const root = path.resolve(__dirname, '../..');
const output = 'QA/COVERAGE/semantic-index.json';
const read = file => fs.readFileSync(path.join(root, file), 'utf8');
const json = file => JSON.parse(read(file));

function buildIndex(inv, progress, registry, readDocument, coverageFiles, verdictRows = []) {
  const counts = reconcileCoverage(inv, progress);
  const documents = {};
  const batch = number => {
    const matches = coverageFiles.filter(file => file.startsWith(`QA/COVERAGE/${number}_`));
    assert.equal(matches.length, 1, `Expected one Batch ${number}`);
    return matches[0];
  };
  const references = files => [...new Set(files)].map(file => {
    if (!documents[file]) {
      const lines = readDocument(file).split(/\r?\n/);
      documents[file] = lines.flatMap((line, i) => /^## /.test(line) ? [{ line: i + 1, heading: line.slice(3) }] : []);
      assert(documents[file].length, `Missing evidence sections: ${file}`);
    }
    return { document: file, sections: documents[file].map(section => section.line) };
  });
  const formGaps = {};
  const base = (id, kind, source, testIds, files, gap = null) => ({
    id, kind, source, testIds, evidence: references(files),
    reconciliation: gap ? 'NAMED-GAP' : 'EVIDENCE-LOCATED',
    remaining: gap || 'Reuse the referenced audit evidence; reopen only a specific omission or source change. Formal Phase 1 acceptance is recorded separately.',
    scenarioMapping: testIds.length ? 'EXPLICIT' : 'INDIVIDUAL-LINK-NOT-RECORDED',
    runtime: 'NOT RUN'
  });
  const forms = inv.forms.map(form => {
    const review = progress.reviewedForms.find(entry => entry.form === form.id);
    const docs = [review.document];
    if (['FINANCE-FORM-001', 'FINANCE-FORM-006', 'OPERATIONS-FORM-001'].includes(review.testId)) docs.unshift(batch('38'));
    if (['LEARNING-FORM-003', 'LEARNING-FORM-004', 'LEARNING-FORM-005', 'LEARNING-FORM-006'].includes(review.testId)) docs.unshift(batch('39'));
    if (['COMPLIANCE-FORM-001', 'COMPLIANCE-FORM-002'].includes(review.testId)) docs.unshift(batch('40'));
    return { ...base(form.id, 'form', { file: form.file, line: form.line }, [review.testId], docs, formGaps[review.testId]),
      fieldIndices: inv.fields.flatMap((field, index) => field.form === form.id ? [index] : []) };
  });
  const controls = progress.reviewedStandaloneControls.map(entry => {
    const index = resolveControl(inv.fields, entry);
    const field = inv.fields[index];
    const docs = [entry.document];
    if (field.file === 'apps/web/src/app/teachers/page.tsx') docs.unshift(batch('35'));
    if (['apps/web/src/app/expenses/page.tsx', 'apps/web/src/app/work-queue/page.tsx'].includes(field.file)) docs.unshift(batch('38'));
    if (['apps/web/src/app/music/page.tsx', 'apps/web/src/app/practice-logs/page.tsx'].includes(field.file)) docs.unshift(batch('39'));
    return { ...base(`control:${index}`, 'standalone-control', { file: field.file, line: field.line }, entry.testIds || [], docs),
      fieldIndex: index, label: entry.name };
  }).sort((a, b) => a.fieldIndex - b.fieldIndex);
  const actions = inv.endpoints.map(endpoint => {
    const { controller, name, method, route, file, line } = endpoint;
    const id = `${method} ${route}::${controller}.${name}`;
    assert(registry.entries[id], `Missing scenario: ${id}`);
    let docs = [progress.controllerDocuments?.[controller] || progress.controllerDocument];
    if (controller === 'ProfilesController') docs = [batch(name.includes('Guardian') ? '07' : name.includes('Student') ? '10' : '11')];
    if (controller === 'PlatformControlController') docs = [batch(/TenantOnboarding/.test(name) ? '12' : /Invoice|SupportCase/.test(name) ? '14' : '13')];
    if (['CertificatesController', 'AuditLogsController', 'AcademyExportsController', 'ExpensesController', 'AdminWorkItemsController'].includes(controller)) docs.unshift(batch('36'));
    if (['ExpensesController', 'AdminWorkItemsController', 'FinanceGovernanceController'].includes(controller)) docs.unshift(batch('38'));
    if (['MusicPiecesController', 'MusicProgressController', 'PracticeLogsController', 'LearningResourcesController', 'StudentImportsController', 'DashboardController', 'AdminIntelligenceController', 'WeatherForecastController'].includes(controller)) docs.unshift(batch('37'));
    if (['MusicPiecesController', 'MusicProgressController', 'PracticeLogsController', 'LearningResourcesController'].includes(controller)) docs.unshift(batch('39'));
    if (controller === 'CertificatesController') docs.unshift(batch('40'));
    if ((controller === 'TeachersController' || controller === 'BatchesController') && name === 'Update') docs.unshift(batch('35'));
    return base(id, 'controller-action', { file, line }, [registry.entries[id]], docs);
  });
  const located = [...forms, ...controls, ...actions];
  assert.equal(new Set(verdictRows.map(row => row.testId)).size, verdictRows.length, 'Duplicate semantic verdict Test ID');
  for (const review of verdictRows) {
    assert.equal(review.state, 'STATIC-SPECIFIED', `Invalid semantic verdict: ${review.testId}`);
    assert(review.pending && review.document && Array.isArray(review.issues), `Incomplete semantic verdict: ${review.testId}`);
    assert.equal(located.filter(row => row.testIds.includes(review.testId)).length, 1, `Missing or ambiguous semantic verdict row: ${review.testId}`);
  }
  const reviews = new Map(verdictRows.map(row => [row.testId, row]));
  const entries = located.map(row => {
    const review = row.testIds.map(id => reviews.get(id)).find(Boolean);
    return { ...row, semanticVerdict: review ? 'STATIC-SPECIFIED' : 'UNREVIEWED',
      acceptance: 'NOT ACCEPTED',
      ...(review ? { review: { pending: review.pending, issues: review.issues,
        evidence: references([review.document]), runtime: 'NOT RUN' } } : {}) };
  });
  assert.equal(new Set(entries.map(entry => entry.id)).size, entries.length, 'Duplicate index key');
  return {
    version: 1, purpose: 'Evidence locator, NOT semantic approval or application testing',
    sourceFingerprint: progress.sourceFingerprint, acceptance: 'SEE FORMAL DECISION',
    formalAcceptance: 'QA/REPORTS/PHASE_1_ACCEPTANCE.md',
    verdictScope: 'Per-row verdicts are supplemental Batch 41/42 checks. UNREVIEWED means absent from that supplemental registry, not absent from the original audit. Row acceptance is not the Phase 1 handoff decision.',
    counts: { ...counts, controllerActions: actions.length, indexRows: entries.length,
      namedFormGaps: forms.filter(row => row.reconciliation === 'NAMED-GAP').length,
      semanticReviewed: entries.filter(row => row.semanticVerdict === 'STATIC-SPECIFIED').length,
      standaloneWithoutIndividualScenarioLink: controls.filter(row => !row.testIds.length).length },
    exclusions: { frameworkManifest: progress.frameworkManifest,
      uiDeclarations: inv.ui.length, uiStateGrouping: 'Workflow/component grouping recorded in Batches 43-45; exhaustive dynamic-branch enumeration is not certified',
      partialInteractions: progress.partialInteractions },
    documents, entries
  };
}

function currentIndex() {
  const verdicts = json('QA/COVERAGE/semantic-verdicts.json');
  assert.equal(verdicts.version, 1);
  for (const row of verdicts.rows) for (const id of row.issues)
    assert(fs.existsSync(path.join(root, `QA/ISSUES/${id}.md`)), `Unknown verdict issue: ${id}`);
  return buildIndex(json('QA/INVENTORY/source-inventory.json'), json('QA/COVERAGE/progress.json'), json('QA/registry.json'), read,
    fs.readdirSync(path.join(root, 'QA/COVERAGE')).filter(file => file.endsWith('.md')).map(file => `QA/COVERAGE/${file}`), verdicts.rows);
}
function validateIndex(stored, expected) {
  assert.deepEqual(stored, expected, 'Evidence index is incomplete, stale, or changes an unreviewed verdict; regenerate and review');
}
if (require.main === module) {
  const result = currentIndex();
  fs.writeFileSync(path.join(root, output), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify({ purpose: result.purpose, acceptance: result.acceptance, counts: result.counts }, null, 2));
}
module.exports = { buildIndex, currentIndex, validateIndex };
