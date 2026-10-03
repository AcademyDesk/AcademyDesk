// Controlled actual TSX rendering/handlers, not real browser or mobile evidence.
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
function nodes(n, predicate) { if (Array.isArray(n)) return n.flatMap(x => nodes(x, predicate)); if (!n || typeof n !== 'object') return [];
  return [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)]; }
function text(n) { if (Array.isArray(n)) return n.map(text).join(''); if (n == null || typeof n === 'boolean') return '';
  return typeof n === 'object' ? text(n.props?.children) : String(n); }
const summary = { grossBilled: 0, collected: 0, outstanding: 0, reconciled: 0, overdueInvoices: 1 };
function page(kind, invoice) {
  const source = fs.readFileSync(path.resolve(__dirname, `../../apps/web/src/app/${kind}/page.tsx`), 'utf8');
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  const states = kind === 'finance' ? [{ id: 'a' }, summary, [invoice], '', undefined]
    : [{ id: 'a' }, [], [invoice], [], { invoiceTemplateKey: 'Classic', payslipTemplateKey: 'Standard' }, '', undefined, false, 'Invoice', false];
  let index = 0; const writes = new Map(), effects = [], calls = [];
  const react = { useState: initial => { const key = index++; return [writes.has(key) ? writes.get(key) : states[key], value => writes.set(key, value)]; }, useEffect: f => effects.push(f) };
  const module = { exports: {} };
  new Function('require', 'module', 'exports', compiled)(name => {
    if (name === 'react') return react; if (name === 'react/jsx-runtime') return jsx; if (name === 'next/link') return { default: 'a' };
    if (name === '@/components/enterprise-page-state') return { EnterprisePageState: 'aside' };
    if (name === '@/lib/api') return { academyApi: async url => { calls.push(url); return { ok: true, status: 200, json: async () => url === '/api/academies' ? [{ id: 'a' }]
      : url.endsWith('/collections') ? [invoice] : url.endsWith('/summary') ? summary : url.endsWith('/settings') ? states[4] : [] }; }, apiHeaders: () => ({}) };
    throw Error('Unexpected import ' + name);
  }, module, module.exports);
  function render() { index = 0; return module.exports.default(); }
  render(); return { render, writes, calls, async load() { effects[0](); await new Promise(resolve => setImmediate(resolve)); } };
}
for (const kind of ['finance', 'finance-governance']) for (const [currency, balance, shown] of [['INR', 500, '₹500.00'], ['INR', 0.01, '₹0.01'], ['USD', 400, '$400.00']])
  test(`${kind} queue: ${currency} remaining ${balance}, not face total`, () => {
    const p = page(kind, { id: 'i', invoiceNumber: 'QA-COL', totalAmount: 1000, adjustedAmount: 100, paidAmount: 400, balance, currency, dueDate: '2026-09-30', daysOverdue: 1 });
    const tree = p.render(); const container = kind === 'finance' ? nodes(tree, n => n.type === 'tbody')[0]
      : nodes(tree, n => n.type === 'section' && text(n).includes('Overdue collections'))[0];
    assert.ok(text(container).includes(shown), text(container)); assert.ok(!text(container).includes(currency === 'INR' ? '₹1,000' : '$1,000'));
    assert.match(text(tree), /Remaining due/);
  });
for (const [currency, balance, shown] of [['INR', 500, '₹500.00'], ['INR', 0.01, '₹0.01'], ['USD', 400, '$400.00']])
  test(`follow-up context preserves ${currency} remaining ${balance}`, () => {
    const p = page('finance-governance', { id: 'i', invoiceNumber: 'QA-COL', totalAmount: 1000, balance, currency, dueDate: '2026-09-30', daysOverdue: 1 });
    nodes(p.render(), n => n.type === 'button' && text(n) === 'Create follow-up →')[0].props.onClick();
    const form = nodes(p.render(), n => n.type === 'form' && text(n).includes('Create follow-up for'))[0];
    assert.ok(text(form).includes(shown), text(form)); assert.match(text(form), /Remaining due/); assert.ok(!text(form).includes('1,000'));
  });
for (const kind of ['finance', 'finance-governance']) test(`${kind} uses fresh collections response without fabricating gross fallback`, async () => {
  const invoice = { id: 'i', invoiceNumber: 'QA-COL', totalAmount: 1000, balance: 500, currency: 'INR', dueDate: '2026-09-30', daysOverdue: 1 };
  const p = page(kind, invoice); await p.load(); assert.ok(p.calls.includes('/api/academies/a/finance-governance/collections'));
  assert.deepEqual(p.writes.get(2), [invoice]); assert.ok(text(p.render()).includes('₹500.00'));
});
