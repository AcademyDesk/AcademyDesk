const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),ts=require('../../apps/web/node_modules/typescript');
function load(file,require){const exports={};new Function('exports','require',ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText)(exports,require);return exports;}
const expense=load('apps/web/src/lib/expense-money.ts',()=>{throw Error('Unexpected dependency');});
const {payrollMoney,payrollTotals,payrollRequestJson}=load('apps/web/src/app/payroll/payroll-money.ts',n=>{assert.equal(n,'@/lib/expense-money');return expense;});
for(const [input,exact,currency,expected] of [[0.01,undefined,'INR','₹0.01'],[0,undefined,'INR','₹0.00'],[1000,undefined,'INR','₹1,000.00'],[Number.MAX_VALUE,'9999999999999999.99','INR','₹9,99,99,99,99,99,99,999.99'],[0.1,'0.01','USD','$0.01'],[NaN,undefined,'INR','Amount unavailable']])test('Payroll exact display '+expected,()=>assert.equal(payrollMoney(input,exact,currency),expected));
test('Totals never merge currencies or lose subunits',()=>assert.equal(payrollTotals([{netAmount:0.01,currency:'INR'},{netAmount:0.01,currency:'INR'},{netAmount:0.01,currency:'USD'}]),'₹0.02 · $0.01'));
test('Large aggregate retains cents from exact response strings',()=>assert.equal(payrollTotals([{netAmount:0,netAmountExact:'9999999999999999.99',currency:'INR'},{netAmount:0.01,currency:'INR'}]),'₹10,00,00,00,00,00,00,000.00'));
test('Invalid value does not fabricate a total',()=>assert.equal(payrollTotals([{netAmount:NaN,currency:'INR'}]),'Totals unavailable'));
test('Missing currency is not relabelled INR',()=>assert.equal(payrollMoney(1.23,undefined,''),'1.23 (currency unavailable)'));

test('Only allowlisted decimal lexemes bypass string encoding',()=>{
 const text='Synthetic "worker"\n<script>not executable</script>';
 const json=payrollRequestJson({workerName:text,monthlyAmount:'9999999999999999.99',amountPerCycle:null,deductions:'0',optional:undefined});
 assert.match(json,/"monthlyAmount":9999999999999999\.99,/);assert.equal(JSON.parse(json).workerName,text);
 assert.equal(JSON.parse(json).deductions,0);assert.equal(JSON.parse(json).amountPerCycle,null);assert.ok(!json.includes('optional'));
});
for(const raw of ['0.001','1e3','NaN','Infinity','10000000000000000','-1','1,"isActive":false'])
 test('Serializer rejects invalid/injected decimal '+raw,()=>assert.equal(payrollRequestJson({monthlyAmount:raw}),null));
test('Serializer rejects binary number input rather than pretending it is exact',()=>assert.equal(payrollRequestJson({grossAmount:1000}),null));
test('Serializer preserves positive cent and trims valid decimal input',()=>assert.equal(payrollRequestJson({grossAmount:' 0.01 ',deductions:'0.00'}),'{"grossAmount":0.01,"deductions":0.00}'));
