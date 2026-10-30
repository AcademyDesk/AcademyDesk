// Controlled real TSX rendering; authenticated browser/SQL acceptance is separate.
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const flush = () => new Promise(resolve => setImmediate(resolve));
function text(node) {
  if (Array.isArray(node)) return node.map(text).join(' ');
  if (node == null || typeof node === 'boolean') return '';
  return typeof node === 'object' ? text(node.props?.children) : String(node);
}
function group(currency, grossBilled, approvedAdjustments, collected, reconciled) {
  return { currency, grossBilled, approvedAdjustments, collected, reconciled,
    outstanding: Math.max(0, grossBilled - approvedAdjustments - collected) };
}
function summary(groups) {
  return { ...(groups.length === 1 ? groups[0] : { currency: null, grossBilled: null,
    approvedAdjustments: null, collected: null, outstanding: null, reconciled: null }),
    currencyTotals: groups, hasMixedCurrencies: groups.length > 1, overdueInvoices: 2 };
}
async function page(kind, response, { failed = false } = {}) {
  const source = fs.readFileSync(path.resolve(__dirname, `../../apps/web/src/app/${kind}/page.tsx`), 'utf8');
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS,
    target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  const states = [], effects = []; let index = 0;
  const react = { useState(initial) { const key = index++; if (!(key in states)) states[key] = initial;
    return [states[key], value => { states[key] = typeof value === 'function' ? value(states[key]) : value; }]; },
    useEffect(fn) { effects.push(fn); } };
  const module = { exports: {} };
  new Function('require', 'module', 'exports', compiled)(name => {
    if (name === 'react') return react;
    if (name === 'react/jsx-runtime') return jsx;
    if (name === 'next/link') return { default: 'a' };
    if (name === '@/components/enterprise-page-state') return { EnterprisePageState: 'aside' };
    if (name === '@/components/workspace-nav') return { WorkspaceNav: 'nav' };
    if (name === '@/lib/api') return { academyApi: async url => ({ ok: !failed, status: failed ? 500 : 200,
      json: async () => url === '/api/academies' ? [{ id: 'a' }] : url.endsWith('/summary') ? response : [] }) };
    throw Error('Unexpected import ' + name);
  }, module, module.exports);
  const render = () => { index = 0; return module.exports.default(); };
  render(); effects[0](); await flush(); await flush();
  return text(render()).replace(/\s+/g, ' ');
}
for (const kind of ['finance', 'finance-summary']) {
  for (const currency of ['INR', 'USD']) test(`${kind}: single ${currency} identifies its real currency`, async () => {
    const shown = await page(kind, summary([group(currency, 1000, 200, 600, 400)]));
    assert.match(shown, new RegExp(currency));
    assert.match(shown, /1,000/);
    if (currency === 'USD') assert.doesNotMatch(shown, /₹[\s\d]/);
  });
  test(`${kind}: original INR/USD reproduction renders separate totals without a combined INR value`, async () => {
    const shown = await page(kind, summary([group('INR', 1000, 100, 600, 200), group('USD', 100, 10, 20, 10)]));
    assert.match(shown, /INR/); assert.match(shown, /USD/);
    assert.match(shown, /1,000/); assert.match(shown, /100/);
    assert.doesNotMatch(shown, /1,100|620|310|210/);
    assert.match(shown, /separately by currency/i);
  });
  test(`${kind}: malformed stored currency stays unavailable without crashing or defaulting INR`, async () => {
    const shown = await page(kind, summary([group('rupees', 123, 0, 20, 10)]));
    assert.match(shown, /currency unavailable/i);
    assert.doesNotMatch(shown, /₹[\s\d]|INR/);
  });
  test(`${kind}: an unfamiliar three-letter unit remains explicit without pretending it is INR`, async () => {
    const shown = await page(kind, summary([group('ZZZ', 123, 0, 20, 10)]));
    assert.match(shown, /ZZZ/); assert.doesNotMatch(shown, /₹[\s\d]|INR/);
  });
  test(`${kind}: an older response without currency groups is unavailable, never converted to zero`, async () => {
    const shown = await page(kind, { grossBilled: 1100, approvedAdjustments: 0, collected: 620,
      outstanding: 480, reconciled: 0, overdueInvoices: 2 });
    assert.match(shown, /Currency totals are unavailable/);
    assert.doesNotMatch(shown, /1,100|620|480|No financial activity/);
  });
  test(`${kind}: empty ledger has no invented INR money`, async () => {
    const shown = await page(kind, summary([]));
    assert.match(shown, /No financial activity/i);
    assert.doesNotMatch(shown, /₹[\s\d]|INR/);
  });
  test(`${kind}: summary failure remains an honest error`, async () => {
    assert.match(await page(kind, null, { failed: true }), /could not be loaded/);
  });
}
test('finance-summary: collection rates and unreconciled balances are calculated per currency', async () => {
  const shown = await page('finance-summary', summary([group('INR', 1000, 100, 450, 200), group('USD', 100, 20, 20, 10)]));
  assert.match(shown, /50%/); assert.match(shown, /25%/);
  assert.match(shown, /250/); assert.match(shown, /10/);
});
