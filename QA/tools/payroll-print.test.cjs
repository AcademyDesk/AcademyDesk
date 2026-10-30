// Actual client page handlers with synthetic transport. Not browser/PDF/SQL proof.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');

const payout = (id, extra = {}) => ({ id, payrollProfileId: 'profile-' + id, workerName: 'Saved worker ' + id,
  workerType: 'Staff', payslipNumber: 'PS-' + id, periodLabel: 'Synthetic period ' + id,
  grossAmount: 1000, deductions: 100, netAmount: 900, currency: 'INR', status: 'Paid', paymentMethod: 'BankTransfer',
  reference: 'REF-' + id, paidAtUtc: '2026-10-28T10:00:00Z', ...extra });

async function fixture(options = {}) {
  const rows = options.empty ? [] : [payout('one'), payout('two')], prints = [], calls = [];
  let printReads = 0, h;
  global.window = { requestAnimationFrame: fn => setTimeout(fn, 0), cancelAnimationFrame: clearTimeout,
    print: () => {
      const paper = nodes(h.render()).find(n => n.props?.className === 'payroll-payslip');
      prints.push({ paper, value: text(paper), all: text(h.render()) });
      if (options.printThrows) throw Error('Synthetic print dialog failure');
    } };
  h = mount('apps/web/src/app/payroll/page.tsx', async (url, init = {}) => {
    calls.push({ url, ...init });
    if (url === '/api/academies') return response([{ id: 'academy' }]);
    if (url.endsWith('/teachers') || url.endsWith('/staff') || url.endsWith('/payroll/profiles')) return response([]);
    if (url.endsWith('/payroll/payouts')) {
      if (++printReads === 1) return response(rows);
      return options.read ? options.read() : response(options.fresh ?? rows);
    }
    throw Error('Unexpected endpoint: ' + url);
  });
  await h.flush();
  const buttons = () => nodes(h.render()).filter(n => n.type === 'button' && /Print \/ Save PDF|Preparing payslip/.test(text(n)));
  const click = (index = 0) => { assert.ok(buttons()[index]); void buttons()[index].props.onClick(); };
  return { h, rows, prints, calls, buttons, click, get printReads() { return printReads - 1; } };
}

test('Each row prints only its freshly loaded saved payout with exact currency and cents', async () => {
  const f = await fixture({ fresh: [payout('one'), payout('two', { workerName: 'Fresh stored identity', grossAmount: 10.05, deductions: 10.04, netAmount: 0.01, currency: 'USD', sessionsCovered: 3 })] });
  f.click(1); await f.h.flush();
  assert.equal(f.prints.length, 1); const p = f.prints[0];
  assert.ok(p.paper, 'A visible selected payslip exists before opening the print dialog');
  assert.equal(p.paper.props['data-payout-id'], 'two');
  for (const value of ['Fresh stored identity', 'PS-two', 'Synthetic period two', 'REF-two', '10.05', '10.04', '0.01', '3']) assert.ok(p.value.includes(value), value);
  assert.match(p.value, /USD|\$/); assert.ok(!p.value.includes('Saved worker one'));
  assert.equal(f.printReads, 1); assert.equal(f.calls.at(-1).cache, 'no-store');
  assert.equal(f.calls.filter(c => c.method).length, 0); f.h.unmount();
});

test('Pending fresh read cannot print; same-tick repeats produce one read/dialog', async () => {
  const wait = deferred(), f = await fixture({ read: () => wait.promise });
  const click = f.buttons()[0].props.onClick; click(); click(); await f.h.flush();
  assert.equal(f.prints.length, 0); assert.equal(f.printReads, 1); assert.ok(f.buttons().every(b => b.props.disabled));
  wait.resolve(response(f.rows)); await f.h.flush(); assert.equal(f.prints.length, 1); f.h.unmount();
});

for (const [name, read] of [
  ['failed read', () => response(null, false)], ['malformed list', () => response({})],
  ['missing saved record', () => response([])], ['invalid money', () => response([payout('one', { netAmount: '900' })])],
  ['network uncertainty', () => { throw Error('Synthetic network'); }],
]) test(name + ' cannot open a stale payslip and releases retry', async () => {
  const f = await fixture({ read }); f.click(); await f.h.flush();
  assert.equal(f.prints.length, 0); assert.match(text(f.h.render()), /could not|unavailable/i);
  assert.ok(f.buttons().every(b => !b.props.disabled)); f.h.unmount();
});

test('A failed read can be retried with the selected saved payout', async () => {
  let fail = true; const f = await fixture({ read: () => response(f.rows, !fail) });
  f.click(1); await f.h.flush(); assert.equal(f.prints.length, 0);
  fail = false; f.click(1); await f.h.flush(); assert.equal(f.prints.length, 1);
  assert.equal(f.prints[0].paper.props['data-payout-id'], 'two'); f.h.unmount();
});

test('Unmount during refresh never opens a dialog or updates the retired page', async () => {
  const wait = deferred(), f = await fixture({ read: () => wait.promise });
  f.click(); f.h.unmount(); wait.resolve(response(f.rows)); await f.h.flush();
  assert.equal(f.prints.length, 0); assert.equal(f.h.lateWrites, 0);
});

test('Print dialog errors release controls and keep failure visible', async () => {
  const f = await fixture({ printThrows: true }); f.click(); await f.h.flush();
  assert.equal(f.prints.length, 1); assert.match(text(f.h.render()), /could not/i);
  assert.ok(f.buttons().every(b => !b.props.disabled)); f.h.unmount();
});

 test('Empty register has no print action or payslip document', async () => {
  const f = await fixture({ empty: true }); assert.equal(f.buttons().length, 0);
  assert.match(text(f.h.render()), /No payouts recorded/);
  assert.equal(nodes(f.h.render()).filter(n => n.props?.className === 'payroll-payslip').length, 0);
  assert.equal(f.prints.length, 0); f.h.unmount();
});
