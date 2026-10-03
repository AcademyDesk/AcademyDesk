const { test } = require('node:test');
const assert = require('node:assert/strict');
const { currentIndex, validateIndex, buildIndex } = require('./semantic-index.cjs');
const index = currentIndex();
test('all inventory categories are indexed without automatic acceptance', () => {
  assert.equal(index.entries.length, index.counts.forms + index.counts.standaloneFields + index.counts.controllerActions);
  assert(index.entries.every(row => ['EVIDENCE-LOCATED', 'NAMED-GAP'].includes(row.reconciliation) && row.runtime === 'NOT RUN'));
  assert.equal(index.entries.filter(row => row.kind === 'form').flatMap(row => row.fieldIndices).length, index.counts.formFields);
  validateIndex(structuredClone(index), currentIndex());
});
for (const [label, change] of [
  ['missing entry', x => x.entries.pop()],
  ['duplicate replacing entry', x => { x.entries[1] = structuredClone(x.entries[0]); }],
  ['stale section', x => x.entries[0].evidence[0].sections.push(999999)],
  ['unreviewed promotion', x => { x.entries[0].reconciliation = 'COMPLETE'; }],
  ['stale source fingerprint', x => { x.sourceFingerprint = 'other'; }]
]) test(`rejects ${label}`, () => {
  const changed = structuredClone(index); change(changed);
  assert.throws(() => validateIndex(changed, index), /incomplete, stale/);
});
test('profile and platform actions resolve to their specific review batches', () => {
  const row = suffix => index.entries.find(entry => entry.id.endsWith(suffix));
  assert.match(row('ProfilesController.Guardian').evidence[0].document, /07_/);
  assert.match(row('ProfilesController.Student').evidence[0].document, /10_/);
  assert.match(row('ProfilesController.Teacher').evidence[0].document, /11_/);
  assert.match(row('PlatformControlController.SaveTenantOnboarding').evidence[0].document, /12_/);
  assert.match(row('PlatformControlController.CreateAnnouncement').evidence[0].document, /13_/);
  assert.match(row('PlatformControlController.CreateInvoice').evidence[0].document, /14_/);
});
test('missing scenario links remain explicit and newly specified forms remain unapproved', () => {
  assert(index.entries.filter(row => !row.testIds.length).every(row => row.scenarioMapping === 'INDIVIDUAL-LINK-NOT-RECORDED'));
  assert.equal(index.counts.namedFormGaps, 0);
  assert.match(index.entries.find(row => row.testIds.includes('FINANCE-FORM-006')).evidence[0].document, /38_/);
  assert.match(index.entries.find(row => row.testIds.includes('LEARNING-FORM-005')).evidence[0].document, /39_/);
  for (const id of ['COMPLIANCE-FORM-001', 'COMPLIANCE-FORM-002']) {
    const row = index.entries.find(entry => entry.testIds.includes(id));
    assert.match(row.evidence[0].document, /40_/);
    assert.equal(row.reconciliation, 'EVIDENCE-LOCATED');
    assert.equal(row.runtime, 'NOT RUN');
  }
});
test('bounded semantic verdicts never imply runtime or Phase 1 acceptance', () => {
  assert.equal(index.acceptance, 'SEE FORMAL DECISION');
  assert.equal(index.formalAcceptance, 'QA/REPORTS/PHASE_1_ACCEPTANCE.md');
  assert.equal(index.counts.semanticReviewed, 21);
  assert.equal(index.entries.filter(row => row.semanticVerdict === 'UNREVIEWED').length, 511);
  assert(index.entries.every(row => row.acceptance === 'NOT ACCEPTED'));
  assert(index.entries.filter(row => row.semanticVerdict === 'STATIC-SPECIFIED').every(row =>
    row.review.runtime === 'NOT RUN' && row.review.pending && /4[12]_/.test(row.review.evidence[0].document)));
  assert(index.entries.find(row => row.testIds.includes('FEES-API-007')).review.issues.includes('BUG-DATA-0055'));
  assert(index.entries.find(row => row.testIds.includes('PAYROLL-API-004')).review.issues.includes('BUG-DATA-0056'));
  assert(index.entries.find(row => row.testIds.includes('PAYROLL-FORM-002')).review.issues.includes('BUG-FUNC-0033'));
});
test('missing evidence headings fail instead of approving a document association', () => {
  const inv = { forms: [{ id: 'f', file: 'x', line: 1 }], fields: [], endpoints: [], ui: [] };
  const progress = { reviewedForms: [{ form: 'f', fields: 0, testId: 'T', document: 'd' }], reviewedStandaloneControls: [], reviewedControllers: [] };
  assert.throws(() => buildIndex(inv, progress, { entries: {} }, () => '# No sections', []), /Missing evidence sections/);
});
