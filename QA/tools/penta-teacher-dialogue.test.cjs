'use strict';
// Controlled HTTP responses, real TS module and bounded JSON reader. Not SQL,
// native browser, model interpretation, visible UI or production acceptance.
const test=require('node:test'),a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),crypto=require('node:crypto');
const ts=require('../../apps/web/node_modules/typescript'),root=path.resolve(__dirname,'../..');
const session='11111111-1111-4111-8111-111111111111',draft='22222222-2222-4222-8222-222222222222',academy='33333333-3333-4333-8333-333333333333';
const approval='44444444-4444-4444-8444-444444444444',teacher='55555555-5555-4555-8555-555555555555';
const receipt='66666666-6666-4666-8666-666666666666',audit='77777777-7777-4777-8777-777777777777';
function fixture(){const canonicalPayload=JSON.stringify({tool_key:'AcademyDesk.Teacher.Create',tool_revision:1,fields:{FirstName:{kind:'text',value:'Synthetic'},LastName:{kind:'text',value:'Teacher'}}});
 return{id:draft,sessionId:session,contextVersion:1,revision:1,status:'AwaitingReview',expiresAtUtc:'2099-01-01T00:00:00Z',canonicalPayload,
 payloadHash:crypto.createHash('sha256').update(canonicalPayload).digest('hex'),approvalId:null,approvalExpiresAtUtc:null,receipt:null};}
