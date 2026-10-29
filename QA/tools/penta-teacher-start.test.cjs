'use strict';
// Synthetic transport tests. Real SQL/browser acceptance is separate.
const a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),crypto=require('node:crypto');
const ts=require('../../apps/web/node_modules/typescript');
const root=path.resolve(__dirname,'../..'),academy='33333333-3333-4333-8333-333333333333',conversation='11111111-1111-4111-8111-111111111111',draft='22222222-2222-4222-8222-222222222222';
const checks=[];
function harness(options={}){
  let token='synthetic',loginEpoch='synthetic-original-login',requestId=options.savedRequestId;const abort=new AbortController(),calls=[],history=[],modules=new Map(),window={location:{pathname:'/penta/teachers/new',search:options.search||'',hash:''}};
  window.history={state:{nextRouter:'retained'},replaceState(state,title,url){if(options.historyFailure)throw Error('Synthetic history failure');history.push({url,state});if(!options.historyNoop){const parsed=new URL(url,'http://127.0.0.1');window.location.pathname=parsed.pathname;window.location.search=parsed.search;}if(options.historyNavigate)window.location.pathname='/dashboard';}};
  const account={academyId:academy,roles:['AcademyAdmin'],workspace:'AcademyAdmin',isPlatformOwner:false};
  const api={apiUrl:'http://127.0.0.1:1',portalAccessToken:()=>token,portalSessionEpoch:()=>loginEpoch,portalWorkspaceForPath:p=>p.startsWith('/teacher')?'Teacher':'AcademyAdmin'};
  function load(file){file=path.resolve(file);if(modules.has(file))return modules.get(file).exports;const mod={exports:{}};modules.set(file,mod);
    const js=ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
    vm.runInNewContext(js,{module:mod,exports:mod.exports,require(name){if(name==='./api')return api;let target=path.resolve(path.dirname(file),name);if(!path.extname(target))target+='.ts';a.ok(target.startsWith(path.join(root,'apps/web/src')+path.sep));return load(target);},window,crypto:crypto.webcrypto,AbortController,TextDecoder,Uint8Array,URLSearchParams,fetch:()=>{throw Error('Unexpected default fetch');}});return mod.exports;}
  async function request(url,init){const route=new URL(url).pathname;calls.push({route,init});const n=calls.length;if(options.before)await options.before(n,{abort,window,replace:()=>token='replaced',replaceEpoch:()=>loginEpoch='synthetic-replacement-login'});
    let value;if(route.endsWith('/session'))value={...account};else if(route.endsWith('/readiness'))value={version:'teacher-start/1',academyId:academy,available:!options.offline,canStart:!options.offline,canExecute:false,syntheticInterpreter:!options.offline};
    else {if(init.method==='POST'){requestId=JSON.parse(init.body).requestId;if(options.lost)throw Error('Lost after real-host simulation');}
      value={version:'teacher-start/1',academyId:academy,requestId,conversationId:conversation,draftId:draft,expiresAtUtc:'2099-10-19T00:00:00Z',status:'conversation_started',canExecute:false,domainEffect:false};}
    if(options.change)options.change(route,value,n);return new Response(options.rawAt===n?'{':JSON.stringify(value),{status:options.statusAt===n?options.status:init.method==='POST'?201:200,headers:{'Content-Type':'application/json'}});}
  const lib=load(path.join(root,'apps/web/src/lib/penta-teacher-start.ts'));return{calls,abort,window,history,replaceEpoch:()=>loginEpoch='synthetic-replacement-login',run:()=>lib.loadTeacherStart(abort.signal,request)};
}
async function check(name,run){try{await run();checks.push({name,pass:true});}catch(error){checks.push({name,pass:false});console.error(name+': '+error.message);}}
(async()=>{
  await check('mount only account/readiness GET; explicit start one POST then current account',async()=>{const h=harness(),s=await h.run();a.equal(h.calls.length,2);a.equal(s.available,true);const r=await s.start();a.equal(r.conversationId,conversation);a.equal(r.draftId,draft);a.equal(h.calls.length,6);a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);await a.rejects(s.start());
    a.ok(h.calls.every(x=>x.init.credentials==='omit'&&x.init.cache==='no-store'&&x.init.redirect==='error'));});
  await check('unknown delivery recovers same request GET only without second start',async()=>{const h=harness({lost:true}),s=await h.run();await a.rejects(s.start());const r=await s.check();a.equal(r.draftId,draft);a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);a.equal(h.calls.filter(x=>x.route.includes('/starts/')).length,1);await a.rejects(s.start());});
  await check('not started check sends nothing',async()=>{const h=harness(),s=await h.run();await a.rejects(s.check());a.equal(h.calls.length,2);});
  await check('offline start remains unavailable',async()=>{const h=harness({offline:true}),s=await h.run();a.equal(s.available,false);await a.rejects(s.start());a.equal(h.calls.length,2);});
  for(const [name,change]of Object.entries({teacher:v=>v.roles=['Teacher'],foreignWorkspace:v=>v.workspace='Teacher',platform:v=>v.isPlatformOwner=true,invalidTenant:v=>v.academyId='../other'}))await check('account rejects '+name,async()=>{const h=harness({change(route,v){if(route.endsWith('/session'))change(v);}});await a.rejects(h.run());a.equal(h.calls.length,1);});
  for(const [name,change]of Object.entries({version:v=>v.version='other',tenant:v=>v.academyId=conversation,grant:v=>v.canExecute=true,mismatch:v=>v.canStart=false,missingSynthetic:v=>delete v.syntheticInterpreter,syntheticType:v=>v.syntheticInterpreter='true',unqualifiedProvider:v=>v.syntheticInterpreter=false,extra:v=>v.secret='never'}))await check('readiness rejects '+name,async()=>{const h=harness({change(route,v){if(route.endsWith('/readiness'))change(v);}});await a.rejects(h.run());a.equal(h.calls.length,2);});
  for(const [name,change]of Object.entries({request:v=>v.requestId=conversation,tenant:v=>v.academyId=conversation,conversation:v=>v.conversationId='../../',draft:v=>v.draftId='bad',same:v=>v.draftId=v.conversationId,expiry:v=>v.expiresAtUtc='2000-01-01T00:00:00Z',zone:v=>v.expiresAtUtc='2099-10-19T00:00:00',grant:v=>v.canExecute=true,effect:v=>v.domainEffect=true,status:v=>v.status='student_created',extra:v=>v.extra='not displayed'}))await check('start result rejects '+name,async()=>{const h=harness({change(route,v){if(route.endsWith('/conversations'))change(v);}}),s=await h.run();await a.rejects(s.start());await a.rejects(s.start());a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);});
  for(const status of[400,401,403,404,409,410,429,500,503])await check('HTTP '+status+' no retry',async()=>{const h=harness({statusAt:5,status}),s=await h.run();await a.rejects(s.start());await a.rejects(s.start());a.equal(h.calls.length,5);});
  for(const [name,before]of Object.entries({abort:(n,t)=>{if(n===5)t.abort.abort();},token:(n,t)=>{if(n===5)t.replace();},route:(n,t)=>{if(n===5)t.window.location.pathname='/dashboard';}}))await check('late '+name+' discards start',async()=>{const h=harness({before}),s=await h.run();await a.rejects(s.start());a.equal(h.calls.length,5);});
  await check('double-click admits only one start',async()=>{let release;const wait=new Promise(r=>release=r),h=harness({before:async n=>{if(n===5)await wait;}}),s=await h.run();const first=s.start();await a.rejects(s.start());release();await first;a.equal(h.calls.filter(x=>x.init.method==='POST').length,1);});
  await check('malformed start body withheld',async()=>{const h=harness({rawAt:5}),s=await h.run();await a.rejects(s.start());await a.rejects(s.start());a.equal(h.calls.length,5);});
  for(const at of[1,2,3,4,5,6])await check('identical-token replacement during start request '+at+' withheld',async()=>{
    const h=harness({before:(n,t)=>{if(n===at)t.replaceEpoch();}});
    if(at<3)await a.rejects(h.run());else{const s=await h.run();await a.rejects(s.start());await a.rejects(s.start());}
    a.equal(h.calls.length,at);
  });
  await check('old ready start handle cannot cross missed login replacement',async()=>{const h=harness(),s=await h.run();h.replaceEpoch();await a.rejects(s.start());a.equal(h.calls.length,2);});
  await check('unknown start cannot recover under identical-token replacement',async()=>{const h=harness({lost:true}),s=await h.run();await a.rejects(s.start());h.replaceEpoch();const count=h.calls.length;await a.rejects(s.check());a.equal(h.calls.length,count);});
  await check('recovery lookup key is recorded in current URL before the POST',async()=>{
    const h=harness({before(n){if(n===5){a.equal(h.history.length,1);a.equal(new URLSearchParams(h.window.location.search).get('teacherStartRequestId'),JSON.parse(h.calls.at(-1).init.body).requestId);}}}),s=await h.run();await s.start();
    a.match(h.history[0].url,/^\/penta\/teachers\/new\?teacherStartRequestId=[0-9a-f-]{36}$/);a.equal(h.history[0].state.nextRouter,'retained');
  });
  await check('lost start survives page reload and performs only same-key GET recovery',async()=>{
    const h=harness({lost:true}),s=await h.run();await a.rejects(s.start());const saved=JSON.parse(h.calls.find(x=>x.init.method==='POST').init.body).requestId;
    const reloaded=harness({search:h.window.location.search,savedRequestId:saved}),again=await reloaded.run();a.equal(again.recovering,true);a.equal(again.available,false);await a.rejects(again.start());const found=await again.check();a.equal(found.draftId,draft);
    a.ok(reloaded.calls.every(x=>x.init.method==='GET'));a.equal(reloaded.calls.filter(x=>x.route.endsWith('/starts/'+saved)).length,1);a.equal(reloaded.history.length,0);
  });
  await check('recovery remains read-only when interpreter becomes unavailable after reload',async()=>{
    const saved='44444444-4444-4444-8444-444444444444',h=harness({offline:true,search:'?teacherStartRequestId='+saved,savedRequestId:saved}),s=await h.run();a.equal(s.recovering,true);a.equal(s.available,false);a.equal((await s.check()).conversationId,conversation);a.ok(h.calls.every(x=>x.init.method==='GET'));
  });
  for(const search of['?teacherStartRequestId=bad','?teacherStartRequestId=00000000-0000-0000-0000-000000000000','?teacherStartRequestId='+conversation+'&teacherStartRequestId='+draft,'?teacherStartRequestId='+conversation+'&academyId='+academy,'?other='+conversation])await check('untrusted recovery query rejects before transport '+search.split('=')[0],async()=>{
    const h=harness({search});await a.rejects(h.run());a.equal(h.calls.length,0);
  });
  for(const option of['historyFailure','historyNoop','historyNavigate'])await check('failed recovery-key persistence blocks POST '+option,async()=>{
    const h=harness({[option]:true}),s=await h.run();await a.rejects(s.start());a.equal(h.calls.filter(x=>x.init.method==='POST').length,0);
  });
  await check('edited recovery URL invalidates the current lookup handle',async()=>{
    const h=harness({savedRequestId:conversation,search:'?teacherStartRequestId='+conversation}),s=await h.run();h.window.location.search='?teacherStartRequestId='+draft;const count=h.calls.length;await a.rejects(s.check());a.equal(h.calls.length,count);
  });
  const report={passed:checks.filter(x=>x.pass).length,failed:checks.filter(x=>!x.pass).length,checks,synthetic:true,modelInference:false,sqlAcceptance:false};
  if(process.env.QA_PENTA_TEACHER_START_OUTPUT){const output=path.resolve(process.env.QA_PENTA_TEACHER_START_OUTPUT);a.ok(!fs.existsSync(output));fs.mkdirSync(path.dirname(output),{recursive:true});fs.writeFileSync(output,JSON.stringify(report,null,2));}
  console.log(JSON.stringify({passed:report.passed,failed:report.failed}));if(report.failed)process.exitCode=1;
})().catch(error=>{console.error(error.message);process.exitCode=1;});
