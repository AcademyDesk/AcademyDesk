// Executes actual page handlers with synthetic React hooks and API transport; not SQL/RBAC acceptance.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const tick = () => new Promise(r => setImmediate(r));
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
const defs = {
  governance: { route: 'academic-governance', notice: 4, saving: 5, drafts: [[6, 'course'], [7, 'required']], actions: ['scheme', 'prerequisite', 'status'] },
  periods: { route: 'academic-periods', notice: 3, saving: 9, drafts: [[4, '2026-09-01'], [5, '2027-08-31'], [6, 'year'], [7, '2026-10-01'], [8, '2026-12-31']], actions: ['year', 'term', 'closeYear', 'closeTerm'] },
};
async function page(domain, config = {}) {
  const d = defs[domain], gov = domain === 'governance';
  const source = process.env.QA_UI_BATCH_BASELINE === '1'
    ? require('node:child_process').execFileSync('git', ['show', '5cf2a98ed92243dc3bde89badf7cb5739f624be0:apps/web/src/app/' + d.route + '/page.tsx'], { encoding: 'utf8' })
    : fs.readFileSync('apps/web/src/app/' + d.route + '/page.tsx', 'utf8');
  const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  let i = 0, r = 0, e = 0;
  const state = new Map(), refs = [], deps = [], effects = [], calls = [], module = { exports: {} };
  const scheme = { id: 'scheme', name: 'Synthetic scheme', passingPercent: 0, bandsJson: '[]', isActive: true };
  const year = { id: 'year', name: 'Synthetic year', startDate: '2026-09-01', endDate: '2027-08-31', isCurrent: true, isClosed: false };
  const term = { id: 'term', academicYearId: 'year', name: 'Synthetic term', startDate: '2026-10-01', endDate: '2026-12-31', isClosed: false };
  const data = gov ? {
    '/api/academies/owned/courses': [{ id: 'course', name: 'Course' }, { id: 'required', name: 'Required' }],
    '/api/academies/owned/academic-governance/grading-schemes': [scheme],
    '/api/academies/owned/academic-governance/prerequisites': [],
  } : { '/api/academies/owned/academic-periods': { years: [year], terms: [term] } };
  const forms = [0, 1].map(() => ({ resets: 0, fields: { name: ' Draft name ', passingPercent: '0', bandsJson: '', isCurrent: 'on' }, reset() { this.resets++; } }));
  const react = {
    useState: v => { const k = i++; if (!state.has(k)) state.set(k, v); return [state.get(k), v => state.set(k, typeof v === 'function' ? v(state.get(k)) : v)]; },
    useRef: v => { const k = r++; return refs[k] ?? (refs[k] = { current: v }); },
    useEffect: (fn, next) => { const k = e++; if (!deps[k] || next.some((v, i) => v !== deps[k][i])) { deps[k] = next; effects.push(fn); } },
  };
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) {
      if (config.writeGate) await config.writeGate;
      if (config.network) throw Error('Synthetic network');
      return { ok: !config.status, status: config.status || 201, json: async () => ({ message: 'Synthetic policy refusal.' }) };
    }
    if (url === '/api/academies') return { ok: !config.academyStatus, json: async () => config.noAcademy ? [] : [{ id: 'owned' }] };
    assert.ok(Object.hasOwn(data, url), url);
    if (calls.some(c => c.method)) { if (config.readGate) await config.readGate; if (config.refreshFailure) return { ok: false, status: 503 }; }
    return { ok: !config.initialFailure, json: async () => config.malformed ? {} : data[url] };
  };
  new Function('require', 'module', 'exports', 'FormData', code)(n => n === 'react/jsx-runtime' ? jsx : n === 'react' ? react
    : n === 'next/link' ? { __esModule: true, default: 'a' }
    : n === '@/lib/api' ? { academyApi: api, apiHeaders: () => ({}) }
    : n === '@/components/workspace-nav' ? { WorkspaceNav: 'nav' }
    : n === '@/components/design-system/controls' ? { StandardSelectField: 'select', StandardDateField: 'date' }
    : (() => { throw Error(n); })(), module, module.exports, class { constructor(form) { this.fields = { ...form.fields }; } get(k) { return this.fields[k] ?? null; } });
  const render = () => { i = r = e = 0; return module.exports.default(); };
  async function settle() { for (let i = 0; i < 5; i++) { for (const fn of effects.splice(0)) fn(); await tick(); render(); } }
  render(); await settle(); for (const [k, v] of d.drafts) state.set(k, v);
  function handler(action) {
    const tree = nodes(render()), formIndex = action === 'scheme' || action === 'year' ? 0 : 1;
    if (['scheme', 'prerequisite', 'year', 'term'].includes(action)) {
      const h = tree.filter(n => n.type === 'form')[formIndex].props.onSubmit;
      return () => { let released = false; const event = { preventDefault() {}, get currentTarget() { if (released) throw Error('Released currentTarget'); return forms[formIndex]; } }; h(event); released = true; };
    }
    const label = action === 'status' ? 'Deactivate' : action === 'closeYear' ? 'Close year' : 'Close term';
    const h = tree.find(n => n.type === 'button' && n.props.children === label).props.onClick;
    return () => h();
  }
  return { d, state, calls, forms, render, settle, handler, writes: () => calls.filter(c => c.method), notice: () => state.get(d.notice) };
}
const successes = { scheme: 'Grading scheme created.', prerequisite: 'Course prerequisite saved.', status: 'Grading scheme made inactive.', year: 'Academic year created.', term: 'Term created.', closeYear: 'Period closed and retained for audit.', closeTerm: 'Period closed and retained for audit.' };
function resets(p, action, accepted) {
  const which = ['scheme', 'year'].includes(action) ? 0 : ['prerequisite', 'term'].includes(action) ? 1 : -1;
  assert.deepEqual(p.forms.map(f => f.resets), [0, 1].map(i => accepted && i === which ? 1 : 0));
  for (const [k, v] of p.d.drafts) {
    const clear = accepted && (action === 'prerequisite' || action === 'year' && k <= 5 || action === 'term' && k >= 6);
    assert.equal(p.state.get(k), clear ? '' : v);
  }
  assert.equal(p.state.get(p.d.saving), false);
}
for (const [domain, d] of Object.entries(defs)) for (const action of d.actions) {
  test(`${domain} ${action}: durable success, captured form, exact request, matching reset only`, async () => {
    const p = await page(domain); p.handler(action)(); await p.settle(); assert.equal(p.notice(), successes[action]); resets(p, action, true);
    assert.equal(p.writes().length, 1); const w = p.writes()[0];
    const expected = {
      scheme: ['/academic-governance/grading-schemes', 'POST', { name: ' Draft name ', passingPercent: 0, bandsJson: '[]' }],
      prerequisite: ['/academic-governance/prerequisites', 'POST', { courseId: 'course', requiredCourseId: 'required' }],
      status: ['/grading-schemes/scheme/status', 'PATCH', { isActive: false }],
      year: ['/academic-periods/years', 'POST', { name: ' Draft name ', startDate: '2026-09-01', endDate: '2027-08-31', isCurrent: true }],
      term: ['/academic-periods/terms', 'POST', { academicYearId: 'year', name: ' Draft name ', startDate: '2026-10-01', endDate: '2026-12-31' }],
      closeYear: ['/academic-periods/years/year/close', 'PATCH', undefined], closeTerm: ['/academic-periods/terms/term/close', 'PATCH', undefined],
    }[action];
    assert.equal(w.url, '/api/academies/owned' + expected[0]); assert.equal(w.method, expected[1]); assert.deepEqual(w.body ? JSON.parse(w.body) : undefined, expected[2]);
    assert.equal(nodes(p.render()).find(n => n.props?.role === 'status').props['aria-live'], 'polite');
    assert.equal(p.calls.filter(c => c.url === '/api/academies').length, 1); assert.ok(p.calls.filter(c => !c.method).every(c => c.cache === 'no-store'));
  });
  test(`${domain} ${action}: accepted write with refresh failure is not a failed save`, async () => {
    const p = await page(domain, { refreshFailure: true }); p.handler(action)(); await p.settle(); assert.ok(p.notice().startsWith(successes[action])); assert.match(p.notice(), /could not be refreshed.*Do not repeat/); resets(p, action, true); assert.equal(p.writes().length, 1);
  });
  for (const status of [400, 403, 500, 503]) test(`${domain} ${action}: HTTP ${status}, draft retained without retry`, async () => {
    const p = await page(domain, { status }); p.handler(action)(); await p.settle(); assert.match(p.notice(), status >= 500 ? /could not be confirmed.*retained.*before trying again/ : /Synthetic policy refusal/); resets(p, action, false); assert.equal(p.writes().length, 1);
  });
  test(`${domain} ${action}: network uncertainty caught`, async () => { const p = await page(domain, { network: true }); p.handler(action)(); await p.settle(); assert.match(p.notice(), /could not be confirmed/); resets(p, action, false); assert.equal(p.writes().length, 1); });
  for (const stage of ['write', 'readback']) test(`${domain} ${action}: ${stage} duplicate/opposing handlers blocked`, async () => {
    let release; const gate = new Promise(r => release = r), p = await page(domain, stage === 'write' ? { writeGate: gate } : { readGate: gate });
    const h = p.handler(action), other = p.handler(d.actions.find(a => a !== action)); h(); await tick();
    try { h(); other(); assert.equal(p.writes().length, 1); assert.equal(p.state.get(d.saving), true); assert.ok(nodes(p.render()).filter(n => n.type === 'fieldset').every(n => n.props.disabled)); assert.ok(nodes(p.render()).filter(n => n.type === 'select' || n.type === 'button' && n.props.type === 'button').every(n => n.props.disabled)); }
    finally { release(); } await p.settle(); assert.equal(p.writes().length, 1); assert.equal(p.state.get(d.saving), false);
  });
}
for (const domain of Object.keys(defs)) for (const config of [{ noAcademy: true }, { academyStatus: 403 }, { initialFailure: true }, { malformed: true }]) test(`${domain}: unavailable workspace ${JSON.stringify(config)}`, async () => {
  const p = await page(domain, config); assert.match(p.notice(), /could not be loaded/); for (const action of p.d.actions.slice(0, 2)) p.handler(action)(); await p.settle(); assert.equal(p.writes().length, 0); assert.equal(p.state.get(0), undefined); assert.ok(nodes(p.render()).filter(n => n.type === 'fieldset').every(n => n.props.disabled));
});
test('Required controlled prerequisite/year/term fields stop writes without draft reset', async () => {
  for (const [domain, action, fields] of [['governance', 'prerequisite', [6, 7]], ['periods', 'year', [4, 5]], ['periods', 'term', [6, 7, 8]]]) for (const key of fields) {
    const p = await page(domain); p.state.set(key, ''); p.handler(action)(); await p.settle(); assert.equal(p.writes().length, 0); assert.deepEqual(p.forms.map(f => f.resets), [0, 0]); assert.match(p.notice(), /Select/);
  }
});
