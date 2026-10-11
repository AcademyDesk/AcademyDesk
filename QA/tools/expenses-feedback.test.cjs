// Actual expense TSX with controlled hooks/transport. Not SQL or device evidence.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_EXPENSES_BASELINE==='1'?require('node:child_process').execFileSync('git',['show','HEAD:apps/web/src/app/expenses/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/expenses/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const moneyModule={exports:{}};new Function('exports',ts.transpileModule(fs.readFileSync('apps/web/src/lib/expense-money.ts','utf8'),{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText)(moneyModule.exports);
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
const tick=()=>new Promise(r=>setImmediate(r));
async function page(config={}){
 let index=0,refIndex=0,effectIndex=0,callbackIndex=0;const state=new Map(),refs=[],callbacks=[],deps=[],effects=[],calls=[],module={exports:{}};
 let rows=[{id:'existing',description:'Existing expense',amount:10.25,currency:'USD',category:'Rent',branchId:null,expenseDate:'2026-10-01',status:'Recorded'},{id:'inr',description:'Local expense',amount:5.50,currency:'INR',category:'General',branchId:null,expenseDate:'2026-10-01',status:'Recorded'}];
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useMemo:fn=>fn(),useCallback:(fn,next)=>{const k=callbackIndex++;if(!callbacks[k]||next.some((v,i)=>v!==callbacks[k].deps[i]))callbacks[k]={fn,deps:next};return callbacks[k].fn;},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});if(init.method){
   if(config.writeGate)await config.writeGate;if(config.network)throw Error('Synthetic response loss');
   if(!config.status||config.committed)rows.push({...JSON.parse(init.body),id:'created',status:'Recorded'});
   return{ok:!config.status,status:config.status||201,json:async()=>{if(config.malformed)throw Error('Malformed');return{message:config.message};}};
  }
  if(url==='/api/academies')return{ok:!config.unauthorized,status:config.unauthorized?401:200,json:async()=>config.empty?[]:[{id:'owned'}]};
  const written=calls.some(c=>c.method);if(written&&config.readGate)await config.readGate;
  if(config.initialFailure&&!written||config.refreshFailure&&written)return{ok:false,status:503};
  if(url.endsWith('/branches'))return config.branchDenied?{ok:false,status:403}:{ok:true,json:async()=>[{id:'branch',name:'Local'}]};
  assert.equal(url,'/api/academies/owned/expenses');return{ok:true,json:async()=>structuredClone(rows)};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/expense-money'?moneyModule.exports:n==='./expenses.module.css'?{default:{content:'expense-test-content'}}:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/design-system/controls'?{StandardDateField:'date',StandardSelectField:'select'}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=refIndex=effectIndex=callbackIndex=0;return module.exports.default();};
 async function settle(){for(let i=0;i<5;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 const field=(placeholder,value)=>nodes(render()).find(n=>n.type==='input'&&n.props.placeholder===placeholder).props.onChange({target:{value}});
 const action=async()=>{await nodes(render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){}});await tick();await tick();};
 const refresh=async()=>{await nodes(render()).find(n=>n.type==='button'&&text(n)==='Refresh expense register').props.onClick();await tick();};
 const writes=()=>calls.filter(c=>c.method),notice=()=>text(nodes(render()).find(n=>n.props?.role==='status'));
 render();await settle();field('e.g. August piano instructor payment','Draft expense');field('Amount in INR','12.50');
 return{calls,render,settle,action,refresh,field,writes,notice,rows:()=>rows};
}
test('Confirmed creation returns to register with durable accessible success',async()=>{
 const p=await page();await p.action();await p.settle();assert.equal(p.notice(),'Expense recorded.');assert.equal(p.writes().length,1);
 assert.equal(nodes(p.render()).find(n=>n.props?.role==='status').props['aria-live'],'polite');
 assert.equal(nodes(p.render()).find(n=>n.props?.placeholder==='Amount in INR').props.value,'');
 assert.deepEqual(JSON.parse(p.writes()[0].body),{description:'Draft expense',amount:12.50,currency:'INR',category:'General',branchId:null,expenseDate:null});
 assert.match(text(p.render()),/Draft expense/);
});
test('Confirmed save/readback failure cannot invite duplicate entry',async()=>{const p=await page({refreshFailure:true});await p.action();assert.match(p.notice(),/Expense recorded.*could not be refreshed.*do not repeat/);assert.equal(nodes(p.render()).find(n=>n.type==='fieldset').props.disabled,true);await p.action();assert.equal(p.writes().length,1);});
for(const status of [400,403])test('HTTP '+status+' retains draft and shows backend guidance',async()=>{
 const p=await page({status,message:'Synthetic field guidance'});await p.action();assert.equal(p.notice(),'Synthetic field guidance');assert.equal(nodes(p.render()).find(n=>n.props?.placeholder==='Amount in INR').props.value,'12.50');assert.equal(p.writes().length,1);
});
test('Malformed rejection uses nonblank safe feedback',async()=>{const p=await page({status:400,malformed:true});await p.action();assert.match(p.notice(),/could not be recorded/);});
for(const config of [{status:500,committed:true},{network:true}])test('Unconfirmed result retains draft, blocks repeat and requires explicit read',async()=>{
 const p=await page(config);await p.action();assert.match(p.notice(),/could not be confirmed.*before retrying/);assert.equal(p.calls.length,4);await p.action();assert.equal(p.writes().length,1);
 assert.equal(nodes(p.render()).find(n=>n.props?.placeholder==='Amount in INR').props.value,'12.50');await p.refresh();assert.match(p.notice(),/Check for the previous expense/);assert.equal(p.writes().length,1);assert.equal(nodes(p.render()).find(n=>n.type==='fieldset').props.disabled,false);
});
for(const stage of ['write','readback'])test(stage+' same-tick duplicate and refresh are blocked',async()=>{
 let release;const gate=new Promise(r=>release=r),p=await page(stage==='write'?{writeGate:gate}:{readGate:gate});const first=p.action();await tick();const duplicate=p.action(),refresh=p.refresh();
 try{assert.equal(p.writes().length,1);assert.equal(nodes(p.render()).find(n=>n.type==='fieldset').props.disabled,true);}finally{release();}
 await Promise.all([first,duplicate,refresh]);assert.equal(p.writes().length,1);assert.equal(nodes(p.render()).find(n=>n.type==='fieldset').props.disabled,false);
});
test('Unrelated late draft changes survive confirmed captured save',async()=>{
 let release;const gate=new Promise(r=>release=r),p=await page({writeGate:gate});const first=p.action();await tick();p.field('e.g. August piano instructor payment','New draft');release();await first;
 assert.equal(nodes(p.render()).find(n=>n.props?.placeholder==='e.g. August piano instructor payment').props.value,'New draft');assert.equal(JSON.parse(p.writes()[0].body).description,'Draft expense');
});
for(const config of [{initialFailure:true},{empty:true},{unauthorized:true}])test('Incomplete initial workspace blocks all writes '+JSON.stringify(config),async()=>{
 const p=await page(config);await p.action();assert.equal(p.writes().length,0);assert.ok(p.notice());assert.equal(nodes(p.render()).find(n=>n.type==='fieldset').props.disabled,true);
});
for(const amount of ['0','-1','NaN'])test('Invalid amount no POST '+amount,async()=>{const p=await page();p.field('Amount in INR',amount);await p.action();assert.equal(p.writes().length,0);assert.match(p.notice(),/positive expense amount/);});
test('Totals retain cents and separate currencies without conversion',async()=>{const p=await page();const kpis=nodes(p.render()).find(n=>n.props?.className==='expenses-kpis');assert.match(text(kpis),/10\.25/);assert.match(text(kpis),/5\.50/);assert.doesNotMatch(text(kpis),/15\.75/);});
test('Draft/filter/notice changes do not trigger additional initial reads',async()=>{const p=await page();await p.settle();assert.equal(p.calls.length,3);p.field('Amount in INR','15');await p.settle();assert.equal(p.calls.length,3);await p.action();await p.settle();assert.equal(p.calls.length,6);});
test('Optional branch denial does not block authorized no-branch expense',async()=>{const p=await page({branchDenied:true});const branch=nodes(p.render()).find(n=>n.type==='select'&&n.props.name==='branchId');assert.equal(branch.props.disabled,true);assert.match(text(p.render()),/without a branch/);await p.action();assert.equal(p.notice(),'Expense recorded.');assert.equal(JSON.parse(p.writes()[0].body).branchId,null);assert.equal(p.writes().length,1);});
test('Maximum decimal is submitted as an exact JSON number lexeme, not a rounded Number',async()=>{const p=await page();p.field('Amount in INR','9999999999999999.99');await p.action();assert.match(p.writes()[0].body,/,"amount":9999999999999999\.99\}$/);assert.doesNotMatch(p.writes()[0].body,/"amount":"/);assert.equal(p.notice(),'Expense recorded.');});
test('Overprecision does not silently round or POST',async()=>{const p=await page();p.field('Amount in INR','1.001');await p.action();assert.equal(p.writes().length,0);assert.match(p.notice(),/two decimal places/);});
