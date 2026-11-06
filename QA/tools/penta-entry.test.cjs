// Current routing/login handlers in a VM with synthetic fetch/storage only; no browser/SQL/model proof.
const fs = require('node:fs'), vm = require('node:vm'), assert = require('node:assert/strict'), { test } = require('node:test');
const ts = require('../../apps/web/node_modules/typescript');
const compile = file => ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX } }).outputText;
const exportsFor = (file, requireFn = require) => { const module = { exports: {} }; vm.runInNewContext(compile(file), { module, exports: module.exports, require: requireFn, URL }); return module.exports; };
const entry = exportsFor('apps/web/src/lib/workspace-entry.ts');
const accounts = [
  [{ workspace: 'AcademyAdmin', roles: ['Owner'], isPlatformOwner: false }, '/penta'],
  [{ workspace: 'AcademyAdmin', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/penta'],
  [{ workspace: 'AcademyAdmin', roles: ['FinanceUser'], isPlatformOwner: false }, '/dashboard'],
  [{ workspace: 'Platform', roles: ['PlatformOwner'], isPlatformOwner: true }, '/platform/ai'],
  [{ workspace: 'Teacher', roles: ['Teacher'], isPlatformOwner: false }, '/teacher/ai'],
  [{ workspace: 'Portal', roles: ['Student'], isPlatformOwner: false }, '/portal/ai'],
  [{ workspace: 'Portal', roles: ['Guardian'], isPlatformOwner: false }, '/portal/ai'],
];
for (const [account, landing] of accounts) {
  test(account.roles[0] + ' scoped landing and explicit Manual return', () => {
    assert.equal(entry.validWorkspaceAccount(account), true);
    assert.equal(entry.workspaceLanding(account), landing);
    const manual = entry.workspaceHomes[account.workspace].manual;
    assert.equal(entry.workspaceLanding(account, manual + '?tab=active#current'), manual + '?tab=active#current');
  });
  for (const bad of ['//evil.invalid', '/\\evil.invalid', 'https://evil.invalid', '/%70latform', '/%2f%2fevil.invalid', '/login', '/register', '/teacher\n/ai'])
    test(account.roles[0] + ' rejects unsafe return ' + JSON.stringify(bad), () => assert.equal(entry.workspaceLanding(account, bad), landing));
}
for (const bad of [null, {}, { workspace: 'Teacher', roles: ['Student'], isPlatformOwner: false }, { workspace: 'Portal', roles: ['Teacher'], isPlatformOwner: false }, { workspace: 'Platform', roles: ['PlatformOwner'], isPlatformOwner: false }, { workspace: 'AcademyAdmin', roles: [], isPlatformOwner: false }, { workspace: 'AcademyAdmin', roles: ['Teacher'], isPlatformOwner: false }, { workspace: 'Other', roles: ['Owner'], isPlatformOwner: false }, { workspace: 'Teacher', roles: [1], isPlatformOwner: false }, { workspace: 'Teacher', roles: ['Teacher'] }])
  test('malformed/mismatched server profile rejected ' + JSON.stringify(bad), () => assert.equal(entry.validWorkspaceAccount(bad), false));
for (const account of accounts.map(x => x[0]).filter(x => x.workspace !== 'AcademyAdmin'))
  test(account.roles[0] + ' cannot return into another token namespace', () => assert.equal(entry.workspaceLanding(account, '/student-onboarding'), entry.workspaceHomes[account.workspace].ai));
async function login(config = {}) {
  const saves = [], navigations = [], requests = [], messages = [];
  let state = 0;
  const react = { ...require('../../apps/web/node_modules/react'), useState(initial) { const i = state++; return [i === 0 ? 'synthetic@example.invalid' : i === 1 ? 'synthetic-private-password' : initial, value => { if (i === 3) messages.push(value); }]; } };
  const module = { exports: {} };
  const requireFn = name => name === 'react' ? react : name === 'react/jsx-runtime' ? require('../../apps/web/node_modules/react/jsx-runtime') : name === 'next/navigation' ? { useRouter: () => ({ push: route => navigations.push(route) }), useSearchParams: () => ({ get: () => config.returnTo ?? null }) } : name === '@/lib/api' ? { beginPortalSession: (...args) => saves.push(args) } : name === '@/lib/workspace-entry' ? entry : require(name);
  const tokens = config.tokens ?? { accessToken: 'synthetic-access', refreshToken: 'synthetic-refresh' };
  const fetch = async (url, init) => { requests.push({ url, init }); if (url.includes('/api/auth/login')) return { ok: !config.loginDenied, json: async () => tokens }; return { ok: !config.profileDenied, json: async () => { if (config.badJson) throw Error('synthetic'); return config.profile ?? accounts[0][0]; } }; };
  vm.runInNewContext(compile('apps/web/src/app/login/page.tsx'), { module, exports: module.exports, require: requireFn, process: { env: {} }, fetch });
  const element = module.exports.default(); const form = element.props.children;
  await form.props.onSubmit({ preventDefault() {} });
  return { saves, navigations, requests, messages };
}
for (const [profile, expected] of accounts) test('actual login saves only verified ' + profile.roles[0] + ' namespace', async () => {
  const result = await login({ profile });
  assert.deepEqual(result.saves, [[profile.workspace, 'synthetic-access', 'synthetic-refresh']]);
  assert.deepEqual(result.navigations, [expected]); assert.equal(result.requests.length, 2);
  assert.equal(result.requests[1].init.cache, 'no-store');
});
for (const config of [{ loginDenied: true }, { profileDenied: true }, { badJson: true }, { profile: {} }, { profile: { workspace: 'Teacher', roles: ['Student'], isPlatformOwner: false } }, { tokens: {} }, { tokens: { accessToken: ' ', refreshToken: 'synthetic' } }, { tokens: { accessToken: 'synthetic', refreshToken: '' } }])
  test('actual login never stores or navigates after invalid token/profile ' + JSON.stringify(config), async () => { const result = await login(config); assert.equal(result.saves.length, 0); assert.equal(result.navigations.length, 0); assert.ok(result.messages.some(Boolean)); });
test('actual platform login rejects external return without persisting its URL', async () => { const result = await login({ profile: accounts[3][0], returnTo: '//evil.invalid' }); assert.deepEqual(result.navigations, ['/platform/ai']); });
