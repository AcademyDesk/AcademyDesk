// Actual TSX with controlled hooks/API; not live React/browser/SQL evidence.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const baseline = process.env.QA_BATCH_FEEDBACK_BASELINE === '1';
const compile = file => ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: {
  module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
function nodes(n, predicate) { if (Array.isArray(n)) return n.flatMap(x => nodes(x, predicate)); if (!n || typeof n !== 'object') return [];
  return [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)]; }
function text(n) { return Array.isArray(n) ? n.map(text).join('') : n == null ? '' : typeof n === 'object' ? text(n.props?.children) : String(n); }
function lockedControls(n, inherited = false) {
  if (Array.isArray(n)) return n.every(x => lockedControls(x, inherited));
  if (!n || typeof n !== 'object') return true;
  const disabled = inherited || (n.type === 'fieldset' && n.props.disabled);
  if (n.type === 'StandardSelectField' && !n.props.disabled) return false; // Its menu portals outside the fieldset.
  if (['button','input','StandardDateField'].includes(n.type) && !(disabled || n.props.disabled)) return false;
  return lockedControls(n.props?.children, disabled);
}
const flush = () => new Promise(resolve => setImmediate(resolve));
const notice = { create: 'Batch created.', edit: 'Batch updated.', toggle: 'Batch marked inactive.', reactivate: 'Batch reactivated.' };
function page({ failure, refreshFailure, delayed, delayedRefresh, active = true, blank = false, academy = 'a' } = {}) {
  const row = { id: 'b', name: 'Synthetic batch', courseId: 'c', capacity: 10, waitlistCapacity: 4, deliveryMode: 'Hybrid',
    meetingLink: 'https://example.invalid/qa', meetingDaysJson: '[{"day":"Mon","startTime":"09:00"}]', classType: 'OneToOne',
    sessionMinutes: 45, sessionsPerWeek: 2, endDate: '2027-01-01', isActive: active };
  const initial = [[{ id: 'a', name: 'Synthetic academy' }], academy, [{ id: 'c', name: 'Synthetic course' }], [], [], [row], 'c', 'r',
    blank ? '  ' : 'New synthetic', '12', 'QA', '3', 'Hybrid', 'OneToOne', '45', '2', 'https://example.invalid/qa',
    ['Mon', 'Fri'], { Mon: '09:00', Fri: '17:45' }, 'QA Room', 'Closed', '2026-10-02', '', null, '', '', '', '', '10', '', null, null];
  const state = new Map(), refs = [], calls = []; let index = 0, refIndex = 0, committed = false, release;
  const gate = new Promise(resolve => { release = resolve; });
  function execute(file) {
    const module = { exports: {} };
    new Function('require', 'module', 'exports', compile(file))(name => {
      if (name === 'react/jsx-runtime') return jsx;
      if (name === 'react') return { useState: value => { const key = index++; return [state.has(key) ? state.get(key) : initial[key] === undefined ? value : initial[key],
        value => state.set(key, typeof value === 'function' ? value(state.has(key) ? state.get(key) : initial[key]) : value)]; },
        useRef: value => refs[refIndex++] ??= { current: value }, useEffect: () => {} };
      if (name === 'next/link') return { default: 'a' };
      if (name === 'next/navigation') return { usePathname: () => '/batch-setup' };
      if (name === '@/components/workspace-nav') return { WorkspaceNav: 'nav' };
      if (name.startsWith('@/components/design-system/')) return new Proxy({}, { get: (_, key) => key });
      if (name === '@/lib/batch-update') return execute('apps/web/src/lib/batch-update.ts');
      if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
        calls.push({ url, ...options });
        if (options.method) {
          if (delayed) await gate;
          if (failure === 'network') throw Error('Synthetic network');
          if (!failure) committed = true;
          return { ok: !failure, status: failure || (options.method === 'POST' ? 201 : 200), json: async () => ({ message: 'Synthetic validation detail.' }) };
        }
        if (delayedRefresh && committed) await gate;
        if (refreshFailure === 'network') throw Error('Synthetic refresh');
        return { ok: !refreshFailure || refreshFailure === 'json', status: refreshFailure || 200,
          json: async () => { if (refreshFailure === 'json') throw Error('Synthetic JSON'); return url.endsWith('/courses') ? initial[2] : url.endsWith('/batches') ? [{ ...row, name: 'Persisted synthetic' }] : []; } };
      } };
      throw Error(name);
    }, module, module.exports); return module.exports;
  }
  const component = execute('apps/web/src/app/batches/page.tsx').default;
  const render = () => { index = 0; refIndex = 0; return component(); };
  const value = key => state.has(key) ? state.get(key) : initial[key];
  const button = label => nodes(render(), n => n.type === 'button' && text(n).trim() === label)[0];
  const edit = () => { button('Edit').props.onClick(); if (blank) state.set(24, '  '); };
  const action = kind => kind === 'create' ? nodes(render(), n => n.type === 'form')[0].props.onSubmit({ preventDefault() {} }) :
    button(kind === 'edit' ? 'Save' : active ? 'Deactivate' : 'Reactivate').props.onClick();
  const settle = async () => { await flush(); await flush(); };
  return { state, row, initial, value, render, button, edit, action, calls, release, settle, setFailure: value => { failure = value; } };
}
for (const kind of ['create', 'edit', 'toggle', 'reactivate']) for (const refreshFailure of baseline ? [undefined] : [undefined, 500, 'network', 'json'])
  test(`${kind}: confirmed notice survives ${refreshFailure || 'successful'} refresh`, async () => {
    const p = page({ refreshFailure, active: kind !== 'reactivate' }); if (kind === 'edit') p.edit();
    await p.action(kind); await p.settle(); assert.ok(p.value(22).includes(notice[kind]));
    if (!baseline) assert.ok(text(nodes(p.render(), n => n.props.role === 'status')[0]).includes(notice[kind]));
    if (refreshFailure) assert.match(p.value(22), /could not be refreshed/); else assert.ok(text(p.render()).includes('Persisted synthetic'));
    assert.equal(p.value(30), null); if (kind === 'edit') assert.equal(p.value(23), null);
    assert.equal(p.calls.filter(x => x.method).length, 1);
  });
