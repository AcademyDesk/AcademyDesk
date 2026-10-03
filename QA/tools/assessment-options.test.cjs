// Actual TSX initialization/create handlers with controlled effects/API; not browser rendering evidence.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=fs.readFileSync('apps/web/src/app/assessments/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const options={batches:[{id:'b',name:'Synthetic batch'}],students:[{id:'s',firstName:'Synthetic',lastName:'Learner'}],enrollments:[{studentId:'s',batchId:'b',status:'Active'}],gradingSchemes:[{id:'g',name:'Synthetic scheme',passingPercent:50}]};
const assessments=[{id:'x',batchId:'b',title:'Synthetic assessment',maxScore:100,isPublished:true}];
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
async function page(config={}) {
 let index=0,effectIndex=0;const state=new Map(),deps=[],pending=[],calls=[],module={exports:{}};
 const react={useState:v=>{const key=index++;if(!state.has(key))state.set(key,v);return[state.get(key),v=>state.set(key,typeof v==='function'?v(state.get(key)):v)];},useEffect:(fn,next)=>{const key=effectIndex++;if(!deps[key]||next.some((v,i)=>v!==deps[key][i])){deps[key]=next;pending.push(fn);}}};
 const api=async(url,init={})=>{
  calls.push({url,...init});
  if(config.network&&url.endsWith('/options'))throw Error('Synthetic network outage');
  const status=url==='/api/academies'?(config.academyStatus??200):url.endsWith('/options')?(config.optionsStatus??200):url.endsWith('/assessments')?(config.assessmentStatus??200):200;
  return{ok:status<400,status,json:async()=>{
   if(config.invalidJson&&url.endsWith('/options'))throw new SyntaxError('Synthetic invalid JSON');
   if(url==='/api/academies')return config.noAcademy?[]:[{id:'a'}];
   if(url.endsWith('/options'))return Object.hasOwn(config,'options')?config.options:options;
   if(url.endsWith('/assessments'))return init.method?{id:'new'}:(config.empty?[]:assessments);
   if(url.endsWith('/results'))return [];
   throw Error('Unexpected endpoint '+url);
  }};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/components/design-system/controls'?{StandardSelectField:'select',StandardDateField:'date',StandardTimeField:'time'}:n==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=0;effectIndex=0;return module.exports.default();};
 async function settle(){for(let i=0;i<4;i++){for(const fn of pending.splice(0))fn();await new Promise(r=>setImmediate(r));render();}}
 render();await settle();return{state,calls,render,settle};
}
test('Academic-only loading uses options and never the four management dependencies',async()=>{
 const p=await page();assert.ok(p.calls.some(x=>x.url==='/api/academies/a/assessments/options'));
 for(const path of ['batches','students','enrollments','grading-schemes/active'])assert.ok(!p.calls.some(x=>x.url==='/api/academies/a/'+path));
 assert.deepEqual(p.state.get(1),options.batches);assert.deepEqual(p.state.get(2),options.students);assert.deepEqual(p.state.get(3),options.enrollments);assert.deepEqual(p.state.get(5),options.gradingSchemes);assert.equal(p.state.get(15),'');
});
test('Actual selectors show names, scheme threshold and assessment batch label',async()=>{
 const p=await page(),selectors=nodes(p.render()).filter(n=>n.type==='select');
 assert.equal(selectors.find(n=>n.props.name==='batch').props.options[0].label,'Synthetic batch');
 assert.equal(selectors.find(n=>n.props.name==='grading-scheme').props.options[0].label,'Synthetic scheme · pass 50%');
 assert.match(selectors.find(n=>n.props.name==='assessment').props.options[0].label,/Synthetic batch/);
});
test('Active student can reach the actual result editor from the options roster',async()=>{
 const p=await page(),row=nodes(p.render()).find(n=>typeof n.type==='function'&&n.type.name==='ResultRow');assert.ok(row);assert.equal(row.props.student.id,'s');assert.equal(row.props.maxScore,100);
});
test('Empty successful options are not mislabeled as denied or populated with old data',async()=>{
 const p=await page({empty:true,options:{batches:[],students:[],enrollments:[],gradingSchemes:[]}});assert.equal(p.state.get(15),'');
 assert.equal(p.state.get(8),'');assert.equal(nodes(p.render()).find(n=>n.type==='button'&&text(n)==='Create assessment').props.disabled,true);
});
for(const key of ['optionsStatus','assessmentStatus'])test(`${key}403 shows academic access guidance without partial options`,async()=>{
 const p=await page({[key]:403});assert.match(p.state.get(15),/academic access and an enabled academic module/);assert.deepEqual(p.state.get(1),[]);assert.deepEqual(p.state.get(2),[]);assert.deepEqual(p.state.get(4),[]);
});
test('Server options failure gives a visible recoverable message',async()=>{const p=await page({optionsStatus:500});assert.match(p.state.get(15),/could not be loaded/);assert.deepEqual(p.state.get(1),[]);});
test('Network rejection is not treated as an empty successful load',async()=>{const p=await page({network:true});assert.equal(p.state.get(15),'Synthetic network outage');assert.deepEqual(p.state.get(1),[]);});
for(const value of [null,{...options,students:null}])test(`Malformed options ${value===null?'null':'array'} do not partially update selectors`,async()=>{const p=await page({options:value});assert.match(p.state.get(15),/could not be loaded completely/);assert.deepEqual(p.state.get(1),[]);assert.deepEqual(p.state.get(4),[]);});
test('Invalid options JSON keeps loading failure visible',async()=>{const p=await page({invalidJson:true});assert.equal(p.state.get(15),'Synthetic invalid JSON');assert.deepEqual(p.state.get(1),[]);});
test('Academy discovery rejection retains fallback error guidance',async()=>{const p=await page({academyStatus:403});assert.match(p.state.get(15),/could not be loaded/);assert.ok(!p.calls.some(x=>x.url.endsWith('/options')));});
test('No academy keeps existing create-academy guidance',async()=>{const p=await page({noAcademy:true});assert.match(p.state.get(15),/Create your academy/);assert.ok(!p.calls.some(x=>x.url.endsWith('/options')));});
test('Create handler uses lookup IDs and refreshes only academic prerequisites',async()=>{
 const p=await page();nodes(p.render()).find(n=>n.type==='input'&&n.props.placeholder==='Assessment title').props.onChange({target:{value:'From options'}});
 nodes(p.render()).find(n=>n.type==='select'&&n.props.name==='grading-scheme').props.onChange('g');
 await nodes(p.render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){}});
 const writes=p.calls.filter(x=>x.method==='POST');assert.equal(writes.length,1);const body=JSON.parse(writes[0].body);assert.equal(body.batchId,'b');assert.equal(body.gradingSchemeId,'g');assert.equal(body.title,'From options');
 assert.equal(p.calls.filter(x=>x.url.endsWith('/options')).length,2);assert.equal(p.state.get(9),'');assert.equal(p.state.get(6),'x');
 for(const path of ['batches','students','enrollments','grading-schemes/active'])assert.ok(!p.calls.some(x=>x.url==='/api/academies/a/'+path));
});
