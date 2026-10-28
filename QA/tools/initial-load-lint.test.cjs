// Actual page/component code with controlled hooks and synthetic delayed responses.
// This verifies state/transport behavior, not browser rendering or SQL persistence.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript');
const jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const root = path.resolve(__dirname, '../..');
const nodes = x => !x || typeof x !== 'object' ? [] : Array.isArray(x) ? x.flatMap(nodes) : [x, ...nodes(x.props?.children)];
const text = x => x == null || typeof x === 'boolean' ? '' : Array.isArray(x) ? x.map(text).join('') : typeof x === 'object' ? text(x.props?.children) : String(x);
const response = (data, ok = true) => ({ ok, status: ok ? 200 : 503, json: async () => data, text: async () => JSON.stringify(data) });
const deferred = () => { let resolve; const promise = new Promise(r => { resolve = r; }); return { promise, resolve }; };
class SyntheticFormData {
  constructor(form) { if (!form) throw Error('Form target was released'); this.values = form.values ?? {}; }
  get(name) { return this.values[name] ?? null; }
  *[Symbol.iterator]() { yield* Object.entries(this.values); }
}

function mount(file, api, props = {}, exportName = 'default') {
  const state = [], refs = [], effects = [], pending = new Set();
  let index, refIndex, effectIndex, dirty, disposed = false, lateWrites = 0;
  const redirects = [], query = new URLSearchParams();
  const router = { replace: url => redirects.push(url), push: url => redirects.push(url) };
  const react = {
    useState(initial) {
      const key = index++;
      if (!(key in state)) state[key] = typeof initial === 'function' ? initial() : initial;
      return [state[key], value => {
        if (disposed) lateWrites++;
        const next = typeof value === 'function' ? value(state[key]) : value;
        if (!Object.is(state[key], next)) { state[key] = next; dirty = true; }
      }];
    },
    useRef(value) { return refs[refIndex++] ??= { current: value }; },
    useMemo: fn => fn(),
    useEffect(fn, deps) {
      const key = effectIndex++, old = effects[key];
      if (!old || !deps || deps.some((v, i) => !Object.is(v, old.deps[i]))) {
        effects[key] = { fn, deps, cleanup: old?.cleanup }; pending.add(key);
      }
    },
  };
  function execute(relative) {
    const mod = { exports: {} };
    let source = fs.readFileSync(path.join(root, relative), 'utf8');
    if (relative === 'apps/web/src/app/teacher/page.tsx') source += '\nexport { TeacherAttendanceRoster, TeacherClassroom };';
    const code = ts.transpileModule(source, {
      compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX },
    }).outputText;
    new Function('require', 'module', 'exports', 'FormData', code)(name => {
      if (name === 'react') return react;
      if (name === 'react/jsx-runtime') return jsx;
      if (name === 'next/link') return { default: 'a' };
      if (name === 'next/navigation') return { usePathname: () => '/batches', useSearchParams: () => query, useRouter: () => router };
      if (name === '@/lib/api') return { academyApi: api, apiHeaders: () => ({}) };
      if (name === '@/lib/course-update') return execute('apps/web/src/lib/course-update.ts');
      if (name === '@/lib/batch-update') return execute('apps/web/src/lib/batch-update.ts');
      if (name === '@/lib/teacher-workspace') return execute('apps/web/src/lib/teacher-workspace.ts');
      if (name === '@/lib/use-clock') return { useClock: () => Date.parse('2026-10-28T12:00:00Z') };
      if (name === '@/components/theme-toggle') return { ThemeToggle: 'theme-toggle' };
      if (name === '@/components/teacher-material-uploader') return { TeacherMaterialUploader: 'uploader' };
      if (name === '@/components/private-material-actions') return { PrivateMaterialActions: 'material-actions' };
      if (name === '@/components/workspace-nav') return { WorkspaceNav: 'nav' };
      if (name === '@/components/standard-date-field') return { StandardDateField: 'date-control' };
      if (name === '@/components/standard-select-field') return { StandardSelectField: 'select-control' };
      if (name === '@/components/design-system/controls') return { StandardSelectField: 'select-control', StandardDateField: 'date-control', StandardTimeField: 'time-control' };
      if (name === '@/components/design-system/interactive') return { StandardDetailModal: 'detail-modal', StandardInteractiveTile: 'tile' };
      throw Error('Unexpected import: ' + name);
    }, mod, mod.exports, SyntheticFormData);
    return mod.exports;
  }
  const component = execute(file)[exportName];
  function render() {
    assert.equal(disposed, false);
    let tree;
    for (let n = 0; n < 20; n++) {
      index = refIndex = effectIndex = 0; dirty = false; tree = component(props);
      if (!dirty) {
        for (const key of pending) {
          effects[key].cleanup?.(); effects[key].cleanup = effects[key].fn();
        }
        pending.clear(); return tree;
      }
    }
    throw Error('Render did not settle');
  }
  async function flush() {
    for (let n = 0; n < 8; n++) { await new Promise(r => setImmediate(r)); if (!disposed) render(); }
  }
  render();
  return { render, flush, props, query, redirects, get lateWrites() { return lateWrites; },
    unmount() { disposed = true; effects.forEach(effect => effect.cleanup?.()); } };
}

