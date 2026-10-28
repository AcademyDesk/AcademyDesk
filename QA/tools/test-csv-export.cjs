const assert = require('node:assert/strict');
const { test } = require('node:test');
const { spreadsheetCsvCell, spreadsheetCsv } = require('../../apps/web/src/lib/csv.ts');

for (const value of ['=1+1', '+SUM(1,2)', '-1+2', '@SUM(1,2)', '\t=1+1', '\r=1+1',
  '\n=1+1', '  =1+1', '\uFEFF=1+1', '\0=1+1', '\tordinary text', '＝1+1', '＋1+1', '－1+1', '＠SUM(1,2)']) {
  test(`neutralizes ${JSON.stringify(value)} without losing content`, () => {
    assert.equal(spreadsheetCsvCell(value), '"\'' + value.replaceAll('"', '""') + '"');
  });
}

test('keeps CSV boundaries, escaped quotes, embedded newlines, blanks and Unicode', () => {
  assert.equal(spreadsheetCsv([['Name', 'Note'], ['नाम', 'comma, "quote"\r\nnext'], ['', 'ordinary']]),
    '"Name","Note"\r\n"नाम","comma, ""quote""\r\nnext"\r\n"","ordinary"');
});

test('quotes injected cell separators inside one inert cell', () => {
  assert.equal(spreadsheetCsvCell('=1+1","=2+2'), '"\'=1+1"",""=2+2"');
});
