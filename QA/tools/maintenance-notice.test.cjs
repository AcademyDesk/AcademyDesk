// Actual transport and notice code, controlled browser/storage/React hooks. No SQL or model.
const assert = require('node:assert/strict'), fs = require('node:fs'), vm = require('node:vm');
const { test } = require('node:test');
const ts = require('../../apps/web/node_modules/typescript');
const jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const source = file => ts.transpileModule(fs.readFileSync(file, 'utf8'), {compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022,jsx:ts.JsxEmit.ReactJSX}}).outputText;
class CustomEvent extends Event { constructor(name, opts={}) { super(name); this.detail=opts.detail; } }
const deferred=()=>{let resolve;return {promise:new Promise(r=>resolve=r),resolve:v=>resolve(v)};};
const tick=async()=>{for(let i=0;i<8;i++)await new Promise(r=>setImmediate(r));};
const maintenance=()=>new Response(JSON.stringify({code:'platform_maintenance',maintenanceMode:true,message:'Maintenance'}),{status:503});
function harness(fetcher=()=>maintenance(), workspace='AcademyAdmin') {
 const storage=new Map(), window=new EventTarget(), calls=[],events=[];
 Object.assign(window,{location:{pathname:({AcademyAdmin:'/academic-governance',Teacher:'/teacher',Portal:'/portal',Platform:'/platform/control'})[workspace]},localStorage:{getItem:k=>storage.get(k)??null,setItem:(k,v)=>storage.set(k,v),removeItem:k=>storage.delete(k)}});
 const mod={exports:{}}; const context={module:mod,exports:mod.exports,process:{env:{}},window,CustomEvent,Event,Headers,Response,AbortController,TextDecoder,setTimeout,clearTimeout,crypto,fetch:async(url,init)=>{calls.push({url,init});return fetcher(url,init);}};
 vm.runInNewContext(source('apps/web/src/lib/api.ts'),context); const api=mod.exports;
 api.beginPortalSession(workspace,'own','refresh');
 window.addEventListener(api.portalMaintenanceEvent,e=>events.push(e.detail));
 return {api,window,storage,calls,events,workspace};
}
for(const workspace of ['AcademyAdmin','Teacher','Portal','Platform'])test('structured503 signals only current '+workspace+' without consuming/replaying response',async()=>{
 const original=maintenance(),h=harness(()=>original,workspace);assert.equal(await h.api.academyApi('/api/business',{method:'POST',body:'draft'}),original);await tick();assert.equal(await original.text(),JSON.stringify({code:'platform_maintenance',maintenanceMode:true,message:'Maintenance'}));assert.equal(h.calls.length,1);assert.equal(h.events.length,1);assert.deepEqual(Object.keys(h.events[0]).sort(),['epoch','serial','workspace']);assert.equal(h.events[0].workspace,workspace);assert.ok(h.api.readPortalMaintenanceSignal(workspace));
});
for(const value of [{},{code:'other',maintenanceMode:true,message:'x'},{code:'platform_maintenance',maintenanceMode:false,message:'x'},{code:'platform_maintenance',maintenanceMode:true,message:''},'broken', 'x'.repeat(9000)])test('generic or malformed503 is not maintenance: '+JSON.stringify(value).slice(0,70),async()=>{
 const h=harness(()=>new Response(typeof value==='string'?value:JSON.stringify(value),{status:503}));await h.api.academyApi('/api/business');await tick();assert.equal(h.events.length,0);assert.equal(h.api.readPortalMaintenanceSignal(h.workspace),null);assert.equal(h.calls.length,1);
});
for(const change of ['foreign','logout','replacement','abort','navigation'])test('ignores stale/foreign503 '+change,async()=>{
 const gate=deferred(),h=harness(()=>gate.promise),controller=new AbortController();const pending=h.api.academyApi('/api/business',{signal:controller.signal,...(change==='foreign'?{headers:{Authorization:'Bearer foreign'}}:{})});
 if(change==='logout')h.api.clearPortalTokens();if(change==='replacement')h.api.beginPortalSession(h.workspace,'own','refresh');if(change==='abort')controller.abort();if(change==='navigation')h.window.location.pathname='/teacher';gate.resolve(maintenance());await pending;await tick();assert.equal(h.events.length,0);assert.equal(h.api.readPortalMaintenanceSignal(h.workspace),null);
});
test('availability itself cannot trigger a recursive hint',async()=>{const h=harness();await h.api.academyApi('/api/auth/session/availability');await tick();assert.equal(h.events.length,0);});
test('acknowledging an older signal cannot remove the newer one',async()=>{const h=harness();await h.api.academyApi('/api/business');await tick();const first=h.api.readPortalMaintenanceSignal(h.workspace);await h.api.academyApi('/api/business');await tick();const next=h.api.readPortalMaintenanceSignal(h.workspace);assert.ok(next.serial>first.serial);h.api.acknowledgePortalMaintenanceSignal(h.workspace,first.serial);assert.equal(h.api.readPortalMaintenanceSignal(h.workspace).serial,next.serial);h.api.acknowledgePortalMaintenanceSignal(h.workspace,next.serial);assert.equal(h.api.readPortalMaintenanceSignal(h.workspace),null);});
test('renewal replay503 signals renewed own session with no further refresh',async()=>{let i=0;const h=harness(()=>++i===1?new Response('',{status:401}):i===2?Response.json({accessToken:'renewed',refreshToken:'new-refresh'}):maintenance());const r=await h.api.academyApi('/api/business');await tick();assert.equal(r.status,503);assert.equal(h.calls.length,3);assert.equal(h.events.length,1);assert.equal(h.storage.get('academydesk.accessToken.AcademyAdmin'),'renewed');});
test('a hanging clone never blocks or signals after its bounded timeout',async()=>{let cancel=0;const response={status:503,clone:()=>({body:new ReadableStream({cancel(){cancel++;}})})};const h=harness(()=>response);assert.equal(await h.api.academyApi('/api/business'),response);await new Promise(r=>setTimeout(r,1600));assert.equal(h.events.length,0);assert.equal(cancel,1);});

