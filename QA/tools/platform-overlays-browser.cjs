// Exported Platform Owner UI / synthetic read fixtures, not real RBAC or SQL.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'platform-overlays-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => { try {
  assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html';
  const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
  if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
  file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' }); if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
} catch { res.writeHead(404).end(); } });

const cases = [
  { route: '/platform', selector: '.platform-kpis', button: /^Total academies/, title: count => String(count), empty: 'No academies recorded yet.' },
  { tab: 'Tenants', selector: '.platform-tenant-kpis', button: /^Total tenants/, title: () => 'All tenants', empty: 'No tenants match this view.' },
  { tab: 'Admins', selector: '.platform-admin-kpis', button: /^Total administrators/, title: () => 'All academy administrators', empty: 'No administrator accounts match this view.' },
  { tab: 'Billing', selector: '.platform-billing-kpis', button: /^All invoices/, title: () => 'All platform invoices', empty: 'No invoices match this view.' },
  { tab: 'Support', selector: '.platform-support-kpis', button: /^All cases/, title: () => 'All support cases', empty: 'No support cases match this view.' },
  { tab: 'Announcements', selector: '.platform-announcement-kpis', button: /^Recent/, title: () => 'Recent announcements', empty: 'No announcements match this view.' },
  { tab: 'Audit & health', selector: '.platform-health-kpis', button: /^API/, title: () => 'API', health: true }
];
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 390, 768, 1440]) for (const theme of ['light', 'dark']) for (const count of [0, 40]) for (const item of cases) {
      const context = await browser.newContext({ viewport: { width, height: 700 }, reducedMotion: 'reduce' }), page = await context.newPage(), errors = [], writes = [];
      page.on('pageerror', error => errors.push(error.message));
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.Platform', 'synthetic-platform-token'); }, theme);
      const rows = Array.from({ length: count }, (_, index) => ({
        id: 'synthetic-' + index, name: 'Synthetic academy ' + index + ' long academy name for wrapping',
        countryCode: 'IN', timeZone: 'Asia/Kolkata', isActive: true, branches: 1, students: 2,
        subscriptionPlan: 'Growth', subscriptionStatus: 'Active', studentLimit: 50, staffLimit: 10, enabledModulesJson: '["Core"]',
        displayName: 'Synthetic administrator ' + index, email: 'synthetic-long-name-' + index + '@example.invalid', academyName: 'Synthetic academy',
        academyId: 'synthetic-0', title: 'Synthetic announcement ' + index, message: 'Long descriptive announcement text '.repeat(8),
        expiresAtUtc: '2099-01-01T00:00:00Z', subject: 'Synthetic support case ' + index, priority: 'Normal',
        status: 'Open', invoiceNumber: 'SYNTHETIC-INVOICE-' + index, currency: 'INR', amount: 1000,
        dueDate: '2099-01-01', periodStart: '2026-01-01', periodEnd: '2026-01-31'
      }));
      const data = {
        '/api/platform/academies': rows, '/api/platform/admins': rows, '/api/platform/support-cases': rows,
        '/api/platform/billing-invoices': rows, '/api/platform/announcements': rows,
        '/api/platform/overview': { openSupportCases: count, totalBilled: count * 1000, collectedBilling: 0, outstandingBilling: count * 1000, overdueInvoices: 0, recentAudit: [] },
        '/api/platform/health': { api: 'Healthy', database: 'Healthy', backgroundJobs: 'Healthy', communicationProviders: 'Not configured', maintenanceMode: false },
        '/api/platform/settings': { platformName: 'Synthetic AcademyDesk', defaultCurrency: 'INR', defaultTrialDays: 14, dataRetentionDays: 30, maintenanceMode: false },
        '/api/auth/session': { displayName: 'Synthetic owner', roles: ['PlatformOwner'], isPlatformOwner: true }
      };
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          if (!['GET', 'OPTIONS'].includes(req.method())) writes.push(req.method() + ' ' + url.pathname);
          return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': origin }, contentType: 'application/json', body: JSON.stringify(url.pathname.endsWith('/onboarding') ? { status: 'Draft', currentSection: 'Personal' } : data[url.pathname] || []) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); return route.abort('blockedbyclient');
      });
      await page.goto(origin + (item.route || '/platform/control?tab=' + encodeURIComponent(item.tab)));
      await page.getByRole('heading', { level: 1 }).waitFor();
      await page.waitForFunction(() => !document.body.textContent.includes('Loading platform controls…') && !document.body.textContent.includes('Loading academy portfolio…'));
      // Health uses a different list container from the financial/register tiles.
      const opener = page.locator(item.selector).getByRole('button', { name: item.button });
      await page.evaluate(() => { document.body.style.overflow = 'auto'; });
      await opener.click();
      const dialog = page.getByRole('dialog', { name: item.title(count), exact: true }); await dialog.waitFor();
      assert.equal(await dialog.evaluate(element => element.matches('dialog:modal')), true, 'native top layer');
      const close = dialog.getByRole('button', { name: 'Close details', exact: true }), panel = dialog.locator(':scope > article');
      assert.equal(await close.evaluate(element => element === document.activeElement), true, 'initial close focus');
      const box = await close.boundingBox(); assert.ok(box.width >= 44 && box.height >= 44, '44px close');
      const geometry = await panel.boundingBox(); assert.ok(geometry.x >= 0 && geometry.x + geometry.width <= width + 1 && geometry.y >= 0 && geometry.y + geometry.height <= 701, 'viewport bounded');
      assert.equal(await panel.evaluate(element => element.scrollWidth <= element.clientWidth + 1), true, 'no panel horizontal overflow');
      if (!item.health) {
        if (count) {
          assert.equal(await dialog.locator('li').count(), count, 'all source records retained');
          assert.equal(await dialog.locator('li small').evaluateAll(elements => elements.every(element => getComputedStyle(element).whiteSpace !== 'nowrap' && element.scrollWidth <= element.clientWidth + 1)), true, 'captions wrap without clipping');
        }
        else await dialog.getByText(item.empty, { exact: true }).waitFor();
      } else await dialog.getByText('Healthy', { exact: true }).waitFor();
      assert.equal(await page.evaluate(() => document.body.style.overflow), 'hidden');
      await opener.evaluate(element => element.focus());
      assert.equal(await dialog.evaluate(element => element.contains(document.activeElement)), true, 'background inert');
      if (count && !item.health) {
        await panel.evaluate(element => { element.scrollTop = element.scrollHeight; });
        assert.equal(await panel.evaluate(element => element.scrollTop > 0), true, 'long list scroll');
        const sticky = await close.boundingBox(); assert.ok(sticky.y >= geometry.y && sticky.y + sticky.height <= geometry.y + geometry.height, 'sticky close visible');
      }
      await page.keyboard.press('Escape'); await dialog.waitFor({ state: 'detached' });
      assert.equal(await opener.evaluate(element => element === document.activeElement), true, 'focus return');
      assert.equal(await page.evaluate(() => document.body.style.overflow), 'auto', 'scroll restore');
      await opener.click(); await dialog.waitFor(); await close.click(); await dialog.waitFor({ state: 'detached' });
      await opener.click(); await dialog.waitFor();
      await page.evaluate(() => { const cover = document.createElement('div'); cover.id = 'layer-probe'; cover.style.cssText = 'position:fixed;inset:0;z-index:2147483647;background:red'; document.body.append(cover); });
      assert.equal(await close.evaluate(element => { const rect = element.getBoundingClientRect(); return document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2) === element; }), true, 'above high z-index host');
      await page.locator('#layer-probe').evaluate(element => element.remove());
      if (width === 320 && count === 40 && ['Billing', 'Announcements'].includes(item.tab)) await page.screenshot({ path: path.join(evidence, theme + '-' + item.tab + '.png') });
      await page.mouse.click(4, 4); await dialog.waitFor({ state: 'detached' });
      assert.equal(await opener.evaluate(element => element === document.activeElement), true, 'backdrop focus return');
      const notifications = page.getByRole('button', { name: 'Platform notifications unavailable', exact: true });
      assert.equal(await notifications.isDisabled(), true, 'unsupported utility not actionable');
      if (item.tab === 'Billing') {
        await page.getByRole('button', { name: 'Search academy portfolio', exact: true }).click();
        await page.waitForURL(url => url.pathname === '/platform' && url.hash === '#academy-search');
        await page.locator('#academy-search').waitFor();
      }
      assert.deepEqual(errors, []); assert.deepEqual(writes, []);
      checks.push({ width, theme, count, tab: item.tab || 'Overview' }); console.log('PASS ' + [width, theme, count, item.tab || 'Overview'].join(' ')); await context.close();
    }
    assert.equal(checks.length, 112);
    const sourceHashes = Object.fromEntries(['apps/web/src/app/platform/page.tsx', 'apps/web/src/app/platform/control/page.tsx', 'apps/web/src/components/platform-navigation.module.css', 'apps/web/src/components/design-system/interactive.tsx'].map(file => [file, crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex')]));
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, sourceHashes, transport: 'Synthetic read fixtures / exported browser UI only; no SQL/RBAC/device acceptance' }, null, 2));
    console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (error) { fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, failure: error.message }, null, 2)); throw error; }
  finally { if (browser) await browser.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error => { console.error(error); process.exitCode = 1; server.close(); });
