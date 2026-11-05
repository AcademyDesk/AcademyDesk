'use strict';
// Exercise the recovered session adapter against the current shared API helper.
const { test } = require('node:test');
const a = require('node:assert/strict'), fs = require('node:fs'), vm = require('node:vm');
const ts = require('../../apps/web/node_modules/typescript');
function setup() {
  const window = new EventTarget(), values = new Map(), modules = new Map();
  window.location = { pathname: '/penta' };
  window.localStorage = { getItem: key => values.get(key) ?? null, setItem: (key, value) => values.set(key, value), removeItem: key => values.delete(key) };
  function load(file) {
    if (modules.has(file)) return modules.get(file).exports;
    const module = { exports: {} }; modules.set(file, module);
    const code = ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
    vm.runInNewContext(code, { module, exports: module.exports, window, CustomEvent, AbortController, crypto: require("node:crypto").webcrypto, process: { env: {} },
      require(name) { a.equal(name, './api'); return load('apps/web/src/lib/api.ts'); }, fetch() { throw Error('Unexpected network'); } });
    return module.exports;
  }
  return { window, api: load('apps/web/src/lib/api.ts'), penta: load('apps/web/src/lib/penta-session.ts') };
}
test('identical-token login replacement advances the observed epoch and emits no credentials', () => {
  const h = setup(), lifetime = new AbortController(), events = [];
  h.api.savePortalTokens('AcademyAdmin', 'synthetic-access', 'synthetic-refresh');
  const before = h.penta.portalSessionEpoch('AcademyAdmin', lifetime.signal);
  h.window.addEventListener(h.penta.portalSessionChangedEvent, event => events.push(event.detail));
  h.api.savePortalTokens('AcademyAdmin', 'synthetic-access', 'synthetic-refresh');
  a.notEqual(h.penta.portalSessionEpoch('AcademyAdmin'), before);
  a.deepEqual(events, ['AcademyAdmin']);
  lifetime.abort();
});
test('multiple views share one observer, preserve other workspaces, and release it on final abort', () => {
  const h = setup(), first = new AbortController(), second = new AbortController(), events = [];
  const before = h.penta.portalSessionEpoch('AcademyAdmin', first.signal);
  h.penta.portalSessionEpoch('AcademyAdmin', first.signal);
  h.penta.portalSessionEpoch('AcademyAdmin', second.signal);
  h.window.addEventListener(h.penta.portalSessionChangedEvent, event => events.push(event.detail));
  h.api.savePortalTokens('Teacher', 'synthetic-other');
  a.equal(h.penta.portalSessionEpoch('AcademyAdmin'), before);
  first.abort(); h.api.clearPortalTokens('AcademyAdmin');
  a.deepEqual(events, ['AcademyAdmin']);
  const active = h.penta.portalSessionEpoch('AcademyAdmin');
  second.abort(); h.api.savePortalTokens('AcademyAdmin', 'synthetic-next');
  a.notEqual(h.penta.portalSessionEpoch('AcademyAdmin'), active);
  a.deepEqual(events, ['AcademyAdmin']);
});
test('aborted lifetimes never retain an observer', () => {
  const h = setup(), lifetime = new AbortController(); lifetime.abort();
  const before = h.penta.portalSessionEpoch('AcademyAdmin', lifetime.signal);
  h.api.savePortalTokens('AcademyAdmin', 'synthetic');
  a.notEqual(h.penta.portalSessionEpoch('AcademyAdmin'), before);
});
