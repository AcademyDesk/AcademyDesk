// Actual schedule TSX initialization/change/submit handlers, controlled hooks/API; not browser evidence.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const code=ts.transpileModule(fs.readFileSync('apps/web/src/app/schedule/page.tsx','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const batches=[{id:'a',name:'Assigned',teacherId:'ta',branchId:'ra'},{id:'b',name:'Unassigned',teacherId:null,branchId:null},{id:'c',name:'Teacher only',teacherId:'tb',branchId:null},{id:'d',name:'Branch only',teacherId:null,branchId:'rb'},{id:'e',name:'Other assigned',teacherId:'tb',branchId:'rb'},{id:'f',name:'Omitted defaults'}];
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
async function page(config={}){
 let index=0,effectIndex=0;const state=new Map(),deps=[],effects=[],calls=[],module={exports:{}},sessions=[];
 const data=config.batches??batches;
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,typeof v==='function'?v():v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});
  if(init.method==='POST'){
   if(config.postStatus)return{ok:false,status:config.postStatus,json:async()=>({})};
   const body=JSON.parse(init.body),batch=data.find(b=>b.id===body.batchId);sessions.push({...body,id:'saved-'+sessions.length,teacherId:body.teacherId??batch.teacherId??null,branchId:body.branchId??batch.branchId??null,status:'Scheduled'});
   return{ok:true,status:201,json:async()=>sessions.at(-1)};
  }
  const result=url==='/api/academies'?[{id:'owned',name:'Synthetic academy'}]:url.endsWith('/batches')?data:url.endsWith('/teachers')?[{id:'ta',firstName:'First',lastName:'Teacher'},{id:'tb',firstName:'Second',lastName:'Teacher'}]:url.endsWith('/branches')?[{id:'ra',name:'First branch'},{id:'rb',name:'Second branch'}]:url.endsWith('/sessions')?sessions:undefined;
  if(!result)throw Error('Unexpected endpoint '+url);return{ok:true,status:200,json:async()=>structuredClone(result)};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=effectIndex=0;return module.exports.default();};
 const form=()=>nodes(render()).find(n=>n.type==='form');
 const selectors=()=>nodes(form()).filter(n=>n.type==='select');
 const choose=(key,value)=>selectors()[key].props.onChange({target:{value}});
 const times=(day=5)=>{const inputs=nodes(form()).filter(n=>n.type==='input'&&n.props.type==='datetime-local');inputs[0].props.onChange({target:{value:`2026-10-${String(day).padStart(2,'0')}T10:00`}});inputs[1].props.onChange({target:{value:`2026-10-${String(day).padStart(2,'0')}T11:00`}});};
 const submit=()=>form().props.onSubmit({preventDefault(){}});
 render();for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await new Promise(r=>setImmediate(r));render();}
 return{state,calls,sessions,render,form,selectors,choose,times,submit,writes:()=>calls.filter(x=>x.method==='POST')};
}
const scenarios=[
 {label:'initial-assigned',changes:[],batch:'a',teacher:'ta',branch:'ra'},
 {label:'assigned-to-unassigned',changes:[[0,'b']],batch:'b',teacher:null,branch:null},
 {label:'assigned-to-teacher-only',changes:[[0,'c']],batch:'c',teacher:'tb',branch:null},
 {label:'assigned-to-branch-only',changes:[[0,'d']],batch:'d',teacher:null,branch:'rb'},
 {label:'assigned-to-other-assigned',changes:[[0,'e']],batch:'e',teacher:'tb',branch:'rb'},
 {label:'empty-to-unassigned',changes:[[0,''],[0,'b']],batch:'b',teacher:null,branch:null},
 {label:'deliberate-override-after-switch',changes:[[0,'b'],[1,'tb'],[2,'rb']],batch:'b',teacher:'tb',branch:'rb'},
 {label:'old-override-cleared-on-switch',changes:[[1,'tb'],[2,'rb'],[0,'b']],batch:'b',teacher:null,branch:null},
 {label:'initial-unassigned',first:'b',changes:[],batch:'b',teacher:null,branch:null},
 {label:'unassigned-to-assigned',changes:[[0,'b'],[0,'a']],batch:'a',teacher:'ta',branch:'ra'},
 {label:'omitted-defaults',changes:[[0,'f']],batch:'f',teacher:null,branch:null},
 {label:'same-batch-keeps-deliberate-override',changes:[[1,'tb'],[2,'rb'],[0,'a']],batch:'a',teacher:'tb',branch:'rb'}
];
for(const [index,scenario] of scenarios.entries())test(scenario.label+' uses current defaults/intent in visible selectors and POST',async()=>{
 const data=scenario.first?[batches.find(x=>x.id===scenario.first),...batches.filter(x=>x.id!==scenario.first)]:batches;
 const p=await page({batches:data});for(const change of scenario.changes)p.choose(...change);
 assert.deepEqual(p.selectors().slice(0,3).map(n=>n.props.value),[scenario.batch,scenario.teacher??'',scenario.branch??'']);p.times(index+5);await p.submit();assert.equal(p.writes().length,1);
 const payload=JSON.parse(p.writes()[0].body);assert.equal(payload.batchId,scenario.batch);assert.equal(payload.teacherId,scenario.teacher);assert.equal(payload.branchId,scenario.branch);assert.equal(payload.roomName,null);assert.equal(new Date(payload.endUtc)-new Date(payload.startUtc),3600000);assert.match(payload.startUtc,/T04:30:00\.000Z$/);
 assert.equal(p.sessions[0].teacherId,scenario.teacher);assert.equal(p.sessions[0].branchId,scenario.branch);
 assert.deepEqual(p.selectors().slice(0,3).map(n=>n.props.value),[scenario.batch,scenario.teacher??'',scenario.branch??'']);
 console.log('SCHEDULEDEFAULTS PAYLOAD '+JSON.stringify({label:scenario.label,payload,expected:{teacherId:scenario.teacher,branchId:scenario.branch}}));
});
test('Empty batch selection clears teacher/branch and cannot submit',async()=>{const p=await page();p.choose(0,'');assert.deepEqual(p.selectors().slice(0,3).map(n=>n.props.value),['','','']);p.times();await p.submit();assert.equal(p.writes().length,0);});
test('Repeated switching clears both partially assigned directions',async()=>{const p=await page();p.choose(0,'c');assert.deepEqual(p.selectors().slice(1,3).map(n=>n.props.value),['tb','']);p.choose(0,'d');assert.deepEqual(p.selectors().slice(1,3).map(n=>n.props.value),['','rb']);p.choose(0,'c');assert.deepEqual(p.selectors().slice(1,3).map(n=>n.props.value),['tb','']);});
test('No batches leaves all defaults empty and Schedule disabled',async()=>{const p=await page({batches:[]});assert.deepEqual(p.selectors().slice(0,3).map(n=>n.props.value),['','','']);assert.equal(nodes(p.form()).find(n=>n.type==='button').props.disabled,true);});
test('Batch switching does not reset entered dates/mode/location or issue extra lookups',async()=>{const p=await page();p.times();p.choose(3,'Online');nodes(p.form()).find(n=>n.type==='input'&&!n.props.type).props.onChange({target:{value:'https://meeting.example.invalid/'}});const count=p.calls.length;p.choose(0,'b');assert.equal(p.state.get(8),'2026-10-05T10:00');assert.equal(p.state.get(9),'2026-10-05T11:00');assert.equal(p.state.get(10),'Online');assert.equal(p.state.get(11),'https://meeting.example.invalid/');assert.equal(p.calls.length,count);});
test('HTTP create rejection retains current selection and deliberate override',async()=>{const p=await page({postStatus:400});p.choose(0,'b');p.choose(1,'tb');p.times();await p.submit();assert.deepEqual(p.selectors().slice(0,3).map(n=>n.props.value),['b','tb','']);assert.match(p.state.get(12),/could not be saved/);assert.equal(p.state.get(8),'2026-10-05T10:00');});
test('Optional blank override keeps existing API batch-default inheritance contract',async()=>{const p=await page();p.choose(1,'');p.choose(2,'');p.times();await p.submit();const payload=JSON.parse(p.writes()[0].body);assert.equal(payload.teacherId,null);assert.equal(payload.branchId,null);assert.equal(p.sessions[0].teacherId,'ta');assert.equal(p.sessions[0].branchId,'ra');});