test('academic periods loads the actual academy/year/term snapshot', async () => {
  const h = mount('apps/web/src/app/academic-periods/page.tsx', async url => response(url === '/api/academies' ? [{ id: 'a' }] : {
    years: [{ id: 'y', name: 'Synthetic year', startDate: '2026-01-01', endDate: '2026-12-31', isCurrent: true, isClosed: false }], terms: [],
  }));
  await h.flush(); assert.match(text(h.render()), /Synthetic year/); h.unmount();
});

for (const page of ['platform', 'platform/control']) test(`${page} preserves the access-denied sign-in redirect`, async () => {
  const h = mount(`apps/web/src/app/${page}/page.tsx`, async () => ({ ok: false, status: 403 }));
  await h.flush(); assert.deepEqual(h.redirects, [`/login?returnTo=/${page}`]); h.unmount();
});

test('platform control ignores an old tenant profile after a newer selection loads', async () => {
  const old = deferred();
  const academies = ['one', 'two'].map(id => ({ id, name: `Academy ${id}`, subscriptionPlan: 'Trial',
    subscriptionStatus: 'Trial', studentLimit: 10, staffLimit: 5, enabledModulesJson: '["Core"]' }));
  const h = mount('apps/web/src/app/platform/control/page.tsx', async url => {
    if (url === '/api/platform/academies') return response(academies);
    if (url === '/api/platform/academies/one/onboarding') return { ok: true, text: () => old.promise };
    if (url === '/api/platform/academies/two/onboarding') return response({ primaryContactName: 'Current tenant contact', documentsJson: '[]' });
    return response([], false);
  });
  await h.flush(); h.query.set('tab', 'Tenant information');
  const selector = nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === 'academy');
  assert.ok(selector); selector.props.onChange('two'); await h.flush();
  old.resolve(JSON.stringify({ primaryContactName: 'Stale tenant contact', documentsJson: '[]' }));
  await h.flush(); assert.match(text(h.render()), /Current tenant contact/);
  assert.doesNotMatch(text(h.render()), /Stale tenant contact/); h.unmount();
});

for (const status of [401, 403, 500]) test(`teacher initial ${status} keeps the correct recovery guidance`, async () => {
  const h = mount('apps/web/src/app/teacher/page.tsx', async () => ({ ok: false, status }));
  await h.flush();
  assert.match(text(h.render()), status === 401 ? /sign in/i : status === 403 ? /Academy Admin/ : /connection and reload/);
  h.unmount();
});

test('teacher attendance drafts reset when the session changes, preserving same-session edits', () => {
  const roster = [{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }];
  const h = mount('apps/web/src/app/teacher/page.tsx', async () => { throw Error('Unexpected request'); },
    { sessionId: 'one', roster, attendance: [{ studentId: 'student', status: 'Present' }], teacherStatus: 'Present', busy: false, onSubmit() {} }, 'TeacherAttendanceRoster');
  const menus = () => nodes(h.render()).filter(n => typeof n.type === 'function' && n.type.name === 'AttendanceStatusMenu');
  menus()[1].props.onChange('Absent'); assert.equal(menus()[1].props.value, 'Absent');
  h.props.sessionId = 'two'; h.props.teacherStatus = 'Absent'; h.props.attendance = [{ studentId: 'student', status: 'Present' }];
  assert.deepEqual(menus().map(n => n.props.value), ['Absent', 'Present']); h.unmount();
});

