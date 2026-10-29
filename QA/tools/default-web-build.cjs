'use strict';
// Offline, self-contained build fixture, not another dev server or deployable
// release. The real worktree may use a junction outside Turbopack's root.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const os = require('node:os');
const { spawn, execFileSync } = require('node:child_process');
const a = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const entries = ['package.json', 'package-lock.json', 'next.config.ts', 'tsconfig.json', 'next-env.d.ts', 'postcss.config.mjs', 'eslint.config.mjs', 'src', 'public', 'scripts'];
const excluded = name => /^\.env(?:\.|$)/i.test(name) || /^\.(?:npmrc|yarnrc)/i.test(name) || /\.(?:pfx|p12|pem|key|publishsettings)$/i.test(name);
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
function memoryGuard(freeBytes = os.freemem()) {
  a.ok(Number.isFinite(freeBytes) && freeBytes >= 3 * 1024 ** 3, '3GiB guard: no new staging/build step started.');
}

function inventory(directory) {
  const files = [];
  function walk(dir) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      if (excluded(entry.name)) continue;
      const file = path.join(dir, entry.name);
      a.ok(!entry.isSymbolicLink(), 'Refusing nested links in offline build inputs.');
      if (entry.isDirectory()) walk(file);
      else if (entry.isFile()) files.push({ file, bytes: fs.statSync(file).size });
      else throw Error('Unsupported build input type.');
    }
  }
  walk(directory);
  a.ok(files.length <= 120_000, 'Offline input file bound exceeded.');
  a.ok(files.reduce((sum, file) => sum + file.bytes, 0) <= 2 * 1024 ** 3, 'Offline input byte bound exceeded.');
  return files;
}
function copyFiles(files, source, destination) {
  for (const { file } of files) {
    const relative = path.relative(source, file);
    a.ok(relative && !relative.startsWith('..') && !path.isAbsolute(relative));
    const target = path.join(destination, relative);
    fs.mkdirSync(path.dirname(target), { recursive: true });
    fs.copyFileSync(file, target, fs.constants.COPYFILE_EXCL);
  }
}
async function hashFile(file) {
  // Most dependency files are tiny. Avoid two stream lifecycles per tiny file
  // while retaining a strict 256KiB allocation bound and streamed large files.
  const bytes=fs.statSync(file).size;
  if(bytes<=256*1024){
    const handle=fs.openSync(file,'r');try{const buffer=Buffer.alloc(bytes),read=fs.readSync(handle,buffer,0,bytes,0);a.equal(read,bytes);a.equal(fs.fstatSync(handle).size,bytes);return hash(buffer);}finally{fs.closeSync(handle);}
  }
  const digest = crypto.createHash('sha256');
  for await (const bytes of fs.createReadStream(file, { highWaterMark: 64 * 1024 })) digest.update(bytes);
  return digest.digest('hex');
}
async function verifyFiles(files, source, destination) {
  let next = 0, failed = false;
  await Promise.all(Array.from({ length: Math.min(4, files.length) }, async () => {
    while (!failed && next < files.length) {
      const { file } = files[next++];
      const relative = path.relative(source, file);
      a.ok(relative && !relative.startsWith('..') && !path.isAbsolute(relative));
      try {
        const [original, staged] = await Promise.all([hashFile(file), hashFile(path.join(destination, relative))]);
        a.equal(staged, original, 'Resumed dependency bytes differ.');
      } catch (error) { failed = true; throw error; }
    }
  }));
}
function sourceFiles(web) {
  return entries.flatMap(name => {
    const file = path.join(web, name), stat = fs.lstatSync(file);
    a.ok(!stat.isSymbolicLink(), 'Application input links are not allowed.');
    return stat.isDirectory() ? inventory(file) : [{ file, bytes: stat.size }];
  });
}
function sourcePins(web) {
  return sourceFiles(web).map(({ file }) => ({ path: path.relative(web, file).replaceAll('\\', '/'), sha256: hash(fs.readFileSync(file)) }));
}
function buildEnvironment(parent) {
  const env = {};
  for (const key of ['PATH', 'SystemRoot', 'WINDIR', 'COMSPEC', 'PATHEXT', 'TEMP', 'TMP', 'USERPROFILE', 'LOCALAPPDATA', 'APPDATA', 'HOMEDRIVE', 'HOMEPATH', 'NUMBER_OF_PROCESSORS']) {
    if (parent[key] !== undefined) env[key] = parent[key];
  }
  return { ...env, NODE_ENV: 'production', NEXT_TELEMETRY_DISABLED: '1', NEXT_PUBLIC_API_URL: 'http://127.0.0.1:18080', npm_config_offline: 'true', npm_config_audit: 'false', npm_config_update_notifier: 'false' };
}
function validateInstalledVersions(manifest, lock, installed) {
  a.equal(lock.lockfileVersion,3);
  a.deepEqual(lock.packages[''].dependencies,manifest.dependencies);
  a.deepEqual(lock.packages[''].devDependencies,manifest.devDependencies);
  for(const name of ['next','react','react-dom','eslint-config-next']){
    const declared=manifest.dependencies?.[name]??manifest.devDependencies?.[name];
    a.equal(installed[name],declared,'Installed direct dependency differs from the reviewed manifest.');
    a.equal(installed[name],lock.packages['node_modules/'+name].version,'Installed dependency differs from the reviewed lock.');
  }
  for(const name of ['sharp','source-map-js'])a.equal(installed[name],lock.packages['node_modules/'+name].version);
  if(manifest.overrides?.['source-map-js'])a.equal(installed['source-map-js'],manifest.overrides['source-map-js']);
}
async function run() {
  a.equal(root.toLowerCase(), 'd:\\academydesk-codex-p0');
  memoryGuard();
  const disk = fs.statfsSync(root);
  a.ok(Number(disk.bavail) * Number(disk.bsize) >= 5 * 1024 ** 3, '5GiB free disk guard.');
  const web = path.join(root, 'apps/web'), dependencies = fs.realpathSync(path.join(web, 'node_modules'));
  a.ok(dependencies.toLowerCase()===path.join(web,'node_modules').toLowerCase()||dependencies.toLowerCase()==='d:\\academydesk\\apps\\web\\node_modules','Only physical worktree dependencies or the known legacy main junction may be staged.');
  validateInstalledVersions(JSON.parse(fs.readFileSync(path.join(web,'package.json'))),JSON.parse(fs.readFileSync(path.join(web,'package-lock.json'))),Object.fromEntries(['next','react','react-dom','eslint-config-next','sharp','source-map-js'].map(name=>[name,JSON.parse(fs.readFileSync(path.join(dependencies,name,'package.json'))).version])));
  const dependencyFiles = inventory(dependencies), sources = sourceFiles(web), pins = sourcePins(web);
  const resume = process.argv[2] === '--resume';
  a.ok(process.argv.length === 2 || (resume && process.argv.length === 4), 'Only an exact owned run may be resumed.');
  const runId = resume ? process.argv[3] : crypto.randomUUID().replaceAll('-', '');
  a.match(runId, /^[0-9a-f]{32}$/);
  const base = path.join(root, '.build-check/default-web-build');
  fs.mkdirSync(base, { recursive: true });
  a.equal(fs.realpathSync(base).toLowerCase(), base.toLowerCase());
  const stage = path.join(base, runId);
  const app = path.join(stage, 'app');
  if (!resume) {
    fs.mkdirSync(stage); // Never replace, delete or clean an existing folder.
    fs.mkdirSync(app);
    console.log(JSON.stringify({ run: runId, stage, phase: 'copy-existing-inputs', dependencyFiles: dependencyFiles.length, dependencyBytes: dependencyFiles.reduce((sum, file) => sum + file.bytes, 0), downloads: false }));
    copyFiles(sources, web, app);
    copyFiles(dependencyFiles, dependencies, path.join(app, 'node_modules'));
  } else {
    for (const directory of [stage, app, path.join(app, 'node_modules')]) a.equal(fs.realpathSync(directory).toLowerCase(), directory.toLowerCase(), 'Resumed inputs must be physical directories.');
    for (const entry of fs.readdirSync(app)) a.ok([...entries, 'node_modules', '.next', 'out', 'tsconfig.tsbuildinfo'].includes(entry), 'Unexpected resumed application input.');
    a.deepEqual(sourcePins(app), pins, 'Resumed source differs from current worktree.');
    const stagedDependencies = inventory(path.join(app, 'node_modules'));
    a.equal(stagedDependencies.length, dependencyFiles.length, 'Resumed dependency file census mismatch.');
    await verifyFiles(dependencyFiles, dependencies, path.join(app, 'node_modules'));
    console.log(JSON.stringify({ run: runId, phase: 'resume-input-bytes-verified', dependencyFiles: dependencyFiles.length, sources: pins.length }));
  }
  a.deepEqual(sourcePins(web), pins, 'Original worktree source changed during staging.');
  memoryGuard();
  const evidence = path.join(root, 'QA/EVIDENCE/default-web-build-' + runId);
  fs.mkdirSync(evidence, { recursive: true });
  const log = path.join(evidence, 'build.log'), stream = fs.createWriteStream(log, { flags: 'wx' });
  const npm = path.join(path.dirname(process.execPath), 'node_modules/npm/bin/npm-cli.js');
  a.ok(fs.existsSync(npm), 'Existing npm runtime required; no download/install.');
  console.log(JSON.stringify({ run: runId, stage, sources: sources.length, dependencyFiles: dependencyFiles.length, dependencyBytes: dependencyFiles.reduce((sum, file) => sum + file.bytes, 0), command: 'npm run build', offline: true }));
  const child = spawn(process.execPath, [npm, 'run', 'build'], { cwd: app, env: buildEnvironment(process.env), windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
  child.stdout.pipe(stream, { end: false }); child.stderr.pipe(stream, { end: false });
  let timedOut = false;
  const timer = setTimeout(() => {
    if (child.exitCode === null && child.signalCode === null) {
      timedOut = true;
      // Only the process tree created immediately above; no other service stop.
      execFileSync('taskkill.exe', ['/PID', String(child.pid), '/T', '/F'], { windowsHide: true, stdio: 'ignore' });
    }
  }, 600_000);
  const exitCode = await new Promise((resolve, reject) => { child.on('error', reject); child.on('close', resolve); }).finally(() => clearTimeout(timer));
  await new Promise(resolve => stream.end(resolve));
  a.deepEqual(sourcePins(web), pins, 'Original source changed during the build.');
  const text = fs.readFileSync(log, 'utf8');
  const result = { run: runId, passed: exitCode === 0 && !timedOut, exitCode, timedOut, stage, command: 'npm run build', sourcePins: pins, dependencyFiles: dependencyFiles.length, dependencyBytes: dependencyFiles.reduce((sum, file) => sum + file.bytes, 0), sourceUnchanged: true, copiedDependencies: true, freshNpmCi: false, installed: false, downloaded: false, webpackOverride: false, turbopackObserved: text.includes('(Turbopack)'), staticPages: text.match(/\((\d+)\/(\d+)\) in/)?.slice(1).map(Number) ?? [], aliases: text.match(/Static segment aliases: (\{[^\r\n]+\})/)?.[1] ?? null, applicationChanged: false, productionApproved: false };
  fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ ...result, sourcePins: result.sourcePins.length, evidence }));
  if (!result.passed) process.exitCode = 1;
}
module.exports = { inventory, copyFiles, verifyFiles, sourceFiles, sourcePins, buildEnvironment, excluded, memoryGuard, validateInstalledVersions };
if (require.main === module) run().catch(error => { console.error(error.message); process.exitCode = 1; });
