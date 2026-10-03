// Real TSX handlers with controlled hooks/API, not browser or database evidence.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const compile = file => ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: {
  module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
const pageCode = compile('apps/web/src/app/courses/page.tsx');
const nodes = (n, predicate) => Array.isArray(n) ? n.flatMap(x => nodes(x, predicate)) : !n || typeof n !== 'object' ? [] :
  [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)];
const text = n => Array.isArray(n) ? n.map(text).join('') : n == null ? '' : typeof n === 'object' ? text(n.props?.children) : String(n);
async function exercise({ type, populated = true, action = 'edit', failure = false, clearLevel = false }) {
  const row = { id: 'c', name: 'Synthetic course', courseCode: populated ? 'QA-C1' : null, academyType: type,
    subjectArea: populated ? 'QA subject' : null, level: populated ? 'Level1' : null, description: populated ? 'QA description' : null,
    durationMonths: populated ? 9 : null, weeklySessions: populated ? 2 : null, sessionMinutes: populated ? 45 : null,
    minimumAge: populated ? 0 : null, maximumAge: populated ? 18 : null, deliveryMode: populated ? 'Hybrid' : null,
    prerequisites: populated ? 'QA prerequisite text' : null, learningOutcomes: populated ? 'QA outcomes' : null,
    isPublished: populated, isActive: action !== 'reactivate' };
  const original = JSON.stringify(row), state = new Map(), refs = [], calls = [];
  const initial = [[{ id: 'a', name: 'QA academy' }], 'a', [row], '', 'Music', '', '', null, '', 'Music', '', null];
  let index = 0, refIndex = 0;
  function execute(code) {
    const module = { exports: {} };
    new Function('require','module','exports',code)(name => {
      if (name === 'react/jsx-runtime') return jsx;
      if (name === 'react') return { useState: value => { const key = index++; return [state.has(key) ? state.get(key) : initial[key] ?? value, value => state.set(key,value)]; },
        useRef: value => refs[refIndex++] ??= { current:value }, useEffect: () => {} };
      if (name === '@/components/workspace-nav') return { WorkspaceNav: 'nav' };
      if (name === '@/components/design-system/controls') return { StandardSelectField: 'select' };
      if (name === '@/lib/course-update') return execute(compile('apps/web/src/lib/course-update.ts'));
      if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
        calls.push({ url, ...options }); return { ok: !(failure && options.method), status: failure ? 400 : 200, json: async () => [row] };
      } };
      throw Error(name);
    },module,module.exports); return module.exports;
  }
  const component = execute(pageCode).default, render = () => { index = 0; refIndex = 0; return component(); };
  const click = label => { const b = nodes(render(), n => n.type === 'button' && text(n).trim() === label)[0]; assert.ok(b,label); b.props.onClick(); };
  if (action === 'edit') { click('Edit'); state.set(8,'Renamed synthetic'); if (clearLevel) state.set(10,''); click('Save'); }
  else click(action === 'reactivate' ? 'Reactivate' : 'Deactivate');
  await new Promise(resolve => setImmediate(resolve)); await new Promise(resolve => setImmediate(resolve));
  const writes = calls.filter(x => x.method); assert.equal(writes.length,1); assert.equal(writes[0].method,'PUT');
  assert.equal(writes[0].url,'/api/academies/a/courses/c'); assert.equal(JSON.stringify(row),original,'source record not mutated');
  if (failure) { assert.equal(calls.length,1); assert.equal(state.get(7),'c'); assert.equal(state.get(8),'Renamed synthetic'); assert.match(state.get(6),/could not be updated/); }
  else {
    const { id, ...expected } = row;
    if (action === 'edit') { expected.name = 'Renamed synthetic'; if (clearLevel) expected.level = null; }
    else expected.isActive = !row.isActive;
    assert.deepEqual(JSON.parse(writes[0].body),expected,'all replacement fields, including zero/null/false, retained');
    assert.equal(state.get(11),null);
    assert.ok(calls.some(x => !x.method));
  }
}
for (const type of ['Music','Tuition','Coaching']) {
  for (const populated of [true,false]) for (const action of ['edit','deactivate','reactivate'])
    test(`${type}/${populated ? 'populated' : 'null-optionals'}/${action}: complete payload preserves settings`, () => exercise({ type,populated,action }));
  test(`${type}: explicit visible level clear preserves hidden settings`, () => exercise({ type,clearLevel:true }));
  test(`${type}: rejected edit retains details and does not refresh`, () => exercise({ type,failure:true }));
}
