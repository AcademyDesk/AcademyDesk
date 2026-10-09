// Actual Schedule TSX handlers with controlled hooks/transport, not live API/SQL proof.
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), cp = require('node:child_process');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const source = process.env.QA_SCHEDULE_BASELINE === '1'
  ? cp.execFileSync('git', ['show', 'HEAD:apps/web/src/app/schedule/page.tsx'], {encoding:'utf8'})
  : fs.readFileSync('apps/web/src/app/schedule/page.tsx', 'utf8');
const code = ts.transpileModule(source, {compilerOptions:{module:ts.ModuleKind.CommonJS, jsx:ts.JsxEmit.ReactJSX}}).outputText;
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
const text = n => Array.isArray(n) ? n.map(text).join('') : n == null || typeof n === 'boolean' ? '' : typeof n === 'object' ? text(n.props?.children) : String(n);
const tick = () => new Promise(r => setImmediate(r));
function deferred() { let resolve; const promise = new Promise(r => resolve = r); return {promise, resolve}; }
async function page(config = {}) {
  let index = 0, effectIndex = 0, refIndex = 0;
  const state = [], refs = [], deps = [], effects = [], calls = [], module = {exports:{}};
  const sessions = ['one','two'].map(id => ({id,batchId:'b',teacherId:null,branchId:null,startUtc:'2026-10-15T04:30:00Z',endUtc:'2026-10-15T05:30:00Z',deliveryMode:'InPerson',roomName:null,status:'Scheduled'}));
  const react = {
    useState: value => { const key=index++; if(!(key in state))state[key]=typeof value==='function'?value():value; return [state[key],value=>state[key]=typeof value==='function'?value(state[key]):value]; },
    useRef: value => { const key=refIndex++; return refs[key]??(refs[key]={current:value}); },
    useEffect: (fn,next) => {const key=effectIndex++;if(!deps[key]||next.some((v,i)=>v!==deps[key][i])){deps[key]=next;effects.push(fn);}},
  };
  const api = async (url, init={}) => {
    calls.push({url,...init});
    if(['POST','PUT'].includes(init.method)) {
      if(config.writeGate)await config.writeGate.promise;
      if(config.network)throw Error('Synthetic network loss');
      if(config.status)return {ok:false,status:config.status};
      const body=JSON.parse(init.body);
      if(init.method==='POST')sessions.push({...body,id:'created',status:'Scheduled'});
      else Object.assign(sessions.find(s=>url.endsWith('/'+s.id)),body);
      return {ok:true,status:init.method==='POST'?201:204};
    }
    if(calls.some(x=>['POST','PUT'].includes(x.method))) {
      if(config.refreshGate)await config.refreshGate.promise;
      if(config.refreshFailure==='network')throw Error('Synthetic read failure');
      if(config.refreshFailure==='http')return {ok:false,status:503};
      if(config.refreshFailure==='json')return {ok:true,status:200,json:async()=>{throw Error('Synthetic JSON failure');}};
    }
    const data=url==='/api/academies'?[{id:'owned',name:'Synthetic academy'}]:url.endsWith('/batches')?[{id:'b',name:'Fixture batch'}]:url.endsWith('/teachers')?[]:url.endsWith('/branches')?[]:url.endsWith('/sessions')?sessions:undefined;
    assert.ok(data,'Unexpected endpoint');return {ok:true,status:200,json:async()=>structuredClone(data)};
  };
  new Function('require','module','exports',code)(name=>name==='react'?react:name==='react/jsx-runtime'?jsx:name==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:name==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:(()=>{throw Error(name)})(),module,module.exports);
  const render=()=>{index=effectIndex=refIndex=0;return module.exports.default();};
  const form=()=>nodes(render()).find(n=>n.type==='form');
  const inputs=()=>nodes(form()).filter(n=>n.type==='input');
  const fill=()=>{inputs()[0].props.onChange({target:{value:'2026-10-15T10:00'}});inputs()[1].props.onChange({target:{value:'2026-10-15T11:00'}});inputs()[2].props.onChange({target:{value:'Room fixture'}});};
  const submit=()=>form().props.onSubmit({preventDefault(){}});
  const statusControls=()=>nodes(render()).filter(n=>n.type==='select'&&nodes(n.props.children).some(x=>x.props?.children==='Completed'));
  const update=(status='Cancelled',row=0)=>statusControls()[row].props.onChange({target:{value:status}});
  const notice=()=>nodes(render()).find(n=>n.props?.role==='status');
  render();for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await tick();render();}
  return {render,form,inputs,fill,submit,update,statusControls,notice,state,calls,sessions,writes:()=>calls.filter(x=>['POST','PUT'].includes(x.method)),message:()=>state[12]};
}
test('confirmed create retains accessible success after readback and clears only per-class fields',async()=>{
  const p=await page();p.fill();await p.submit();assert.equal(p.message(),'Class scheduled successfully.');assert.equal(text(p.notice()),p.message());assert.equal(p.notice().props['aria-live'],'polite');assert.deepEqual(p.inputs().map(x=>x.props.value),['','','']);assert.equal(p.sessions.length,3);assert.equal(p.state[5],'b');assert.equal(p.state[10],'InPerson');assert.equal(p.state[13],null);
});
for(const status of ['Scheduled','Completed','Cancelled','NoShow'])test('confirmed status '+status+' survives readback',async()=>{
  const p=await page();p.update(status);await tick();await tick();assert.equal(p.message(),`Class status updated to ${status}.`);assert.equal(p.sessions[0].status,status);assert.equal(p.writes().length,1);assert.equal(p.state[13],null);
  const body=JSON.parse(p.writes()[0].body);assert.equal(body.startUtc,'2026-10-15T04:30:00Z');assert.equal(body.endUtc,'2026-10-15T05:30:00Z');assert.equal(body.roomName,null);
});
for(const operation of ['create','status'])for(const failure of ['network','http','json'])test(operation+' confirmed save but '+failure+' refresh failure keeps confirmation',async()=>{
  const p=await page({refreshFailure:failure});p.fill();if(operation==='create')await p.submit();else{p.update();await tick();await tick();}
  assert.match(p.message(),operation==='create'?/^Class scheduled successfully\./:/^Class status updated to Cancelled\./);assert.match(p.message(),/could not be refreshed.*do not submit/);assert.equal(p.state[13],null);assert.equal(p.writes().length,1);
});
for(const operation of ['create','status'])for(const failure of [400,500,'network'])test(operation+' '+failure+' write failure never claims success',async()=>{
  const p=await page(failure==='network'?{network:true}:{status:failure});p.fill();if(operation==='create')await p.submit();else{p.update();await tick();await tick();}
  assert.doesNotMatch(p.message(),/successfully|updated to/);assert.match(p.message(),failure===400?/could not be saved|could not be updated/:/could not be confirmed.*before trying again/);assert.deepEqual(p.inputs().map(x=>x.props.value),['2026-10-15T10:00','2026-10-15T11:00','Room fixture']);assert.equal(p.state[13],null);assert.equal(p.sessions.length,2);assert.equal(p.sessions[0].status,'Scheduled');
});
for(const phase of ['write','refresh'])for(const operation of ['create','status'])test(operation+' '+phase+' pending guards same/opposing stale handlers until completion',async()=>{
  const gate=deferred(),p=await page(phase==='write'?{writeGate:gate}:{refreshGate:gate});p.fill();
  const submit=p.form().props.onSubmit,change=p.statusControls()[0].props.onChange;
  const action=operation==='create'?submit({preventDefault(){}}):change({target:{value:'Cancelled'}});await tick();
  try {
    const repeat=submit({preventDefault(){}});change({target:{value:'Completed'}});p.update('NoShow',1);await tick();assert.equal(p.writes().length,1);
    assert.ok(nodes(p.form()).filter(n=>['input','select','button'].includes(n.type)).every(n=>n.props.disabled));assert.ok(p.statusControls().every(n=>n.props.disabled));
    gate.resolve();await repeat;
  } finally { gate.resolve(); }
  await action;await tick();await tick();assert.equal(p.state[13],null);assert.equal(p.writes().length,1);
});
for(const mode of ['Online','Hybrid'])test(mode+' still requires meeting link before any write',async()=>{
  const p=await page();p.fill();nodes(p.form()).filter(n=>n.type==='select')[3].props.onChange({target:{value:mode}});p.inputs()[2].props.onChange({target:{value:'  '}});await p.submit();assert.equal(p.writes().length,0);assert.match(p.message(),/meeting link is required/);
});