// Isolated controlled hooks for the actual notice; sibling mounting is additionally
// verified in Chromium against the real WorkspaceFrame and entered form values.
function mountNotice(h) {
 let state=[],refs=[],effects=[],pending=[],index=0,ri=0,ei=0,disposed=false,late=0;const timers=[],timeouts=new Map();let timeoutId=0;
 const react={useState(initial){const k=index++;if(!(k in state))state[k]=initial;return [state[k],v=>{if(disposed)late++;state[k]=typeof v==='function'?v(state[k]):v;}];},useRef(initial){return refs[ri++]??={current:initial};},useEffect(fn,deps){const k=ei++,old=effects[k];if(!old||deps.some((x,i)=>x!==old.deps[i])){effects[k]={fn,deps,cleanup:old?.cleanup};pending.push(k);}}};
 const mod={exports:{}};vm.runInNewContext(source('apps/web/src/components/service-availability-notice.tsx'),{module:mod,exports:mod.exports,require:name=>name==='react'?react:name==='react/jsx-runtime'?jsx:name==='@/lib/api'?h.api:(()=>{throw Error(name)})(),window:h.window,AbortController,setTimeout:fn=>{const id=++timeoutId;timeouts.set(id,fn);return id;},clearTimeout:id=>timeouts.delete(id),setInterval:fn=>(timers.push(fn),timers.length),clearInterval:()=>{}});
 const render=()=>{index=ri=ei=0;const tree=mod.exports.ServiceAvailabilityNotice({workspace:h.workspace});for(const k of pending){effects[k].cleanup?.();effects[k].cleanup=effects[k].fn();}pending=[];return tree;};
 render();return {render,async flush(){await tick();return render();},timers,expire(){for(const fn of [...timeouts.values()])fn();},get late(){return late;},unmount(){disposed=true;effects.forEach(e=>e.cleanup?.());}};
}
const nodes=x=>!x||typeof x!=='object'?[]:Array.isArray(x)?x.flatMap(nodes):[x,...nodes(x.props?.children)];
const text=x=>x==null||typeof x==='boolean'?'':Array.isArray(x)?x.map(text).join(''):typeof x==='object'?text(x.props?.children):String(x);
const click=(view,label)=>nodes(view.render()).find(x=>x.type==='button'&&text(x)===label).props.onClick();
const status=(maintenanceMode=true,restricted=true)=>Response.json({maintenanceMode,restricted,message:maintenanceMode?'Saved <script>plain text</script>':null});
test('no signal or anonymous view produces no speculative availability request',async()=>{const h=harness(),view=mountNotice(h);await view.flush();h.window.dispatchEvent(new Event('focus'));view.timers.forEach(f=>f());await view.flush();assert.equal(view.render(),null);assert.equal(h.calls.length,0);view.unmount();});
for(const owner of [false,true])test('verified notice and GET-only recovery keep clear deliberate-submit guidance, owner='+owner,async()=>{
 let off=false;const h=harness(url=>url.endsWith('/availability')?status(!off,off?false:!owner):maintenance()),view=mountNotice(h);await h.api.academyApi('/api/business',{method:'POST',body:'draft'});await view.flush();assert.match(text(view.render()),owner?/Maintenance is enabled/:/Maintenance in progress/);assert.match(text(view.render()),/Saved <script>plain text<\/script>/);assert.equal(nodes(view.render()).some(n=>n.props?.dangerouslySetInnerHTML),false);assert.equal(nodes(view.render()).some(n=>n.props?.href==='/platform/control?tab=Settings'),owner);
 off=true;click(view,'Check availability');await view.flush();assert.match(text(view.render()),/Service is available again/);assert.match(text(view.render()),/Review saved records before submitting again/);assert.equal(h.calls.filter(x=>x.init.method==='POST').length,1);assert.ok(h.calls.filter(x=>x.url.endsWith('/availability')).every(x=>x.init.method==='GET'&&x.init.cache==='no-store'));view.unmount();
});
test('newer signal discards old mode-off status and coalesces one fresh read',async()=>{
 const gate=deferred();let reads=0;const h=harness(url=>url.endsWith('/availability')?(++reads===1?gate.promise:status()):maintenance()),view=mountNotice(h);await h.api.academyApi('/api/business');await view.flush();await h.api.academyApi('/api/business');await view.flush();assert.equal(reads,1);gate.resolve(status(false,false));await view.flush();assert.equal(reads,2);assert.doesNotMatch(text(view.render()),/Service is available again/);assert.match(text(view.render()),/Maintenance in progress/);view.unmount();
});
for(const bad of [()=>new Response('',{status:503}),()=>new Response('',{status:401}),()=>Response.json({maintenanceMode:false,restricted:true}),()=>Response.json({})])test('failed/malformed status cannot announce recovery '+bad,async()=>{const h=harness(url=>url.endsWith('/availability')?bad():maintenance()),view=mountNotice(h);await h.api.academyApi('/api/business');await view.flush();assert.match(text(view.render()),/could not be verified/);assert.doesNotMatch(text(view.render()),/available again/);assert.equal(h.api.portalAccessToken(h.workspace),'own');view.unmount();});
for(const change of ['unmount','logout','replacement','legacy-change'])test('pending availability cannot leak across '+change,async()=>{
 const gate=deferred(),h=harness(url=>url.endsWith('/availability')?gate.promise:maintenance());if(change==='legacy-change')h.storage.delete('academydesk.sessionEpoch.'+h.workspace);const view=mountNotice(h);await h.api.academyApi('/api/business');await view.flush();if(change==='unmount')view.unmount();if(change==='logout')h.api.clearPortalTokens();if(change==='replacement')h.api.beginPortalSession(h.workspace,'new','next');if(change==='legacy-change')h.storage.set('academydesk.accessToken.'+h.workspace,'changed');gate.resolve(status());await tick();if(change!=='unmount'){await view.flush();assert.equal(view.render(),null);view.unmount();}assert.equal(view.late,0);
});
test('availability timeout releases a stuck read, allows fresh check and discards late mode-off',async()=>{
 const gate=deferred();let reads=0;const h=harness(url=>url.endsWith('/availability')?(++reads===1?gate.promise:status()):maintenance()),view=mountNotice(h);await h.api.academyApi('/api/business');await view.flush();view.expire();await view.flush();assert.match(text(view.render()),/could not be verified/);assert.equal(h.calls[1].init.signal.aborted,true);click(view,'Check availability');await view.flush();assert.match(text(view.render()),/Maintenance in progress/);gate.resolve(status(false,false));await view.flush();assert.doesNotMatch(text(view.render()),/available again/);assert.equal(reads,2);view.unmount();
});
test('a failed check keeps the last verified maintenance message and no false recovery',async()=>{
 let fail=false;const h=harness(url=>url.endsWith('/availability')?(fail?new Response('',{status:503}):status()):maintenance()),view=mountNotice(h);await h.api.academyApi('/api/business');await view.flush();fail=true;click(view,'Check availability');await view.flush();assert.match(text(view.render()),/could not be verified/);assert.match(text(view.render()),/Saved <script>plain text<\/script>/);assert.doesNotMatch(text(view.render()),/available again/);view.unmount();
});
test('different workspace events do not start status reads or clear own verified warning',async()=>{
 const h=harness(url=>url.endsWith('/availability')?status():maintenance()),view=mountNotice(h);await h.api.academyApi('/api/business');await view.flush();const count=h.calls.length;h.window.dispatchEvent(new CustomEvent(h.api.portalMaintenanceEvent,{detail:{workspace:'Teacher',epoch:'different',serial:99}}));h.window.dispatchEvent(new CustomEvent(h.api.portalSignOutEvent,{detail:'Teacher'}));await view.flush();assert.match(text(view.render()),/Maintenance in progress/);assert.equal(h.calls.length,count);view.unmount();
});
test('focus and interval coalesce while active and stop checking after confirmed recovery',async()=>{
 const gate=deferred();let off=false,first=true;const h=harness(url=>url.endsWith('/availability')?(first?(first=false,gate.promise):status(!off,!off)):maintenance()),view=mountNotice(h);await h.api.academyApi('/api/business');await view.flush();h.window.dispatchEvent(new Event('focus'));view.timers.forEach(f=>f());await view.flush();assert.equal(h.calls.length,2);gate.resolve(status());await view.flush();off=true;h.window.dispatchEvent(new Event('focus'));await view.flush();assert.match(text(view.render()),/available again/);const count=h.calls.length;view.timers.forEach(f=>f());h.window.dispatchEvent(new Event('focus'));await view.flush();assert.equal(h.calls.length,count);view.unmount();
});
