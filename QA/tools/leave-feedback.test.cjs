// Controlled actual TSX hooks/transport; not live SQL/browser/device evidence.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_LEAVE_BASELINE==='1'?require('node:child_process').execFileSync('git',['show','HEAD:apps/web/src/app/leave/page.tsx'],{encoding:'utf8'}):fs.readFileSync('apps/web/src/app/leave/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
const tick=()=>new Promise(r=>setImmediate(r));
async function page(config={}){
 let index=0,refIndex=0,effectIndex=0;const state=new Map(),refs=[],deps=[],effects=[],calls=[],module={exports:{}};
 const rows=[{id:'existing',requesterType:'Student',studentId:'student',startDate:'2026-10-31',endDate:'2026-10-31',reason:'Original reason',status:'Requested'}];
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});
  if(init.method){if(config.writeGate)await config.writeGate;if(config.network)throw Error('Synthetic transport failure');const body=JSON.parse(init.body);
   if(!config.status||config.committed){if(init.method==='POST')rows.push({...body,id:'created',status:'Requested'});else Object.assign(rows[0],body);}
   return{ok:!config.status,status:config.status||200,json:async()=>{if(!config.status)throw Error('Successful leave response body is deliberately empty');if(config.malformed)throw Error('Synthetic malformed error');return{message:config.message};}};
  }
  if(calls.some(c=>c.method)){if(config.readGate)await config.readGate;if(config.refreshFailure)return{ok:false,status:503,json:async()=>({})};}
  const data=url==='/api/academies'?[{id:'owned'}]:url.endsWith('/students')?[{id:'student',firstName:'Synthetic',lastName:'Student'}]:url.endsWith('/teachers')?[{id:'teacher',firstName:'Synthetic',lastName:'Teacher'}]:url.endsWith('/leave-requests')?structuredClone(rows):undefined;
  assert.ok(data,'Unexpected endpoint '+url);return{ok:true,status:200,json:async()=>data};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=refIndex=effectIndex=0;return module.exports.default();};
 async function settle(){for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await tick();render();}}
 const selects=()=>nodes(render()).filter(n=>n.type==='select');const inputs=()=>nodes(render()).filter(n=>n.type==='input');
 const change=(n,value)=>n.props.onChange({target:{value}});
 const submit=()=>nodes(render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){}});
 const decision=async label=>{await nodes(render()).find(n=>n.type==='button'&&text(n)===label).props.onClick();await tick();await tick();};
 const writes=()=>calls.filter(c=>c.method);
 render();await settle();change(selects()[1],'student');change(inputs()[0],'2026-10-31');change(inputs()[1],'2026-10-31');change(nodes(render()).find(n=>n.type==='textarea'),'Draft reason');
 return{state,rows,calls,render,settle,selects,inputs,change,submit,decision,writes};
}
for(const action of ['create','Approve','Reject'])test(action+' durable success and empty successful body compatibility',async()=>{
 const p=await page();await(action==='create'?p.submit():p.decision(action));await p.settle();assert.equal(p.writes().length,1);
 const expected=action==='create'?'Leave request submitted.':`Leave request ${action==='Approve'?'approved':'rejected'}.`;assert.equal(p.state.get(9),expected);
 const notice=nodes(p.render()).find(n=>n.props?.role==='status');assert.equal(notice.props['aria-live'],'polite');assert.equal(text(notice),expected);assert.equal(p.state.get(10),false);
 if(action==='create'){assert.equal(p.state.get(5),'');assert.equal(p.state.get(8),'');}else assert.equal(p.rows[0].status,action==='Approve'?'Approved':'Rejected');
});
for(const type of ['Student','Teacher'])test(type+' sends only matching identity and unchanged date/reason contract',async()=>{
 const p=await page();p.change(p.selects()[0],type);assert.equal(p.state.get(5),'');p.change(p.selects()[1],type.toLowerCase());await p.submit();
 assert.deepEqual(JSON.parse(p.writes()[0].body),{requesterType:type,studentId:type==='Student'?'student':null,teacherId:type==='Teacher'?'teacher':null,startDate:'2026-10-31',endDate:'2026-10-31',reason:'Draft reason'});
});
for(const action of ['create','Approve'])test(action+' confirmed save/readback failure keeps saved guidance',async()=>{
 const p=await page({refreshFailure:true});await(action==='create'?p.submit():p.decision(action));assert.match(p.state.get(9),/submitted\.|approved\./);assert.match(p.state.get(9),/could not be refreshed.*do not submit/);assert.equal(p.state.get(10),false);assert.equal(p.writes().length,1);
});
for(const action of ['create','Approve'])for(const status of [400,403,500,503])test(`${action} HTTP ${status} retains inputs and explains outcome`,async()=>{
 const p=await page({status,message:'Synthetic server guidance',committed:status>=500});await(action==='create'?p.submit():p.decision(action));assert.match(p.state.get(9),/Synthetic server guidance/);if(status>=500)assert.match(p.state.get(9),/could not be confirmed.*before retrying/);
 assert.equal(p.state.get(5),'student');assert.equal(p.state.get(8),'Draft reason');assert.equal(p.calls.length,5);assert.equal(p.writes().length,1);assert.equal(p.state.get(10),false);
});
for(const action of ['create','Approve'])for(const fault of ['network','malformed'])test(action+' '+fault+' is handled without lost inputs',async()=>{
 const p=await page(fault==='network'?{network:true}:{status:400,malformed:true});await(action==='create'?p.submit():p.decision(action));assert.match(p.state.get(9),fault==='network'?/could not be confirmed/:/Check the person|decision could not be saved/);assert.equal(p.state.get(8),'Draft reason');assert.equal(p.state.get(10),false);
});
for(const stage of ['write','readback'])test(stage+' blocks duplicate and opposing actions through completion',async()=>{
 let release;const gate=new Promise(r=>release=r),p=await page(stage==='write'?{writeGate:gate}:{readGate:gate});const first=p.submit();await tick();const duplicate=p.submit(),opposing=p.decision('Reject');await tick();
 try{assert.equal(p.writes().length,1);assert.ok(nodes(p.render()).filter(n=>['select','input','textarea','button'].includes(n.type)).every(n=>n.props.disabled));}finally{release();}
 await Promise.all([first,duplicate,opposing]);assert.equal(p.writes().length,1);assert.equal(p.state.get(10),false);
});
test('Decision-first guard blocks opposite decision and create',async()=>{
 let release;const gate=new Promise(r=>release=r),p=await page({writeGate:gate});const first=p.decision('Approve');await tick();const opposite=p.decision('Reject'),create=p.submit();
 try{assert.equal(p.writes().length,1);}finally{release();}await Promise.all([first,opposite,create]);assert.equal(p.rows[0].status,'Approved');assert.deepEqual(JSON.parse(p.writes()[0].body),{status:'Approved',notes:null});
});

test('Draft, notice and saving renders do not restart the mount loader',async()=>{
 const p=await page();assert.equal(p.calls.length,4);p.change(p.selects()[0],'Teacher');p.change(p.selects()[1],'teacher');await p.settle();assert.equal(p.calls.length,4);
 await p.submit();await p.settle();assert.equal(p.calls.length,8);assert.equal(p.writes().length,1);assert.equal(p.state.get(9),'Leave request submitted.');
 p.render();await p.settle();assert.equal(p.calls.length,8);
});
