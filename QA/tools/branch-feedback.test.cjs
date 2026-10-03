// Actual page TSX with controlled hooks/requests, not live React/browser/SQL evidence.
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const compiled = ts.transpileModule(fs.readFileSync('apps/web/src/app/branches/page.tsx', 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
const branch = { id: 'b', name: 'Synthetic branch', city: null, state: null, isActive: true };
const baseline = process.env.QA_BRANCH_BASELINE === '1';
function nodes(n, predicate) { if (Array.isArray(n)) return n.flatMap(x => nodes(x, predicate)); if (!n || typeof n !== 'object') return [];
  return [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)]; }
function text(n) { if (Array.isArray(n)) return n.map(text).join(''); if (n == null || typeof n === 'boolean') return ''; return typeof n === 'object' ? text(n.props?.children) : String(n); }
const flush = () => new Promise(resolve => setImmediate(resolve));
function page({ failure, refreshFailure, active = true, blankCreate = false, blankEdit = false, delayed = false, delayedRefresh = false, delayedRead = false, readStatus = 200 } = {}) {
  const row = { ...branch, isActive: active };
  const initial = [[{ id: 'a', name: 'Synthetic academy' }], 'a', [row], blankCreate ? ' ' : 'New synthetic', '', '', '', null, '', '', '', null];
  const states = new Map(), refs = [], effects = [], calls = []; let index = 0, refIndex = 0, committed = false, release;
  const gate = new Promise(resolve => { release = resolve; });
  const react = { useState: value => { const key = index++; return [states.has(key) ? states.get(key) : initial[key] ?? value, value => states.set(key, typeof value === 'function' ? value(states.has(key) ? states.get(key) : initial[key]) : value)]; },
    useRef: value => { const key = refIndex++; return refs[key] ??= { current: value }; }, useEffect: fn => effects.push(fn) };
  const module = { exports: {} };
  new Function('require', 'module', 'exports', compiled)(name => {
    if (name === 'react') return react; if (name === 'react/jsx-runtime') return jsx;
    if (name === '@/components/design-system/controls') return { StandardSelectField: 'select' };
    if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
      calls.push({ url, ...options }); const write = options.method && options.method !== 'GET';
      if (write) {
        if (delayed) await gate;
        if (failure === 'network') throw Error('Synthetic network');
        if (!failure) committed = true;
        return { ok: !failure, status: failure || (options.method === 'POST' ? 201 : 200), json: async () => row };
      }
      if (committed && delayedRefresh) await gate;
      if (!committed && delayedRead) await gate;
      if (committed && refreshFailure === 'network') throw Error('Synthetic refresh network');
      if (committed && refreshFailure === 'json') return { ok: true, json: async () => { throw Error('Synthetic JSON'); } };
      return { ok: !(committed && refreshFailure) && readStatus === 200, status: committed && refreshFailure ? 500 : readStatus,
        json: async () => url === '/api/academies' ? initial[0] : committed ? [{ ...row, name: 'Persisted synthetic' }] : [row] };
    } };
    throw Error('Unexpected import ' + name);
  }, module, module.exports);
  function render() { index = 0; refIndex = 0; return module.exports.default(); }
  function button(label) { return nodes(render(), n => n.type === 'button' && text(n) === label)[0]; }
  function edit() { button('Edit').props.onClick(); if (blankEdit) states.set(8, ' '); }
  function action(kind) {
    if (kind === 'create') return nodes(render(), n => n.type === 'form')[0].props.onSubmit({ preventDefault() {} });
    if (kind === 'edit') return button('Save').props.onClick();
    return button(active ? 'Deactivate' : 'Reactivate').props.onClick();
  }
  // Void onClick handlers do not expose promises; observe controlled async state instead.
  async function settle() { await flush(); await flush(); }
  render(); return { render, states, value: key => states.has(key) ? states.get(key) : initial[key], calls, button, edit, action, settle, release, effects, row, setFailure: value => { failure = value; } };
}
if (!baseline) {
  test('Catalogue loading preserves a selected academy and loads supported data', async () => {
    const p = page(); p.effects[0](); await p.settle(); assert.deepEqual(p.value(0), [{ id: 'a', name: 'Synthetic academy' }]); assert.equal(p.value(1), 'a');
  });
  test('Branch loading presents the current academy list without a false success', async () => {
    const p = page(); p.effects[1](); await p.settle(); assert.deepEqual(p.value(2), [p.row]); assert.equal(p.value(6), '');
  });
  for (const effect of [0, 1]) test(`Loading effect ${effect} does not update state after cleanup`, async () => {
    const p = page({ delayedRead: true }); const cleanup = p.effects[effect](); cleanup(); p.release(); await p.settle(); assert.equal(p.states.size, 0);
  });
  for (const effect of [0, 1]) test(`Loading effect ${effect} reports rejected reads`, async () => {
    const p = page({ readStatus: 403 }); p.effects[effect](); await p.settle(); assert.match(p.value(6), effect === 0 ? /not reachable/ : /could not be loaded/);
  });
}
const notice = { create: 'Branch created.', edit: 'Branch updated.', toggle: 'Branch marked inactive.', reactivate: 'Branch reactivated.' };
for (const kind of ['create', 'edit', 'toggle', 'reactivate']) for (const refreshFailure of baseline ? [undefined] : [undefined, 500, 'network', 'json'])
  test(`${kind}: confirmed success survives ${refreshFailure ? 'failed ' + refreshFailure : 'successful'} refresh`, async () => {
    const p = page({ active: kind !== 'reactivate', refreshFailure }); if (kind === 'edit') p.edit();
    await p.action(kind); await p.settle(); assert.match(p.states.get(6), new RegExp(notice[kind].replace('.', '\\.')));
    assert.ok(text(nodes(p.render(), n => n.props?.role === 'status')[0]).includes(notice[kind]));
    if (refreshFailure) assert.match(p.states.get(6), /could not be refreshed/); else assert.ok(text(p.render()).includes('Persisted synthetic'));
    assert.equal(p.value(11), null); if (kind === 'create') for (const field of [3, 4, 5]) assert.equal(p.value(field), '');
    if (kind === 'edit') assert.equal(p.states.get(7), null);
    const writes = p.calls.filter(x => x.method); assert.equal(writes.length, 1); assert.ok(writes[0].url.startsWith('/api/academies/a/branches'));
    const body = JSON.parse(writes[0].body); if (kind === 'create') assert.equal(body.name, 'New synthetic');
    if (kind === 'edit') { assert.equal(body.name, branch.name); assert.equal(body.city, null); assert.equal(body.state, null); assert.equal(body.isActive, true); }
    if (kind === 'toggle' || kind === 'reactivate') assert.equal(body.isActive, kind === 'reactivate');
  });
