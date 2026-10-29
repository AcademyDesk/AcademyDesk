'use strict';
const { test } = require('node:test');
const a = require('node:assert/strict');
const fs = require('node:fs'), path = require('node:path');
const { spawnSync } = require('node:child_process');
const { inventory, copyFiles, verifyFiles, buildEnvironment, excluded, memoryGuard } = require('./default-web-build.cjs');
const base = path.resolve(__dirname, '../../.build-check/default-web-build-tests');
fs.mkdirSync(base, { recursive: true });
function fixture(t) {
  const dir = fs.mkdtempSync(path.join(base, 'case-'));
  t.after(() => { a.equal(path.dirname(dir), base); fs.rmSync(dir, { recursive: true }); });
  return dir;
}
test('environment and credential input names are excluded', () => {
  for (const name of ['.env', '.env.local', '.ENV.production', '.npmrc', '.yarnrc.yml', 'local.pfx', 'private.key', 'cert.pem', 'local.p12', 'deploy.publishsettings']) a.ok(excluded(name), name);
  for (const name of ['package.json', 'page.tsx', 'script.mjs', '.package-lock.json']) a.equal(excluded(name), false);
});
test('memory guard retains the exact 3GiB admission threshold', () => {
  a.throws(() => memoryGuard(3*1024**3-1), /3GiB guard/);
  memoryGuard(3*1024**3); memoryGuard(4*1024**3);
});
test('invalid memory measurements cannot admit a build', () => {
  for(const bytes of [NaN, Infinity, -1, 0]) a.throws(() => memoryGuard(bytes), /3GiB guard/);
});
test('child environment excludes private credentials, flags and inherited build modifiers', () => {
  const result = buildEnvironment({ PATH: 'synthetic-path', SystemRoot: 'synthetic-system', AZURE_CREDENTIALS: 'synthetic-secret', NEXT_PUBLIC_API_URL: 'https://untrusted.invalid', NODE_OPTIONS: '--require=untrusted', PENTA_TOKEN: 'synthetic-token' });
  a.equal(result.PATH, 'synthetic-path'); a.equal(result.AZURE_CREDENTIALS, undefined);
  a.equal(result.PENTA_TOKEN, undefined); a.equal(result.NODE_OPTIONS, undefined);
  a.equal(result.NEXT_PUBLIC_API_URL, 'http://127.0.0.1:18080');
  a.equal(result.npm_config_offline, 'true'); a.equal(result.NEXT_TELEMETRY_DISABLED, '1');
});
test('only permitted files are copied, byte-for-byte, without source writes', t => {
  const dir = fixture(t), source = path.join(dir, 'source'), target = path.join(dir, 'target');
  fs.mkdirSync(source); fs.writeFileSync(path.join(source, 'module.js'), Buffer.from([0, 255, 13, 10]));
  fs.writeFileSync(path.join(source, '.env.local'), 'synthetic-only');
  const files = inventory(source); a.equal(files.length, 1); copyFiles(files, source, target);
  a.deepEqual(fs.readFileSync(path.join(target, 'module.js')), fs.readFileSync(path.join(source, 'module.js')));
  a.equal(fs.existsSync(path.join(target, '.env.local')), false);
  a.equal(fs.readFileSync(path.join(source, '.env.local'), 'utf8'), 'synthetic-only');
});
test('existing destination files cannot be overwritten', t => {
  const dir = fixture(t), source = path.join(dir, 'source'), target = path.join(dir, 'target');
  fs.mkdirSync(source); fs.mkdirSync(target); fs.writeFileSync(path.join(source, 'file'), 'new'); fs.writeFileSync(path.join(target, 'file'), 'existing');
  a.throws(() => copyFiles(inventory(source), source, target), /EEXIST/);
  a.equal(fs.readFileSync(path.join(target, 'file'), 'utf8'), 'existing');
});
test('bounded streamed verifier checks every copied file, including binary bytes', async t => {
  const dir = fixture(t), source = path.join(dir, 'source'), target = path.join(dir, 'target'); fs.mkdirSync(source);
  for(let i=0;i<12;i++) fs.writeFileSync(path.join(source, 'file-'+i), Buffer.from([0, 255, i]));
  const files = inventory(source); copyFiles(files, source, target);
  await verifyFiles(files, source, target); a.equal(files.length, 12);
});
test('bounded streamed verifier rejects tampered copied bytes', async t => {
  const dir = fixture(t), source = path.join(dir, 'source'), target = path.join(dir, 'target'); fs.mkdirSync(source);
  fs.writeFileSync(path.join(source, 'file'), 'original'); const files=inventory(source); copyFiles(files,source,target);
  fs.writeFileSync(path.join(target, 'file'), 'tampered');
  await a.rejects(verifyFiles(files, source, target), /Resumed dependency bytes differ/);
});
test('nested junctions cannot escape a staged dependency tree', t => {
  const dir = fixture(t), source = path.join(dir, 'source'), outside = path.join(dir, 'outside');
  fs.mkdirSync(source); fs.mkdirSync(outside); fs.symlinkSync(outside, path.join(source, 'linked'), 'junction');
  a.throws(() => inventory(source), /nested links/);
});
test('a supplied copy path cannot escape its source root', t => {
  const dir = fixture(t), source = path.join(dir, 'source'); fs.mkdirSync(source);
  const outside = path.join(dir, 'outside.js'); fs.writeFileSync(outside, 'synthetic');
  a.throws(() => copyFiles([{ file: outside }], source, path.join(dir, 'target')));
  a.equal(fs.existsSync(path.join(dir, 'target')), false);
});
test('static byte runner rejects an unowned export before any HTTP or evidence writes', t => {
  const dir = fixture(t), run = '11111111111141118111111111111111';
  const evidence = path.resolve(__dirname, '../EVIDENCE/static-web-runtime-' + run);
  const before = fs.existsSync(evidence);
  const result = spawnSync(process.execPath, [path.join(__dirname, 'static-web-runtime.cjs'), 'http://127.0.0.1:12345', run, dir], { encoding: 'utf8', windowsHide: true });
  a.notEqual(result.status, 0); a.match(result.stderr, /Only current or owned isolated exports allowed/);
  a.equal(fs.existsSync(evidence), before);
});
test('browser runner rejects an unowned export before launching a browser', t => {
  const dir = fixture(t);
  const result = spawnSync(process.execPath, [path.join(__dirname, 'penta-chat-contract-browser.cjs')], { env: { ...process.env, QA_PENTA_EXPORT_ROOT: dir }, encoding: 'utf8', windowsHide: true });
  a.notEqual(result.status, 0); a.match(result.stderr, /Only current or owned isolated exports allowed/);
});

