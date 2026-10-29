// Actual TSX controlled hooks/handlers, not live React/browser/device evidence.
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), path = require('node:path');
const cp = require('node:child_process');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const sourcePath = 'apps/web/src/app/finance-governance/page.tsx';
const source = process.env.QA_SOURCE_REF ? cp.execFileSync('git', ['show', process.env.QA_SOURCE_REF + ':' + sourcePath], { encoding: 'utf8' }) : fs.readFileSync(path.resolve(__dirname, '../..', sourcePath), 'utf8');
const compiled = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX },
}).outputText;
const item = { id: 'w', type: 'Collections', title: 'Synthetic collection', description: null, priority: 'High', status: 'Open', escalationStage: 'Initial', promisedPaymentDate: null };
const invoice = { id: 'i', invoiceNumber: 'QA-I', totalAmount: 100, balance: 100, currency: 'INR', dueDate: '2026-09-01', daysOverdue: 30 };
const adjustment = { id: 'd', type: 'Discount', amount: 1, reason: 'Synthetic', status: 'PendingApproval' };
const settings = { invoiceTemplateKey: 'Classic', payslipTemplateKey: 'Standard', settingsVersion: 'A'.repeat(64) };
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
function page({ ready = false, deniedRead = false, deniedWrite = false, failRefresh = false, allowGeneric = false, documentError } = {}) {
  const states = ready ? [{ id: 'a' }, [adjustment], [invoice], [item], settings, '', false, undefined, false, undefined, 'Invoice', false, false, true, false] : [];
  const writes = new Map(), effects = [], calls = [], refs = []; let index = 0, refIndex = 0, mutated = false;
  const react = { useState: initial => { const key = index++; return [writes.has(key) ? writes.get(key) : ready ? states[key] : initial,
    value => writes.set(key, typeof value === 'function' ? value(writes.has(key) ? writes.get(key) : states[key]) : value)]; }, useCallback: fn => fn, useEffect: fn => effects.push(fn), useRef: value => { const key = refIndex++; return refs[key] ??= { current: ready && key === 2 ? { ready: true, pending: false, review: false, snapshot: 0 } : value }; } };
  const module = { exports: {} };
  new Function('require', 'module', 'exports', 'FormData', 'window', compiled)(name => {
    if (name === 'react') return react;
    if (name === 'next/image') return { default: 'img' }; if (name === 'react/jsx-runtime') return jsx;
    if (name === 'next/link') return { default: 'a' };
    if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
      const write = options.method && options.method !== 'GET'; calls.push({ url, ...options });
      const generic = url.includes('/admin-work-items');
      if (write && url.endsWith('/settings') && documentError !== undefined) return { ok: false, status: 400, json: async () => ({ message: documentError }) };
      const status = (generic && !allowGeneric) || (write ? deniedWrite : deniedRead || (mutated && failRefresh)) ? 403 : 200;
      if (write && status === 200) mutated = true;
      return { ok: status === 200, status, json: async () => url === '/api/academies' ? [{ id: 'a' }]
        : url.endsWith('/collection-tasks') || generic ? [item] : url.endsWith('/collections') ? [invoice]
        : url.endsWith('/finance-adjustments') ? [adjustment] : url.endsWith('/settings') ? settings : {} };
    } };
    if (name.endsWith('.module.css')) return { default: {} };
    if (name === '@/components/design-system/form-fields') return { StandardFormDate: 'date', StandardFormSelect: 'form-select' };
    if (name === '@/components/design-system/controls') return { StandardSelectField: 'select' };
    throw Error('Unexpected import ' + name);
  }, module, module.exports, class { constructor(fields) { this.fields = fields; } get(key) { return this.fields[key] ?? null; } }, { prompt: () => '' });
  function render() { index = 0; refIndex = 0; return module.exports.default(); }
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
  const edit = nodes(p.render(), n => n.type === 'button' && text(n) === 'Edit document setup')[0];
  assert.equal(edit.props.disabled, true);
  edit.props.onClick(); assert.equal(nodes(p.render(), n => n.type === 'input' && n.props.name === 'invoiceAuthorityName').length, 0);
});
test('Initial loading cannot open the editor or suppress later settings hydration', async () => {
  const p = page(); const edit = nodes(p.render(), n => n.type === 'button' && text(n) === 'Edit document setup')[0];
  assert.equal(edit.props.disabled, true); edit.props.onClick();
  await p.initialise(); assert.deepEqual(p.writes.get(4), settings);
  const loaded = nodes(p.render(), n => n.type === 'button' && text(n) === 'Edit document setup')[0];
  assert.equal(loaded.props.disabled, false); loaded.props.onClick();
  assert.equal(nodes(p.render(), n => n.type === 'input' && n.props.name === 'invoiceAuthorityName').length, 1);
  assert.equal(p.calls.filter(x => x.method).length, 0);
});
test('Loaded editor opens and closes without a write', () => {
  const p = page({ ready: true }); let edit = nodes(p.render(), n => n.type === 'button' && text(n) === 'Edit document setup')[0];
  assert.equal(edit.props.disabled, false); edit.props.onClick();
  edit = nodes(p.render(), n => n.type === 'button' && text(n) === 'Close edit')[0]; edit.props.onClick();
  assert.equal(nodes(p.render(), n => n.type === 'input' && n.props.name === 'invoiceAuthorityName').length, 0); assert.equal(p.calls.length, 0);
});
for (const reason of ['Tax label must not exceed 30 characters. Your draft has not been saved.', 'Tax registration number must not exceed 50 characters. Your draft has not been saved.', null, { unexpected: true }, 'Untrusted server detail']) test('Document length rejection retains editor/version and safe guidance: ' + JSON.stringify(reason), async () => {
  const p = page({ ready: true, documentError: reason });
  nodes(p.render(), n => n.type === 'button' && text(n) === 'Edit document setup')[0].props.onClick();
  const form = nodes(p.render(), n => n.type === 'form' && nodes(n, c => c.type === 'input' && c.props.name === 'invoiceAuthorityName').length)[0];
  await p.submit(form, { invoiceAuthorityName: 'Retained draft', invoiceTemplateKey: 'Modern', payslipTemplateKey: 'Professional' });
  assert.equal(p.writes.get(5), typeof reason === 'string' && reason.startsWith('Tax ') ? reason : 'Document settings could not be saved.');
  assert.equal(p.writes.get(6), true); assert.equal(p.writes.get(11), true); assert.equal(p.writes.get(12), false);
  assert.equal(p.writes.has(4), false); assert.equal(p.calls.filter(x => x.method === 'PUT').length, 1);
  assert.equal(JSON.parse(p.calls.find(x => x.method === 'PUT').body).settingsVersion, settings.settingsVersion);
});
for (const scenario of ['saved', 'denied', 'refresh-failed']) test(`Escalation ${scenario}: narrow route, nullable date and honest notice`, async () => {
  const p = page({ ready: true, deniedWrite: scenario === 'denied', failRefresh: scenario === 'refresh-failed' });
  const form = nodes(p.render(), n => n.type === 'form' && nodes(n, c => c.type === 'form-select' && c.props.name === 'stage').length)[0];
  await p.submit(form, { stage: 'Reminder', promiseDate: '' });
  const write = p.calls.find(x => x.method === 'PATCH');
  assert.equal(write.url, '/api/academies/a/finance-governance/collection-tasks/w');
  assert.deepEqual(JSON.parse(write.body), { escalationStage: 'Reminder', promisedPaymentDate: null });
  assert.equal(p.writes.get(8), false);
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
    assert.equal(p.writes.get(7), undefined);
  } else {
    nodes(p.render(), n => n.type === 'button' && text(n) === 'Approve')[0].props.onClick();
    assert.equal(p.calls.filter(x => x.method).length, 0);
    const form = nodes(p.render(), n => n.type === 'form' && nodes(n, c => c.type === 'button' && text(c) === 'Confirm approval').length)[0];
    await p.submit(form, {});
  }
  assert.match(p.writes.get(5), kind === 'follow-up' ? /Collections follow-up created/ : /Adjustment approved/);
  if (failRefresh) assert.match(p.writes.get(5), /could not be refreshed/);
  assert.equal(p.writes.get(8), false);
});
