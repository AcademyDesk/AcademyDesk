// Actual TSX with controlled hooks/API; not browser/device/print evidence.
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const compiled = ts.transpileModule(fs.readFileSync('apps/web/src/app/invoices/page.tsx', 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
const student = { id: 's', firstName: 'Synthetic', lastName: 'Student', email: 'qa@example.invalid' };
const invoice = { id: 'i', studentId: 's', invoiceNumber: 'QA-I', totalAmount: 100, currency: 'INR', issuedDate: '2026-10-01', dueDate: '2026-10-08', status: 'Issued' };
const settings = { taxRegistrationNumber: 'QA-TAX', taxLabel: 'GST', invoiceLogoUrl: 'https://example.invalid/logo.png',
  invoiceSignatureUrl: 'https://example.invalid/sign.png', invoiceAuthorityName: 'Synthetic Signatory', invoiceAuthorityTitle: 'Finance', invoiceTemplateKey: 'Modern' };
const defaults = { taxRegistrationNumber: '', taxLabel: 'GST', invoiceLogoUrl: null, invoiceSignatureUrl: null, invoiceAuthorityName: null, invoiceAuthorityTitle: null, invoiceTemplateKey: 'Classic' };
function nodes(n, predicate) { if (Array.isArray(n)) return n.flatMap(x => nodes(x, predicate)); if (!n || typeof n !== 'object') return [];
  return [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)]; }
function text(n) { if (Array.isArray(n)) return n.map(text).join(''); if (n == null || typeof n === 'boolean') return ''; return typeof n === 'object' ? text(n.props?.children) : String(n); }
async function page({ documentStatus = 200, documentThrows = false, data = settings, legacyDenied = true } = {}) {
  const states = new Map(), effects = [], calls = []; let index = 0;
  const react = { useState: initial => { const key = index++; return [states.has(key) ? states.get(key) : initial, value => states.set(key, value)]; }, useEffect: f => effects.push(f), useMemo: f => f() };
  const module = { exports: {} };
  new Function('require', 'module', 'exports', compiled)(name => {
    if (name === 'react') return react; if (name === 'react/jsx-runtime') return jsx;
    if (name === '@/lib/api') return { academyApi: async url => {
      calls.push(url); const document = url.endsWith('/invoices/document-settings'), legacy = url.endsWith('/finance-governance/settings');
      if (document && documentThrows) throw Error('Synthetic network failure');
      const status = document ? documentStatus : legacy && legacyDenied ? 403 : 200;
      return { status, ok: status === 200, json: async () => url === '/api/academies' ? [{ id: 'a', name: 'Synthetic Academy', legalName: 'Synthetic Legal Academy' }]
        : url.endsWith('/student-options') ? [student] : url.endsWith('/invoices') ? [invoice] : document || legacy ? data : [] };
    }, apiHeaders: () => ({}) };
    throw Error('Unexpected import ' + name);
  }, module, module.exports);
  function render() { index = 0; return module.exports.default(); }
  render(); effects[0](); await new Promise(resolve => setImmediate(resolve)); return { render, calls, states };
}
test('Finance-only invoices load using seven-field document lookup, not Governance', async () => {
  const p = await page(); assert.ok(p.calls.includes('/api/academies/a/invoices/document-settings')); assert.ok(p.calls.every(x => !x.includes('/finance-governance/')));
  assert.equal(p.states.get(10), ''); assert.deepEqual(p.states.get(4), settings); assert.ok(text(p.render()).includes('QA-I'));
});
for (const theme of ['Classic', 'Modern', 'Minimal', 'Formal']) test(`Configured ${theme} preview preserves branding/tax/authority/contact`, async () => {
  const p = await page({ data: { ...settings, invoiceTemplateKey: theme } }); nodes(p.render(), n => n.type === 'button' && text(n) === 'Preview / Print')[0].props.onClick();
  const paper = nodes(p.render(), n => n.type === 'section' && n.props.className?.startsWith('invoice-paper invoice-theme-'))[0];
  assert.ok(paper.props.className.includes('invoice-theme-' + theme.toLowerCase())); assert.match(text(paper), /GST: QA-TAX/); assert.match(text(paper), /Synthetic Signatory/);
  assert.match(text(paper), /Synthetic Legal Academy/); assert.match(text(paper), /qa@example.invalid/);
  assert.equal(nodes(paper, n => n.type === 'img' && n.props.alt === 'Academy logo')[0].props.src, settings.invoiceLogoUrl);
  assert.equal(nodes(paper, n => n.type === 'img' && n.props.alt === 'Authorised signature')[0].props.src, settings.invoiceSignatureUrl);
});
test('No-settings default is Classic with fallback mark/signatory, not foreign data', async () => {
  const p = await page({ data: defaults }); nodes(p.render(), n => n.type === 'button' && text(n) === 'Preview / Print')[0].props.onClick();
  const paper = nodes(p.render(), n => n.type === 'section' && n.props.className?.startsWith('invoice-paper invoice-theme-'))[0];
  assert.ok(paper.props.className.includes('invoice-theme-classic')); assert.match(text(paper), /Authorised signatory/); assert.equal(nodes(paper, n => n.type === 'img').length, 0);
  assert.ok(!text(paper).includes('QA-TAX'));
});
for (const status of [401, 403, 500]) test(`Denied/error document lookup ${status} does not present successful loading`, async () => {
  const p = await page({ documentStatus: status, legacyDenied: false }); assert.match(p.states.get(10), /could not be loaded/); assert.equal(p.states.has(3), false); assert.equal(p.states.has(4), false);
});
test('Network failure remains an honest loading error', async () => {
  const p = await page({ documentThrows: true, legacyDenied: false }); assert.match(p.states.get(10), /could not be loaded/); assert.equal(p.states.has(4), false);
});
