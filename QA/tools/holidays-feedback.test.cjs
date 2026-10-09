// Actual TSX/controlled hooks and transport, not live SQL/device proof.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_HOLIDAYS_BASELINE==='1'?require('node:child_process').execFileSync('git',['show','HEAD:apps/web/src/app/holidays/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/holidays/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
const tick=()=>new Promise(r=>setImmediate(r));
async function page(config={}){
 let index=0,refIndex=0,effectIndex=0;const state=new Map(),refs=[],deps=[],effects=[],calls=[],module={exports:{}};let rows=[{id:'existing',name:'Existing holiday',holidayDate:'2026-10-31'}];
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});if(init.method){
   if(config.writeGate)await config.writeGate;if(config.network)throw Error('Synthetic network');
   if(!config.status||config.committed){if(init.method==='DELETE')rows=rows.filter(x=>x.id!=='existing');else rows.push(url.endsWith('defaults')?{id:'default',name:'Fixture default',holidayDate:'2026-01-26'}:{...JSON.parse(init.body),id:'created'});}
   return{ok:!config.status,status:config.status||(init.method==='DELETE'?204:200),json:async()=>{if(!config.status||config.malformed)throw Error('Empty/malformed body');return{message:config.message};}};
  }
  if(url==='/api/academies')return{ok:true,json:async()=>[{id:'owned'}]};
  if(calls.some(c=>c.method)){if(config.readGate)await config.readGate;if(config.refreshFailure)return{ok:false,status:503};}
  assert.equal(url,'/api/academies/owned/holidays');return{ok:true,json:async()=>structuredClone(rows)};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:n==='@/components/design-system/controls'?{StandardDateField:'date'}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=refIndex=effectIndex=0;return module.exports.default();};
 async function settle(){for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 const action=async kind=>{if(kind==='create')await nodes(render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){}});else await nodes(render()).find(n=>n.type==='button'&&text(n)===(kind==='defaults'?'Add India holidays 2026':'Remove')).props.onClick();await tick();await tick();};
 const writes=()=>calls.filter(c=>c.method);render();await settle();nodes(render()).find(n=>n.type==='input').props.onChange({target:{value:'Draft holiday'}});nodes(render()).find(n=>n.type==='date').props.onChange('2026-10-31');
 return{state,calls,render,settle,action,writes,rows:()=>rows};
}
const success={create:'Holiday added.',defaults:'Official India holidays added.',remove:'Holiday removed.'};
for(const kind of Object.keys(success))test(kind+' durable success survives readback and empty bodies',async()=>{
 const p=await page();await p.action(kind);await p.settle();assert.equal(p.state.get(3),success[kind]);assert.equal(p.writes().length,1);assert.equal(p.state.get(4),false);
 const notice=nodes(p.render()).find(n=>n.props?.role==='status');assert.equal(notice.props['aria-live'],'polite');assert.equal(text(notice),success[kind]);
 if(kind==='create')assert.deepEqual(JSON.parse(p.writes()[0].body),{name:'Draft holiday',holidayDate:'2026-10-31',notes:'',isClosed:true,scope:'Custom'});
 if(kind==='defaults')assert.equal(p.writes()[0].body,undefined);if(kind==='remove')assert.equal(p.writes()[0].method,'DELETE');
});
for(const kind of Object.keys(success))test(kind+' saved/readback failure is explicit',async()=>{const p=await page({refreshFailure:true});await p.action(kind);assert.ok(p.state.get(3).startsWith(success[kind]));assert.match(p.state.get(3),/could not be refreshed.*do not repeat/);assert.equal(p.state.get(4),false);});
for(const kind of Object.keys(success))for(const status of [400,500])test(`${kind} HTTP ${status} retains form and never retries`,async()=>{
 const p=await page({status,message:'Synthetic server guidance',committed:status>=500});await p.action(kind);assert.match(p.state.get(3),/Synthetic server guidance/);if(status>=500)assert.match(p.state.get(3),/could not be confirmed.*before retrying/);assert.equal(p.state.get(2).name,'Draft holiday');assert.equal(p.writes().length,1);assert.equal(p.calls.length,3);assert.equal(p.state.get(4),false);
});
for(const kind of Object.keys(success))test(kind+' network uncertainty is caught',async()=>{const p=await page({network:true});await p.action(kind);assert.match(p.state.get(3),/could not be confirmed.*before retrying/);assert.equal(p.state.get(2).name,'Draft holiday');assert.equal(p.state.get(4),false);});
for(const kind of Object.keys(success))test(kind+' malformed rejection has nonblank fallback',async()=>{const p=await page({status:400,malformed:true});await p.action(kind);assert.match(p.state.get(3),/could not be added|could not be removed/);assert.equal(p.state.get(4),false);});
for(const stage of ['write','readback'])test(stage+' blocks repeated and opposing actions',async()=>{
 let release;const gate=new Promise(r=>release=r),p=await page(stage==='write'?{writeGate:gate}:{readGate:gate});const first=p.action('create');await tick();const duplicate=p.action('create'),opposing=p.action('defaults'),remove=p.action('remove');await tick();
 try{assert.equal(p.writes().length,1);assert.ok(nodes(p.render()).filter(n=>['button','input','fieldset'].includes(n.type)).every(n=>n.props.disabled));}finally{release();}
 await Promise.all([first,duplicate,opposing,remove]);assert.equal(p.state.get(4),false);assert.equal(p.writes().length,1);
});
test('Default-first guard blocks create and removal',async()=>{let release;const gate=new Promise(r=>release=r),p=await page({writeGate:gate});const first=p.action('defaults');await tick();const opposing=p.action('remove'),create=p.action('create');try{assert.equal(p.writes().length,1);}finally{release();}await Promise.all([first,opposing,create]);assert.equal(p.writes().length,1);assert.equal(p.state.get(2).name,'Draft holiday');});
test('Draft, notice and saving changes do not reload mount data',async()=>{const p=await page();await p.settle();assert.equal(p.calls.length,2);await p.action('create');await p.settle();assert.equal(p.calls.length,4);p.render();await p.settle();assert.equal(p.calls.length,4);});
