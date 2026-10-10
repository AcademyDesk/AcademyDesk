// Actual TSX handlers, controlled hooks/API. Not live Identity/SQL acceptance.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_SIGN_OFF_BASELINE==='1'?require('node:child_process').execFileSync('git',['show','HEAD:apps/web/src/app/access-review/sign-off/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/access-review/sign-off/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
const tick=()=>new Promise(r=>setImmediate(r));
async function page(config={}){
 let index=0,refIndex=0,effectIndex=0,resets=0;const state=new Map(),refs=[],deps=[],effects=[],calls=[],module={exports:{}};
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});if(init.method){assert.equal(url,'/api/academies/owned/access-reviews');assert.equal(init.method,'POST');if(config.writeGate)await config.writeGate;if(config.network)throw Error('Synthetic network');return{ok:!config.status,status:config.status||201};}
  if(url==='/api/academies')return{ok:!config.academyFailure,json:async()=>config.noAcademy?[]:[{id:'owned'}]};
  assert.equal(url,'/api/academies/owned/access-reviews');
  if(calls.some(c=>c.method)){if(config.readGate)await config.readGate;if(config.refreshFailure)return{ok:false,status:503};}
  return{ok:!config.initialFailure,json:async()=>config.malformed?{}:[]};
 };
 class Form{constructor(element){assert.ok(element);}get(name){return name==='notes'?'  Synthetic review notes  ':config.followUp?'on':null;}}
 new Function('require','module','exports','FormData',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:(()=>{throw Error(n)})(),module,module.exports,Form);
 const render=()=>{index=refIndex=effectIndex=0;return module.exports.default();};
 async function settle(){for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 render();await settle();
 const event=()=>{let element={reset(){resets++;}};queueMicrotask(()=>element=null);return{preventDefault(){},get currentTarget(){return element;}};};
 const handler=()=>nodes(render()).find(n=>n.type==='form').props.onSubmit;
 return{state,calls,render,settle,handler,event,resets:()=>resets,writes:()=>calls.filter(c=>c.method),notice:()=>state.get(2)};
}
for(const followUp of[false,true])test('Durable accessible confirmation, exact follow-up '+followUp,async()=>{const p=await page({followUp});await p.handler()(p.event());await p.settle();assert.equal(p.notice(),'Access review signed off.');assert.deepEqual(JSON.parse(p.writes()[0].body),{notes:'  Synthetic review notes  ',createFollowUp:followUp});assert.equal(p.resets(),1);assert.equal(p.writes().length,1);assert.equal(p.state.get(3),false);assert.equal(nodes(p.render()).find(n=>n.props?.role==='status').props['aria-live'],'polite');assert.equal(nodes(p.render()).find(n=>n.type==='textarea').props['aria-label'],'Review notes');});
test('Confirmed save, failed readback is still success',async()=>{const p=await page({refreshFailure:true});await p.handler()(p.event());assert.match(p.notice(),/^Access review signed off.*could not be refreshed; do not repeat/);assert.equal(p.resets(),1);assert.equal(p.writes().length,1);assert.equal(p.state.get(3),false);});
for(const status of[400,403,500,503])test('HTTP '+status+' preserves form, no automatic retries',async()=>{const p=await page({status,followUp:true});await p.handler()(p.event());assert.match(p.notice(),status>=500?/could not be confirmed.*before retrying/:/could not be saved.*retained/);assert.equal(p.resets(),0);assert.equal(p.calls.length,3);assert.equal(p.writes().length,1);assert.equal(p.state.get(3),false);});
test('Network uncertainty caught, form untouched',async()=>{const p=await page({network:true});await p.handler()(p.event());assert.match(p.notice(),/could not be confirmed.*before retrying/);assert.equal(p.resets(),0);assert.equal(p.writes().length,1);assert.equal(p.state.get(3),false);});
for(const stage of['write','readback'])test(stage+' locks same-tick stale handler through refresh',async()=>{let release;const gate=new Promise(r=>release=r),p=await page(stage==='write'?{writeGate:gate}:{readGate:gate});p.state.set(2,'Old error');const h=p.handler(),first=h(p.event());await tick();const second=h(p.event()),settled=Promise.allSettled([first,second]);try{assert.equal(p.writes().length,1);assert.ok(nodes(p.render()).find(n=>n.type==='fieldset')?.props.disabled);assert.notEqual(p.notice(),'Old error');}finally{release();}for(const r of await settled)if(r.status==='rejected')throw r.reason;assert.equal(p.writes().length,1);assert.equal(p.resets(),1);assert.equal(p.state.get(3),false);});
for(const config of[{noAcademy:true},{academyFailure:true},{initialFailure:true},{malformed:true}])test('Unavailable initial history '+JSON.stringify(config)+' cannot submit',async()=>{const p=await page(config);assert.match(p.notice(),/could not be loaded/);assert.ok(nodes(p.render()).find(n=>n.type==='fieldset')?.props.disabled);await p.handler()(p.event());assert.equal(p.writes().length,0);assert.equal(p.resets(),0);});
test('Renders do not refetch initial history; save fetches history once',async()=>{const p=await page();await p.settle();assert.equal(p.calls.length,2);await p.handler()(p.event());await p.settle();assert.equal(p.calls.length,4);await p.settle();assert.equal(p.calls.length,4);});
