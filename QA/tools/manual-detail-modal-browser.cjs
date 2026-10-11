// Real exported UI, synthetic read fixtures only. No SQL/auth/device acceptance.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'manual-detail-modal-' + Date.now());
fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => {
  try {
    assert.ok(['GET', 'HEAD'].includes(req.method));
    let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
    file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep));
    res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
function contrast(foreground, background) {
  const luminance = color => {
    const channels = color.match(/[\d.]+/g).slice(0, 3).map(Number).map(value => {
      value /= 255; return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
    });
    return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
  };
  const a = luminance(foreground), b = luminance(background);
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = 'http://127.0.0.1:' + server.address().port, checks = [];
  let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 390, 768, 1440]) for (const theme of ['light', 'dark']) for (const kind of ['student', 'teacher']) for (const count of [0, 40]) {
      const context = await browser.newContext({ viewport: { width, height: 700 }, reducedMotion: 'reduce' });
      const page = await context.newPage(), errors = [], writes = [];
      page.on('pageerror', error => errors.push(error.message));
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-modal-token'); }, theme);
      const records = Array.from({ length: count }, (_, index) => ({ id: 'synthetic-' + index, firstName: 'Synthetic' + index, lastName: index === 0 ? 'LongName'.repeat(25) : 'Person', isActive: true }));
      const summary = { totalStudents: count, activeStudents: count, inactiveStudents: 0, totalAdmissionFees: 0, totalSubjectFees: 0, outstandingFees: 0, overdueFees: 0, activeSubjectFeeArrangements: 0 };
      const data = { '/api/academies': [{ id: 'owned', name: 'Synthetic academy', enabledModulesJson: '["Core"]' }], '/api/auth/session': { academyId: 'owned', displayName: 'Synthetic admin', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/api/portal/announcements': [], ['/api/academies/owned/' + kind + 's']: records, '/api/academies/owned/students/overview': summary };
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          if (!['GET', 'OPTIONS'].includes(req.method())) writes.push(req.method() + ' ' + url.pathname);
          return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': origin }, contentType: 'application/json', body: JSON.stringify(data[url.pathname] || []) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue();
        return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/' + kind + '-overview');
      const opener = page.getByRole('button', { name: new RegExp('^Total ' + kind + 's') });
      await page.evaluate(() => { document.body.style.overflow = 'auto'; });
      await opener.click();
      const dialog = page.getByRole('dialog', { name: kind === 'student' ? 'Students' : 'Teachers', exact: true });
      await dialog.waitFor();
      assert.equal(await dialog.evaluate(element => element.matches('dialog:modal')), true, 'native top-layer dialog');
      const close = dialog.getByRole('button', { name: 'Close details', exact: true });
      assert.equal(await close.evaluate(element => element === document.activeElement), true, 'initial close focus');
      const bounds = await close.boundingBox(); assert.ok(bounds.width >= 44 && bounds.height >= 44, '44px close target');
      const colors = await close.evaluate(element => { const style = getComputedStyle(element); return { foreground: style.color, background: style.backgroundColor }; });
      assert.ok(contrast(colors.foreground, colors.background) >= 4.5, 'close text contrast');
      const panel = dialog.locator(':scope > article');
      const geometry = await panel.boundingBox(); assert.ok(geometry.x >= 0 && geometry.x + geometry.width <= width + 1 && geometry.y >= 0 && geometry.y + geometry.height <= 701, 'viewport-contained panel');
      assert.equal(await panel.evaluate(element => element.scrollWidth <= element.clientWidth + 1), true, 'no horizontal panel overflow');
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true, 'no horizontal page overflow');
      const originalOverflow = await page.evaluate(() => document.body.style.overflow);
      assert.equal(originalOverflow, 'hidden', 'background scroll locked');
      await opener.evaluate(element => element.focus());
      assert.equal(await dialog.evaluate(element => element.contains(document.activeElement)), true, 'background cannot take focus');
      for (const key of ['Tab', 'Shift+Tab']) {
        await close.focus(); await page.keyboard.press(key);
        // The browser may put focus in its own chrome (body activeElement), but
        // no background application control may become focused.
        assert.equal(await dialog.evaluate(element => element.contains(document.activeElement) || document.activeElement === document.body), true, 'no background keyboard focus');
      }
      if (count) {
        await panel.evaluate(element => { element.scrollTop = element.scrollHeight; });
        assert.equal(await panel.evaluate(element => element.scrollTop > 0), true, 'long content scrolls');
        const scrolledClose = await close.boundingBox();
        assert.ok(scrolledClose.y >= geometry.y && scrolledClose.y + scrolledClose.height <= geometry.y + geometry.height, 'sticky close stays visible');
        await close.click();
      } else {
        await dialog.getByText('No ' + kind + 's found.', { exact: true }).waitFor();
        await page.keyboard.press('Escape');
      }
      await dialog.waitFor({ state: 'detached' });
      assert.equal(await opener.evaluate(element => element === document.activeElement), true, 'focus returned');
      assert.equal(await page.evaluate(() => document.body.style.overflow), 'auto', 'prior scroll style restored');
      await opener.click(); await dialog.waitFor();
      // A deliberately high-z-index host sibling must not cover the top layer.
      await page.evaluate(() => { const cover = document.createElement('div'); cover.id = 'synthetic-layer-probe'; cover.style.cssText = 'position:fixed;inset:0;z-index:2147483647;background:red'; document.body.append(cover); });
      assert.equal(await close.evaluate(element => { const r = element.getBoundingClientRect(); return document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2) === element; }), true, 'top layer beats host stacking');
      await page.locator('#synthetic-layer-probe').evaluate(element => element.remove());
      if (width === 320 && count === 40) await page.screenshot({ path: path.join(evidence, theme + '-' + kind + '.png') });
      await page.mouse.click(4, 4); await dialog.waitFor({ state: 'detached' });
      assert.equal(await opener.evaluate(element => element === document.activeElement), true, 'backdrop close returns focus');
      assert.deepEqual(writes, []); assert.deepEqual(errors, []);
      checks.push({ width, theme, kind, count }); console.log('PASS ' + [width, theme, kind, count].join(' '));
      await context.close();
    }
    assert.equal(checks.length, 32);
    const sourceHashes = Object.fromEntries(['apps/web/src/components/design-system/interactive.tsx', 'apps/web/src/components/design-system/detail-modal.module.css'].map(file => [file, crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex')]));
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, sourceHashes, transport: 'Exported Admin overview UI / synthetic read data only; not SQL, model, screen-reader or device acceptance' }, null, 2));
    console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (error) {
    fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, failure: error.message }, null, 2)); throw error;
  } finally { if (browser) await browser.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error => { console.error(error); process.exitCode = 1; server.close(); });
