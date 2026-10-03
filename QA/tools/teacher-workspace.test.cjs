// Actual TS classification with explicit errors; not real HTTP/browser proof.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript');
const source = fs.readFileSync(path.resolve(__dirname, '../../apps/web/src/lib/teacher-workspace.ts'), 'utf8');
const compiled = ts.transpileModule(source, {compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
const workspaceModule = {exports:{}};
new Function('module','exports',compiled)(workspaceModule,workspaceModule.exports);
const {TeacherWorkspaceError,teacherWorkspaceFailure} = workspaceModule.exports;

test('final unauthorized response asks for sign-in, never claims missing teacher profile', () => {
  const result = teacherWorkspaceFailure(new TeacherWorkspaceError(401));
  assert.equal(result.signIn, true);
  assert.match(result.message, /Please sign in/);
  assert.doesNotMatch(result.message, /not linked/);
});
test('authenticated profile/access denial keeps separate Academy Admin guidance', () => {
  const result = teacherWorkspaceFailure(new TeacherWorkspaceError(403));
  assert.equal(result.signIn, false);
  assert.match(result.message, /active teacher profile/);
  assert.match(result.message, /Academy Admin/);
});
test('server, missing endpoint, network and parsing failures do not mislabel authentication or expose internal errors', () => {
  for (const error of [new TeacherWorkspaceError(404),new TeacherWorkspaceError(429),new TeacherWorkspaceError(500),new TypeError('sensitive transport detail'),new SyntaxError('bad response'),null]) {
    const result = teacherWorkspaceFailure(error);
    assert.equal(result.signIn, false);
    assert.match(result.message, /Check your connection and reload/);
    assert.doesNotMatch(result.message, /not linked|expired|sensitive|bad response/);
  }
});
test('Teacher page wires the actual response status and conditionally renders only a fixed local sign-in link', () => {
  const page = fs.readFileSync(path.resolve(__dirname,'../../apps/web/src/app/teacher/page.tsx'),'utf8');
  assert.match(page, /if \(!r\.ok\) throw new TeacherWorkspaceError\(r\.status\)/);
  assert.match(page, /const failure = teacherWorkspaceFailure\(error\)/);
  assert.match(page, /setNeedsSignIn\(failure\.signIn\)/);
  assert.match(page, /needsSignIn &&[\s\S]*href="\/login"/);
});
