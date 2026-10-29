'use strict';
const { test } = require('node:test');
const a = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const root = path.resolve(__dirname, '../..');
const scratch = path.join(root, '.build-check/static-segment-tests');
fs.mkdirSync(scratch, { recursive: true });
const moduleReady = import(pathToFileURL(path.join(root, 'apps/web/scripts/prepare-static-segments.mjs')).href);
function fixture(t) {
  const dir = fs.mkdtempSync(path.join(scratch, 'case-'));
  t.after(() => {
    a.equal(path.dirname(dir), scratch);
    a.match(path.basename(dir), /^case-/);
    fs.rmSync(dir, { recursive: true });
  });
  function put(relative, contents = Buffer.from('0:["synthetic-flight"]\n')) {
    const file = path.join(dir, relative);
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, contents);
    return file;
  }
  return { dir, put };
}

test('one-level segment gets an exact-byte direct static alias', async t => {
  const { prepareStaticSegments } = await moduleReady;
  const { dir, put } = fixture(t), bytes = Buffer.from([0, 255, 13, 10, 97]);
  const source = put('dashboard/__next.dashboard/__PAGE__.txt', bytes);
  a.deepEqual(prepareStaticSegments(dir), { segments: 1, created: 1, unchanged: 0 });
  a.deepEqual(fs.readFileSync(path.join(dir, 'dashboard/__next.dashboard.__PAGE__.txt')), bytes);
  a.deepEqual(fs.readFileSync(source), bytes);
});
test('nested and future deeper segments preserve route prefixes', async t => {
  const { prepareStaticSegments } = await moduleReady;
  const { dir, put } = fixture(t);
  for (const route of ['penta/onboarding', 'platform/control/ai', 'a/b/c/d']) {
    const names = route.split('/');
    put(route + '/__next.' + names.join('/') + '/__PAGE__.txt', Buffer.from(route));
  }
  a.equal(prepareStaticSegments(dir).segments, 3);
  for (const route of ['penta/onboarding', 'platform/control/ai', 'a/b/c/d']) {
    a.equal(fs.readFileSync(path.join(dir, route, '__next.' + route.replaceAll('/', '.') + '.__PAGE__.txt'), 'utf8'), route);
  }
});
test('second preparation is idempotent, leaving payload and unrelated bytes intact', async t => {
  const { prepareStaticSegments } = await moduleReady;
  const { dir, put } = fixture(t);
  put('teacher/ai/__next.teacher/ai/__PAGE__.txt');
  const html = put('teacher/ai.html', '<html>synthetic</html>');
  const tree = put('teacher/ai/__next._tree.txt', 'tree');
  prepareStaticSegments(dir);
  a.deepEqual(prepareStaticSegments(dir), { segments: 1, created: 0, unchanged: 1 });
  a.equal(fs.readFileSync(html, 'utf8'), '<html>synthetic</html>');
  a.equal(fs.readFileSync(tree, 'utf8'), 'tree');
});
test('conflicting file aborts the whole preflight without creating other aliases', async t => {
  const { prepareStaticSegments } = await moduleReady;
  const { dir, put } = fixture(t);
  put('a/__next.a/__PAGE__.txt');
  put('z/__next.z/__PAGE__.txt');
  const conflict = put('z/__next.z.__PAGE__.txt', 'unrelated');
  a.throws(() => prepareStaticSegments(dir), /refusing to overwrite/);
  a.equal(fs.existsSync(path.join(dir, 'a/__next.a.__PAGE__.txt')), false);
  a.equal(fs.readFileSync(conflict, 'utf8'), 'unrelated');
});
test('directory collisions are refused without overwriting', async t => {
  const { prepareStaticSegments } = await moduleReady;
  const { dir, put } = fixture(t);
  put('a/__next.a/__PAGE__.txt');
  fs.mkdirSync(path.join(dir, 'a/__next.a.__PAGE__.txt'));
  a.throws(() => prepareStaticSegments(dir), /refusing to overwrite/);
});
test('unsupported component grammar fails closed before any alias write', async t => {
  const { prepareStaticSegments } = await moduleReady;
  const { dir, put } = fixture(t);
  put('a/__next.a/__PAGE__.txt');
  put('z/__next.z/unsupported.name/__PAGE__.txt');
  a.throws(() => prepareStaticSegments(dir), /Unsupported exported static segment layout/);
  a.equal(fs.existsSync(path.join(dir, 'a/__next.a.__PAGE__.txt')), false);
});
test('missing export and changed export layout cannot silently pass', async t => {
  const { prepareStaticSegments } = await moduleReady;
  const { dir, put } = fixture(t);
  a.throws(() => prepareStaticSegments(path.join(dir, 'missing')), /ENOENT/);
  put('index.html', '<html/>');
  a.throws(() => prepareStaticSegments(dir), /No exported static segments/);
});
test('linked directories cannot escape the export or cause partial alias writes', async t => {
  const { prepareStaticSegments } = await moduleReady;
  const { dir, put } = fixture(t);
  put('export/a/__next.a/__PAGE__.txt');
  const outside = put('outside/private.txt', 'synthetic-outside-sentinel');
  fs.symlinkSync(path.dirname(outside), path.join(dir, 'export/z-link'), 'junction');
  a.throws(() => prepareStaticSegments(path.join(dir, 'export')), /symbolic links/);
  a.equal(fs.existsSync(path.join(dir, 'export/a/__next.a.__PAGE__.txt')), false);
  a.equal(fs.readFileSync(outside, 'utf8'), 'synthetic-outside-sentinel');
});
test('build hook covers existing Azure and Docker npm build entry points', () => {
  const pkg = JSON.parse(fs.readFileSync(path.join(root, 'apps/web/package.json'), 'utf8'));
  a.equal(pkg.scripts.build, 'next build');
  a.equal(pkg.scripts.postbuild, 'node scripts/prepare-static-segments.mjs');
  a.match(fs.readFileSync(path.join(root, '.github/workflows/deploy-azure.yml'), 'utf8'), /npm run build/);
  a.match(fs.readFileSync(path.join(root, 'apps/web/Dockerfile'), 'utf8'), /RUN npm run build/);
});
