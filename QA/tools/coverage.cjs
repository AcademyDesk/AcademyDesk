/* Audit declaration accounting only; no application execution. */
const assert = require('node:assert/strict');

function resolveControl(fields, entry) {
  const property = entry.sourceProperty || 'name';
  assert(['name', 'value', 'checked', 'ref', 'identity'].includes(property), `Unsupported property: ${property}`);
  if (property !== 'name') {
    assert(Number.isInteger(entry.line) && entry.line > 0, 'Source line required');
    assert(typeof entry.sourceName === 'string' && entry.sourceName.length, 'Source expression required');
  }
  const expected = entry.sourceName || JSON.stringify(entry.name);
  const matches = fields.flatMap((field, index) => {
    const actual = property === 'identity'
      ? `${field.tag}:${field.props.type || field.tag}:${field.props.accept || ''}`
      : field.props[property];
    return field.file === entry.file && !field.form && actual === expected &&
      (entry.line === undefined || field.line === entry.line) ? [index] : [];
  });
  assert.equal(matches.length, 1, `${entry.file}:${entry.name}:${entry.line ?? ''}: expected one declaration`);
  return matches[0];
}

function reconcileCoverage(inventory, progress) {
  const reviewedForms = new Set(progress.reviewedForms.map(form => form.form));
  assert.equal(reviewedForms.size, progress.reviewedForms.length, 'Duplicate reviewed form');
  assert.deepEqual([...reviewedForms].sort(), inventory.forms.map(form => form.id).sort(), 'Missing or extra form');
  const represented = new Map();
  for (const entry of progress.reviewedStandaloneControls) {
    const index = resolveControl(inventory.fields, entry);
    assert(!represented.has(index), `Two mappings target the same declaration: ${entry.file}:${entry.line}`);
    represented.set(index, entry);
  }
  const missing = inventory.fields.filter((field, index) => !field.form && !represented.has(index));
  assert.equal(missing.length, 0, `Unmapped declarations: ${missing.map(field => `${field.file}:${field.line}`).join(', ')}`);
  const formFields = inventory.fields.filter(field => field.form && reviewedForms.has(field.form)).length;
  assert.equal(formFields, progress.reviewedForms.reduce((sum, form) => sum + form.fields, 0), 'Incorrect form-field total');
  assert.equal(formFields + represented.size, inventory.fields.length, 'Control totals differ');
  const controllers = new Set(progress.reviewedControllers);
  assert.equal(controllers.size, progress.reviewedControllers.length, 'Duplicate controller');
  assert.deepEqual([...controllers].sort(), [...new Set(inventory.endpoints.map(endpoint => endpoint.controller))].sort(), 'Missing or extra controller');
  return { forms: reviewedForms.size, formFields, standaloneFields: represented.size, totalFields: inventory.fields.length, controllers: controllers.size };
}

module.exports = { resolveControl, reconcileCoverage };
