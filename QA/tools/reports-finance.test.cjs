// Actual Reports TSX with controlled data; not SQL or live browser acceptance.
const { test } = require('node:test'), a = require('node:assert/strict');
const { mount, nodes, text, deferred } = require('./frontend-page-runtime.cjs');
function fixture() { return {
  students: [{ id: 's', firstName: 'Synthetic', lastName: 'Student', isActive: true }],
  teachers: [{ id: 't', firstName: 'Synthetic', lastName: 'Teacher', isActive: true }],
  batches: [{ id: 'b', name: 'Synthetic batch', capacity: 10 }], enrollments: [{ studentId: 's', batchId: 'b', status: 'Active' }],
  sessions: [{ id: 'ss', batchId: 'b', startUtc: '2026-11-08T12:00:00Z', status: 'Scheduled' }], attendance: [{ studentId: 's', status: 'Present' }, { studentId: 's', status: 'Absent' }],
  invoices: [
    { id: 'usd', studentId: 's', invoiceNumber: 'USD-DUE', totalAmount: 100.55, adjustedAmount: .05, paidAmount: 20.1, balance: 80.4, currency: 'USD', status: 'Issued', dueDate: '2026-11-09' },
    { id: 'over', studentId: 's', invoiceNumber: 'INR-OVER', totalAmount: 100, paidAmount: 150, balance: 0, currency: 'INR', status: 'Paid', dueDate: '2026-11-09' },
    { id: 'inr', studentId: 's', invoiceNumber: 'INR-DUE', totalAmount: 80.15, paidAmount: 0, balance: 80.15, currency: 'INR', status: 'Issued', dueDate: '2026-11-09' },
    { id: 'cancel', studentId: 's', invoiceNumber: 'INR-CANCEL', totalAmount: 500, paidAmount: 0, balance: 500, currency: 'INR', status: 'cancelled', dueDate: '2026-11-09' }],
  payments: [{ invoiceId: 'usd', amount: 20.1, currency: 'USD', status: 'Reconciled' }, { invoiceId: 'over', amount: 150, currency: 'INR', status: 'Completed' }, { amount: 9999, currency: 'INR', status: 'Pending' }, { amount: 9999, currency: 'USD', status: 'Voided' }],
  expenses: [{ description: 'USD expense', amount: 5.05, currency: 'USD', category: 'Supplies', expenseDate: '2026-11-01' }, { description: 'INR expense', amount: 10.10, currency: 'INR', category: 'Rent', expenseDate: '2026-11-01' }],
  payroll: [{ workerName: 'Unpaid worker', periodLabel: 'October', netAmount: 999.55, currency: 'EUR', status: 'Approved' }, { workerName: 'Paid worker', netAmount: 500, currency: 'INR', status: 'Paid' }]
}; }
async function page(data = fixture(), config = {}) {
  const calls = [], pause = deferred(); let failed = false;
  const h = mount(process.env.QA_REPORTS_SOURCE || 'apps/web/src/app/reports/page.tsx', async (url, init = {}) => {
    calls.push({ url, method: init.method || 'GET' });
    if (config.pause && url === '/api/academies') await pause.promise;
    const key = url === '/api/academies' ? 'academies' : url.endsWith('/attendance') ? 'attendance' : url.endsWith('/payroll/payouts') ? 'payroll' : url.split('/').at(-1);
    let status = config.fail === key && (!config.failOnce || !failed) ? config.status || 503 : 200; if (status !== 200) failed = true;
    return { ok: status === 200, status, json: async () => key === 'academies' ? config.emptyAcademy ? [] : [{ id: 'a' }] : data[key] || [] };
  });
  await h.flush();
  return { h, calls, release: () => pause.resolve() };
}
function metric(h, label) {
  const tile = nodes(h.render()).find(n => n.type === 'tile' && n.props.label === label);
  if (tile) return tile.props.value;
  const card = nodes(h.render()).find(n => n.type === 'article' && nodes(n).some(c => c.type === 'span' && text(c) === label));
  a.ok(card, 'metric ' + label); return text(nodes(card).find(n => n.type === 'strong'));
}
function detail(f, key) { nodes(f.h.render()).find(n => n.type === 'tile' && n.props.label === key).props.onClick(); return nodes(f.h.render()).filter(n => n.type?.name === 'ReportDetailList').map(n => n.props); }
const noReady = h => { a.ok(!nodes(h.render()).some(n => n.props?.['aria-label'] === 'Operational metrics')); a.ok(!nodes(h.render()).some(n => n.type === 'button' && text(n).includes('Download CSV'))); };
test('mixed report money, expenses and their difference retain independent units and cents', async () => {
  const f = await page(); try {
    for (const [label, expected] of [['Collected', /INR.*150\.00.*USD.*20\.10/], ['Outstanding', /INR.*80\.15.*USD.*80\.40/], ['Expenses', /INR.*10\.10.*USD.*5\.05/], ['Collected less recorded expenses', /INR.*139\.90.*USD.*15\.05/]]) { a.match(metric(f.h, label), expected); a.doesNotMatch(metric(f.h, label), /EUR|999|₹/); }
    const summary = nodes(f.h.render()).find(n => n.props?.className === 'workspace-reports-finance-summary'); a.match(text(summary), /INR.*680\.15.*USD.*100\.55.*issued/); a.ok(f.calls.every(c => c.method === 'GET')); a.ok(!f.calls.some(c => c.url.includes('finance-governance')));
  } finally { f.h.unmount(); }
});
test('reconciled and completed receipts appear in detail without pending or voided money', async () => { const f = await page(); try { const list = detail(f, 'Collected')[0]; a.equal(list.rows.length, 2); a.match(list.rows[0][1], /USD.*20\.10 paid/); a.doesNotMatch(JSON.stringify(list.rows), /9999|9,999/); } finally { f.h.unmount(); } });
test('outstanding detail uses adjusted API balances, excludes cancelled/overpaid rows and separates payroll', async () => { const f = await page(); try { const lists = detail(f, 'Outstanding'); a.equal(lists[0].rows.length, 2); a.match(JSON.stringify(lists[0].rows), /USD.*80\.40/); a.doesNotMatch(JSON.stringify(lists[0].rows), /INR-CANCEL|INR-OVER|90\.45/); a.equal(lists[1].rows.length, 1); a.match(lists[1].rows[0][1], /EUR.*999\.55.*Approved/); } finally { f.h.unmount(); } });
test('empty successful data is unitless zero; no currency is guessed', async () => { const f = await page({}); try { for (const label of ['Collected', 'Outstanding', 'Expenses', 'Collected less recorded expenses']) a.equal(metric(f.h, label), '0.00'); } finally { f.h.unmount(); } });
for (const key of ['invoices', 'payments', 'expenses']) test(key + ' missing currency is unavailable rather than guessed INR', async () => { const d = fixture(); d[key][0].currency = undefined; const f = await page(d); try { a.equal(metric(f.h, key === 'invoices' ? 'Outstanding' : key === 'payments' ? 'Collected' : 'Expenses'), 'Currency details unavailable'); } finally { f.h.unmount(); } });
test('nonfinite invoice balance is unavailable instead of a false zero', async () => { const d = fixture(); d.invoices[0].balance = NaN; const f = await page(d); try { a.equal(metric(f.h, 'Outstanding'), 'Currency details unavailable'); } finally { f.h.unmount(); } });
test('negative difference is retained in the actual expense-only currency', async () => { const d = fixture(); d.expenses.push({ amount: 9.15, currency: 'EUR' }); const f = await page(d); try { a.match(metric(f.h, 'Collected less recorded expenses'), /-EUR.*9\.15.*INR.*139\.90.*USD.*15\.05/); } finally { f.h.unmount(); } });
for (const [key, status] of [['academies',401],['academies',403],['academies',429],['academies',503],['students',503],['teachers',503],['batches',503],['enrollments',503],['sessions',503],['invoices',503],['payments',503],['expenses',503],['payroll',503],['attendance',503],['payments',401],['invoices',403]]) test(`${key}/${status} gates all money and exports without internal connection details`, async () => { const f = await page(fixture(), { fail: key, status }); try { noReady(f.h); a.doesNotMatch(text(f.h.render()), /5092|localhost/); a.ok(nodes(f.h.render()).some(n => n.props?.role === 'alert')); } finally { f.h.unmount(); } });
test('empty academy gates reports rather than publishing ready zero balances', async () => { const f = await page(fixture(), { emptyAcademy: true }); try { noReady(f.h); a.match(text(f.h.render()), /Create your academy/); } finally { f.h.unmount(); } });
test('manual retry recovers a failed required feed without replaying writes', async () => { const f = await page(fixture(), { fail: 'invoices', failOnce: true }); try { noReady(f.h); const button = nodes(f.h.render()).find(n => n.type === 'button' && text(n) === 'Try again'); a.ok(button); button.props.onClick(); f.h.render(); await f.h.flush(); a.match(metric(f.h, 'Collected'), /USD.*20\.10/); a.ok(f.calls.every(c => c.method === 'GET')); } finally { f.h.unmount(); } });
test('loading is gated and unmounted late responses never write state', async () => { const f = await page(fixture(), { pause: true }); noReady(f.h); f.h.unmount(); f.release(); await f.h.flush(); a.equal(f.h.lateWrites, 0); a.equal(f.calls.length, 1); });
async function download(fn) { const oldDoc = global.document, oldCreate = URL.createObjectURL, oldRevoke = URL.revokeObjectURL; let blob, clicked, revoked;
  global.document = { createElement: () => { const anchor = { click() { clicked = anchor.download; } }; return anchor; } }; URL.createObjectURL = b => { blob = b; return 'blob:synthetic'; }; URL.revokeObjectURL = u => { revoked = u; };
  try { fn(); a.equal(revoked, 'blob:synthetic'); return { filename: clicked, csv: await blob.text() }; } finally { global.document = oldDoc; URL.createObjectURL = oldCreate; URL.revokeObjectURL = oldRevoke; }
}
test('both invoice exports preserve amount/currency columns and real spreadsheet escaping', async () => { const d = fixture(); d.students[0].firstName = '=FORMULA'; const f = await page(d); try {
  const tree = f.h.render(), panel = nodes(tree).find(n => n.type === 'section' && n.props.className === 'workspace-reports-panel' && nodes(n).some(c => c.type === 'h2' && text(c) === 'Finance export'));
  const top = nodes(panel).find(n => n.type === 'button'), card = nodes(tree).find(n => n.type?.name === 'ExportCard' && n.props.title === 'Invoices');
  const one = await download(top.props.onClick), two = await download(card.props.onClick); a.deepEqual(one, two); a.equal(one.filename, 'academydesk-invoices.csv'); a.match(one.csv, /^"Invoice","Student","Amount","Currency","Due date"\r\n/); a.match(one.csv, /"100\.55","USD"/); a.match(one.csv, /"'=FORMULA Student"/);
} finally { f.h.unmount(); } });
test('expense export carries the actual currency for every row', async () => { const f = await page(); try { const card = nodes(f.h.render()).find(n => n.type?.name === 'ExportCard' && n.props.title === 'Expenses'); const result = await download(card.props.onClick); a.match(result.csv, /^"Expense amount","Currency"\r\n/); a.match(result.csv, /"5\.05","USD"/); a.match(result.csv, /"10\.1","INR"/); } finally { f.h.unmount(); } });
test('operational counts and batch occupancy CSV remain intact', async () => { const f = await page(); try { a.equal(metric(f.h, 'Active students'), 1); a.equal(metric(f.h, 'Teachers'), 1); a.equal(metric(f.h, 'Scheduled classes'), 1); a.equal(metric(f.h, 'Attendance rate'), '50%'); const card = nodes(f.h.render()).find(n => n.type?.name === 'ExportCard' && n.props.title === 'Batches'); const result = await download(card.props.onClick); a.match(result.csv, /"Synthetic batch","1","10"/); } finally { f.h.unmount(); } });

test('numeric strings cannot become an apparently valid expense difference through coercion', async () => { const d = fixture(); d.expenses[0].amount = '5.05'; const f = await page(d); try { a.equal(metric(f.h, 'Expenses'), 'Currency details unavailable'); a.equal(metric(f.h, 'Collected less recorded expenses'), 'Currency details unavailable'); } finally { f.h.unmount(); } });
