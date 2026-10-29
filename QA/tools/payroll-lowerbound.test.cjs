const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const source=fs.readFileSync(process.env.QA_PAYROLL_LOWERBOUND_BEFORE==='1'?'QA/EVIDENCE/batch1-20261012/payroll-lowerbound-before.tsx':'apps/web/src/app/payroll/page.tsx','utf8');
for(const name of ['monthlyAmount','amountPerCycle','grossAmount'])test('Native '+name+' accepts the existing API positive-cent lower bound',()=>{
 const input=source.match(new RegExp('name="'+name+'"[^>]*'));assert.ok(input);assert.match(input[0],/min="0\.01"/);assert.match(input[0],/step="0\.01"/);assert.match(input[0],/required/);
});
