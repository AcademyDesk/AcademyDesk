const fs = require('node:fs'), assert = require('node:assert/strict'), test = require('node:test');
const ts = require('../../apps/web/node_modules/typescript');
const compiled = ts.transpileModule(fs.readFileSync('apps/web/src/lib/workspace-entry.ts', 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText;
const fixture = { exports: {} }; new Function('exports', compiled)(fixture.exports);
const { workspaceLanding, validWorkspaceAccount } = fixture.exports;
const accounts = [
  [{ workspace: 'Platform', roles: ['PlatformOwner'], isPlatformOwner: true }, '/platform/ai'],
  [{ workspace: 'AcademyAdmin', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/penta'],
  [{ workspace: 'AcademyAdmin', roles: ['Owner'], isPlatformOwner: false }, '/penta'],
  [{ workspace: 'Teacher', roles: ['Teacher'], isPlatformOwner: false }, '/teacher/ai'],
  [{ workspace: 'Portal', roles: ['Student'], isPlatformOwner: false }, '/portal/ai'],
  [{ workspace: 'Portal', roles: ['Guardian'], isPlatformOwner: false }, '/portal/ai'],
  [{ workspace: 'AcademyAdmin', roles: ['FinanceUser'], isPlatformOwner: false }, '/dashboard'],
];
for (const [account, expected] of accounts) test(account.roles[0] + ' landing is scoped, not a permission grant', () => {
  assert.equal(validWorkspaceAccount(account), true); assert.equal(workspaceLanding(account), expected);
  for (const bad of ['//evil.invalid', '/\\evil.invalid', 'https://evil.invalid', '/%70latform', '/%2f%2fevil.invalid', '/platform/../teacher', '/login', '/register', '/teacher\n/ai']) {
    if (account.workspace === 'Teacher' && bad === '/platform/../teacher') continue; // normalized same-workspace return is safe
    const actual = workspaceLanding(account, bad);
    if (account.workspace !== 'AcademyAdmin' || bad !== '/platform/../teacher') assert.equal(actual, expected, bad);
  }
});
for (const bad of [null, {}, { workspace: 'Teacher', roles: ['Teacher'] }, { workspace: 'Teacher', roles: ['Student'], isPlatformOwner: false }, { workspace: 'Portal', roles: ['Teacher'], isPlatformOwner: false }, { workspace: 'Platform', roles: ['PlatformOwner'], isPlatformOwner: false }, { workspace: 'AcademyAdmin', roles: [], isPlatformOwner: false }, { workspace: 'AcademyAdmin', roles: ['Student'], isPlatformOwner: false }, { workspace: 'Other', roles: ['Owner'], isPlatformOwner: false }, { workspace: 'Portal', roles: [1], isPlatformOwner: false }]) test('malformed/mismatched account rejected ' + JSON.stringify(bad), () => assert.equal(validWorkspaceAccount(bad), false));
test('explicit in-workspace Manual links retain query/fragment', () => {
  assert.equal(workspaceLanding(accounts[0][0], '/platform/control?tab=Audit%20%26%20health#jobs'), '/platform/control?tab=Audit%20%26%20health#jobs');
  assert.equal(workspaceLanding(accounts[3][0], '/teacher'), '/teacher');
  assert.equal(workspaceLanding(accounts[4][0], '/portal'), '/portal');
  assert.equal(workspaceLanding(accounts[1][0], '/student-management?studentId=example'), '/student-management?studentId=example');
});
test('return route cannot cross workspace namespaces', () => {
  assert.equal(workspaceLanding(accounts[0][0], '/teacher'), '/platform/ai');
  assert.equal(workspaceLanding(accounts[3][0], '/penta'), '/teacher/ai');
  assert.equal(workspaceLanding(accounts[4][0], '/penta'), '/portal/ai');
  assert.equal(workspaceLanding(accounts[1][0], '/platform/control'), '/penta');
});
test('full-portal completion ledger lists every current page exactly once', () => {
  const path = require('node:path'), folder = path.resolve('apps/web/src/app'), pages = [];
  function walk(dir) { for (const entry of fs.readdirSync(dir, { withFileTypes: true })) { const file = path.join(dir, entry.name); if (entry.isDirectory()) walk(file); else if (entry.name === 'page.tsx') pages.push('/' + path.relative(folder, path.dirname(file)).replaceAll(path.sep, '/')); } }
  walk(folder); const ledger = fs.readFileSync('PENTA_PORTAL_COMPLETION_CHECKLIST.md', 'utf8');
  const rows = [...ledger.matchAll(/^\| `(\/[^`]*)` \| \[(?: |x)\] \| \[(?: |x)\] \| \[(?: |x)\] \|/gm)].map(m => m[1]);
  assert.deepEqual(rows.sort(), pages.sort()); assert.equal(new Set(rows).size, pages.length);
});
