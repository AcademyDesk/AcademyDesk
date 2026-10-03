// Controlled actual TSX payload checks; not browser/SQL acceptance.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const compiled = ts.transpileModule(fs.readFileSync('apps/web/src/app/branches/page.tsx', 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
function nodes(n, predicate) {
  if (Array.isArray(n)) return n.flatMap(x => nodes(x, predicate));
  if (!n || typeof n !== 'object') return [];
  return [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)];
}
function text(n) { return Array.isArray(n) ? n.map(text).join('') : n == null ? '' : typeof n === 'object' ? text(n.props?.children) : String(n); }
for (const populated of [true, false]) for (const action of ['edit', 'deactivate', 'reactivate'])
  test(`${action} preserves ${populated ? 'populated' : 'null'} address/postcode in actual PUT payload`, async () => {
    const row = { id: 'b', name: 'Synthetic branch', city: 'Synthetic city', state: 'Synthetic state',
      addressLine1: populated ? '12 Synthetic street' : null, postalCode: populated ? '560001' : null, isActive: action !== 'reactivate' };
    const initial = [[{ id: 'a', name: 'Synthetic academy' }], 'a', [row], '', '', '', '', null, '', '', '', null];
    const state = new Map(), refs = [], calls = []; let index = 0, refIndex = 0;
    const module = { exports: {} };
    new Function('require', 'module', 'exports', compiled)(name => {
      if (name === 'react/jsx-runtime') return jsx;
      if (name === 'react') return {
        useState: value => { const key = index++; return [state.has(key) ? state.get(key) : initial[key] ?? value, value => state.set(key, value)]; },
        useRef: value => refs[refIndex++] ??= { current: value }, useEffect: () => {} };
      if (name === '@/components/design-system/controls') return { StandardSelectField: 'select' };
      if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
        calls.push({ url, ...options }); return { ok: true, status: 200, json: async () => [row] };
      } };
      throw Error(name);
    }, module, module.exports);
    const render = () => { index = 0; refIndex = 0; return module.exports.default(); };
    const click = label => nodes(render(), n => n.type === 'button' && text(n) === label)[0].props.onClick();
    if (action === 'edit') { click('Edit'); state.set(8, 'Renamed synthetic'); click('Save'); }
    else click(action === 'deactivate' ? 'Deactivate' : 'Reactivate');
    await new Promise(resolve => setImmediate(resolve));
    const writes = calls.filter(x => x.method); assert.equal(writes.length, 1);
    assert.equal(writes[0].url, '/api/academies/a/branches/b'); assert.equal(writes[0].method, 'PUT');
    assert.deepEqual(JSON.parse(writes[0].body), {
      name: action === 'edit' ? 'Renamed synthetic' : row.name, city: row.city, state: row.state,
      addressLine1: row.addressLine1, postalCode: row.postalCode,
      isActive: action === 'edit' ? row.isActive : !row.isActive });
  });
