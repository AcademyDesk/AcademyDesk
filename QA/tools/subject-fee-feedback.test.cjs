// Actual shared TSX, controlled hooks/transport. Not live SQL/device acceptance.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=fs.readFileSync('apps/web/src/components/student-fee-arrangements.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
const tick=()=>new Promise(r=>setImmediate(r));
async function fixture(config={}){
 let index=0,refIndex=0,effectIndex=0;const state=new Map(),refs=[],deps=[],effects=[],cleanups=[],calls=[],module={exports:{}};
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});assert.equal(url,'/api/academies/owned/students/student/fee-arrangements');
  if(init.method){if(config.writeGate)await config.writeGate;if(config.network)throw Error('Synthetic network');return{ok:!config.status,status:config.status||200};}
  if(config.readGate&&calls.some(c=>c.method))await config.readGate;
  if(config.mountGate&&!calls.some(c=>c.method))await config.mountGate;
  if(config.initialFailure||config.refreshFailure&&calls.some(c=>c.method))return{ok:false,status:503};
  return{ok:true,json:async()=>config.malformed?{}:[]};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/design-system/controls'?{StandardSelectField:'select'}:(()=>{throw Error(n)})(),module,module.exports);
 const wrapper=module.exports.StudentFeeArrangements({academyId:'owned',studentId:'student'});
 const render=()=>{index=refIndex=effectIndex=0;return wrapper.type(wrapper.props);};
 async function settle(){for(let i=0;i<4;i++){for(const fn of effects.splice(0)){const cleanup=fn();if(cleanup)cleanups.push(cleanup);}await tick();render();}}
 render();await settle();state.set(1,'Synthetic Piano');state.set(2,'1250.50');
 const handler=()=>nodes(render()).find(n=>n.type==='form').props.onSubmit;
 return{state,calls,render,settle,wrapper,other:module.exports.StudentFeeArrangements({academyId:'owned',studentId:'other'}),handler,action:()=>handler()({preventDefault(){}}),unmount:()=>cleanups.forEach(fn=>fn()),writes:()=>calls.filter(c=>c.method)};
}
test('Durable accessible success, exact payload, matching reset and decimal constraints',async()=>{const p=await fixture();await p.action();await p.settle();assert.equal(p.state.get(4),'Fee arrangement added.');assert.equal(p.state.get(1),'');assert.equal(p.state.get(2),'');assert.equal(p.state.get(3),'Monthly');assert.equal(p.state.get(6),false);assert.equal(p.writes().length,1);assert.deepEqual(JSON.parse(p.writes()[0].body),{subjectName:'Synthetic Piano',amount:1250.5,frequency:'Monthly',effectiveFrom:new Date().toISOString().slice(0,10)});assert.ok(p.calls.filter(c=>!c.method).every(c=>c.cache==='no-store'));const ns=nodes(p.render());assert.equal(ns.find(n=>n.props?.role==='status').props['aria-live'],'polite');const amount=ns.find(n=>n.props?.type==='number');assert.equal(amount.props.min,'1');assert.equal(amount.props.step,'0.01');});
for(const status of[400,403,500,503])test('HTTP '+status+' retains draft and distinguishes uncertainty',async()=>{const p=await fixture({status});await p.action();assert.equal(p.state.get(1),'Synthetic Piano');assert.equal(p.state.get(2),'1250.50');assert.equal(p.state.get(6),false);assert.match(p.state.get(4),status>=500?/could not be confirmed.*before retrying/:/could not be added.*retained/);assert.equal(p.writes().length,1);assert.equal(p.calls.length,2);});
test('Network ambiguity retains draft without retry',async()=>{const p=await fixture({network:true});await p.action();assert.match(p.state.get(4),/could not be confirmed/);assert.equal(p.state.get(2),'1250.50');assert.equal(p.calls.length,2);});
test('Committed write and failed refresh remain confirmed',async()=>{const p=await fixture({refreshFailure:true});await p.action();assert.match(p.state.get(4),/^Fee arrangement added.*could not be refreshed.*do not repeat/);assert.equal(p.state.get(2),'');assert.equal(p.writes().length,1);});
for(const config of[{initialFailure:true},{malformed:true}])test('Failed/malformed initial load disables writes '+JSON.stringify(config),async()=>{const p=await fixture(config);await p.action();assert.equal(p.writes().length,0);assert.equal(p.state.get(5),false);assert.match(p.state.get(4),/could not be loaded/);assert.ok(nodes(p.render()).filter(n=>['input','select','button'].includes(n.type)).every(n=>n.props.disabled));});
for(const stage of['write','readback'])test(stage+' synchronous stale-handler double submit guarded',async()=>{let release;const gate=new Promise(r=>release=r),p=await fixture(stage==='write'?{writeGate:gate}:{readGate:gate}),handler=p.handler();const first=handler({preventDefault(){}});await tick();const second=handler({preventDefault(){}});try{assert.equal(p.writes().length,1);assert.ok(nodes(p.render()).filter(n=>['input','select','button'].includes(n.type)).every(n=>n.props.disabled));}finally{release();}await Promise.all([first,second]);assert.equal(p.state.get(6),false);assert.equal(p.writes().length,1);});
test('Later draft not erased by matching-only confirmed reset',async()=>{let release;const gate=new Promise(r=>release=r),p=await fixture({writeGate:gate}),first=p.action();p.state.set(1,'Later subject');p.state.set(2,'1500.25');release();await first;assert.equal(p.state.get(1),'Later subject');assert.equal(p.state.get(2),'1500.25');});
test('Student scope keyed to fresh editor',async()=>{const p=await fixture();assert.notEqual(p.wrapper.key,p.other.key);assert.deepEqual(JSON.parse(p.wrapper.key),['owned','student']);});
for(const stage of['mount','write','readback'])test('Unmount during '+stage+' ignores late results and stale handler',async()=>{let release;const gate=new Promise(r=>release=r),p=await fixture(stage==='mount'?{mountGate:gate}:stage==='write'?{writeGate:gate}:{readGate:gate});let first;if(stage!=='mount')first=p.action();await tick();p.unmount();const snapshot=[...p.state];release();if(first)await first;await p.settle();assert.deepEqual([...p.state],snapshot);const before=p.writes().length;await p.action();assert.equal(p.writes().length,before);});
test('Rerender does not reload initial list or erase success',async()=>{const p=await fixture();await p.settle();assert.equal(p.calls.length,1);await p.action();await p.settle();assert.equal(p.calls.length,3);assert.equal(p.state.get(4),'Fee arrangement added.');});
