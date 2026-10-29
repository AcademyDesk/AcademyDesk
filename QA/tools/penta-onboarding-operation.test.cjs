'use strict';
// Synthetic host boundary tests; no real model, SQL or browser acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { fixture, harness, conversation, draft, academy, actor, epoch } = require('./penta-onboarding-review.test.cjs');
const approval = '66666666-6666-4666-8666-666666666666', receiptId='77777777-7777-4777-8777-777777777777';
const audit='88888888-8888-4888-8888-888888888888', student='99999999-9999-4999-8999-999999999999';
const checks=[];
function setup(options={}) {
  const f=fixture(), ref=`onboarding:${draft.replaceAll('-','')}:1`;
  f.view.status='Prepared'; f.current.snapshot.draft.status='awaiting_approval'; f.current.snapshot.draft.operation_ref=ref;
  const operation={ ref,request_ref:epoch,scope:structuredClone(f.current.snapshot.scope),tool_key:'AcademyDesk.StudentOnboarding.Create',
    tool_revision:1,draft_ref:draft,draft_revision:1,payload_hash:f.view.payloadHash,canonical_fields:structuredClone(f.current.snapshot.draft.fields),
    preview:'Synthetic exact operation',approval_ref:null,approval_expires_at:null,status:'awaiting_approval' };
  const receipt={operation_ref:ref,scope:structuredClone(operation.scope),payload_hash:f.view.payloadHash,draft_revision:1,
    status:'awaiting_approval',receipt_ref:null,audit_ref:null,source_refs:[],error_code:'none'};
  function status(value) {operation.status=value; receipt.status=value; f.current.snapshot.draft.status=value;}
  const h=harness(f,{onRequest:options.onRequest,async respond(n,url,init){
    const route=new URL(url).pathname;
    let value;
    if(n<=4) value=n===1?f.account:n===3?f.view:f.current;
    else if(route.endsWith('/core/challenge')){operation.approval_ref=approval;operation.approval_expires_at='2099-10-18T12:00:00Z';value=operation;}
    else if(route.endsWith('/core/confirm')){status('approved');if(options.lostConfirm)throw Error('Synthetic lost confirmation response');value=operation;}
    else if(route.endsWith('/core/execute')){
      if(options.wait)await options.wait;
      status('succeeded');Object.assign(receipt,{receipt_ref:receiptId,audit_ref:audit,source_refs:[student]});
      if(options.lostExecute)throw Error('Synthetic lost response after commit');value=receipt;
    }else if(route.endsWith('/core/operation'))value=operation;
    else if(route.endsWith('/core/status'))value=receipt;
    else if(route==='/api/auth/session')value=f.account;
    else if(route.endsWith('/current'))value=f.current;
    else throw Error('Unexpected test request');
    value=structuredClone(value); if(options.change)options.change(route,value,n);
    return new Response(options.rawAt===route?'{':JSON.stringify(value),{status:options.statusAt===route?options.status:200,headers:{'Content-Type':'application/json'}});
  }});
  return {...h,async start(){const p=await h.run();const lib=h.operation();const channel=lib.createOnboardingOperationChannel(p,request);
    return{channel,p,lib};},getOperation:()=>operation};
  function request(url,init){ // Same controlled transport used by the authenticated loader.
    return transport(url,init);
  }
  async function transport(url,init){
    // harness exposes the same request closure below; no production substitution.
    return h.request(url,init);
  }
}
async function check(name,run){try{await run();checks.push({name,pass:true});}catch(error){checks.push({name,pass:false,type:error.name});console.error(`FAIL ${name}: ${error.message}`);}}
const intent=p=>({kind:'confirm',operation_ref:p.operation.ref,approval_ref:p.operation.approval_ref,revision:p.operation.draft_revision,payload_hash:p.operation.payload_hash});
(async()=>{
  await check('construction and passive mount dispatch no action',async()=>{const h=setup(),{channel}=await h.start();a.equal(h.calls.length,4);channel.dispose();});
  await check('exact explicit review then approval then creation gives source-backed result',async()=>{
    const h=setup(),{channel}=await h.start();let s=await channel.review();a.equal(s.phase,'review');a.equal(s.projection.controls.confirm,true);
    a.equal(s.projection.receipt.status,'awaiting_approval');a.equal(s.studentId,null);
    s=await channel.confirm(intent(s.projection));a.equal(s.phase,'approved');a.equal(s.projection.controls.confirm,false);a.equal(s.studentId,null);
    s=await channel.execute();a.equal(s.phase,'succeeded');a.equal(s.studentId,student);a.equal(s.projection.receipt.audit_ref,audit);
    for(const action of['challenge','confirm','execute'])a.equal(h.calls.filter(x=>x.url.endsWith('/core/'+action)).length,1);
    a.ok(h.calls.every(x=>x.init.credentials==='omit'&&x.init.redirect==='error'&&x.init.cache==='no-store'));
    await a.rejects(channel.execute());a.equal(h.calls.filter(x=>x.url.endsWith('/core/execute')).length,1);channel.dispose();
  });
  await check('no execute before explicit current approval',async()=>{const h=setup(),{channel}=await h.start();await a.rejects(channel.execute());a.equal(h.calls.length,4);channel.dispose();});
  await check('lost execute response reconciles committed receipt without second execution',async()=>{
    const h=setup({lostExecute:true}),{channel}=await h.start();const s=await channel.review();await channel.confirm(intent(s.projection));
    await a.rejects(channel.execute());const recovered=await channel.recover();a.equal(recovered.phase,'succeeded');a.equal(recovered.studentId,student);
    a.equal(h.calls.filter(x=>x.url.endsWith('/core/execute')).length,1);channel.dispose();
  });
  await check('copied projection cannot invent verified review provenance',async()=>{const h=setup(),{p,lib,channel}=await h.start();
    a.throws(()=>lib.createOnboardingOperationChannel(JSON.parse(JSON.stringify(p))));a.equal(h.calls.length,4);channel.dispose();});
  await check('mutated original projection cannot invent review authority',async()=>{const h=setup(),{p,lib,channel}=await h.start();p.host.scope.actor_id=epoch;
    a.throws(()=>lib.createOnboardingOperationChannel(p));a.equal(h.calls.length,4);channel.dispose();});
  for(const[key,value]of Object.entries({operation_ref:'foreign',approval_ref:epoch,revision:2,payload_hash:'a'.repeat(64)}))
    await check('confirm rejects manipulated '+key,async()=>{const h=setup(),{channel}=await h.start(),s=await channel.review();const before=h.calls.length;
      await a.rejects(channel.confirm({...intent(s.projection),[key]:value}));a.equal(h.calls.length,before);channel.dispose();});
  for(const[name,change]of Object.entries({
    'foreign actor':v=>{v.scope.actor_id=epoch;},'foreign tenant':v=>{v.scope.tenant_id=epoch;},'wrong digest':v=>{v.payload_hash='a'.repeat(64);},
    'wrong draft':v=>{v.draft_ref=actor;},'wrong revision':v=>{v.draft_revision++;},'wrong tool':v=>{v.tool_key='GrantRole';},
    'changed fields':v=>{v.canonical_fields.StudentFirstName.value='Different';},'expired proof':v=>{v.approval_expires_at='2000-01-01T00:00:00Z';},
    'unknown property':v=>{v.privateExtra='Never expose';}
  }))await check('withhold operation '+name,async()=>{const h=setup({change(route,v){if(route.endsWith('/core/operation'))change(v);}}),{channel}=await h.start();
    await a.rejects(channel.review());a.equal(h.calls.filter(x=>x.url.endsWith('/core/execute')).length,0);channel.dispose();});
  for(const status of[401,403,404,409,410,429,503])await check('challenge HTTP '+status+' is never retried',async()=>{
    const h=setup({statusAt:`/api/academies/${academy}/penta/onboarding/conversations/${conversation}/drafts/${draft}/core/challenge`,status}),{channel}=await h.start();
    await a.rejects(channel.review());await a.rejects(channel.review());a.equal(h.calls.filter(x=>x.url.endsWith('/core/challenge')).length,1);channel.dispose();});
  for(const[name,change]of Object.entries({
    'missing receipt':v=>{v.receipt_ref=null;},'missing audit':v=>{v.audit_ref=null;},'missing source':v=>{v.source_refs=[];},
    'duplicate source':v=>{v.source_refs=[student,student];},'foreign receipt scope':v=>{v.scope.actor_id=epoch;},'false receipt error':v=>{v.error_code='unknown';}
  }))await check('no success with '+name,async()=>{const h=setup({change(route,v){if(route.endsWith('/core/status')&&v.status==='succeeded')change(v);}}),{channel}=await h.start();
    const s=await channel.review();await channel.confirm(intent(s.projection));await a.rejects(channel.execute());channel.dispose();});
  await check('later catalogue drift withholds confirmation availability',async()=>{const h=setup({change(route,v,n){if(n>4&&route.endsWith('/current'))v.snapshot.tools[0].description='Changed';}}),{channel}=await h.start();await a.rejects(channel.review());channel.dispose();});
  await check('later pending conversation withholds actions',async()=>{const h=setup({change(route,v,n){if(n>4&&route.endsWith('/current'))v.pendingRequestId=epoch;}}),{channel}=await h.start();await a.rejects(channel.review());channel.dispose();});
  await check('delivered logout aborts all subsequent actions',async()=>{const h=setup(),{channel}=await h.start();const event=new Event('academydesk:portal-sign-out');event.detail='AcademyAdmin';h.window.dispatchEvent(event);await a.rejects(channel.review());a.equal(h.calls.length,4);channel.dispose();});
  await check('parallel execution submits only one command',async()=>{let release;const wait=new Promise(r=>{release=r;}),h=setup({wait}),{channel}=await h.start();
    const s=await channel.review();await channel.confirm(intent(s.projection));const first=channel.execute();await a.rejects(channel.execute());release();a.equal((await first).phase,'succeeded');
    a.equal(h.calls.filter(x=>x.url.endsWith('/core/execute')).length,1);channel.dispose();});
  await check('aborted confirmation dispatches nothing',async()=>{const h=setup(),{channel}=await h.start(),s=await channel.review();
    const abort=new AbortController();abort.abort();const before=h.calls.length;
    await a.rejects(channel.confirm(intent(s.projection),abort.signal));a.equal(h.calls.length,before);channel.dispose();});
  await check('late confirmation abort cannot grant creation',async()=>{const abort=new AbortController();const h=setup({onRequest(n){if(n===10)abort.abort();}}),{channel}=await h.start();
    const s=await channel.review();await a.rejects(channel.confirm(intent(s.projection),abort.signal));await a.rejects(channel.execute());
    a.equal(h.calls.filter(x=>x.url.endsWith('/core/execute')).length,0);channel.dispose();});
  await check('route change after review prevents confirmation',async()=>{const h=setup(),{channel}=await h.start(),s=await channel.review();
    const before=h.calls.length;h.window.location.pathname='/dashboard';await a.rejects(channel.confirm(intent(s.projection)));a.equal(h.calls.length,before);channel.dispose();});
  await check('credential replacement during review withholds controls',async()=>{const h=setup({onRequest(n,tools){if(n===6)tools.replaceToken();}}),{channel}=await h.start();
    await a.rejects(channel.review());a.equal(h.calls.filter(x=>x.url.endsWith('/core/confirm')).length,0);channel.dispose();});
  await check('lost confirmation reads status but never infers permission to execute',async()=>{const h=setup({lostConfirm:true}),{channel}=await h.start(),s=await channel.review();
    await a.rejects(channel.confirm(intent(s.projection)));a.equal((await channel.recover()).phase,'unknown');await a.rejects(channel.execute());
    a.equal(h.calls.filter(x=>x.url.endsWith('/core/confirm')).length,1);a.equal(h.calls.filter(x=>x.url.endsWith('/core/execute')).length,0);channel.dispose();});
  for(const[key,value]of Object.entries({request_ref:actor,approval_ref:actor,approval_expires_at:'2099-10-19T12:00:00Z'}))
    await check('approval refresh cannot replace pinned '+key,async()=>{const h=setup({change(route,v,n){if(n>9&&route.endsWith('/core/operation'))v[key]=value;}}),{channel}=await h.start(),s=await channel.review();
      await a.rejects(channel.confirm(intent(s.projection)));await a.rejects(channel.execute());a.equal(h.calls.filter(x=>x.url.endsWith('/core/execute')).length,0);channel.dispose();});
  for(const at of[5,6,7,8,9,10,11,12,13,14,15,16,17,18])await check('identical-token replacement withholds operation request '+at,async()=>{
    const h=setup({onRequest(n,tools){if(n===at)tools.replaceEpoch();}}),{channel}=await h.start();
    let failed=false;
    try {const review=await channel.review();await channel.confirm(intent(review.projection));await channel.execute();}catch{failed=true;}
    a.equal(failed,true);a.equal(h.calls.length,at);const before=h.calls.length;
    await a.rejects(channel.recover());await a.rejects(channel.execute());a.equal(h.calls.length,before);channel.dispose();
  });
  await check('confirmed operation cannot execute after missed same-token login replacement',async()=>{
    const h=setup(),{channel}=await h.start(),s=await channel.review();await channel.confirm(intent(s.projection));
    h.replaceEpoch();const count=h.calls.length;await a.rejects(channel.execute());a.equal(h.calls.length,count);channel.dispose();
  });
  const report={passed:checks.filter(x=>x.pass).length,failed:checks.filter(x=>!x.pass).length,checks,synthetic:true,sqlAcceptance:false,modelInference:false};
  if(process.env.QA_PENTA_OPERATION_OUTPUT){const output=path.resolve(process.env.QA_PENTA_OPERATION_OUTPUT);a.ok(!fs.existsSync(output));fs.mkdirSync(path.dirname(output),{recursive:true});fs.writeFileSync(output,JSON.stringify(report,null,2));}
  console.log(JSON.stringify({passed:report.passed,failed:report.failed}));if(report.failed)process.exitCode=1;
})().catch(error=>{console.error(error.message);process.exitCode=1;});