for (const kind of ['create', 'edit', 'toggle']) for (const failure of baseline ? [400, 403] : [400, 403, 500, 'network'])
  test(`${kind}: ${failure} preserves entered details and releases pending state`, async () => {
    const p = page({ failure }); if (kind === 'edit') p.edit();
    await p.action(kind); await p.settle(); assert.ok(!p.value(22).includes(notice[kind]));
    assert.match(p.value(22), failure === 'network' || failure === 500 ? /could not be confirmed/ : /could not be (saved|updated)/);
    assert.equal(p.value(30), null); assert.equal(p.calls.filter(x => !x.method).length, 0);
    for (let key = 7; key <= 21; key++) assert.deepEqual(p.value(key), p.initial[key]);
    if (kind === 'edit') { assert.equal(p.value(23), 'b'); assert.equal(p.value(24), p.row.name); }
  });
for (const kind of ['create', 'edit', 'toggle']) test(`${kind}: pending save blocks duplicates/opposing writes and controls`, async () => {
  const p = page({ delayed: true }); if (kind === 'edit') p.edit(); const first = p.action(kind), duplicate = p.action(kind);
  if (!baseline) p.action(kind === 'create' ? 'toggle' : 'create');
  try {
    assert.equal(p.calls.filter(x => x.method).length, 1);
    if (!baseline) {
      const tree = p.render(); assert.ok(lockedControls(tree));
      const academy = nodes(tree, n => n.props?.name === 'academy')[0]; academy.props.onChange('other'); assert.equal(p.value(1), 'a');
      if (kind === 'edit') p.button('Cancel').props.onClick(); assert.equal(p.value(23), kind === 'edit' ? 'b' : null);
    }
  } finally { p.release(); await Promise.all([first, duplicate]); await p.settle(); }
  assert.equal(p.value(30), null);
});
for (const kind of ['create', 'edit']) test(`${kind}: blank name does not send a request`, async () => {
  const p = page({ blank: true }); if (kind === 'edit') p.edit(); await p.action(kind); await p.settle(); assert.equal(p.calls.length, 0);
});
if (!baseline) {
  test('Same-render create handler blocks a second submit before pending state renders', async () => {
    const p = page({ delayed: true }); const submit = nodes(p.render(), n => n.type === 'form')[0].props.onSubmit;
    const first = submit({ preventDefault() {} }), duplicate = submit({ preventDefault() {} });
    try { assert.equal(p.calls.filter(x => x.method).length, 1); }
    finally { p.release(); await Promise.all([first, duplicate]); await p.settle(); }
    assert.equal(p.value(30), null);
  });
  for (const kind of ['create', 'edit', 'toggle']) {
    test(`${kind}: delayed readback retains notice and lock`, async () => {
      const p = page({ delayedRefresh: true }); if (kind === 'edit') p.edit(); const first = p.action(kind); await p.settle();
      try { assert.ok(p.value(22).includes(notice[kind])); assert.notEqual(p.value(30), null); p.action(kind === 'edit' ? 'create' : 'toggle'); assert.equal(p.calls.filter(x => x.method).length, 1); }
      finally { p.release(); await first; await p.settle(); } assert.equal(p.value(30), null);
    });
    test(`${kind}: explicit retry after definite rejection clears old error`, async () => {
      const p = page({ failure: 400 }); if (kind === 'edit') p.edit(); await p.action(kind); await p.settle(); p.setFailure(undefined); await p.action(kind); await p.settle();
      assert.equal(p.value(22), notice[kind]); assert.equal(p.calls.filter(x => x.method).length, 2);
    });
  }
  test('Confirmed create resets all per-batch fields while keeping academy/course context', async () => {
    const p = page(); await p.action('create'); await p.settle();
    const expected = { 7: '', 8: '', 9: '10', 10: '', 11: '0', 12: 'InPerson', 13: 'Group', 14: '60', 15: '1', 16: '', 17: [], 18: {}, 19: '', 20: 'Open', 21: '' };
    for (const [key, value] of Object.entries(expected)) assert.deepEqual(p.value(Number(key)), value);
    assert.equal(p.value(1), 'a'); assert.equal(p.value(6), 'c');
    p.state.set(8, 'Another synthetic'); await p.action('create'); const body = JSON.parse(p.calls.filter(x => x.method)[1].body);
    assert.equal(body.meetingDaysJson, null); assert.equal(body.meetingLink, null); assert.equal(body.classType, 'Group'); assert.equal(body.capacity, 10);
  });
  for (const kind of ['create','edit','toggle']) test(`${kind}: no academy means no mutation`, async () => {
    const p = page({ academy: '' }); if (kind === 'edit') p.edit(); await p.action(kind); await p.settle(); assert.equal(p.calls.length, 0);
  });
}
