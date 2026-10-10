// Actual onboarding handlers, synthetic FormData/transport; not SQL/device proof.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
async function fixture(kind,dates,status=200){
 const code=ts.transpileModule(fs.readFileSync('apps/web/src/app/'+kind+'-onboarding/page.tsx','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
 const state=new Map(),calls=[],pushes=[],module={exports:{}};let index=0,resets=0;
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useEffect:()=>{}};
 const api=async(url,init)=>{calls.push({url,...init});return{ok:status===200,status,json:async()=>({id:'synthetic-created',message:'Synthetic rejection'})};};
 class FakeFormData extends Map{constructor(){super(Object.entries(kind==='teacher'?{firstName:'Synthetic',lastName:'Teacher',email:'qa@example.invalid',employmentType:'Full-time',phone:'',...dates}:{studentFirstName:'Synthetic',studentLastName:'Learner',dateOfBirth:'2000-01-01',studentNumber:'',...dates}));}}
 new Function('require','module','exports','FormData',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='next/navigation'?{useRouter:()=>({push:url=>pushes.push(url)})}:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n.startsWith('@/components/')?{StandardDateField:'date',StandardSelectField:'select'}:(()=>{throw Error(n)})(),module,module.exports,FakeFormData);
 const render=()=>{index=0;return module.exports.default();};render();state.set(0,{id:'owned'});if(kind==='teacher')state.set(2,[{subject:' Piano ',certification:''}]);
 const action=async()=>{const event={preventDefault(){},currentTarget:{reset(){resets++;}}};const pending=nodes(render()).find(n=>n.type==='form').props.onSubmit(event);event.currentTarget=null;await pending;};
 return{action,calls,pushes,state,resets:()=>resets,render};
}
for(const kind of['teacher','student'])for(const value of['','2026-10-01','malformed'])test(kind+' optional date '+JSON.stringify(value)+' preserves contract',async()=>{
 const dates=kind==='teacher'?{dateOfBirth:value,joiningDate:value}:{admissionDate:value};const p=await fixture(kind,dates,value==='malformed'?400:200);await p.action();
 assert.equal(p.calls.length,1);const body=JSON.parse(p.calls[0].body);
 for(const key of Object.keys(dates))assert.equal(body[key],value||null);
 if(kind==='student'){assert.equal(body.dateOfBirth,'2000-01-01');assert.equal(body.allowParentPortalAccess,false);assert.equal(body.allowFinance,false);}else{assert.equal(body.specialties,'Piano');assert.equal(body.certificationsJson,'[{"subject":"Piano","certification":null}]');assert.equal(body.availabilityJson,'[]');}
 assert.equal(p.resets(),value==='malformed'?0:1);
 if(kind==='student')assert.deepEqual(p.pushes,value==='malformed'?[]:['/student-management?studentId=synthetic-created&notice=student-created']);
 else assert.match(p.state.get(5),value==='malformed'?/Synthetic rejection/:/Teacher onboarded/);
});
for(const kind of['teacher','student'])test(kind+' no academy prevents POST',async()=>{const p=await fixture(kind,{});p.state.set(0,undefined);await p.action();assert.equal(p.calls.length,0);assert.equal(p.resets(),0);});
test('Teacher subject requirement retained',async()=>{const p=await fixture('teacher',{});p.state.set(2,[{subject:'',certification:''}]);await p.action();assert.equal(p.calls.length,0);assert.match(p.state.get(5),/at least one subject/);});