for (const kind of ['create', 'edit', 'toggle']) for (const failure of baseline ? [400, 403] : [400, 403, 500, 'network'])
  test(`${kind}: ${failure} retains details, unlocks actions and never shows success`, async () => {
    const p = page({ failure }); if (kind === 'edit') p.edit();
    await p.action(kind); await p.settle(); assert.ok(!p.states.get(6).includes(notice[kind]));
    assert.match(p.states.get(6), failure === 'network' || failure >= 500 ? /could not be confirmed/ : /could not be (saved|updated)/);
    assert.equal(p.value(11), null); assert.equal(p.calls.filter(x => !x.method).length, 0);
    if (kind === 'create') assert.equal(p.states.get(3) ?? 'New synthetic', 'New synthetic');
    if (kind === 'edit') { assert.equal(p.states.get(7), 'b'); assert.equal(p.states.get(8), branch.name); }
    assert.equal(p.row.isActive, true);
  });
for (const kind of ['create', 'edit', 'toggle']) test(`${kind}: pending save blocks duplicate/opposing requests and academy change`, async () => {
  const p = page({ delayed: true }); if (kind === 'edit') p.edit();
  const first = p.action(kind); const duplicate = p.action(kind);
  if (!baseline) p.action(kind === 'create' ? 'toggle' : 'create');
  try {
    assert.equal(p.calls.filter(x => x.method).length, 1);
    const tree = p.render(); assert.ok(nodes(tree, n => n.type === 'button').every(n => n.props.disabled));
    assert.equal(nodes(tree, n => n.type === 'select')[0].props.disabled, true);
    assert.ok(nodes(tree, n => n.type === 'input').every(n => n.props.disabled));
  } finally { p.release(); await Promise.all([first, duplicate]); await p.settle(); }
  assert.equal(p.states.get(11), null); assert.match(p.states.get(6), /Branch (created|updated|marked inactive)/);
});
test('Blank edit reports validation without a request and leaves editor open', async () => {
  const p = page({ blankEdit: true }); p.edit(); p.action('edit'); await p.settle(); assert.equal(p.calls.length, 0); assert.match(p.states.get(6), /name is required/); assert.equal(p.states.get(7), 'b');
});
test('Blank create does not issue a request', async () => { const p = page({ blankCreate: true }); await p.action('create'); assert.equal(p.calls.length, 0); });
if (!baseline) for (const kind of ['create', 'edit', 'toggle']) {
  test(`${kind}: pending refresh retains success and stays locked until readback ends`, async () => {
    const p = page({ delayedRefresh: true }); if (kind === 'edit') p.edit(); const first = p.action(kind); await p.settle();
    try {
      assert.match(p.states.get(6), new RegExp(notice[kind].replace('.', '\\.'))); assert.notEqual(p.value(11), null);
      p.action(kind === 'edit' ? 'create' : 'toggle'); assert.equal(p.calls.filter(x => x.method).length, 1);
    } finally { p.release(); await first; await p.settle(); }
    assert.equal(p.value(11), null); assert.match(p.states.get(6), new RegExp(notice[kind].replace('.', '\\.')));
  });
  test(`${kind}: explicit retry after rejection succeeds without a stale error`, async () => {
    const p = page({ failure: 403 }); if (kind === 'edit') p.edit(); await p.action(kind); await p.settle();
    assert.match(p.states.get(6), /could not be (saved|updated)/); p.setFailure(undefined); await p.action(kind); await p.settle();
    assert.equal(p.calls.filter(x => x.method).length, 2); assert.equal(p.value(11), null); assert.equal(p.states.get(6), notice[kind]);
  });
}
