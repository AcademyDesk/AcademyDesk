'use strict';
// Presentation qualification only. Does not qualify SQL authorization or device clocks.
const fs = require('node:fs'), path = require('node:path');
const a = require('node:assert/strict'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const uiId = '2e6a10b2410142f183d546379a04d7cf';
const windowId = '15dac26542a2469f8d04cbec00fc57fe';
const retestId = '5bb52c9ef1aa4d1d92a8a9712ff1c58a';
const read = p => fs.readFileSync(path.join(root, 'QA/EVIDENCE', p));
const json = p => JSON.parse(read(p));
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
function run() {
  const ui = json('shared-select-ui-' + uiId + '/result.json');
  const web = require('./shared-select-ui-export.cjs').verify(uiId);
  a.equal(ui.passed, false); a.equal(ui.exportReady, true);
  a.equal(ui.failure, 'Shared select validation failed: browser\n\n1 !== 0\n');
  a.equal(ui.handlerChecks, 63);
  a.deepEqual(ui.steps.map(s => s.name), ['handlers', 'typecheck', 'targeted-lint', 'build', 'browser']);
  a.ok(ui.steps.slice(0, 4).every(s => s.exitCode === 0 && s.timedOut === false));
  a.equal(ui.steps[4].exitCode, 1); a.equal(ui.steps[4].timedOut, false);
  const prior = json('access-expiry-ui-11505c82b2d4463e86d925bf4069e17e/result.json');
  const pins = new Map(prior.sourcePins.map(f => [f.path, f]));
  a.equal(ui.sourcePins.length, prior.sourcePins.length);
  a.deepEqual(ui.sourcePins.filter(f => JSON.stringify(f) !== JSON.stringify(pins.get(f.path))).map(f => f.path), ['src/components/standard-select-field.tsx']);
  const retest = json('shared-select-retest-' + retestId + '/result.json');
  a.equal(retest.passed, true); a.equal(retest.sourceUi, uiId); a.equal(retest.webBuild, web.build);
  a.equal(retest.exitCode, 0); a.equal(retest.timedOut, false);
  for (const k of ['buildRepeated', 'applicationChanged', 'productionApproved']) a.equal(retest[k], false);
  a.match(retest.browserRun, /^[0-9a-f]{32}$/);
  const browserPath = 'shared-select-browser-' + retest.browserRun;
  const b = json(browserPath + '/result.json');
  a.equal(b.passed, true); a.equal(b.ui, uiId); a.equal(b.webBuild, web.build);
  a.equal(b.verifiedExportFiles, 627);
  for (const k of ['apiResponseMocks', 'syntheticToken']) a.equal(b[k], true);
  for (const k of ['actualSql', 'serverAuthorizationTested', 'physicalDevicesTested', 'productionApproved']) a.equal(b[k], false);
  a.equal(b.failure, undefined); a.equal(b.failedCase, undefined);
  a.deepEqual(b.cases.map(c => [c.width, c.theme]), [320, 390, 768, 1440].flatMap(width => [[width, 'light'], [width, 'dark']]));
  const screenshots = b.cases.map(c => {
    a.equal(c.options, 6);
    for (const k of ['selectedFocus', 'arrowHomeEnd', 'typeAhead', 'enterAndSpace', 'tabAndReverseTab', 'escapeRestoresFocus', 'pointerParity', 'topLayerContained', 'noImplicitChange', 'noWrites']) a.equal(c[k], true);
    a.equal(c.pageErrors, 0);
    const bytes = read(browserPath + '/select-' + c.width + '-' + c.theme + '.png');
    a.equal(bytes.subarray(0, 8).toString('hex'), '89504e470d0a1a0a');
    return { width: c.width, theme: c.theme, sha256: hash(bytes) };
  });
  const w = json('api-runtime-window-' + windowId + '/result.json');
  for (const k of ['passed', 'runtimePassed', 'restorationPassed', 'inventoryPreserved']) a.equal(w[k], true);
  a.equal(w.uiRun, retestId); a.equal(w.webBuild, web.build); a.equal(w.runtimeExit, 0);
  a.equal(w.failure, undefined); a.equal(w.productionApproved, false); a.equal(w.restartInvocations, 0);
  a.ok(w.admissionFreeBytes >= 3 * 1024 ** 3);
  a.deepEqual(w.before, w.after); a.deepEqual(w.restorationErrors, []);
  a.deepEqual(w.services.map(s => s.name).sort(), ['pentaaimodels-api-1', 'pentaaimodels-inference-1']);
  a.equal(w.restored.length, 2);
  for (const s of w.services) {
    const r = w.restored.find(x => x.name === s.name);
    a.equal(r.id, s.id); a.equal(r.image, s.image); a.equal(r.running, true);
    a.ok(r.health === null || r.health === 'healthy');
  }
  const log = JSON.parse(read('api-runtime-window-' + windowId + '/runtime.log').toString().trim());
  a.equal(log.passed, true); a.equal(log.run, retestId); a.equal(log.sourceUi, uiId); a.equal(log.webBuild, web.build); a.equal(log.browserRun, b.run);
  const priorWindow = json('api-runtime-window-e4397e8082534cac81fc633ec6a88b21/result.json');
  a.equal(priorWindow.passed, false); a.equal(priorWindow.runtimeExit, 1);
  a.equal(priorWindow.failure, 'Approved API test failed; retain runtime log and restore services.\n\n1 !== 0\n');
  for (const k of ['restorationPassed', 'inventoryPreserved']) a.equal(priorWindow[k], true);
  a.deepEqual(priorWindow.before, priorWindow.after); a.deepEqual(priorWindow.restorationErrors, []);
  const failedBrowser = json('shared-select-browser-1261c23a7cd64e9e8090cb65194c97c5/result.json');
  a.equal(failedBrowser.passed, false); a.equal(failedBrowser.ui, uiId); a.equal(failedBrowser.cases.length, 2);
  a.deepEqual([failedBrowser.failedCase.width, failedBrowser.failedCase.theme, failedBrowser.failedCase.stage], [390, 'light', 'layer containment and pointer parity']);
  a.equal(fs.existsSync(path.join(root, '.build-check/qa-validation-window.lock')), false);
  console.log(JSON.stringify({ passed: true, scope: 'Shared select keyboard and pointer presentation only', uiRun: uiId, browserRun: b.run, windowRun: windowId, webBuild: web.build, handlerChecks: 63, exportFiles: 627, browserJourneys: 8, screenshots, browserSha256: hash(read(browserPath + '/result.json')), windowSha256: hash(read('api-runtime-window-' + windowId + '/result.json')), apiResponseMocks: true, serverAuthorizationTested: false, physicalDevicesTested: false, enterpriseComplete: false, productionApproved: false }, null, 2));
}
if (require.main === module) try { run(); } catch (error) {
  console.error(JSON.stringify({ passed: false, scope: 'Shared select proof incomplete or changed', type: error.name, sourceLines: [...String(error.stack || '').matchAll(/shared-select-qualification\.cjs:(\d+):(\d+)/g)].map(m => ({ line: Number(m[1]), column: Number(m[2]) })) }));
  process.exitCode = 1;
}
