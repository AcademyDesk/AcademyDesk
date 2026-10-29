'use strict';
// Real adapter/provenance code; controlled clock/events, no model or SQL claim.
const a=require('node:assert/strict'),{test}=require('node:test');
const{fixture,harness}=require('./penta-onboarding-review.test.cjs');
async function setup(){
 const f=fixture(),deadline=Date.parse(f.view.expiresAtUtc);let now=deadline-60000;
 class ClockDate extends Date{static now(){return now;}}
 const h=harness(f,{Date:ClockDate}),projection=await h.run(),timers=new Map();let sequence=0,expired=0;
 const focus=new EventTarget(),visibility=new EventTarget();
 const environment={now:()=>now,schedule:(callback,delay)=>{const id=++sequence;timers.set(id,{callback,delay});return id;},
  cancel:id=>timers.delete(id),focus,visibility};
 return {h,projection,deadline,timers,focus,visibility,environment,advance:value=>{now=value;},
  watch:()=>h.lib.watchOnboardingReviewExpiry(projection,()=>{expired++;},environment),expired:()=>expired,
  fire:()=>{const[id,timer]=timers.entries().next().value;timers.delete(id);timer.callback();}};
}
test('verified expiry is exact host lifetime and clone has no provenance',async()=>{
 const s=await setup();a.equal(s.h.lib.onboardingReviewExpiry(s.projection),s.deadline);
 a.throws(()=>s.h.lib.onboardingReviewExpiry(structuredClone(s.projection)));
});
test('changed projection cannot inherit the deadline',async()=>{
 const s=await setup();s.projection.messages[0].text='Changed';a.throws(()=>s.h.lib.onboardingReviewExpiry(s.projection));
});
test('binding is unavailable at the exact expiry boundary',async()=>{
 const s=await setup();s.advance(s.deadline);a.throws(()=>s.h.lib.verifiedOnboardingReviewBinding(s.projection));a.equal(s.h.calls.length,4);
});
test('presentation carries original expiry without extending it',async()=>{
 const s=await setup(),p=s.h.lib.presentOnboardingDialogue(s.projection,s.projection.messages,false);
 a.equal(s.h.lib.onboardingReviewExpiry(p),s.deadline);s.advance(s.deadline);
 a.throws(()=>s.h.lib.presentOnboardingDialogue(p,p.messages,false));a.equal(s.h.calls.length,4);
});
test('expiry during final HTTP read cannot adopt the projection',async()=>{
 const f=fixture();let now=Date.parse(f.view.expiresAtUtc)-1000;class ClockDate extends Date{static now(){return now;}}
 const h=harness(f,{Date:ClockDate,onRequest:async n=>{if(n===4)now=Date.parse(f.view.expiresAtUtc);}});
 await a.rejects(h.run());a.equal(h.calls.length,4);
});
test('idle deadline fires once and makes no network request',async()=>{
 const s=await setup();s.watch();a.equal(s.timers.size,1);s.advance(s.deadline);s.fire();
 a.equal(s.expired(),1);a.equal(s.timers.size,0);s.focus.dispatchEvent(new Event('focus'));a.equal(s.expired(),1);a.equal(s.h.calls.length,4);
});
for(const event of['focus','visibilitychange'])test(event+' catches a suspended timer without polling',async()=>{
 const s=await setup();s.watch();s.advance(s.deadline+1);
 (event==='focus'?s.focus:s.visibility).dispatchEvent(new Event(event));
 a.equal(s.expired(),1);a.equal(s.timers.size,0);a.equal(s.h.calls.length,4);
});
test('cleanup ignores even an already-queued old callback',async()=>{
 const s=await setup(),dispose=s.watch(),callback=s.timers.values().next().value.callback;dispose();s.advance(s.deadline);callback();
 s.focus.dispatchEvent(new Event('focus'));s.visibility.dispatchEvent(new Event('visibilitychange'));
 a.equal(s.expired(),0);a.equal(s.timers.size,0);
});
test('early timer re-arms for remaining lifetime and never refreshes it',async()=>{
 const s=await setup();s.watch();s.advance(s.deadline-1000);s.fire();
 a.equal(s.expired(),0);a.equal(s.timers.values().next().value.delay,1000);s.advance(s.deadline);s.fire();a.equal(s.expired(),1);
});
test('long deadlines do not overflow the browser timer',async()=>{
 const s=await setup();s.advance(0);const dispose=s.watch();a.equal(s.timers.values().next().value.delay,2147483647);dispose();
});
test('non-finite local clock fails closed without a request',async()=>{
 const s=await setup();s.advance(NaN);s.watch();a.equal(s.expired(),1);a.equal(s.timers.size,0);a.equal(s.h.calls.length,4);
});
