// Actual TSX load handlers with controlled hooks/API. Not live browser evidence.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript');
const jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const student = { id: 's', firstName: 'Synthetic', lastName: 'Student', email: 'qa@example.invalid', phone: null };
const invoice = { id: 'i', studentId: 's', invoiceNumber: 'QA-I', totalAmount: 100, balance: 100,
  currency: 'INR', issuedDate: '2026-10-01', dueDate: '2026-10-08', status: 'Issued' };
async function loadPage(page, lookupStatus = 200) {
  const source = fs.readFileSync(path.resolve(__dirname, `../../apps/web/src/app/${page}/page.tsx`), 'utf8');
  const compiled = ts.transpileModule(source, { compilerOptions: {
    module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX,
  } }).outputText;
  const effects = [], calls = [], writes = new Map(); let index = 0;
  const react = { useState: initial => { const key = index++; return [writes.has(key) ? writes.get(key) : initial, value => writes.set(key, value)]; },
    useEffect: fn => effects.push(fn), useMemo: fn => fn() };
  const module = { exports: {} };
  new Function('require', 'module', 'exports', compiled)(name => {
    if (name === 'react') return react;
    if (name === 'react/jsx-runtime') return jsx;
    if (name === '@/lib/api') return { academyApi: async url => {
      calls.push(url);
      const lookup = url.endsWith('/invoices/student-options');
      const status = url.endsWith('/students') ? 403 : lookup ? lookupStatus : 200;
      return { ok: status === 200, status, json: async () => url === '/api/academies'
        ? [{ id: 'a', name: 'Synthetic academy' }] : lookup ? [student] : url.endsWith('/invoices') ? [invoice] : url.endsWith('/settings') ? {} : [] };
    } };
    throw Error('Unexpected import ' + name);
  }, module, module.exports);
  module.exports.default(); effects.forEach(fn => fn());
  // Resolve the actual async initialise/load promises; no network or real lifecycle.
  await new Promise(resolve => setImmediate(resolve));
  return { calls, writes, render: () => { index = 0; return module.exports.default(); } };
}
function nodes(node, predicate) {
  if (Array.isArray(node)) return node.flatMap(child => nodes(child, predicate));
  if (!node || typeof node !== 'object') return [];
  return [...(predicate(node) ? [node] : []), ...nodes(node.props?.children, predicate)];
}
function text(node) {
  if (Array.isArray(node)) return node.map(text).join('');
  if (node == null || typeof node === 'boolean') return '';
  return typeof node === 'object' ? text(node.props?.children) : String(node);
}
test('Payments retains resolved student names in the invoice selector and directory', async () => {
  const { render } = await loadPage('payments'); const tree = render();
  assert.ok(text(nodes(tree, n => n.type === 'option' && n.props.value === 'i')[0]).includes('Synthetic Student'));
  assert.ok(text(nodes(tree, n => n.props?.className?.includes('payments-directory'))[0]).includes('Synthetic Student'));
});
test('Invoices retains billing contact in the actual Preview/Print handler', async () => {
  const { render } = await loadPage('invoices');
  const preview = nodes(render(), n => n.type === 'button' && text(n) === 'Preview / Print')[0];
  assert.ok(preview); preview.props.onClick();
  const bill = nodes(render(), n => n.props?.className === 'invoice-paper-bill')[0];
  assert.ok(text(bill).includes('Synthetic Student')); assert.ok(text(bill).includes(student.email));
});
for (const page of ['payments', 'invoices']) {
  test(`${page}: FinanceUser loads via finance lookup, never student-management`, async () => {
    const { calls, writes } = await loadPage(page);
    assert.ok(calls.includes('/api/academies/a/invoices/student-options'));
    assert.ok(!calls.includes('/api/academies/a/students'));
    assert.deepEqual(writes.get(1), [student]);
    assert.equal(writes.get(page === 'payments' ? 8 : 10), '');
    if (page === 'invoices') assert.equal(writes.get(5), 's');
  });
  test(`${page}: denied lookup is not presented as successful/empty loading`, async () => {
    const { writes } = await loadPage(page, 403);
    assert.equal(writes.has(1), false);
    assert.match(writes.get(page === 'payments' ? 8 : 10), /could not be loaded/);
  });
}
