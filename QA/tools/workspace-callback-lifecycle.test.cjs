// Actual page TSX with dependency-aware controlled hooks and synthetic reads.
// These tests do not certify server roles, tenant isolation or SQL operations.
const fs = require('node:fs'), assert = require('node:assert/strict'), { test } = require('node:test');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const callbackHookFixture = require('./callback-hook-fixture.cjs');
const nodes = n => n == null ? [] : Array.isArray(n) ? n.flatMap(nodes) : typeof n === 'object' ? [n, ...nodes(n.props?.children)] : [];
const tick = () => new Promise(resolve => setImmediate(resolve));
const people = ['one', 'two'].map(id => ({ id, firstName: 'Synthetic', lastName: id, isActive: true }));
function data(url) {
  if (url === '/api/academies') return [{ id: 'owned', name: 'Synthetic academy' }];
  if (url === '/api/portal/me') return { role: 'Parent', displayName: 'Synthetic Parent', parentId: 'parent', children: people.map(p => ({ ...p, name: p.lastName })) };
  if (/\/profile$/.test(url)) return { preferredName: url.includes('/two/') ? 'Second' : 'First' };
  if (/\/compensation$/.test(url)) return { model: 'Monthly', monthlySalary: url.includes('/two/') ? 200 : 100 };
  if (/\/(teachers|guardians|students|student-options)$/.test(url)) return people;
  if (/\/batches$/.test(url)) return [{ id: 'batch', name: 'Synthetic batch', teacherId: 'one', branchId: 'branch', isActive: true, capacity: 10 }];
  if (/\/branches$/.test(url)) return [{ id: 'branch', name: 'Synthetic branch' }];
  if (/\/courses$/.test(url)) return [{ id: 'course', name: 'Synthetic course' }];
  if (/\/document-settings$/.test(url)) return {};
  if (/\/portal\/students\/[^/]+$/.test(url)) return { name: url.endsWith('/two') ? 'Second learner' : 'First learner' };
  return [];
}
async function fixture(route) {
  let stateIndex = 0, effectIndex = 0, refIndex = 0;
  const state = [], effects = [], pending = [], refs = [], calls = [], callbacks = callbackHookFixture();
  const react = {
    useCallback: callbacks.useCallback,
    useState(initial) { const i = stateIndex++; if (!(i in state)) state[i] = typeof initial === 'function' ? initial() : initial; return [state[i], next => { state[i] = typeof next === 'function' ? next(state[i]) : next; }]; },
    useEffect(fn, deps) { const i = effectIndex++, old = effects[i]; if (!old || deps.some((value, j) => !Object.is(value, old.deps[j]))) { old?.cleanup?.(); effects[i] = { deps }; pending.push(() => { effects[i].cleanup = fn(); }); } },
    useRef(initial) { return refs[refIndex++] ??= { current: initial }; },
    useMemo: fn => fn(),
  };
  const api = async (url, options = {}) => { calls.push({ url, ...options }); assert.equal(options.method, undefined, 'read-only fixture'); return { ok: true, status: 200, json: async () => data(url) }; };
  function execute(file) {
    const m = { exports: {} }, code = ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
    new Function('require', 'module', 'exports', 'window', code)(name => {
      if (name === 'next/image') return { default: 'img' }; if (name === 'react/jsx-runtime') return jsx;
      if (name === 'react') return react;
      if (name === '@/lib/api') return { academyApi: api, apiHeaders: () => ({}) };
      if (name === 'next/link') return { default: 'a' };
      if (name === 'next/navigation') return { useRouter: () => ({ push() {}, replace() {} }), usePathname: () => '/' + route };
      if (name === '@/lib/batch-update') return execute('apps/web/src/lib/batch-update.ts');
      if (name === '@/components/penta-manual-theme') return { usePentaManualTheme: () => ({ className: '' }) };
      if (name.endsWith('.module.css')) return { default: {} };
      if (name.startsWith('@/components/')) return new Proxy({}, { get: (_, key) => key });
      throw Error('Unexpected import ' + name);
    }, m, m.exports, { location: { search: '' } });
    return m.exports;
  }
  const component = execute('apps/web/src/app/' + route + '/page.tsx').default;
  const render = () => { stateIndex = effectIndex = refIndex = 0; callbacks.reset(); const tree = component(); pending.splice(0).forEach(fn => fn()); return nodes(tree); };
  const settle = async () => { for (let i = 0; i < 7; i++) { render(); await tick(); } return render(); };
  await settle();
  return { state, calls, render, settle, dispose: () => effects.forEach(effect => effect?.cleanup?.()) };
}
const routes = ['guardian-profile', 'teacher-profile', 'teacher-payments', 'staff', 'teachers', 'schedule', 'invoices', 'payments', 'portal'];
for (const route of routes) {
  test(route + ': discovery runs once across settled renders', async () => {
    const f = await fixture(route), discovery = route === 'portal' ? '/api/portal/me' : '/api/academies';
    assert.equal(f.calls.filter(call => call.url === discovery).length, 1);
    assert.ok(f.calls.length > 1, 'dependent reads completed');
    const before = f.calls.length; await f.settle(); assert.equal(f.calls.length, before, 'no callback-driven reload loop');
    f.dispose();
  });
  test(route + ': unrelated UI state does not restart discovery or erase selection', async () => {
    const f = await fixture(route), before = f.calls.length;
    const index = route === 'portal' ? 7 : route === 'schedule' ? 7 : route === 'invoices' ? 7 : route === 'payments' ? 5 : route === 'staff' ? 2 : route === 'teachers' ? 6 : route === 'teacher-payments' ? 3 : 4;
    const draft = route.endsWith('-profile') ? { ...f.state[index], preferredName: 'Preserved draft' } : route === 'teacher-payments' ? { ...f.state[index], monthlySalary: '321' } : 'Preserved draft';
    f.state[index] = draft; await f.settle(); assert.deepEqual(f.state[index], draft); assert.equal(f.calls.length, before);
    f.dispose();
  });
}
for (const route of ['guardian-profile', 'teacher-profile', 'teacher-payments']) {
  test(route + ': second record uses explicit current academy without resetting to first', async () => {
    const f = await fixture(route), selection = f.render().find(n => n.props?.value === 'one' && (n.type === 'select' || n.props?.options));
    assert.ok(selection, 'record selector'); selection.props.onChange(selection.props.options ? 'two' : { target: { value: 'two' } }); await f.settle();
    const suffix = route === 'teacher-payments' ? 'compensation' : 'profile';
    assert.ok(f.calls.some(call => call.url === '/api/academies/owned/' + (route === 'guardian-profile' ? 'guardians' : 'teachers') + '/two/' + suffix));
    assert.equal(f.state[2], 'two'); assert.equal(f.calls.filter(call => call.url === '/api/academies').length, 1);
    assert.equal(f.state[route === 'teacher-payments' ? 3 : 4][route === 'teacher-payments' ? 'monthlySalary' : 'preferredName'], route === 'teacher-payments' ? 200 : 'Second'); f.dispose();
  });
}
test('Parent portal: selected-child refresh is not captured from initial discovery', async () => {
  const f = await fixture('portal'), select = f.render().find(n => n.type === 'select' && n.props.value === 'one'); assert.ok(select);
  select.props.onChange({ target: { value: 'two' } }); await f.settle();
  const view = f.render().find(n => typeof n.type === 'function' && n.type.name === 'View'); assert.ok(view); assert.equal(view.props.id, 'two');
  const before = f.calls.length; await view.props.refresh(); await f.settle();
  assert.equal(f.calls[before].url, '/api/portal/students/two'); assert.equal(f.state[1], 'two'); assert.equal(f.calls.filter(call => call.url === '/api/portal/me').length, 1); f.dispose();
});
test('Callback fixture: equal dependencies retain identity; changed scope gets fresh closure', () => {
  const f = callbackHookFixture(), first = f.useCallback(() => 'A', ['A']); f.reset();
  assert.equal(f.useCallback(() => 'ignored', ['A']), first); f.reset();
  const next = f.useCallback(() => 'B', ['B']); assert.notEqual(next, first); assert.equal(next(), 'B');
});
