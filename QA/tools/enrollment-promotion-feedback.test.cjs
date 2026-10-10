// Executes actual page handlers with synthetic React hooks and API transport; not SQL/RBAC acceptance.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const tick = () => new Promise(r => setImmediate(r));
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
const defs = {
  enrollments: { route: 'enrollments', notice: 8, saving: 10, drafts: [[4, 'student'], [5, 'source'], [6, '2026-10-20'], [7, 'Waitlisted']], actions: ['create', 'status'] },
  promotions: { route: 'batch-promotions', notice: 5, saving: 6, drafts: [[7, 'student'], [8, 'source'], [9, 'target'], [10, '2026-10-20']], actions: ['create', 'approve', 'reject'] },
};
async function page(domain, config = {}) {
  const d = defs[domain], promo = domain === 'promotions';
  const source = process.env.QA_UI_BATCH_BASELINE === '1'
    ? require('node:child_process').execFileSync('git', ['show', 'c0d348a1592a2946b398639e8fcb17c2e2d5d359:apps/web/src/app/' + d.route + '/page.tsx'], { encoding: 'utf8' })
    : fs.readFileSync('apps/web/src/app/' + d.route + '/page.tsx', 'utf8');
  const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  let i = 0, r = 0, e = 0;
  const state = new Map(), refs = [], deps = [], effects = [], calls = [], module = { exports: {} };
  const data = {
    '/api/academies/owned/students': [{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }],
    '/api/academies/owned/batches': [{ id: 'source', name: 'Source', capacity: 10 }, { id: 'target', name: 'Target', capacity: 10 }],
    '/api/academies/owned/enrollments': [{ id: 'enrollment', studentId: 'student', batchId: 'source', startDate: '2026-10-01', endDate: null, status: 'Active' }],
    '/api/academies/owned/batch-promotions': [{ id: 'promotion', studentId: 'student', sourceBatchId: 'source', targetBatchId: 'target', effectiveDate: '2026-10-20', status: 'Pending' }],
  };
  const forms = [{ resets: 0, fields: { notes: config.blank ? '' : ' Draft note ' }, reset() { this.resets++; } }];
  let prompts = 0;
  const react = {
    useMemo: fn => fn(),
    useState: v => { const k = i++; if (!state.has(k)) state.set(k, v); return [state.get(k), v => state.set(k, typeof v === 'function' ? v(state.get(k)) : v)]; },
    useRef: v => { const k = r++; return refs[k] ?? (refs[k] = { current: v }); },
    useEffect: (fn, next) => { const k = e++; if (!deps[k] || next.some((v, i) => v !== deps[k][i])) { deps[k] = next; effects.push(fn); } },
  };
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) {
      if (config.writeGate) await config.writeGate;
      if (config.network) throw Error('Synthetic network');
      return { ok: !config.status, status: config.status || 201, json: async () => ({ message: config.message || 'Synthetic policy refusal.' }) };
    }
    if (url === '/api/academies') return { ok: !config.academyStatus, json: async () => config.noAcademy ? [] : [{ id: 'owned' }] };
    assert.ok(Object.hasOwn(data, url), url);
    if (calls.some(c => c.method)) { if (config.readGate) await config.readGate; if (config.refreshFailure) return { ok: false, status: 503 }; }
    return { ok: !config.initialFailure, json: async () => config.malformed ? {} : data[url] };
  };
  new Function('require', 'module', 'exports', 'FormData', 'window', code)(n => n === 'react/jsx-runtime' ? jsx : n === 'react' ? react
    : n === 'next/link' ? { __esModule: true, default: 'a' }
    : n === '@/lib/api' ? { academyApi: api, apiHeaders: () => ({}) }
    : n === '@/components/workspace-nav' ? { WorkspaceNav: 'nav' }
    : n === '@/components/standard-date-field' ? { StandardDateField: 'date' }
    : n === '@/components/design-system/controls' ? { StandardSelectField: 'select', StandardDateField: 'date' }
    : (() => { throw Error(n); })(), module, module.exports, class { constructor(form) { this.fields = { ...form.fields }; } get(k) { return this.fields[k] ?? null; } }, { location: { search: '?studentId=student' }, prompt() { prompts++; return Object.hasOwn(config, 'prompt') ? config.prompt : ' Decision note '; } });
  const render = () => { i = r = e = 0; return module.exports.default(); };
  async function settle() { for (let i = 0; i < 5; i++) { for (const fn of effects.splice(0)) fn(); await tick(); render(); } }
  render(); await settle(); for (const [k, v] of d.drafts) state.set(k, v);
  function handler(action) {
    const tree = nodes(render());
    if (action === 'create') {
      const h = tree.find(n => n.type === 'form').props.onSubmit;
      return () => { let released = false; const event = { preventDefault() {}, get currentTarget() { if (released) throw Error('Released currentTarget'); return forms[0]; } }; const result = h(event); released = true; return result; };
    }
    if (!promo) return () => tree.find(n => n.type === 'select' && n.props.value === 'Active').props.onChange({ target: { value: 'Paused' } });
    return () => tree.find(n => n.type === 'button' && n.props.children === (action === 'approve' ? 'Approve' : 'Reject')).props.onClick();
  }
  return { d, state, calls, forms, render, settle, handler, prompts: () => prompts, writes: () => calls.filter(c => c.method), notice: () => state.get(d.notice) };
}
function success(domain, action) { return domain === 'enrollments' ? action === 'create' ? 'Enrolment created.' : 'Enrolment updated.' : action === 'create' ? 'Promotion request recorded.' : action === 'approve' ? 'Promotion approved.' : 'Promotion rejected.'; }

