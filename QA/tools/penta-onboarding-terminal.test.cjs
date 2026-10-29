'use strict';
// Additive terminal re-entry checks. Synthetic HTTP is not SQL/model acceptance.
const a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {fixture,harness,conversation,draft,academy,actor,epoch}=require('./penta-onboarding-review.test.cjs');
const approval='66666666-6666-4666-8666-666666666666',receiptId='77777777-7777-4777-8777-777777777777';
const audit='88888888-8888-4888-8888-888888888888',student='99999999-9999-4999-8999-999999999999';
const checks=[];
function setup(options={}) {
  const f=fixture(),ref=`onboarding:${draft.replaceAll('-','')}:1`;
  f.view.status='Succeeded';Object.assign(f.current.snapshot.draft,{status:'succeeded',operation_ref:ref});
  const operation={ref,request_ref:epoch,scope:structuredClone(f.current.snapshot.scope),tool_key:'AcademyDesk.StudentOnboarding.Create',
    tool_revision:1,draft_ref:draft,draft_revision:1,payload_hash:f.view.payloadHash,canonical_fields:structuredClone(f.current.snapshot.draft.fields),
    preview:'Previously committed synthetic operation',approval_ref:approval,approval_expires_at:'2000-01-01T00:00:00Z',status:'succeeded'};
  const receipt={operation_ref:ref,scope:structuredClone(operation.scope),payload_hash:f.view.payloadHash,draft_revision:1,status:'succeeded',
    receipt_ref:receiptId,audit_ref:audit,source_refs:[student],error_code:'none'};
  const h=harness(f,{onRequest:options.onRequest,async respond(n,url,init) {
    const route=new URL(url).pathname;let value;
    if(n<=4)value=n===1?f.account:n===3?f.view:f.current;
    else if(route.endsWith('/core/operation'))value=operation;
    else if(route.endsWith('/core/status'))value=receipt;
    else if(route==='/api/auth/session')value=f.account;
    else if(route.endsWith('/current'))value=f.current;
    else throw Error('Unexpected command on terminal re-entry');
    if(n>4)a.ok(init.method==='GET'||/\/core\/(operation|status)$/.test(route));
    value=structuredClone(value);if(options.change)options.change(route,value,n);
    if(options.wait&&n===5)await options.wait;
    if(options.lostRead&&n===5)throw Error('Synthetic lost saved-result read');
    return new Response(options.raw&&n===5?'{':JSON.stringify(value),{status:options.status&&n===5?options.status:200,headers:{'Content-Type':'application/json'}});
  }});
  return {...h,async start(){const p=await h.run(),lib=h.operation();return{p,lib,channel:lib.createOnboardingOperationChannel(p,h.request)};}};
}
async function check(name,run){try{await run();checks.push({name,pass:true});}catch(e){checks.push({name,pass:false,type:e.name});console.error(`FAIL ${name}: ${e.message}`);}}
(async()=>{
  await check('completed draft reload is passive; no receipt or success invented',async()=>{
    const h=setup(),{p,channel}=await h.start();a.equal(h.calls.length,4);a.ok(h.calls.every(x=>x.init.method==='GET'));
    a.equal(p.host.draft.status,'succeeded');a.equal(p.receipt,null);a.equal(p.operation,null);a.ok(Object.values(p.controls).every(x=>x===false));channel.dispose();
  });
  await check('explicit saved-result check verifies same completed operation and source; expired approval is not reused',async()=>{
    const h=setup(),{channel}=await h.start(),s=await channel.review();a.equal(s.phase,'succeeded');a.equal(s.studentId,student);
    a.equal(s.projection.receipt.audit_ref,audit);a.ok(Object.values(s.projection.controls).every(x=>x===false));a.equal(h.calls.length,8);
    a.deepEqual(h.calls.slice(4).map(x=>new URL(x.url).pathname.split('/').pop()),['operation','status','session','current']);
    a.ok(!h.calls.some(x=>/\/(challenge|confirm|execute|turns)$/.test(x.url)));channel.dispose();
  });
  await check('terminal channel never approves or executes before or after read',async()=>{
    const h=setup(),{channel}=await h.start();const s=await channel.review(),before=h.calls.length;
    await a.rejects(channel.confirm({kind:'confirm',operation_ref:s.projection.operation.ref,approval_ref:approval,revision:1,payload_hash:s.projection.operation.payload_hash}));
    await a.rejects(channel.execute());await a.rejects(channel.review());a.equal(h.calls.length,before);channel.dispose();
  });
  await check('copied completed projection is not verified provenance',async()=>{const h=setup(),{p,lib,channel}=await h.start();a.throws(()=>lib.createOnboardingOperationChannel(structuredClone(p),h.request));channel.dispose();});
  await check('terminal reload does not extend expired draft access',async()=>{const h=setup({change(route,v,n){if(n===3)v.expiresAtUtc='2000-01-01T00:00:00Z';}});await a.rejects(h.run());a.equal(h.calls.length,3);});
  await check('lost saved-result read recovers by same bound reads with no approval or execution',async()=>{
    const h=setup({lostRead:true}),{channel}=await h.start();await a.rejects(channel.review());a.equal((await channel.recover()).phase,'succeeded');
    a.equal(h.calls.filter(x=>x.url.endsWith('/core/operation')).length,2);a.ok(!h.calls.some(x=>/\/(challenge|confirm|execute|turns)$/.test(x.url)));channel.dispose();
  });
  for(const[name,change]of Object.entries({
    'missing receipt':v=>{v.receipt_ref=null;},'missing audit':v=>{v.audit_ref=null;},'missing source':v=>{v.source_refs=[];},
    'duplicate source':v=>{v.source_refs=[student,student];},'foreign tenant':v=>{v.scope.tenant_id=epoch;},'wrong digest':v=>{v.payload_hash='a'.repeat(64);},
    'wrong revision':v=>{v.draft_revision++;},'unknown receipt field':v=>{v.role='Owner';},'unknown error':v=>{v.error_code='unknown';}
  }))await check('terminal result withheld for '+name,async()=>{const h=setup({change(route,v){if(route.endsWith('/core/status'))change(v);}}),{channel}=await h.start();await a.rejects(channel.review());await a.rejects(channel.execute());channel.dispose();});
  for(const[name,change]of Object.entries({
    'regressed status':v=>{v.status='approved';},'foreign actor':v=>{v.scope.actor_id=epoch;},'changed canonical fields':v=>{v.canonical_fields.StudentFirstName.value='Changed';},
    'absent approval':v=>{v.approval_ref=null;v.approval_expires_at=null;},'wrong operation':v=>{v.ref='other';}
  }))await check('terminal operation withheld for '+name,async()=>{const h=setup({change(route,v){if(route.endsWith('/core/operation'))change(v);}}),{channel}=await h.start();await a.rejects(channel.review());channel.dispose();});
  for(const status of[401,403,404,409,410,429,503])await check('terminal HTTP '+status+' no retry or write fallback',async()=>{
    const h=setup({status}),{channel}=await h.start();await a.rejects(channel.review());await a.rejects(channel.review());a.equal(h.calls.length,5);channel.dispose();
  });
  await check('late token change hides saved result',async()=>{const h=setup({onRequest(n,tools){if(n===6)tools.replaceToken();}}),{channel}=await h.start();await a.rejects(channel.review());channel.dispose();});
  await check('navigation change hides saved result',async()=>{const h=setup({onRequest(n,tools){if(n===6)tools.window.location.pathname='/dashboard';}}),{channel}=await h.start();await a.rejects(channel.review());channel.dispose();});
  await check('delivered logout prevents any saved-result read',async()=>{const h=setup(),{channel}=await h.start();const e=new Event('academydesk:portal-sign-out');e.detail='AcademyAdmin';h.window.dispatchEvent(e);await a.rejects(channel.review());a.equal(h.calls.length,4);channel.dispose();});
  await check('changed current catalogue invalidates completed receipt',async()=>{const h=setup({change(route,v,n){if(n>4&&route.endsWith('/current'))v.snapshot.tools[0].description='Changed';}}),{channel}=await h.start();await a.rejects(channel.review());channel.dispose();});
  await check('parallel terminal checks admit one read sequence only',async()=>{let release;const wait=new Promise(r=>{release=r;}),h=setup({wait}),{channel}=await h.start();const first=channel.review();await a.rejects(channel.review());release();a.equal((await first).phase,'succeeded');a.equal(h.calls.length,8);channel.dispose();});
  await check('malformed saved response withheld',async()=>{const h=setup({raw:true}),{channel}=await h.start();await a.rejects(channel.review());channel.dispose();});
  const report={passed:checks.filter(x=>x.pass).length,failed:checks.filter(x=>!x.pass).length,checks,synthetic:true,sqlAcceptance:false,modelInference:false};
  if(process.env.QA_PENTA_TERMINAL_OUTPUT){const out=path.resolve(process.env.QA_PENTA_TERMINAL_OUTPUT);a.ok(!fs.existsSync(out));fs.mkdirSync(path.dirname(out),{recursive:true});fs.writeFileSync(out,JSON.stringify(report,null,2));}
  console.log(JSON.stringify({passed:report.passed,failed:report.failed}));if(report.failed)process.exitCode=1;
})().catch(e=>{console.error(e.message);process.exitCode=1;});
