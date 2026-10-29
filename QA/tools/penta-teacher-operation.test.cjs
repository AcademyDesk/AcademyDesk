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
 const window=Object.assign(new EventTarget(),{location:{pathname:'/penta/teachers',search:''}});
 const api={apiUrl:'http://127.0.0.1:1',portalAccessToken:()=>token,portalSessionEpoch:()=>epoch,portalWorkspaceForPath:p=>p.startsWith('/penta')?'AcademyAdmin':'Teacher',
  portalSessionChangedEvent:'session-change',portalSignOutEvent:'sign-out',isPortalSignOutEvent:e=>e.key==='sign-out'};
 const account={academyId:academy,roles:['AcademyAdmin'],isPlatformOwner:false,workspace:'AcademyAdmin'};
 function load(file){file=path.resolve(file);if(modules.has(file))return modules.get(file).exports;const mod={exports:{}};modules.set(file,mod);
 const js=ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
 vm.runInNewContext(js,{module:mod,exports:mod.exports,require(name){if(name==='./api')return api;const target=path.resolve(path.dirname(file),name+'.ts');a.ok(target.startsWith(path.join(root,'apps/web/src')+path.sep));return load(target);},
 window,Date,crypto:crypto.webcrypto,TextEncoder,TextDecoder,Uint8Array,AbortController,fetch:()=>{throw Error('Uncontrolled fetch');}});return mod.exports;}
 const lib=load(path.join(root,'apps/web/src/lib/penta-teacher-operation.ts'));
 async function request(url,init){const route=new URL(url).pathname,n=calls.length+1;calls.push({route,init});
  if(options.before)await options.before(n,{window,abort,replaceToken:()=>token='replacement',replaceEpoch:()=>epoch='replacement-login'});
  let value;if(route.endsWith('/session'))value={...account};else{
   if(route.endsWith('/review'))Object.assign(state,{status:'Reviewed',approvalId:approval,approvalExpiresAtUtc:'2098-01-01T00:00:00Z'});
   if(route.endsWith('/approve'))state.status='Authorized';
   if(route.endsWith('/cancel'))Object.assign(state,{status:'Cancelled',approvalId:null,approvalExpiresAtUtc:null});
   if(route.endsWith('/execute')){Object.assign(state,{status:'Succeeded',receipt:{receiptId:receipt,teacherId:teacher,auditId:audit,completedAtUtc:'2026-10-27T12:00:00Z'}});if(options.lost)throw Error('Synthetic lost response after commit');}
   value=structuredClone(state);
  }
  if(options.change)options.change(n,route,value);
  return new Response(options.rawAt===n?'{':JSON.stringify(value),{status:options.statusAt===n?options.status:route.endsWith('/execute')?201:200,
   headers:{'Content-Type':options.contentType??'application/json'}});
 }
 return{lib,window,abort,calls,run:(s=session,d=draft)=>lib.loadTeacherCreationChannel(s,d,abort.signal,request),
  replaceEpoch:()=>epoch='replacement-login',state:()=>state,replaceState:value=>state=value};
}
async function prepared(h){const channel=await h.run();await channel.review();await channel.approve();return channel;}
test('passive load performs account/draft/account GET only and explicit operations produce receipt',async()=>{
 const h=harness(),channel=await h.run();a.equal(h.calls.length,3);a.ok(h.calls.every(x=>x.init.method==='GET'));
 await channel.review();await channel.approve();const result=await channel.execute();a.equal(result.receipt.teacherId,teacher);a.equal(result.status,'Succeeded');
 a.equal(h.calls.filter(x=>x.route.endsWith('/execute')).length,1);await a.rejects(channel.execute());
 a.equal(JSON.parse(h.calls.find(x=>x.route.endsWith('/approve')).init.body).intent,'CreateTeacherWithoutAccount');
 a.ok(h.calls.every(x=>x.init.credentials==='omit'&&x.init.cache==='no-store'&&x.init.redirect==='error'));channel.dispose();
});
test('lost execution permits GET-only receipt recovery without another execution',async()=>{
 const h=harness({lost:true}),channel=await prepared(h);await a.rejects(channel.execute());a.equal(channel.outcomeUnconfirmed,true);
 const value=await channel.refresh();a.equal(value.receipt.teacherId,teacher);await a.rejects(channel.execute());a.equal(h.calls.filter(x=>x.route.endsWith('/execute')).length,1);channel.dispose();
});
test('a returned snapshot cannot mutate the private operation binding',async()=>{
 const h=harness(),channel=await h.run(),copy=channel.current;copy.revision=99;copy.payloadHash='x';await channel.review();
 a.equal(JSON.parse(h.calls.at(-1).init.body).revision,1);channel.dispose();
});
test('review is not execution approval',async()=>{const h=harness(),channel=await h.run();await channel.review();const n=h.calls.length;await a.rejects(channel.execute());a.equal(h.calls.length,n);channel.dispose();});
test('cancel is terminal and does not create teacher',async()=>{const h=harness(),channel=await h.run();await channel.review();await channel.cancel();await a.rejects(channel.approve());await a.rejects(channel.execute());a.equal(h.calls.filter(x=>x.route.endsWith('/execute')).length,0);channel.dispose();});
test('unknown approval does not automatically approve or execute after recovery',async()=>{
 const h=harness({statusAt:5,status:503}),channel=await h.run();await channel.review();await a.rejects(channel.approve());await channel.refresh();
 const n=h.calls.length;await a.rejects(channel.approve());await a.rejects(channel.execute());a.equal(h.calls.length,n);channel.dispose();
});
test('double-click sends at most one execution',async()=>{
 let release;const wait=new Promise(r=>release=r),h=harness({before:async n=>{if(n===6)await wait;}}),channel=await prepared(h);
 const first=channel.execute();await a.rejects(channel.execute());release();await first;a.equal(h.calls.filter(x=>x.route.endsWith('/execute')).length,1);channel.dispose();
});
for(const [name,change]of Object.entries({role:v=>v.roles=['Teacher'],platform:v=>v.isPlatformOwner=true,workspace:v=>v.workspace='Teacher',tenant:v=>v.academyId='../tenant'}))
test('account denies '+name,async()=>{const h=harness({change(n,route,v){if(n===1)change(v);}});await a.rejects(h.run());a.equal(h.calls.length,1);});
for(const [name,change]of Object.entries({id:v=>v.id=session,session:v=>v.sessionId=draft,revision:v=>v.revision=0,context:v=>v.contextVersion=-1,status:v=>v.status='Paid',hash:v=>v.payloadHash='a'.repeat(64),
 extra:v=>v.role='Owner',approval:v=>v.approvalId=approval,receipt:v=>v.receipt={teacherId:teacher},zone:v=>v.expiresAtUtc='2099-01-01T00:00:00'}))
