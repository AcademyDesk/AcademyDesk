// Actual batch TSX; retained controlled-hook harness with route injection only.
const {test}=require('node:test'),a=require('node:assert/strict'),fs=require('node:fs');
const base=fs.readFileSync(__dirname+'/frontend-page-runtime.cjs','utf8');
function runtime(route){a.ok(base.includes("usePathname: () => '/batches'"));const mod={exports:{}};new Function('require','module','exports','__dirname',base.replace("usePathname: () => '/batches'","usePathname: () => "+JSON.stringify(route)))(require,mod,mod.exports,__dirname);return mod.exports;}
const labels={courses:[{id:'course',name:'Piano',academyType:'Music'}],teachers:[{id:'teacher',firstName:'Synthetic',lastName:'Teacher'}],branches:[{id:'branch',name:'Central'}]};
const row={id:'batch',name:'Saved batch',courseId:'course',teacherId:'teacher',branchId:'branch',capacity:10,isActive:true,activeEnrolments:2,deliveryMode:'InPerson',startDate:null};
async function fixture(config={},route='/batch-setup'){
 const {mount,nodes,text,response,deferred}=runtime(route);let failure=config.status||0,value=config.value===undefined?structuredClone(labels):config.value;const calls=[];
 const api=async(url,init={})=>{calls.push({url,method:init.method||'GET',...init});
  if(init.method&&init.method!=='GET')return response({...row,...JSON.parse(init.body)});
  if(url==='/api/academies')return response([{id:'a',name:'Synthetic academy'},{id:'b',name:'Second academy'}]);
  if(config.gate)await config.gate.promise;
  if(config.aGate&&url.includes('/a/'))await config.aGate.promise;
  if(url.endsWith('/batches/options')){if(failure)return {...response({},false),status:failure};return response(value);}
  if(url.endsWith('/batches'))return response(config.badRows?{}:config.rows??[row]);
  throw Error('Related management endpoint forbidden '+url);
 };
 const h=mount('apps/web/src/app/batches/page.tsx',api);await h.flush();const all=()=>nodes(h.render()),button=label=>all().find(n=>n.type==='button'&&text(n).trim()===label),control=name=>all().find(n=>n.props.name===name);
 return {h,all,button,control,calls,text:()=>text(h.render()),recover(next=labels){failure=0;value=structuredClone(next);},async submit(){await all().find(n=>n.type==='form').props.onSubmit({preventDefault(){}});await h.flush();},async reload(){await button('Reload batch records').props.onClick();await h.flush();},deferred};
}
for(const route of ['/batches','/batch-setup'])test(route+' reads scoped options and register only',async()=>{const f=await fixture({},route);try{a.ok(f.calls.some(x=>x.url.endsWith('/batches/options')));a.ok(f.calls.every(x=>!['/courses','/teachers','/branches'].some(t=>x.url.endsWith(t))));a.match(f.text(),/Saved batch/);}finally{f.h.unmount();}});
for(const route of ['/batches','/batch-setup'])for(const status of [401,403,503])test(route+' failed lookup '+status+' does not fabricate empty or zero data',async()=>{const f=await fixture({status},route);try{a.match(f.text(),/Batch information could not be loaded/);a.doesNotMatch(f.text(),/No batches yet|No active classes or batches|0 records/);a.equal(f.all().filter(n=>n.props.className==='batch-overview-kpis').length,0);if(route==='/batch-setup'){a.equal(f.button('Create batch').props.disabled,true);await f.submit();a.equal(f.calls.filter(x=>x.method!=='GET').length,0);}f.recover();await f.reload();a.match(f.text(),/Saved batch/);}finally{f.h.unmount();}});
for(const value of [null,{}, {courses:[],teachers:[]},{courses:{},teachers:[],branches:[]},{...labels,courses:[{id:'course',name:'Piano'}]},{...labels,teachers:[{id:'teacher'}]},{...labels,branches:[{id:'branch'}]},{...labels,courses:[labels.courses[0],labels.courses[0]]}])test('malformed options fail closed '+JSON.stringify(value),async()=>{const f=await fixture({value});try{a.equal(f.button('Create batch').props.disabled,true);await f.submit();a.equal(f.calls.filter(x=>x.method!=='GET').length,0);}finally{f.h.unmount();}});
test('malformed batch register cannot enable changes',async()=>{const f=await fixture({badRows:true});try{a.equal(f.button('Create batch').props.disabled,true);await f.submit();a.equal(f.calls.filter(x=>x.method!=='GET').length,0);}finally{f.h.unmount();}});
test('successful empty data is distinguished from failure',async()=>{const f=await fixture({rows:[],value:{courses:[],teachers:[],branches:[]}});try{a.match(f.text(),/No batches yet/);a.doesNotMatch(f.text(),/could not be loaded/);a.equal(f.button('Create batch').props.disabled,true);}finally{f.h.unmount();}});
test('refresh retains the entered batch draft',async()=>{const f=await fixture();try{const input=f.all().find(n=>n.props.placeholder?.startsWith('Batch name'));input.props.onChange({target:{value:'Retained draft'}});await f.h.flush();await f.reload();a.equal(f.all().find(n=>n.props.placeholder?.startsWith('Batch name')).props.value,'Retained draft');}finally{f.h.unmount();}});
test('late read cannot update an unmounted page',async()=>{const {deferred}=runtime('/batch-setup'),gate=deferred(),f=await fixture({gate});f.h.unmount();gate.resolve();await f.h.flush();a.equal(f.h.lateWrites,0);});
test('old academy response cannot replace a newer selection',async()=>{const {deferred}=runtime('/batch-setup'),gate=deferred(),f=await fixture({aGate:gate});try{f.control('academy').props.onChange('b');await f.h.flush();gate.resolve();await f.h.flush();a.equal(f.control('academy').props.value,'b');a.equal(f.button('Create batch').props.disabled,false);a.ok(f.calls.some(x=>x.url==='/api/academies/b/batches/options'));}finally{gate.resolve();f.h.unmount();}});
