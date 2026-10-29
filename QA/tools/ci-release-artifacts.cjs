/* Source/content binding for CI developer checks, NOT staging or production approval. */
'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const cp = require('node:child_process');
const MINIMUM_API_TESTS = 3183;
const VERSION = 'academy-ci-artifact/1';
const fail = message => { throw new Error(message); };
const sha = value => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
const hash = file => {
  const h = crypto.createHash('sha256'), descriptor = fs.openSync(file, 'r');
  const buffer = Buffer.alloc(1024 * 1024);
  try { let count; while ((count = fs.readSync(descriptor, buffer, 0, buffer.length, null))) h.update(buffer.subarray(0, count)); }
  finally { fs.closeSync(descriptor); }
  return h.digest('hex');
};
function publicApi(value) {
  let url;
  try { url = new URL(value); } catch { fail('A valid public HTTPS API origin is required.'); }
  if (url.protocol !== 'https:' || url.username || url.password || url.search || url.hash ||
      url.pathname !== '/' || !url.hostname || url.hostname === 'localhost' ||
      /^(127\.|10\.|192\.168\.|169\.254\.|0\.|\[)/.test(url.hostname) ||
      /^172\.(1[6-9]|2\d|3[01])\./.test(url.hostname)) fail('A public HTTPS API origin without credentials or extra URL components is required.');
  return url.origin;
}
function counters(trx) {
  if (typeof trx !== 'string' || trx.length > 64 * 1024 * 1024 || /<!DOCTYPE|<!ENTITY/i.test(trx)) fail('Invalid API test report.');
  const matches = [...trx.matchAll(/<Counters\b([^>]*?)\/?\s*>/g)];
  if (matches.length !== 1) fail('Exactly one API test counter is required.');
  const attrs = {};
  for (const match of matches[0][1].matchAll(/([A-Za-z][\w]*)="(\d+)"/g)) {
    if (Object.hasOwn(attrs, match[1])) fail('Duplicate API test counter.');
    attrs[match[1]] = Number(match[2]);
  }
  for (const name of ['total', 'executed', 'passed', 'failed', 'error', 'timeout', 'aborted', 'notExecuted', 'notRunnable'])
    if (!Number.isSafeInteger(attrs[name]) || attrs[name] < 0) fail('Incomplete API test counters.');
  if (attrs.total < MINIMUM_API_TESTS || attrs.executed !== attrs.total || attrs.passed !== attrs.total ||
      ['failed', 'error', 'timeout', 'aborted', 'notExecuted', 'notRunnable'].some(key => attrs[key] !== 0))
    fail('API developer gate requires the accepted test-count floor, all passing and zero skipped/unexecuted.');
  const results = [...trx.matchAll(/<UnitTestResult\b([^>]*?)\/?\s*>/g)];
  if (results.length !== attrs.total || results.some(result => !/\boutcome="Passed"/.test(result[1])))
    fail('API test results do not reconcile with passing counters.');
  return { total: attrs.total, passed: attrs.passed, skipped: 0 };
}
function files(folder, prefix = '') {
  const entries = [];
  for (const item of fs.readdirSync(folder, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))) {
    if (item.isSymbolicLink()) fail('CI payload cannot contain links.');
    const relative = prefix ? `${prefix}/${item.name}` : item.name;
    if (/[^\x20-\x7e]|\\|:/.test(relative) || relative.split('/').some(part => part === '.' || part === '..')) fail('Unsafe CI payload path.');
    const full = path.join(folder, item.name);
    if (item.isDirectory()) entries.push(...files(full, relative));
    else if (item.isFile()) {
      const size = fs.statSync(full).size;
      if (!Number.isSafeInteger(size) || size > 4 * 1024 ** 3) fail('CI payload exceeds its file bound.');
      entries.push({ path: relative, bytes: size, sha256: hash(full) });
    } else fail('CI payload must contain only regular files and directories.');
    if (entries.length > 100000) fail('CI payload exceeds its file-count bound.');
  }
  return entries;
}
function source(expected) {
  if (!sha(expected)) fail('Exact source SHA required.');
  const cwd = path.resolve(__dirname, '../..');
  if (cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd, encoding: 'utf8' }).trim() !== expected ||
      cp.execFileSync('git', ['status', '--porcelain', '--untracked-files=all'], { cwd, encoding: 'utf8' }).trim())
    fail('CI artifacts require the exact clean checkout, including untracked source.');
}
function writeManifest(folder, kind, expected, options = {}) {
  if (!sha(expected) || !['api', 'web'].includes(kind)) fail('Invalid artifact identity.');
  const manifestFile = path.join(folder, 'manifest.json');
  if (fs.existsSync(manifestFile)) fail('Do not overwrite an existing CI manifest.');
  const payload = files(folder);
  if (!payload.length || kind === 'api' && (payload.length !== 1 || payload[0].path !== 'api-image.tar.gz') ||
      kind === 'web' && (!payload.some(x => x.path === 'out/index.html') || payload.some(x => !x.path.startsWith('out/'))))
    fail('Artifact payload layout is incomplete.');
  const value = { version: VERSION, kind, sourceSha: expected, developerChecksOnly: true,
    stagingApproved: false, productionApproved: false,
    ...(kind === 'web' ? { apiOrigin: publicApi(options.apiOrigin) } : { tests: counters(options.trx) }), files: payload };
  fs.writeFileSync(manifestFile, `${JSON.stringify(value, null, 2)}\n`, { flag: 'wx' });
  return value;
}
function verify(folder, kind, expected, apiOrigin) {
  if (!sha(expected) || !['api', 'web'].includes(kind)) fail('Invalid expected artifact identity.');
  const manifestFile = path.join(folder, 'manifest.json');
  if (fs.lstatSync(manifestFile).isSymbolicLink() || fs.statSync(manifestFile).size > 32 * 1024 * 1024) fail('Invalid CI manifest file.');
  const value = JSON.parse(fs.readFileSync(manifestFile, 'utf8'));
  const keys = ['version', 'kind', 'sourceSha', 'developerChecksOnly', 'stagingApproved', 'productionApproved', 'files', kind === 'web' ? 'apiOrigin' : 'tests'].sort();
  if (!value || Object.keys(value).sort().join() !== keys.join() || value.version !== VERSION || value.kind !== kind || value.sourceSha !== expected ||
      value.developerChecksOnly !== true || value.stagingApproved !== false || value.productionApproved !== false || !Array.isArray(value.files)) fail('CI artifact identity or approval boundary mismatch.');
  if (kind === 'web' && (value.apiOrigin !== publicApi(apiOrigin) || value.apiOrigin === 'https://api.example.invalid')) fail('Web artifact API origin differs from the deployment target.');
  if (kind === 'api' && (!value.tests || Object.keys(value.tests).sort().join() !== 'passed,skipped,total' ||
      !Number.isSafeInteger(value.tests.total) || value.tests.total < MINIMUM_API_TESTS || value.tests.passed !== value.tests.total || value.tests.skipped !== 0)) fail('API test manifest does not satisfy the developer gate.');
  const actual = files(folder).filter(item => item.path !== 'manifest.json');
  if (!actual.length || JSON.stringify(actual) !== JSON.stringify(value.files)) fail('CI artifact content does not match its manifest.');
  if (kind === 'api' && (actual.length !== 1 || actual[0].path !== 'api-image.tar.gz') ||
      kind === 'web' && (!actual.some(x => x.path === 'out/index.html') || actual.some(x => !x.path.startsWith('out/')))) fail('Unexpected CI artifact payload layout.');
  return value;
}
function main(args) {
  const [action, folder, kind, expected, argument] = args;
  if (args.length !== 5 || !['create', 'verify'].includes(action)) fail('Usage: create|verify <folder> api|web <source-sha> <trx-path|api-origin>');
  source(expected);
  const value = action === 'create' ? writeManifest(folder, kind, expected,
    kind === 'api' ? { trx: fs.readFileSync(argument, 'utf8') } : { apiOrigin: argument }) : verify(folder, kind, expected, argument);
  console.log(JSON.stringify({ kind: value.kind, sourceSha: value.sourceSha, files: value.files.length, developerChecksOnly: true, stagingApproved: false, productionApproved: false }));
}
module.exports = { counters, files, publicApi, writeManifest, verify, source, main, MINIMUM_API_TESTS };
if (require.main === module) { try { main(process.argv.slice(2)); } catch (error) { console.error(error.message); process.exitCode = 1; } }
