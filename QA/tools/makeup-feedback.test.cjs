// Actual TSX with controlled hooks/transport; not live SQL or physical-device proof.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_MAKEUP_BASELINE==='1'?require('node:child_process').execFileSync('git',['show','HEAD:apps/web/src/app/makeup/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/makeup/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
const tick=()=>new Promise(r=>setImmediate(r));
async function page(config={}){
 let index=0,refIndex=0,effectIndex=0;const state=new Map(),refs=[],deps=[],effects=[],calls=[],module={exports:{}};
 const rows=[{id:'existing',studentId:'student',batchId:'batch',startUtc:'2026-10-31T03:30:00Z',deliveryMode:'Offline',venue:'Original room',usesNextScheduledClass:false,status:'Scheduled'}];
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,typeof v==='function'?v():v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});
  if(init.method){
   if(config.writeGate)await config.writeGate;
   if(config.network)throw Error('Synthetic transport failure');
   const body=JSON.parse(init.body);
   if(!config.status||config.committed){if(init.method==='POST')rows.push({...body,id:'created',usesNextScheduledClass:body.useNextScheduledClass,status:'Scheduled'});else Object.assign(rows[0],body);}
   return{ok:!config.status,status:config.status||201,json:async()=>{if(config.malformed)throw Error('Synthetic malformed body');return{message:config.message};}};
  }
  const afterWrite=calls.some(c=>c.method);
  if(afterWrite&&config.readGate)await config.readGate;
  if(afterWrite&&config.refreshFailure)return{ok:false,status:503,json:async()=>({})};
  const data=url==='/api/academies'?[{id:'owned'}]:url.endsWith('/students')?[{id:'student',firstName:'Synthetic',lastName:'Student'}]:url.endsWith('/batches')?[{id:'batch',name:'Synthetic batch'}]:url.endsWith('/teachers')?[]:url.endsWith('/makeup-classes')?structuredClone(rows):undefined;
  assert.ok(data,'Unexpected endpoint '+url);return{ok:true,status:200,json:async()=>data};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:n==='@/components/design-system/controls'?{StandardSelectField:'select',StandardDateField:'date',StandardTimeField:'time'}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=refIndex=effectIndex=0;return module.exports.default();};
 async function settle(){for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 const control=name=>nodes(render()).find(n=>n.props?.name===name);
 const input=placeholder=>nodes(render()).find(n=>n.props?.placeholder===placeholder);
 const submit=()=>nodes(render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){}});
 const writes=()=>calls.filter(c=>c.method);
 render();await settle();control('student').props.onChange('student');control('batch').props.onChange('batch');control('makeup-date').props.onChange('2026-10-31');input('Room or venue (optional)').props.onChange({target:{value:'Draft room'}});
 return{state,rows,calls,render,settle,control,input,submit,writes,status:async value=>{await control('makeup-status-existing').props.onChange(value);await tick();await tick();}};
}
for(const action of ['create','status'])test(action+' announces durable success after readback',async()=>{
 const p=await page();await(action==='create'?p.submit():p.status('Completed'));await p.settle();
 assert.equal(p.writes().length,1);assert.equal(p.state.get(13),action==='create'?'Make-up class scheduled.':'Make-up status updated to Completed.');
 const notice=nodes(p.render()).find(n=>n.props?.role==='status');assert.equal(notice.props['aria-live'],'polite');assert.equal(text(notice),p.state.get(13));
 assert.equal(p.state.get(14),false);assert.equal(p.rows.length,action==='create'?2:1);
 if(action==='create'){assert.equal(p.state.get(9),'');assert.equal(p.state.get(11),'');}else assert.equal(p.rows[0].status,'Completed');
});
for(const action of ['create','status'])test(action+' committed save/readback failure is not reported as failed write',async()=>{
 const p=await page({refreshFailure:true});await(action==='create'?p.submit():p.status('Cancelled'));
 assert.match(p.state.get(13),/scheduled\.|updated to Cancelled\./);assert.match(p.state.get(13),/could not be refreshed.*do not submit/);assert.equal(p.writes().length,1);assert.equal(p.state.get(14),false);
});
for(const action of ['create','status'])for(const status of [400,403,500,503])test(`${action} HTTP ${status} preserves draft and server guidance`,async()=>{
 const p=await page({status,message:'Synthetic validation guidance',committed:status>=500});await(action==='create'?p.submit():p.status('Completed'));
 assert.match(p.state.get(13),/Synthetic validation guidance/);if(status>=500)assert.match(p.state.get(13),/could not be confirmed.*before retrying/);
 assert.equal(p.state.get(9),'2026-10-31T09:00');assert.equal(p.state.get(11),'Draft room');assert.equal(p.state.get(14),false);assert.equal(p.writes().length,1);assert.equal(p.calls.length,6);
});
for(const action of ['create','status'])test(action+' network uncertainty is caught and retains inputs',async()=>{
 const p=await page({network:true});await(action==='create'?p.submit():p.status('Completed'));
 assert.match(p.state.get(13),/could not be confirmed.*before retrying/);assert.equal(p.state.get(11),'Draft room');assert.equal(p.state.get(14),false);assert.equal(p.writes().length,1);
});
for(const action of ['create','status'])test(action+' malformed rejection produces nonblank fallback',async()=>{
 const p=await page({status:400,malformed:true});await(action==='create'?p.submit():p.status('Completed'));assert.match(p.state.get(13),/could not be scheduled|could not be updated/);
});
for(const stage of ['write','readback'])test(stage+' guard blocks repeated and opposing actions until settled',async()=>{
 let release;const gate=new Promise(r=>release=r),p=await page(stage==='write'?{writeGate:gate}:{readGate:gate});
 const first=p.submit();await tick();const duplicate=p.submit(),opposing=p.status('Cancelled');await tick();
 try{assert.equal(p.writes().length,1);assert.equal(p.control('student').props.disabled,true);assert.equal(p.control('makeup-status-existing').props.disabled,true);assert.equal(p.input('Room or venue (optional)').props.disabled,true);assert.equal(nodes(p.render()).find(n=>n.type==='fieldset').props.disabled,true);}finally{release();}
 await Promise.all([first,duplicate,opposing]);assert.equal(p.state.get(14),false);assert.equal(p.writes().length,1);
});
test('Status-first guard blocks duplicate/opposing status and create requests',async()=>{
 let release;const gate=new Promise(r=>release=r),p=await page({writeGate:gate});const first=p.status('Completed');await tick();const duplicate=p.status('Cancelled'),opposing=p.submit();
 try{assert.equal(p.writes().length,1);}finally{release();}await Promise.all([first,duplicate,opposing]);assert.equal(p.rows[0].status,'Completed');assert.equal(p.state.get(14),false);
});
test('Online rejected scheduling keeps its link and manual mode',async()=>{
 const p=await page({status:400});p.control('delivery-mode').props.onChange('Online');p.input('Meeting link').props.onChange({target:{value:'https://meeting.example.invalid/draft'}});await p.submit();assert.equal(p.state.get(12),'https://meeting.example.invalid/draft');assert.equal(p.state.get(8),'Manual');assert.equal(p.writes().length,1);
});
