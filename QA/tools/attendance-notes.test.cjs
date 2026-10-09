// Executes the actual TSX handlers with controlled hooks/API, not browser/device evidence.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_ATTENDANCE_BASELINE==='1'?require('node:child_process').execFileSync('git',['show','HEAD:apps/web/src/app/attendance/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/attendance/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
const tick=()=>new Promise(r=>setImmediate(r));
async function page(config={}){
 let index=0,refIndex=0,effectIndex=0;const state=new Map(),refs=[],deps=[],effects=[],calls=[],module={exports:{}};
 const stored={one:[{studentId:'s',status:'Present',notes:Object.hasOwn(config,'notes')?config.notes:'Saved note'}],two:[{studentId:'s',status:'Late',notes:'Session two note'}]};
 if(config.newRecord)stored.one=[];
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});const id=url.match(/sessions\/(one|two)\/attendance/)?.[1];
  if(id){
   if(init.method==='POST'){
    if(config.postGate)await config.postGate;
    if(config.network)throw Error('Synthetic network failure');
    if(config.postStatus){if(config.commitBeforeError){const body=JSON.parse(init.body);stored[id]=[{studentId:body.studentId,status:body.status,notes:body.notes}];}return{ok:false,status:config.postStatus,json:async()=>{if(config.jsonFault)throw Error('Synthetic malformed error');return{message:config.message};}};}
    const body=JSON.parse(init.body);stored[id]=[{studentId:body.studentId,status:body.status,notes:body.notes?.trim()??null}];
    return{ok:true,status:200,json:async()=>stored[id][0]};
   }
   if(config.getGate)await config.getGate(id);
   const afterSave=calls.some(c=>c.method==='POST');
   if(config.refreshFailure&&afterSave)return{ok:false,status:500,json:async()=>null};
   return{ok:true,status:200,json:async()=>config.malformed?null:structuredClone(stored[id])};
  }
  const data=url==='/api/academies'?[{id:'a'}]:url.endsWith('/students')?[{id:'s',firstName:'Synthetic',lastName:'Student'}]:url.endsWith('/batches')?[{id:'b',name:'Synthetic batch'}]:url.endsWith('/enrollments')?[{studentId:'s',batchId:'b',status:'Active'}]:url.endsWith('/sessions')?['one','two'].map(id=>({id,batchId:'b',startUtc:'2026-10-01T10:00:00Z'})):undefined;
  if(!data)throw Error('Unexpected endpoint '+url);return{ok:true,status:200,json:async()=>data};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/components/design-system/controls'?{StandardSelectField:'select'}:n==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=refIndex=effectIndex=0;return module.exports.default();};
 async function settle(){for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 const select=name=>nodes(render()).find(n=>n.type==='select'&&n.props.name===name);
 const input=()=>nodes(render()).find(n=>n.type==='input');
 const button=()=>nodes(render()).find(n=>n.type==='button');
 const writes=()=>calls.filter(c=>c.method==='POST');
 const edit=value=>input().props.onChange({target:{value}});
 async function save(status){if(status)select('attendance-s').props.onChange(status);else button().props.onClick();await tick();await tick();render();}
 render();await settle();return{state,calls,stored,render,settle,select,input,button,writes,edit,save};
}
for(const scenario of [
 {label:'untouched-status',before:'Saved note',status:'Late',expected:'Saved note'},
 {label:'unchanged-save',before:'Saved note',expected:'Saved note'},
 {label:'existing-null',before:null,status:'Present',expected:null},
 {label:'existing-empty',before:'',status:'Online',expected:null},
 {label:'explicit-clear',before:'Saved note',edit:'',expected:null},
 {label:'edited-note',before:'Saved note',edit:'  New note  ',expected:'New note'},
 {label:'unicode-multiline',before:'Saved note',edit:'Music ♪\nSecond line',expected:'Music ♪\nSecond line'},
 {label:'max-length',before:'Saved note',edit:'n'.repeat(500),expected:'n'.repeat(500)},
 {label:'new-optional-note',before:null,newRecord:true,status:'Absent',expected:null},
 {label:'new-typed-note',before:null,newRecord:true,edit:'New attendance',status:'Excused',expected:'New attendance'}
])test(scenario.label+' submits displayed notes and preserves explicit clear',async()=>{
 const p=await page({notes:scenario.before,newRecord:scenario.newRecord});
 if(Object.hasOwn(scenario,'edit'))p.edit(scenario.edit);
 const visible=p.input().props.value;await p.save(scenario.status);assert.equal(p.writes().length,1);
 const payload=JSON.parse(p.writes()[0].body);assert.equal(payload.notes,visible||null);assert.equal(p.stored.one[0].notes,scenario.expected);assert.equal(p.state.get(7),'Attendance saved.');assert.deepEqual(p.state.get(9),{});
 console.log('ATTENDANCE PAYLOAD '+JSON.stringify({label:scenario.label,before:scenario.before,newRecord:Boolean(scenario.newRecord),payload,expected:scenario.expected}));
});
test('Drafts follow their session and survive switching back without leaking',async()=>{
 const p=await page();p.edit('Session one draft');p.select('session').props.onChange('two');await p.settle();assert.equal(p.input().props.value,'Session two note');p.edit('Session two draft');p.select('session').props.onChange('one');await p.settle();assert.equal(p.input().props.value,'Session one draft');await p.save();assert.equal(p.state.get(9)['two:s'],'Session two draft');assert.equal(p.stored.two[0].notes,'Session two note');
});
test('Late session response cannot overwrite the selected session',async()=>{
 let release;const gate=new Promise(r=>release=r);const p=await page({getGate:id=>id==='one'?gate:Promise.resolve()});assert.equal(p.input(),undefined);p.select('session').props.onChange('two');await p.settle();assert.equal(p.input().props.value,'Session two note');release();await p.settle();assert.equal(p.input().props.value,'Session two note');assert.equal(p.state.get(6).one[0].notes,'Saved note');
});
test('Unloaded or malformed records cannot be saved as empty notes',async()=>{const p=await page({malformed:true});assert.equal(p.input(),undefined);assert.equal(p.button(),undefined);assert.match(p.state.get(7),/could not be loaded/);assert.equal(p.writes().length,0);});
test('Same-tick repeated saves produce one request and disable all editable controls',async()=>{
 let release;const postGate=new Promise(r=>release=r),p=await page({postGate});const click=p.button().props.onClick;click();click();await tick();assert.equal(p.writes().length,1);assert.equal(p.select('session').props.disabled,true);assert.equal(p.select('attendance-s').props.disabled,true);assert.equal(p.input().props.disabled,true);assert.equal(p.button().props.disabled,true);release();await tick();await tick();assert.equal(p.button().props.disabled,false);
});
for(const code of [400,403,500])test(`HTTP ${code} retains draft and displays server guidance`,async()=>{const p=await page({postStatus:code,message:'Synthetic access/validation guidance'});p.edit('Keep my draft');await p.save();assert.equal(p.state.get(9)['one:s'],'Keep my draft');assert.equal(p.state.get(7),code>=500?'Synthetic access/validation guidance Attendance could not be confirmed. Your notes have been retained; check the saved record before retrying.':'Synthetic access/validation guidance');assert.equal(p.state.get(8),'');});
test('Empty server guidance falls back to a visible failure message',async()=>{const p=await page({postStatus:500});p.edit('Keep');await p.save();assert.match(p.state.get(7),/could not be confirmed.*before retrying/);assert.equal(p.state.get(9)['one:s'],'Keep');});
for(const message of ['', '  '])test('Blank server guidance remains visible '+JSON.stringify(message),async()=>{const p=await page({postStatus:500,message});await p.save();assert.match(p.state.get(7),/could not be confirmed.*before retrying/);});
test('Saving one student leaves other student drafts untouched',async()=>{const p=await page();p.state.set(9,{'one:s':'First draft','one:t':'Second draft'});await p.save();assert.equal(p.stored.one[0].notes,'First draft');assert.deepEqual(p.state.get(9),{'one:t':'Second draft'});});
test('Network uncertainty retains draft and does not claim success',async()=>{const p=await page({network:true});p.edit('Keep');await p.save();assert.match(p.state.get(7),/could not be confirmed/);assert.equal(p.state.get(9)['one:s'],'Keep');});
test('Saved but failed readback reports saved state and retains draft',async()=>{const p=await page({refreshFailure:true});p.edit('Keep');await p.save();assert.match(p.state.get(7),/saved, but records could not be refreshed/);assert.equal(p.state.get(9)['one:s'],'Keep');assert.equal(p.stored.one[0].notes,'Keep');});
test('Input respects existing 500-character contract; success is announced',async()=>{const p=await page();assert.equal(p.input().props.maxLength,500);await p.save();const notice=nodes(p.render()).find(n=>n.props?.role==='status');assert.equal(notice.props['aria-live'],'polite');assert.equal(text(notice),'Attendance saved.');});

