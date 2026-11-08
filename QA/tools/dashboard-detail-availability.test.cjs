// Actual Dashboard TSX and pure DetailList render; controlled APIs, not live SQL.
const { test } = require('node:test'), a = require('node:assert/strict');
const { mount, nodes, text, deferred } = require('./frontend-page-runtime.cjs');
const keys = ['students', 'invoices', 'payroll', 'sessions', 'attendance'];
async function page(config = {}) {
  const calls = [], pending = deferred(); let failed = false;
  const h = mount(process.env.QA_DASHBOARD_DETAILS_SOURCE || 'apps/web/src/app/dashboard/page.tsx', async (url, init = {}) => {
    calls.push({ url, method: init.method || 'GET' });
    const key = url === '/api/academies' ? 'academies' : url === '/api/auth/session' ? 'session' : url.endsWith('/attendance') ? 'attendance' : url.endsWith('/payroll/payouts') ? 'payroll' : url.split('/').at(-1);
    if (config.pause === key) await pending.promise;
    const selected = (config.fail === key || config.failBoth && ['invoices', 'payroll'].includes(key)) && (!config.once || !failed);
    if (selected) failed = true;
    if (selected && config.transport) throw Error('Synthetic read interruption');
    let status = selected ? config.status || 503 : 200;
    const data = { academies: [{ id: 'a' }], session: { displayName: 'Synthetic Admin' },
      dashboard: { activeStudents: 1, activeTeachers: 1, activeCourses: 1, activeBatches: 1, openLeads: 0, attendanceRecordsLast30Days: 2, presentAttendanceLast30Days: 1, classesToday: 1,
        byCurrency: [{ currency: 'USD', grossBilled: 100.55, approvedAdjustments: .05, collected: 20.1, outstanding: 80.4, reconciled: 20.1 }],
        todaySchedule: [{ id: 'schedule', batchName: 'Primary schedule', teacherName: 'Synthetic Teacher', startUtc: '2026-11-08T10:00:00Z', status: 'Scheduled' }] },
      students: [{ id: 's', firstName: 'Synthetic', lastName: 'Student', isActive: true }],
      invoices: [{ id: 'i', studentId: 's', invoiceNumber: 'USD-DUE', totalAmount: 100.55, paidAmount: 20.1, balance: 80.4, currency: 'USD', status: 'Issued' }],
      payroll: [{ workerName: 'Synthetic Staff', periodLabel: 'October', netAmount: 999.55, currency: 'EUR', status: 'Approved' }],
      sessions: [{ id: 'ss' }], attendance: [{ studentId: 's', status: 'Present' }],
    };
    if (config.empty && keys.includes(key)) data[key] = [];
    return { ok: status === 200, status, json: async () => { if (selected && config.badJson) throw Error('Synthetic invalid JSON'); return selected && config.badShape ? {} : data[key]; } };
  });
  await h.flush();
  return { h, calls, release: () => pending.resolve(),
    lists(label) { const tile = nodes(h.render()).find(n => n.type === 'tile' && n.props.label === label); a.ok(tile); tile.props.onClick(); return nodes(h.render()).filter(n => n.type?.name === 'DetailList').map(n => ({ props: n.props, tree: n.type(n.props), text: text(n.type(n.props)) })); } };
}
function ready(f) { a.ok(nodes(f.h.render()).some(n => n.props?.['aria-label'] === 'Academy operating indicators')); const tile = nodes(f.h.render()).find(n => n.type === 'tile' && n.props.label === 'Outstanding fees'); a.match(tile.props.value, /USD.*80\.40/); a.ok(f.calls.every(c => c.method === 'GET')); }
function unavailable(list) { a.match(list.text, /could not be loaded/); a.ok(nodes(list.tree).some(n => n.props?.role === 'alert')); a.doesNotMatch(list.text, /No student fees|No payroll records|No active students|No attendance recorded|100%/); }
for (const key of keys) for (const status of [401, 403, 503]) test(`${key}/${status} keeps overview and shows truthful affected detail`, async () => {
  const f = await page({ fail: key, status }); try { ready(f);
    if (key === 'invoices' || key === 'payroll') { const lists = f.lists('Outstanding fees'); unavailable(lists[key === 'invoices' ? 0 : 1]); a.match(lists[key === 'invoices' ? 1 : 0].text, key === 'invoices' ? /EUR.*999\.55/ : /USD.*80\.40/); }
    else if (key === 'students') unavailable(f.lists('Active students')[0]); else unavailable(f.lists('Attendance rate')[0]);
    a.ok(nodes(f.h.render()).some(n => n.type === 'button' && text(n) === 'Reload details'));
  } finally { f.h.unmount(); }
});
for (const key of keys) test(key + ' transport rejection is isolated from successful independent details', async () => { const f = await page({ fail: key, transport: true }); try { ready(f); if (key === 'invoices' || key === 'payroll') unavailable(f.lists('Outstanding fees')[key === 'invoices' ? 0 : 1]); else if (key === 'students') unavailable(f.lists('Active students')[0]); else unavailable(f.lists('Attendance rate')[0]); } finally { f.h.unmount(); } });
for (const mode of ['badJson', 'badShape']) for (const key of ['invoices', 'payroll']) test(`${key}/${mode} is unavailable, not an empty-success list`, async () => { const f = await page({ fail: key, status: 200, [mode]: true }); try { ready(f); unavailable(f.lists('Outstanding fees')[key === 'invoices' ? 0 : 1]); } finally { f.h.unmount(); } });
test('two financial detail failures remain independently unavailable without changing summary money', async () => { const f = await page({ failBoth: true }); try { ready(f); for (const list of f.lists('Outstanding fees')) unavailable(list); } finally { f.h.unmount(); } });
test('successful empty invoices and payroll retain genuine empty messages', async () => { const f = await page({ empty: true }); try { ready(f); const lists = f.lists('Outstanding fees'); a.equal(lists[0].text, 'No student fees are outstanding.'); a.equal(lists[1].text, 'No payroll records are listed.'); a.ok(!nodes(f.h.render()).some(n => n.type === 'button' && text(n) === 'Reload details')); } finally { f.h.unmount(); } });
test('missing student names do not hide available invoice identifiers or amounts', async () => { const f = await page({ fail: 'students', status: 403 }); try { ready(f); const list = f.lists('Outstanding fees')[0]; a.match(list.text, /USD-DUE.*USD.*80\.40/); a.match(text(f.h.render()), /Student names could not be loaded/); } finally { f.h.unmount(); } });
test('primary schedule remains available when the optional sessions read fails', async () => { const f = await page({ fail: 'sessions' }); try { ready(f); a.match(f.lists('Classes today')[0].text, /Primary schedule/); } finally { f.h.unmount(); } });
for (const status of [500, 503]) test('required session ' + status + ' gates ready indicators', async () => { const f = await page({ fail: 'session', status }); try { a.ok(!nodes(f.h.render()).some(n => n.props?.['aria-label'] === 'Academy operating indicators')); a.ok(nodes(f.h.render()).some(n => n.type === 'button' && text(n) === 'Try again')); } finally { f.h.unmount(); } });
test('manual read-only reload recovers a failed invoice detail', async () => { const f = await page({ fail: 'invoices', once: true }); try { ready(f); unavailable(f.lists('Outstanding fees')[0]); const button = nodes(f.h.render()).find(n => n.type === 'button' && text(n) === 'Reload details'); a.ok(button); button.props.onClick(); f.h.render(); await f.h.flush(); ready(f); a.match(f.lists('Outstanding fees')[0].text, /USD-DUE.*USD.*80\.40/); a.ok(!nodes(f.h.render()).some(n => n.type === 'button' && text(n) === 'Reload details')); } finally { f.h.unmount(); } });
test('pending optional request keeps primary overview usable and shows loading details, not no debt', async () => { const f = await page({ pause: 'invoices' }); try { ready(f); const list = f.lists('Outstanding fees')[0]; a.match(list.text, /Loading details/); a.doesNotMatch(list.text, /No student fees|could not be loaded/); } finally { f.h.unmount(); f.release(); await f.h.flush(); a.equal(f.h.lateWrites, 0); } });
test('unmount during late optional reads preserves cancellation without state writes', async () => { const f = await page({ pause: 'payroll' }); f.h.unmount(); f.release(); await f.h.flush(); a.equal(f.h.lateWrites, 0); });
