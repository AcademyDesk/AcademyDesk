'use strict';
// Actual read/recovery adapters and vendor validator with synthetic transport.
// Separate SQL/browser runner is penta-onboarding-expiry-live.cjs.
const a=require('node:assert/strict'),{test}=require('node:test');
const{fixture,harness,conversation,draft,epoch}=require('./penta-onboarding-review.test.cjs');
const json=(value,status=200)=>new Response(JSON.stringify(value),{status,headers:{'Content-Type':'application/json'}});
for(const stage of[2,3,4])test('expiry410 at initial read stage '+stage+' withholds projection and never retries',async()=>{
 const h=harness(fixture(),{statusAt:stage,status:410});let error;try{await h.run();}catch(e){error=e;}
 a.equal(error?.reason,'changed');a.equal(error.recovery,undefined);a.equal(h.calls.length,stage);a.ok(h.calls.every(x=>x.init.method==='GET'));
});
for(const expiresAtUtc of['2000-01-01T00:00:00Z','2000-01-01T05:30:00+05:30','invalid','2099-10-18T12:00:00'])test('unsafe local expiry '+expiresAtUtc+' cannot expose a draft despite HTTP200',async()=>{
 const f=fixture();f.view.expiresAtUtc=expiresAtUtc;const h=harness(f);await a.rejects(h.run());a.equal(h.calls.length,3);a.ok(h.calls.every(x=>x.init.method==='GET'));
});
for(const stage of[3,4])test('captured pending handle encounters expiry at stage '+stage+' and never resubmits',async()=>{
 const f=fixture(),pending={...f.current,version:2,status:'pending',pendingRequestId:epoch,snapshot:null};
 const h=harness(f,{respond:async(n)=>json(n===1?f.account:pending,n===stage?410:200)});
 let error;try{await h.run();}catch(e){error=e;}a.equal(error?.reason,'pending');a.ok(error.recovery);a.equal(h.calls.length,2);
 let expired;try{await error.recovery.check();}catch(e){expired=e;}
 a.equal(expired?.reason,'changed');a.equal(expired.recovery,undefined);a.equal(h.calls.length,stage);a.ok(h.calls.every(x=>x.init.method==='GET'));
 a.ok(h.calls.every(x=>!x.url.endsWith('/turns')&&!x.url.endsWith('/claims')));
});
test('existing selected projection does not authorize a later expired reload',async()=>{
 const f=fixture(),first=harness(f),projection=await first.run();a.equal(projection.host.draft.ref,draft);
 const next=harness(f,{statusAt:2,status:410});await a.rejects(next.run());a.equal(next.calls.length,2);
 a.ok(next.calls[1].url.includes('/'+conversation+'/current'));a.ok(next.calls.every(x=>x.init.method==='GET'));
});