for(const code of [500,502,503])for(const committed of [false,true])test(`HTTP ${code} uncertain outcome committed=${committed} retains draft and never retries`,async()=>{
 const p=await page({postStatus:code,commitBeforeError:committed,message:'Synthetic service unavailable'});p.edit('Keep uncertain note');await p.save();
 assert.match(p.state.get(7),/could not be confirmed.*check the saved record before retrying/);assert.match(p.state.get(7),/Synthetic service unavailable/);assert.doesNotMatch(p.state.get(7),/Attendance saved\./);
 assert.equal(p.state.get(9)['one:s'],'Keep uncertain note');assert.equal(p.input().props.value,'Keep uncertain note');assert.equal(p.writes().length,1);assert.equal(p.state.get(8),'');assert.equal(p.stored.one[0].notes,committed?'Keep uncertain note':'Saved note');
});
test('Malformed 5xx error body remains unconfirmed without losing draft',async()=>{const p=await page({postStatus:500,jsonFault:true});p.edit('Retain HTML-error note');await p.save();assert.match(p.state.get(7),/could not be confirmed/);assert.equal(p.input().props.value,'Retain HTML-error note');assert.equal(p.writes().length,1);});
test('Readback remains guarded against repeated saves and status changes',async()=>{
 let release,hold=false;const gate=new Promise(r=>release=r),p=await page({getGate:()=>hold?gate:Promise.resolve()});p.edit('Retain slow note');hold=true;
 try {await p.save();await p.save('Absent');assert.equal(p.writes().length,1);assert.equal(p.input().props.disabled,true);assert.equal(p.button().props.disabled,true);assert.equal(p.select('session').props.disabled,true);assert.equal(p.select('attendance-s').props.disabled,true);assert.equal(p.state.get(7),'Attendance saved.');assert.equal(p.state.get(9)['one:s'],'Retain slow note');}
 finally {release();}
 await tick();await tick();assert.equal(p.input().props.disabled,false);assert.equal(p.writes().length,1);assert.deepEqual(p.state.get(9),{});
});
