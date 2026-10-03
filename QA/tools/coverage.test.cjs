const { test } = require('node:test');
const assert = require('node:assert/strict');
const { resolveControl, reconcileCoverage } = require('./coverage.cjs');
const field = (props, line = 1) => ({ file: 'example.tsx', line, tag: 'input', form: '', props });
const entry = { file: 'example.tsx', name: 'q' };
const fixture = () => ({
  inventory: { forms: [{ id: 'example#form-1' }], fields: [
    { ...field({ name: '"inside"' }), form: 'example#form-1' }, field({ name: '"q"' }, 2)
  ], endpoints: [{ controller: 'ExampleController' }] },
  progress: { reviewedForms: [{ form: 'example#form-1', fields: 1 }], reviewedStandaloneControls: [entry], reviewedControllers: ['ExampleController'] }
});
test('legacy name mapping can omit line; totals derive from distinct declarations', () => {
  const { inventory, progress } = fixture();
  assert.deepEqual(reconcileCoverage(inventory, progress), { forms: 1, formFields: 1, standaloneFields: 1, totalFields: 2, controllers: 1 });
});
test('missing control is rejected even when every provided entry is valid', () => {
  const { inventory, progress } = fixture(); progress.reviewedStandaloneControls = [];
  assert.throws(() => reconcileCoverage(inventory, progress), /Unmapped declarations/);
});
test('aliases cannot double-count a single declaration', () => {
  const { inventory, progress } = fixture();
  progress.reviewedStandaloneControls.push({ ...entry, name: 'alias', sourceName: '"q"', line: 2 });
  assert.throws(() => reconcileCoverage(inventory, progress), /same declaration/);
});
test('unnamed CSV input resolves by derived structure and line', () => {
  assert.equal(resolveControl([field({ type: '"file"', accept: '".csv"' }, 17)], {
    file: 'example.tsx', name: 'CSV', line: 17, sourceProperty: 'identity', sourceName: 'input:"file":".csv"'
  }), 0);
});
test('same-line structural collisions fail instead of choosing a control', () => {
  const input = field({ type: '"file"', accept: '".csv"' }, 17);
  assert.throws(() => resolveControl([input, { ...input }], { file: 'example.tsx', name: 'CSV', line: 17, sourceProperty: 'identity', sourceName: 'input:"file":".csv"' }), /expected one declaration/);
});
test('edited props cannot be concealed by a stale identity attribute', () => {
  const input = { ...field({ type: '"file"', accept: '".png"' }, 17), identity: 'input:"file":".csv"' };
  assert.throws(() => resolveControl([input], { file: 'example.tsx', name: 'CSV', line: 17, sourceProperty: 'identity', sourceName: input.identity }), /expected one declaration/);
});
test('stale line and unknown properties are rejected', () => {
  assert.throws(() => resolveControl([field({ value: '{q}' })], { ...entry, line: 2, sourceProperty: 'value', sourceName: '{q}' }), /expected one declaration/);
  assert.throws(() => resolveControl([], { ...entry, sourceProperty: 'anything' }), /Unsupported property/);
});
test('missing forms or controllers fail completeness', () => {
  const { inventory, progress } = fixture();
  assert.throws(() => reconcileCoverage(inventory, { ...progress, reviewedForms: [] }), /Missing or extra form/);
  assert.throws(() => reconcileCoverage(inventory, { ...progress, reviewedControllers: [] }), /Missing or extra controller/);
});
