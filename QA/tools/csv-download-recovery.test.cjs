// Actual Data Operations/Finance handlers; controlled React/DOM/transport, not SQL.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const compile = file => ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: {
  module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX,
}}).outputText;
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
const text = n => Array.isArray(n) ? n.map(text).join('') : n == null || typeof n === 'boolean' ? '' : typeof n === 'object' ? text(n.props?.children) : String(n);

async function page(kind, config = {}) {
  let index = 0, refIndex = 0, mounted = false;
  const state = new Map(), refs = [], effects = [], calls = [], blobs = [], revoked = [], links = [], module = { exports: {} };
  const react = {
    useState(initial) { const key = index++; if (!state.has(key)) state.set(key, initial); return [state.get(key), value => state.set(key, value)]; },
    useRef(initial) { const key = refIndex++; return refs[key] ??= { current: initial }; },
    useEffect(fn) { if (!mounted) effects.push(fn); },
  };
  let exportCount = 0;
  const api = async url => {
    calls.push(url);
    if (url.includes('/exports/')) {
      exportCount++;
      if (config.gate) await config.gate;
      if (config.network) throw Error('Synthetic transport failure');
      return { ok: !config.status, status: config.status || 200,
        headers: config.mime === null ? new Headers() : new Headers({ 'content-type': config.mime || 'text/csv;charset=utf-8' }),
        blob: async () => { if (config.bodyGate) await config.bodyGate; if (config.blobFailure) throw Error('Synthetic interrupted body'); return new Blob(['"safe"']); },
      };
    }
    return { ok: true, status: 200, json: async () => url === '/api/academies' ? [{ id: 'owned' }] : url.endsWith('/summary') ? {
      grossBilled: 0, approvedAdjustments: 0, collected: 0, outstanding: 0, reconciled: 0, overdueInvoices: 0,
    } : [] };
  };
  const dom = { createElement(tag) { assert.equal(tag, 'a'); const link = { click() { if (config.clickFailure) throw Error('Synthetic click failure'); links.push({ href: this.href, download: this.download }); } }; return link; } };
  const urls = { createObjectURL(blob) { blobs.push(blob); return 'blob:synthetic-' + blobs.length; }, revokeObjectURL(url) { revoked.push(url); } };
  new Function('require', 'module', 'exports', 'document', 'URL', compile(`apps/web/src/app/${kind}/page.tsx`))(name => {
    if (name === 'react') return react;
    if (name === 'react/jsx-runtime') return jsx;
    if (name === '@/lib/api') return { academyApi: api };
    if (name === 'next/link') return { default: 'a' };
    if (name === '@/components/enterprise-page-state') return { EnterprisePageState: props => jsx.jsx('p', props) };
    if (name === '@/lib/student-import-feedback') return { studentImportErrors: () => null };
    if (name.endsWith('.module.css')) return { default: {} };
    throw Error(name);
  }, module, module.exports, dom, urls);
  const render = () => { index = refIndex = 0; const tree = module.exports.default(); mounted = true; return tree; };
  render();
  for (const fn of effects.splice(0)) fn();
  const settle = async () => { for (let i = 0; i < 5; i++) { await new Promise(r => setImmediate(r)); render(); } };
  await settle();
  const buttons = () => nodes(render()).filter(n => n.type === 'button' && n.props.onClick &&
    (kind === 'data-operations' ? /Download CSV|Preparing download/.test(text(n)) : /Invoice ledger CSV|Payment ledger CSV/.test(text(n))));
  return { render, settle, calls, blobs, revoked, links, buttons,
    count: () => exportCount,
    notice: () => text(render()),
  };
}

for (const kind of ['data-operations', 'finance']) {
  test(`${kind}: successful CSV starts one correctly named download and revokes its URL`, async () => {
    const p = await page(kind);
    assert.ok(p.buttons().length >= 2);
    p.buttons()[0].props.onClick(); await p.settle();
    assert.equal(p.count(), 1); assert.equal(p.links.length, 1);
    assert.equal(p.links[0].download, kind === 'finance' ? 'academydesk-invoices.csv' : 'academydesk-students.csv');
    assert.deepEqual(p.revoked, ['blob:synthetic-1']);
    assert.match(p.notice(), /download started/); assert.doesNotMatch(p.notice(), /export downloaded/);
    assert.equal(p.buttons()[0].props.disabled, false);
  });
  for (const config of [{ network: true }, { status: 403 }, { status: 500 }, { mime: 'text/html' },
    { mime: 'application/json' }, { mime: null }, { blobFailure: true }, { clickFailure: true }]) {
    test(`${kind}: recovery ${JSON.stringify(config)} gives no false success or automatic retry`, async () => {
      const p = await page(kind, config);
      p.buttons()[0].props.onClick(); await p.settle();
      assert.equal(p.count(), 1); assert.equal(p.links.length, 0);
      assert.match(p.notice(), /could not/); assert.doesNotMatch(p.notice(), /download started/);
      assert.equal(p.buttons()[0].props.disabled, false);
      assert.equal(p.revoked.length, config.clickFailure ? 1 : 0);
    });
  }
  test(`${kind}: same-tick duplicate and opposing export stay blocked through body completion`, async () => {
    let release; const gate = new Promise(resolve => { release = resolve; });
    const p = await page(kind, { gate });
    const [first, opposing] = p.buttons();
    first.props.onClick(); first.props.onClick(); opposing.props.onClick(); await p.settle();
    assert.equal(p.count(), 1);
    assert.ok(p.buttons().every(button => button.props.disabled));
    release(); await p.settle();
    assert.equal(p.links.length, 1); assert.ok(p.buttons().every(button => !button.props.disabled));
    p.buttons()[1].props.onClick(); await p.settle();
    assert.equal(p.count(), 2); assert.equal(p.links.length, 2);
  });
  test(`${kind}: a received header does not release pending protection while body is unread`, async () => {
    let release; const bodyGate = new Promise(resolve => { release = resolve; });
    const p = await page(kind, { bodyGate });
    const [first, opposing] = p.buttons();
    first.props.onClick(); await p.settle(); opposing.props.onClick();
    assert.equal(p.count(), 1); assert.equal(p.links.length, 0);
    assert.ok(p.buttons().every(button => button.props.disabled));
    release(); await p.settle();
    assert.equal(p.links.length, 1); assert.equal(p.revoked.length, 1);
  });
}
