// Actual TSX with controlled hooks/API. Not live browser/SQL acceptance.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
function compile(file) { return ts.transpileModule(fs.readFileSync(file, 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText; }
function nodes(n, predicate) { if (Array.isArray(n)) return n.flatMap(x => nodes(x, predicate)); if (!n || typeof n !== 'object') return [];
  return [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)]; }
function text(n) { return Array.isArray(n) ? n.map(text).join('') : n == null ? '' : typeof n === 'object' ? text(n.props?.children) : String(n); }
for (const mode of ['InPerson', 'Online', 'Hybrid', 'null-optionals']) for (const action of ['edit', 'deactivate', 'reactivate', 'assign'])
  test(`${mode}/${action}: complete replacement payload preserves unrelated settings`, async () => {
    const empty = mode === 'null-optionals', row = {
      id: 'b', name: 'Synthetic batch', batchCode: empty ? null : 'QA-B1', courseId: 'c', teacherId: 't', branchId: empty ? null : 'r',
      capacity: 12, waitlistCapacity: 7, deliveryMode: empty ? 'InPerson' : mode, classType: 'OneToOne', sessionMinutes: 45, sessionsPerWeek: 2,
      meetingDaysJson: empty ? null : '[{"Day":"Mon","StartTime":"10:30"}]', meetingLink: empty ? null : 'https://example.invalid/qa-meeting',
      meetingPattern: empty ? null : 'Mon 10:30', roomName: empty ? null : 'QA Room', enrollmentStatus: 'Closed', adminNotes: empty ? null : 'QA notes',
      startDate: empty ? null : '2026-10-01', endDate: empty ? null : '2027-01-01', isActive: action !== 'reactivate', activeEnrolments: 0 };
    const academy = { id: 'a', name: 'Synthetic academy' }, teachers = [{ id: 't', firstName: 'Synthetic', lastName: 'Teacher', isActive: true }];
    const assigning = action === 'assign', initial = assigning
      ? [academy, [], teachers, [row], [], null, 't2', 'b', '', false]
      : [[academy], 'a', [{ id: 'c', name: 'Synthetic course' }], teachers, [], [row], 'c'];
    const state = new Map(), calls = [], refs = []; let index = 0, refIndex = 0;
    function execute(file) {
      const module = { exports: {} };
      new Function('require', 'module', 'exports', compile(file))(name => {
        if (name === 'react/jsx-runtime') return jsx;
        if (name === 'react') return { useState: value => { const key = index++; return [state.has(key) ? state.get(key) : initial[key] === undefined ? value : initial[key], value => state.set(key, value)]; }, useRef: value => refs[refIndex++] ??= { current: value }, useEffect: () => {} };
        if (name === 'next/link') return { default: 'a' };
        if (name === 'next/navigation') return { usePathname: () => '/batch-setup' };
        if (name === '@/components/workspace-nav') return { WorkspaceNav: 'nav' };
        if (name.startsWith('@/components/design-system/')) return new Proxy({}, { get: (_, key) => key });
        if (name === '@/lib/batch-update') return execute('apps/web/src/lib/batch-update.ts');
        if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
          calls.push({ url, ...options }); return { ok: true, status: 200, json: async () => options.method ? row :
            url.endsWith('/batches') ? [row] : url.endsWith('/teachers') ? teachers : url.endsWith('/courses') ? initial[2] : [] };
        } };
        throw Error(name);
      }, module, module.exports); return module.exports;
    }
    const component = execute(assigning ? 'apps/web/src/app/teachers/page.tsx' : 'apps/web/src/app/batches/page.tsx').default;
    const render = () => { index = 0; refIndex = 0; return component(); };
    const click = label => { const button = nodes(render(), n => n.type === 'button' && text(n).trim() === label)[0]; assert.ok(button, label); button.props.onClick(); };
    if (action === 'edit') { click('Edit'); state.set(24, 'Renamed synthetic'); click('Save'); }
    else click(assigning ? 'Save' : action === 'deactivate' ? 'Deactivate' : 'Reactivate');
    await new Promise(resolve => setImmediate(resolve));
    const writes = calls.filter(x => x.method); assert.equal(writes.length, 1); assert.equal(writes[0].method, 'PUT');
    assert.equal(writes[0].url, '/api/academies/a/batches/b');
    const { id, activeEnrolments, ...expected } = row;
    if (action === 'edit') expected.name = 'Renamed synthetic';
    if (action === 'deactivate' || action === 'reactivate') expected.isActive = !row.isActive;
    if (assigning) expected.teacherId = 't2';
    assert.deepEqual(JSON.parse(writes[0].body), expected);
  });
