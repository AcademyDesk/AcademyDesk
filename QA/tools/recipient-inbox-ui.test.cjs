// Actual PortalNotifications TSX with controlled hooks/API; not a live browser.
const fs=require('node:fs'),assert=require('node:assert/strict'),{test}=require('node:test');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=fs.readFileSync('apps/web/src/app/portal/page.tsx','utf8');
const component=source.slice(source.indexOf('function PortalNotifications('),source.indexOf('function View('));
const code=ts.transpileModule('export '+component,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX,target:ts.ScriptTarget.ES2022}}).outputText;
function nodes(x){return !x||typeof x!=='object'?[]:Array.isArray(x)?x.flatMap(nodes):[x,...nodes(x.props?.children)];}
const text=x=>x==null?'':Array.isArray(x)?x.map(text).join(''):typeof x==='object'?text(x.props?.children):String(x);
const fixture=(id,status='Queued',isRead=false)=>({id,title:'Synthetic '+id,message:'Synthetic body',status,isRead,readAtUtc:null});
const deferred=()=>{let resolve;const promise=new Promise(r=>resolve=r);return{promise,resolve};};
function mount(notices,onRead){let stateIndex=0,refIndex=0;const states=[],refs=[],calls=[],mod={exports:{}};const react={useState:v=>{const i=stateIndex++;if(!(i in states))states[i]=v;return[states[i],v=>states[i]=typeof v==='function'?v(states[i]):v];},useRef:v=>{const i=refIndex++;return refs[i]??=( {current:v});},useEffect(){}};
 const api=async url=>{const id=url.split('/').at(-2);calls.push(id);return onRead?onRead(id):{ok:true,json:async()=>({id,isRead:true,status:'Queued',readAtUtc:'2026-10-01T10:00:00Z'})};};
 new Function('require','module','exports','useState','useRef','useEffect','academyApi',code)(n=>n==='react/jsx-runtime'?jsx:(()=>{throw Error(n);})(),mod,mod.exports,react.useState,react.useRef,react.useEffect,api);
 const render=()=>{stateIndex=refIndex=0;return mod.exports.PortalNotifications({notices,setNotices:f=>notices=typeof f==='function'?f(notices):f});};
 const click=()=>nodes(render()).find(n=>n.type==='button').props.onClick();
 const settle=async()=>{for(let i=0;i<5;i++)await new Promise(r=>setImmediate(r));render();};
 return{render,click,settle,calls,notices:()=>notices,replace:n=>notices=n};
}
for(const status of ['Queued','Sent','Read'])test('ack preserves delivery '+status,async()=>{const h=mount([fixture('one',status,status==='Read')]);h.click();await h.settle();assert.equal(h.notices()[0].status,status);assert.equal(h.notices()[0].isRead,true);assert.equal(h.calls.length,status==='Read'?0:1);});
for(const outcome of ['http','network','malformed','wrong-id','false-read'])test('unconfirmed '+outcome+' retained with retry feedback',async()=>{const h=mount([fixture('one')],async()=>{if(outcome==='network')throw Error('offline');return{ok:outcome!=='http',json:async()=>outcome==='malformed'?null:{id:outcome==='wrong-id'?'foreign':'one',isRead:outcome!=='false-read',readAtUtc:'2026-10-01T10:00:00Z'}};});h.click();await h.settle();assert.equal(h.notices()[0].isRead,false);assert.equal(h.notices()[0].status,'Queued');assert.match(text(h.render()),/could not.*read/i);});
test('only eight displayed rows acknowledged',async()=>{const h=mount(Array.from({length:11},(_,i)=>fixture(String(i))));h.click();await h.settle();assert.deepEqual(h.calls,Array.from({length:8},(_,i)=>String(i)));assert.equal(h.notices().filter(n=>!n.isRead).length,3);});
test('close does not send read requests',async()=>{let fail=true;const h=mount([fixture('one')],async()=>({ok:!fail,json:async()=>({id:'one',isRead:true,readAtUtc:'2026-10-01T10:00:00Z'})}));h.click();await h.settle();h.click();await h.settle();assert.equal(h.calls.length,1);fail=false;h.click();await h.settle();assert.equal(h.calls.length,2);assert.equal(h.notices()[0].isRead,true);});
test('partial success changes only confirmed row',async()=>{const h=mount([fixture('one'),fixture('two','Sent')],async id=>({ok:id==='one',json:async()=>({id,isRead:true,readAtUtc:'2026-10-01T10:00:00Z'})}));h.click();await h.settle();assert.deepEqual(h.notices().map(n=>n.isRead),[true,false]);assert.deepEqual(h.notices().map(n=>n.status),['Queued','Sent']);});
test('in-flight duplicate clicks produce one request',async()=>{const d=deferred(),h=mount([fixture('one')],()=>d.promise);h.click();h.click();assert.equal(h.calls.length,1);d.resolve({ok:true,json:async()=>({id:'one',isRead:true,readAtUtc:'2026-10-01T10:00:00Z'})});await h.settle();assert.equal(h.notices()[0].isRead,true);});
test('newer rows and delivery status are not overwritten by acknowledgment',async()=>{const d=deferred(),h=mount([fixture('one')],()=>d.promise);h.click();h.replace([fixture('one','Sent'),fixture('new')]);d.resolve({ok:true,json:async()=>({id:'one',isRead:true,status:'Queued',readAtUtc:'2026-10-01T10:00:00Z'})});await h.settle();assert.deepEqual(h.notices().map(n=>n.status),['Sent','Queued']);assert.deepEqual(h.notices().map(n=>n.isRead),[true,false]);});
test('already acknowledged delivery row has no unread badge',()=>{const h=mount([fixture('one','Queued',true)]);assert.equal(nodes(h.render()).filter(n=>n.type==='em').length,0);});
