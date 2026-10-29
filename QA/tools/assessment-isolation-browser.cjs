// Real React/DOM checks with intercepted synthetic data, not Identity/SQL proof.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'playwright-core');
const root = path.resolve(__dirname, '../..'), exported = path.join(root, 'apps/web/out');
const output = path.join(root, '.build-check/assessment-browser');
if (!process.env.QA_BROWSER_EXECUTABLE || !fs.existsSync(path.join(exported, 'assessments.html')))
  throw Error('Select a Chrome executable and build the loopback-configured static app first.');
fs.mkdirSync(output, { recursive: true });
const api = 'http://127.0.0.1:49142';
const types = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.json': 'application/json', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => {
  try {
    if (req.method !== 'GET') { res.writeHead(405); return res.end(); }
    let file = path.resolve(exported, '.' + decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname));
    if (file !== exported && !file.startsWith(exported + path.sep)) throw Error();
    if (file === exported) file = path.join(exported, 'index.html'); else if (!path.extname(file)) file += '.html';
    if (!fs.existsSync(file) || !fs.statSync(file).isFile()) { res.writeHead(404); return res.end(); }
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream' }); fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404); res.end(); }
});
async function main() {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = 'http://127.0.0.1:' + server.address().port;
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, args: ['--no-sandbox', '--disable-background-networking'] });
  try {
    for (const viewport of [{ width: 1280, height: 900 }, { width: 390, height: 844 }]) {
      const context = await browser.newContext({ viewport }), writes = [], errors = [], releases = [];
      const rows = Object.fromEntries(['one', 'two'].map(id => [id, { studentId: 's', score: id === 'one' ? 80 : 20, grade: id === 'one' ? 'Merit' : 'Fail', isGradeManual: true, remarks: id + ' saved notes', isPublished: true }]));
      let failure = '', heldId = '', held, rejectSave = false, failReadback = false;
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.origin === origin) return route.continue();
        if (url.origin !== api) return route.abort();
        assert.equal(req.headers().authorization, undefined, 'Never accept account credentials in synthetic transport');
        let body = [], status = 200;
        if (url.pathname === '/api/academies') body = [{ id: 'a', name: 'Synthetic academy', subscriptionStatus: 'Active', enabledModulesJson: '["Core","AcademicGovernance"]' }];
        else if (url.pathname === '/api/auth/session') body = { displayName: 'Synthetic admin', roles: ['AcademyAdmin'] };
        else if (url.pathname.endsWith('/options')) body = { batches: [{ id: 'b', name: 'Synthetic batch' }], students: [{ id: 's', firstName: 'Shared', lastName: 'Learner' }], enrollments: [{ studentId: 's', batchId: 'b', status: 'Active' }], gradingSchemes: [] };
        else if (url.pathname.endsWith('/assessments')) body = ['one', 'two'].map(id => ({ id, batchId: 'b', title: id === 'one' ? 'First assessment' : 'Second assessment', maxScore: 100 }));
        else if (/assessments\/(one|two)\/results$/.test(url.pathname)) {
          const id = url.pathname.match(/assessments\/(one|two)\/results$/)[1];
          if (req.method() === 'POST') {
            const payload = req.postDataJSON(); writes.push({ id, payload });
            if (rejectSave) { status = 400; body = { message: 'Synthetic result rejected; draft retained.' }; }
            else { rows[id] = payload; body = payload; }
          } else {
            if (id === heldId) await held;
            if (id === failure || failReadback && writes.length) { status = 503; body = {}; }
            else body = [rows[id]];
          }
        }
        return route.fulfill({ status, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': origin }, body: JSON.stringify(body) });
      });
      const page = await context.newPage(); page.on('pageerror', error => errors.push(error.message));
      try {
        await page.goto(origin + '/assessments');
        const panel = page.locator('.assessments-results-panel'), score = panel.getByLabel('Assessment score');
        const picker = page.locator('.standard-select-field').filter({ has: page.locator('input[name="assessment"]') }).locator('button.standard-select-trigger');
        const select = async id => { await picker.click(); await page.getByRole('option', { name: id === 'one' ? /First assessment/ : /Second assessment/ }).click(); };
        const waitScore = async value => { await score.waitFor(); await page.waitForFunction(expected => document.querySelector('[aria-label="Assessment score"]')?.value === expected, String(value)); };
        await waitScore(80);
        heldId = 'two'; let release; held = new Promise(resolve => { release = resolve; releases.push(resolve); });
        await select('two'); await panel.getByRole('status').filter({ hasText: 'Loading assessment results' }).waitFor();
        assert.equal(await score.count(), 0); assert.equal(writes.length, 0); release(); heldId = ''; await waitScore(20);
        console.log('ASSESSMENT BROWSER PASS:' + viewport.width + '; pending selection hides old score and cannot submit.');
        failure = 'one'; await select('one'); await panel.getByRole('button', { name: 'Retry results' }).waitFor(); assert.equal(await score.count(), 0);
        failure = ''; await panel.getByRole('button', { name: 'Retry results' }).click(); await waitScore(80);
        console.log('ASSESSMENT BROWSER PASS:' + viewport.width + '; failed selection locks; explicit retry restores correct assessment.');
        heldId = 'two'; held = new Promise(resolve => { release = resolve; releases.push(resolve); });
        await select('two'); await select('one'); await waitScore(80); release(); heldId = '';
        await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))); assert.equal(await score.inputValue(), '80');
        console.log('ASSESSMENT BROWSER PASS:' + viewport.width + '; reversed reads preserve current assessment.');
        await score.fill('81'); await panel.getByPlaceholder('Remarks').fill('New draft'); rejectSave = true;
        await panel.getByRole('button', { name: 'Save', exact: true }).click(); await page.getByRole('status').filter({ hasText: 'Synthetic result rejected' }).waitFor();
        assert.equal(await score.inputValue(), '81'); assert.equal(await panel.getByPlaceholder('Remarks').inputValue(), 'New draft');
        rejectSave = false; failReadback = true; await panel.getByRole('button', { name: 'Save', exact: true }).click();
        await page.getByRole('status').filter({ hasText: 'saved, but results could not be refreshed' }).waitFor(); assert.equal(await score.inputValue(), '81');
        assert.equal(writes.length, 2); assert.ok(writes.every(x => x.id === 'one' && x.payload.score === 81 && x.payload.remarks === 'New draft'));
        assert.equal(rows.two.score, 20); assert.deepEqual(errors, []);
        await page.screenshot({ path: path.join(output, viewport.width + '-retained-draft.png'), fullPage: true });
        fs.writeFileSync(path.join(output, viewport.width + '-writes.json'), JSON.stringify(writes, null, 2));
        console.log('ASSESSMENT BROWSER PASS:' + viewport.width + '; rejected draft retained; confirmed save/readback failure truthful; other assessment unchanged; no page errors.');
      } finally { releases.forEach(resolve => resolve()); await context.close(); }
    }
    console.log('ASSESSMENT BROWSER REGRESSION PASS:8 grouped checks at desktop/phone; intercepted synthetic transport only; authenticated SQL acceptance remains pending.');
  } finally { await browser.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; }).finally(() => server.close());
