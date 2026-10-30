// Actual payment page with synthetic delayed transport: UI lifecycle proof, not SQL/browser proof.
const test = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');
const file = 'apps/web/src/app/payments/page.tsx';
const invoice = { id: 'invoice', invoiceNumber: 'SYNTHETIC', studentId: 'student', totalAmount: 1000, balance: 800, currency: 'INR', dueDate: '2026-10-30', status: 'PartiallyPaid' };
async function setup(writer, readback = () => false) {
  const calls = []; let retire, writes = 0;
  const api = async (url, init = {}) => {
    calls.push({ url, init });
    if (init.method === 'POST') { writes++; return writer(); }
    if (readback(writes)) throw Error('Synthetic readback unavailable');
    if (url === '/api/academies') return response([{ id: 'academy' }]);
    if (url.endsWith('/student-options')) return response([{ id: 'student', firstName: 'Synthetic', lastName: 'Student' }]);
    if (url.endsWith('/invoices')) return response([invoice]);
    return response([{ id: 'paid', amount: 200, currency: 'INR', status: 'Reconciled' }]);
  };
  api.subscribePortalSessionChanges = (workspace, listener) => { assert.equal(workspace, 'AcademyAdmin'); retire = listener; return () => { retire = undefined; }; };
  const h = mount(file, api); await h.flush();
  function choose() { const tree = h.render(); nodes(tree).find(n => n.type === 'select').props.onChange({ target: { value: 'invoice' } }); }
  choose(); h.render();
  return { h, calls, submit: () => nodes(h.render()).find(n => n.type === 'form').props.onSubmit({ preventDefault() {} }), retire: () => retire?.(), get writes() { return writes; } };
}
test('payment same-tick duplicate submit sends one partial payment while inputs stay locked', async () => {
  const pending = deferred(), f = await setup(() => pending.promise);
  const a = f.submit(), b = f.submit(); assert.equal(f.writes, 1);
  assert.equal(nodes(f.h.render()).find(n => n.type === 'input').props.disabled, true);
  pending.resolve(response({ id: 'new' })); await Promise.all([a,b]); await f.h.flush();
});
test('a rejected amount preserves the exact draft and permits a corrected submission', async () => {
  const f = await setup(() => ({ ...response({}, false), status: 400 }));
  await f.submit(); await f.h.flush();
  assert.equal(nodes(f.h.render()).find(n => n.type === 'input').props.value, '800');
  await f.submit(); assert.equal(f.writes, 2);
});
for (const [label, writer] of [['lost response', async () => { throw Error('Synthetic transport failure'); }], ['server error', async () => response({}, false)]]) test(label + ' blocks blind retry and preserves the draft', async () => {
  const f = await setup(writer); await f.submit(); await f.h.flush();
  assert.match(text(f.h.render()), /check.*register|check.*ledger/i);
  await f.submit(); assert.equal(f.writes, 1);
  assert.equal(nodes(f.h.render()).find(n => n.type === 'input').props.value, '800');
});
test('confirmed payment with unavailable readback cannot be blindly submitted again', async () => {
  const f = await setup(async () => response({ id: 'saved' }), writes => writes > 0);
  await f.submit(); await f.h.flush(); await f.submit();
  assert.equal(f.writes, 1); assert.match(text(f.h.render()), /saved.*refresh|saved.*load/i);
});
for (const boundary of ['unmount', 'session replacement']) test('late payment response cannot update after ' + boundary, async () => {
  const pending = deferred(), f = await setup(() => pending.promise), request = f.submit();
  if (boundary === 'unmount') f.h.unmount(); else { f.retire(); assert.doesNotMatch(text(f.h.render()), /SYNTHETIC/); }
  pending.resolve(response({ id: 'new' })); await request; await f.h.flush();
  assert.equal(f.h.lateWrites, 0);
  assert.equal(f.calls.filter(c => c.url.endsWith('/invoices')).length, 1);
});
test('mixed currency payment totals retain both units without adding them', async () => {
  const api = async url => response(url === '/api/academies' ? [{ id: 'academy' }] : url.endsWith('/invoices') ? [invoice, { ...invoice, id: 'usd', balance: 10, currency: 'USD' }] : url.endsWith('/payments') ? [{ amount: 200, currency: 'INR', status: 'Reconciled' }, { amount: 20, currency: 'USD', status: 'Completed' }] : []);
  const h = mount(file, api); await h.flush();
  const tile = label => nodes(h.render()).find(n => n.type === 'article' && text(n).startsWith(label));
  assert.match(text(tile('Collected')), /200\.00/); assert.match(text(tile('Collected')), /20\.00/);
  assert.match(text(tile('Collected')), /\$|USD/); assert.doesNotMatch(text(tile('Collected')), /220\.00/);
  assert.doesNotMatch(text(tile('Outstanding')), /810\.00/);
});
