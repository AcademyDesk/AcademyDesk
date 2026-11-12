// Actual TSX controlled hooks/handlers, not live React/browser/device evidence.
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const compiled = ts.transpileModule(fs.readFileSync(path.resolve(__dirname, '../../apps/web/src/app/finance-governance/page.tsx'), 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX },
}).outputText;
const summaryModule = { exports: {} };
new Function('module', 'exports', ts.transpileModule(fs.readFileSync(path.resolve(__dirname, '../../apps/web/src/lib/finance-summary.ts'), 'utf8'),
  { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText)(summaryModule, summaryModule.exports);
const item = { id: 'w', type: 'Collections', title: 'Synthetic collection', description: null, priority: 'High', status: 'Open', escalationStage: 'Initial', promisedPaymentDate: null };
const invoice = { id: 'i', invoiceNumber: 'QA-I', totalAmount: 100, balance: 100, currency: 'INR', dueDate: '2026-09-01', daysOverdue: 30 };
const adjustment = { id: 'd', type: 'Discount', amountExact: '125.01', currency: 'USD', reason: 'Synthetic', status: 'PendingApproval' };
const settings = { invoiceTemplateKey: 'Classic', payslipTemplateKey: 'Standard' };
const flush = () => new Promise(resolve => setImmediate(resolve));
function nodes(node, predicate) {
  if (Array.isArray(node)) return node.flatMap(x => nodes(x, predicate));
  if (!node || typeof node !== 'object') return [];
  return [...(predicate(node) ? [node] : []), ...nodes(node.props?.children, predicate)];
}
function text(node) {
  if (Array.isArray(node)) return node.map(text).join('');
  if (node == null || typeof node === 'boolean') return '';
  return typeof node === 'object' ? text(node.props?.children) : String(node);
}
function page({ ready = false, deniedRead = false, deniedWrite = false, failRefresh = false, allowGeneric = false, promptAnswer = '', pendingAdjustment = adjustment } = {}) {
  const states = ready ? [{ id: 'a' }, [pendingAdjustment], [invoice], [item], settings, '', undefined, false, 'Invoice', false] : [];
  const writes = new Map(), effects = [], calls = []; let index = 0, mutated = false;
  const react = { useState: initial => { const key = index++; return [writes.has(key) ? writes.get(key) : ready ? states[key] : initial,
    value => writes.set(key, typeof value === 'function' ? value(writes.has(key) ? writes.get(key) : states[key]) : value)]; }, useEffect: fn => effects.push(fn) };
  const module = { exports: {} };
  new Function('require', 'module', 'exports', 'FormData', 'window', compiled)(name => {
    if (name === 'react') return react;
    if (name === 'react/jsx-runtime') return jsx;
    if (name === 'next/link') return { default: 'a' };
    if (name === '@/lib/finance-summary') return summaryModule.exports;
    if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
      const write = options.method && options.method !== 'GET'; calls.push({ url, ...options });
      const generic = url.includes('/admin-work-items');
      const status = (generic && !allowGeneric) || (write ? deniedWrite : deniedRead || (mutated && failRefresh)) ? 403 : 200;
      if (write && status === 200) mutated = true;
      return { ok: status === 200, status, json: async () => url === '/api/academies' ? [{ id: 'a' }]
        : url.endsWith('/collection-tasks') || generic ? [item] : url.endsWith('/collections') ? [invoice]
        : url.endsWith('/finance-adjustments') ? [pendingAdjustment] : url.endsWith('/settings') ? settings : {} };
    } };
    throw Error('Unexpected import ' + name);
  }, module, module.exports, class { constructor(fields) { this.fields = fields; } get(key) { return this.fields[key] ?? null; } }, { prompt: () => promptAnswer });
  function render() { index = 0; return module.exports.default(); }
  render();
  return { render, writes, calls, async initialise() { effects[0](); await flush(); }, async submit(form, fields) {
    await form.props.onSubmit({ preventDefault() {}, currentTarget: fields }); await flush();
  } };
}
test('FinanceUser loads the collection register without generic admin-work-items', async () => {
  const p = page(); await p.initialise();
  assert.ok(p.calls.some(x => x.url === '/api/academies/a/finance-governance/collection-tasks'));
  assert.ok(p.calls.every(x => !x.url.includes('/admin-work-items')));
  assert.deepEqual(p.writes.get(3), [item]); assert.equal(p.writes.get(5), '');
  assert.ok(text(p.render()).includes(item.title));
});
test('Denied prerequisite does not falsely present successful loading', async () => {
  const p = page({ deniedRead: true }); await p.initialise();
  assert.equal(p.writes.has(3), false); assert.match(p.writes.get(5), /could not be loaded/);
});
for (const scenario of ['saved', 'denied', 'refresh-failed']) test(`Escalation ${scenario}: narrow route, nullable date and honest notice`, async () => {
  const p = page({ ready: true, deniedWrite: scenario === 'denied', failRefresh: scenario === 'refresh-failed' });
  const form = nodes(p.render(), n => n.type === 'form' && nodes(n, c => c.type === 'select' && c.props.name === 'stage').length)[0];
  await p.submit(form, { stage: 'Reminder', promiseDate: '' });
  const write = p.calls.find(x => x.method === 'PATCH');
  assert.equal(write.url, '/api/academies/a/finance-governance/collection-tasks/w');
  assert.deepEqual(JSON.parse(write.body), { escalationStage: 'Reminder', promisedPaymentDate: null });
  assert.equal(p.writes.get(7), false);
  assert.match(p.writes.get(5), scenario === 'denied' ? /could not be saved/ : /Collections escalation state updated/);
  if (scenario === 'refresh-failed') assert.match(p.writes.get(5), /could not be refreshed/);
  if (scenario === 'saved') assert.ok(text(p.render()).includes('Collections escalation state updated'));
});
for (const kind of ['follow-up', 'approval']) for (const failRefresh of [false, true]) test(`${kind} success survives refresh (${failRefresh ? 'failed' : 'successful'})`, async () => {
  const p = page({ ready: true, failRefresh, allowGeneric: true }); // Isolate notice loss from the prior Admin-only dependency.
  if (kind === 'follow-up') {
    nodes(p.render(), n => n.type === 'button' && text(n) === 'Create follow-up →')[0].props.onClick();
    const form = nodes(p.render(), n => n.type === 'form' && nodes(n, c => c.type === 'input' && c.props.name === 'note').length)[0];
    await p.submit(form, { note: 'Synthetic', priority: 'High', dueAtUtc: '' });
    assert.equal(p.writes.get(6), undefined);
  } else {
    nodes(p.render(), n => n.type === 'button' && text(n) === 'Approve')[0].props.onClick(); await flush();
  }
  assert.match(p.writes.get(5), kind === 'follow-up' ? /Collections follow-up created/ : /Adjustment approved/);
  if (failRefresh) assert.match(p.writes.get(5), /could not be refreshed/);
  assert.equal(p.writes.get(7), false);
});