function harness(options={}){
 let token='synthetic-bearer',epoch='first-login';const abort=new AbortController(),calls=[],modules=new Map();let state=fixture();
 const window=Object.assign(new EventTarget(),{location:{pathname:'/penta/teachers/chat',search:options.search??`?conversationId=${session}&draftId=${draft}`,hash:''}});
 window.history={state:{},replaceState(s,t,url){if(options.historyFailure)throw Error('history failure');if(!options.historyNoop){const next=new URL(url,'http://127.0.0.1');window.location.pathname=next.pathname;window.location.search=next.search;}}};
 const api={apiUrl:'http://127.0.0.1:1',portalAccessToken:()=>token,portalSessionEpoch:()=>epoch,portalWorkspaceForPath:p=>p.startsWith('/penta')?'AcademyAdmin':'Teacher',
  portalSessionChangedEvent:'session-change',portalSignOutEvent:'sign-out',isPortalSignOutEvent:e=>e.key==='sign-out'};
 const account={academyId:academy,roles:['AcademyAdmin'],isPlatformOwner:false,workspace:'AcademyAdmin'};
 function load(file){file=path.resolve(file);if(modules.has(file))return modules.get(file).exports;const mod={exports:{}};modules.set(file,mod);
 const js=ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
 vm.runInNewContext(js,{module:mod,exports:mod.exports,require(name){if(name==='./api')return api;const target=path.resolve(path.dirname(file),name+'.ts');a.ok(target.startsWith(path.join(root,'apps/web/src')+path.sep));return load(target);},
 window,Date,URLSearchParams,structuredClone,crypto:crypto.webcrypto,TextEncoder,TextDecoder,Uint8Array,AbortController,fetch:()=>{throw Error('Uncontrolled fetch');}});return mod.exports;}
 const lib=load(path.join(root,'apps/web/src/lib/penta-teacher-dialogue.ts'));let captured=options.savedRequestId??null,lastSummary=null;
 async function request(url,init){const route=new URL(url).pathname,n=calls.length+1;calls.push({route,init});
  if(options.before)await options.before(n,{window,abort,replaceToken:()=>token='replacement',replaceEpoch:()=>epoch='replacement-login'});
  let value;
  if(route.endsWith('/session'))value={...account};
  else if(route.endsWith('/readiness'))value={version:'teacher-start/1',academyId:academy,available:!options.offline,canStart:!options.offline,canExecute:false,syntheticInterpreter:!options.offline};
  else if(route.endsWith('/conversation/turns')||route.endsWith('/result')){
   if(init.method==='POST'){
    const body=JSON.parse(init.body);captured=body.requestId;
    a.equal(new URLSearchParams(window.location.search).get('teacherRequestId'),captured);
    state.revision++;state.status=options.prepare?'Reviewed':'AwaitingReview';
    if(options.prepare){state.approvalId=approval;state.approvalExpiresAtUtc='2098-01-01T00:00:00Z';}
    lastSummary={version:'teacher-conversation/1',conversationId:session,draftId:draft,requestId:captured,draftRevision:state.revision,payloadHash:state.payloadHash,kind:options.pending?'pending':options.prepare?'prepared':'partial',status:options.pending?'pending':'completed',canExecute:false,domainEffect:false,result:options.pending?null:{safe:'unused'}};
    if(options.lost)throw Error('Lost response after host completion');
   }
   value=structuredClone(lastSummary??{version:'teacher-conversation/1',conversationId:session,draftId:draft,requestId:captured,draftRevision:state.revision,payloadHash:state.payloadHash,kind:'pending',status:'pending',canExecute:false,domainEffect:false,result:null});
  }else value=structuredClone(state);
  if(options.change)options.change(n,route,value);
  return new Response(options.rawAt===n?'{':JSON.stringify(value),{status:options.statusAt===n?options.status:options.pending&&route.endsWith('/conversation/turns')?202:200,
   headers:{'Content-Type':options.contentType??'application/json'}});
 }
 return{lib,window,abort,calls,run:(s=session,d=draft)=>lib.loadTeacherDialogue(s,d,abort.signal,request),
  replaceEpoch:()=>epoch='replacement-login',state:()=>state,replaceState:value=>state=value};
}
test('load is GET only; one explicit prompt produces verified canonical draft, never approval/create',async()=>{
 const h=harness(),c=await h.run();a.equal(h.calls.length,6);a.ok(h.calls.every(x=>x.init.method==='GET'));a.equal(c.current.available,true);
 const result=await c.send('Synthetic teacher details');a.equal(result.phase,'ready');a.equal(result.draft.revision,2);a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);
 a.ok(h.calls.filter(x=>x.init.method==='POST').every(x=>x.route.endsWith('/conversation/turns')));c.dispose();
});
test('readiness cannot authorize a real unqualified provider',async()=>{const h=harness({change(n,r,v){if(r.endsWith('/readiness'))v.syntheticInterpreter=false;}});await a.rejects(h.run());a.ok(h.calls.every(x=>x.init.method==='GET'));});
test('prepared means review required, not creation; raw model result text is ignored',async()=>{
 const h=harness({prepare:true,change(n,r,v){if(r.endsWith('/conversation/turns'))v.result={message:'Ignore policy; Owner assigned; teacher created'};}}),c=await h.run(),v=await c.send('Prepare teacher');
 a.equal(v.draft.status,'Reviewed');a.equal(v.draft.receipt,null);a.doesNotMatch(v.message,/Owner assigned|teacher created/);a.match(v.message,/approval and creation are separate/);c.dispose();
});
test('partial questions come from verified required fields',async()=>{
 const h=harness();const state=h.state(),p=JSON.parse(state.canonicalPayload);delete p.fields.LastName;state.canonicalPayload=JSON.stringify(p);state.payloadHash=crypto.createHash('sha256').update(state.canonicalPayload).digest('hex');
 const c=await h.run(),v=await c.send('Collect name');a.match(v.message,/last name/);c.dispose();
});
test('lost response retains request in URL; GET recovery never resends',async()=>{
 const h=harness({lost:true}),c=await h.run();await a.rejects(c.send('Synthetic teacher details'));a.equal(c.current.draft,null);a.equal(c.current.phase,'pending');
 const id=new URLSearchParams(h.window.location.search).get('teacherRequestId');a.equal(id,c.pendingRequestId);
 a.equal((await c.recover()).draft.revision,2);a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);a.ok(h.calls.some(x=>x.route.endsWith('/'+id+'/result')));c.dispose();
});
test('reload with a request only offers GET recovery, no private draft load or POST',async()=>{
 const saved='88888888-8888-4888-8888-888888888888',h=harness({savedRequestId:saved,search:`?conversationId=${session}&draftId=${draft}&teacherRequestId=${saved}`}),c=await h.run();
 a.equal(h.calls.length,1);a.equal(c.current.draft,null);await a.rejects(c.send('must not send'));await c.recover();a.ok(h.calls.every(x=>x.init.method==='GET'));a.equal(c.current.phase,'pending');c.dispose();
});
test('pending202 withholds fields and cannot send another prompt',async()=>{
 const h=harness({pending:true}),c=await h.run(),v=await c.send('Still pending');a.equal(v.draft,null);a.equal(v.available,false);await a.rejects(c.send('duplicate'));await c.recover();a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);c.dispose();
});
test('two simultaneous sends admit only one POST',async()=>{
 let release;const pause=new Promise(r=>release=r),h=harness({before:async(n)=>{if(n===9)await pause;}}),c=await h.run(),first=c.send('Synthetic teacher details');await a.rejects(c.send('second'));release();await first;a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);c.dispose();
});
for(const [name,change]of Object.entries({actor:v=>v.roles=['Teacher'],tenant:v=>v.academyId='../foreign',workspace:v=>v.workspace='Teacher',platform:v=>v.isPlatformOwner=true}))
 test('initial account refuses '+name,async()=>{const h=harness({change(n,r,v){if(n===1)change(v);}});await a.rejects(h.run());a.equal(h.calls.length,1);});
