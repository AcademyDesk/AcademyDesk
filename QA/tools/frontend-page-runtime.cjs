// Actual page/component code with controlled hooks and synthetic delayed responses.
// This verifies state/transport behavior, not browser rendering or SQL persistence.
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
    if (relative === 'apps/web/src/app/teacher/page.tsx') source += '\nexport { TeacherAttendanceRoster, TeacherClassroom, TeacherTasks, TeacherSelfService };';
    if (relative === 'apps/web/src/app/portal/page.tsx') source += '\nexport { Action };';
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
      if (name === '@/lib/use-record-editor') return execute('apps/web/src/lib/use-record-editor.ts');
      if (name === '@/lib/use-clock') return { useClock: () => Date.parse('2026-10-28T12:00:00Z') };
      if (name === '@/components/theme-toggle') return { ThemeToggle: 'theme-toggle' };
      if (name === '@/components/teacher-material-uploader') return { TeacherMaterialUploader: 'uploader' };
      if (name === '@/components/private-material-actions') return { PrivateMaterialActions: 'material-actions' };
      if (name === '@/components/workspace-nav') return { WorkspaceNav: 'nav' };
      if (name === '@/components/student-fee-arrangements') return { StudentFeeArrangements: 'fee-arrangements' };
      if (name === '@/components/student-admin-profile') return { StudentAdminProfile: 'student-profile-editor' };
      if (name === '@/components/standard-date-field') return { StandardDateField: 'date-control' };
      if (name === '@/components/standard-select-field') return { StandardSelectField: 'select-control' };
      if (name === '@/components/design-system/controls') return { StandardSelectField: 'select-control', StandardDateField: 'date-control', StandardTimeField: 'time-control', StandardDetailModal: 'detail-modal', StandardInteractiveTile: 'tile' };
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

module.exports = { mount, nodes, text, response, deferred };
