// Execute the actual page with controlled hook state. Not a live browser/React lifecycle test.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript');
const jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const source = fs.readFileSync(path.resolve(__dirname, '../../apps/web/src/app/payments/page.tsx'), 'utf8');
const compiled = ts.transpileModule(source, { compilerOptions: {
  module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX,
} }).outputText;
const money = amount => new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(amount);
const invoice = { id: 'i', invoiceNumber: 'QA-I', studentId: 's', totalAmount: 1000,
  adjustedAmount: 200, paidAmount: 700, balance: 100, currency: 'INR', dueDate: '2026-10-01', status: 'PartiallyPaid' };
const payments = [{ id: 'r', invoiceId: 'i', amount: 600, status: 'Reconciled' },
  { id: 'c', invoiceId: 'i', amount: 100, status: 'Completed' },
  { id: 'v', invoiceId: 'i', amount: 75, status: 'Voided' }];
function render(invoices = [invoice], rows = payments, selected = 'i') {
  const states = [{ id: 'a' }, [{ id: 's', firstName: 'Synthetic', lastName: 'Student' }],
    invoices, rows, selected, '', 'UPI', '', ''];
  const writes = new Map(); let index = 0;
  const react = { useState: () => { const key = index++; return [states[key], value => writes.set(key, value)]; },
    useEffect: () => {}, useMemo: fn => fn() };
  const module = { exports: {} };
  new Function('require', 'module', 'exports', compiled)(name => {
    if (name === 'react') return react;
    if (name === 'react/jsx-runtime') return jsx;
    if (name === '@/lib/api') return { academyApi: () => { throw Error('No network in controlled page test'); } };
    throw Error('Unexpected import ' + name);
  }, module, module.exports);
  return { tree: module.exports.default(), writes };
}
function all(node, predicate) {
  if (Array.isArray(node)) return node.flatMap(child => all(child, predicate));
  if (!node || typeof node !== 'object') return [];
  return [...(predicate(node) ? [node] : []), ...all(node.props?.children, predicate)];
}
function text(node) {
  if (Array.isArray(node)) return node.map(text).join('');
  if (node == null || typeof node === 'boolean') return '';
  return typeof node === 'object' ? text(node.props?.children) : String(node);
}
function kpi(tree, label) {
  const tile = all(tree, node => node.type === 'article').find(node =>
    all(node, child => child.type === 'span').some(child => text(child) === label));
  return text(all(tile, node => node.type === 'strong')[0]);
}
test('Collected counts Completed plus Reconciled and excludes Voided', () => {
  assert.equal(kpi(render().tree, 'Collected'), money(700));
});
test('Outstanding uses canonical API balance after applied adjustment', () => {
  assert.equal(kpi(render().tree, 'Outstanding'), money(100));
});
test('invoice option and directory show the API balance', () => {
  const { tree } = render();
  const option = all(tree, n => n.type === 'option' && n.props.value === 'i')[0];
  assert.match(text(option), new RegExp('Balance ' + money(100).replace(/[.*+?^${}()|[\]\\]/g, '\\$&')));
  const directory = all(tree, n => n.type === 'section' && n.props.className?.includes('payments-directory'))[0];
  assert.ok(text(directory).includes('Balance ' + money(100)));
});
test('selected detail and amount maximum use same canonical balance', () => {
  const { tree } = render();
  const detail = all(tree, n => n.props?.className === 'payments-selected')[0];
  assert.ok(text(detail).includes('Balance ' + money(100)));
  assert.equal(all(tree, n => n.type === 'input' && n.props.type === 'number')[0].props.max, 100);
});
test('selecting invoice pre-fills only its API outstanding amount', () => {
  const { tree, writes } = render();
  all(tree, n => n.type === 'select')[0].props.onChange({ target: { value: 'i' } });
  assert.equal(writes.get(5), '100');
});
test('fully adjusted invoice is not open or selectable for collection', () => {
  const { tree } = render([{ ...invoice, adjustedAmount: 1000, paidAmount: 0, balance: 0, status: 'Paid' }], [], '');
  assert.equal(kpi(tree, 'Open balances'), '0');
  assert.equal(kpi(tree, 'Outstanding'), money(0));
  assert.equal(all(tree, n => n.type === 'option' && n.props.value === 'i').length, 0);
});
test('empty ledger remains zero and has no invoice collection options', () => {
  const { tree } = render([], [], '');
  assert.equal(kpi(tree, 'Collected'), money(0));
  assert.equal(kpi(tree, 'Outstanding'), money(0));
  assert.equal(all(tree, n => n.type === 'option').filter(n => n.props.value).length, 0);
});