test('fresh builds require actual installed versions to equal reviewed manifest and lock',()=>{
 const{validateInstalledVersions}=require('./default-web-build.cjs'),manifest={dependencies:{next:'16.4.1',react:'19.2.8','react-dom':'19.2.8'},devDependencies:{'eslint-config-next':'16.4.1'},overrides:{'source-map-js':'1.2.2'}},installed={...manifest.dependencies,...manifest.devDependencies,sharp:'0.35.5','source-map-js':'1.2.2'},lock={lockfileVersion:3,packages:{'':{dependencies:manifest.dependencies,devDependencies:manifest.devDependencies},...Object.fromEntries(Object.entries(installed).map(([name,version])=>['node_modules/'+name,{version}]))}};
 validateInstalledVersions(manifest,lock,installed);
 for(const name of Object.keys(installed)){a.throws(()=>validateInstalledVersions(manifest,lock,{...installed,[name]:'old-version'}));}
 const mismatched=structuredClone(lock);mismatched.packages[''].dependencies.next='16.3.5';a.throws(()=>validateInstalledVersions(manifest,mismatched,installed));
});

test('large dependency files retain streamed verification and a final-byte alteration is rejected',async t=>{
 const dir=fixture(t),source=path.join(dir,'source'),target=path.join(dir,'target');fs.mkdirSync(source);fs.mkdirSync(target);fs.writeFileSync(path.join(source,'large.bin'),Buffer.alloc(1024*1024,0xab));const files=inventory(source);copyFiles(files,source,target);await verifyFiles(files,source,target);
 const handle=fs.openSync(path.join(target,'large.bin'),'r+');try{fs.writeSync(handle,Buffer.from([0xcd]),0,1,1024*1024-1);}finally{fs.closeSync(handle);}await a.rejects(()=>verifyFiles(files,source,target));
});
