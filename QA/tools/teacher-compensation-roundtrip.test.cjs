// Actual TSX save/load handlers; synthetic hooks/transport, not native SQL/device acceptance.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),cp=require('node:child_process');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_SOURCE_REF?cp.execFileSync('git',['show',process.env.QA_SOURCE_REF+':apps/web/src/app/teacher-payments/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/teacher-payments/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const nodes=n=>Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];
const tick=()=>new Promise(r=>setImmediate(r));
async function page(initial,config={}){
 let index=0,mounted=false,value={...initial};const state=[],effects=[],calls=[],module={exports:{}};
 const react={useCallback: fn => fn, useState:v=>{const k=index++;if(!(k in state))state[k]=v;return[state[k],v=>state[k]=typeof v==='function'?v(state[k]):v];},useEffect:fn=>{if(!mounted){mounted=true;effects.push(fn);}}};
 const api=async(url,init={})=>{calls.push({url,...init});if(url==='/api/academies')return{ok:true,json:async()=>[{id:'owned'}]};if(url.endsWith('/teachers'))return{ok:true,json:async()=>[{id:'teacher',firstName:'Synthetic',lastName:'Teacher',isActive:true}]};
  if(init.method){if(!config.rejected)value={...JSON.parse(init.body)};return{ok:!config.rejected,json:async()=>config.rejected?{message:'Synthetic policy rejection.'}:value};}return{ok:true,json:async()=>value};};
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({'Content-Type':'application/json'})}:n==='@/components/design-system/controls'?{StandardDateField:'date',StandardSelectField:'select'}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=0;return module.exports.default();};async function settle(){for(let i=0;i<6;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 const submit=()=>nodes(render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){}});
 const edit=(key,value)=>{const input=nodes(render()).find(n=>n.type==='input'&&n.props.value===state[3][key]);assert.ok(input);input.props.onChange({target:{value}});};
 render();await settle();return{render,settle,submit,edit,state,calls};
}
const fields=['monthlySalary','standardHourlyRate','beginnerHourlyRate','intermediateHourlyRate','advancedHourlyRate'];
const base=field=>({model:field==='monthlySalary'?'Monthly':'Hourly',monthlySalary:null,standardHourlyRate:field==='monthlySalary'?null:10,beginnerHourlyRate:null,intermediateHourlyRate:null,advancedHourlyRate:null,effectiveFrom:null});
for(const field of fields)for(const input of[0,'0',125.5,'125.50',null,''])test(`${field}: loaded/typed ${JSON.stringify(input)} retains zero vs absence`,async()=>{
 const initial={...base(field),[field]:input},p=await page(initial);await p.submit();await p.settle();const writes=p.calls.filter(c=>c.method==='PUT');assert.equal(writes.length,1);assert.equal(writes[0].url,'/api/academies/owned/teachers/teacher/compensation');
 const expected=input==null||input===''?null:Number(input);assert.deepEqual(JSON.parse(writes[0].body),{...base(field),[field]:expected});assert.equal(p.state[3][field],expected);assert.equal(p.state[4],'Teacher payment details saved.');
 // Numeric response hydration followed by a second unchanged save must remain identical.
 await p.submit();await p.settle();assert.deepEqual(JSON.parse(p.calls.filter(c=>c.method==='PUT')[1].body),JSON.parse(writes[0].body));
});
for(const field of fields)test(`${field}: persisted numeric zero resave with real input events`,async()=>{const p=await page({...base(field),[field]:22});p.edit(field,'0');await p.submit();await p.settle();assert.equal(p.state[3][field],0);await p.submit();await p.settle();assert.equal(JSON.parse(p.calls.filter(c=>c.method==='PUT')[1].body)[field],0);});
for(const model of['Monthly','Hourly'])test(`${model}: other-model rates omitted and optional date retained`,async()=>{const p=await page({model,monthlySalary:0,standardHourlyRate:0,beginnerHourlyRate:0,intermediateHourlyRate:0,advancedHourlyRate:0,effectiveFrom:'2026-10-01'});await p.submit();await p.settle();const body=JSON.parse(p.calls.find(c=>c.method==='PUT').body);assert.equal(body.effectiveFrom,'2026-10-01');for(const f of fields)assert.equal(body[f],model==='Monthly'?(f==='monthlySalary'?0:null):(f==='monthlySalary'?null:0));});
for(const field of['monthlySalary','standardHourlyRate'])test(`${field}: rejected rate draft not rewritten`,async()=>{const p=await page({...base(field),[field]:-1},{rejected:true});await p.submit();await p.settle();assert.equal(JSON.parse(p.calls.find(c=>c.method==='PUT').body)[field],-1);assert.equal(p.state[3][field],-1);assert.equal(p.state[4],'Synthetic policy rejection.');});