test('teacher classroom clears the old one-to-one focus on a different group class', async () => {
  const props = { ownerId: 'teacher', batches: [{ id: 'private', name: 'Private', capacity: 1 }, { id: 'group', name: 'Group', capacity: 5 }],
    sessions: [{ id: 'one', batchId: 'private' }, { id: 'two', batchId: 'group' }], sessionId: 'one',
    roster: [{ id: 'old', firstName: 'Old', lastName: 'Focus' }], onSessionSelect() {} };
  const h = mount('apps/web/src/app/teacher/page.tsx', async () => response({ resources: [], homework: [] }), props, 'TeacherClassroom');
  assert.match(text(h.render()), /Old Focus/);
  props.sessionId = 'two'; props.roster = [{ id: 'new', firstName: 'New', lastName: 'Learner' }];
  const focus = nodes(h.render()).find(n => typeof n.type === 'function' && n.type.name === 'TeacherDropdown' && n.props.label === 'Teaching focus');
  assert.ok(focus); assert.equal(focus.props.value, '');
  assert.ok(focus.props.options.some(x => x.value === 'new'));
  assert.ok(!focus.props.options.some(x => x.value === 'old'));
  await h.flush(); h.unmount();
});

for (const fail of ['academy', 'periods']) test(`academic periods handles a failed ${fail} response`, async () => {
  const h = mount('apps/web/src/app/academic-periods/page.tsx', async url => response(
    url === '/api/academies' ? [{ id: 'a' }] : {}, (url === '/api/academies') !== (fail === 'academy')));
  await h.flush(); assert.match(text(h.render()), /Academic governance could not be loaded/); h.unmount();
});

for (const page of ['academic-periods', 'courses', 'batches', 'access-review', 'access-review/sign-off', 'platform-services', 'students', 'work-queue']) test(`${page} ignores a late initial response after unmount`, async () => {
  const pending = deferred();
  const h = mount(`apps/web/src/app/${page}/page.tsx`, async url => {
    assert.equal(url, '/api/academies'); return { ok: true, json: () => pending.promise };
  });
  h.unmount(); pending.resolve([]); await h.flush(); assert.equal(h.lateWrites, 0);
});

test('work queue ignores the initial All response when a newer filter finishes first', async () => {
  const initial = deferred();
  const h = mount('apps/web/src/app/work-queue/page.tsx', async url => {
    if (url === '/api/academies') return response([{ id: 'a' }]);
    if (url.endsWith('/staff')) return response([]);
    if (url.endsWith('/admin-work-items')) return { ok: true, json: () => initial.promise };
    assert.ok(url.endsWith('/admin-work-items?status=Open'));
    return response([{ id: 'new', title: 'Current filtered task', type: 'Operations', priority: 'Normal', status: 'Open' }]);
  });
  await h.flush();
  nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === 'workQueueFilter').props.onChange('Open');
  await h.flush();
  initial.resolve([{ id: 'old', title: 'Stale all task', type: 'Operations', priority: 'Normal', status: 'Completed' }]);
  await h.flush();
  assert.match(text(h.render()), /Current filtered task/);
  assert.doesNotMatch(text(h.render()), /Stale all task/); h.unmount();
});

test('fee arrangements ignores an old student response after selection changes', async () => {
  const old = deferred();
  const h = mount('apps/web/src/components/student-fee-arrangements.tsx', async url => {
    if (url.includes('/students/old/')) return { ok: true, json: () => old.promise };
    return response([{ id: 'new-fee', subjectName: 'Current subject', amount: 12, frequency: 'Monthly', effectiveFrom: '2026-01-01', isActive: true }]);
  }, { academyId: 'a', studentId: 'old' }, 'StudentFeeArrangements');
  h.props.studentId = 'new'; h.render(); await h.flush();
  old.resolve([{ id: 'old-fee', subjectName: 'Stale subject', amount: 99, frequency: 'Monthly', isActive: true }]);
  await h.flush(); assert.match(text(h.render()), /Current subject/); assert.doesNotMatch(text(h.render()), /Stale subject/); h.unmount();
});

