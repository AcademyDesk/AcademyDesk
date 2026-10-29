// Native downloads using actual TSX handler functions. Synthetic transport only;
// not full authenticated React pages, SQL, responsive layout or spreadsheet execution.
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const ts = require('../../apps/web/node_modules/typescript');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE ||
  'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..');
const evidence = path.join(root, 'QA/EVIDENCE', 'csv-download-recovery-' + Date.now());
fs.mkdirSync(evidence, { recursive: true });
const compilerOptions = { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS };
const handlers = {};
for (const kind of ['data-operations', 'finance']) {
  const source = fs.readFileSync(path.join(root, `apps/web/src/app/${kind}/page.tsx`), 'utf8');
  const start = source.indexOf(kind === 'finance' ? '  async function downloadExport(' : '  async function download(');
  const end = source.indexOf(kind === 'finance' ? '\n  const cards =' : '\n  async function loadCsv(', start);
  assert.ok(start > 0 && end > start);
  const name = kind === 'finance' ? 'downloadExport' : 'download';
  handlers[kind] = ts.transpileModule(source.slice(start, end) + `\nexport { ${name} };`, { compilerOptions }).outputText;
}
(async () => {
  const checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, downloadsPath: evidence,
      executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const kind of Object.keys(handlers)) for (const width of [320, 1440]) for (const scenario of ['normal', 'denied', 'network', 'wrong-type', 'body-failure']) {
      const context = await browser.newContext({ viewport: { width, height: 900 }, acceptDownloads: true });
      try {
        const page = await context.newPage(), downloads = [], errors = [];
        page.on('download', download => downloads.push(download));
        page.on('pageerror', error => errors.push(error.message));
        await context.route('**/*', route => route.abort());
        await page.setContent('<!doctype html><title>CSV handler test</title><button id="go">Export synthetic CSV</button><p id="notice"></p>');
        await page.evaluate(({ code, kind, scenario }) => {
          const pending = { current: false }, counts = { requests: 0, urls: 0, revoked: 0 };
          window.csvCheck = { counts, pending, done: false };
          const create = URL.createObjectURL.bind(URL), revoke = URL.revokeObjectURL.bind(URL);
          URL.createObjectURL = blob => { counts.urls++; return create(blob); };
          URL.revokeObjectURL = url => { counts.revoked++; revoke(url); };
          const setNotice = notice => { document.getElementById('notice').textContent = notice; };
          const setPending = resource => { document.getElementById('go').disabled = resource !== undefined; };
          const api = async () => {
            counts.requests++;
            await new Promise(resolve => setTimeout(resolve, 25));
            if (scenario === 'network') throw Error('Synthetic network failure');
            if (scenario === 'body-failure') return { ok: true, headers: new Headers({ 'content-type': 'text/csv' }), blob: async () => { throw Error('Synthetic body failure'); } };
            return new Response('"safe"\r\n"\t=1+2"', { status: scenario === 'denied' ? 403 : 200,
              headers: { 'content-type': scenario === 'wrong-type' ? 'text/html' : 'text/csv;charset=utf-8' } });
          };
          const exported = {};
          new Function('exports', 'academy', 'downloadPending', 'exportPending', 'setDownloading', 'setExporting',
            'setMessage', 'setNotice', 'academyApi', code)(exported, { id: 'synthetic' }, pending, pending,
              setPending, setPending, setNotice, setNotice, api);
          const fn = kind === 'finance' ? exported.downloadExport : exported.download;
          document.getElementById('go').onclick = () => {
            const first = fn(kind === 'finance' ? 'invoices' : 'students');
            // Invoke a duplicate/opposing stale handler in the same tick.
            const duplicate = fn('payments');
            Promise.all([first, duplicate]).then(() => { window.csvCheck.done = true; });
          };
        }, { code: handlers[kind], kind, scenario });
        await page.locator('#go').click();
        await page.waitForFunction(() => window.csvCheck.done);
        const state = await page.evaluate(() => ({ ...window.csvCheck.counts, pending: window.csvCheck.pending.current,
          notice: document.getElementById('notice').textContent, disabled: document.getElementById('go').disabled }));
        assert.equal(state.requests, 1); assert.equal(state.pending, false); assert.equal(state.disabled, false);
        if (scenario === 'normal') {
          await page.waitForTimeout(100);
          assert.equal(downloads.length, 1); assert.equal(state.urls, 1); assert.equal(state.revoked, 1);
          assert.match(state.notice, /download started/);
          const download = downloads[0]; assert.equal(await download.failure(), null);
          assert.equal(download.suggestedFilename(), kind === 'finance' ? 'academydesk-invoices.csv' : 'academydesk-students.csv');
          const file = path.join(evidence, `${kind}-${width}.csv`); await download.saveAs(file);
          assert.equal(fs.readFileSync(file, 'utf8'), '"safe"\r\n"\t=1+2"');
        } else {
          assert.equal(downloads.length, 0); assert.equal(state.urls, 0); assert.equal(state.revoked, 0);
          assert.match(state.notice, /could not/); assert.doesNotMatch(state.notice, /download started/);
        }
        assert.deepEqual(errors, []);
        checks.push({ kind, width, scenario, ...state, downloaded: downloads.length, page_errors: 0 });
      } finally { await context.close(); }
    }
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({
      scope: 'actual extracted TSX handlers and native browser downloads; synthetic responses', checks,
      physical_device: false, actual_SQL: false, authenticated_full_page: false, production_approved: false,
    }, null, 2));
    console.log(JSON.stringify({ evidence, passed: checks.length }));
  } finally { if (browser) await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
