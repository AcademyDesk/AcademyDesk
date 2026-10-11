// Actual MusicPage handler with synthetic hooks/transport; not SQL/browser proof.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs'), cp = require('node:child_process');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const file = 'apps/web/src/app/music/page.tsx';
const source = process.env.QA_SOURCE_REF ? cp.execFileSync('git', ['show', process.env.QA_SOURCE_REF + ':' + file], { encoding: 'utf8' }) : fs.readFileSync(file, 'utf8');
const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
async function fixture(score, rejected) {
  let index = 0, stored = { id: 'progress', studentId: 'student', musicPieceId: 'piece', status: 'Assigned', score, targetDate: '2026-10-20', notes: 'Retain notes' };
  const state = [], effects = [], calls = [], module = { exports: {} }, original = { ...stored };
  const react = { useState: initial => { const k = index++; if (!(k in state)) state[k] = initial; return [state[k], value => { state[k] = value; }]; }, useEffect: fn => effects.push(fn) };
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) { assert.equal(init.method, 'PATCH'); if (!rejected) stored = { ...stored, ...JSON.parse(init.body) }; return { ok: !rejected, status: rejected ? 403 : 200 }; }
    const data = url === '/api/academies' ? [{ id: 'owned' }] : url.endsWith('/students') ? [{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }] : url.endsWith('/music-pieces') ? [{ id: 'piece', title: 'Synthetic piece', difficulty: 'Beginner' }] : [{ ...stored }];
    return { ok: true, json: async () => data };
  };
  new Function('require', 'module', 'exports', code)(name => name === 'react' ? react : name === 'react/jsx-runtime' ? jsx : name === '@/lib/api' ? { academyApi: api, apiHeaders: () => ({ 'Content-Type': 'application/json' }) } : name === '@/components/workspace-nav' ? { WorkspaceNav: () => null } : name === '@/components/design-system/controls' ? { StandardSelectField: () => null, StandardDateField: () => null } : (() => { throw Error(name); })(), module, module.exports);
  const render = () => { index = 0; return module.exports.default(); };
  render(); effects.splice(0).forEach(fn => fn()); await new Promise(resolve => setImmediate(resolve));
  return { calls, original, read: () => stored, render, change: async status => { nodes(render()).find(n => n.props?.name === 'progress-progress').props.onChange(status); await new Promise(resolve => setImmediate(resolve)); } };
}
for (const score of [0, 1, 85.5, null, undefined]) for (const status of ['Learning', 'Mastered']) for (const rejected of [false, true]) {
  test(`status-only preserves score ${score}, ${status}, ${rejected ? 'denied' : 'saved'}`, async () => {
    const f = await fixture(score, rejected); await f.change(status);
    const writes = f.calls.filter(x => x.method); assert.equal(writes.length, 1); assert.equal(writes[0].url, '/api/academies/owned/music-progress/progress');
    assert.deepEqual(JSON.parse(writes[0].body), { status, targetDate: '2026-10-20', score: score ?? null, notes: 'Retain notes' });
    if (rejected) { assert.deepEqual(f.read(), f.original); assert.ok(nodes(f.render()).some(n => n.props?.children === 'Progress could not be updated.')); assert.equal(f.calls.length, 5); }
    else { assert.equal(f.read().score, score ?? null); assert.equal(f.read().status, status); assert.equal(f.calls.length, 8); }
  });
}
