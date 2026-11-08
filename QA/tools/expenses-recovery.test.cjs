// Actual Expenses TSX with synthetic persistence/transport; not live SQL acceptance.
const { test } = require('node:test'), a = require('node:assert/strict');
const { mount, nodes, text, deferred } = require('./frontend-page-runtime.cjs');
const fixture = () => [
  { id: 'inr', description: 'General cost', amount: 10.10, currency: 'INR', category: 'General', status: 'Recorded', branchId: null, expenseDate: '2026-11-01' },
  { id: 'usd', description: 'USD teaching cost', amount: 5.25, currency: 'USD', category: 'TeacherPayout', status: 'Recorded', branchId: 'b', expenseDate: '2026-11-02' },
  { id: 'teacher', description: 'INR teaching cost', amount: 2.15, currency: 'INR', category: 'TeacherPayout', status: 'Recorded', branchId: null, expenseDate: '2026-11-03' },
];
async function page(config = {}) {
  const calls = [], writes = [], rows = config.rows || fixture(), hold = deferred(); let failed = false, refreshBlocked = !!config.refreshFail;
  const h = mount(process.env.QA_EXPENSES_SOURCE || 'apps/web/src/app/expenses/page.tsx', async (url, init = {}) => {
    const key = url === '/api/academies' ? 'academies' : (url.endsWith('/branches') || url.endsWith('/expenses/branch-options')) ? 'branches' : 'expenses';
    const method = init.method || 'GET'; calls.push({ url, method });
    if (method === 'POST') {
      const body = JSON.parse(init.body); writes.push(body); if (config.holdPost) await hold.promise;
      if (config.postStatus && config.postStatus < 500) return response(config.postStatus, {});
      if (config.commitUnknown || !config.postTransport && !(config.postStatus >= 500)) {
        if (!body.description?.trim() || !Number.isFinite(body.amount) || body.amount <= 0) return response(400, {});
        rows.push({ ...body, id: 'saved-' + rows.length, description: body.description.trim(), status: 'Recorded', expenseDate: body.expenseDate || '2026-11-08' });
      }
      if (config.postTransport) throw Error('Synthetic lost response');
      return response(config.postStatus || 201, rows.at(-1));
    }
    if (config.holdRead === key) await hold.promise;
    const selected = config.fail === key && (!config.once || !failed); if (selected) failed = true;
    if (selected && config.transport || writes.length && refreshBlocked && key === 'expenses') throw Error('Synthetic read interruption');
    const status = selected ? config.status || 503 : 200;
    return { ok: status === 200, status, json: async () => {
      if (selected && config.badJson) throw Error('Synthetic bad JSON');
      if (selected && config.badShape) return {};
      return key === 'academies' ? config.emptyAcademy ? [] : [{ id: 'a' }] : key === 'branches' ? [{ id: 'b', name: 'Synthetic branch' }] : [...rows];
    } };
  });
  await h.flush();
  return { h, calls, writes, rows, release: () => hold.resolve(), allowReads: () => { refreshBlocked = false; },
    fill(values = {}) {
      const all = nodes(h.render()); all.find(n => n.type === 'input' && n.props.placeholder?.startsWith('e.g.')).props.onChange({ target: { value: values.description ?? 'New cost' } });
      all.find(n => n.type === 'input' && n.props.placeholder === 'Amount in INR').props.onChange({ target: { value: values.amount ?? '25.55' } });
      all.find(n => n.props?.name === 'expenseCategory').props.onChange(values.category || 'General');
      all.find(n => n.props?.name === 'branchId').props.onChange(values.branchId || ''); all.find(n => n.props?.name === 'expenseDate').props.onChange(values.expenseDate || ''); h.render();
    },
    submit() { const fn = nodes(h.render()).find(n => n.type === 'form')?.props.onSubmit; a.ok(fn, 'Expense form'); return () => fn({ preventDefault() {} }); },
  };
}
function response(status, data) { return { ok: status >= 200 && status < 300, status, json: async () => data }; }
function metric(f, label) { const card = nodes(f.h.render()).find(n => n.type === 'article' && nodes(n).some(c => c.type === 'span' && text(c) === label)); a.ok(card, label); return text(nodes(card).find(n => n.type === 'b')); }
function button(f, label) { const found = nodes(f.h.render()).find(n => n.type === 'button' && text(n) === label); a.ok(found, label); return found; }
function input(f, kind) { return nodes(f.h.render()).find(n => n.type === 'input' && (kind === 'description' ? n.props.placeholder?.startsWith('e.g.') : n.props.placeholder === 'Amount in INR')); }
function blocked(f) { a.ok(!nodes(f.h.render()).some(n => n.props?.className === 'expenses-kpis')); a.ok(!nodes(f.h.render()).some(n => n.type === 'form')); a.doesNotMatch(text(f.h.render()), /5092|localhost/); }
test('mixed totals and TeacherPayout amounts retain distinct currencies and cents', async () => { const f = await page(); try { a.match(metric(f, 'Total recorded'), /INR.*12\.25.*USD.*5\.25/); a.match(metric(f, 'Teacher payouts'), /INR.*2\.15.*USD.*5\.25/); a.equal(metric(f, 'Expenses logged'), '3'); a.match(text(f.h.render()), /USD.*5\.25/); a.ok(f.calls.every(c => c.method === 'GET')); } finally { f.h.unmount(); } });
test('USD-only and empty categories do not invent an INR unit', async () => { const f = await page({ rows: [fixture()[1]] }); try { a.match(metric(f, 'Total recorded'), /USD.*5\.25/); a.doesNotMatch(metric(f, 'Total recorded'), /INR|₹/); } finally { f.h.unmount(); } });
test('successful empty register shows unitless zero and a usable form', async () => { const f = await page({ rows: [] }); try { a.equal(metric(f, 'Total recorded'), '0.00'); a.equal(metric(f, 'Teacher payouts'), '0.00'); a.ok(nodes(f.h.render()).some(n => n.type === 'form')); } finally { f.h.unmount(); } });
test('missing currency is unavailable instead of guessed INR', async () => { const row = { ...fixture()[0], currency: undefined }, f = await page({ rows: [row] }); try { a.equal(metric(f, 'Total recorded'), 'Currency details unavailable'); a.match(text(f.h.render()), /Currency details unavailable/); } finally { f.h.unmount(); } });
test('invalid legacy currency is labelled safely without Intl failure', async () => { const f = await page({ rows: [{ ...fixture()[0], currency: 'bad unit' }] }); try { a.match(metric(f, 'Total recorded'), /BAD UNIT 10\.10/); } finally { f.h.unmount(); } });
test('nonfinite monetary data is unavailable rather than zero or NaN', async () => { const f = await page({ rows: [{ ...fixture()[0], amount: NaN }] }); try { a.equal(metric(f, 'Total recorded'), 'Currency details unavailable'); a.doesNotMatch(text(f.h.render()), /NaN/); } finally { f.h.unmount(); } });
for (const [key, status] of [['academies',401],['academies',403],['academies',429],['academies',503],['expenses',401],['expenses',403],['expenses',503],['branches',403],['branches',503]]) test(`${key}/${status} never publishes ready zero totals or editable intake`, async () => { const f = await page({ fail: key, status }); try { blocked(f); a.ok(nodes(f.h.render()).some(n => n.props?.role === 'alert')); } finally { f.h.unmount(); } });
for (const mode of ['transport', 'badJson', 'badShape']) for (const key of ['academies', 'expenses', 'branches']) test(`${key}/${mode} is a recoverable load failure`, async () => { const f = await page({ fail: key, status: 200, [mode]: true }); try { blocked(f); a.ok(nodes(f.h.render()).some(n => n.type === 'button' && text(n) === 'Try again')); } finally { f.h.unmount(); } });
test('empty academy is gated rather than an editable empty register', async () => { const f = await page({ emptyAcademy: true }); try { blocked(f); a.match(text(f.h.render()), /Create your academy/); } finally { f.h.unmount(); } });
test('manual initial read retry recovers without a POST', async () => { const f = await page({ fail: 'expenses', once: true }); try { blocked(f); button(f, 'Try again').props.onClick(); f.h.render(); await f.h.flush(); a.match(metric(f, 'Total recorded'), /USD.*5\.25/); a.equal(f.writes.length, 0); } finally { f.h.unmount(); } });
test('category filtering retains full-register totals and branch/date metadata', async () => { const f = await page(); try { nodes(f.h.render()).find(n => n.props?.name === 'expenseFilter').props.onChange('TeacherPayout'); f.h.render(); a.equal(nodes(f.h.render()).filter(n => n.type === 'li').length, 2); a.match(text(f.h.render()), /Synthetic branch.*2026-11-02/); a.match(metric(f, 'Total recorded'), /12\.25/); } finally { f.h.unmount(); } });
for (const amount of ['0', '-1', 'NaN', 'Infinity']) test('invalid amount ' + amount + ' is blocked before submission', async () => { const f = await page(); try { f.fill({ amount }); await f.submit()(); a.equal(f.writes.length, 0); } finally { f.h.unmount(); } });
test('blank description is blocked before submission', async () => { const f = await page(); try { f.fill({ description: '   ' }); await f.submit()(); a.equal(f.writes.length, 0); } finally { f.h.unmount(); } });
test('same-render duplicate and stale success submits send one INR payload and clear once', async () => { const f = await page({ holdPost: true }); try { f.fill({ category: 'TeacherPayout', branchId: 'b', expenseDate: '2026-11-09' }); const submit = f.submit(), one = submit(), two = submit(); await f.h.flush(); a.equal(f.writes.length, 1); a.ok(nodes(f.h.render()).some(n => n.type === 'fieldset' && n.props.disabled)); f.release(); await Promise.all([one,two]); await f.h.flush(); a.deepEqual(f.writes[0], { description: 'New cost', amount: 25.55, currency: 'INR', category: 'TeacherPayout', branchId: 'b', expenseDate: '2026-11-09' }); a.equal(input(f, 'description').props.value, ''); a.equal(input(f, 'amount').props.value, ''); await submit(); a.equal(f.writes.length, 1); a.match(text(f.h.render()), /Expense recorded/); } finally { f.h.unmount(); } });
for (const status of [400, 401, 403, 409, 422, 429]) test('definitive rejection ' + status + ' preserves draft and unlocks correction', async () => { const f = await page({ postStatus: status }); try { f.fill(); const result = await f.submit()().then(() => 'resolved', () => 'rejected'); await f.h.flush(); a.equal(result, 'resolved'); a.equal(input(f, 'description').props.value, 'New cost'); a.equal(input(f, 'amount').props.value, '25.55'); a.ok(!nodes(f.h.render()).some(n => n.type === 'fieldset' && n.props.disabled)); a.doesNotMatch(text(f.h.render()), /Expense recorded/); } finally { f.h.unmount(); } });
test('acknowledged save with failed readback stays saved and blocks duplicate until read recovery', async () => { const f = await page({ refreshFail: true }); try { f.fill(); const stale = f.submit(); a.equal(await stale().then(() => 'resolved', () => 'rejected'), 'resolved'); await f.h.flush(); a.match(text(f.h.render()), /Expense saved.*could not be refreshed/); blocked(f); await stale(); a.equal(f.writes.length, 1); f.allowReads(); button(f, 'Reload financial records').props.onClick(); f.h.render(); await f.h.flush(); a.equal(f.writes.length, 1); a.equal(input(f, 'description').props.value, ''); a.ok(!nodes(f.h.render()).some(n => n.type === 'fieldset' && n.props.disabled)); } finally { f.h.unmount(); } });
for (const config of [{ postTransport:true,commitUnknown:true },{ postTransport:true },{ postStatus:503,commitUnknown:true }]) test('unknown outcome '+JSON.stringify(config)+' requires fresh read and explicit new-entry reset', async () => { const f = await page(config); try { f.fill(); const stale=f.submit(); a.equal(await stale().then(() => 'resolved', () => 'rejected'), 'resolved'); await f.h.flush(); a.match(text(f.h.render()), /could not be confirmed/); blocked(f); await stale(); a.equal(f.writes.length,1); button(f,'Reload financial records').props.onClick(); f.h.render(); await f.h.flush(); a.equal(input(f,'description').props.value,'New cost'); a.ok(nodes(f.h.render()).some(n=>n.type==='fieldset'&&n.props.disabled)); await stale(); a.equal(f.writes.length,1); button(f,'Start a new expense').props.onClick(); f.h.render(); a.equal(input(f,'description').props.value,''); await stale(); a.equal(f.writes.length,1); } finally { f.h.unmount(); } });
test('unmount ignores late initial reads', async () => { const f = await page({ holdRead: 'expenses' }); f.h.unmount(); f.release(); await f.h.flush(); a.equal(f.h.lateWrites,0); });
test('unmount ignores late save/readback completion', async () => { const f = await page({ holdPost: true }); f.fill(); const pending=f.submit()(); f.h.unmount(); f.release(); await pending.catch(()=>{}); await f.h.flush(); a.equal(f.h.lateWrites,0); });

test('expense lookup uses the Finance-scoped option endpoint without branch-management reads', async () => { const f = await page(); try { a.ok(f.calls.some(c => c.url.endsWith('/expenses/branch-options'))); a.ok(!f.calls.some(c => c.url.endsWith('/branches'))); } finally { f.h.unmount(); } });
