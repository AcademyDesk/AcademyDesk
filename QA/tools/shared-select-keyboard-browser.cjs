'use strict';
// Actual exported controls, synthetic reads/token only. Never authorization/SQL evidence.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http');
const a = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..');
async function run() {
  a.equal(process.argv.length, 3);
  const ui = process.argv[2], verified = require('./shared-select-ui-export.cjs').verify(ui);
  const run = crypto.randomUUID().replaceAll('-', ''), evidence = path.join(root, 'QA/EVIDENCE/shared-select-browser-' + run);
  fs.mkdirSync(evidence, { recursive: true });
  const result = { run, ui, webBuild: verified.build, verifiedExportFiles: verified.verifiedExportFiles, passed: false, apiResponseMocks: true, syntheticToken: true, actualSql: false, serverAuthorizationTested: false, physicalDevicesTested: false, productionApproved: false, cases: [] };
  const save = () => fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify(result, null, 2)); save();
  const server = http.createServer((req, res) => {
    try {
      a.ok(['GET', 'HEAD'].includes(req.method));
      const file = require('./api-image-payment-browser.cjs').staticFile(verified.web, new URL(req.url, 'http://localhost').pathname);
      res.writeHead(200, { 'content-type': ({ '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.txt': 'text/plain', '.svg': 'image/svg+xml', '.woff2': 'font/woff2', '.ico': 'image/x-icon' })[path.extname(file)] || 'application/octet-stream' });
      if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
    } catch { res.writeHead(404).end(); }
  });
  await new Promise(r => server.listen(0, '127.0.0.1', r));
  const origin = 'http://127.0.0.1:' + server.address().port, academy = '11111111-1111-4111-8111-111111111111'; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 390, 768, 1440]) for (const theme of ['light', 'dark']) {
      const context = await browser.newContext({ viewport: { width, height: 900 }, reducedMotion: 'reduce' });
      const page = await context.newPage(), errors = [], writes = []; let stage = 'load';
      try {
        await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-select-keyboard-token'); }, theme);
        const people = ['Alice', 'Bob', 'Charlie', 'Chloe', 'Zulu'].map((name, i) => ({ id: 'synthetic-' + i, displayName: name, email: name.toLowerCase() + '@example.invalid', roles: ['Operations'], isActive: true }));
        const data = { '/api/auth/session': { academyId: academy, displayName: 'Synthetic keyboard reviewer', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/api/academies': [{ id: academy, name: 'Synthetic Keyboard Academy', enabledModulesJson: '["Core","AccessGovernance"]' }], '/api/portal/announcements': [], ['/api/academies/' + academy + '/staff']: people, ['/api/academies/' + academy + '/access-grants']: [] };
        page.on('pageerror', () => errors.push('uncaught control error'));
        await context.route('**/*', async route => {
          const req = route.request(), u = new URL(req.url());
          if (u.pathname.startsWith('/api/')) {
            if (!['localhost', '127.0.0.1'].includes(u.hostname)) return route.abort();
            if (req.method() !== 'GET') { writes.push(req.method()); return route.abort(); }
            return route.fulfill({ status: u.pathname.endsWith('/chat/health') ? 503 : 200, contentType: 'application/json', headers: { 'access-control-allow-origin': origin }, body: JSON.stringify(data[u.pathname] ?? []) });
          }
          if (u.origin === origin || u.protocol === 'data:') return route.continue();
          errors.push('external request refused'); return route.abort();
        });
        await page.goto(origin + '/access-review');
        const field = page.locator('.standard-select-field').filter({ has: page.locator('input[name="userId"]') });
        const trigger = field.getByRole('button'), hidden = field.locator('input[name="userId"]'), menu = page.getByRole('listbox', { name: 'Select staff member', exact: true });
        const focused = async label => { await page.waitForFunction(label => document.activeElement?.getAttribute('role') === 'option' && document.activeElement.textContent === label, label); };
        const selected = async value => { a.equal(await hidden.inputValue(), value); };
        await page.waitForFunction(() => document.querySelector('.access-review-fields')?.disabled === false);
        stage = 'initial focus and navigation'; await trigger.focus(); await page.keyboard.press('ArrowDown');
        await menu.waitFor(); await focused('Select staff member');
        a.equal(await menu.getByRole('option').count(), 6);
        a.ok(await menu.getByRole('option').evaluateAll(options => options.every(option => option.tabIndex === -1)));
        await page.keyboard.press('End'); await focused('Zulu · Operations'); await selected('');
        await page.keyboard.press('ArrowUp'); await focused('Chloe · Operations');
        await page.keyboard.press('Home'); await focused('Select staff member');
        await page.keyboard.press('ArrowDown'); await focused('Alice · Operations');
        stage = 'type-ahead and Enter'; await page.keyboard.press('b'); await focused('Bob · Operations'); await selected('');
        await page.keyboard.press('Enter'); await menu.waitFor({ state: 'detached' }); await selected('synthetic-1');
        a.equal(await trigger.evaluate(e => e === document.activeElement), true);
        await page.getByRole('button', { name: 'Staff member Bob · Operations', exact: true }).waitFor();
        stage = 'selected focus and Space'; await page.keyboard.press('ArrowUp'); await menu.waitFor(); await focused('Bob · Operations');
        await page.keyboard.press('ArrowDown'); await focused('Charlie · Operations'); await page.keyboard.press('Space');
        await menu.waitFor({ state: 'detached' }); await selected('synthetic-2');
        stage = 'Escape cancellation'; await trigger.click(); await focused('Charlie · Operations');
        await page.keyboard.press('c'); await focused('Chloe · Operations'); await page.keyboard.press('Escape');
        await menu.waitFor({ state: 'detached' }); await selected('synthetic-2'); a.equal(await trigger.evaluate(e => e === document.activeElement), true);
        stage = 'native Tab exit'; await page.keyboard.press('ArrowDown'); await menu.waitFor(); await page.keyboard.press('Tab'); await menu.waitFor({ state: 'detached' });
        a.equal(await page.getByLabel('Sales & marketing', { exact: true }).evaluate(e => e === document.activeElement), true); await selected('synthetic-2');
        stage = 'native reverse Tab exit'; await trigger.focus(); await page.keyboard.press('ArrowDown'); await menu.waitFor(); await page.keyboard.press('Shift+Tab'); await menu.waitFor({ state: 'detached' });
        a.equal(await page.getByRole('button', { name: 'Refresh access controls', exact: true }).evaluate(e => e === document.activeElement), true); await selected('synthetic-2');
        stage = 'layer containment and pointer parity'; await trigger.click(); await menu.waitFor();
        const geometry = await menu.boundingBox(), button = await trigger.boundingBox();
        a.ok(geometry.x >= 0 && geometry.x + geometry.width <= width + 1 && geometry.y >= 0 && geometry.y + geometry.height <= 901);
        a.ok(geometry.y >= button.y + button.height - 1 || geometry.y + geometry.height <= button.y + 1);
        a.equal(await menu.evaluate(e => { const r = e.getBoundingClientRect(); const hit = document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2); return e.contains(hit); }), true);
        // Capture the actual viewport; full-page capture can scroll/resize the
        // host and legitimately dismiss a popup anchored to an offscreen trigger.
        await page.screenshot({ path: path.join(evidence, 'select-' + width + '-' + theme + '.png') });
        a.equal(await menu.isVisible(), true, 'viewport capture preserves the live popup');
        await menu.getByRole('option', { name: 'Alice · Operations', exact: true }).click(); await menu.waitFor({ state: 'detached' }); await selected('synthetic-0');
        await trigger.click(); await menu.waitFor(); await page.getByRole('heading', { name: 'Grant function access', exact: true }).click(); await menu.waitFor({ state: 'detached' }); await selected('synthetic-0');
        a.deepEqual(errors, []); a.deepEqual(writes, []); a.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false);
        result.cases.push({ width, theme, options: 6, selectedFocus: true, arrowHomeEnd: true, typeAhead: true, enterAndSpace: true, tabAndReverseTab: true, escapeRestoresFocus: true, pointerParity: true, topLayerContained: true, noImplicitChange: true, noWrites: true, pageErrors: 0 }); save();
      } catch (error) {
        result.failedCase = { width, theme, stage, type: error.name, sourceLines: [...String(error.stack || '').matchAll(/shared-select-keyboard-browser\.cjs:(\d+):(\d+)/g)].map(m => ({ line: Number(m[1]), column: Number(m[2]) })) };
        await page.screenshot({ path: path.join(evidence, 'failed.png'), fullPage: true }).catch(() => {}); throw error;
      } finally { await context.close(); }
    }
    a.equal(result.cases.length, 8); require('./shared-select-ui-export.cjs').verify(ui); result.passed = true;
  } catch { result.failure = 'Synthetic shared-select keyboard check failed; retain source and screenshot.'; process.exitCode = 1; }
  finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); save(); console.log(JSON.stringify({ run, passed: result.passed, journeys: result.cases.length, evidence, syntheticReads: true, serverAuthorizationTested: false, productionApproved: false })); }
}
if (require.main === module) run().catch(() => { console.error('Shared-select browser preflight failed; no live API changes.'); process.exitCode = 1; });
