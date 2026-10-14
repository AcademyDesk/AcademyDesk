// Exported Platform Owner UI / synthetic read fixtures, not real RBAC or SQL.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {verifyVendor,assertManual}=require('./penta-foundation-contract.cjs');verifyVendor();
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
    for (const [width, height] of [[320, 700], [390, 700], [768, 700], [1024, 700], [1440, 700], [768, 360]]) for (const theme of ['light', 'dark']) for (const initial of ['/platform', '/platform/control?tab=Admins']) {
      const context = await browser.newContext({ viewport: { width, height }, reducedMotion: 'reduce' }), page = await context.newPage(), errors = [], writes = [];
      page.on('pageerror', error => errors.push(error.message));
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.Platform', 'synthetic-platform-token'); }, theme);
      const academies = Array.from({ length: 15 }, (_, index) => ({ id: 'owned-' + index, name: 'Synthetic academy ' + index, countryCode: 'IN', timeZone: 'Asia/Kolkata', isActive: true, branches: 1, students: 0, subscriptionPlan: 'Growth', subscriptionStatus: 'Active', studentLimit: 50, staffLimit: 10, enabledModulesJson: '["Core"]' }));
      const ownerName = 'Synthetic Platform Owner with an intentionally long display name for wrapping';
      const ownerEmail = 'synthetic-owner-' + 'long-'.repeat(24) + '@example.invalid';
      const data = { '/api/platform/academies': academies, '/api/platform/overview': { openSupportCases: 0, totalBilled: 0, collectedBilling: 0, outstandingBilling: 0, overdueInvoices: 0, recentAudit: [] }, '/api/platform/health': { api: 'Healthy', database: 'Healthy', backgroundJobs: 'Healthy', communicationProviders: 'Not configured', maintenanceMode: false }, '/api/platform/settings': { platformName: 'Synthetic AcademyDesk', defaultCurrency: 'INR', defaultTrialDays: 14, dataRetentionDays: 30, maintenanceMode: false }, '/api/auth/session': { displayName: ownerName, email: ownerEmail, roles: ['PlatformOwner'], isPlatformOwner: true } };
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          if (!['GET', 'OPTIONS'].includes(req.method())) writes.push(req.method() + ' ' + url.pathname);
          return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': origin }, contentType: 'application/json', body: JSON.stringify(url.pathname.endsWith('/onboarding') ? { status: 'Draft', currentSection: 'Personal' } : data[url.pathname] || []) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); return route.abort('blockedbyclient');
      });
      await page.goto(origin + initial); await page.getByRole('heading', { level: 1 }).waitFor();
      await assertManual(page,theme);
      const assertHeader = async () => {
        const contextBox = await page.locator('.platform-topbar-context span').evaluate(element => { const range = document.createRange(); range.selectNodeContents(element); const rect = range.getBoundingClientRect(); return { x: rect.x, y: rect.y, width: rect.width, height: rect.height }; }), utilitiesBox = await page.locator('.platform-topbar .enterprise-utilities').boundingBox();
        assert.ok(contextBox.x + contextBox.width <= utilitiesBox.x + 1 || contextBox.y + contextBox.height <= utilitiesBox.y + 1 || utilitiesBox.y + utilitiesBox.height <= contextBox.y + 1, 'owner context does not overlap utilities');
        assert.equal(await page.locator('.platform-topbar-context span').evaluate(element => element.scrollWidth <= element.clientWidth + 1), true, 'role label is not clipped');
      };
      await assertHeader();
      await page.waitForFunction(name => document.querySelector('.platform-topbar-context strong')?.textContent === name, ownerName);
      const utilities = page.locator('.platform-topbar .enterprise-utilities');
      const utilityTargets = utilities.locator(':scope > button, :scope > .theme-toggle button, :scope > .platform-profile > summary');
      assert.equal(await utilityTargets.evaluateAll(elements => elements.every(element => {
        const rect = element.getBoundingClientRect(); return rect.width >= 44 && rect.height >= 44;
      })), true, 'all utility/theme/profile targets at least44px');
      if (width <= 640) assert.equal(await utilityTargets.evaluateAll(elements => {
        const centres = elements.map(element => { const rect = element.getBoundingClientRect(); return rect.y + rect.height / 2; });
        return Math.max(...centres) - Math.min(...centres) <= 2;
      }), true, 'phone utilities fit one row without sacrificing target sizes');
      const ownerText = page.locator('.platform-topbar-context strong');
      assert.equal(await ownerText.textContent(), ownerName, 'full owner name retained');
      assert.equal(await ownerText.evaluate(element => element.scrollWidth <= element.clientWidth + 1 && getComputedStyle(element).whiteSpace !== 'nowrap'), true, 'owner name wraps without clipping');
      // Native summary exposes the same accessible name but its role differs by browser.
      const profileSummary = page.locator('.platform-profile summary');
      await profileSummary.click();
      const profileMenu = page.locator('.platform-profile-menu');
      const menuRect = await profileMenu.boundingBox();
      assert.ok(menuRect.x >= 0 && menuRect.x + menuRect.width <= width + 1, 'profile menu inside horizontal viewport');
      assert.equal(await profileMenu.evaluate(element => element.scrollWidth <= element.clientWidth + 1), true, 'long profile text wraps');
      const signOut = profileMenu.getByRole('button', { name: 'Sign out', exact: true });
      await signOut.scrollIntoViewIfNeeded();
      assert.equal(await signOut.evaluate(element => { const rect = element.getBoundingClientRect(); return document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2) === element; }), true, 'profile action is not behind content');
      await profileSummary.click();
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true, 'long profile does not widen page');
      const navigation = () => page.locator(width <= 900 ? '.platform-mobile-nav' : '.platform-nav');
      const openNavigation = async () => {
        if (width > 900) return;
        const summary = navigation().locator('summary');
        assert.equal(await navigation().locator('details').getAttribute('open'), null, 'chooser starts collapsed');
        const closed = await navigation().boundingBox(); assert.ok(closed.height <= 100, 'compact collapsed navigation');
        await page.keyboard.press('Tab'); await summary.focus(); assert.notEqual(await summary.evaluate(element => getComputedStyle(element).outlineStyle), 'none', 'chooser keyboard focus');
        await page.keyboard.press('Enter');
        assert.notEqual(await navigation().locator('details').getAttribute('open'), null, 'keyboard opens chooser');
        const menu = navigation().locator('summary + div');
        assert.equal(await menu.evaluate(element => { element.scrollTop = element.scrollHeight; return element.scrollTop > 0; }), true, 'all sections available through bounded list scrolling');
        assert.ok((await menu.boundingBox()).height <= height * .55 + 1, 'list bounded on short viewports');
        assert.equal(await menu.evaluate(element => element.scrollWidth <= element.clientWidth + 1), true, 'list wraps without horizontal clipping');
      };
      if (width <= 900) {
        await openNavigation(); await page.keyboard.press('Escape');
        assert.equal(await navigation().locator('details').getAttribute('open'), null, 'Escape collapses chooser');
        assert.equal(await navigation().locator('summary').evaluate(element => element === document.activeElement), true, 'Escape returns focus');
        await openNavigation(); await page.locator('.platform-topbar-context').click();
        assert.equal(await navigation().locator('details').getAttribute('open'), null, 'outside click collapses chooser');
        if (width === 320) {
          await page.screenshot({ path: path.join(evidence, theme + '-collapsed-' + (initial.includes('control') ? 'control' : 'overview') + '.png') });
          await openNavigation(); await navigation().locator('summary + div').evaluate(element => { element.scrollTop = 0; });
          await page.screenshot({ path: path.join(evidence, theme + '-expanded-' + (initial.includes('control') ? 'control' : 'overview') + '.png') });
          await page.keyboard.press('Escape');
        }
      }
      const labels = await navigation().locator('a,button').allTextContents();
      // Icons are intentionally not text in the final accessible names. The
      // baseline has glyph text; compare destination labels as suffixes here.
      assert.equal(labels.length, destinations.length, 'all eleven destinations present from either entry');
      for (const label of destinations) assert.ok(labels.some(text => text.trim().endsWith(label)), 'destination present: ' + label);
      for (const label of sequence) {
        await openNavigation();
        const control = navigation().getByRole(label === 'Overview' || width <= 900 || new URL(page.url()).pathname === '/platform' ? 'link' : 'button', { name: label, exact: true });
        await page.keyboard.press('Tab'); await control.focus(); const focusStyle = await control.evaluate(element => getComputedStyle(element).outlineStyle); assert.notEqual(focusStyle, 'none', 'visible keyboard focus');
        const box = await control.boundingBox(); assert.ok(box.width >= 44 && box.height >= 44, '44px navigation target');
        await page.keyboard.press('Enter');
        const expectedTab = { 'Tenant Management': 'Tenants', 'Tenant Onboarding': 'Tenant onboarding', 'Tenant Information': 'Tenant information', 'Academy Admins': 'Admins' }[label] || label;
        await page.waitForURL(url => label === 'Overview' ? url.pathname === '/platform' : url.pathname === '/platform/control' && url.searchParams.get('tab') === expectedTab);
        const title = label === 'Overview' ? 'Overview' : label;
        await page.getByRole('heading', { level: 1, name: title, exact: true }).waitFor();
        if (width <= 900) {
          assert.equal(await navigation().locator('details').getAttribute('open'), null, 'selection collapses navigation');
          assert.equal(await navigation().locator('summary').locator('span').nth(1).textContent(), label, 'current section label updates');
        }
        const current = navigation().locator('[aria-current="page"]'); assert.equal(await current.count(), 1, 'one current destination');
        const currentRole = label === 'Overview' || width <= 900 ? 'link' : 'button';
        if (width <= 900) assert.equal(await current.locator('span').textContent(), label, 'collapsed source link matches selected destination');
        else assert.equal(await navigation().getByRole(currentRole, { name: label, exact: true }).getAttribute('aria-current'), 'page');
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true, 'no page horizontal overflow');
        await assertHeader();
      }
      await page.goBack();
      await page.getByRole('heading', { level: 1, name: 'Tenant Management', exact: true }).waitFor();
      if (width <= 900) {
        assert.equal(await navigation().locator('[aria-current="page"] span').textContent(), 'Tenant Management', 'back synchronizes collapsed active section');
        assert.equal(await navigation().locator('summary span').nth(1).textContent(), 'Tenant Management', 'back synchronizes visible label');
      } else assert.equal(await navigation().getByRole('button', { name: 'Tenant Management', exact: true }).getAttribute('aria-current'), 'page', 'back synchronizes active section');
      await page.goForward(); await page.getByRole('heading', { level: 1, name: 'Overview', exact: true }).waitFor();
      // Existing scroll correction: phones scroll the document; desktop uses
      // its bounded content area. Fifteen portfolio records ensure real overflow.
      const scroller = width <= 900 ? page.locator('html') : page.locator('.platform-content');
      const searchField = page.locator('#academy-search');
      assert.equal(await searchField.evaluate(element => element.getBoundingClientRect().height >= 44), true, 'Manual field44px');
      if (width <= 900) assert.equal(await searchField.evaluate(element => getComputedStyle(element).fontSize), '16px', 'phone field text16px');
      const primary = page.getByRole('button', { name: 'Onboard academy', exact: false });
      const contrast = await primary.evaluate(element => {
        const luminance = value => {
          const channels = value.match(/[\d.]+/g).slice(0, 3).map(Number).map(channel => channel / 255).map(channel => channel <= .04045 ? channel / 12.92 : ((channel + .055) / 1.055) ** 2.4);
          return .2126 * channels[0] + .7152 * channels[1] + .0722 * channels[2];
        };
        const style = getComputedStyle(element), text = luminance(style.color), fill = luminance(style.backgroundColor);
        return (Math.max(text, fill) + .05) / (Math.min(text, fill) + .05);
      });
      assert.ok(contrast >= 4.5, 'actual primary action normal-text contrast');
      assert.equal(await scroller.evaluate(element => { const target = element === document.documentElement ? document.scrollingElement : element; target.scrollTop = target.scrollHeight; return target.scrollTop > 0; }), true, 'content can scroll');
      if (width > 900) await navigation().getByRole('link', { name: 'Overview', exact: true }).focus();
      if (width === 320) await page.screenshot({ path: path.join(evidence, theme + '-' + (initial.includes('control') ? 'control' : 'overview') + '.png') });
      assert.deepEqual(writes, []); assert.deepEqual(errors, []); checks.push({ width, height, theme, initial, destinations: sequence.length }); console.log('PASS ' + [width, height, theme, initial].join(' ')); await context.close();
    }
    const sourceHashes = Object.fromEntries(['apps/web/src/app/platform/page.tsx', 'apps/web/src/app/platform/control/page.tsx', 'apps/web/src/components/platform-navigation.ts', 'apps/web/src/components/platform-navigation.module.css', 'apps/web/src/components/platform-mobile-navigation.tsx', 'apps/web/src/components/platform-mobile-navigation.module.css', 'apps/web/src/components/manual-workspace.module.css'].map(file => [file, crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex')]));
    assert.equal(checks.length, 24); fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, sourceHashes, transport: 'Synthetic reads and exported UI; not SQL/authorization/device acceptance' }, null, 2)); console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (error) { fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, failure: error.message }, null, 2)); throw error; }
  finally { if (browser) await browser.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error => { console.error(error); process.exitCode = 1; server.close(); });
