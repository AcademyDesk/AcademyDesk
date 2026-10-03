// Execute the real password-change handler with controlled transport and form.
// These checks cover handler behavior, not physical browser acceptance.
const fs = require('node:fs');
const assert = require('node:assert/strict');
const { test } = require('node:test');
const ts = require('../../apps/web/node_modules/typescript');
const page = fs.readFileSync('apps/web/src/app/platform/control/page.tsx', 'utf8');
const ast = ts.createSourceFile('page.tsx', page, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
let handler;
function visit(node) {
  if (ts.isFunctionDeclaration(node) && node.name?.text === 'changeOwnerPassword') handler = node.getText(ast);
  ts.forEachChild(node, visit);
}
visit(ast);
assert.ok(handler, 'actual password-change handler must exist');
const code = ts.transpileModule(handler, { compilerOptions: { target: ts.ScriptTarget.ES2022 } }).outputText;
async function run(config = {}) {
  const calls = [], messages = [], busy = [], clears = [], redirects = [];
  let resets = 0;
  const form = { values: { currentPassword: 'synthetic-old', newPassword: 'synthetic-new', confirmPassword: config.mismatch ? 'different' : 'synthetic-new' }, reset() { resets++; } };
  const event = { currentTarget: form, preventDefault() {} };
  const api = async (url, init) => {
    calls.push({ url, init });
    // React clears currentTarget after the synchronous event handler returns.
    event.currentTarget = null;
    await Promise.resolve();
    if (config.network) throw Error('Connection lost.');
    return { ok: !config.denied, json: async () => ({ message: config.denied ? 'Current password incorrect.' : 'Password changed.' }) };
  };
  const fn = new Function('academyApi', 'apiHeaders', 'FormData', 'setBusy', 'setMessage', 'clearPortalTokens', 'router', code + '\nreturn changeOwnerPassword;')(
    api, () => ({ 'Content-Type': 'application/json' }), class { constructor(x) { this.form = x; } get(k) { return this.form.values[k]; } },
    x => busy.push(x), x => messages.push(x), x => clears.push(x), { replace: x => redirects.push(x) });
  await fn(event);
  return { calls, messages, busy, clears, redirects, resets };
}
test('successful change clears only Platform, resets captured form and routes to new-password sign-in without protected reload', async () => {
  const r = await run();
  assert.equal(r.calls.length, 1);
  assert.equal(r.calls[0].url, '/api/auth/session/change-password');
  assert.deepEqual(JSON.parse(r.calls[0].init.body), { currentPassword: 'synthetic-old', newPassword: 'synthetic-new' });
  assert.deepEqual(r.clears, ['Platform']);
  assert.deepEqual(r.redirects, ['/login?returnTo=/platform/control&passwordChanged=1']);
  assert.equal(r.resets, 1);
  assert.deepEqual(r.busy, [true, false]);
});
for (const config of [{ denied: true }, { network: true }]) test('failure preserves form and session ' + JSON.stringify(config), async () => {
  const r = await run(config);
  assert.equal(r.resets, 0); assert.deepEqual(r.clears, []); assert.deepEqual(r.redirects, []);
  assert.match(r.messages.at(-1), config.denied ? /Current password incorrect/ : /Connection lost/);
  assert.deepEqual(r.busy, [true, false]);
});
test('confirmation mismatch makes no request or session change', async () => {
  const r = await run({ mismatch: true });
  assert.deepEqual(r.calls, []); assert.deepEqual(r.clears, []); assert.equal(r.resets, 0);
  assert.match(r.messages.at(-1), /must match/);
});
test('login renders an accessible new-password notice from the redirect', () => {
  const jsx = require('../../apps/web/node_modules/react/jsx-runtime');
  const source = fs.readFileSync('apps/web/src/app/login/page.tsx', 'utf8');
  const output = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  const mod = { exports: {} };
  new Function('require', 'module', 'exports', output)(name => name === 'react/jsx-runtime' ? jsx : name === 'react' ? { useState: x => [x, () => {}] } : name === 'next/navigation' ? { useRouter: () => ({}), useSearchParams: () => new URLSearchParams('passwordChanged=1') } : {}, mod, mod.exports);
  const nodes = x => !x || typeof x !== 'object' ? [] : Array.isArray(x) ? x.flatMap(nodes) : [x, ...nodes(x.props?.children)];
  const notice = nodes(mod.exports.default()).find(x => x.props?.role === 'status');
  assert.ok(notice); assert.equal(notice.props.children, 'Password changed. Sign in with your new password.');
});
