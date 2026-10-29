'use strict';
// Actual TSX handlers with controlled React hooks/API/session marker. No browser,
// real inference or SQL claim. Tests deliberately withhold replacement events.
const a=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm'),{test}=require('node:test');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const compile=file=>ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const contract={exports:{}};vm.runInNewContext(compile('apps/web/src/lib/penta-chat-contract.ts'),{module:contract,exports:contract.exports,Intl});
const component=compile('apps/web/src/components/penta/penta-chat-workspace.tsx');
const academy='11111111-1111-4111-8111-111111111111',conversation='22222222-2222-4222-8222-222222222222';
const tick=()=>new Promise(resolve=>setImmediate(resolve));
function harness(options={}){
 let loginEpoch='synthetic-initial-login',token='synthetic-token',index=0,initial=true,tree;
 const states=[],refs=[],effects=[],calls=[],window=new EventTarget(),document=Object.assign(new EventTarget(),{visibilityState:'visible'});
 window.localStorage={getItem:key=>key.includes('sessionEpoch')?loginEpoch:token};
 const api={portalSessionEpoch:()=>loginEpoch,portalAccessToken:()=>token,portalSignOutEvent:'logout',portalSessionChangedEvent:'replacement',isPortalSignOutEvent:()=>false,
  async academyApi(path,init={}){
   calls.push({path,method:init.method||'GET'});const n=calls.length;
   let value=path==='/api/auth/session'?{academyId:academy,roles:['AcademyAdmin'],isPlatformOwner:false}:
    path.endsWith('/academy-context')?{academyId:academy,name:'PRIVATE-OLD-ACADEMY',timeZone:'Asia/Kolkata'}:
    path.endsWith('/chat/health')?{status:'Available',provider:'PENTA Mini',protocol:'0.1',readOnly:true}:
    path.endsWith('/conversations')?{conversationId:conversation,version:0,expiresAtUtc:'2099-10-08T10:00:00Z'}:
    {conversationId:conversation,requestId:JSON.parse(init.body).requestId,version:1,kind:'TEXT_RESPONSE',message:'PRIVATE-OLD-REPLY',capability:'executor',provider:'PENTA Mini',protocol:'0.1',context:{filters:{},sort_by:null,current_learner_id:null,current_result_ids:[]},result:null};
   return{ok:true,status:200,json:async()=>{if(options.replaceAt===n)loginEpoch='synthetic-replacement';return value;}};
  }};
 const react={useState(value){const n=index++;if(!(n in states))states[n]=value;return[states[n],next=>states[n]=typeof next==='function'?next(states[n]):next];},
  useRef(value){const n=index++;return refs[n]??= {current:value};},useCallback:fn=>fn,
  useEffect(fn){if(initial)effects.push(fn);}};
 const mod={exports:{}};
 new Function('require','module','exports','window','document','crypto',component)(name=>name==='react/jsx-runtime'?jsx:name==='react'?react:name==='next/link'?{default:'a'}:
   name==='@/lib/api'?api:name==='@/lib/penta-chat-contract'?contract.exports:name.endsWith('.css')?new Proxy({},{get:(_,key)=>String(key)}):{default:()=>null},mod,mod.exports,window,document,require('node:crypto').webcrypto);
 const render=()=>{index=0;tree=mod.exports.default({active:true});return tree;};
 render();initial=false;const cleanups=effects.map(fn=>fn());
 const nodes=value=>value==null?[]:Array.isArray(value)?value.flatMap(nodes):typeof value==='object'?[value,...nodes(value.props?.children)]:[value];
 return{calls,window,document,states,async ready(){await tick();render();},replace(){loginEpoch='synthetic-replacement';},renew(){token='synthetic-renewed';},
  render,privateContext:()=>states.some(value=>value?.name==='PRIVATE-OLD-ACADEMY'),reply:()=>states.some(value=>Array.isArray(value)&&value.some(turn=>turn.receipt?.message==='PRIVATE-OLD-REPLY')),
  async send(){let list=nodes(render());const input=list.find(node=>node?.type==='textarea');a.ok(input);input.props.onChange({target:{value:'synthetic read'}});list=nodes(render());const form=list.find(node=>node?.type==='form');a.ok(form);await form.props.onSubmit({preventDefault(){}});await tick();render();},
  event(name,detail){const event=new Event(name);event.detail=detail;window.dispatchEvent(event);render();},dispose(){cleanups.forEach(fn=>fn?.());}};
}
for(const at of[1,2,3])test('read bootstrap withholds replaced login at stage '+at,async()=>{
 const h=harness({replaceAt:at});await h.ready();a.equal(h.calls.length,at);a.equal(h.privateContext(),false);h.dispose();
});
test('old ready read chat refuses direct submit after missed identical-token replacement',async()=>{
 const h=harness();await h.ready();a.equal(h.privateContext(),true);h.replace();await h.send();a.equal(h.calls.length,3);a.equal(h.reply(),false);a.equal(h.privateContext(),false);h.dispose();
});
for(const at of[4,5])test('read request withholds replacement at request '+at+' without another dispatch',async()=>{
 const h=harness({replaceAt:at});await h.ready();await h.send();a.equal(h.calls.length,at);a.equal(h.reply(),false);a.equal(h.privateContext(),false);h.dispose();
});
for(const event of['focus','pageshow','visibilitychange'])test('read chat clears missed replacement on '+event,async()=>{
 const h=harness();await h.ready();h.replace();if(event==='visibilitychange')h.document.dispatchEvent(new Event(event));else h.event(event);
 h.render();a.equal(h.privateContext(),false);a.equal(h.calls.length,3);h.dispose();
});
test('same-document replacement clears private read state',async()=>{const h=harness();await h.ready();h.replace();h.event('replacement','AcademyAdmin');a.equal(h.privateContext(),false);a.equal(h.calls.length,3);h.dispose();});
test('other workspace replacement does not clear read context',async()=>{const h=harness();await h.ready();h.event('replacement','Teacher');a.equal(h.privateContext(),true);a.equal(h.calls.length,3);h.dispose();});
test('normal same-login token renewal preserves read flow',async()=>{const h=harness();await h.ready();h.renew();h.event('focus');await h.send();a.equal(h.reply(),true);a.equal(h.calls.length,5);h.dispose();});
test('disposed lifecycle listeners cannot change another mounted view',async()=>{const h=harness();await h.ready();h.dispose();h.replace();h.event('focus');a.equal(h.privateContext(),true);});
