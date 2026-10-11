// Runs the real TeacherTasks TSX handler with native FormData and synthetic transport.
// Not browser, HTTP model-binding, SQL persistence or physical-device acceptance.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const cp = require('node:child_process');
const ts = require('../../apps/web/node_modules/typescript');
const jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const path = 'apps/web/src/app/teacher/page.tsx';
const source = process.env.QA_SOURCE_REF
  ? cp.execFileSync('git', ['show', `${process.env.QA_SOURCE_REF}:${path}`], { encoding: 'utf8' })
  : fs.readFileSync(path, 'utf8');
// Return the otherwise-discarded create promise to observe faults without changing its body.
const code = ts.transpileModule(source.replaceAll('onSubmit={(e) => void create(', 'onSubmit={(e) => create(') + '\nexport { TeacherTasks as QaTeacherTasks };', {
  compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX },
}).outputText;
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];

function page(fields, ok = true) {
  let index = 0, resets = 0;
  const state = [], calls = [], module = { exports: {} };
  const form = { reset() { resets++; } };
  const event = { currentTarget: form, preventDefault() {} };
  const react = {
    useState(value) { const slot = index++; if (!(slot in state)) state[slot] = value; return [state[slot], next => { state[slot] = typeof next === 'function' ? next(state[slot]) : next; }]; },
    useEffect() {},
  };
  const api = async (url, init) => {
    calls.push({ url, ...init });
    // React's event.currentTarget is no longer available after dispatch returns.
    event.currentTarget = null;
    await new Promise(resolve => setImmediate(resolve));
    return { ok };
  };
  function FormDataFromFixture(actual) {
    assert.equal(actual, form);
    const data = new FormData();
    for (const [name, value] of Object.entries(fields)) data.set(name, value);
    return data;
  }
  new Function('require', 'module', 'exports', 'FormData', code)(name => {
    if (name === 'react/jsx-runtime') return jsx;
    if (name === 'react') return react;
    if (name === '@/lib/api') return { academyApi: api, apiHeaders: () => ({ 'Content-Type': 'application/json' }) };
    if (name === 'next/navigation' || name.startsWith('@/components/') || name === '@/lib/teacher-workspace') return {};
    throw new Error(`Unexpected import: ${name}`);
  }, module, module.exports, FormDataFromFixture);
  const render = () => { index = 0; return module.exports.QaTeacherTasks({ batches: [{ id: 'owned', name: 'Synthetic batch' }] }); };
  async function submit(kind) {
    const formIndex = { assignments: 0, 'lesson-plans': 1, assessments: 2 }[kind];
    try { await nodes(render()).filter(n => n.type === 'form')[formIndex].props.onSubmit(event); }
    catch (error) { return error; }
  }
  return { render, submit, state, calls, resets: () => resets };
}

// Assertions inspect the payload and successful form-reset contract independently.
for (const kind of ['assignments', 'assessments']) for (const published of ['true', 'false']) {
  test(`${kind}: ${published} is a JSON boolean, with confirmed reset`, async () => {
    const fields = { batchId: 'owned', title: 'Synthetic title', type: kind === 'assignments' ? 'Homework' : 'Performance', isPublished: published };
    if (kind === 'assignments') fields.description = '';
    else { fields.maxScore = '12.5'; fields.scheduledAtUtc = '2026-10-11T13:30'; }
    const p = page(fields); await p.submit(kind);
    assert.equal(p.calls.length, 1); assert.equal(p.calls[0].url, `/api/teacher/${kind}`);
    assert.equal(p.calls[0].method, 'POST');
    assert.deepEqual(JSON.parse(p.calls[0].body), { ...fields, isPublished: published === 'true', ...(kind === 'assessments' ? { maxScore: 12.5, scheduledAtUtc: new Date(fields.scheduledAtUtc).toISOString() } : {}) });
    assert.equal(p.resets(), 1); assert.equal(p.state[0], 'Saved for your assigned batch.');
  });
}
test('Assessment optional blank date serializes null, not empty string', async () => {
  const p = page({ batchId: 'owned', title: 'Synthetic', maxScore: '10', scheduledAtUtc: '', isPublished: 'false' });
  await p.submit('assessments'); assert.equal(JSON.parse(p.calls[0].body).scheduledAtUtc, null);
});
test('Lesson plan retains its original payload and confirmed reset', async () => {
  const fields = { batchId: 'owned', title: 'Synthetic lesson', objectives: '' };
  const p = page(fields); await p.submit('lesson-plans'); assert.deepEqual(JSON.parse(p.calls[0].body), fields); assert.equal(p.resets(), 1);
});
for (const kind of ['assignments', 'assessments', 'lesson-plans']) {
  test(`${kind}: missing batch writes nothing and retains form`, async () => {
    const p = page({ batchId: '', title: 'Synthetic' }); await p.submit(kind);
    assert.equal(p.calls.length, 0); assert.equal(p.resets(), 0); assert.equal(p.state[0], 'Select an assigned batch before saving.');
  });
  test(`${kind}: rejected response does not reset draft or report success`, async () => {
    const p = page({ batchId: 'owned', title: 'Synthetic', isPublished: 'false' }, false); await p.submit(kind);
    assert.equal(p.calls.length, 1); assert.equal(p.resets(), 0); assert.equal(p.state[0], 'The teaching record could not be saved.');
  });
}