test('draft rejects '+name,async()=>{const h=harness({change(n,route,v){if(n===2)change(v);}});await a.rejects(h.run());a.equal(h.calls.length,2);});
for(const [name,change]of Object.entries({role:p=>p.fields.Role={kind:'text',value:'Owner'},tool:p=>p.tool_key='AcademyDesk.StudentOnboarding.Create',reference:p=>p.fields.BranchId={kind:'reference',value:'../foreign'},
 date:p=>p.fields.DateOfBirth={kind:'date',value:'2026-02-30'},long:p=>p.fields.FirstName.value='x'.repeat(121),control:p=>p.fields.FirstName.value='bad\nvalue'}))
test('canonical payload rejects '+name,async()=>{const h=harness({change(n,route,v){if(n===2){const p=JSON.parse(v.canonicalPayload);change(p);v.canonicalPayload=JSON.stringify(p);v.payloadHash=crypto.createHash('sha256').update(v.canonicalPayload).digest('hex');}}});await a.rejects(h.run());});
for(const n of[1,2,3,4,5,6])test('identical token replacement at request '+n+' discards response',async()=>{
 const h=harness({before(number,t){if(number===n)t.replaceEpoch();}});let c;
 if(n<=3)await a.rejects(h.run());else{c=await h.run();if(n===4)await a.rejects(c.review());else{await c.review();if(n===5)await a.rejects(c.approve());else{await c.approve();await a.rejects(c.execute());}}c.dispose();}
 a.equal(h.calls.length,n);
});
for(const [name,before]of Object.entries({abort:t=>t.abort.abort(),token:t=>t.replaceToken(),route:t=>t.window.location.pathname='/teacher',query:t=>t.window.location.search='?other=1'}))
test('late '+name+' cannot report success',async()=>{const h=harness({before(n,t){if(n===6)before(t);}}),c=await prepared(h);await a.rejects(c.execute());await a.rejects(c.refresh());a.equal(h.calls.length,6);c.dispose();});
for(const status of[400,401,403,404,409,410,429,500,503])test('HTTP '+status+' never retries execution',async()=>{
 const h=harness({statusAt:6,status}),c=await prepared(h);await a.rejects(c.execute());await a.rejects(c.execute());a.equal(h.calls.filter(x=>x.route.endsWith('/execute')).length,1);c.dispose();
});
test('malformed mutation response becomes unconfirmed without retry',async()=>{const h=harness({rawAt:6}),c=await prepared(h);await a.rejects(c.execute());await a.rejects(c.execute());a.equal(c.outcomeUnconfirmed,true);c.dispose();});
test('unsafe lookup keys refuse before transport',async()=>{const h=harness();await a.rejects(h.run('../other',draft));await a.rejects(h.run(session,session));a.equal(h.calls.length,0);});
test('missed login event still invalidates existing channel',async()=>{const h=harness(),c=await h.run();h.replaceEpoch();await a.rejects(c.review());await a.rejects(c.refresh());a.equal(h.calls.length,3);c.dispose();});
test('non-JSON response fails closed',async()=>{const h=harness({contentType:'text/html'});await a.rejects(h.run());a.equal(h.calls.length,1);});
for(const status of[401,403])test('server '+status+' closes old private view and every operation',async()=>{
 const h=harness({statusAt:6,status}),c=await prepared(h);await a.rejects(c.execute());a.throws(()=>c.current);
 const n=h.calls.length;await a.rejects(c.refresh());await a.rejects(c.cancel());a.equal(h.calls.length,n);c.dispose();
});
