// Actual TSX handlers; controlled hooks and synthetic transport, not SQL/role acceptance.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const tick=()=>new Promise(r=>setImmediate(r));
const nodes=n=>Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];
const drafts=[[8,'batch'],[9,' Draft assessment '],[10,'Recital'],[11,'125.5'],[12,'2026-10-20'],[13,'11:15'],[14,'scheme']];
async function page(config={}) {
 const source=process.env.QA_UI_BATCH_BASELINE==='1'?require('node:child_process').execFileSync('git',['show','78579800cb57b32d45a7d25a52179236e55e43fa:apps/web/src/app/assessments/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/assessments/page.tsx','utf8');
 const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
 let i=0,r=0,e=0;const state=new Map(),refs=[],deps=[],effects=[],calls=[],module={exports:{}};
 const options={batches:[{id:'batch',name:'Synthetic batch'}],students:[{id:'student',firstName:'Synthetic',lastName:'Learner'}],enrollments:[{studentId:'student',batchId:'batch',status:'Active'}],gradingSchemes:[{id:'scheme',name:'Synthetic scheme',passingPercent:50}]};
 const assessments=[{id:'assessment',batchId:'batch',title:'Existing assessment',type:'Performance',maxScore:100,isPublished:true}];
 const react={useState:v=>{const k=i++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=r++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=e++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});
  if(init.method){if(config.writeGate)await config.writeGate;if(config.network)throw Error('Synthetic network');return{ok:!config.status,status:config.status||201,json:async()=>({message:'Synthetic validation refusal.'})};}
  if(url==='/api/academies')return{ok:!config.academyStatus,json:async()=>config.noAcademy?[]:[{id:'owned'}]};
  if(url.endsWith('/results'))return{ok:true,json:async()=>[]};
  assert.ok(url.endsWith('/assessments/options')||url.endsWith('/assessments'),url);
  if(calls.some(c=>c.method)){if(config.readGate)await config.readGate;if(config.refreshFailure)return{ok:false,status:503};}
  return{ok:!config.initialFailure,status:config.initialFailure?403:200,json:async()=>config.malformed?{}:url.endsWith('/options')?options:assessments};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:n==='@/components/design-system/controls'?{StandardSelectField:'select',StandardDateField:'date',StandardTimeField:'time'}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{i=r=e=0;return module.exports.default();};
 async function settle(){for(let i=0;i<5;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 render();await settle();for(const[k,v]of drafts)state.set(k,v);
 const handler=()=>nodes(render()).find(n=>n.type==='form').props.onSubmit;
 const resultHandler=()=>nodes(render()).find(n=>typeof n.type==='function'&&n.type.name==='ResultRow').props.onSave;
 return{state,calls,render,settle,create:()=>handler()({preventDefault(){}}),result:()=>resultHandler()('student',80,'Pass','Result draft',true),writes:()=>calls.filter(c=>c.method),message:()=>state.get(15)};
}
function draft(p,reset){for(const[k,v]of drafts)assert.equal(p.state.get(k),reset&&[9,12,14].includes(k)?'':v);assert.equal(p.state.get(17),false);}
test('Create durable success, exact payload/time conversion and matching draft reset',async()=>{const p=await page();await p.create();await p.settle();assert.equal(p.message(),'Assessment created.');draft(p,true);assert.equal(p.writes().length,1);assert.equal(p.writes()[0].url,'/api/academies/owned/assessments');assert.deepEqual(JSON.parse(p.writes()[0].body),{batchId:'batch',title:' Draft assessment ',type:'Recital',maxScore:125.5,gradingSchemeId:'scheme',scheduledAtUtc:new Date('2026-10-20T11:15:00').toISOString(),isPublished:true});assert.equal(p.state.get(6),'assessment');assert.equal(nodes(p.render()).find(n=>n.props?.role==='status').props['aria-live'],'polite');});
test('Confirmed creation with readback failure remains success, no retry',async()=>{const p=await page({refreshFailure:true});await p.create();assert.match(p.message(),/^Assessment created.*could not be refreshed.*Do not repeat/);draft(p,true);assert.equal(p.writes().length,1);});
for(const status of[400,403,409,500,503])test('Create HTTP'+status+' preserves draft and no false success',async()=>{const p=await page({status});await p.create();assert.match(p.message(),status>=500?/could not be confirmed.*retained.*before retrying/:/Synthetic validation refusal/);draft(p,false);assert.equal(p.writes().length,1);});
test('Create network uncertainty retained without automatic retry',async()=>{const p=await page({network:true});await p.create();assert.match(p.message(),/could not be confirmed/);draft(p,false);assert.equal(p.writes().length,1);});
for(const stage of['write','readback'])test('Create '+stage+' duplicate and result handler overlap blocked',async()=>{let release;const gate=new Promise(r=>release=r),p=await page(stage==='write'?{writeGate:gate}:{readGate:gate});const first=p.create();await tick();const duplicate=p.create(),opposite=p.result();try{assert.equal(p.writes().length,1);assert.equal(p.state.get(17),true);const tree=nodes(p.render());assert.ok(tree.find(n=>n.type==='fieldset').props.disabled);assert.ok(tree.filter(n=>n.type==='select').every(n=>n.props.disabled));assert.ok(tree.find(n=>typeof n.type==='function'&&n.type.name==='ResultRow').props.saving);}finally{release();await Promise.allSettled([first,duplicate,opposite]);}await p.settle();assert.equal(p.writes().length,1);draft(p,true);});
test('Pending result save blocks creation and preserves create draft',async()=>{let release;const gate=new Promise(r=>release=r),p=await page({writeGate:gate});const first=p.result();await tick();const duplicate=p.result(),opposite=p.create();try{assert.equal(p.writes().length,1);assert.ok(nodes(p.render()).find(n=>n.type==='fieldset').props.disabled);}finally{release();await Promise.allSettled([first,duplicate,opposite]);}assert.equal(p.message(),'Assessment result saved.');draft(p,false);assert.equal(p.state.get(16),'');});
test('Optional scheme and schedule remain null',async()=>{const p=await page();p.state.set(12,'');p.state.set(14,'');await p.create();const body=JSON.parse(p.writes()[0].body);assert.equal(body.gradingSchemeId,null);assert.equal(body.scheduledAtUtc,null);});
for(const config of[{academyStatus:403},{initialFailure:true},{malformed:true}])test('Initial unavailable workspace stays disabled '+JSON.stringify(config),async()=>{const p=await page(config);assert.match(p.message(),/could not be loaded/);await p.create();assert.equal(p.writes().length,0);assert.equal(p.state.get(0),undefined);assert.ok(nodes(p.render()).find(n=>n.type==='fieldset').props.disabled);});
test('No academy retains create-academy guidance and blocks writes',async()=>{const p=await page({noAcademy:true});assert.match(p.message(),/Create your academy/);await p.create();assert.equal(p.writes().length,0);});
test('Readback stays on captured academy and narrow academic options, no render reload',async()=>{const p=await page();const count=p.calls.length;await p.settle();assert.equal(p.calls.length,count);await p.create();await p.settle();assert.equal(p.calls.filter(c=>c.url==='/api/academies').length,1);assert.equal(p.calls.filter(c=>c.url.endsWith('/options')).length,2);assert.ok(p.calls.filter(c=>!c.method).every(c=>c.cache==='no-store'));for(const c of p.calls)assert.ok(!/\/(students|batches|enrollments|grading-schemes\/active)$/.test(c.url));});
