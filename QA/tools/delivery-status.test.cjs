const test = require('node:test'), assert = require('node:assert/strict');
const { summarize } = require('./delivery-status.cjs');
const issue = (id, priority='P1', status='OPEN') => ({ name: id + '.md',
  text: `# ${id} — Synthetic title\n\n| Status | ${status} |\n| Priority | ${priority} |\n` });
test('compound authentication IDs remain distinct rather than collapsing into undefined', () => {
  const report = summarize([issue('BUG-AUTH-REFRESH-001'), issue('BUG-AUTH-REVOCATION-001',
    'P1 — broader scenario is a P0 release gate', 'FIXED LOCALLY / BOUNDED RETEST PASS — broader acceptance OPEN')],
    '[BUG-AUTH-REFRESH-001](one.md) [BUG-AUTH-REVOCATION-001](two.md)');
  assert.equal(report.records, 2); assert.equal(report.unique_ids, 2);
  assert.deepEqual(report.priorities, { P0: 0, P1: 2, P2: 0, P3: 0 });
  assert.equal(report.auto_closed, 0); assert.equal(report.production_approved, false);
  assert.deepEqual(report.missing_from_index, []);
});
test('missing and orphaned index entries are explicit, not silently treated as work complete', () => {
  const report = summarize([issue('BUG-DATA-0001', 'P0')], '[BUG-SEC-0001](x.md)');
  assert.deepEqual(report.missing_from_index, ['BUG-DATA-0001']);
  assert.deepEqual(report.index_without_issue, ['BUG-SEC-0001']);
  assert.deepEqual(report.p0_ids, ['BUG-DATA-0001']);
});
test('local-pass prose never grants closure or production acceptance', () => {
  const report = summarize([issue('BUG-SEC-0001', 'P0', 'FIXED LOCALLY; physical and critical OPEN')], '');
  assert.equal(report.records, 1); assert.equal(report.auto_closed, 0);
  assert.equal(report.production_approved, false);
});
test('duplicate identities, filename mismatches and unmapped metadata fail visibly', () => {
  const row = issue('BUG-DATA-0001');
  assert.throws(() => summarize([row, row], ''), /Duplicate/);
  assert.throws(() => summarize([{ ...row, name: 'BUG-DATA-0002.md' }], ''), /Unmapped/);
  assert.throws(() => summarize([{ ...row, text: '# BUG-DATA-0001' }], ''), /Unmapped/);
});
test('inventory digest is deterministic and changes with source checkpoints', () => {
  const first = issue('BUG-DATA-0001'), second = issue('BUG-UI-0001', 'P2');
  const a = summarize([first, second], ''), b = summarize([second, first], '');
  assert.equal(a.source_manifest_sha256, b.source_manifest_sha256);
  assert.notEqual(a.source_manifest_sha256, summarize([{ ...first, text: first.text + '\nNew checkpoint' }, second], '').source_manifest_sha256);
});
