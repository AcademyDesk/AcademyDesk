// Actual platform page with synthetic transport; SQL/browser acceptance is separate.
const test = require('node:test'), assert = require('node:assert/strict');
const { mount, text, response } = require('./frontend-page-runtime.cjs');
async function shown(groups) {
  const data = { openSupportCases: 0, overdueInvoices: 0, totalBilled: null, collectedBilling: null,
    outstandingBilling: null, recentAudit: [], currencyTotals: groups };
  const h = mount('apps/web/src/app/platform/page.tsx', async url => response(url === '/api/platform/overview' ? data
    : url === '/api/platform/academies' ? [] : url === '/api/auth/session' ? { displayName: 'Synthetic owner' } : {}));
  await h.flush(); return { h, value: text(h.render()) };
}
test('platform mixed currency cards and billing exposure preserve both exact currency units', async () => {
  const { value } = await shown([{ currency: 'INR', totalBilled: 1000.25, collectedBilling: 600.05, outstandingBilling: 400.20 },
    { currency: 'USD', totalBilled: 100.50, collectedBilling: 20.10, outstandingBilling: 80.40 }]);
  for (const amount of ['1,000.25', '600.05', '400.20', '100.50', '20.10', '80.40']) assert.ok(value.includes(amount), amount);
  assert.match(value, /INR/); assert.match(value, /USD/); assert.doesNotMatch(value, /1,100.75|620.15|480.60/);
});
for (const currency of ['USD', 'JPY', 'rupees', 'ZZZ']) test('platform stored unit ' + currency + ' never defaults to INR', async () => {
  const { value } = await shown([{ currency, totalBilled: 123.45, collectedBilling: 20.05, outstandingBilling: 103.40 }]);
  assert.ok(value.includes('123.45')); assert.doesNotMatch(value, /₹\d|INR/); assert.match(value, currency === 'rupees' ? /currency unavailable/i : new RegExp(currency));
});
test('empty platform billing book does not invent currency or money', async () => {
  const { value } = await shown([]); assert.match(value, /No billing activity/); assert.doesNotMatch(value, /₹0|INR/);
});
test('old platform summary without currency groups remains unavailable rather than showing zero', async () => {
  const { value } = await shown(undefined); assert.match(value, /Currency totals unavailable/); assert.doesNotMatch(value, /₹0/);
});
