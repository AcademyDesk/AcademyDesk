// Actual TSX with synthetic status replies and a controlled client clock; not live authorization/SQL.
const { test } = require('node:test'), a = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');
const instant = Date.parse('2026-11-07T12:00:00Z');
function clock() {
  const previous = global.window, now = Date.now, timers = new Map(); let id = 0, time = instant;
  Date.now = () => time;
  global.window = { ...previous, setTimeout(fn, delay) { timers.set(++id, { fn, delay }); return id; }, clearTimeout(key) { timers.delete(key); } };
  return { timers, advance(value) { time = value; }, fire() { const list = [...timers]; timers.clear(); for (const [, timer] of list) timer.fn(); }, restore() { Date.now = now; global.window = previous; } };
}
const grants = [
  { id: 'permanent', userName: 'Permanent', isPermanent: true, expiresAtUtc: '2026-01-01T00:00:00Z' },
  { id: 'future', userName: 'Future', isPermanent: false, expiresAtUtc: '2026-11-07T12:00:01Z' },
  { id: 'boundary', userName: 'Boundary', isPermanent: false, expiresAtUtc: '2026-11-07T12:00:00Z' },
  { id: 'past', userName: 'Past', isPermanent: false, expiresAtUtc: '2026-11-07T11:59:59Z' },
  { id: 'revoked', userName: 'Revoked', isPermanent: true, revokedAtUtc: '2026-11-07T11:00:00Z' },
].map(row => ({ ...row, permissions: ['students.manage'] }));
async function access(rows = grants) {
  const h = mount('apps/web/src/app/access-review/page.tsx', async url => response(url === '/api/academies' ? [{ id: 'a' }] : url.endsWith('/access-grants') ? rows : [])); await h.flush(); return h;
}
test('access count excludes expired and boundary grants, retaining permanent and future grants', async () => {
  const c = clock(); let h; try { h = await access(); const header = nodes(h.render()).find(n => n.props?.className?.includes('access-grants-panel')); a.match(text(header), /2 active/); }
  finally { h?.unmount(); c.restore(); }
});
test('access grant rows distinguish active, expired and revoked while retaining revoke actions', async () => {
  const c = clock(); let h; try {
    h = await access(); const rows = nodes(h.render()).filter(n => n.type === 'li');
    for (const [name, expected] of [['Permanent', 'Active'], ['Future', 'Active'], ['Boundary', 'Expired'], ['Past', 'Expired'], ['Revoked', 'Revoked']]) {
      const row = rows.find(n => nodes(n).some(x => x.type === 'b' && text(x) === name));
      a.equal(text(nodes(row).find(n => n.props?.['data-grant-status'])), expected);
      a.equal(nodes(row).filter(n => n.type === 'button').length, expected === 'Revoked' ? 0 : 1);
    }
  } finally { h?.unmount(); c.restore(); }
});
test('open access register updates at expiry without sending a mutation or refetch', async () => {
  const c = clock(); let h; try {
    h = await access(); a.ok(c.timers.size > 0); a.ok([...c.timers.values()].every(t => t.delay > 0 && t.delay <= 1000));
    c.advance(instant + 1000); c.fire(); await h.flush(); a.match(text(nodes(h.render()).find(n => n.props?.className?.includes('access-grants-panel'))), /1 active/);
  } finally { h?.unmount(); c.restore(); }
});
test('invalid temporary grant expiry renders safely and is never active', async () => {
  const c = clock(); let h; try { h = await access([{ id: 'invalid', userName: 'Invalid', permissions: [], isPermanent: false, expiresAtUtc: 'invalid' }]); a.match(text(h.render()), /0 active/); a.match(text(h.render()), /valid expiry/i); }
  finally { h?.unmount(); c.restore(); }
});