function resets(p, action, accepted) {
  assert.deepEqual(p.forms.map(f => f.resets), [0]);
  for (const [k, v] of p.d.drafts) {
    const clear = accepted && action === 'create' && (p.d.route === 'enrollments' ? k === 6 : k >= 9);
    assert.equal(p.state.get(k), clear ? '' : v);
  }
  assert.equal(p.state.get(p.d.saving), false);
}
for (const [domain, d] of Object.entries(defs)) for (const action of d.actions) {
  test(`${domain} ${action}: durable success, captured form, exact request, matching reset only`, async () => {
    const p = await page(domain); p.handler(action)(); await p.settle(); assert.equal(p.notice(), success(domain, action)); resets(p, action, true);
    assert.equal(p.writes().length, 1); const w = p.writes()[0];
    const expected = domain === 'enrollments' ? action === 'create'
      ? ['/enrollments', 'POST', { studentId: 'student', batchId: 'source', startDate: '2026-10-20', status: 'Waitlisted' }]
      : ['/enrollments/enrollment', 'PUT', { status: 'Paused', endDate: null }]
      : action === 'create' ? ['/batch-promotions', 'POST', { studentId: 'student', sourceBatchId: 'source', targetBatchId: 'target', effectiveDate: '2026-10-20', notes: ' Draft note ' }]
      : ['/batch-promotions/promotion/decision', 'PATCH', { status: action === 'approve' ? 'Approved' : 'Rejected', notes: ' Decision note ' }];
    assert.equal(w.url, '/api/academies/owned' + expected[0]); assert.equal(w.method, expected[1]); assert.deepEqual(w.body ? JSON.parse(w.body) : undefined, expected[2]);
    assert.equal(nodes(p.render()).find(n => n.props?.role === 'status').props['aria-live'], 'polite');
    assert.equal(p.calls.filter(c => c.url === '/api/academies').length, 1); assert.ok(p.calls.filter(c => !c.method).every(c => c.cache === 'no-store'));
  });
  test(`${domain} ${action}: accepted write with refresh failure is not a failed save`, async () => {
    const p = await page(domain, { refreshFailure: true }); p.handler(action)(); await p.settle(); assert.ok(p.notice().startsWith(success(domain, action))); assert.match(p.notice(), /could not be refreshed.*Do not repeat/); resets(p, action, true); assert.equal(p.writes().length, 1);
  });
  for (const status of [400, 403, 500, 503]) test(`${domain} ${action}: HTTP ${status}, draft retained without retry`, async () => {
    const p = await page(domain, { status }); p.handler(action)(); await p.settle(); assert.match(p.notice(), status >= 500 ? /could not be confirmed.*retained.*before trying again/ : /Synthetic policy refusal/); resets(p, action, false); assert.equal(p.writes().length, 1);
  });
  test(`${domain} ${action}: network uncertainty caught`, async () => { const p = await page(domain, { network: true }); p.handler(action)(); await p.settle(); assert.match(p.notice(), /could not be confirmed/); resets(p, action, false); assert.equal(p.writes().length, 1); });
  for (const stage of ['write', 'readback']) test(`${domain} ${action}: ${stage} duplicate/opposing handlers blocked`, async () => {
    let release; const gate = new Promise(r => release = r), p = await page(domain, stage === 'write' ? { writeGate: gate } : { readGate: gate });
    const h = p.handler(action), other = p.handler(d.actions.find(a => a !== action)); h(); await tick();
    try { h(); other(); assert.equal(p.writes().length, 1); assert.equal(p.state.get(d.saving), true); const tree = nodes(p.render()), fieldsets = tree.filter(n => n.type === 'fieldset'), inherited = fieldsets.flatMap(nodes); assert.ok(fieldsets.length && fieldsets.every(n => n.props.disabled)); assert.ok(tree.filter(n => (n.type === 'select' || n.type === 'button' && n.props.type === 'button') && !inherited.includes(n)).every(n => n.props.disabled)); }
    finally { release(); } await p.settle(); assert.equal(p.writes().length, 1); assert.equal(p.state.get(d.saving), false);
  });
}
for (const domain of Object.keys(defs)) for (const config of [{ noAcademy: true }, { academyStatus: 403 }, { initialFailure: true }, { malformed: true }]) test(`${domain}: unavailable workspace ${JSON.stringify(config)}`, async () => {
  const p = await page(domain, config); assert.match(p.notice(), /could not be loaded/); p.handler('create')(); await p.settle(); assert.equal(p.writes().length, 0); assert.equal(p.state.get(0), undefined); assert.ok(nodes(p.render()).filter(n => n.type === 'fieldset').every(n => n.props.disabled));
});

