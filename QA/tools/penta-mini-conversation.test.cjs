// Actual client state machine, synthetic responses/storage; no model/SQL acceptance.
const { test } = require('node:test'), a = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript');
function load(name, imports = {}) {
 const module = { exports: {} }, source = fs.readFileSync('apps/web/src/lib/' + name + '.ts', 'utf8');
 new Function('require','module','exports',ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText)(n=>{if(!(n in imports))throw Error('Unexpected import '+n);return imports[n];},module,module.exports);
 return module.exports;
}
const channel = load('penta-onboarding-channel',{'./api':{}}), contract = load('penta-chat-contract');
const lib = load('penta-mini-conversation',{'./penta-onboarding-channel':channel,'./penta-chat-contract':contract});
const id = n => String(n).padStart(8,'0')+'-1111-4111-8111-111111111111', academy=id(1), conversation=id(2), learner=id(3);
const expiry='2030-01-01T00:00:00Z', epoch='synthetic-epoch';
const clone=x=>JSON.parse(JSON.stringify(x));
function fixture(options={}) {
 const map=options.map||new Map(), requests=[], receipts=new Map(); let state, current=true, next=10, version=0, plans=0;
 const storage={getItem:k=>map.get(k)||null,setItem:(k,v)=>{if(options.storageFail&&k===lib.miniPointerKey)throw Error('blocked');map.set(k,v);},removeItem:k=>map.delete(k)};
 const scope={academyId:academy,epoch,current:()=>current,invalidate(){current=false;},signal:new AbortController().signal,
  async send(path,method,body){
   requests.push({path,method,body});let code=200,value;
   if(path==='/health')value=options.health||{status:'Available',provider:'PENTA Mini',protocol:'0.1',readOnly:true};
   else if(path==='/conversations'){a.equal(method,'POST');code=201;value={conversationId:conversation,version:0,expiresAtUtc:expiry};}
   else if(path.endsWith('/turns')){
    a.equal(method,'POST');plans++;version++;
    value={conversationId:conversation,requestId:body.requestId,version,kind:'RESULT',message:'Verified learner read.',capability:'executor',provider:'PENTA Mini',protocol:'0.1',
     context:{filters:{},sort_by:null,current_learner_id:null,current_result_ids:[learner]},
     result:{count:1,hasMore:false,rows:[{sourceId:learner,displayName:'Synthetic learner',recordCode:'SYN-1',subjects:['Piano'],balances:[{currency:'USD',outstanding:450.01}],sourcePath:'/student-management?studentId='+learner}],asOfUtc:'2026-11-18T09:00:00Z',source:'Invoices',balanceScope:'All outstanding fees'}};
    receipts.set(body.requestId,clone(value));code=201;
    if(options.lost)throw Error('lost reply');
    if(options.patch)value=options.patch(clone(value));
    if(options.hold)await options.hold;
   }else {a.equal(method,'GET');value=receipts.get(path.split('/').at(-1))||options.recovered;code=options.recoveryStatus||200;}
   return Response.json(value??{}, {status:code,headers:{'cache-control':options.cached?'public':'no-store'}});
  }};
 const driver=()=>lib.createMiniConversation(scope,()=>storage,v=>{state=v;},()=>id(next++),()=>Date.parse('2026-11-18T09:00:00Z'));
 const mini=driver();
 return {mini,driver,scope,storage,map,requests,state:()=>state,plans:()=>plans,receipts,setCurrent:v=>{current=v;},async ready(){await mini.initialise();await mini.start();}};
}
test('no POST occurs before explicit start and send',async()=>{const f=fixture();await f.mini.initialise();a.equal(f.state().phase,'ready');a.deepEqual(f.requests.map(x=>x.method),['GET']);await f.mini.start();a.equal(f.state().session.version,0);a.equal(f.plans(),0);await f.mini.send('Find learner Mira');a.equal(f.state().receipt.kind,'RESULT');a.equal(f.plans(),1);a.deepEqual(Object.keys(f.requests.at(-1).body).sort(),['capability','expectedVersion','requestId','text']);});
test('recovery stores only six scoped fields and no prompt, token or receipt',async()=>{const f=fixture();await f.ready();await f.mini.send('PRIVATE SYNTHETIC PROMPT');const raw=f.map.get(lib.miniPointerKey);a.deepEqual(Object.keys(JSON.parse(raw)).sort(),['academyId','conversationId','epoch','expectedVersion','expiresAtUtc','requestId']);a.doesNotMatch(raw,/PRIVATE|Synthetic learner|accessToken|450/);});
test('lost response uses GET-only recovery and never redispatches the prompt',async()=>{const f=fixture({lost:true});await f.ready();await f.mini.send('Find Mira');a.equal(f.state().phase,'unknown');a.equal(f.state().receipt,null);await f.mini.send('Repeat');a.equal(f.plans(),1);const before=f.requests.length;await f.mini.recover();a.equal(f.state().receipt.kind,'RESULT');a.ok(f.requests.slice(before).every(x=>x.method==='GET'));a.equal(f.plans(),1);});
test('reload with saved IDs recovers without a new conversation or planner call',async()=>{const f=fixture({lost:true});await f.ready();await f.mini.send('Find Mira');f.mini.dispose();const before=f.requests.length;await f.driver().initialise();a.equal(f.state().receipt.kind,'RESULT');a.ok(f.requests.slice(before).every(x=>x.method==='GET'));a.equal(f.plans(),1);});
test('stored response recovers even when inference is unavailable',async()=>{const o={lost:true},f=fixture(o);await f.ready();await f.mini.send('Find Mira');o.health={status:'Unavailable',provider:'PENTA Mini',protocol:'0.1',readOnly:true};f.mini.dispose();await f.driver().initialise();a.equal(f.state().available,false);a.equal(f.state().receipt.kind,'RESULT');a.equal(f.plans(),1);});
test('storage failure prevents sending a turn',async()=>{const o={},f=fixture(o);await f.ready();o.storageFail=true;a.equal(await f.mini.send('Find Mira'),false);a.equal(f.plans(),0);a.match(f.state().notice,/No Mini prompt was sent/);});
for(const patch of [v=>({...v,epoch:'other'}),v=>({...v,academyId:id(99)}),v=>({...v,requestId:'bad'}),v=>({...v,privatePrompt:'private'}),v=>({...v,expectedVersion:-1}),v=>({...v,expiresAtUtc:'2026-02-30T00:00:00Z'})])test('invalid or foreign pointer is discarded '+patch.toString(),()=>{const f=fixture(),p={academyId:academy,epoch,conversationId:conversation,requestId:id(10),expectedVersion:0,expiresAtUtc:expiry};f.map.set(lib.miniPointerKey,JSON.stringify(patch(p)));a.equal(lib.readMiniPointer(f.storage,academy,epoch),null);a.equal(f.map.has(lib.miniPointerKey),false);});
const corrupt=[
 ['foreign session',v=>({...v,conversationId:id(99)})],['foreign request',v=>({...v,requestId:id(99)})],['stale version',v=>({...v,version:9})],
 ['wrong provider',v=>({...v,provider:'Other'})],['wrong capability',v=>({...v,capability:'autopilot'})],['wrong protocol',v=>({...v,protocol:'9'})],
 ['extra envelope',v=>({...v,privateContact:'private'})],['extra context',v=>({...v,context:{...v.context,actor:'private'}})],
 ['extra result',v=>({...v,result:{...v.result,privateContact:'private'}})],['extra row',v=>({...v,result:{...v.result,rows:[{...v.result.rows[0],privateContact:'private'}]}})],
 ['extra balance',v=>({...v,result:{...v.result,rows:[{...v.result.rows[0],balances:[{currency:'USD',outstanding:1,privateContact:'private'}]}]}})],
 ['unsafe source',v=>({...v,result:{...v.result,rows:[{...v.result.rows[0],sourcePath:'/platform'}]}})],
 ['row mismatch',v=>({...v,context:{...v.context,current_result_ids:[id(99)]}})],['invented count',v=>({...v,result:{...v.result,count:2}})],
 ['refusal carrying rows',v=>({...v,kind:'REFUSAL'})],['missing result',v=>({...v,result:null})],['duplicate row IDs',v=>({...v,result:{...v.result,rows:[v.result.rows[0],v.result.rows[0]],count:2}})]
];
for(const [name,patch]of corrupt)test('unverified '+name+' is hidden, original receipt recovers',async()=>{const f=fixture({patch});await f.ready();await f.mini.send('Find Mira');a.equal(f.state().receipt,null);a.equal(f.state().phase,'unknown');await f.mini.recover();a.equal(f.state().receipt.kind,'RESULT');a.equal(f.plans(),1);});
for(const status of ['Degraded','Unavailable'])test(status+' cannot start a model conversation',async()=>{const f=fixture({health:{status,provider:'PENTA Mini',protocol:'0.1',readOnly:true}});await f.mini.initialise();await f.mini.start();a.equal(f.requests.filter(x=>x.method==='POST').length,0);});
for(const code of [404,409,410,503])test('recovery '+code+' never replays a POST',async()=>{const f=fixture({lost:true,recoveryStatus:code});await f.ready();await f.mini.send('Find Mira');const before=f.requests.length;await f.mini.recover();a.equal(f.state().receipt,null);a.ok(f.requests.slice(before).every(x=>x.method==='GET'));a.equal(f.plans(),1);});
test('late response after session change cannot be accepted',async()=>{let release;const o={hold:new Promise(r=>release=r)},f=fixture(o);await f.ready();const pending=f.mini.send('Find Mira');while(!f.plans())await new Promise(r=>setTimeout(r,1));f.setCurrent(false);release();await pending;a.equal(f.state().receipt,null);});
test('competing pointer is not overwritten by a new request',async()=>{const f=fixture();await f.ready();const raw=JSON.stringify({epoch,academyId:academy,conversationId:id(90),requestId:id(91),expectedVersion:0,expiresAtUtc:expiry});f.map.set(lib.miniPointerKey,raw);await f.mini.send('Find Mira');a.equal(f.plans(),0);a.equal(f.map.get(lib.miniPointerKey),raw);});
test('duplicate submissions share a single pending turn',async()=>{let release;const f=fixture({hold:new Promise(r=>release=r)});await f.ready();const pending=f.mini.send('Find Mira');await f.mini.send('Other prompt');release();await pending;a.equal(f.plans(),1);});
