// Exported Platform Owner UI / synthetic read fixtures, not real RBAC or SQL.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'platform-navigation-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => { try {
  assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html';
  const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
  if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
  file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' }); if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
} catch { res.writeHead(404).end(); } });
const destinations = ['Overview', 'Tenant Management', 'Tenant Onboarding', 'Tenant Information', 'Academy Admins', 'Billing', 'Support', 'Announcements', 'Activity logs', 'Settings', 'Audit & health'];
const sequence = ['Announcements', 'Academy Admins', 'Billing', 'Support', 'Activity logs', 'Settings', 'Audit & health', 'Tenant Information', 'Tenant Onboarding', 'Tenant Management', 'Overview'];
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 390, 768, 1440]) for (const theme of ['light', 'dark']) for (const initial of ['/platform', '/platform/control?tab=Admins']) {
      const context = await browser.newContext({ viewport: { width, height: 700 }, reducedMotion: 'reduce' }), page = await context.newPage(), errors = [], writes = [];
      page.on('pageerror', error => errors.push(error.message));
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.Platform', 'synthetic-platform-token'); }, theme);
      const academies = Array.from({ length: 15 }, (_, index) => ({ id: 'owned-' + index, name: 'Synthetic academy ' + index, countryCode: 'IN', timeZone: 'Asia/Kolkata', isActive: true, branches: 1, students: 0, subscriptionPlan: 'Growth', subscriptionStatus: 'Active', studentLimit: 50, staffLimit: 10, enabledModulesJson: '["Core"]' }));
      const data = { '/api/platform/academies': academies, '/api/platform/overview': { openSupportCases: 0, totalBilled: 0, collectedBilling: 0, outstandingBilling: 0, overdueInvoices: 0, recentAudit: [] }, '/api/platform/health': { api: 'Healthy', database: 'Healthy', backgroundJobs: 'Healthy', communicationProviders: 'Not configured', maintenanceMode: false }, '/api/platform/settings': { platformName: 'Synthetic AcademyDesk', defaultCurrency: 'INR', defaultTrialDays: 14, dataRetentionDays: 30, maintenanceMode: false }, '/api/auth/session': { displayName: 'Synthetic owner', roles: ['PlatformOwner'], isPlatformOwner: true } };
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          if (!['GET', 'OPTIONS'].includes(req.method())) writes.push(req.method() + ' ' + url.pathname);
          return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': origin }, contentType: 'application/json', body: JSON.stringify(url.pathname.endsWith('/onboarding') ? { status: 'Draft', currentSection: 'Personal' } : data[url.pathname] || []) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); return route.abort('blockedbyclient');
      });
      await page.goto(origin + initial); await page.getByRole('heading', { level: 1 }).waitFor();
      const assertHeader = async () => {
        const contextBox = await page.locator('.platform-topbar-context span').evaluate(element => { const range = document.createRange(); range.selectNodeContents(element); const rect = range.getBoundingClientRect(); return { x: rect.x, y: rect.y, width: rect.width, height: rect.height }; }), utilitiesBox = await page.locator('.platform-topbar .enterprise-utilities').boundingBox();
        assert.ok(contextBox.x + contextBox.width <= utilitiesBox.x + 1 || contextBox.y + contextBox.height <= utilitiesBox.y + 1 || utilitiesBox.y + utilitiesBox.height <= contextBox.y + 1, 'owner context does not overlap utilities');
        assert.equal(await page.locator('.platform-topbar-context span').evaluate(element => element.scrollWidth <= element.clientWidth + 1), true, 'role label is not clipped');
      };
      await assertHeader();
      const navigation = () => page.locator(width <= 900 ? '.platform-mobile-nav' : '.platform-nav');
      const labels = await navigation().locator('a,button').allTextContents();
      // Icons are intentionally not text in the final accessible names. The
      // baseline has glyph text; compare destination labels as suffixes here.
      assert.equal(labels.length, destinations.length, 'all eleven destinations present from either entry');
      for (const label of destinations) assert.ok(labels.some(text => text.trim().endsWith(label)), 'destination present: ' + label);
      for (const label of sequence) {
        const control = navigation().getByRole(label === 'Overview' || width <= 900 || new URL(page.url()).pathname === '/platform' ? 'link' : 'button', { name: label, exact: true });
        await control.focus(); const focusStyle = await control.evaluate(element => getComputedStyle(element).outlineStyle); assert.notEqual(focusStyle, 'none', 'visible keyboard focus');
        const box = await control.boundingBox(); assert.ok(box.width >= 44 && box.height >= 44, '44px navigation target');
        await page.keyboard.press('Enter');
        const expectedTab = { 'Tenant Management': 'Tenants', 'Tenant Onboarding': 'Tenant onboarding', 'Tenant Information': 'Tenant information', 'Academy Admins': 'Admins' }[label] || label;
        await page.waitForURL(url => label === 'Overview' ? url.pathname === '/platform' : url.pathname === '/platform/control' && url.searchParams.get('tab') === expectedTab);
        const title = label === 'Overview' ? 'Overview' : label;
        await page.getByRole('heading', { level: 1, name: title, exact: true }).waitFor();
        const current = navigation().locator('[aria-current="page"]'); assert.equal(await current.count(), 1, 'one current destination');
        const currentRole = label === 'Overview' || width <= 900 ? 'link' : 'button';
        assert.equal(await navigation().getByRole(currentRole, { name: label, exact: true }).getAttribute('aria-current'), 'page');
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true, 'no page horizontal overflow');
        await assertHeader();
      }
      await page.goBack();
      await page.getByRole('heading', { level: 1, name: 'Tenant Management', exact: true }).waitFor();
      assert.equal(await navigation().getByRole(width <= 900 ? 'link' : 'button', { name: 'Tenant Management', exact: true }).getAttribute('aria-current'), 'page', 'back synchronizes active section');
      await page.goForward(); await page.getByRole('heading', { level: 1, name: 'Overview', exact: true }).waitFor();
      // Existing scroll correction: phones scroll the document; desktop uses
      // its bounded content area. Fifteen portfolio records ensure real overflow.
      const scroller = width <= 900 ? page.locator('html') : page.locator('.platform-content');
      assert.equal(await scroller.evaluate(element => { const target = element === document.documentElement ? document.scrollingElement : element; target.scrollTop = target.scrollHeight; return target.scrollTop > 0; }), true, 'content can scroll');
      const overview = navigation().getByRole('link', { name: 'Overview', exact: true }); await overview.focus();
      if (width === 320) await page.screenshot({ path: path.join(evidence, theme + '-' + (initial.includes('control') ? 'control' : 'overview') + '.png') });
      assert.deepEqual(writes, []); assert.deepEqual(errors, []); checks.push({ width, theme, initial, destinations: sequence.length }); console.log('PASS ' + [width, theme, initial].join(' ')); await context.close();
    }
    const sourceHashes = Object.fromEntries(['apps/web/src/app/platform/page.tsx', 'apps/web/src/app/platform/control/page.tsx', 'apps/web/src/components/platform-navigation.ts', 'apps/web/src/components/platform-navigation.module.css'].map(file => [file, crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex')]));
    assert.equal(checks.length, 16); fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, sourceHashes, transport: 'Synthetic reads and exported UI; not SQL/authorization/device acceptance' }, null, 2)); console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (error) { fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, failure: error.message }, null, 2)); throw error; }
  finally { if (browser) await browser.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error => { console.error(error); process.exitCode = 1; server.close(); });
