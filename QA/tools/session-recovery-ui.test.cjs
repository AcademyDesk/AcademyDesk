// Actual dashboard effect/render, controlled responses; not native HTTP/browser proof.
const fs=require('node:fs'),a=require('node:assert/strict'),{test}=require('node:test');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=process.env.QA_DASHBOARD_SOURCE||'apps/web/src/app/dashboard/page.tsx';
const output=ts.transpileModule(fs.readFileSync(source,'utf8'),{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
async function run(config={}){
  const values=[],calls=[],clears=[];let index=0,effect,cleanup,release;
  const response=(status,body)=>new Response(JSON.stringify(body),{status,headers:{'Content-Type':'application/json'}});
  const api=async path=>{
    calls.push(path);
    if(config.pause&&calls.length===1)await new Promise(r=>{release=r;});
    if(config.network)throw Error('Synthetic network interruption');
    if(path==='/api/academies')return response(config.academies||200,[{id:'qa-a',name:'Synthetic Academy'}]);
    if(path.endsWith('/dashboard'))return response(config.dashboard||200,{activeStudents:2,activeTeachers:1,activeCourses:1,activeBatches:1,openLeads:0,attendanceRecordsLast30Days:0,presentAttendanceLast30Days:0,outstandingBalance:0});
    if(path==='/api/auth/session')return response(config.session||200,{displayName:'Synthetic Admin',roles:['AcademyAdmin'],academyId:'qa-a'});
    return response(config.optional||200,[]);
  };
  const mod={exports:{}};
  new Function('require','module','exports',output)(name=>name==='react/jsx-runtime'?jsx:name==='react'?{
    useState:init=>{const key=index++;if(!(key in values))values[key]=init;return [values[key],v=>{values[key]=typeof v==='function'?v(values[key]):v;}];},
    useEffect:fn=>{effect=fn;}
  }:name==='@/lib/api'?{academyApi:api,clearPortalTokens:w=>clears.push(w)}:name==='next/link'?{default:'a'}:{StandardInteractiveTile:'tile',StandardDetailModal:'modal'},mod,mod.exports);
  index=0;const initial=mod.exports.default();cleanup=effect();
  if(config.pause){if(typeof cleanup==='function')cleanup();release();}
  for(let i=0;i<20;i++)await new Promise(r=>setImmediate(r));
  index=0;const rendered=mod.exports.default();
  const nodes=x=>!x||typeof x!=='object'?[]:Array.isArray(x)?x.flatMap(nodes):[x,...nodes(x.props?.children)];
  const all=nodes(rendered),text=all.map(x=>typeof x.props?.children==='string'?x.props.children:'').join('|');
  return {values,calls,clears,all,text,initial};
}
for(const [status,state,re] of [[401,'signin',/session has ended/],[403,'denied',/do not have access/],[429,'retry',/Too many requests/],[500,'retry',/Check your connection/]])test('primary status '+status+' gives truthful gated recovery',async()=>{
 const r=await run({academies:status});a.match(r.text,re);a.equal(r.values[5],state);a.ok(r.all.some(x=>x.props?.role==='alert'));a.ok(!r.all.some(x=>x.props?.['aria-label']==='Academy operating indicators'));a.doesNotMatch(r.text,/5092/);a.deepEqual(r.clears,status===401?['AcademyAdmin']:[]);a.equal(r.calls.length,1);
});
for(const [stage,status] of [['dashboard',401],['dashboard',403],['session',401],['session',403],['session',429]])test(stage+' '+status+' does not render misleading ready data',async()=>{
 const r=await run({[stage]:status});a.equal(r.values[5],status===401?'signin':status===403?'denied':'retry');a.ok(!r.all.some(x=>x.props?.['aria-label']==='Academy operating indicators'));a.deepEqual(r.clears,status===401?['AcademyAdmin']:[]);
});
test('network failure has manual retry without deleting credentials',async()=>{const r=await run({network:true});a.match(r.text,/Check your connection/);a.deepEqual(r.clears,[]);a.ok(r.all.some(x=>x.type==='button'&&x.props.children==='Try again'));});
test('active native-shaped controls retain ready indicators',async()=>{const r=await run();a.ok(r.all.some(x=>x.props?.['aria-label']==='Academy operating indicators'));a.deepEqual(r.clears,[]);});
test('optional module403 is not mislabeled account deactivation',async()=>{const r=await run({optional:403});a.ok(r.all.some(x=>x.props?.['aria-label']==='Academy operating indicators'));a.deepEqual(r.clears,[]);});
test('unmounted initial effect ignores late responses and never clears session',async()=>{const r=await run({academies:401,pause:true});a.equal(r.values[5],'loading');a.deepEqual(r.clears,[]);a.equal(r.calls.length,1);});
