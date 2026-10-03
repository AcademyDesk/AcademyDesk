// Actual channel-page TSX handlers with controlled hooks/API. Not browser/React/device evidence.
const fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const code=ts.transpileModule(fs.readFileSync('apps/web/src/app/communication-settings/page.tsx','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const channels=['Email','WhatsApp','Meeting'],fields=['provider','status','senderName','senderAddress','replyToAddress','phoneNumber','externalAccountReference','messagesEnabled'];
const base=channels.map(channel=>({channel,provider:channel==='WhatsApp'?'MetaCloudApi':'GoogleWorkspace',status:'Configured',senderName:channel+' sender',senderAddress:channel.toLowerCase()+'@example.invalid',replyToAddress:'reply-'+channel.toLowerCase()+'@example.invalid',phoneNumber:'+910000000001',externalAccountReference:'synthetic-'+channel,messagesEnabled:true,hasSecureConnection:true}));
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
const clean=x=>typeof x==='string'?(x.trim()||null):x;
const expected=body=>Object.fromEntries(fields.map(key=>[key,key==='provider'?body[key].trim():key==='status'?body[key]:key==='messagesEnabled'?body[key]&&body.status==='Configured':clean(body[key])]));
async function page(config={}){
 let index=0,effectIndex=0,refIndex=0,getCount=0;const state=new Map(),refs=new Map(),deps=[],effects=[],calls=[],module={exports:{}};let rows=structuredClone(Object.hasOwn(config,'rows')?config.rows:base);
 const react={useRef:v=>{const k=refIndex++;if(!refs.has(k))refs.set(k,{current:v});return refs.get(k);},useState:v=>{const k=index++;if(!state.has(k))state.set(k,typeof v==='function'?v():v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 function MeetingProviderSettings(){}
 const api=async(url,init={})=>{
  calls.push({url,...init});if(url==='/api/academies')return{ok:true,status:200,json:async()=>[{id:'owned'}]};
  if(init.method==='PUT'){
   const channel=url.split('/').at(-1),body=JSON.parse(init.body),old=rows.find(x=>x.channel===channel);
   const stored={channel,...expected(body),hasSecureConnection:old?.hasSecureConnection??false};
   const commit=()=>{rows=rows.filter(x=>x.channel!==channel).concat(stored);return{ok:true,status:200,json:async()=>structuredClone(stored)};};
   if(config.onPut)return config.onPut({channel,body,stored,commit});
   commit();
   return{ok:true,status:200,json:async()=>structuredClone(stored)};
  }
  getCount++;if(config.onGet)await config.onGet(getCount);
  if(config.defer)await config.defer;
  return{ok:!config.getStatus,status:config.getStatus??200,json:async()=>structuredClone(rows)};
 };
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/workspace-nav'?{WorkspaceNav(){}}:n==='@/components/meeting-provider-settings'?{MeetingProviderSettings}:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=effectIndex=refIndex=0;return module.exports.default();};
 const flush=async()=>{for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await new Promise(r=>setImmediate(r));render();}};
 render();await flush();
 const form=channel=>nodes(render()).find(n=>n.type==='form'&&text(n).includes(channel+' sender'));
 const meeting=()=>nodes(render()).find(n=>n.type===MeetingProviderSettings);
 const edit=(channel,key,value)=>{
  if(channel==='Meeting'){const p=meeting().props;const mapping={senderName:'organizerName',senderAddress:'organizerEmail',externalAccountReference:'reference'};p.onChange({...p.value,[mapping[key]??key]:value});}
  else if(key==='provider')nodes(form(channel)).find(n=>n.type==='select').props.onChange({target:{value}});
  else {const placeholder=key==='senderName'?'Display name':channel==='Email'?'Sender email':'Dedicated WhatsApp number';nodes(form(channel)).find(n=>n.type==='input'&&n.props.placeholder===placeholder).props.onChange({target:{value}});}
 };
 const save=async channel=>{if(channel==='Meeting')meeting().props.onSave();else form(channel).props.onSubmit({preventDefault(){}});await new Promise(r=>setImmediate(r));await new Promise(r=>setImmediate(r));render();};
 const draft=channel=>state.get(channel==='Email'?2:channel==='WhatsApp'?3:4);
 const notice=channel=>nodes(render()).filter(n=>n.props?.['data-channel-notice']===channel).map(text).join('');
 const disabled=channel=>channel==='Meeting'?meeting().props.disabled:nodes(form(channel)).find(n=>n.type==='button').props.disabled;
 return{state,calls,render,flush,form,meeting,edit,save,draft,notice,disabled,rows:()=>rows,writes:()=>calls.filter(x=>x.method==='PUT')};
}
module.exports={channels,fields,base,nodes,text,expected,page};
