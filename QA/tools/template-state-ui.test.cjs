const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=fs.readFileSync('apps/web/src/app/communications/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX,target:ts.ScriptTarget.ES2022}}).outputText;
const nodes=n=>Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];
const person='40000000-0000-0000-0000-000000000001';
const template=(channel,status,isActive=true)=>({id:'50000000-0000-0000-0000-000000000001',channel,status,isActive,name:'Synthetic title',body:'Hello {{name}}'});
async function page(fixture,serverReject=false){let index=0,effectIndex=0;const state=new Map(),deps=[],effects=[],calls=[],module={exports:{}};function StandardSelectField(){}function StandardDateField(){}function StandardTimeField(){}
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}},useMemo:fn=>fn()};
 const api=async(url,init={})=>{calls.push({url,...init});if(init.method==='POST')return{ok:!serverReject,json:async()=>serverReject?{message:'The selected template is unavailable.'}:{status:'BlockedConsent'}};const entity=url.split('/').at(-1);return{ok:true,json:async()=>structuredClone(entity==='academies'?[{id:'owned'}]:entity==='communication-templates'?[fixture]:entity==='notifications'?[]:[{id:person,firstName:'Synthetic',lastName:entity}])};};
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/workspace-nav'?{WorkspaceNav(){}}:n==='@/components/design-system/controls'?{StandardSelectField,StandardDateField,StandardTimeField}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=effectIndex=0;return module.exports.default();};const flush=async()=>{for(let i=0;i<5;i++){for(const fn of effects.splice(0))fn();await new Promise(r=>setImmediate(r));render();}};render();await flush();
 const field=name=>nodes(render()).find(n=>n.type===StandardSelectField&&n.props.name===name);
 const select=(name,value)=>field(name).props.onChange(value);
 const edit=(label,value)=>{const field=nodes(render()).find(n=>n.type==='label'&&Array.isArray(n.props.children)&&n.props.children[0]?.trim?.()===label);nodes(field).find(n=>['input','textarea'].includes(n.type)).props.onChange({target:{value}});};
 const save=async()=>{await nodes(render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){}});await flush();};
 return{state,field,select,edit,save,writes:()=>calls.filter(x=>x.method==='POST')};
}
for(const channel of ['Email','WhatsApp'])for(const status of ['Draft','Approved','Disabled','dIsAbLeD',' Disabled '])for(const active of [true,false])test(`${channel} ${status} active=${active}: options, forged selection and submit policy`,async()=>{
 const t=template(channel,status,active),p=await page(t),allowed=active&&status.trim().toLowerCase()!=='disabled';
 assert.equal(p.field('templateId').props.options.some(x=>x.value===t.id),allowed);
 p.select('recipientId',person);p.select('templateId',t.id);
 assert.equal(p.state.get(8),allowed?t.id:'');
 if(allowed){p.edit('name','Synthetic');await p.save();const body=JSON.parse(p.writes()[0].body);assert.equal(body.templateId,t.id);assert.equal(body.channel,channel);assert.equal(body.title,t.name);assert.deepEqual(body.variables,{name:'Synthetic'});}
 else{p.state.set(8,t.id);p.state.set(12,channel);p.state.set(10,t.name);p.state.set(11,t.body);await p.save();assert.equal(p.writes().length,0);assert.match(p.state.get(19),/unavailable/i);}
});
for(const channel of ['Email','WhatsApp'])for(const change of ['Disabled','Inactive'])test(`${channel} ${change} committed in loaded state blocks a stale selection`,async()=>{
 const t=template(channel,'Approved'),p=await page(t);p.select('recipientId',person);p.select('templateId',t.id);p.state.set(4,[{...t,...(change==='Disabled'?{status:'Disabled'}:{isActive:false})}]);await p.save();assert.equal(p.writes().length,0);assert.match(p.state.get(19),/unavailable/i);assert.equal(p.state.get(8),t.id);
});
for(const channel of ['Email','WhatsApp'])test(`${channel} server disable after load retains draft and displays rejection`,async()=>{
 const t=template(channel,'Approved'),p=await page(t,true);p.select('recipientId',person);p.select('templateId',t.id);p.edit('name','Synthetic');await p.save();assert.equal(p.writes().length,1);assert.equal(p.state.get(8),t.id);assert.equal(p.state.get(10),t.name);assert.equal(p.state.get(11),t.body);assert.deepEqual(p.state.get(9),{name:'Synthetic'});assert.equal(p.state.get(19),'The selected template is unavailable.');
});
