const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { checkProductionGates, knownCritical } = require('./check-production-gates.cjs');
function fixture() {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'academydesk-release-gates-'));
  const issues = path.join(root, 'QA/ISSUES'); fs.mkdirSync(issues, { recursive: true });
  const write = (id, status = 'CLOSED', result = 'PASS') =>
    fs.writeFileSync(path.join(issues, `${id}.md`), `# Synthetic issue\n| Status | ${status} |\n| Final verification | ${result} |\n`);
  knownCritical.forEach(id => write(id));
  const register = value => fs.writeFileSync(path.join(issues, 'INDEX.md'), value);
  register(knownCritical.map(id => `| [${id}](${id}.md) | Synthetic | P0 | Critical |`).join('\n'));
  return { root, issues, write, register, cleanup: () => fs.rmSync(root, { recursive: true, force: true }) };
}
test('fully accepted synthetic critical records pass this guard', () => {
  const f = fixture(); try { assert.equal(checkProductionGates(f.root).ok, true); } finally { f.cleanup(); }
});
for (const status of ['OPEN', 'OPEN / FIX-IN-PROGRESS', 'CLOSED / PARTIAL']) test(`critical status ${status} cannot pass`, () => {
  const f = fixture(); try { f.write(knownCritical[0], status); assert.equal(checkProductionGates(f.root).ok, false); } finally { f.cleanup(); }
});
for (const result of ['NOT RUN', 'PASS / SQL pending', 'PASS / partial browser', 'FAIL', '']) test(`critical verification ${result || 'missing'} cannot pass`, () => {
  const f = fixture(); try { f.write(knownCritical[0], 'CLOSED', result); assert.equal(checkProductionGates(f.root).ok, false); } finally { f.cleanup(); }
});
test('removing an open known P0 from the register cannot bypass it', () => {
  const f = fixture(); try { f.register('No open issues listed.'); f.write(knownCritical[0], 'OPEN'); assert.equal(checkProductionGates(f.root).ok, false); } finally { f.cleanup(); }
});
test('new P0 records are automatically required', () => {
  const f = fixture(); try { f.register('| [BUG-SEC-9999](BUG-SEC-9999.md) | New | P0 | Critical |'); f.write('BUG-SEC-9999', 'OPEN'); assert.equal(checkProductionGates(f.root).ok, false); } finally { f.cleanup(); }
});
test('malformed links cannot escape the issue directory', () => {
  const f = fixture(); try { f.register('| [BUG-SEC-9999](../../secret.md) | New | P0 | Critical |'); assert.equal(checkProductionGates(f.root).ok, false); } finally { f.cleanup(); }
});
test('missing critical record fails closed', () => {
  const f = fixture(); try { fs.unlinkSync(path.join(f.issues, knownCritical[0] + '.md')); assert.equal(checkProductionGates(f.root).ok, false); } finally { f.cleanup(); }
});
test('missing register fails closed', () => {
  const f = fixture(); try { fs.unlinkSync(path.join(f.issues, 'INDEX.md')); assert.throws(() => checkProductionGates(f.root)); } finally { f.cleanup(); }
});
