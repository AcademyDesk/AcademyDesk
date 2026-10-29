// Actual TypeScript serialization; inert arithmetic fixtures only. Not Excel execution.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript');
const source = fs.readFileSync('apps/web/src/lib/spreadsheet-csv.ts', 'utf8');
const output = ts.transpileModule(source, { compilerOptions: {
  target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS,
}}).outputText;
const api = {};
new Function('exports', output)(api);

for (const value of ['=1+2', '+1+2', '-1+2', '@SUM(1)', '\tordinary', '\r=1', '\n=1',
  '  =1', '\u00a0\u200b=1', '\ufeff=1', '\u{E0001}=1', "'=1", '＝1', '＋1', '－1', '＠SUM(1)', '=1",;"=2']) {
  test(`guard formula/control text ${JSON.stringify(value)}`, () => {
    assert.equal(api.spreadsheetCsvCell(value), `"\t${value.replaceAll('"', '""')}"`);
  });
}
for (const value of ['ordinary', '  ordinary', "O'Neil", 'Piano, strings; drums', 'A "quoted" label',
  'line one\r\nline two', '', 'ಕನ್ನಡ नाम']) {
  test(`preserve ordinary text ${JSON.stringify(value)}`, () => {
    assert.equal(api.spreadsheetCsvCell(value), `"${value.replaceAll('"', '""')}"`);
  });
}
test('typed values and one-cell quoting preserve their distinction', () => {
  assert.equal(api.spreadsheetCsvCell(-12), '"-12"');
  assert.equal(api.spreadsheetCsvCell(false), '"false"');
  assert.equal(api.spreadsheetCsvCell(null), '""');
  assert.equal(api.spreadsheetCsvCell(undefined), '""');
  assert.equal(api.spreadsheetCsv([['Header', 'Other'], ['=1', 'a,b'], ['line\r\nbreak', -12]]),
    '"Header","Other"\r\n"\t=1","a,b"\r\n"line\r\nbreak","-12"');
});
test('all report downloads use the shared serializer, not local quote-only escaping', () => {
  const page = fs.readFileSync('apps/web/src/app/reports/page.tsx', 'utf8');
  assert.match(page, /import \{ spreadsheetCsv \} from "@\/lib\/spreadsheet-csv"/);
  assert.match(page, /const content = spreadsheetCsv\(rows\)/);
  assert.doesNotMatch(page, /value\.replaceAll/);
});
