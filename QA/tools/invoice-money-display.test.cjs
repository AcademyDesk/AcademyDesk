const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm'), assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const ts = require(path.join(root, 'apps/web/node_modules/typescript'));
const render = require(path.join(root, 'apps/web/node_modules/react-dom/server'));
const baseline = process.argv[2];
if (baseline) assert.match(baseline,/^[a-f0-9]{40}$/);
const read = file => baseline ? require('node:child_process').execFileSync('git',['show',baseline+':'+file], {cwd:root,encoding:'utf8',stdio:['pipe','pipe','pipe']}) : fs.readFileSync(path.join(root,file),'utf8');
const portal = read('apps/web/src/app/portal/page.tsx');
const invoice = read('apps/web/src/app/invoices/page.tsx');
const cashSource = portal.slice(portal.indexOf('const cash ='), portal.indexOf('const indiaDateKey ='));
const cash = vm.runInNewContext(ts.transpile(cashSource + '\ncash;', { target: ts.ScriptTarget.ES2022 }), { Intl });
let passed = 0, failed = 0;
function test(name, run) { try { run(); passed++; console.log('PASS: ' + name); } catch (e) { failed++; console.log('FAIL: ' + name + ': ' + e.message); } }
for (const [name,source] of [['Student',portal],['Invoices',invoice]]) test(name+' page TSX parses', () => assert.equal(ts.createSourceFile(name+'.tsx',source,ts.ScriptTarget.Latest,true,ts.ScriptKind.TSX).parseDiagnostics.length,0));
for (const [value, currency, expected] of [[0.01,'INR','₹0.01'],[0,'INR','₹0.00'],[400,'INR','₹400.00'],[799.99,'INR','₹799.99'],[0.01,'USD','$0.01'],[12,'JPY','JP¥12'],[0.001,'KWD','KWD\u00a00.001']]) {
  test('actual Student formatter ' + value + ' ' + currency, () => assert.equal(cash(value,currency),expected));
}
test('home and invoice rows use the same actual formatter', () => { assert.ok(portal.includes('b={cash(balance)}')); assert.ok(portal.includes('cash(invoice.balance, invoice.currency)')); });
const componentPath = path.join(root, 'apps/web/src/components/invoice-ledger-summary.tsx');
let component, legacyPreview;
if (!baseline && fs.existsSync(componentPath)) {
  const exports = {}, source = ts.transpileModule(fs.readFileSync(componentPath,'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  vm.runInNewContext(source, { exports, require: name => require(path.join(root,'apps/web/node_modules',name)), Intl }); component = exports.InvoiceLedgerSummary;
} else {
  const markup = invoice.match(/<div className="invoice-paper-total">.*?<\/div>/)[0];
  const formatter = invoice.match(/^const money = .*$/m)[0];
  const source = ts.transpileModule(formatter + '\nexports.preview = (selected) => (' + markup + ');', { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  const exports = {}; vm.runInNewContext(source, { exports, require: name => require(path.join(root,'apps/web/node_modules',name)), Intl }); legacyPreview = exports.preview;
}
for (const [name, paidAmount, adjustedAmount, balance] of [['partial',600,0,400],['paid',1000,0,0],['fraction',799.99,200,0.01],['adjusted-paid',800,200,0],['no-cash',0,1000,0]]) {
  test('actual preview ' + name, () => {
    const data = {totalAmount:1000,paidAmount,adjustedAmount,balance,currency:'INR'};
    const html = render.renderToStaticMarkup(component ? component({invoice:data}) : legacyPreview(data));
    assert.ok(html.includes('Invoice total')); assert.ok(html.includes('Approved adjustments')); assert.ok(html.includes('Collected payments')); assert.ok(html.includes('Remaining due'));
    for (const [label,value] of [['Invoice total',1000],['Approved adjustments',adjustedAmount],['Collected payments',paidAmount],['Remaining due',balance]]) {
      assert.ok(html.includes(`<span>${label}</span><b>${cash(value)}</b>`), label + ' must match authoritative value');
    }
    assert.ok(!html.includes('Total payable'));
  });
}
test('invoice page uses server balance, not recomputed gross or fallback zero', () => { assert.ok(invoice.includes('<InvoiceLedgerSummary invoice={selected}/>')); assert.ok(invoice.includes('balance: number')); });
console.log(JSON.stringify({passed,failed})); if (failed) process.exitCode = 1;
