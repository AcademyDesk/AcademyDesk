// Execute the actual compliance TSX with controlled hooks/HTTP/FormData.
// Shared control internals, DOM layout and device interactions are NOT simulated proof.
const fs = require('node:fs'), assert = require('node:assert/strict'), { test } = require('node:test');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const code = ts.transpileModule(fs.readFileSync('apps/web/src/app/compliance/page.tsx', 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, target: ts.ScriptTarget.ES2022 } }).outputText;
const students = [{ id: 's1', firstName: 'Student', lastName: 'One' }, { id: 'shared', firstName: 'Student', lastName: 'Collision' }];
const guardians = [{ id: 'g1', firstName: 'Parent', lastName: 'One' }, { id: 'shared', firstName: 'Parent', lastName: 'Collision' }];
const nodes = x => !x || typeof x !== 'object' ? [] : Array.isArray(x) ? x.flatMap(nodes) : [x, ...nodes(x.props?.children)];
const text = x => x == null ? '' : Array.isArray(x) ? x.map(text).join(' ') : typeof x === 'object' ? text(x.props?.children) : String(x);
async function page(config = {}) {
  const contexts = new Map(), effects = [], calls = [], mod = { exports: {} }; let active, tree, saved = false, release;
  const react = {
    useState(initial) { const context = active, k = context.i++; if (!(k in context.state)) context.state[k] = typeof initial === 'function' ? initial() : initial; return [context.state[k], value => context.state[k] = typeof value === 'function' ? value(context.state[k]) : value]; },
    useRef(initial) { const k = active.r++; return active.refs[k] ??= { current: initial }; },
    useMemo(fn) { return fn(); },
    useCallback(fn, deps) { const k = active.c++; if (!active.callbackDeps[k] || deps.some((v, j) => v !== active.callbackDeps[k][j])) { active.callbackDeps[k] = deps; active.callbacks[k] = fn; } return active.callbacks[k]; },
    useEffect(fn, deps) { const k = active.e++; if (!active.deps[k] || deps.some((v, j) => v !== active.deps[k][j])) { active.deps[k] = deps; effects.push(fn); } },
  };
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) { if (config.defer) await new Promise(resolve => release = resolve); if (config.network) throw Error('offline'); if (config.saveDenied) return { ok: false, status: config.saveDenied }; saved = true; return { ok: true }; }
    if ((saved && config.refreshDenied) || config.loadDenied) return { ok: false, json: async () => [] };
    if (saved && config.refreshNetwork) throw Error('offline');
    if (saved && config.refreshJson) return { ok: true, json: async () => { throw Error('invalid json'); } };
    const data = url === '/api/academies' ? [{ id: 'owned' }] : url.endsWith('/students') ? students : url.endsWith('/guardians') ? guardians : url.endsWith('/documents') ? config.documents ?? [] : config.consents ?? [];
    return { ok: true, json: async () => config.loadMalformed ? {} : data };
  };
  class FormData { constructor(form) { this.values = { ...form.values }; } get(name) { return this.values[name] ?? null; } }
  new Function('require', 'module', 'exports', 'FormData', code)(name => name === 'react' ? react : name === 'react/jsx-runtime' ? jsx : name === '@/lib/api' ? { academyApi: api, apiHeaders: () => ({}) } : name === '@/components/enterprise-page-state' ? { EnterprisePageState: 'notice' } : name === '@/components/design-system/controls' ? { StandardSelectField: 'select', StandardDateField: 'date' } : (() => { throw Error(name); })(), mod, mod.exports, FormData);
  function component(fn, props, path) { const previous = active; active = contexts.get(path) ?? { state: [], refs: [], deps: [], callbacks: [], callbackDeps: [] }; contexts.set(path, active); active.i = active.r = active.e = active.c = 0; const result = fn(props); active = previous; return expand(result, path); }
  function expand(x, path) { if (!x || typeof x !== 'object') return x; if (Array.isArray(x)) return x.map((child, i) => expand(child, path + '/' + i)); if (typeof x.type === 'function') return component(x.type, x.props, path + '/' + x.type.name + ':' + (x.key ?? '')); return { ...x, props: { ...x.props, children: expand(x.props?.children, path + '/children') } }; }
  const render = () => tree = component(mod.exports.default, {}, 'root');
  const flush = async () => { for (let i = 0; i < 6; i++) { effects.splice(0).forEach(fn => fn()); await new Promise(resolve => setImmediate(resolve)); render(); } };
  render(); await flush();
  const forms = () => nodes(render()).filter(x => x.type === 'form');
  const control = (index, name) => nodes(forms()[index]).find(x => x.props?.name === name);
  const change = (index, name, value) => { control(index, name).props.onChange(value); render(); };
  function submit(index, overrides = {}) {
    const formNode = forms()[index], values = { personType: control(index, 'personType').props.value, personId: control(index, 'personId').props.value, documentType: 'ID', fileName: 'synthetic.pdf', secureReference: '', consentType: 'Media', evidenceReference: '', ...(index === 0 ? { expiryDate: control(0, 'expiryDate').props.value, visibility: control(0, 'visibility').props.value } : {}), ...overrides };
    const form = { values, resets: 0, reset() { this.resets++; } }; let target = form;
    const event = { preventDefault() {}, get currentTarget() { return target; } };
    const promise = formNode.props.onSubmit(event); target = null; return { promise, form };
  }
  return { render, flush, calls, control, change, submit, forms, release: () => release?.(), message: () => text(nodes(render()).find(x => x.type === 'notice')) };
}
test('typed options in both forms exclude the opposite person table; changing type clears only its own choice', async () => {
  const h = await page();
  for (const i of [0, 1]) assert.deepEqual(h.control(i, 'personId').props.options.map(x => x.value), ['s1', 'shared']);
  h.change(0, 'personId', 's1'); h.change(1, 'personId', 'shared'); h.change(0, 'personType', 'guardian');
  assert.deepEqual(h.control(0, 'personId').props.options.map(x => x.value), ['g1', 'shared']);
  assert.equal(h.control(0, 'personId').props.value, ''); assert.equal(h.control(1, 'personId').props.value, 'shared');
});
for (const index of [0, 1]) for (const [type, id] of [['student', ''], ['student', 'g1'], ['guardian', 's1'], ['guardian', 'missing'], ['', 's1'], ['unknown', 's1']]) test('invalid typed selection sends no write ' + index + '/' + type + '/' + id, async () => {
  const h = await page(); h.change(index, 'personType', type); h.change(index, 'personId', id);
  const s = h.submit(index); await s.promise; await h.flush();
  assert.equal(h.calls.filter(x => x.method).length, 0); assert.equal(s.form.resets, 0); assert.equal(h.control(index, 'personId').props.value, id); assert.match(h.message(), /Choose a valid Student or Parent/);
});
for (const index of [0, 1]) for (const config of [{ saveDenied: 400 }, { saveDenied: 403 }, { saveDenied: 500 }, { network: true }]) test('failed save retains both drafts and is not success ' + index + '/' + JSON.stringify(config), async () => {
  const h = await page(config); h.change(0, 'personId', 's1'); h.change(0, 'expiryDate', '2028-02-01'); h.change(0, 'visibility', 'StaffRestricted'); h.change(1, 'personType', 'guardian'); h.change(1, 'personId', 'g1');
  const s = h.submit(index); await s.promise; await h.flush();
  assert.equal(s.form.resets, 0); assert.equal(h.control(0, 'personId').props.value, 's1'); assert.equal(h.control(1, 'personId').props.value, 'g1'); assert.equal(h.control(0, 'expiryDate').props.value, '2028-02-01'); assert.equal(h.control(0, 'visibility').props.value, 'StaffRestricted');
  assert.match(h.message(), /could not be saved/); assert.equal(nodes(h.render()).find(x => x.type === 'notice').props.tone, 'error'); assert.equal(h.calls.filter(x => x.method).length, 1);
  if (config.network) { await h.submit(index).promise; assert.equal(h.calls.filter(x => x.method).length, 1); assert.match(h.message(), /could not be confirmed/); }
});
for (const index of [0, 1]) for (const config of [{ refreshDenied: true }, { refreshNetwork: true }, { refreshJson: true }]) test('save success survives failed readback with refresh-only recovery ' + index + '/' + JSON.stringify(config), async () => {
  const h = await page(config); h.change(index, 'personId', 's1'); h.change(1 - index, 'personId', 'shared');
  const s = h.submit(index); await s.promise; await h.flush();
  assert.equal(s.form.resets, 1); assert.equal(h.control(index, 'personId').props.value, ''); assert.equal(h.control(1 - index, 'personId').props.value, 'shared'); assert.match(h.message(), /save succeeded; refresh records/); assert.equal(nodes(h.render()).find(x => x.type === 'notice').props.tone, 'success');
  h.change(index, 'personId', 's1'); await h.submit(index).promise; assert.equal(h.calls.filter(x => x.method).length, 1);
  Object.keys(config).forEach(key => config[key] = false);
  await nodes(h.render()).find(x => x.type === 'button' && text(x) === 'Refresh records').props.onClick(); await h.flush();
  assert.match(h.message(), /records refreshed/); assert.equal(h.calls.filter(x => x.method).length, 1); assert.equal(h.control(1 - index, 'personId').props.value, 'shared'); assert.equal(h.control(index, 'personId').props.disabled, false);
});
for (const index of [0, 1]) test('pending save prevents duplicate and cross-form writes, then unlocks ' + index, async () => {
  const h = await page({ defer: true }); h.change(0, 'personId', 's1'); h.change(1, 'personId', 'shared');
  const s = h.submit(index); await h.flush(); await h.submit(index).promise; await h.submit(1 - index).promise;
  assert.equal(h.calls.filter(x => x.method).length, 1); assert.ok(h.forms().every(form => nodes(form).find(x => x.type === 'fieldset').props.disabled));
  h.release(); await s.promise; await h.flush(); assert.ok(h.forms().every(form => !nodes(form).find(x => x.type === 'fieldset').props.disabled)); assert.equal(h.control(1 - index, 'personId').props.value, index === 0 ? 'shared' : 's1');
});
for (const config of [{ loadDenied: true }, { loadMalformed: true }]) test('failed initial load locks forms and does not claim an empty queue ' + JSON.stringify(config), async () => {
  const h = await page(config); assert.match(h.message(), /could not be loaded/); assert.doesNotMatch(text(h.render()), /No document records yet|No consent evidence yet/);
  assert.ok(h.forms().every(form => nodes(form).find(x => x.type === 'fieldset').props.disabled)); await h.submit(0).promise; assert.equal(h.calls.filter(x => x.method).length, 0);
});
for (const index of [0, 1]) test('student payload uses only correct typed ID with blank optional fields; forged form ID ignored ' + index, async () => {
  const h = await page(); h.change(index, 'personId', 's1'); const s = h.submit(index, { personType: 'guardian', personId: 'g1' }); await s.promise; await h.flush();
  const request = h.calls.find(x => x.method); assert.equal(request.url, '/api/academies/owned/compliance/' + (index === 0 ? 'documents' : 'consents')); const body = JSON.parse(request.body);
  assert.deepEqual(body, index === 0 ? { studentId: 's1', guardianId: null, documentType: 'ID', fileName: 'synthetic.pdf', secureReference: '', expiryDate: null, visibility: 'AdminOnly' } : { studentId: 's1', guardianId: null, consentType: 'Media', granted: true, evidenceReference: '' });
});
for (const action of ['Approve', 'Reject', 'Granted · withdraw']) for (const config of [{}, { saveDenied: 500 }, { refreshDenied: true }]) test('review/withdraw use correct route and preserve drafts/feedback ' + action + '/' + JSON.stringify(config), async () => {
  const h = await page({ ...config, documents: [{ id: 'd1', studentId: 's1', documentType: 'ID', fileName: 'synthetic.pdf', status: 'PendingReview', visibility: 'AdminOnly' }], consents: [{ id: 'c1', guardianId: 'g1', consentType: 'Media', granted: true, recordedAtUtc: '2026-10-01T00:00:00Z' }] });
  h.change(0, 'personId', 's1'); h.change(1, 'personId', 'shared'); const button = nodes(h.render()).find(x => x.type === 'button' && text(x) === action); await button.props.onClick(); await h.flush();
  const request = h.calls.find(x => x.method); assert.equal(request.method, 'PATCH'); assert.equal(request.url, '/api/academies/owned/compliance/' + (action.startsWith('Granted') ? 'consents/c1/withdraw' : 'documents/d1/review')); if (!action.startsWith('Granted')) assert.deepEqual(JSON.parse(request.body), { status: action === 'Approve' ? 'Approved' : 'Rejected' }); else assert.equal(request.body, undefined);
  assert.equal(h.control(0, 'personId').props.value, 's1'); assert.equal(h.control(1, 'personId').props.value, 'shared'); assert.equal(nodes(h.render()).find(x => x.type === 'notice').props.tone, config.saveDenied ? 'error' : 'success');
  if (config.refreshDenied) assert.match(h.message(), /save succeeded/);
});
test('typed subject display handles shared IDs, wrong-type historic IDs, missing and ambiguous references without guessing', async () => {
  const refs = [{ studentId: 'shared' }, { guardianId: 'shared' }, { studentId: 'g1' }, { guardianId: 's1' }, {}, { studentId: 's1', guardianId: 'g1' }];
  const h = await page({ documents: refs.map((r, i) => ({ ...r, id: 'd' + i, documentType: 'ID', fileName: 'synthetic.pdf', status: 'PendingReview', visibility: 'AdminOnly' })), consents: refs.map((r, i) => ({ ...r, id: 'c' + i, consentType: 'Media', granted: false, recordedAtUtc: '2026-10-01T00:00:00Z' })) });
  for (const table of nodes(h.render()).filter(x => x.type === 'tbody')) {
    const subjects = nodes(table).filter(x => x.type === 'tr').map(row => text(nodes(row).filter(x => x.type === 'td')[1]));
    assert.deepEqual(subjects, ['Student · Student Collision', 'Parent · Parent Collision', 'Student unavailable (g1)', 'Parent unavailable (s1)', 'Unassigned record', 'Ambiguous subject · Student s1 / Parent g1']);
  }
});
for (const index of [0, 1]) test('confirmed save captures async form and resets only submitted native/controlled draft ' + index, async () => {
  const h = await page(); h.change(0, 'personType', 'guardian'); h.change(0, 'personId', 'g1'); h.change(0, 'expiryDate', '2027-01-01'); h.change(0, 'visibility', 'StaffRestricted'); h.change(1, 'personType', 'guardian'); h.change(1, 'personId', 'g1');
  const s = h.submit(index); await s.promise; await h.flush();
  assert.equal(s.form.resets, 1); assert.equal(h.control(index, 'personType').props.value, 'student'); assert.equal(h.control(index, 'personId').props.value, '');
  assert.equal(h.control(1 - index, 'personType').props.value, 'guardian'); assert.equal(h.control(1 - index, 'personId').props.value, 'g1');
  assert.equal(h.control(0, 'expiryDate').props.value, index === 0 ? '' : '2027-01-01'); assert.equal(h.control(0, 'visibility').props.value, index === 0 ? 'AdminOnly' : 'StaffRestricted');
  assert.match(h.message(), index === 0 ? /registered/ : /recorded/);
  const payload = JSON.parse(h.calls.find(x => x.method === 'POST').body);
  assert.equal(payload.studentId, null); assert.equal(payload.guardianId, 'g1');
  if (index === 0) { assert.equal(payload.expiryDate, '2027-01-01'); assert.equal(payload.visibility, 'StaffRestricted'); assert.equal(payload.secureReference, ''); } else { assert.equal(payload.granted, true); assert.equal(payload.evidenceReference, ''); }
});
