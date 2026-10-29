// Native download plumbing for actual Reports downloadCsv + shared TS helper.
// No application auth/SQL/Excel/physical-device or full-page layout acceptance.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const ts = require('../../apps/web/node_modules/typescript');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE ||
  'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..');
const evidence = path.join(root, 'QA/EVIDENCE', 'spreadsheet-csv-' + Date.now());
fs.mkdirSync(evidence, { recursive: true });
const compilerOptions = { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS };
const helperSource = fs.readFileSync(path.join(root, 'apps/web/src/lib/spreadsheet-csv.ts'), 'utf8');
const helperCode = ts.transpileModule(helperSource, { compilerOptions }).outputText;
const pageSource = fs.readFileSync(path.join(root, 'apps/web/src/app/reports/page.tsx'), 'utf8');
const fn = pageSource.slice(pageSource.indexOf('function downloadCsv('), pageSource.indexOf('export default function ReportsPage'));
assert.ok(fn.startsWith('function downloadCsv(') && fn.includes('spreadsheetCsv(rows)'));
const downloadCode = ts.transpileModule(fn + '\nexport { downloadCsv };', { compilerOptions }).outputText;
const rows = [['Label', 'Other'], ['=1+2', 'a,b'], ['\u{E0001}=1', '=1",;"=2'],
  ['\r=1', 'line one\r\nline two'], ['ಕನ್ನಡ नाम', 'ordinary'], ['+910000000000', '-12']];

// Independent cell parser, preserving leading tab and embedded newlines.
function parseCsv(text) {
  const rows = []; let row = [], cell = '', quoted = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === '"') {
      if (quoted && text[i + 1] === '"') { cell += '"'; i++; }
      else quoted = !quoted;
    } else if (!quoted && c === ',') { row.push(cell); cell = ''; }
    else if (!quoted && c === '\r' && text[i + 1] === '\n') { row.push(cell); rows.push(row); row = []; cell = ''; i++; }
    else cell += c;
  }
  assert.equal(quoted, false); row.push(cell); rows.push(row); return rows;
}

(async () => {
  let browser;
  const checks = [];
  try {
    browser = await chromium.launch({ headless: true, downloadsPath: evidence,
      executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 1440]) for (const theme of ['light', 'dark']) {
      const context = await browser.newContext({ viewport: { width, height: 900 }, acceptDownloads: true });
      try {
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await context.route('**/*', route => route.abort());
        await page.setContent('<!doctype html><title>CSV native download test</title><button id="download">Download synthetic CSV</button>');
        await page.evaluate(({ helperCode, downloadCode, rows, theme }) => {
          document.documentElement.dataset.theme = theme;
          const helper = {};
          new Function('exports', helperCode)(helper);
          const downloads = {};
          new Function('exports', 'spreadsheetCsv', downloadCode)(downloads, helper.spreadsheetCsv);
          document.getElementById('download').onclick = () => downloads.downloadCsv('synthetic-safe-view.csv', rows);
        }, { helperCode, downloadCode, rows, theme });
        const pending = page.waitForEvent('download');
        await page.locator('#download').click();
        const download = await pending;
        assert.equal(download.suggestedFilename(), 'synthetic-safe-view.csv');
        assert.equal(await download.failure(), null);
        const file = path.join(evidence, `native-${width}-${theme}.csv`);
        await download.saveAs(file);
        const actual = parseCsv(fs.readFileSync(file, 'utf8'));
        assert.deepEqual(actual, [rows[0], ['\t=1+2', 'a,b'], ['\t\u{E0001}=1', '\t=1",;"=2'],
          ['\t\r=1', 'line one\r\nline two'], rows[4], ['\t+910000000000', '\t-12']]);
        assert.deepEqual(errors, []);
        checks.push({ width, theme, native_download: true, parsed_rows: actual.length,
          columns_per_row: 2, raw_bytes_and_quoting: 'PASS', page_errors: 0 });
      } finally { await context.close(); }
    }
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({
      scope: 'native Blob/anchor download of actual Reports function; synthetic data', checks,
      inference_calls: 0, database_operations: 0, external_requests: 0,
      spreadsheet_client_acceptance: 'NOT_RUN', production_approved: false,
    }, null, 2));
    process.stdout.write(JSON.stringify({ evidence, passed: checks.length }) + '\n');
  } finally { if (browser) await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
