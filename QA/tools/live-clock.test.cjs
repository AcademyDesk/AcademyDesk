// Actual external-store hook: cached snapshots, shared delivery and cleanup.
const fs=require('node:fs'),assert=require('node:assert/strict'),{test}=require('node:test');
const ts=require('../../apps/web/node_modules/typescript');
function fixture(){let now=100,api,samples=0;const events=new Map(),docEvents=new Map(),timers=new Map(),module={exports:{}};
 const eventApi=map=>({addEventListener:(key,fn)=>map.set(key,fn),removeEventListener:(key,fn)=>{assert.equal(map.get(key),fn);map.delete(key);}});
 const doc={visibilityState:'visible',...eventApi(docEvents)};
 const win={...eventApi(events),setInterval:(fn,ms)=>{assert.equal(ms,60000);timers.set(1,fn);return 1;},clearInterval:key=>timers.delete(key)};
 const code=ts.transpileModule(fs.readFileSync('apps/web/src/lib/live-clock.ts','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
 new Function('require','module','exports','window','document','Date',code)(()=>({useSyncExternalStore:(subscribe,get,server)=>{api={subscribe,get,server};return get();}}),module,module.exports,win,doc,{now:()=>{samples++;return now;}});
 return{read:()=>module.exports.useLiveClock(),api:()=>api,set:value=>now=value,events,docEvents,timers,doc,samples:()=>samples};
}
test('server and first snapshot are stable zero with no render clock read',()=>{const f=fixture();assert.equal(f.read(),0);assert.equal(f.api().server(),0);assert.equal(f.read(),0);assert.equal(f.samples(),0);});
test('subscribe samples client once and repeated snapshots do not resample',()=>{const f=fixture();f.read();let calls=0;const stop=f.api().subscribe(()=>calls++);assert.equal(f.api().get(),100);assert.equal(calls,1);assert.equal(f.read(),100);assert.equal(f.samples(),1);stop();});
test('focus/timer/visible updates notify only a changed timestamp',()=>{const f=fixture();f.read();let calls=0;const stop=f.api().subscribe(()=>calls++);f.events.get('focus')();assert.equal(calls,1);f.set(200);f.doc.visibilityState='hidden';f.timers.get(1)();assert.equal(f.api().get(),100);f.doc.visibilityState='visible';f.docEvents.get('visibilitychange')();assert.equal(f.api().get(),200);assert.equal(calls,2);assert.equal(f.api().server(),0);stop();});
test('all subscribers receive changes, shared timer stops only after last unsubscribe',()=>{const f=fixture();f.read();let first=0,second=0;const stopFirst=f.api().subscribe(()=>first++),stopSecond=f.api().subscribe(()=>second++);assert.equal(f.timers.size,1);f.set(300);f.timers.get(1)();assert.equal(first,2);assert.equal(second,1);stopFirst();assert.equal(f.timers.size,1);f.set(400);f.events.get('focus')();assert.equal(first,2);assert.equal(second,2);stopSecond();assert.equal(f.timers.size,0);assert.equal(f.events.size,0);assert.equal(f.docEvents.size,0);});
