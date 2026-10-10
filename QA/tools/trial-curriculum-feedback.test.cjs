// Actual TSX handlers, synthetic hooks/transport. Not SQL or authorization acceptance.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const tick = () => new Promise(r => setImmediate(r));
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
const definitions = {
  trials: { route: 'trial-bookings', base: '/api/academies/owned/sales-marketing/trials', notice: 4, saving: 10, row: 3,
    drafts: [[5, 'lead'], [6, 'teacher'], [7, '2026-10-20'], [8, '11:15'], [9, ' Draft notes ']],
    success: { create: 'Trial class booked.', update: 'Trial status updated to No show.' } },
  curriculum: { route: 'curriculum', base: '/api/academies/owned/course-modules', notice: 6, saving: 7, row: 2,
    drafts: [[3, 'course'], [4, ' Draft module '], [5, '3']],
    success: { create: 'Module saved as draft.', update: 'Module published.' } },
};
async function page(domain, config = {}) {
  const d = definitions[domain], trial = domain === 'trials';
  const source = process.env.QA_UI_BATCH_BASELINE === '1'
    ? require('node:child_process').execFileSync('git', ['show', '01e5531783bb34c44da282e85b6d8c4d9bf4a75e:apps/web/src/app/' + d.route + '/page.tsx'], { encoding: 'utf8' })
    : fs.readFileSync('apps/web/src/app/' + d.route + '/page.tsx', 'utf8');
  const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  let index = 0, ri = 0, ei = 0;
  const state = new Map(), refs = [], deps = [], effects = [], calls = [], module = { exports: {} };
  const row = trial ? { id: 'trial', leadId: 'lead', teacherId: 'teacher', scheduledAtUtc: '2026-10-20T05:45:00Z', status: 'Booked', notes: 'Original note' }
    : { id: 'module', courseId: 'course', title: 'Synthetic module', sequence: 1, isPublished: !!config.published };
  const data = trial ? {
    '/api/academies/owned/leads': [{ id: 'lead', fullName: 'Synthetic lead' }],
    '/api/academies/owned/teachers': [{ id: 'teacher', firstName: 'Synthetic', lastName: 'Teacher' }],
    [d.base]: [row],
  } : { '/api/academies/owned/courses': [{ id: 'course', name: 'Synthetic course' }], [d.base]: [row] };
  const react = {
    useState: v => { const k = index++; if (!state.has(k)) state.set(k, v); return [state.get(k), v => state.set(k, typeof v === 'function' ? v(state.get(k)) : v)]; },
    useRef: v => { const k = ri++; return refs[k] ?? (refs[k] = { current: v }); },
    useEffect: (fn, next) => { const k = ei++; if (!deps[k] || next.some((v, i) => v !== deps[k][i])) { deps[k] = next; effects.push(fn); } },
  };
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) { if (config.writeGate) await config.writeGate; if (config.network) throw Error('Synthetic network'); return { ok: !config.status, status: config.status || 200 }; }
    if (url === '/api/academies') return { ok: !config.academyStatus, status: config.academyStatus || 200, json: async () => config.noAcademy ? [] : [{ id: 'owned' }] };
    assert.ok(Object.hasOwn(data, url), url);
    if (calls.some(c => c.method)) { if (config.readGate) await config.readGate; if (config.refreshFailure) return { ok: false, status: 503 }; }
    return { ok: !config.initialFailure, json: async () => config.malformed ? {} : data[url] };
  };
  new Function('require', 'module', 'exports', code)(n => n === 'react/jsx-runtime' ? jsx : n === 'react' ? react
    : n === '@/lib/api' ? { academyApi: api, apiHeaders: () => ({}) }
    : n === '@/components/workspace-nav' ? { WorkspaceNav: 'nav' }
    : n === '@/components/design-system/controls' ? { StandardSelectField: 'select', StandardDateField: 'date', StandardTimeField: 'time' }
    : (() => { throw Error(n); })(), module, module.exports);
  const render = () => { index = ri = ei = 0; return module.exports.default(); };
  async function settle() { for (let i = 0; i < 5; i++) { for (const fn of effects.splice(0)) fn(); await tick(); render(); } }
  render(); await settle(); for (const [k, v] of d.drafts) state.set(k, v);
  if (config.blank) { if (trial) { state.set(6, ''); state.set(9, ''); } else state.set(5, ''); }
  const handler = kind => {
    const tree = nodes(render());
    if (kind === 'create') { const h = tree.find(n => n.type === 'form').props.onSubmit; return () => h({ preventDefault() {} }); }
    if (trial) { const h = tree.find(n => n.props?.name === 'trial-status-trial').props.onChange; return async () => { h('NoShow'); await settle(); }; }
    const h = tree.find(n => n.type === 'button' && n.props.type === 'button').props.onClick;
    return async () => { h(); await settle(); };
  };
  return { d, state, calls, row, render, settle, handler, writes: () => calls.filter(c => c.method), notice: () => state.get(d.notice) };
}
function assertDraft(p, reset) {
  for (const [k, v] of p.d.drafts) {
    const expected = !reset ? v : p.d.route === 'trial-bookings' ? (k === 8 ? '10:00' : '') : k === 4 ? '' : k === 5 ? '4' : v;
    assert.equal(p.state.get(k), expected);
  }
  assert.equal(p.state.get(p.d.saving), false);
}
for (const domain of Object.keys(definitions)) for (const kind of ['create', 'update']) {
  test(domain + ' ' + kind + ' durable polite success and exact payload/reset', async () => {
    const p = await page(domain); await p.handler(kind)(); await p.settle(); assert.equal(p.notice(), p.d.success[kind]);
    const write = p.writes()[0], trial = domain === 'trials';
    assert.equal(write.method, kind === 'create' ? 'POST' : 'PATCH');
    assert.equal(write.url, p.d.base + (kind === 'create' ? '' : trial ? '/trial/status' : '/module/publication'));
    assert.deepEqual(JSON.parse(write.body), trial ? kind === 'create'
      ? { leadId: 'lead', teacherId: 'teacher', scheduledAtUtc: new Date('2026-10-20T11:15:00').toISOString(), notes: ' Draft notes ' } : { status: 'NoShow' }
      : kind === 'create' ? { courseId: 'course', title: ' Draft module ', description: null, sequence: 3 } : { isPublished: true });
    assertDraft(p, kind === 'create'); assert.equal(nodes(p.render()).find(n => n.props?.role === 'status').props['aria-live'], 'polite');
  });
  test(domain + ' ' + kind + ' confirmed write / failed refresh remains success', async () => {
    const p = await page(domain, { refreshFailure: true }); await p.handler(kind)(); assert.ok(p.notice().startsWith(p.d.success[kind]));
    assert.match(p.notice(), /could not be refreshed; do not repeat/); assertDraft(p, kind === 'create'); assert.equal(p.writes().length, 1);
  });
  for (const status of [400, 403, 500, 503]) test(domain + ' ' + kind + ' HTTP' + status + ' draft retained / no retry', async () => {
    const p = await page(domain, { status }); await p.handler(kind)(); assert.match(p.notice(), status >= 500 ? /could not be confirmed.*before retrying/ : /could not/);
    assertDraft(p, false); assert.equal(p.writes().length, 1); assert.equal(p.calls.filter(c => !c.method).length, domain === 'trials' ? 4 : 3);
  });
  test(domain + ' ' + kind + ' network uncertainty caught / no retry', async () => {
    const p = await page(domain, { network: true }); await p.handler(kind)(); assert.match(p.notice(), /could not be confirmed.*retained.*before retrying/); assertDraft(p, false); assert.equal(p.writes().length, 1);
  });
  for (const stage of ['write', 'readback']) test(domain + ' ' + kind + ' ' + stage + ' duplicate/opposing stale handlers blocked', async () => {
    let release; const gate = new Promise(r => release = r), p = await page(domain, stage === 'write' ? { writeGate: gate } : { readGate: gate });
    p.state.set(p.d.notice, 'Old error'); const h = p.handler(kind), other = p.handler(kind === 'create' ? 'update' : 'create'), first = h(); await tick();
    const all = Promise.allSettled([first, h(), other()]);
    try { assert.equal(p.writes().length, 1); const tree = nodes(p.render()); assert.ok(tree.find(n => n.type === 'fieldset')?.props.disabled);
      assert.ok(tree.filter(n => n.type === 'button' || n.type === 'select').every(n => n.props.disabled)); assert.notEqual(p.notice(), 'Old error'); }
    finally { release(); }
    for (const result of await all) if (result.status === 'rejected') throw result.reason;
    await p.settle(); assert.equal(p.writes().length, 1); assert.equal(p.state.get(p.d.saving), false);
  });
}
test('Trial optional teacher and notes remain null', async () => { const p = await page('trials', { blank: true }); await p.handler('create')(); const b = JSON.parse(p.writes()[0].body); assert.equal(b.teacherId, null); assert.equal(b.notes, null); });
test('Trial required lead/date validation retains draft without writing', async () => { for (const key of [5, 7]) { const p = await page('trials'); p.state.set(key, ''); await p.handler('create')(); assert.match(p.notice(), /Select a lead and date/); assert.equal(p.writes().length, 0); } });
test('Curriculum original blank sequence Number conversion preserved, separate domain gate', async () => { const p = await page('curriculum', { blank: true }); await p.handler('create')(); assert.equal(JSON.parse(p.writes()[0].body).sequence, 0); assert.equal(p.state.get(5), '1'); });
test('Curriculum published module can return to draft without resetting unrelated draft', async () => { const p = await page('curriculum', { published: true }); await p.handler('update')(); assert.equal(p.notice(), 'Module returned to draft.'); assert.deepEqual(JSON.parse(p.writes()[0].body), { isPublished: false }); assertDraft(p, false); });
for (const domain of Object.keys(definitions)) {
  for (const config of [{ noAcademy: true }, { academyStatus: 401 }, { academyStatus: 403 }, { initialFailure: true }, { malformed: true }]) test(domain + ' unavailable initial workspace ' + JSON.stringify(config), async () => {
    const p = await page(domain, config); assert.match(p.notice(), /could not be loaded/); await p.handler('create')(); assert.equal(p.writes().length, 0);
    assert.ok(nodes(p.render()).find(n => n.type === 'fieldset')?.props.disabled); assert.deepEqual(p.state.get(p.d.row), []);
  });
  test(domain + ' fresh initial/readback lists without render refetch', async () => {
    const p = await page(domain), initial = domain === 'trials' ? 4 : 3; await p.settle(); assert.equal(p.calls.length, initial);
    await p.handler('create')(); await p.settle(); assert.equal(p.calls.length, initial * 2); assert.ok(p.calls.filter(c => !c.method).every(c => c.cache === 'no-store'));
  });
}
