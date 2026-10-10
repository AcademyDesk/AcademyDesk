// Actual TSX handlers with synthetic hooks/transport. Not live SQL/device proof.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_WORK_QUEUE_BASELINE==='1'?require('node:child_process').execFileSync('git',['show','HEAD:apps/web/src/app/work-queue/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/work-queue/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
const tick=()=>new Promise(r=>setImmediate(r));
async function page(config={}){
 let index=0,refIndex=0,effectIndex=0,resets=0;const state=new Map(),refs=[],deps=[],effects=[],calls=[],module={exports:{}};
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});if(init.method){if(config.writeGate)await config.writeGate;if(config.network)throw Error('Synthetic network');return{ok:!config.status,status:config.status||201};}
  if(calls.some(c=>c.method)){if(config.readGate)await config.readGate;if(config.refreshFailure)return{ok:false,status:503};}
  if(url==='/api/academies')return{ok:true,json:async()=>[{id:'owned'}]};
  if(url.endsWith('/staff'))return{ok:!config.staffFailure,json:async()=>[]};
  assert.match(url,/^\/api\/academies\/owned\/admin-work-items(?:\?status=Open)?$/);
  if(url.endsWith('?status=Open')&&config.filterGate)await config.filterGate;
  return{ok:true,json:async()=>[{id:'work',type:'Operations',title:'Existing action',priority:'Normal',status:'Open'}]};
 };
 class Form{constructor(element){assert.ok(element);}get(name){return name==='title'?'Draft action':config.blank?'':'Fixture context';}}
 new Function('require','module','exports','FormData',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/design-system/controls'?{StandardDateField:'date',StandardTimeField:'time',StandardSelectField:'select'}:(()=>{throw Error(n)})(),module,module.exports,Form);
 const render=()=>{index=refIndex=effectIndex=0;return module.exports.default();};
 async function settle(){for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 render();await settle();if(!config.blank){state.set(7,'staff');state.set(8,'2026-10-31');state.set(9,'11:15');}
 const event=()=>{let target={reset(){resets++;}};queueMicrotask(()=>target=null);return{preventDefault(){},get currentTarget(){return target;}};};
 const handler=kind=>kind==='create'?()=>nodes(render()).find(n=>n.type==='form').props.onSubmit(event()):async()=>{nodes(render()).find(n=>n.props?.name==='status-work').props.onChange('Completed');await settle();};
 return{state,calls,render,settle,handler,resets:()=>resets,writes:()=>calls.filter(c=>c.method),notice:()=>state.get(4)};
}
for(const kind of['create','move']){
 test(kind+' durable polite success and exact payload',async()=>{const p=await page();await p.handler(kind)();await p.settle();assert.equal(p.notice(),kind==='create'?'Work item added to the operational queue.':'Work item moved to Completed.');assert.equal(nodes(p.render()).find(n=>n.props?.role==='status').props['aria-live'],'polite');assert.equal(p.writes().length,1);assert.deepEqual(JSON.parse(p.writes()[0].body),kind==='create'?{type:'Operations',title:'Draft action',description:'Fixture context',priority:'Normal',entityType:null,entityId:null,assignedUserId:'staff',dueAtUtc:new Date('2026-10-31T11:15:00').toISOString()}:{status:'Completed'});assert.equal(p.resets(),kind==='create'?1:0);assert.equal(p.state.get(10),false);});
 test(kind+' confirmed save survives failed refresh',async()=>{const p=await page({refreshFailure:true});await p.handler(kind)();assert.match(p.notice(),/Work item.*could not be refreshed.*do not repeat/);assert.equal(p.resets(),kind==='create'?1:0);assert.equal(p.writes().length,1);assert.equal(p.state.get(10),false);});
 for(const status of[400,403,500])test(kind+' HTTP '+status+' retains draft and never retries',async()=>{const p=await page({status});await p.handler(kind)();assert.match(p.notice(),status>=500?/could not be confirmed.*before retrying/:/could not be saved.*retained/);assert.equal(p.resets(),0);assert.equal(p.state.get(7),'staff');assert.equal(p.state.get(8),'2026-10-31');assert.equal(p.calls.length,4);assert.equal(p.writes().length,1);assert.equal(p.state.get(10),false);});
 test(kind+' network uncertainty caught without reset',async()=>{const p=await page({network:true});await p.handler(kind)();assert.match(p.notice(),/could not be confirmed.*before retrying/);assert.equal(p.resets(),0);assert.equal(p.writes().length,1);assert.equal(p.state.get(10),false);});
 for(const stage of['write','readback'])test(kind+' '+stage+' blocks same-tick duplicate/opposing write and filter',async()=>{let release;const gate=new Promise(r=>release=r),p=await page(stage==='write'?{writeGate:gate}:{readGate:gate});const action=p.handler(kind),other=p.handler(kind==='create'?'move':'create');p.state.set(4,'Old error');const first=action();await tick();const second=action(),third=other(),settled=Promise.allSettled([first,second,third]);try{assert.equal(p.writes().length,1);const tree=nodes(p.render());assert.ok(tree.find(n=>n.type==='fieldset')?.props.disabled);assert.ok(tree.find(n=>n.props?.name==='workQueueFilter').props.disabled);assert.ok(tree.find(n=>n.props?.name==='status-work').props.disabled);tree.find(n=>n.props?.name==='workQueueFilter').props.onChange('Open');assert.equal(p.state.get(3),'All');assert.notEqual(p.notice(),'Old error');}finally{release();}const results=await settled;for(const result of results)if(result.status==='rejected')throw result.reason;assert.equal(p.writes().length,1);assert.equal(p.state.get(10),false);});
 test(kind+' partial staff refresh remains confirmed',async()=>{const p=await page({staffFailure:true});await p.handler(kind)();assert.match(p.notice(),/Work item.*Staff options could not be refreshed; do not repeat/);assert.equal(p.writes().length,1);});
}
test('Blank optional fields remain null; defaults unchanged',async()=>{const p=await page({blank:true});await p.handler('create')();const b=JSON.parse(p.writes()[0].body);for(const k of['description','entityType','entityId','assignedUserId','dueAtUtc'])assert.equal(b[k],null);assert.equal(b.type,'Operations');assert.equal(b.priority,'Normal');assert.equal(p.state.get(9),'09:00');});
test('Renders do not reload mount data',async()=>{const p=await page();await p.settle();assert.equal(p.calls.length,3);await p.handler('create')();await p.settle();assert.equal(p.calls.length,7);await p.settle();assert.equal(p.calls.length,7);});
test('Older filter response cannot erase newer save confirmation',async()=>{let release;const gate=new Promise(r=>release=r),p=await page({filterGate:gate});nodes(p.render()).find(n=>n.props?.name==='workQueueFilter').props.onChange('Open');await tick();nodes(p.render()).find(n=>n.props?.name==='workQueueFilter').props.onChange('All');await p.settle();await p.handler('create')();release();await p.settle();assert.equal(p.notice(),'Work item added to the operational queue.');assert.equal(p.state.get(3),'All');assert.equal(p.writes().length,1);});
