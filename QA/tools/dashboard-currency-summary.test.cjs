// Actual dashboard TSX with synthetic responses; not live Identity/SQL acceptance.
const { test } = require('node:test'), a = require('node:assert/strict');
const { mount, nodes, response } = require('./frontend-page-runtime.cjs');
const totals = (currency, outstanding) => ({ currency, grossBilled: 1000, approvedAdjustments: 200, collected: 400, outstanding, reconciled: 100 });
async function page(summary, invoices = [], payroll = []) {
  const calls = [], h = mount('apps/web/src/app/dashboard/page.tsx', async (url, init = {}) => {
    calls.push({ url, method: init.method || 'GET' });
    return response(url === '/api/academies' ? [{ id: 'a' }] : url.endsWith('/dashboard') ? { activeStudents: 2, classesToday: 0, ...summary }
      : url === '/api/auth/session' ? { displayName: 'Synthetic Admin' } : url.endsWith('/invoices') ? invoices : url.endsWith('/payroll/payouts') ? payroll : []);
  });
  await h.flush();
  const tile = () => nodes(h.render()).find(n => n.type === 'tile' && n.props.label === 'Outstanding fees');
  const lists = () => { tile().props.onClick(); return nodes(h.render()).filter(n => n.type?.name === 'DetailList').map(n => n.props); };
  return { h, calls, tile, lists };
}
const invoice = (changes = {}) => ({ id: 'i', studentId: 's', invoiceNumber: 'FEE-1', totalAmount: 100, paidAmount: 10, adjustedAmount: 20, balance: 70, currency: 'USD', status: 'Issued', ...changes });
const payout = (changes = {}) => ({ id: 'p', workerName: 'Synthetic Staff', periodLabel: 'October', netAmount: 999.55, currency: 'EUR', status: 'Approved', ...changes });
for (const [name, summary, expected] of [
  ['mixed', { byCurrency: [totals('INR', 400.25), totals('USD', 40.15)], outstandingBalance: 999999 }, /INR.*400\.25.*USD.*40\.15/],
  ['USD only', { byCurrency: [totals('USD', 70.15)] }, /USD.*70\.15/],
  ['INR only', { byCurrency: [totals('INR', 70.15)] }, /INR.*70\.15/],
  ['empty', { byCurrency: [] }, /^0\.00$/],
  ['legacy', { outstandingBalance: 1100 }, /^Currency details unavailable$/],
  ['duplicate units', { byCurrency: [totals('USD', 10), totals(' usd ', 20)] }, /^Currency details unavailable$/],
  ['nonfinite', { byCurrency: [totals('USD', NaN)] }, /^Currency details unavailable$/],
]) test(`${name}: fee tile uses grouped receivables and excludes payroll`, async () => {
  const f = await page(summary, [], [payout()]); try { a.match(f.tile().props.value, expected); a.doesNotMatch(f.tile().props.value, /999|NaN|₹/); a.ok(f.calls.every(c => c.method === 'GET')); } finally { f.h.unmount(); }
});
test('invoice detail uses authoritative adjusted balance and actual currency with cents', async () => {
  const f = await page({ byCurrency: [totals('USD', 70.15)] }, [invoice({ balance: 70.15 })]);
  try { const fees = f.lists()[0]; a.equal(fees.rows.length, 1); a.match(fees.rows[0][1], /FEE-1.*USD.*70\.15 due/); a.doesNotMatch(fees.rows[0][1], /90\.00|₹/); } finally { f.h.unmount(); }
});
for (const status of ['Cancelled', 'cancelled', 'CANCELLED']) test(`${status}: invoice detail excludes cancelled receivables`, async () => {
  const f = await page({ byCurrency: [] }, [invoice({ status })]); try { a.equal(f.lists()[0].rows.length, 0); } finally { f.h.unmount(); }
});
test('zero, overpaid, missing and nonfinite balances are never recomputed from totals', async () => {
  const f = await page({ byCurrency: [] }, [0, -20, undefined, NaN].map(balance => invoice({ balance })));
  try { a.equal(f.lists()[0].rows.length, 0); } finally { f.h.unmount(); }
});
test('payroll remains separately labelled, preserves raw status selection and real currency', async () => {
  const f = await page({ byCurrency: [totals('USD', 70)] }, [], [payout(), payout({ id: 'paid', status: 'Paid' })]);
  try { const list = f.lists()[1]; a.equal(list.rows.length, 1); a.match(list.rows[0][1], /October.*EUR.*999\.55.*Approved/); a.match(f.tile().props.value, /USD.*70\.00/); } finally { f.h.unmount(); }
});
test('unknown invoice or payroll currency is unavailable rather than labelled INR', async () => {
  const f = await page({ byCurrency: [] }, [invoice({ currency: undefined })], [payout({ currency: undefined })]);
  try { const lists = f.lists(); for (const list of lists) { a.match(list.rows[0][1], /Currency details unavailable/); a.doesNotMatch(list.rows[0][1], /INR|₹/); } } finally { f.h.unmount(); }
});
