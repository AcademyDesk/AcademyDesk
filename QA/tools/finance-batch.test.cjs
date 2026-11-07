// Actual financial TSX and controlled APIs; not native browser/Identity/SQL proof.
const { test } = require('node:test');
const a = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');
const kinds = ['payment', 'invoice', 'profile', 'payout', 'reconcile'];
const pages = { payment: 'payments', invoice: 'invoices', profile: 'payroll', payout: 'payroll', reconcile: 'finance-reconciliation' };
const money = (n, c = 'INR') => new Intl.NumberFormat('en-IN', { style: 'currency', currency: c }).format(n);

async function fixture(kind, options = {}) {
  const writes = [], reads = [];
  let resets = 0, saved = false;
  const invoices = [{ id: 'i', invoiceNumber: 'QA-I', studentId: 's', totalAmount: 1000, balance: 400, currency: 'INR', dueDate: '2026-12-01', status: 'PartiallyPaid' },
    ...(options.mixed ? [{ id: 'usd', invoiceNumber: 'QA-USD', studentId: 's', totalAmount: 100, balance: 80, currency: 'USD', dueDate: '2026-12-01', status: 'PartiallyPaid' }] : [])];
  const payments = [{ id: 'p', invoiceId: 'i', amount: 600, currency: 'INR', status: 'Reconciled', method: 'UPI', paidAtUtc: '2026-11-01T10:00:00Z' },
    { id: 'pending', invoiceId: 'i', amount: 25.5, currency: 'INR', status: 'Completed', method: 'Cash', paidAtUtc: '2026-11-01T11:00:00Z' },
    { id: 'voided', invoiceId: 'i', amount: 75, currency: 'INR', status: 'Voided', method: 'Cash', paidAtUtc: '2026-11-01T12:00:00Z' },
    ...(options.mixed ? [{ id: 'usd', invoiceId: 'usd', amount: 20, currency: 'USD', status: 'Completed', method: 'Cash', paidAtUtc: '2026-11-01T10:00:00Z' }] : [])];
  const h = mount(`apps/web/src/app/${pages[kind]}/page.tsx`, async (url, init = {}) => {
    if (init.method) {
      writes.push({ url, method: init.method, body: JSON.parse(init.body) });
      if (options.write) { const result = await options.write(writes.at(-1), writes.length); if (result && !result.ok) return result; }
      saved = true; return response({ id: 'saved', ...writes.at(-1).body });
    }
    reads.push(url);
    if (options.failRefresh && saved) return response({}, false);
    if (url === '/api/academies') return response([{ id: 'a', name: 'Synthetic academy' }]);
    if (url.endsWith('/invoices/student-options')) return response([{ id: 's', firstName: 'Synthetic', lastName: 'Learner' }]);
    if (url.endsWith('/invoices/document-settings')) return response({ invoiceTemplateKey: 'Classic' });
    if (url.endsWith('/invoices')) return response(invoices);
    if (url.endsWith('/fee-plans')) return response([]);
    if (url.endsWith('/payments')) return response(payments);
    if (url.endsWith('/teachers')) return response([{ id: 't', firstName: 'Synthetic', lastName: 'Teacher' }]);
    if (url.endsWith('/staff')) return response([]);
    if (url.endsWith('/payroll/profiles')) return response([{ id: 'pr', workerType: 'Teacher', workerName: 'Synthetic Teacher', paymentModel: 'Monthly', monthlyAmount: 1000, effectiveFrom: '2026-01-01', isActive: true }]);
    if (url.endsWith('/payroll/payouts')) return response([{ id: 'historic', workerName: 'Original saved name', workerType: 'Teacher', periodLabel: 'October', netAmount: 900, currency: 'INR', paidAtUtc: '2026-11-01T00:00:00Z' },
      ...(options.mixed ? [{ id: 'usd', workerName: 'USD saved name', workerType: 'Teacher', periodLabel: 'October', netAmount: 50, currency: 'USD', paidAtUtc: '2026-11-01T00:00:00Z' }] : [])]);
    throw Error('Unexpected fixture GET ' + url);
  });
  const all = () => nodes(h.render());
  const custom = name => all().find(n => n.type === 'select-control' && n.props.name === name);
  const values = { monthlyAmount: '1000', periodLabel: 'November 2026', deductions: '1', reference: 'Synthetic reference', ...options.values };
  const target = { values, reset() { resets++; } };
  const form = () => all().filter(n => n.type === 'form')[kind === 'payout' ? 1 : 0];
  const submit = () => { const event = { preventDefault() {}, currentTarget: target }; const promise = form().props.onSubmit(event); event.currentTarget = null; return promise; };
  await h.flush();
  if (kind === 'payment') { all().find(n => n.type === 'select').props.onChange({ target: { value: 'i' } }); }
  if (kind === 'invoice') { all().find(n => n.type === 'input' && n.props.type === 'number').props.onChange({ target: { value: '100' } }); }
  if (kind === 'profile') { custom('workerId').props.onChange('t'); await h.flush(); all().find(n => n.type === 'date-control').props.onChange('2026-11-07'); }
  if (kind === 'payout') custom('payrollProfileId').props.onChange('pr');
  if (kind === 'reconcile') {
    all().find(n => n.type === 'button' && /Reconcile/.test(text(n))).props.onClick(); await h.flush();
    all().find(n => n.type === 'input' && n.props.placeholder?.includes('HDFC')).props.onChange({ target: { value: 'Synthetic evidence' } });
  }
  await h.flush();
  const notice = () => text(all().find(n => n.type === 'p' && (n.props.className || '').includes({payment:'enterprise-settings-empty',invoice:'invoices-notice',profile:'payroll-notice',payout:'payroll-notice',reconcile:'reconciliation-notice'}[kind])));
  return { h, all, writes, reads, submit, target, resets: () => resets, form, notice };
}

