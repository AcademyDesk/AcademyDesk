// Actual ResultRow handlers with controlled hooks; browser rendering is a separate gate.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const code=ts.transpileModule(fs.readFileSync('apps/web/src/app/assessments/page.tsx','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText+'\nmodule.exports.ResultRow = ResultRow;';
function row(result,saving=false,locked=false){let index=0;const state=new Map(),calls=[],module={exports:{}};
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?{useState:v=>{const key=index++;return[state.has(key)?state.get(key):v,v=>state.set(key,v)];},useEffect:()=>{}}:n==='@/components/design-system/controls'?{StandardSelectField:'mode'}:n==='@/components/workspace-nav'?{WorkspaceNav:'nav'}:n==='@/lib/api'?{}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=0;return module.exports.ResultRow({student:{id:'s',firstName:'QA',lastName:'Learner'},result,maxScore:100,saving,locked,onSave:async(...args)=>calls.push(args)});};
 const nodes=n=>Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];
 const find=label=>nodes(render()).find(n=>n.props?.['aria-label']===label),mode=()=>nodes(render()).find(n=>n.type==='mode'),save=()=>nodes(render()).find(n=>n.type==='button');return{find,mode,save,calls};}
test('Automatic result score edit retains automatic mode and sends explicit mode',()=>{const r=row({score:80,grade:'Pass',isGradeManual:false});r.find('Assessment score').props.onChange({target:{value:'20'}});assert.equal(r.mode().props.value,'automatic');assert.equal(r.find('Manual grade').props.disabled,true);r.save().props.onClick();assert.equal(r.calls[0][1],20);assert.equal(r.calls[0][4],false);});
test('Manual override survives score-only edit',()=>{const r=row({score:80,grade:'Merit',isGradeManual:true});r.find('Assessment score').props.onChange({target:{value:'0'}});assert.equal(r.mode().props.value,'manual');assert.equal(r.save().props.disabled,false);r.save().props.onClick();assert.equal(r.calls[0][2],'Merit');assert.equal(r.calls[0][4],true);});
test('Legacy grade is preserved until explicit automatic selection',()=>{const r=row({score:80,grade:'Pass'});assert.equal(r.mode().props.value,'manual');r.mode().props.onChange('automatic');r.save().props.onClick();assert.equal(r.calls[0][4],false);});
test('New result defaults to automatic and zero score is valid',()=>{const r=row();assert.equal(r.mode().props.value,'automatic');r.find('Assessment score').props.onChange({target:{value:'0'}});assert.equal(r.save().props.disabled,false);});
for(const value of ['', '-1','101','NaN'])test(`Invalid score ${value} blocks save`,()=>{const r=row();r.find('Assessment score').props.onChange({target:{value}});assert.equal(r.save().props.disabled,true);});
test('Manual empty grade blocks save',()=>{const r=row({score:50});r.mode().props.onChange('manual');assert.equal(r.save().props.disabled,true);});
test('Saving disables mode and grade controls',()=>{const r=row({score:80,grade:'Merit',isGradeManual:true},true);assert.equal(r.mode().props.disabled,true);assert.equal(r.find('Manual grade').props.disabled,true);assert.equal(r.save().props.disabled,true);});

const { mount, nodes: pageNodes, text: pageText, response: pageResponse } = require('./frontend-page-runtime.cjs');
async function page({manual=false,failure,refreshFailure=false}={}) {
 const result={studentId:'s',score:80,grade:'Pass',isGradeManual:manual,isPublished:true},calls=[];let wrote=false;
 const h=mount('apps/web/src/app/assessments/page.tsx',async(url,options={})=>{
  calls.push({url,...options});
  if(options.method){wrote=true;if(failure==='network')throw Error('Synthetic network');return pageResponse({message:'Synthetic save rejected.'},!failure);}
  if(url==='/api/academies')return pageResponse([{id:'a'}]);
  if(url.endsWith('/options'))return pageResponse({batches:[{id:'b',name:'QA batch'}],students:[{id:'s',firstName:'QA',lastName:'Learner'}],enrollments:[{studentId:'s',batchId:'b',status:'Active'}],gradingSchemes:[]});
  if(url.endsWith('/assessments'))return pageResponse([{id:'x',batchId:'b',title:'QA',maxScore:100}]);
  return pageResponse([result],!wrote||!refreshFailure);
 });
 await h.flush();const row=()=>pageNodes(h.render()).find(n=>typeof n.type==='function'&&n.type.name==='ResultRow');assert.ok(row());
 return{h,calls,save:async()=>{await row().props.onSave('s',20,'Pass','Note',manual);await h.flush();},notice:()=>pageText(pageNodes(h.render()).find(n=>n.props?.role==='status'&&n.props?.className?.includes('assessments-message'))),saving:()=>Boolean(row()?.props.saving)};
}
for(const manual of [false,true])test(`Page save ${manual?'manual':'automatic'} sends explicit mode and confirmed notice`,async()=>{const p=await page({manual});await p.save();const writes=p.calls.filter(x=>x.method);assert.equal(writes.length,1);const body=JSON.parse(writes[0].body);assert.equal(body.isGradeManual,manual);assert.equal(body.grade,manual?'Pass':null);assert.equal(body.score,20);assert.equal(p.notice(),'Assessment result saved.');assert.equal(p.saving(),false);p.h.unmount();});
test('Confirmed save with failed refresh keeps success notice',async()=>{const p=await page({refreshFailure:true});await p.save();assert.match(p.notice(),/^Assessment result saved, but/);assert.equal(p.saving(),false);p.h.unmount();});
test('Rejected save shows server guidance and retains entries',async()=>{const p=await page({failure:400});await p.save();assert.equal(p.notice(),'Synthetic save rejected.');assert.equal(p.calls.filter(x=>x.method).length,1);assert.equal(p.calls.filter(x=>!x.method&&x.url.endsWith("/results")).length,1);assert.equal(p.saving(),false);p.h.unmount();});
test('Unconfirmed network save retains entries and releases pending state',async()=>{const p=await page({failure:'network'});await p.save();assert.match(p.notice(),/could not be confirmed/);assert.equal(p.saving(),false);p.h.unmount();});

test('Another pending learner save locks this row without claiming it is being saved',()=>{const r=row({score:80,grade:'Merit',isGradeManual:true},false,true);assert.equal(r.find('Assessment score').props.disabled,true);assert.equal(r.mode().props.disabled,true);assert.equal(r.save().props.disabled,true);assert.equal(r.save().props.children,'Save');});
