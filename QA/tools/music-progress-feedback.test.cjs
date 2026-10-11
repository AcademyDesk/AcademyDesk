// Actual MusicPage handlers with controlled transport/hooks; not SQL or physical-device acceptance.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs'), cp = require('node:child_process');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const tick = () => new Promise(r => setImmediate(r));
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
const gate = () => { let release; const promise = new Promise(r => release = r); return { promise, release }; };
const file = 'apps/web/src/app/music/page.tsx';
const source = process.env.QA_SOURCE_REF ? cp.execFileSync('git', ['show', process.env.QA_SOURCE_REF + ':' + file], { encoding: 'utf8' }) : fs.readFileSync(file, 'utf8');
const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
async function fixture(config = {}) {
  let si = 0, ri = 0, ei = 0, reads = 0, changes = 0;
  const state = [], refs = [], deps = [], effects = [], cleanups = [], calls = [], module = { exports: {} };
  let stored = { id: 'progress', studentId: 'student', musicPieceId: 'piece', status: 'Assigned', score: 0, notes: 'Keep notes', targetDate: '2026-10-20' };
  const original = { ...stored };
  const react = {
    useState: initial => { const k = si++; if (!(k in state)) state[k] = initial; return [state[k], v => { changes++; state[k] = typeof v === 'function' ? v(state[k]) : v; }]; },
    useRef: initial => { const k = ri++; return refs[k] ?? (refs[k] = { current: initial }); },
    useEffect: (fn, next) => { const k = ei++; if (!deps[k] || next.some((v, i) => v !== deps[k][i])) { deps[k] = next; effects.push(fn); } },
  };
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) {
      if (config.writeGate) await config.writeGate;
      if (config.network) throw Error('Synthetic unknown outcome');
      if (!config.status && init.method === 'PATCH') stored = { ...stored, ...JSON.parse(init.body) };
      return { ok: !config.status, status: config.status || 200 };
    }
    const n = ++reads, after = calls.some(c => c.method);
    const data = url === '/api/academies' ? (config.empty ? [] : [{ id: 'owned' }])
      : url.endsWith('/students') ? [{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }]
        : url.endsWith('/music-pieces') ? [{ id: 'piece', title: 'Synthetic piece', difficulty: 'Beginner' }] : [{ ...stored }];
    if (!after && config.initialGate && n === 1) await config.initialGate;
    if (after && config.readGate) await config.readGate;
    if (config.getNetwork) throw Error('Synthetic read failure');
    return { ok: !(config.initialFailure || (after && config.refreshFailure)), status: config.initialFailure ? 403 : 200,
      json: async () => config.malformed ? {} : data };
  };
  new Function('require', 'module', 'exports', code)(n => n === 'react' ? react : n === 'react/jsx-runtime' ? jsx
    : n === '@/lib/api' ? { academyApi: api, apiHeaders: () => ({}) }
      : n === '@/components/workspace-nav' ? { WorkspaceNav: 'nav' }
        : n === '@/components/design-system/controls' ? { StandardSelectField: 'select', StandardDateField: 'date' }
          : (() => { throw Error(n); })(), module, module.exports);
  const render = () => { si = ri = ei = 0; return module.exports.default(); };
  const settle = async () => { for (let i = 0; i < 4; i++) { for (const fn of effects.splice(0)) cleanups.push(fn()); await tick(); render(); } };
  const field = (key, value) => {
    const tree = nodes(render()), node = tree.find(n => n.props?.placeholder === key || n.props?.name === key);
    assert.ok(node, key); node.props.onChange(node.type === 'input' ? { target: { value } } : value);
  };
  render(); await settle(); field('Piece title', ' Draft title '); field('Optional composer', ' Draft composer '); field('target', '2026-11-02');
  const handler = kind => { const tree = nodes(render()); if (kind === 'update') { const h = tree.find(n => n.props?.name === 'progress-progress')?.props.onChange; assert.ok(h); return () => h('Mastered'); }
    if (kind === 'refresh') return tree.find(n => n.type === 'button' && /Refresh/.test(n.props.children)).props.onClick;
    return () => tree.filter(n => n.type === 'form')[kind === 'piece' ? 0 : 1].props.onSubmit({ preventDefault() {} }); };
  const notice = () => nodes(render()).find(n => n.props?.role === 'status')?.props.children;
  const blocked = () => nodes(render()).filter(n => n.type === 'fieldset' || n.type === 'select').every(n => n.props.disabled);
  const drafts = () => nodes(render()).filter(n => n.type === 'input' && ['Piece title', 'Optional composer'].includes(n.props.placeholder)).map(n => n.props.value).concat(nodes(render()).find(n => n.props?.name === 'target').props.value);
  return { config, calls, field, handler, render, settle, notice, blocked, drafts, original, stored: () => stored, writes: () => calls.filter(c => c.method), changes: () => changes, unmount: () => cleanups.forEach(fn => fn?.()) };
}
const success = { piece: 'Repertoire piece added.', assign: 'Piece assigned to student.', update: 'Progress updated to Mastered.' };
for (const kind of ['piece', 'assign', 'update']) {
  test(kind + ' confirmed success is durable, polite and exact payload preserved', async () => {
    const f = await fixture(); await f.handler(kind)(); await f.settle(); assert.equal(f.notice(), success[kind]);
    const write = f.writes()[0]; assert.equal(write.method, kind === 'update' ? 'PATCH' : 'POST');
    assert.equal(write.url, '/api/academies/owned/' + (kind === 'piece' ? 'music-pieces' : 'music-progress' + (kind === 'update' ? '/progress' : '')));
    assert.deepEqual(JSON.parse(write.body), kind === 'piece' ? { title: ' Draft title ', composer: ' Draft composer ', instrument: 'Piano', genre: null, difficulty: 'Beginner', durationMinutes: null }
      : kind === 'assign' ? { studentId: 'student', musicPieceId: 'piece', targetDate: '2026-11-02', notes: null }
        : { status: 'Mastered', targetDate: '2026-10-20', score: 0, notes: 'Keep notes' });
    assert.deepEqual(f.drafts(), kind === 'piece' ? ['', '', '2026-11-02'] : kind === 'assign' ? [' Draft title ', ' Draft composer ', ''] : [' Draft title ', ' Draft composer ', '2026-11-02']);
    assert.equal(nodes(f.render()).find(n => n.props?.role === 'status').props['aria-live'], 'polite'); assert.equal(f.blocked(), false);
  });
  test(kind + ' confirmed write / failed refresh does not become save failure', async () => {
    const f = await fixture({ refreshFailure: true }); await f.handler(kind)(); await f.settle(); assert.ok(f.notice().startsWith(success[kind])); assert.match(f.notice(), /do not repeat.*before saving/); assert.equal(f.blocked(), true); assert.equal(f.writes().length, 1);
    f.handler('piece')(); f.handler('assign')(); f.handler('update')(); await f.settle(); assert.equal(f.writes().length, 1);
    f.config.refreshFailure = false; f.handler('refresh')(); await f.settle(); assert.equal(f.blocked(), false); assert.match(f.notice(), /refreshed/);
  });
  for (const status of [400, 403, 409, 429, 500, 503, 'network']) test(kind + ' ' + status + ' retained draft, truthful outcome, no automatic retry', async () => {
    const f = await fixture(status === 'network' ? { network: true } : { status }); const drafts = f.drafts(); await f.handler(kind)(); await f.settle();
    assert.deepEqual(f.drafts(), drafts); assert.equal(f.writes().length, 1); assert.deepEqual(f.stored(), f.original); assert.ok(f.notice());
    assert.equal(f.calls.filter(c => !c.method).length, 4);
    if (status === 'network' || status >= 500) { assert.match(f.notice(), /could not be confirmed.*Refresh.*before retrying/); assert.equal(f.blocked(), true); }
    else { assert.doesNotMatch(f.notice(), /added\.|assigned to student\.|updated to/); assert.equal(f.blocked(), false); }
    if (kind === 'assign' && status === 409) assert.equal(f.notice(), 'This piece is already assigned to this student.');
  });
  for (const stage of ['write', 'readback']) test(kind + ' ' + stage + ' synchronous duplicate/opposing/refresh handlers blocked', async () => {
    const g = gate(), f = await fixture(stage === 'write' ? { writeGate: g.promise } : { readGate: g.promise }); const old = ['piece', 'assign', 'update'].map(k => f.handler(k));
    const first = f.handler(kind)(); await tick(); try { assert.equal(f.blocked(), true); assert.ok(f.notice()); for (const h of old) h(); f.handler('refresh')(); await tick(); assert.equal(f.writes().length, 1); }
    finally { g.release(); } await first; await f.settle(); assert.equal(f.writes().length, 1); assert.equal(f.blocked(), false);
  });
  for (const stage of ['write', 'readback']) test(kind + ' ' + stage + ' late result after unmount has no page effects', async () => {
    const g = gate(), f = await fixture(stage === 'write' ? { writeGate: g.promise } : { readGate: g.promise }); const first = f.handler(kind)(); await tick(); f.unmount(); const changes = f.changes(), reads = f.calls.filter(c => !c.method).length;
    g.release(); await first; await f.settle(); assert.equal(f.changes(), changes); assert.equal(f.calls.filter(c => !c.method).length, reads);
  });
  test(kind + ' changed draft is not erased by delayed confirmed save', async () => {
    const g = gate(), f = await fixture({ writeGate: g.promise }); const first = f.handler(kind)(); f.field('Piece title', 'New title'); f.field('Optional composer', 'New composer'); f.field('target', '2026-12-03');
    g.release(); await first; await f.settle(); assert.deepEqual(f.drafts(), ['New title', 'New composer', '2026-12-03']);
  });
}
for (const config of [{ empty: true }, { initialFailure: true }, { malformed: true }, { getNetwork: true }]) test('unchecked initial workspace cannot write ' + JSON.stringify(config), async () => {
  const f = await fixture(config); assert.equal(f.blocked(), true); assert.equal(f.calls.some(c => c.method), false); f.handler('piece')(); f.handler('assign')(); await f.settle(); assert.equal(f.writes().length, 0);
  assert.match(f.notice(), /could not be loaded/); Object.assign(f.config, { empty: false, initialFailure: false, malformed: false, getNetwork: false }); f.handler('refresh')(); await f.settle(); assert.equal(f.blocked(), false);
});
test('stale captured row handler cannot overwrite a newer local readback', async () => { const f = await fixture(), old = f.handler('update'); old(); await f.settle(); assert.equal(f.stored().status, 'Mastered'); old(); await f.settle(); assert.equal(f.writes().length, 1); });
test('initial read superseded by explicit refresh cannot erase newer feedback or data', async () => { const g = gate(), f = await fixture({ initialGate: g.promise }); f.handler('refresh')(); await f.settle(); assert.equal(f.blocked(), false); const changes = f.changes(), notice = f.notice(); g.release(); await f.settle(); assert.equal(f.changes(), changes); assert.equal(f.notice(), notice); });
test('initial request after unmount neither commits nor issues followup reads', async () => { const g = gate(), f = await fixture({ initialGate: g.promise }); f.unmount(); const changes = f.changes(), reads = f.calls.length; g.release(); await f.settle(); assert.equal(f.changes(), changes); assert.equal(f.calls.length, reads); });
test('malformed confirmed readback blocks writes until an explicit fresh recovery', async () => { const f = await fixture(); f.config.malformed = true; f.handler('update')(); await f.settle(); assert.match(f.notice(), /updated to Mastered.*do not repeat/); assert.equal(f.blocked(), true); f.config.malformed = false; f.handler('refresh')(); await f.settle(); assert.equal(f.blocked(), false); });
test('every read is uncached; repeated renders do not refetch', async () => { const f = await fixture(); await f.settle(); assert.equal(f.calls.length, 4); f.handler('update')(); await f.settle(); assert.equal(f.calls.length, 8); assert.ok(f.calls.filter(c => !c.method).every(c => c.cache === 'no-store')); });
