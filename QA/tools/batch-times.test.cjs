// Actual create-page handler with controlled hooks/API; not browser or live API acceptance.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
function nodes(n, predicate) { if (Array.isArray(n)) return n.flatMap(x => nodes(x, predicate)); if (!n || typeof n !== 'object') return [];
  return [...(predicate(n) ? [n] : []), ...nodes(n.props?.children, predicate)]; }
for (const mode of ['InPerson', 'Online', 'Hybrid']) for (const populated of [false, true])
  test(`${mode}/${populated ? 'selected-days' : 'optional-no-days'}: actual create schedule payload`, async () => {
    const academy = { id: 'a', name: 'Synthetic academy' }, course = { id: 'c', name: 'Synthetic course' };
    const initial = [[academy], 'a', [course], [], [], [], 'c', '', 'Synthetic batch', '10', '', '0', mode, 'Group', '60', '1',
      mode === 'InPerson' ? '' : 'https://example.invalid/qa', populated ? ['Mon', 'Fri'] : [], populated ? { Mon: '09:00', Fri: '17:45' } : {}, '', 'Open', ''];
    const calls = [], state = new Map(); let index = 0;
    function execute(file) {
      const module = { exports: {} }, code = ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: {
        module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
      new Function('require', 'module', 'exports', code)(name => {
        if (name === 'react/jsx-runtime') return jsx;
        if (name === 'react') return { useState: value => { const key = index++; return [state.has(key) ? state.get(key) : initial[key] === undefined ? value : initial[key], value => state.set(key, value)]; }, useRef: value => ({ current: value }), useEffect: () => {} };
        if (name === 'next/link') return { default: 'a' };
        if (name === 'next/navigation') return { usePathname: () => '/batch-setup' };
        if (name === '@/components/workspace-nav') return { WorkspaceNav: 'nav' };
        if (name.startsWith('@/components/design-system/')) return new Proxy({}, { get: (_, key) => key });
        if (name === '@/lib/batch-update') return execute('apps/web/src/lib/batch-update.ts');
        if (name === '@/lib/api') return { apiHeaders: () => ({}), academyApi: async (url, options = {}) => {
          calls.push({ url, ...options }); return { ok: true, status: options.method ? 201 : 200, json: async () => url.endsWith('/courses') ? [course] : [] };
        } };
        throw Error(name);
      }, module, module.exports); return module.exports;
    }
    const page = execute('apps/web/src/app/batches/page.tsx').default();
    const form = nodes(page, n => n.type === 'form' && n.props.onSubmit)[0]; assert.ok(form);
    let prevented = false; await form.props.onSubmit({ preventDefault() { prevented = true; } }); assert.equal(prevented, true);
    const writes = calls.filter(x => x.method); assert.equal(writes.length, 1); assert.equal(writes[0].method, 'POST');
    assert.equal(writes[0].url, '/api/academies/a/batches'); const body = JSON.parse(writes[0].body);
    assert.equal(body.meetingDaysJson, populated ? '[{"day":"Mon","startTime":"09:00"},{"day":"Fri","startTime":"17:45"}]' : null);
    assert.equal(body.meetingPattern, populated ? 'Mon 09:00 · Fri 17:45' : null);
    assert.equal(body.deliveryMode, mode); assert.equal(body.meetingLink, initial[16] || null);
    assert.equal(body.teacherId, null); assert.equal(body.branchId, null); assert.equal(body.endDate, null);
    assert.equal(calls.filter(x => !x.method).length, 4, 'existing post-save workspace refresh retained');
  });
