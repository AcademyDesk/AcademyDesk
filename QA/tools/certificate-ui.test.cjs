// Actual certificate TSX with controlled hooks/requests. Not browser/SQL/DOM proof.
const fs = require('node:fs'), assert = require('node:assert/strict'), { test } = require('node:test');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const code = ts.transpileModule(fs.readFileSync('apps/web/src/app/certificates/page.tsx', 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, target: ts.ScriptTarget.ES2022 } }).outputText;
const baseline = process.env.QA_CERTIFICATE_UI_BASELINE === '1';
const students = [{ id: 's1', firstName: 'Student', lastName: 'One' }, { id: 's2', firstName: 'Student', lastName: 'Two' }];
const batches = [{ id: 'b1', name: 'Active class' }, { id: 'b2', name: 'Completed class', isActive: false }, { id: 'b3', name: 'Other learner class' }, { id: 'b4', name: 'Waitlist' }, { id: 'b5', name: 'Unrelated class' }];
const enrollments = [{ studentId: 's1', batchId: 'b1', status: 'Active' }, { studentId: 's1', batchId: 'b2', status: 'Completed' }, { studentId: 's2', batchId: 'b3', status: 'Active' }, { studentId: 's1', batchId: 'b4', status: 'Waitlisted' }];
const nodes = x => !x || typeof x !== 'object' ? [] : Array.isArray(x) ? x.flatMap(nodes) : [x, ...nodes(x.props?.children)];
const text = x => x == null ? '' : Array.isArray(x) ? x.map(text).join(' ') : typeof x === 'object' ? text(x.props?.children) : String(x);
async function page(config = {}) {
  const state = [], refs = [], effects = [], deps = [], callbacks = [], callbackDeps = [], calls = []; let i, r, e, c, saved = false, releaseWrite, releaseRead;
  const react = {
    useState(initial) { const k = i++; if (!(k in state)) state[k] = typeof initial === 'function' ? initial() : initial; return [state[k], value => state[k] = typeof value === 'function' ? value(state[k]) : value]; },
    useRef(initial) { const k = r++; return refs[k] ??= { current: initial }; },
    useMemo(fn) { return fn(); },
    useCallback(fn, next) { const k = c++; if (!callbackDeps[k] || next.some((v, j) => v !== callbackDeps[k][j])) { callbackDeps[k] = next; callbacks[k] = fn; } return callbacks[k]; },
    useEffect(fn, next) { const k = e++; if (!deps[k] || next.some((v, j) => v !== deps[k][j])) { deps[k] = next; effects.push(fn); } },
  };
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) {
      if (config.deferWrite) await new Promise(resolve => releaseWrite = resolve);
      if (config.network) throw Error('offline');
      if (config.saveDenied) return { ok: false, status: config.saveDenied, json: async () => { if (config.badErrorJson) throw Error('invalid json'); return { message: config.errorMessage ?? 'Select a class or batch where this student has an Active or Completed enrollment.' }; } };
      saved = true; return { ok: true, status: 201 };
    }
    if (config.deferRead && url.endsWith('/enrollments')) await new Promise(resolve => releaseRead = resolve);
    const failedRead = config.loadFailure === 'all' || (url.endsWith('/enrollments') && config.loadFailure);
    if (failedRead === 'network' || (saved && config.refreshFailure === 'network')) throw Error('offline');
    if (failedRead === 'denied' || failedRead === true || (saved && config.refreshFailure === 'denied')) return { ok: false, status: 403, json: async () => [] };
    const data = url === '/api/academies' ? config.academies ?? [{ id: 'owned' }] : url.endsWith('/students') ? config.students ?? students : url.endsWith('/batches') ? config.batches ?? batches : url.endsWith('/enrollments') ? config.enrollments ?? enrollments : url.endsWith('/branding') ? { academyName: 'Synthetic Academy', accentColor: '#0F6CBD' } : config.certificates ?? [];
    return { ok: true, status: 200, json: async () => { if (failedRead === 'json' || (saved && config.refreshFailure === 'json')) throw Error('invalid json'); return failedRead === 'malformed' || (saved && config.refreshFailure === 'malformed') ? {} : data; } };
  };
  const mod = { exports: {} };
  new Function('require', 'module', 'exports', code)(name => name === 'react' ? react : name === 'react/jsx-runtime' ? jsx : name === '@/lib/api' ? { academyApi: api, apiHeaders: () => ({ 'Content-Type': 'application/json' }), apiUrl: 'http://qa.invalid' } : name === '@/components/workspace-nav' ? { WorkspaceNav: 'nav' } : name === '@/components/enterprise-page-state' ? { EnterprisePageState: 'notice' } : name === '@/components/design-system/controls' ? { StandardSelectField: 'select', StandardDateField: 'date' } : (() => { throw Error(name); })(), mod, mod.exports);
  const render = () => { i = r = e = c = 0; return mod.exports.default(); };
  const flush = async () => { for (let n = 0; n < 5; n++) { effects.splice(0).forEach(fn => fn()); await new Promise(resolve => setImmediate(resolve)); render(); } };
  render(); await flush();
  const form = () => nodes(render()).find(x => x.type === 'form' && x.props.className?.includes('certificate-issue-form'));
  const control = name => nodes(form()).find(x => x.props?.name === name);
  const change = (name, value) => { const node = control(name); node.props.onChange(node.type === 'select' || node.type === 'date' ? value : { target: { value } }); render(); };
  const submit = () => form().props.onSubmit({ preventDefault() {}, currentTarget: null });
  const refresh = () => nodes(render()).find(x => x.type === 'button' && text(x) === 'Refresh records').props.onClick();
  return { render, flush, calls, form, control, change, submit, refresh, config, releaseWrite: () => releaseWrite?.(), releaseRead: () => releaseRead?.(), notice: () => nodes(render()).find(x => x.type === 'notice'), message: () => text(nodes(render()).find(x => x.type === 'notice')) || text(nodes(render()).find(x => x.props?.role === 'status')) };
}
test('batch picker includes only selected student Active/Completed enrollments, including inactive completed history', async () => {
  const h = await page(); assert.deepEqual(h.control('batchId').props.options.map(x => x.value), ['b1', 'b2']);
});
test('changing student clears stale batch and replaces eligible options immediately', async () => {
  const h = await page(); h.change('batchId', 'b1'); h.change('studentId', 's2');
  assert.equal(h.control('batchId').props.value, ''); assert.deepEqual(h.control('batchId').props.options.map(x => x.value), ['b3']);
});
test('server enrollment rejection remains useful rather than generic', async () => {
  const h = await page({ saveDenied: 400 }); h.change('batchId', 'b1'); await h.submit(); await h.flush(); assert.match(h.message(), /Active or Completed enrollment/);
});
test('confirmed issuance success survives record refresh', async () => {
  const h = await page(); await h.submit(); await h.flush(); assert.match(h.message(), /Certificate issued/);
});
if (!baseline) {
  test('pending initial read blocks refresh and issuance until eligibility is loaded', async () => {
    const h = await page({ deferRead: true }); const before = h.calls.length;
    await h.refresh(); await h.submit(); assert.equal(h.calls.length, before); assert.equal(nodes(h.form()).find(x => x.type === 'fieldset').props.disabled, true);
    h.releaseRead(); await h.flush(); assert.equal(nodes(h.form()).find(x => x.type === 'fieldset').props.disabled, false);
  });
  for (const status of ['Active', 'Completed', 'active', 'cOmPlEtEd']) test('eligible enrollment status ' + status, async () => {
    const h = await page({ enrollments: [{ studentId: 'S1', batchId: 'B1', status }] }); assert.deepEqual(h.control('batchId').props.options.map(x => x.value), ['b1']);
  });
  for (const status of ['Waitlisted', 'Paused', 'Withdrawn', 'Cancelled', 'Transferred', 'Unknown', '', null, 42]) test('ineligible/malformed status excluded ' + JSON.stringify(status), async () => {
    const h = await page({ enrollments: [{ studentId: 's1', batchId: 'b1', status }] }); assert.deepEqual(h.control('batchId').props.options, []);
  });
  test('duplicate enrollment rows do not duplicate batches; missing batch references do not create options', async () => {
    const h = await page({ enrollments: [...enrollments, enrollments[0], { studentId: 's1', batchId: 'missing', status: 'Active' }] }); assert.deepEqual(h.control('batchId').props.options.map(x => x.value), ['b1', 'b2']);
  });
  test('no selected student has no batch options and cannot issue', async () => {
    const h = await page(); h.change('studentId', ''); assert.deepEqual(h.control('batchId').props.options, []); await h.submit(); assert.equal(h.calls.filter(x => x.method).length, 0); assert.match(h.message(), /Choose a valid student/);
  });
  for (const batch of ['b3', 'b4', 'b5', 'missing']) test('forged ineligible batch sends no POST ' + batch, async () => {
    const h = await page(); h.change('batchId', batch); await h.submit(); assert.equal(h.calls.filter(x => x.method).length, 0); assert.match(h.message(), /Active or Completed enrollment/);
  });
  test('missing student sends no POST', async () => {
    const h = await page(); h.change('studentId', 'missing'); await h.submit(); assert.equal(h.calls.filter(x => x.method).length, 0); assert.match(h.message(), /Choose a valid student/);
  });
  for (const batch of ['', 'b1', 'b2']) test('exact issue payload preserves optional fields and eligible batch ' + (batch || 'independent'), async () => {
    const h = await page(); h.change('batchId', batch); await h.submit(); const call = h.calls.find(x => x.method === 'POST');
    assert.equal(call.url, '/api/academies/owned/certificates'); assert.deepEqual(JSON.parse(call.body), { studentId: 's1', batchId: batch || null, title: 'Certificate of Achievement', templateKey: 'music-recital', issuedDate: null, notes: null });
  });
  test('confirmed save clears notes/date only and preserves title/theme/student/batch', async () => {
    const h = await page(); h.change('batchId', 'b2'); h.change('title', 'Synthetic honour'); h.change('themeKey', 'school-merit'); h.change('issuedDate', '2020-06-01'); h.change('notes', 'Synthetic note');
    await h.submit(); await h.flush();
    assert.deepEqual(JSON.parse(h.calls.find(x => x.method === 'POST').body), { studentId: 's1', batchId: 'b2', title: 'Synthetic honour', templateKey: 'school-merit', issuedDate: '2020-06-01', notes: 'Synthetic note' });
    for (const [name, value] of [['notes', ''], ['issuedDate', ''], ['title', 'Synthetic honour'], ['themeKey', 'school-merit'], ['studentId', 's1'], ['batchId', 'b2']]) assert.equal(h.control(name).props.value, value);
    assert.equal(h.notice().props.tone, 'success');
  });
  for (const status of [400, 403, 500]) test('HTTP failure retains draft and unlocks ' + status, async () => {
    const h = await page({ saveDenied: status }); h.change('batchId', 'b1'); h.change('issuedDate', '2020-06-01'); h.change('notes', 'Keep note'); await h.submit(); await h.flush();
    for (const [name, value] of [['notes', 'Keep note'], ['issuedDate', '2020-06-01'], ['batchId', 'b1']]) assert.equal(h.control(name).props.value, value);
    assert.equal(h.notice().props.tone, 'error'); assert.equal(nodes(h.form()).find(x => x.type === 'fieldset').props.disabled, false);
  });
  test('malformed error body uses safe generic feedback and retains draft', async () => {
    const h = await page({ saveDenied: 400, badErrorJson: true }); h.change('notes', 'Keep note'); await h.submit(); await h.flush(); assert.match(h.message(), /could not be issued/); assert.equal(h.control('notes').props.value, 'Keep note');
  });
  test('network uncertainty retains draft, blocks retry until explicit refresh', async () => {
    const h = await page({ network: true }); h.change('notes', 'Keep note'); await h.submit(); await h.flush(); assert.match(h.message(), /could not be confirmed/); await h.submit(); assert.equal(h.calls.filter(x => x.method).length, 1); assert.equal(h.control('notes').props.value, 'Keep note');
    h.config.network = false; await h.refresh(); await h.flush(); await h.submit(); assert.equal(h.calls.filter(x => x.method).length, 2);
  });
  for (const failure of ['denied', 'network', 'json', 'malformed']) test('successful issue plus failed readback remains success and blocks writes until recovery ' + failure, async () => {
    const h = await page({ refreshFailure: failure, certificates: [{ id: 'old', studentId: 's1', certificateNumber: 'CERT-SYNTHETIC', title: 'Already issued certificate', templateKey: 'music-recital', issuedDate: '2020-06-01', verificationCode: 'SYNTHETIC', status: 'Issued' }] });
    h.config.certificates = []; h.change('notes', 'Saved note'); await h.submit(); await h.flush(); assert.match(h.message(), /issued.*records could not be refreshed/i); assert.equal(h.notice().props.tone, 'success'); assert.equal(h.control('notes').props.value, ''); assert.match(text(h.render()), /Already issued certificate/);
    await h.submit(); assert.equal(h.calls.filter(x => x.method).length, 1); h.config.refreshFailure = undefined; await h.refresh(); await h.flush(); assert.match(h.message(), /records refreshed/); assert.equal(nodes(h.form()).find(x => x.type === 'fieldset').props.disabled, false);
  });
  for (const failure of ['denied', 'network', 'json', 'malformed']) test('initial enrollment read failure blocks writes and does not claim an empty register ' + failure, async () => {
    const h = await page({ loadFailure: failure }); assert.match(h.message(), /could not be loaded/); assert.doesNotMatch(text(h.render()), /No certificates have been issued yet/); assert.equal(nodes(h.form()).find(x => x.type === 'fieldset').props.disabled, true); await h.submit(); assert.equal(h.calls.filter(x => x.method).length, 0);
    h.config.loadFailure = undefined; await h.refresh(); await h.flush(); assert.equal(nodes(h.form()).find(x => x.type === 'fieldset').props.disabled, false);
  });
  test('pending issue rejects duplicates and refresh; student/batch remain frozen', async () => {
    const h = await page({ deferWrite: true }); h.change('batchId', 'b1'); const pending = h.submit(); await h.flush(); await h.submit(); await h.refresh(); h.change('studentId', 's2'); h.change('batchId', 'b2'); h.change('themeKey', 'school-merit');
    assert.equal(h.calls.filter(x => x.method).length, 1); assert.equal(h.control('studentId').props.value, 's1'); assert.equal(h.control('batchId').props.value, 'b1'); assert.equal(h.control('themeKey').props.value, 'music-recital'); assert.equal(h.control('themeKey').props.disabled, true); assert.equal(nodes(h.form()).find(x => x.type === 'fieldset').props.disabled, true);
    h.releaseWrite(); await pending; await h.flush(); assert.equal(nodes(h.form()).find(x => x.type === 'fieldset').props.disabled, false);
  });
  test('refresh clears newly ineligible selection and keeps learner/title/note', async () => {
    const h = await page(); h.change('batchId', 'b1'); h.change('notes', 'Keep draft'); h.config.enrollments = [{ studentId: 's1', batchId: 'b1', status: 'Withdrawn' }]; await h.refresh(); await h.flush();
    assert.equal(h.control('batchId').props.value, ''); assert.equal(h.control('studentId').props.value, 's1'); assert.equal(h.control('notes').props.value, 'Keep draft'); assert.deepEqual(h.control('batchId').props.options, []);
  });
  test('refresh retains valid selection using current academy, not changed first academy', async () => {
    const h = await page(); h.change('batchId', 'b2'); h.config.academies = [{ id: 'other' }]; const start = h.calls.length; await h.refresh(); await h.flush();
    assert.equal(h.control('batchId').props.value, 'b2'); assert.ok(h.calls.slice(start).every(x => x.url.startsWith('/api/academies/owned/')));
  });
  test('refresh of removed learner clears previous class while retaining drafts', async () => {
    const h = await page(); h.change('batchId', 'b1'); h.change('notes', 'Keep draft'); h.config.students = [students[1]]; await h.refresh(); await h.flush(); assert.equal(h.control('studentId').props.value, 's2'); assert.equal(h.control('batchId').props.value, ''); assert.equal(h.control('notes').props.value, 'Keep draft');
  });
  test('empty learner dataset retains optional no-class semantics but cannot issue', async () => {
    const h = await page({ students: [] }); assert.equal(h.control('studentId').props.value, ''); assert.equal(h.control('batchId').props.value, ''); assert.deepEqual(h.control('batchId').props.options, []); await h.submit(); assert.equal(h.calls.filter(x => x.method).length, 0);
  });
  test('pending refresh blocks issuance and duplicate refresh until completed', async () => {
    const h = await page(); h.config.deferRead = true; const pending = h.refresh(); await h.flush(); const before = h.calls.length; await h.refresh(); await h.submit(); assert.equal(h.calls.length, before); assert.equal(h.calls.filter(x => x.method).length, 0); h.releaseRead(); await pending; await h.flush();
  });
}
