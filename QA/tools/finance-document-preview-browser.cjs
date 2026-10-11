// Read-only exported application DOM; synthetic transport, never real academy data.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'finance-document-preview-' + Date.now());
fs.mkdirSync(evidence, { recursive: true });
const server = http.createServer((req, res) => {
  try {
    assert.ok(['GET', 'HEAD'].includes(req.method));
    let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
    file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep));
    res.writeHead(200, { 'Content-Type': ({ '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.txt': 'text/plain', '.svg': 'image/svg+xml', '.woff2': 'font/woff2' })[path.extname(file)] || 'application/octet-stream' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
const image = 'data:image/svg+xml,' + encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="200" height="60"><rect width="200" height="60" fill="#0e7490"/></svg>');
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r));
  const origin = 'http://127.0.0.1:' + server.address().port, checks = [], failures = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 390, 768, 1440]) for (const mode of ['light', 'dark']) for (const branding of ['default', 'long']) {
      const context = await browser.newContext({ viewport: { width, height: 1000 } }), page = await context.newPage(), errors = [], calls = [];
      const name = branding === 'long' ? 'SyntheticAuthorisedSignatory'.repeat(5) : 'Authorised signatory';
      const title = branding === 'long' ? 'Synthetic finance designation '.repeat(5).trim() : 'Finance';
      const settings = { taxRegistrationNumber: 'Synthetic', taxLabel: 'GST', taxRatePercent: 18, defaultPaymentTermsDays: 7, taxInclusivePricing: false, invoiceLogoUrl: branding === 'long' ? image : null, invoiceSignatureUrl: branding === 'long' ? image : null, invoiceAuthorityName: name, invoiceAuthorityTitle: title, invoiceTemplateKey: 'Classic', payslipTemplateKey: 'Standard', settingsVersion: 'A'.repeat(64) };
      page.on('pageerror', e => errors.push(e.message)); page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
      await context.addInitScript(mode => { localStorage.setItem('academydesk.theme', mode); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-preview-token'); }, mode);
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          calls.push({ method: req.method(), path: url.pathname }); assert.equal(req.method(), 'GET');
          const fixtures = { '/api/academies': [{ id: 'owned', name: 'Synthetic academy', enabledModulesJson: '["Core","Finance","FinanceControls"]' }], '/api/auth/session': { academyId: 'owned', displayName: 'Synthetic admin', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/api/portal/announcements': [], '/api/academies/owned/finance-governance/settings': settings, '/api/academies/owned/finance-adjustments': [], '/api/academies/owned/finance-governance/collections': [], '/api/academies/owned/finance-governance/collection-tasks': [] };
          assert.ok(Object.hasOwn(fixtures, url.pathname), url.pathname);
          return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': origin }, contentType: 'application/json', body: JSON.stringify(fixtures[url.pathname]) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue();
        errors.push('Unexpected network: ' + url.origin); return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/finance-governance'); await page.locator('.document-full-preview').waitFor(); await page.evaluate(() => document.fonts.ready);
      // Counterfactual CSS-only reproduction, not a frozen full application build.
      // Remove only this repair's scoped rules; retain identical DOM/assertions.
      if (process.env.QA_PREVIEW_WITHOUT_REPAIR === '1') await page.evaluate(() => {
        let removed = 0;
        const strip = sheet => { for (let i = sheet.cssRules.length - 1; i >= 0; i--) { const rule = sheet.cssRules[i]; if (rule.selectorText?.startsWith('.finance-control-documents .document-full-preview')) { sheet.deleteRule(i); removed++; } else if (rule.cssRules) strip(rule); } };
        for (const sheet of document.styleSheets) strip(sheet);
        if (removed !== 15) throw new Error('Unexpected repair rule count: ' + removed);
      });
      for (const [kind, themes] of [['Invoice', ['Classic', 'Modern', 'Minimal', 'Formal']], ['Payslip', ['Standard', 'Compact', 'Professional']]]) {
        await page.locator('.finance-control-documents select').nth(0).selectOption(kind);
        for (const theme of themes) {
          await page.locator('.finance-control-documents select').nth(1).selectOption(theme);
          const preview = page.locator('.document-full-preview'); await preview.scrollIntoViewIfNeeded();
          await page.waitForFunction(theme => document.querySelector('.document-full-preview').classList.contains('document-template-' + theme.toLowerCase()), theme);
          const geometry = await preview.evaluate(el => {
            const box = el.getBoundingClientRect(), problems = [], bounds = r => r.left >= box.left - 1 && r.right <= box.right + 1;
            for (const child of el.querySelectorAll('*')) if (!bounds(child.getBoundingClientRect())) problems.push('element:' + child.tagName);
            const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
            while (walker.nextNode()) { if (!walker.currentNode.textContent.trim()) continue; const range = document.createRange(); range.selectNodeContents(walker.currentNode); for (const rect of range.getClientRects()) if (!bounds(rect)) problems.push('text:' + walker.currentNode.textContent); }
            for (const selector of ['header', '.document-full-preview-body']) {
              const children = [...el.querySelector(selector).children].map(x => x.getBoundingClientRect());
              children.forEach((a, i) => children.slice(i + 1).forEach(b => { if (Math.min(a.right, b.right) - Math.max(a.left, b.left) > 1 && Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) > 1) problems.push('overlap:' + selector); }));
            }
            return { problems, scrollWidth: el.scrollWidth, clientWidth: el.clientWidth, pageOverflow: document.documentElement.scrollWidth > innerWidth + 1 };
          });
          assert.equal(await preview.locator('footer strong').textContent(), name); assert.equal(await preview.locator('footer span').textContent(), title);
          assert.equal(await preview.locator('header > b').textContent(), kind === 'Invoice' ? 'INV-2026-001' : 'PS-2026-001');
          assert.equal(await preview.locator('.document-full-preview-total strong').textContent(), kind === 'Invoice' ? '₹2,500' : '₹18,000');
          const result = { width, mode, branding, kind, theme, geometry };
          if (geometry.problems.length || geometry.scrollWidth > geometry.clientWidth + 1 || geometry.pageOverflow) failures.push(result); else checks.push(result);
          if (width === 320 && branding === 'long' && ['Modern', 'Professional'].includes(theme)) await preview.screenshot({ path: path.join(evidence, `${kind}-${theme}-${mode}.png`) });
        }
      }
      assert.deepEqual(errors, []); assert.ok(calls.every(x => x.method === 'GET')); await context.close();
    }
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ passed: checks.length, failed: failures.length, checks, failures }, null, 2));
    console.log(`DOCUMENT PREVIEW ${checks.length} PASS / ${failures.length} FAIL evidence=${evidence}`); assert.equal(failures.length, 0, JSON.stringify(failures.slice(0, 2)));
  } finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(error => { fs.writeFileSync(path.join(evidence, 'failure.txt'), error.stack); console.error(error); process.exitCode = 1; });
