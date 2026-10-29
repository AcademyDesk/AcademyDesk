// Exported host UI, isolated synthetic API. No real accounts, SQL, AI or device acceptance.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const { assertManual, verifyVendor } = require('./penta-foundation-contract.cjs'); verifyVendor();
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'onboarding-states-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => { try {
  assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html';
  const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file)); if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
  file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' }); if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
} catch { res.writeHead(404).end(); } });
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser, currentPage;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    async function fixture(width, theme, kind, state) {
      const context = await browser.newContext({ viewport: { width, height: 800 } }), page = currentPage = await context.newPage(), calls = [], errors = [];
      page.on('pageerror', e => errors.push(e.message));
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-onboarding'); }, theme);
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url()), headers = { 'Access-Control-Allow-Origin': origin };
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          calls.push({ path: url.pathname, method: req.method() }); assert.equal(req.method(), 'GET', 'State checks must not mutate');
          if (url.pathname === '/api/academies') {
            // Keep loading visible long enough to test the disabled form, not just the final error.
            await new Promise(r => setTimeout(r, 500));
            return route.fulfill({ status: typeof state === 'number' ? state : 200, headers, contentType: 'application/json', body: JSON.stringify(state === 'empty' ? [] : state === 'malformed' ? [{ id: null }] : [{ id: 'owned', name: 'Synthetic academy', enabledModulesJson: '["Core"]' }]) });
          }
          const data = url.pathname === '/api/auth/session' ? { workspace: 'AcademyAdmin', academyId: 'owned', roles: ['AcademyAdmin'], isPlatformOwner: false, displayName: 'Synthetic admin' } : [];
          return route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(data) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/' + kind + '-onboarding'); await assertManual(page, theme);
      return { context, page, calls, errors };
    }
    for (const width of [320, 1440]) for (const theme of ['light', 'dark']) for (const kind of ['student', 'teacher']) for (const state of ['empty', 'malformed', 403, 503]) {
      const f = await fixture(width, theme, kind, state), main = f.page.locator('main.enterprise-settings');
      await main.getByRole('status').filter({ hasText: state === 'empty' ? 'Create an academy first.' : 'could not be loaded' }).waitFor();
      assert.equal(await main.locator('fieldset').evaluate(el => el.disabled), true); assert.equal(await main.locator('button[type="submit"], button.student-onboarding-submit, button.teacher-onboarding-submit').isDisabled(), true);
      assert.equal(await main.locator('input').first().isDisabled(), true); assert.ok(await f.page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)); assert.deepEqual(f.errors, []);
      checks.push({ width, theme, kind, state }); await f.context.close();
    }
    for (const width of [320, 768, 1440]) for (const theme of ['light', 'dark']) {
      const f = await fixture(width, theme, 'student', 'ready'), page = f.page, main = page.locator('main.enterprise-settings');
      await main.locator('fieldset:enabled').waitFor();
      const dob = main.locator('.standard-date-field').filter({ has: page.locator('input[name="dateOfBirth"]') });
      await dob.getByRole('button').click(); const calendar = dob.getByRole('dialog');
      await calendar.getByLabel('Select year').selectOption('2020'); await calendar.getByLabel('Select month').selectOption('0'); await calendar.locator('button[data-current="true"]').filter({ hasText: /^1$/ }).click();
      await main.getByText('Minor student: Parent details and a primary Parent link are required.', { exact: true }).waitFor();
      for (const name of ['parentFirstName', 'parentLastName', 'parentEmail']) assert.equal(await main.locator(`input[name="${name}"]`).getAttribute('required'), '');
      await main.locator('input[name="studentFirstName"]').fill('Synthetic'); await main.locator('input[name="studentLastName"]').fill('Minor');
      await main.getByRole('button', { name: 'Complete onboarding', exact: true }).click();
      assert.equal(await main.locator('input[name="parentFirstName"]').evaluate(el => el === document.activeElement), true, 'Required parent blocks submit');
      const permission = main.locator('input[name="allowParentPortalAccess"]'); assert.equal(await permission.isDisabled(), true); assert.equal(await permission.isChecked(), true, 'Minor grant reflects existing server rule');
      await page.screenshot({ path: path.join(evidence, `${width}-${theme}-student-minor.png`), fullPage: true }); assert.deepEqual(f.errors, []);
      async function changeBirthYear(year) {
        await dob.getByRole('button').click(); const picker = dob.getByRole('dialog'); await picker.getByLabel('Select year').selectOption(year); await picker.locator('button[data-current="true"]').filter({ hasText: /^1$/ }).click();
      }
      await changeBirthYear('2000'); assert.equal(await permission.isDisabled(), false); assert.equal(await permission.isChecked(), false, 'Automatic minor grant is not adult consent');
      await permission.check(); await changeBirthYear('2020'); assert.equal(await permission.isDisabled(), true); assert.equal(await permission.isChecked(), true);
      await changeBirthYear('2000'); assert.equal(await permission.isChecked(), true, 'Explicit adult consent retained across DOB correction');
      checks.push({ width, theme, kind: 'student', state: 'minor-parent-required' }); await f.context.close();
      const teacher = await fixture(width, theme, 'teacher', 'ready'), tp = teacher.page, tm = tp.locator('main.enterprise-settings'); await tm.locator('fieldset:enabled').waitFor();
      const time = tm.locator('.standard-time-field').first(); await time.getByRole('button').click(); const dialog = time.getByRole('dialog');
      assert.ok(await dialog.evaluate(el => { const b = el.getBoundingClientRect(); return el.matches(':popover-open') && b.left >= 0 && b.right <= innerWidth + 1 && b.top >= 0 && b.bottom <= innerHeight + 1; }), 'Time popup fits viewport/top layer');
      await dialog.locator('.standard-time-options').nth(0).getByRole('button', { name: '11', exact: true }).click();
      await dialog.getByRole('button', { name: 'Clear time', exact: true }).click(); assert.equal(await time.locator('input').inputValue(), ''); assert.equal(await time.getByRole('button').innerText(), 'Select time\n◷');
      await time.getByRole('button').click(); await tp.keyboard.press('Escape'); assert.equal(await time.getByRole('button').evaluate(el => el === document.activeElement), true);
      await tp.screenshot({ path: path.join(evidence, `${width}-${theme}-teacher.png`), fullPage: true }); assert.deepEqual(teacher.errors, []);
      checks.push({ width, theme, kind: 'teacher', state: 'time-clear-and-focus' }); await teacher.context.close();
    }
    assert.equal(checks.length, 44);
    const sources = Object.fromEntries(['apps/web/src/app/student-onboarding/page.tsx', 'apps/web/src/app/teacher-onboarding/page.tsx', 'apps/web/src/components/design-system/onboarding-field.tsx', 'apps/web/src/components/design-system/onboarding.module.css', 'apps/web/src/components/standard-select-field.tsx', 'apps/web/src/components/standard-time-field.tsx'].map(file => [file, crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex')]));
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, sources, transport: 'Synthetic API; no SQL/auth/model/physical-device acceptance' }, null, 2)); console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (e) { if (currentPage && !currentPage.isClosed()) await currentPage.screenshot({ path: path.join(evidence, 'failure.png'), fullPage: true }).catch(() => {}); fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, failure: e.message }, null, 2)); throw e; }
  finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(e => { console.error(e); process.exitCode = 1; server.close(); });
