// Runs the real TSX handlers with delayed synthetic transport; not SQL/browser acceptance.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');
const paths = { payroll: 'apps/web/src/app/payroll/page.tsx', reconciliation: 'apps/web/src/app/finance-reconciliation/page.tsx' };
const profiles = [{ id: 'profile', workerType: 'Teacher', workerName: 'Teacher', paymentModel: 'Monthly', monthlyAmount: 1000, isActive: true }];
const payment = (id, currency, amount) => ({ id, invoiceId: 'invoice', currency, amount, method: 'BankTransfer', status: 'Recorded', paidAtUtc: '2026-10-30T12:00:00Z' });
async function fixture(page, write, options = {}) {
  const calls = []; let resets = 0, retire;
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) return write(url, init);
    if (options.refreshFails && calls.some(c => c.method)) throw Error('read unavailable');
    if (url === '/api/academies') return response([{ id: 'academy' }]);
    if (url.endsWith('/teachers')) return response([{ id: 'teacher', firstName: 'Test', lastName: 'Teacher' }]);
    if (url.endsWith('/staff')) return response([]);
    if (url.endsWith('/profiles')) return response(profiles);
    if (url.endsWith('/payouts')) return response(options.payouts ?? []);
    if (url.endsWith('/invoices')) return response([{ id: 'invoice', invoiceNumber: 'INV-1' }]);
    if (url.endsWith('/payments')) return response(options.payments ?? [payment('payment', 'INR', 100)]);
    throw Error(url);
  };
  api.subscribePortalSessionChanges = (workspace, listener) => { assert.equal(workspace, "AcademyAdmin"); retire = listener; return () => { retire = undefined; }; };
  const h = mount(paths[page], api);
  await h.flush();
  const find = (type, predicate) => nodes(h.render()).find(n => n.type === type && predicate(n));
  const select = (name, value, type = 'select-control') => find(type, n => n.props.name === name).props.onChange(value);
  function prepare(kind) {
    if (kind === 'profile') { select('workerId', 'teacher'); select('effectiveFrom', '2026-10-01', 'date-control'); }
    if (kind === 'payout') select('payrollProfileId', 'profile');
    if (kind === 'reconcile') {
      find('button', n => /Reconcile/.test(text(n))).props.onClick();
      find('input', n => n.props.autoFocus).props.onChange({ target: { value: '  BANK-123  ' } });
    }
    const form = nodes(h.render()).filter(n => n.type === 'form')[kind === 'payout' ? 1 : 0];
    return { handler: form.props.onSubmit, event: { preventDefault() {}, currentTarget: { values: { monthlyAmount: '1000', periodLabel: 'October 2026', deductions: '10', reference: 'BANK-123' }, reset() { resets++; } } } };
  }
  return { h, calls, prepare, retire() { assert.ok(retire); retire(); }, get writes() { return calls.filter(c => c.method); }, get resets() { return resets; } };
}
for (const kind of ['profile', 'payout', 'reconcile']) {
  const page = kind === 'reconcile' ? 'reconciliation' : 'payroll';
  test(kind + ': same-tick submissions admit one write and lock editable controls', async () => {
    const wait = deferred(), f = await fixture(page, () => wait.promise), s = f.prepare(kind);
    const first = s.handler(s.event), second = s.handler(s.event); await f.h.flush();
    assert.equal(f.writes.length, 1);
    assert.ok(nodes(f.h.render()).some(n => n.type === 'fieldset' && n.props.disabled));
    assert.match(text(f.h.render()), /saving|recording/i);
    wait.resolve(response({})); await Promise.all([first, second]); await f.h.flush();
    if (kind === 'reconcile') assert.deepEqual(JSON.parse(f.writes[0].body), { reference: 'BANK-123' });
    else assert.equal(f.resets, 1);
    assert.match(text(f.h.render()), kind === 'profile' ? /profile saved/i : kind === 'payout' ? /payout recorded/i : /Payment reconciled/);
    f.h.unmount();
  });
  for (const outcome of ['network', 'server']) test(kind + ': ' + outcome + ' uncertainty preserves draft and blocks blind repeat', async () => {
    const f = await fixture(page, () => { if (outcome === 'network') throw Error('lost transport'); return response({}, false); });
    const s = f.prepare(kind); await s.handler(s.event); await f.h.flush();
    assert.equal(f.resets, 0);
    assert.match(text(f.h.render()), /could not confirm|unconfirmed|unknown/i);
    assert.match(text(f.h.render()), /reload/i);
    await s.handler(s.event); await f.h.flush(); assert.equal(f.writes.length, 1);
    f.h.unmount();
  });
  test(kind + ': rejected validation preserves draft and permits a corrected retry', async () => {
    let invalid = true;
    const f = await fixture(page, () => invalid ? { ...response({}, false), status: 400 } : response({}));
    const s = f.prepare(kind); await s.handler(s.event); await f.h.flush();
    assert.equal(f.resets, 0); assert.match(text(f.h.render()), /could not|not saved|not recorded/i);
    invalid = false; await s.handler(s.event); await f.h.flush(); assert.equal(f.writes.length, 2);
    f.h.unmount();
  });
  test(kind + ': confirmed write and failed refresh remain visibly confirmed and prevent repeat', async () => {
    const f = await fixture(page, () => response({}), { refreshFails: true }), s = f.prepare(kind);
    await s.handler(s.event); await f.h.flush();
    assert.match(text(f.h.render()), kind === 'profile' ? /profile saved/i : kind === 'payout' ? /payout recorded/i : /Payment reconciled/);
    assert.match(text(f.h.render()), /refresh|reload/i);
    await s.handler(s.event); assert.equal(f.writes.length, 1); f.h.unmount();
  });
  test(kind + ': session replacement clears finance data and ignores the old write result', async () => {
    const wait = deferred(), f = await fixture(page, () => wait.promise), s = f.prepare(kind);
    const pending = s.handler(s.event); f.retire(); await f.h.flush();
    assert.match(text(f.h.render()), /session changed/i);
    wait.resolve(response({})); await pending; await f.h.flush();
    assert.match(text(f.h.render()), /session changed/i); assert.equal(f.resets, 0);
    assert.equal(nodes(f.h.render()).filter(n => n.type === "form").length, 0);
    assert.doesNotMatch(text(f.h.render()), /profile saved|payout recorded|Payment reconciled|BANK-123/);
    await s.handler(s.event); assert.equal(f.writes.length, 1); f.h.unmount();
  });
  test(kind + ': completion after unmount does not write retired state or reset the form', async () => {
    const wait = deferred(), f = await fixture(page, () => wait.promise), s = f.prepare(kind);
    const pending = s.handler(s.event); f.h.unmount(); wait.resolve(response({})); await pending; await f.h.flush();
    assert.equal(f.h.lateWrites, 0); assert.equal(f.resets, 0);
  });
}
test('Payroll admits only one financial write across both forms', async () => {
  const wait = deferred(), f = await fixture('payroll', () => wait.promise);
  f.prepare('profile'); f.prepare('payout');
  const a = f.prepare('profile'), b = f.prepare('payout');
  const first = a.handler(a.event), second = b.handler(b.event);
  assert.equal(f.writes.length, 1); wait.resolve(response({})); await Promise.all([first, second]); f.h.unmount();
});
for (const page of ['payroll', 'reconciliation']) test(page + ': amount summary separates stored currencies and retains cents', async () => {
  const f = await fixture(page, () => response({}), {
    payouts: [{ id: 'one', netAmount: 10.15, currency: 'INR', paidAtUtc: '2026-10-30' }, { id: 'two', netAmount: 2.25, currency: 'USD', paidAtUtc: '2026-10-30' }],
    payments: [payment('one', 'INR', 10.15), payment('two', 'USD', 2.25)],
  });
  const card = nodes(f.h.render()).find(n => n.type === 'article' && text(n).includes(page === 'payroll' ? 'Paid value' : 'Value awaiting match'));
  assert.match(text(card), /INR/); assert.match(text(card), /USD/);
  assert.match(text(card), /10\.15/); assert.match(text(card), /2\.25/); assert.doesNotMatch(text(card), /12\.40|12\.4/);
  f.h.unmount();
});

for (const page of ['payroll', 'reconciliation']) test(page + ': initial academy read cannot hydrate an unmounted page', async () => {
  const wait = deferred();
  const h = mount(paths[page], () => wait.promise);
  h.unmount(); wait.resolve(response([{ id: 'retired-academy' }])); await h.flush();
  assert.equal(h.lateWrites, 0);
});
