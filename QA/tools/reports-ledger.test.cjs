// Actual Reports TSX against controlled authoritative DTOs. No SQL/model acceptance.
const test=require('node:test'), assert=require('node:assert/strict'), fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'), jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const nodes=n=>Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];
const compile=file=>ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function helper(file){const exports={};new Function('require','exports',compile(file))(name=>name==='@/lib/expense-money'?helper('apps/web/src/lib/expense-money.ts'):(()=>{throw Error(name)})(),exports);return exports;}
async function page(config={}){
 let index=0,mounted=false;const state=new Map(),effects=[],module={exports:{}};
 const react={useState:v=>{const key=index++;if(!state.has(key))state.set(key,v);return[state.get(key),v=>state.set(key,v)];},useMemo:fn=>fn(),useEffect:fn=>{if(!mounted)effects.push(fn);}};
 const invoice={id:'invoice',studentId:'student',invoiceNumber:'CANONICAL',totalAmount:1000,adjustedAmount:200,paidAmount:600,balance:200,currency:'INR',dueDate:'2026-10-25',status:'PartiallyPaid'};
 const data={students:[{id:'student',firstName:'Synthetic',lastName:'Learner',isActive:true}],teachers:[],batches:[],enrollments:[],sessions:[],
  invoices:config.invoices??[invoice],payments:config.payments??[{invoiceId:'invoice',amount:600,currency:'INR',status:'Reconciled'},{invoiceId:'invoice',amount:900,currency:'INR',status:'Voided'}],
  expenses:config.expenses??[],payouts:config.payroll??[]};
 const calls=[];
 new Function('require','module','exports',compile('apps/web/src/app/reports/page.tsx'))(name=>{
  if(name==='react')return react;if(name==='react/jsx-runtime')return jsx;
  if(name==='@/lib/api')return{academyApi:async url=>{calls.push(url);return{ok:true,status:200,json:async()=>url==='/api/academies'?[{id:'owned'}]:data[url.split('/').at(-1)]};}};
  if(name==='@/components/design-system/controls')return{StandardInteractiveTile:'tile',StandardDetailModal:'modal'};
  if(name==='@/lib/spreadsheet-csv')return helper('apps/web/src/lib/spreadsheet-csv.ts');
  if(name==='@/lib/report-money')return helper('apps/web/src/lib/report-money.ts');
  throw Error(name);
 },module,module.exports);
 const render=()=>{index=0;const tree=module.exports.default();mounted=true;return tree;};
 render();for(const fn of effects.splice(0))fn();for(let i=0;i<6;i++){await new Promise(r=>setImmediate(r));render();}
 return{render,calls,tile:label=>nodes(render()).find(n=>n.type==='tile'&&n.props.label===label),
  details:()=>nodes(render()).filter(n=>Array.isArray(n.props?.rows)).flatMap(n=>n.props.rows)};
}
test('Reports includes reconciled collections, excludes voids, uses canonical adjusted balance',async()=>{
 const p=await page();assert.match(p.tile('Collected').props.value,/600\.00/);
 assert.match(p.tile('Outstanding').props.value,/200\.00/);
 assert.doesNotMatch(p.tile('Outstanding').props.value,/1,000\.00|400\.00/);
 p.tile('collected')?.props.onClick();
 p.tile('Collected').props.onClick();assert.equal(p.details().length,1);assert.match(p.details()[0][1],/600\.00/);
 p.tile('Outstanding').props.onClick();assert.equal(p.details().length,1);assert.match(p.details()[0][1],/200\.00/);
});
test('Reports never offsets one settled/credit invoice against another canonical debt',async()=>{
 const p=await page({invoices:[{id:'old',studentId:'student',invoiceNumber:'SETTLED',totalAmount:1000,adjustedAmount:0,paidAmount:1500,balance:0,currency:'INR',status:'Paid'},
  {id:'new',studentId:'student',invoiceNumber:'DUE',totalAmount:400,adjustedAmount:0,paidAmount:0,balance:400,currency:'INR',status:'Issued'}]});
 assert.match(p.tile('Outstanding').props.value,/400\.00/);
 p.tile('Outstanding').props.onClick();assert.equal(p.details().length,1);assert.match(p.details()[0][1],/DUE/);
});
test('Reports preserves currencies and separates fee receivables from payroll obligations',async()=>{
 const p=await page({invoices:[{id:'a',totalAmount:100,balance:40,currency:'INR',status:'Issued'},{id:'b',totalAmount:30,balance:20,currency:'USD',status:'Issued'}],
  payments:[{invoiceId:'a',amount:60,currency:'INR',status:'Completed'},{invoiceId:'b',amount:10,currency:'USD',status:'Reconciled'}],
  payroll:[{workerName:'Pending worker',periodLabel:'Synthetic',netAmount:50,netAmountExact:'50.00',currency:'INR',status:'Pending'},
   {workerName:'No cash worker',periodLabel:'Synthetic',netAmount:0,netAmountExact:'0.00',currency:'INR',status:'NoCashDue'}]});
 assert.match(p.tile('Collected').props.value,/60\.00/);assert.match(p.tile('Collected').props.value,/10\.00/);
 assert.doesNotMatch(p.tile('Collected').props.value,/70\.00/);
 assert.match(p.tile('Outstanding').props.value,/Fees/);assert.match(p.tile('Outstanding').props.value,/Payroll/);
 p.tile('Outstanding').props.onClick();assert.equal(p.details().length,3);
 assert.ok(!p.details().flat().some(value=>String(value).includes('No cash worker')));
});
test('Reports expenses use exact wire amounts rather than unsafe Number approximations',async()=>{
 const p=await page({expenses:[{description:'Exact maximum',category:'Synthetic',amount:10000000000000000,amountExact:'9999999999999999.99',currency:'INR',expenseDate:'2026-10-21'}]});
 assert.match(p.tile('Expenses').props.value,/99,99,99,99,99,99,999\.99/);
 p.tile('Expenses').props.onClick();assert.match(p.details()[0][1],/99,99,99,99,99,99,999\.99/);
});
