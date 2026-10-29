'use strict';
// Explicitly synthetic coordinator contracts, not real SQL/model acceptance.
const a=require('node:assert/strict'),crypto=require('node:crypto'),fs=require('node:fs'),path=require('node:path');
const {fixture,harness,conversation,draft,academy,actor,epoch}=require('./penta-onboarding-review.test.cjs');
const checks=[];
function setup(options={}) {
  const f=fixture();for(const[key,kind,max]of[['StudentLastName','text',120],['DateOfBirth','date',10]])f.current.snapshot.tools[0].fields.push({key,kind,label:key,required:true,clearable:false,choices:[],columns:[],max_length:max});
  let saved=null,posts=0,recovered=false;
  const h=harness(f,{onRequest:options.onRequest,async respond(n,url,init){
    const route=new URL(url).pathname;let value,status=200;
    if(route==='/api/auth/session')value=f.account;
    else if(route.endsWith('/current'))value=f.current;
    else if(route.endsWith('/drafts/'+draft))value=f.view;
    else if(route.endsWith('/send-readiness')) value={version:'onboarding-send/1',academyId:academy,actorId:actor,conversationId:conversation,draftId:draft,
      conversationVersion:f.current.version,draftRevision:f.view.revision,contextVersion:1,authorityVersion:1,payloadHash:f.view.payloadHash,
      canSend:options.unavailable!==true,runtime:options.unavailable?'unavailable':'synthetic_test',qualification:'not_qualified',canExecute:false};
    else if(route.endsWith('/browser-turns')) {
      const body=JSON.parse(init.body);a.equal(body.expectedVersion,f.current.version);a.equal(body.draftRevision,f.view.revision);a.equal(body.payloadHash,f.view.payloadHash);
      a.deepEqual(Object.keys(body).sort(),['draftRevision','expectedVersion','payloadHash','requestId','text']);posts++;
      if(options.wait)await options.wait;
      f.current.version++;f.view.revision++;f.current.draftRevision=f.view.revision;
      const fields={StudentFirstName:{kind:'text',value:'Synthetic'}};
      if(posts>1)Object.assign(fields,{StudentLastName:{kind:'text',value:'Conversation'},DateOfBirth:{kind:'date',value:'2000-01-01'}});
      f.view.canonicalPayload=JSON.stringify({tool_key:'AcademyDesk.StudentOnboarding.Create',tool_revision:1,fields});
      f.view.payloadHash=crypto.createHash('sha256').update(f.view.canonicalPayload).digest('hex');
      f.view.status=posts>1?'Prepared':'AwaitingReview';Object.assign(f.current.snapshot.draft,{revision:f.view.revision,fields,status:posts>1?'awaiting_approval':'collecting',
        operation_ref:posts>1?`onboarding:${draft.replaceAll('-','')}:${f.view.revision}`:null});
      const op={ref:f.current.snapshot.draft.operation_ref,request_ref:body.requestId,draft_ref:draft,draft_revision:f.view.revision,
        tool_key:'AcademyDesk.StudentOnboarding.Create',tool_revision:1,approval_ref:null,status:'awaiting_approval'};
      saved={conversationId:conversation,requestId:body.requestId,version:f.current.version,draftId:draft,draftRevision:f.view.revision,
        kind:posts>1?'host_prepare':'clarify',status:'draft_updated',snapshot:structuredClone(f.current.snapshot),operations:posts>1?[op]:[],
        questions:posts>1?[]:['StudentLastName','DateOfBirth'],canExecute:false};
      if(options.lost)throw Error('Synthetic delivery lost after host completion');value=saved;status=201;
    }else if(/\/claims\/[0-9a-f-]{36}\/result$/.test(route)){
      a.ok(saved);a.ok(route.endsWith('/'+saved.requestId+'/result'));value=saved;
      if(options.pendingOnce&&!recovered){recovered=true;status=202;value={conversationId:conversation,requestId:saved.requestId,version:saved.version,status:'pending',canExecute:false};}
    }else throw Error('Unexpected dialogue endpoint');
    value=structuredClone(value);if(options.change)options.change(route,value,n);
    if(options.httpAt&&route.endsWith(options.httpAt))status=options.http;
    return new Response(JSON.stringify(value),{status,headers:{'Content-Type':'application/json'}});
  }});
  const outer=new AbortController();
  return {...h,outer,async start(){const p=await h.run(),lib=h.dialogue(),dialogue=lib.createOnboardingDialogue(p,outer.signal,h.request);return{p,lib,dialogue};},get posts(){return posts;}};
}
const intent=(revision=1,text='Synthetic onboarding prompt')=>({kind:'send',text,capability:'Executor',base_revision:revision});
async function check(name,run){try{await run();checks.push({name,pass:true});}catch(e){checks.push({name,pass:false,type:e.name});console.error('FAIL '+name+': '+e.message);}}
(async()=>{
  await check('construction never checks readiness or sends on mount',async()=>{const h=setup(),{dialogue}=await h.start();a.equal(h.calls.length,4);dialogue.dispose();});
  await check('explicit connect only reads; synthetic label never a qualified model',async()=>{const h=setup(),{dialogue}=await h.start(),r=await dialogue.connect();a.equal(r.available,true);a.equal(r.projection.controls.send,true);a.equal(r.projection.models.length,0);a.match(r.projection.messages[0].text,/not qualified AI/);a.ok(h.calls.every(x=>x.init.method==='GET'));dialogue.dispose();});
  await check('two explicit turns collect then prepare, preserving dialogue and verified provenance',async()=>{const h=setup(),{dialogue}=await h.start();await dialogue.connect();
    const partial=await dialogue.send(intent(),new AbortController().signal);a.equal(partial.host.draft.revision,2);a.equal(partial.controls.send,true);a.match(partial.messages[1].text,/StudentLastName, DateOfBirth/);
    const prepared=await dialogue.send(intent(2,'Synthetic second turn'),new AbortController().signal);a.equal(prepared.host.draft.revision,3);a.equal(prepared.controls.send,false);a.equal(prepared.messages.length,4);
    a.equal(h.posts,2);const channel=h.operation().createOnboardingOperationChannel(prepared,h.request);channel.dispose();
    a.ok(!h.calls.some(x=>/\/(challenge|confirm|execute)$/.test(x.url)));dialogue.dispose();});
  await check('unavailable response leaves send disabled and no POST',async()=>{const h=setup({unavailable:true}),{dialogue}=await h.start();const r=await dialogue.connect();a.equal(r.available,false);await a.rejects(dialogue.send(intent(),new AbortController().signal));a.equal(h.posts,0);dialogue.dispose();});
  await check('copied initial projection cannot invent provenance',async()=>{const h=setup(),{p,lib,dialogue}=await h.start();a.throws(()=>lib.createOnboardingDialogue(structuredClone(p),h.outer.signal,h.request));dialogue.dispose();});
  for(const[key,value]of Object.entries({actorId:epoch,academyId:epoch,conversationId:epoch,draftId:epoch,conversationVersion:2,draftRevision:2,
    payloadHash:'a'.repeat(64),contextVersion:2,authorityVersion:2,canExecute:true,qualification:'qualified',runtime:'real_model',canSend:'true',role:'Owner'}))
    await check('readiness rejects manipulated '+key,async()=>{const h=setup({change(route,v){if(route.endsWith('/send-readiness'))v[key]=value;}}),{dialogue}=await h.start();await a.rejects(dialogue.connect());a.equal(h.posts,0);dialogue.dispose();});
  for(const http of[400,401,403,404,409,410,429,503])await check('readiness HTTP '+http+' no retry',async()=>{const h=setup({httpAt:'/send-readiness',http}),{dialogue}=await h.start();await a.rejects(dialogue.connect());a.equal(h.calls.length,6);a.equal(h.posts,0);dialogue.dispose();});
  await check('lost completed response reconciles same request GET and fresh projection without POST replay',async()=>{const h=setup({lost:true}),{dialogue}=await h.start();await dialogue.connect();await a.rejects(dialogue.send(intent(),new AbortController().signal));a.equal(dialogue.pending,true);await a.rejects(dialogue.send(intent(),new AbortController().signal));
    const before=h.calls.length,r=await dialogue.recover();a.equal(r.host.draft.revision,2);a.equal(r.messages.length,2);a.ok(h.calls.slice(before).every(x=>x.init.method==='GET'));a.equal(h.posts,1);dialogue.dispose();});
  await check('pending202 remains unknown until explicit same-ID completion check',async()=>{const h=setup({lost:true,pendingOnce:true}),{dialogue}=await h.start();await dialogue.connect();await a.rejects(dialogue.send(intent(),new AbortController().signal));a.equal(await dialogue.recover(),null);a.equal(dialogue.pending,true);a.equal((await dialogue.recover()).host.draft.revision,2);a.equal(h.posts,1);dialogue.dispose();});
  await check('foreign question key is never rendered as prose',async()=>{const h=setup({change(route,v){if(route.endsWith('/browser-turns'))v.questions=['GrantRole'];}}),{dialogue}=await h.start();await dialogue.connect();await a.rejects(dialogue.send(intent(),new AbortController().signal));a.equal(dialogue.pending,true);dialogue.dispose();});
  await check('parallel sends admit one prompt',async()=>{let release;const wait=new Promise(r=>{release=r;}),h=setup({wait}),{dialogue}=await h.start();await dialogue.connect();const first=dialogue.send(intent(),new AbortController().signal);await a.rejects(dialogue.send(intent(),new AbortController().signal));release();a.equal((await first).host.draft.revision,2);a.equal(h.posts,1);dialogue.dispose();});
  await check('credential replacement during turn cannot publish private result',async()=>{const h=setup({onRequest(n,tools){if(n===9)tools.replaceToken();}}),{dialogue}=await h.start();await dialogue.connect();await a.rejects(dialogue.send(intent(),new AbortController().signal));dialogue.dispose();});
  await check('route replacement during turn cannot publish private result',async()=>{const h=setup({onRequest(n,tools){if(n===9)tools.window.location.search='?other=1';}}),{dialogue}=await h.start();await dialogue.connect();await a.rejects(dialogue.send(intent(),new AbortController().signal));dialogue.dispose();});
  await check('pre-aborted prompt does not dispatch',async()=>{const h=setup(),{dialogue}=await h.start();await dialogue.connect();const stop=new AbortController();stop.abort();await a.rejects(dialogue.send(intent(),stop.signal));a.equal(h.calls.length,6);dialogue.dispose();});
  await check('outer abort prevents reads and writes',async()=>{const h=setup(),{dialogue}=await h.start();h.outer.abort();await a.rejects(dialogue.connect());a.equal(h.calls.length,4);dialogue.dispose();});
  await check('invalid text and stale revision do not dispatch',async()=>{for(const v of[intent(2),intent(1,''),intent(1,'x'.repeat(2001)),intent(1,'bad\u0001')]){const h=setup(),{dialogue}=await h.start();await dialogue.connect();await a.rejects(dialogue.send(v,new AbortController().signal));a.equal(h.posts,0);dialogue.dispose();}});
  for(const at of[5,6,7,8,9,10,11,12,13,14,15])await check('identical-token replacement during dialogue request '+at+' withheld',async()=>{
    const h=setup({onRequest(n,tools){if(n===at)tools.replaceEpoch();}}),{dialogue}=await h.start();let failed=false;
    try{await dialogue.connect();await dialogue.send(intent(),new AbortController().signal);}catch{failed=true;}
    a.equal(failed,true);a.equal(h.calls.length,at);const count=h.calls.length;
    await a.rejects(dialogue.recover());a.equal(h.calls.length,count);dialogue.dispose();
  });
  const report={passed:checks.filter(x=>x.pass).length,failed:checks.filter(x=>!x.pass).length,checks,synthetic:true,sqlAcceptance:false,modelInference:false};
  if(process.env.QA_PENTA_DIALOGUE_OUTPUT){const file=path.resolve(process.env.QA_PENTA_DIALOGUE_OUTPUT);a.ok(!fs.existsSync(file));fs.mkdirSync(path.dirname(file),{recursive:true});fs.writeFileSync(file,JSON.stringify(report,null,2));}
  console.log(JSON.stringify({passed:report.passed,failed:report.failed}));if(report.failed)process.exitCode=1;
})().catch(e=>{console.error(e.message);process.exitCode=1;});
