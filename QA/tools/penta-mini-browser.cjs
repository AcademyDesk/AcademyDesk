// Real local Next.js -> authenticated API -> private Mini -> disposable SQL.
// No response mocks, token injection, domain writes, Azure or external pages.
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..');
const run = process.env.QA_PENTA_RUN;
assert.match(run || '', /^[a-f0-9]{32}$/);
const marker = path.join(process.env.TEMP, 'AcademyDesk-QA', run, '.qa-owner');
assert.equal(fs.readFileSync(marker, 'utf8').split(/\r?\n/)[0], run, 'Exact disposable fixture ownership required');
const origin = 'http://127.0.0.1:49542', api = 'http://127.0.0.1:49541';
const evidence = path.join(root, 'QA/EVIDENCE', 'penta-mini-' + run);
fs.mkdirSync(evidence, { recursive: true });
const checks = [], errors = [], http = [];
const pass = name => { checks.push(name); console.log('PENTA MINI BROWSER PASS ' + name); };
(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  try {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      return [origin, api].includes(url.origin) || url.protocol === 'data:' ? route.continue() : route.abort('blockedbyclient');
    });
    const page = await context.newPage();
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('response', response => { if (response.url().startsWith(origin) && response.status() >= 400) console.error('PENTA PREVIEW RESOURCE ' + response.status() + ' ' + new URL(response.url()).pathname); });
    page.on('response', response => { if (response.url().startsWith(api)) http.push({ path: new URL(response.url()).pathname, method: response.request().method(), status: response.status() }); });
    await page.goto(origin + '/login');
    await page.getByRole('button', { name: 'Show password' }).click();
    await page.getByRole('button', { name: 'Hide password' }).click();
    await page.getByLabel('User name', { exact: true }).fill('penta-admin-a@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const login = page.waitForResponse(r => r.url().startsWith(api + '/api/auth/login') && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    assert.equal((await login).status(), 200);
    await page.waitForURL('**/dashboard');
    await page.goto(origin + '/penta');
    await page.getByText('● Mini connected').waitFor({ timeout: 60000 });
    pass('real Identity login and Mini readiness');
    async function send(text) {
      await page.getByLabel('Message PENTA AI', { exact: true }).fill(text);
      const native = page.waitForResponse(r => r.url().startsWith(api) && /\/penta\/chat\/conversations\/[^/]+\/turns$/.test(new URL(r.url()).pathname) && r.request().method() === 'POST', { timeout: 140000 });
      await page.getByRole('button', { name: 'Send ↑', exact: true }).click();
      const response = await native;
      assert.equal(response.status(), 201, 'Real turn response');
      const receipt = await response.json();
      assert.equal(receipt.kind, 'RESULT', 'Must be a real verified tool result, not text or a canned answer');
      await page.getByRole('button', { name: 'Send ↑', exact: true }).waitFor();
      return receipt;
    }
    const first = await send('Show students with pending fees.');
    assert.equal(first.result.count, 4);
    assert.ok(first.result.rows.every(row => !row.displayName.includes('Foreign') && !row.displayName.includes('Inactive')));
    const second = await send('Only piano.');
    assert.equal(second.result.count, 3);
    const third = await send('Highest first.');
    assert.deepEqual(third.result.rows.map(row => row.balances[0].outstanding), [400, 300, 200]);
    const fourth = await send('Show the second one.');
    assert.equal(fourth.result.rows[0].sourceId, third.result.rows[1].sourceId);
    assert.equal(fourth.result.rows[0].displayName, 'Ananya Piano');
    assert.equal(fourth.result.rows[0].balances[0].outstanding, 300);
    await page.getByText('Here is the selected student\'s verified fee summary.', { exact: true }).waitFor();
    pass('four-turn actual Mini / SQL conversation and sorted ordinal');
    const panel = page.getByRole('region', { name: 'PENTA conversation' });
    assert.equal(await panel.getByRole('link', { name: 'Open Student 360 ↗' }).last().getAttribute('href'), fourth.result.rows[0].sourcePath);
    await page.getByRole('button', { name: 'Workspace Manual', exact: true }).click();
    await page.getByRole('heading', { name: 'Your academy workspace' }).waitFor();
    await page.getByRole('button', { name: 'PENTA AI', exact: true }).click();
    await panel.getByText('Here is the selected student\'s verified fee summary.', { exact: true }).waitFor();
    pass('manual switch preserves current conversation and verified source link');
    await page.getByRole('button', { name: 'About Executor', exact: true }).click();
    await page.getByRole('note').waitFor();
    assert.match(await page.getByRole('note').innerText(), /not enabled/);
    await page.getByRole('note').getByRole('button', { name: 'Close' }).click();
    assert.equal(await page.getByRole('button', { name: 'About Executor', exact: true }).evaluate(el => el === document.activeElement), true);
    await page.getByRole('button', { name: 'About Executor', exact: true }).click();
    await page.keyboard.press('Escape');
    assert.equal(await page.getByRole('note').count(), 0);
    await page.getByRole('button', { name: 'T Twin', exact: true }).click();
    assert.equal(await panel.getByText('Here is the selected student\'s verified fee summary.', { exact: true }).count(), 1);
    pass('capability help and shared conversation retained across capability switch');
    for (const width of [320, 390, 1440]) {
      await page.setViewportSize({ width, height: width < 600 ? 844 : 1000 });
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, 'No horizontal overflow at ' + width);
      assert.ok(await page.getByLabel('Message PENTA AI', { exact: true }).isVisible());
      await page.screenshot({ path: path.join(evidence, `penta-${width}-light.png`), fullPage: true });
      await page.getByLabel('Message PENTA AI', { exact: true }).scrollIntoViewIfNeeded();
      await page.screenshot({ path: path.join(evidence, `penta-${width}-latest-light.png`) });
      pass('responsive ' + width + 'px; source cards / composer / context visible');
    }
    // Only switch presentation; never invent data or authorization through DOM.
    await page.getByRole('button', { name: 'Use dark theme', exact: true }).click();
    await page.waitForTimeout(300); // allow existing 160–180ms theme transitions to finish
    await page.screenshot({ path: path.join(evidence, 'penta-1440-dark.png'), fullPage: true });
    await page.getByRole('button', { name: 'Use light theme', exact: true }).click();
    fs.writeFileSync(path.join(evidence, 'browser-console.json'), JSON.stringify(errors, null, 2));
    assert.equal(errors.length, 0, 'No runtime/hydration exceptions');
    assert.equal(http.filter(x => x.method === 'POST' && /\/turns$/.test(x.path)).length, 4, 'No automatic duplicate model calls');
    pass('no browser runtime errors and exactly four turn POSTs');
    // Open the source after same-page preservation assertions: Next's development
    // route compilation may refresh peer tabs; navigation intentionally resets chat.
    const sourcePage = await context.newPage();
    sourcePage.on('pageerror', error => errors.push(error.message));
    sourcePage.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    await sourcePage.goto(origin + fourth.result.rows[0].sourcePath);
    await sourcePage.getByRole('heading', { name: 'Ananya Piano', exact: true }).waitFor({ timeout: 60000 });
    await sourcePage.getByRole('button', { name: /Fees.*₹300/ }).waitFor({ timeout: 60000 });
    await sourcePage.close();
    assert.equal(errors.length, 0, 'Source link must not introduce console/hydration errors');
    pass('verified source opens Ananya Student 360 with matching adjusted INR 300 and no hydration error');
    fs.writeFileSync(path.join(evidence, 'browser-result.json'), JSON.stringify({ run, checks, errors, http, sourceIds: third.result.rows.map(row => row.sourceId), ordinal: fourth.result.rows[0].sourceId, physicalDevices: 'NOT RUN' }, null, 2));
  } finally { await browser.close(); }
})().catch(error => { console.error('PENTA MINI BROWSER FAIL ' + error.message); process.exitCode = 1; });
