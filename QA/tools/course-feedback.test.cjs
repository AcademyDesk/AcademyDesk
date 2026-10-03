// Reuse established controlled TSX test pattern; not live React/browser/SQL evidence.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const baseline = process.env.QA_COURSE_FEEDBACK_BASELINE === '1';
const compiled = ts.transpileModule(fs.readFileSync('apps/web/src/app/courses/page.tsx', 'utf8'), { compilerOptions: {
  module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
function nodes(n, predicate) { if (Array.isArray(n)) return n.flatMap(x => nodes(x, predicate)); if (!n || typeof n !== 'object') return [];
  return [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)]; }
function text(n) { return Array.isArray(n) ? n.map(text).join('') : n == null ? '' : typeof n === 'object' ? text(n.props?.children) : String(n); }
const flush = () => new Promise(resolve => setImmediate(resolve));
const notice = { create: 'Course created.', edit: 'Course updated.', toggle: 'Course marked inactive.', reactivate: 'Course reactivated.' };
function page({ failure, refreshFailure, delayed, delayedRefresh, active = true, blank = false, academy = 'a', courseType = 'Tuition', level = 'QA level' } = {}) {
  const row = { id: 'c', name: 'Synthetic course', academyType: 'Music', level: 'Level1', description: 'Synthetic description', isActive: active,
    courseCode: null, subjectArea: null, durationMonths: null, weeklySessions: null, sessionMinutes: null,
    minimumAge: null, maximumAge: null, deliveryMode: null, prerequisites: null, learningOutcomes: null, isPublished: false };
  const initial = [[{ id: 'a', name: 'Synthetic academy' }], academy, [row], blank ? '  ' : 'New synthetic', courseType, level, '', null, '', 'Music', '', null];
  const state = new Map(), refs = [], calls = []; let index = 0, refIndex = 0, committed = false, release;
  const gate = new Promise(resolve => { release = resolve; });
  const module = { exports: {} };
  new Function('require', 'module', 'exports', compiled)(name => {
    if (name === 'react/jsx-runtime') return jsx;
    if (name === 'react') return { useState: value => { const key = index++; return [state.has(key) ? state.get(key) : initial[key] === undefined ? value : initial[key],
      value => state.set(key, typeof value === 'function' ? value(state.has(key) ? state.get(key) : initial[key]) : value)]; },
      useRef: value => refs[refIndex++] ??= { current: value }, useEffect: () => {} };
    if (name === '@/components/workspace-nav') return { WorkspaceNav: 'nav' };
    if (name === '@/components/design-system/controls') return { StandardSelectField: 'select' };
    if (name === '@/lib/course-update') {
      const helper = { exports: {} };
      const code = ts.transpileModule(fs.readFileSync('apps/web/src/lib/course-update.ts','utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText;
      new Function('module','exports',code)(helper,helper.exports); return helper.exports;
    }
    if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
      calls.push({ url, ...options });
      if (options.method) {
        if (delayed) await gate;
        if (failure === 'network') throw Error('Synthetic network');
        if (!failure) committed = true;
        return { ok: !failure, status: failure || (options.method === 'POST' ? 201 : 200) };
      }
      if (delayedRefresh && committed) await gate;
      if (refreshFailure === 'network') throw Error('Synthetic refresh');
      return { ok: !refreshFailure || refreshFailure === 'json', status: refreshFailure || 200,
        json: async () => { if (refreshFailure === 'json') throw Error('Synthetic JSON'); return url === '/api/academies' ? initial[0] : [{ ...row, name: 'Persisted synthetic' }]; } };
    } };
    throw Error(name);
  }, module, module.exports);
  const render = () => { index = 0; refIndex = 0; return module.exports.default(); };
  const value = key => state.has(key) ? state.get(key) : initial[key];
  const button = label => nodes(render(), n => n.type === 'button' && text(n).trim() === label)[0];
  const edit = () => { button('Edit').props.onClick(); if (blank) state.set(8, '  '); };
  const action = kind => kind === 'create' ? nodes(render(), n => n.type === 'form')[0].props.onSubmit({ preventDefault() {} }) :
    button(kind === 'edit' ? 'Save' : active ? 'Deactivate' : 'Reactivate').props.onClick();
  const settle = async () => { await flush(); await flush(); };
  return { state, row, initial, value, render, button, edit, action, calls, release, settle, setFailure: value => { failure = value; } };
}
for (const kind of ['create', 'edit', 'toggle', 'reactivate']) for (const refreshFailure of baseline ? [undefined] : [undefined, 500, 'network', 'json'])
  test(`${kind}: confirmed notice survives ${refreshFailure || 'successful'} refresh`, async () => {
    const p = page({ refreshFailure, active: kind !== 'reactivate' }); if (kind === 'edit') p.edit();
    await p.action(kind); await p.settle(); assert.ok(p.value(6).includes(notice[kind]));
    if (!baseline) assert.ok(text(nodes(p.render(), n => n.props.role === 'status')[0]).includes(notice[kind]));
    if (refreshFailure) assert.match(p.value(6), /could not be refreshed/); else assert.ok(text(p.render()).includes('Persisted synthetic'));
    assert.equal(p.value(11), null); if (kind === 'edit') assert.equal(p.value(7), null);
    const writes = p.calls.filter(x => x.method); assert.equal(writes.length, 1);
    assert.equal(writes[0].url, '/api/academies/a/courses' + (kind === 'create' ? '' : '/c'));
    const body = JSON.parse(writes[0].body);
    // Retain exact payload checks; PUT intentionally expands to the full replacement contract.
    assert.deepEqual(body, kind === 'create' ? { name: p.initial[3], academyType: p.initial[4], level: p.initial[5] } : {
      name: p.row.name, academyType: p.row.academyType, level: p.row.level, description: p.row.description,
      durationMonths: null, isActive: kind === 'edit' ? p.row.isActive : !p.row.isActive,
      courseCode: null, subjectArea: null, weeklySessions: null, sessionMinutes: null,
      minimumAge: null, maximumAge: null, deliveryMode: null, prerequisites: null, learningOutcomes: null, isPublished: false });
  });
for (const kind of ['create', 'edit', 'toggle']) for (const failure of baseline ? [400, 403] : [400, 403, 500, 'network'])
  test(`${kind}: ${failure} preserves details and releases pending state`, async () => {
    const p = page({ failure }); if (kind === 'edit') p.edit(); await p.action(kind); await p.settle();
    assert.ok(!p.value(6).includes(notice[kind])); assert.match(p.value(6), failure === 'network' || failure === 500 ? /could not be confirmed/ : /could not be (saved|updated)/);
    assert.equal(p.value(11), null); assert.equal(p.calls.filter(x => !x.method).length, 0); assert.equal(p.calls.filter(x => x.method).length, 1);
    for (const key of [3,4,5]) assert.deepEqual(p.value(key), p.initial[key]);
    if (kind === 'edit') { assert.equal(p.value(7), 'c'); assert.equal(p.value(8), p.row.name); assert.equal(p.value(9), p.row.academyType); assert.equal(p.value(10), p.row.level); }
  });
for (const kind of ['create','edit','toggle']) test(`${kind}: pending save blocks duplicate/opposing writes and controls`, async () => {
  const p = page({ delayed: true }); if (kind === 'edit') p.edit(); const first = p.action(kind), duplicate = p.action(kind);
  if (!baseline) p.action(kind === 'create' ? 'toggle' : 'create');
  try {
    assert.equal(p.calls.filter(x => x.method).length, 1);
    if (!baseline) {
      const tree = p.render(); assert.ok(nodes(tree, n => ['button','input','select'].includes(n.type)).every(n => n.props.disabled));
      nodes(tree, n => n.props?.name === 'academy')[0].props.onChange('other'); assert.equal(p.value(1), 'a');
      if (kind === 'edit') { p.button('Cancel').props.onClick(); assert.equal(p.value(7), 'c'); }
    }
  } finally { p.release(); await Promise.all([first, duplicate]); await p.settle(); }
  assert.equal(p.value(11), null);
});
for (const kind of ['create','edit']) test(`${kind}: blank name sends no request`, async () => {
  const p = page({ blank: true }); if (kind === 'edit') p.edit(); await p.action(kind); await p.settle(); assert.equal(p.calls.length, 0);
});
if (!baseline) {
  test('Same-render create callback blocks duplicate submit before pending state renders', async () => {
    const p = page({ delayed: true }), submit = nodes(p.render(), n => n.type === 'form')[0].props.onSubmit;
    const first = submit({ preventDefault() {} }), duplicate = submit({ preventDefault() {} });
    try { assert.equal(p.calls.filter(x => x.method).length, 1); } finally { p.release(); await Promise.all([first,duplicate]); await p.settle(); }
  });
  for (const kind of ['create','edit','toggle']) {
    test(`${kind}: delayed readback keeps success and lock`, async () => {
      const p = page({ delayedRefresh: true }); if (kind === 'edit') p.edit(); const first = p.action(kind); await p.settle();
      try { assert.ok(p.value(6).includes(notice[kind])); assert.notEqual(p.value(11), null); p.action(kind === 'edit' ? 'create' : 'toggle'); assert.equal(p.calls.filter(x => x.method).length, 1); }
      finally { p.release(); await first; await p.settle(); } assert.equal(p.value(11), null);
    });
    test(`${kind}: explicit retry after definite rejection succeeds`, async () => {
      const p = page({ failure: 400 }); if (kind === 'edit') p.edit(); await p.action(kind); await p.settle(); p.setFailure(undefined); await p.action(kind); await p.settle();
      assert.equal(p.value(6), notice[kind]); assert.equal(p.calls.filter(x => x.method).length, 2);
    });
    test(`${kind}: missing academy sends no request`, async () => {
      const p = page({ academy: '' }); if (kind === 'edit') p.edit(); await p.action(kind); await p.settle(); assert.equal(p.calls.length, 0);
    });
  }
  for (const courseType of ['Music','Tuition','Coaching']) for (const level of ['', 'QA level'])
    test(`create ${courseType}/${level ? 'populated-level' : 'optional-blank-level'}: payload, reset and next create`, async () => {
      const p = page({ courseType, level }); await p.action('create'); await p.settle();
      assert.deepEqual(JSON.parse(p.calls.find(x => x.method).body), { name: 'New synthetic', academyType: courseType, level });
      assert.equal(p.value(3), ''); assert.equal(p.value(4), 'Music'); assert.equal(p.value(5), ''); assert.equal(p.value(1), 'a');
      p.state.set(3, 'Another synthetic'); await p.action('create'); await p.settle();
      assert.deepEqual(JSON.parse(p.calls.filter(x => x.method)[1].body), { name: 'Another synthetic', academyType: 'Music', level: '' });
    });
}
