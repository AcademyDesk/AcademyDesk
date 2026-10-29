'use strict';
// Real HTTP/Identity/disposable SQL; URL relay only, never synthetic API responses.
const assert = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), http = require('node:http');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api, run] = process.argv.slice(2);
assert.match(api || '', /^http:\/\/127\.0\.0\.1:\d+$/); assert.match(run || '', /^[0-9a-f]{32}$/);
const root = path.resolve(__dirname, '../..'), web = path.join(root, 'apps/web/out');
assert.ok(fs.existsSync(path.join(web, 'penta/onboarding.html')), 'Build the product export before SQL acceptance');
const evidence = path.join(root, 'QA/EVIDENCE', `penta-onboarding-review-live-${run}`);
fs.mkdirSync(evidence, { recursive:true });
const mime = { '.html':'text/html', '.js':'application/javascript', '.css':'text/css', '.txt':'text/plain', '.json':'application/json', '.woff2':'font/woff2', '.ico':'image/x-icon' };
const server = http.createServer((req, res) => {
  try {
    assert.ok(['GET','HEAD'].includes(req.method));
    let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    assert.ok(file.startsWith(web + path.sep)); if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) file = path.join(path.dirname(file), '__next.' + segment[1], '__PAGE__.txt');
    file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep));
    res.writeHead(200, { 'Content-Type':mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control':'no-store' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
const checks = []; let browser;
async function main() {
  let input = ''; for await (const chunk of process.stdin) { input += chunk; assert.ok(input.length < 65536); }
  const fixture = JSON.parse(input); input = ''; // Private synthetic bearer material never written to evidence.
  for (const name of ['selected','pending']) for (const key of ['conversation','draft'])
    assert.match(fixture[name][key], /^[0-9a-f-]{36}$/);
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = `http://127.0.0.1:${server.address().port}`;
  browser = await chromium.launch({ headless:true, executablePath:process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const cases = [];
  for (const theme of ['dark','light']) for (const width of [320,390,768,1440]) cases.push({ name:`valid-${theme}-${width}`, theme, width, scenario:'valid', token:'admin' });
  for (const scenario of ['pending','wrong-draft','other','foreign','teacher','logout','pending-recovery'])
    cases.push({ name:scenario, theme:'dark', width:390, scenario, token:['other','foreign','teacher'].includes(scenario) ? scenario : 'admin' });
  try {
    for (const c of cases) {
      const context = await browser.newContext({ viewport:{ width:c.width, height:900 } });
      const calls = [], errors = [], expectedHttp = [];
      const page = await context.newPage();
      try {
        await context.addInitScript(({ token, theme }) => {
          localStorage.setItem('academydesk.accessToken.AcademyAdmin', token); localStorage.setItem('academydesk.theme', theme);
        }, { token:fixture.tokens[c.token], theme:c.theme });
        page.on('pageerror', error => errors.push(error.message));
        page.on('console', message => {
          if (message.type() !== 'error') return;
          if (['other','foreign'].includes(c.scenario) && /^Failed to load resource: the server responded with a status of 404 \(/.test(message.text())) expectedHttp.push(message.text());
          else errors.push(message.text());
        });
        await context.route('**/*', async route => {
          const request = route.request(), url = new URL(request.url());
          if (url.pathname.startsWith('/api/')) {
            assert.ok(['localhost','127.0.0.1'].includes(url.hostname)); assert.equal(request.method(), 'GET');
            // Forward original request authentication to the actual guarded host.
            const response = await route.fetch({ url:api + url.pathname + url.search, maxRedirects:0, maxRetries:0 });
            assert.notEqual(response.status(), 429);
            const diagnostic = {};
            if (url.pathname.includes('/drafts/') && response.status() === 200) {
              const view = await response.json();
              diagnostic.expiryHasExplicitZone = typeof view.expiresAtUtc === 'string' && /(Z|[+-]\d{2}:\d{2})$/.test(view.expiresAtUtc);
            }
            calls.push({ method:request.method(), path:url.pathname, status:response.status(), ...diagnostic });
            return route.fulfill({ response, headers:{ ...response.headers(), 'access-control-allow-origin':origin, 'access-control-allow-headers':'Authorization,Accept' } });
          }
          if (url.origin === origin || url.protocol === 'data:') return route.continue();
          throw Error('Unexpected external browser request');
        });
        const target = ['pending','pending-recovery'].includes(c.scenario) ? fixture.pending : { ...fixture.selected };
        if (c.scenario === 'wrong-draft') target.draft = '11111111-1111-4111-8111-111111111111';
        await page.goto(`${origin}/penta/onboarding?conversationId=${target.conversation}&draftId=${target.draft}`);
        if (['valid','logout'].includes(c.scenario)) {
          await Promise.race([
            page.getByRole('button', { name:/Work canvas/ }).waitFor(),
            page.getByText('Onboarding actions are unavailable', { exact:true }).waitFor().then(() => { throw Error('Real authenticated bootstrap was withheld; inspect bounded HTTP diagnostics'); })
          ]);
          assert.equal(await page.getByRole('textbox', { name:'Ask PENTA', exact:true }).isDisabled(), true);
          assert.equal(await page.getByRole('button', { name:'Send', exact:true }).isDisabled(), true);
          await page.getByRole('button', { name:/Work canvas/ }).click();
          const dialog = page.getByRole('dialog', { name:'Work canvas', exact:true }); await dialog.waitFor();
          const first = dialog.getByRole('textbox', { name:'Student First Name (required)', exact:true });
          assert.equal(await first.inputValue(), 'SQL-REVIEW-ONLY'); assert.equal(await first.isDisabled(), true);
          const last = dialog.getByLabel('Allow Leave', { exact:true }); await last.scrollIntoViewIfNeeded(); assert.equal(await last.isDisabled(), true);
          const submit = dialog.locator('.op-draft-form > button[type="submit"]');
          assert.equal(await submit.isDisabled(), true); assert.equal(await submit.isVisible(), false);
          assert.equal(await page.getByRole('button', { name:'Confirm exact operation' }).count(), 0);
          const close = dialog.getByRole('button', { name:'Close Work canvas', exact:true });
          const bounds = await close.boundingBox(); assert.ok(bounds && bounds.y >= 0 && bounds.y + bounds.height <= 900);
          await page.screenshot({ path:path.join(evidence, `${c.name}.png`) }); await close.click();
          if (c.scenario === 'logout') {
            await page.evaluate(() => window.dispatchEvent(new CustomEvent('academydesk:portal-sign-out', { detail:'AcademyAdmin' })));
            // The retained shared session boundary now unmounts the entire
            // protected workspace on logout, before the inner review notice.
            await page.getByRole('heading', { name:'Session ended', exact:true }).waitFor();
            await page.getByRole('alert').filter({ hasText:'This workspace was signed out, here or in another tab.' }).waitFor();
            assert.equal(await page.getByRole('link', { name:'Sign in again', exact:true }).getAttribute('href'), '/login');
            assert.equal(await page.getByRole('button', { name:/Work canvas/ }).count(), 0);
            assert.equal(await page.locator('input').count(), 0);
          }
          assert.equal(calls.length, 4); assert.ok(calls.every(call => call.status === 200));
          assert.equal(calls[2].expiryHasExplicitZone, true);
          assert.ok(calls[0].path === '/api/auth/session' && calls[1].path.endsWith('/current') &&
            calls[2].path.endsWith(`/drafts/${target.draft}`) && calls[3].path === calls[1].path);
        } else {
          await page.getByText('Onboarding actions are unavailable', { exact:true }).waitFor();
          assert.equal(await page.getByRole('button', { name:/Work canvas/ }).count(), 0);
          assert.equal(await page.locator('input').count(), 0);
          assert.ok(!(await page.locator('body').innerText()).includes('SQL-REVIEW-ONLY'));
          assert.equal(calls.length, c.scenario === 'teacher' ? 1 : 2);
          assert.equal(calls[0].status, 200);
          if (['other','foreign'].includes(c.scenario)) assert.equal(calls[1].status, 404);
          else assert.ok(calls.every(call => call.status === 200));
          if (c.scenario === 'pending-recovery') {
            const before = calls.length;
            await page.getByRole('button', { name:'Check pending request', exact:true }).click();
            await page.getByRole('status').filter({ hasText:'The request is still pending.' }).waitFor();
            assert.equal(calls.length, before + 2);
            assert.ok(calls[before].path.endsWith('/current')); assert.equal(calls[before].status, 200);
            assert.match(calls[before + 1].path, /\/claims\/[0-9a-f-]{36}\/result$/);
            assert.equal(calls[before + 1].status, 202);
            assert.ok(calls.every(call => call.method === 'GET'));
            assert.equal(await page.getByRole('button', { name:/Work canvas/ }).count(), 0);
          }
          await page.screenshot({ path:path.join(evidence, `${c.name}.png`) });
        }
        if (c.scenario !== 'logout') assert.equal(await page.getByRole('link', { name:'Manual workspace', exact:true }).getAttribute('href'), '/dashboard');
        else assert.equal(await page.getByRole('link', { name:'Manual workspace', exact:true }).count(), 0);
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        assert.deepEqual(errors, []);
        checks.push({ scenario:c.name, pass:true, calls, errors, expectedHttp });
        console.log(`PASS: PENTAONBOARDINGREVIEW browser ${c.name};${calls.length} actual host GETs`);
      } catch (error) {
        await page.screenshot({ path:path.join(evidence, `failed-${c.name}.png`) }).catch(() => {});
        fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ scenario:c.name, calls, errors, error:error.message }, null, 2));
        throw error;
      } finally { await context.close(); }
    }
    assert.equal(checks.length, 15);
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ run, checks, sqlBacked:true, syntheticIdentities:true,
      syntheticApiResponses:false, browserWrites:0, inference:false, physicalDevices:'NOT RUN', sqlPostconditions:'enforced by parent harness after browser exits' }, null, 2));
    console.log(`PASS: PENTAONBOARDINGREVIEW browser evidence=${evidence}`);
  } finally { await browser.close(); await new Promise(resolve => server.close(resolve)); }
}
main().catch(error => { console.error(`SQL review browser failed: ${error.name}: ${error.message}`); process.exitCode = 1; server.close(); });