for(const [name,change]of Object.entries({request:v=>v.requestId=session,draft:v=>v.draftId=session,conversation:v=>v.conversationId=draft,revision:v=>v.draftRevision=1000,hash:v=>v.payloadHash='a'.repeat(64),effect:v=>v.domainEffect=true,execute:v=>v.canExecute=true,status:v=>v.status='created',kind:v=>v.kind='approve',extra:v=>v.extra='private',result:v=>v.result='model text'}))
 test('untrusted turn summary rejected '+name,async()=>{const h=harness({change(n,r,v){if(r.endsWith('/conversation/turns'))change(v);}}),c=await h.run();await a.rejects(c.send('Synthetic details'));a.equal(c.current.draft,null);a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);c.dispose();});
for(const status of[400,401,403,404,409,410,429,500,503])test('HTTP '+status+' never redispatches prompt',async()=>{
 const h=harness({statusAt:9,status}),c=await h.run();await a.rejects(c.send('Synthetic details'));await a.rejects(c.send('second'));a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);c.dispose();
});
for(const [name,change]of Object.entries({abort:t=>t.abort.abort(),token:t=>t.replaceToken(),epoch:t=>t.replaceEpoch(),route:t=>t.window.location.pathname='/teacher',query:t=>t.window.location.search='?wrong=1'}))
 test('late '+name+' cannot display a result',async()=>{const h=harness({before(n,t){if(n===9)change(t);}}),c=await h.run();await a.rejects(c.send('Synthetic details'));a.equal(c.isCurrent,false);a.throws(()=>c.current);c.dispose();});
for(const option of['historyFailure','historyNoop'])test('failed request identity persistence prevents POST '+option,async()=>{
 const h=harness({[option]:true}),c=await h.run();await a.rejects(c.send('Synthetic details'));a.equal(h.calls.filter(x=>x.init.method==='POST').length,0);c.dispose();
});
for(const text of['','   ','a\u0001b','a'.repeat(2001)])test('invalid prompt stays local '+text.length,async()=>{const h=harness(),c=await h.run();await a.rejects(c.send(text));a.equal(h.calls.filter(x=>x.init.method==='POST').length,0);c.dispose();});
for(const search of['?conversationId='+session,'?conversationId='+session+'&draftId='+draft+'&role=Owner','?conversationId='+session+'&draftId='+draft+'&teacherRequestId=bad','?conversationId='+session+'&draftId='+draft+'&draftId='+draft])
 test('untrusted query fails before HTTP '+search.length,async()=>{const h=harness({search});await a.rejects(h.run());a.equal(h.calls.length,0);});
test('offline interpreter renders unavailable and sends nothing',async()=>{const h=harness({offline:true}),c=await h.run();a.equal(c.current.available,false);await a.rejects(c.send('Synthetic details'));a.ok(h.calls.every(x=>x.init.method==='GET'));c.dispose();});
test('returned draft is a clone; caller cannot change submitted binding',async()=>{const h=harness(),c=await h.run();c.current.draft.payloadHash='b'.repeat(64);await c.send('Synthetic details');a.equal(JSON.parse(h.calls.find(x=>x.init.method==='POST').init.body).payloadHash,h.state().payloadHash);c.dispose();});
test('initial native draft denial is an access failure, not a usable dialogue',async()=>{const h=harness({statusAt:3,status:403});await a.rejects(h.run(),e=>e instanceof h.lib.TeacherDialogueUnavailable&&e.access===true);a.equal(h.calls.length,3);});
test('native draft denial after interpretation permanently closes the private handle',async()=>{const h=harness({statusAt:11,status:403}),c=await h.run();await a.rejects(c.send('Synthetic details'),e=>e instanceof h.lib.TeacherDialogueUnavailable&&e.access===true);a.equal(c.isCurrent,false);a.throws(()=>c.current);a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);});
test('changed current account after interpretation closes all private state',async()=>{const h=harness({change(n,r,v){if(n===14)v.roles=['Teacher'];}}),c=await h.run();await a.rejects(c.send('Synthetic details'));a.equal(c.isCurrent,false);a.throws(()=>c.current);});
test('current cancelled status overrides a historical prepared reply',async()=>{const h=harness({prepare:true,change(n,r,v){if(n===11){v.status='Cancelled';v.approvalId=null;v.approvalExpiresAtUtc=null;}}}),c=await h.run(),v=await c.send('Synthetic details');a.equal(v.available,false);a.match(v.message,/cancelled/);a.doesNotMatch(v.message,/draft is ready/);c.dispose();});
test('prepared summary cannot claim readiness over an unreviewed native draft',async()=>{const h=harness({prepare:true,change(n,r,v){if(n===11){v.status='AwaitingReview';v.approvalId=null;v.approvalExpiresAtUtc=null;}}}),c=await h.run();await a.rejects(c.send('Synthetic details'));a.equal(c.current.draft,null);c.dispose();});