for (const kind of kinds) {
  test(`${kind}: a held save blocks a second handler invocation`, async () => {
    const gate = deferred(), f = await fixture(kind, { write: () => gate.promise });
    let first, second;
    try { first = f.submit(); second = f.submit(); await f.h.flush(); a.equal(f.writes.length, 1); }
    finally { gate.resolve(response({})); await Promise.allSettled([first, second]); await f.h.flush(); f.h.unmount(); }
  });
  test(`${kind}: confirmed save survives a failed readback`, async () => {
    const f = await fixture(kind, { failRefresh: true });
    try { await a.doesNotReject(f.submit()); await f.h.flush(); a.match(f.notice(), /(?:saved|recorded|issued|reconciled).*refresh/i); a.equal(f.writes.length, 1); }
    finally { f.h.unmount(); }
  });
  test(`${kind}: network uncertainty is caught and cannot silently submit again`, async () => {
    const f = await fixture(kind, { write: () => { throw Error('Synthetic lost response'); } });
    try {
      await a.doesNotReject(f.submit()); await f.h.flush();
      a.match(f.notice(), /could not be confirmed/i); a.equal(f.resets(), 0);
      await f.submit(); a.equal(f.writes.length, 1, 'No ambiguous financial replay');
    } finally { f.h.unmount(); }
  });
  test(`${kind}: successful readback leaves a durable confirmation`, async () => {
    const f = await fixture(kind);
    try { await f.submit(); await f.h.flush(); a.match(f.notice(), /saved|recorded|issued|reconciled/i); a.equal(f.writes.length, 1); }
    finally { f.h.unmount(); }
  });
  test(`${kind}: pending response cannot reset or fetch after unmount`, async () => {
    const gate = deferred(), f = await fixture(kind, { write: () => gate.promise });
    const first = f.submit(), count = f.reads.length; f.h.unmount(); gate.resolve(response({})); await Promise.allSettled([first]); await f.h.flush();
    a.equal(f.h.lateWrites, 0); a.equal(f.reads.length, count); a.equal(f.resets(), 0);
  });
}

for (const deductions of ['1000', '1001']) test(`payout: deductions ${deductions} cannot submit a zero or negative net payout`, async () => {
  const f = await fixture('payout', { values: { deductions } });
  try { await f.submit(); await f.h.flush(); a.equal(f.writes.length, 0); a.match(f.notice(), /deductions.*less.*gross/i); }
  finally { f.h.unmount(); }
});

for (const kind of ['payment', 'invoice', 'payout', 'reconcile']) test(`${kind}: mixed currency cards never sum USD into INR`, async () => {
  const f = await fixture(kind, { mixed: true });
  try {
    const cls = { payment: 'payments-kpis', invoice: 'invoices-kpis', payout: 'payroll-kpis', reconcile: 'reconciliation-kpis' }[kind];
    const kpi = f.all().find(n => n.props?.className === cls), content = text(kpi);
    a.match(content, /USD/); a.ok(content.includes(money(kind === 'payment' ? 625.5 : kind === 'invoice' ? 1000 : kind === 'payout' ? 900 : 25.5)));
    a.ok(!content.includes(money(kind === 'payment' ? 645.5 : kind === 'invoice' ? 1100 : kind === 'payout' ? 950 : 45.5)));
  } finally { f.h.unmount(); }
});