for (const [amountExact, currency, expected] of [
  ['125.01', 'USD', 'USD 125.01'], ['0.01', 'EUR', 'EUR 0.01'],
  ['9999999999999999.99', 'INR', 'INR 9999999999999999.99'],
]) test(`adjustment approval shows exact ${expected}`, () => {
  const p = page({ ready: true, pendingAdjustment: { ...adjustment, amountExact, currency } });
  assert.ok(text(p.render()).includes(expected));
});
test('an unverifiable adjustment amount leaves approval disabled', () => {
  const p = page({ ready: true, pendingAdjustment: { ...adjustment, amountExact: undefined } });
  const view = p.render();
  assert.match(text(view), /Amount unavailable/);
  assert.equal(nodes(view, node => node.type === 'button' && text(node) === 'Approve')[0].props.disabled, true);
});

// Actual decision handler: null is Cancel/Escape; empty string is deliberate OK.
for (const label of ['Approve','Reject']) test(label+' prompt cancellation sends no mutation or refresh',async()=>{
  const p=page({ready:true,promptAnswer:null});
  nodes(p.render(),n=>n.type==='button'&&text(n)===label)[0].props.onClick();await flush();
  assert.equal(p.calls.length,0);assert.equal(p.writes.has(7),false);assert.equal(p.writes.has(5),false);
});
for (const note of ['', 'Approved exact synthetic discount']) test('explicit approval accepts optional note '+JSON.stringify(note),async()=>{
  const p=page({ready:true,promptAnswer:note});
  nodes(p.render(),n=>n.type==='button'&&text(n)==='Approve')[0].props.onClick();await flush();
  const writes=p.calls.filter(c=>c.method==='PATCH');assert.equal(writes.length,1);
  assert.deepEqual(JSON.parse(writes[0].body),{approve:true,notes:note});
});
for (const note of ['', '   ']) test('rejection requires a nonblank reason '+JSON.stringify(note),async()=>{
  const p=page({ready:true,promptAnswer:note});
  nodes(p.render(),n=>n.type==='button'&&text(n)==='Reject')[0].props.onClick();await flush();
  assert.equal(p.calls.length,0);assert.equal(p.writes.has(7),false);assert.match(p.writes.get(5),/rejection reason is required/);
});
test('explicit rejection preserves the exact supplied reason',async()=>{
  const p=page({ready:true,promptAnswer:'Synthetic reason'});
  nodes(p.render(),n=>n.type==='button'&&text(n)==='Reject')[0].props.onClick();await flush();
  const writes=p.calls.filter(c=>c.method==='PATCH');assert.equal(writes.length,1);
  assert.deepEqual(JSON.parse(writes[0].body),{approve:false,notes:'Synthetic reason'});
});