test('fee arrangements reports transport failure and does not request an empty selection', async () => {
  let calls = 0;
  const h = mount('apps/web/src/components/student-fee-arrangements.tsx', async () => { calls++; throw Error('private transport detail'); },
    { academyId: 'a', studentId: '' }, 'StudentFeeArrangements');
  await h.flush(); assert.equal(calls, 0);
  h.props.studentId = 'student'; h.render(); await h.flush();
  assert.match(text(h.render()), /Subject fees could not be loaded/);
  assert.doesNotMatch(text(h.render()), /private transport detail/); h.unmount();
});

test('profile edits survive rerender; a new controlled profile replaces all fields', () => {
  const h = mount('apps/web/src/components/student-admin-profile.tsx', async () => { throw Error('Unexpected transport'); },
    { academyId: 'a', studentId: 'one', profile: { studentNumber: 'ONE', adminNotes: 'First notes' }, onSaved() {} }, 'StudentAdminProfile');
  const input = () => nodes(h.render()).find(n => n.type === 'input' && n.props.value === 'ONE');
  assert.ok(input()); input().props.onChange({ target: { value: 'UNSAVED' } });
  assert.ok(nodes(h.render()).some(n => n.type === 'input' && n.props.value === 'UNSAVED'));
  h.props.studentId = 'two'; h.props.profile = { studentNumber: 'TWO', adminNotes: 'Second notes' };
  const tree = h.render();
  assert.ok(nodes(tree).some(n => n.type === 'input' && n.props.value === 'TWO'));
  assert.ok(nodes(tree).some(n => n.type === 'textarea' && n.props.value === 'Second notes'));
  assert.ok(!nodes(tree).some(n => n.props?.value === 'UNSAVED')); h.unmount();
});

for (const page of ['academic-periods', 'access-review', 'access-review/sign-off', 'work-queue', 'platform-services', 'teacher-onboarding', 'student-onboarding']) {
  for (const accepted of [true, false]) test(`${page} ${accepted ? 'successful' : 'rejected'} submit survives a released event target`, async () => {
    const writes = [];
    const h = mount(`apps/web/src/app/${page}/page.tsx`, async (url, options = {}) => {
      if (options.method) { writes.push({ url, ...options }); return response({ id: 'created', message: 'Synthetic rejection' }, accepted); }
      if (url === '/api/academies') return response([{ id: 'a', name: 'Synthetic Academy' }]);
      if (url.endsWith('/academic-periods')) return response({ years: [], terms: [] });
      return response([]);
    });
    await h.flush();
    if (page === 'academic-periods') {
      nodes(h.render()).find(n => n.type === 'date-control' && n.props.name === 'year-start').props.onChange('2026-01-01');
      nodes(h.render()).find(n => n.type === 'date-control' && n.props.name === 'year-end').props.onChange('2026-12-31');
    }
    if (page === 'teacher-onboarding') {
      nodes(h.render()).find(n => n.type === 'input' && n.props['aria-label'] === 'Subject 1').props.onChange({ target: { value: 'Piano' } });
    }
    if (page === 'access-review') {
      nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === 'userId').props.onChange('staff');
      nodes(h.render()).find(n => n.type === 'input' && n.props.type === 'checkbox' && n.props.onChange).props.onChange({ target: { checked: true } });
    }
    let resets = 0;
    const form = { values: { name: 'Synthetic year', notes: 'Synthetic review', title: 'Synthetic action', subject: 'Synthetic support', firstName: 'Synthetic', lastName: 'Person', 'students.manage': 'on', reason: 'Synthetic access review' }, reset() { resets++; } };
    const event = { preventDefault() {}, currentTarget: form };
    const handler = nodes(h.render()).find(n => n.type === 'form').props.onSubmit;
    const completion = handler(event);
    event.currentTarget = null; // React releases currentTarget when dispatch returns.
    await completion; await h.flush();
    assert.equal(writes.length, 1);
    assert.equal(resets, accepted ? 1 : 0);
    if (page === 'teacher-onboarding' && accepted) assert.match(text(h.render()), /Teacher onboarded/);
    if (page === 'student-onboarding') assert.deepEqual(h.redirects, accepted ? ['/student-management?studentId=created&notice=student-created'] : []);
    h.unmount();
  });
}