test('Promotion prompt cancellation is not an approval or rejection', async () => { for (const action of ['approve', 'reject']) { const p = await page('promotions', { prompt: null }); p.state.set(5, 'Prior notice'); p.handler(action)(); await p.settle(); assert.equal(p.writes().length, 0); assert.equal(p.notice(), 'Prior notice'); } });
test('Promotion rejection still requires a nonblank reason', async () => { const p = await page('promotions', { prompt: '  ' }); p.handler('reject')(); await p.settle(); assert.equal(p.writes().length, 0); assert.match(p.notice(), /reason is required/); });
test('Promotion approval note remains optional and creation notes remain null when blank', async () => { const p = await page('promotions', { prompt: '', blank: true }); p.handler('approve')(); await p.settle(); assert.equal(JSON.parse(p.writes()[0].body).notes, ''); const q = await page('promotions', { blank: true }); q.handler('create')(); await q.settle(); assert.equal(JSON.parse(q.writes()[0].body).notes, null); });
test('Enrollment duplicate409 remains understandable and draft is retained', async () => { const p = await page('enrollments', { status: 409 }); await p.handler('create')(); await p.settle(); assert.match(p.notice(), /already actively enrolled/); resets(p, 'create', false); });
test('Enrollment optional start date remains null', async () => { const p = await page('enrollments'); p.state.set(6, ''); await p.handler('create')(); await p.settle(); assert.equal(JSON.parse(p.writes()[0].body).startDate, null); });
test('Existing real API lifecycle reason validation is displayed, not bypassed or marked success', async () => { const reason = 'A lifecycle reason is required when changing an enrolment from Active.'; const p = await page('enrollments', { status: 400, message: reason }); p.handler('status')(); await p.settle(); assert.equal(p.notice(), reason); assert.deepEqual(JSON.parse(p.writes()[0].body), { status: 'Paused', endDate: null }); resets(p, 'status', false); });