async function platform(kind, options = {}) {
  const writes = [], reads = []; let status = kind === 'invoice' ? 'Issued' : 'Open', saved = false;
  const initial = status, target = kind === 'invoice' ? 'Paid' : 'Closed';
  const h = mount('apps/web/src/app/platform/control/page.tsx', async (url, init = {}) => {
    if (init.method) {
      const write = { url, body: JSON.parse(init.body), method: init.method }; writes.push(write);
      if (options.gate) await options.gate.promise;
      if (options.reject) return { ...response({ message: 'Synthetic denial' }, false), status: options.reject, json: async () => { if (options.bodyGate) await options.bodyGate.promise; return { message: 'Synthetic denial' }; } };
      if (options.transport) { if (options.committed) { status = write.body.status; saved = true; } throw Error('Synthetic lost response'); }
      status = write.body.status; saved = true; return response({});
    }
    reads.push(url);
    if (options.failRead && saved && /billing-invoices|support-cases/.test(url)) return response({}, false);
    if (url === '/api/platform/academies') return response([{ id: 'a', name: 'Synthetic academy', subscriptionPlan: 'Trial', subscriptionStatus: 'Trial', enabledModulesJson: '["Core"]', studentLimit: 10, staffLimit: 5 }]);
    if (url.endsWith('/onboarding')) return response({});
    if (url === '/api/platform/billing-invoices') return response(options.removed && saved ? [] : [{ id: 'bill', academyId: 'a', academyName: 'Synthetic academy', invoiceNumber: 'QA-BILL', amount: 100, currency: 'INR', status: kind === 'invoice' ? saved && options.authority ? options.authority : status : 'Issued', dueDate: '2026-12-01' }]);
    if (url === '/api/platform/support-cases') return response(options.removed && saved ? [] : [{ id: 'case', academyId: 'a', academyName: 'Synthetic academy', subject: 'Synthetic case', priority: 'High', status: kind === 'case' ? saved && options.authority ? options.authority : status : 'Open' }]);
    if (url === '/api/platform/settings') return response({ platformName: 'Synthetic platform', defaultCurrency: 'INR', defaultTrialDays: 14, dataRetentionDays: 90 });
    if (url === '/api/auth/session') return response({ displayName: 'Synthetic owner' });
    if (url === '/api/platform/health') return response({});
    return response([]);
  });
  await h.flush(); h.query.set('tab', kind === 'invoice' ? 'Billing' : 'Support'); await h.flush();
  const field = () => nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === (kind === 'invoice' ? 'invoice-status-bill' : 'case-status-case'));
  a.ok(field());
  const change = async value => { field().props.onChange(value); await h.flush(); };
  const refresh = async () => { const button = nodes(h.render()).find(n => n.type === 'button' && text(n) === 'Refresh platform status lists'); a.ok(button); button.props.onClick(); await h.flush(); };
  return { h, writes, reads, field, change, refresh, initial, target, options };
}
for (const kind of ['invoice', 'case']) {
  test(`${kind}: rejected status restores the authoritative selection`, async () => {
    const f = await platform(kind, { reject: 403 }); try { await f.change(f.target); a.equal(f.field().props.value, f.initial); a.equal(f.writes.length, 1); } finally { f.h.unmount(); }
  });
  test(`${kind}: server failure stays unconfirmed and blocks another mutation`, async () => {
    const f = await platform(kind, { reject: 500 }); try { await f.change(f.target); a.equal(f.field().props.value, f.initial); a.equal(f.field().props.disabled, true); a.match(text(f.h.render()), /could not be confirmed/i); await f.change(f.target); a.equal(f.writes.length, 1); } finally { f.h.unmount(); }
  });
  test(`${kind}: lost committed response is reconciled by GET without replay`, async () => {
    const f = await platform(kind, { transport: true, committed: true }); try { await f.change(f.target); a.equal(f.field().props.value, f.initial); a.equal(f.field().props.disabled, true); f.options.transport = false; await f.refresh(); a.equal(f.field().props.value, f.target); a.equal(f.writes.length, 1); } finally { f.h.unmount(); }
  });
  test(`${kind}: fresh response outranks a stale optimistic selection`, async () => {
    const authority = kind === 'invoice' ? 'Overdue' : 'In progress', f = await platform(kind, { authority }); try { await f.change(f.target); a.equal(f.field().props.value, authority); } finally { f.h.unmount(); }
  });
  test(`${kind}: pending status guards duplicate callbacks`, async () => {
    const gate = deferred(), f = await platform(kind, { gate }); try { const old = f.field(); old.props.onChange(f.target); old.props.onChange(f.target); await f.h.flush(); a.equal(f.writes.length, 1); } finally { gate.resolve(); await f.h.flush(); f.h.unmount(); }
  });
  test(`${kind}: placeholder status sends no mutation`, async () => {
    const f = await platform(kind); try { await f.change(''); a.equal(f.writes.length, 0); } finally { f.h.unmount(); }
  });
  test(`${kind}: confirmed write with failed read stays locked until fresh lists load`, async () => {
    const f = await platform(kind, { failRead: true }); try { await f.change(f.target); a.equal(f.field().props.disabled, true); a.match(text(f.h.render()), /updated.*could not be refreshed/i); f.options.failRead = false; await f.refresh(); a.equal(f.field().props.value, f.target); a.equal(f.writes.length, 1); } finally { f.h.unmount(); }
  });
  test(`${kind}: stale callback cannot mutate a removed row`, async () => {
    const f = await platform(kind, { removed: true }); try { const old = f.field(); await f.change(f.target); a.equal(f.field(), undefined); old.props.onChange(f.target); await f.h.flush(); a.equal(f.writes.length, 1); } finally { f.h.unmount(); }
  });
  test(`${kind}: pending response cannot fetch or update after unmount`, async () => {
    const gate = deferred(), f = await platform(kind, { gate }); f.field().props.onChange(f.target); const count = f.reads.length; f.h.unmount(); gate.resolve(); await f.h.flush(); a.equal(f.reads.length, count); a.equal(f.h.lateWrites, 0);
    const bodyGate = deferred(), denied = await platform(kind, { reject: 403, bodyGate }); denied.field().props.onChange(denied.target); await denied.h.flush(); denied.h.unmount(); bodyGate.resolve(); await denied.h.flush(); a.equal(denied.h.lateWrites, 0, 'Late denial body must not write after unmount');
  });
}

for (const [label, href, view] of [['Leads', '/leads', 'overview'], ['Follow-ups due', '/sales-marketing?view=follow-ups', 'overview'], ['Trial bookings', '/trial-bookings', 'overview'], ['Converted', '/sales-marketing?view=conversion', 'overview'], ['Total leads', '/leads', 'conversion'], ['Conversion rate', '/sales-marketing?view=conversion', 'conversion']]) test(`sales tile ${label} retains its details route`, async () => {
  const h = mount('apps/web/src/app/sales-marketing/page.tsx', async url => response(url === '/api/academies' ? [{ id: 'a' }] : []), {}, 'SalesMarketingContent');
  try { await h.flush(); h.query.set('view', view); await h.flush(); const tile = nodes(h.render()).find(n => n.type === 'tile' && n.props.label === label); a.ok(tile, label); tile.props.onClick(); await h.flush(); const link = nodes(h.render()).find(n => n.type === 'a' && text(n) === 'Open details'); a.ok(link); a.equal(link.props.href, href); } finally { h.unmount(); }
});
