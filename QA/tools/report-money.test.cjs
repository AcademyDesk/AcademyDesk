const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript');
function load(file) {
  const exports = {};
  const code = ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: {
    target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS,
  } }).outputText;
  new Function('require', 'exports', code)(name => { assert.equal(name, '@/lib/expense-money');
    return load('apps/web/src/lib/expense-money.ts'); }, exports);
  return exports;
}
const { reportMoney, reportBalanceIsOpen, reportDecimal } = load('apps/web/src/lib/report-money.ts');
test('presentation sums cents, not binary fractions or foreign exchange', () => {
  assert.match(reportMoney([{ amount: 0.1, currency: 'INR' }, { amount: 0.2, currency: 'INR' }]), /INR ₹0\.30/);
  const mixed = reportMoney([{ amount: 1, currency: ' usd ' }, { amount: 2, currency: 'INR' }]);
  assert.match(mixed, /INR .*2\.00 · USD .*1\.00/); assert.doesNotMatch(mixed, /3\.00/);
  assert.match(reportMoney([{ amount: '100.01', currency: 'INR' }], [{ amount: '1.00', currency: 'USD' }]), /INR .*100\.01 · USD -.*1\.00/);
});
test('large exact wire values and negative display subtotals remain exact', () => {
  assert.match(reportMoney([{ amount: '9999999999999999.99', currency: 'INR' }]), /99,99,99,99,99,99,999\.99/);
  assert.match(reportMoney([], [{ amount: '1.01', currency: 'INR' }]), /-₹1\.01/);
  assert.equal(reportDecimal('9999999999999999.99'), '9999999999999999.99');
  assert.equal(reportDecimal('-0.01'), '-0.01'); assert.equal(reportDecimal(0), '0.00');
});
for (const value of [10000000000000000, NaN, Infinity, '1e3', '1.001', 'not-money', undefined, null]) {
  test(`missing/unsafe amount ${String(value)} is unavailable, never zero or rounded`, () => {
    assert.equal(reportMoney([{ amount: value, currency: 'INR' }]), 'Amount unavailable');
    assert.equal(reportDecimal(value), 'Amount unavailable');
    assert.equal(reportBalanceIsOpen(value), true);
  });
}
for (const currency of ['', null, undefined, 'invalid', 'INR<script>']) {
  test(`invalid currency ${String(currency)} never falls back to INR`, () => {
    assert.equal(reportMoney([{ amount: 10, currency }]), 'Amount unavailable');
  });
}
test('settled balances and empty datasets are explicit', () => {
  assert.equal(reportBalanceIsOpen(0), false); assert.equal(reportBalanceIsOpen('0.00'), false);
  assert.equal(reportBalanceIsOpen('0.01'), true);
  assert.equal(reportMoney([]), 'No amounts recorded');
});
