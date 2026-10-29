'use strict';
// Presentation qualification only. Does not qualify SQL authorization or device clocks.
const fs = require('node:fs'), path = require('node:path');
const a = require('node:assert/strict'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const uiId = '11505c82b2d4463e86d925bf4069e17e';
const windowId = '0e2dade20e8c40979df785641e4945db';
const read = p => fs.readFileSync(path.join(root, 'QA/EVIDENCE', p));
const json = p => JSON.parse(read(p));
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
function run() {
  const ui = json('access-expiry-ui-' + uiId + '/result.json');
  const web = require('./access-expiry-ui-export.cjs').verify(uiId);
  a.equal(ui.passed, true); a.equal(ui.exportReady, true);
  a.equal(ui.handlerChecks, 23);
  a.deepEqual(ui.steps.map(s => s.name), ['handlers', 'typecheck', 'targeted-lint', 'build', 'browser']);
  a.ok(ui.steps.every(s => s.exitCode === 0 && s.timedOut === false));
  const prior = json('staff-ui-5ce03dcd687040d584db0ffde6a9290c/result.json');
  const pins = new Map(prior.sourcePins.map(f => [f.path, f]));
  a.equal(ui.sourcePins.length, prior.sourcePins.length);
  a.deepEqual(ui.sourcePins.filter(f => JSON.stringify(f) !== JSON.stringify(pins.get(f.path))).map(f => f.path), ['src/app/access-review/page.tsx']);
  a.match(ui.browserRun, /^[0-9a-f]{32}$/);
  const browserPath = 'access-expiry-browser-' + ui.browserRun;
  const b = json(browserPath + '/result.json');
  a.equal(b.passed, true); a.equal(b.ui, uiId); a.equal(b.webBuild, web.build);
  a.equal(b.verifiedExportFiles, 627);
  for (const k of ['apiResponseMocks', 'syntheticToken']) a.equal(b[k], true);
  for (const k of ['actualSql', 'serverAuthorizationTested', 'physicalDevicesTested', 'productionApproved']) a.equal(b[k], false);
  a.equal(b.failure, undefined); a.equal(b.failedCase, undefined);
  a.deepEqual(b.cases.map(c => [c.width, c.theme]), [[390, 'light'], [390, 'dark'], [1440, 'light'], [1440, 'dark']]);
  const screenshots = b.cases.map(c => {
    a.equal(c.records, 7); a.equal(c.initialActive, 2); a.equal(c.atExpiryActive, 1);
    for (const k of ['exactBoundaryExpired', 'invalidDatesSafe', 'explicitIstTime', 'longOpenUpdatedWithoutRead', 'noWrites', 'noHorizontalOverflow']) a.equal(c[k], true);
    a.equal(c.pageErrors, 0);
    const bytes = read(browserPath + '/expiry-' + c.width + '-' + c.theme + '.png');
    a.equal(bytes.subarray(0, 8).toString('hex'), '89504e470d0a1a0a');
    return { width: c.width, theme: c.theme, sha256: hash(bytes) };
  });
  const w = json('api-runtime-window-' + windowId + '/result.json');
  for (const k of ['passed', 'runtimePassed', 'restorationPassed', 'inventoryPreserved']) a.equal(w[k], true);
  a.equal(w.uiRun, uiId); a.equal(w.webBuild, web.build); a.equal(w.runtimeExit, 0);
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
  a.equal(log.passed, true); a.equal(log.run, uiId); a.equal(log.webBuild, web.build); a.equal(log.exportFiles, 627);
  a.equal(fs.existsSync(path.join(root, '.build-check/qa-validation-window.lock')), false);
  console.log(JSON.stringify({ passed: true, scope: 'Access Review expiry presentation only', uiRun: uiId, browserRun: b.run, windowRun: windowId, webBuild: web.build, handlerChecks: 23, exportFiles: 627, browserJourneys: 4, screenshots, browserSha256: hash(read(browserPath + '/result.json')), windowSha256: hash(read('api-runtime-window-' + windowId + '/result.json')), apiResponseMocks: true, serverAuthorizationTested: false, physicalDevicesTested: false, enterpriseComplete: false, productionApproved: false }, null, 2));
}
if (require.main === module) try { run(); } catch (error) {
  console.error(JSON.stringify({ passed: false, scope: 'Access expiry proof incomplete or changed', type: error.name, sourceLines: [...String(error.stack || '').matchAll(/access-expiry-qualification\.cjs:(\d+):(\d+)/g)].map(m => ({ line: Number(m[1]), column: Number(m[2]) })) }));
  process.exitCode = 1;
}
