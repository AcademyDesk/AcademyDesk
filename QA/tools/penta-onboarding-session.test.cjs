'use strict';
// Actual host loaders/channels/provenance, synthetic responses and explicit epoch
// replacement without changing tokens or delivering a browser event.
const a=require('node:assert/strict'),{test}=require('node:test');
const {fixture,harness}=require('./penta-onboarding-review.test.cjs');
for(const n of[1,2,3,4])test('review rejects identical-token replacement during GET '+n,async()=>{
 const h=harness(fixture(),{onRequest:number=>{if(number===n)h.replaceEpoch();}});
 await a.rejects(h.run());a.equal(h.calls.length,n);
});
test('review provenance cannot cross explicit-login replacement',async()=>{
 const h=harness(),p=await h.run();h.replaceEpoch();a.throws(()=>h.lib.verifiedOnboardingReviewBinding(p));a.equal(h.calls.length,4);
});
test('presented dialogue cannot inherit old login provenance',async()=>{
 const h=harness(),p=await h.run(),next=h.lib.presentOnboardingDialogue(p,[],false);h.replaceEpoch();
 a.throws(()=>h.lib.verifiedOnboardingReviewBinding(next));a.throws(()=>h.lib.presentOnboardingDialogue(p,[],false));a.equal(h.calls.length,4);
});
test('detached dialogue construction rejects old reviewed projection',async()=>{
 const h=harness(),p=await h.run();h.replaceEpoch();a.throws(()=>h.dialogue().createOnboardingDialogue(p,new AbortController().signal,h.request));a.equal(h.calls.length,4);
});
test('existing dialogue refuses connect after missed replacement event',async()=>{
 const h=harness(),p=await h.run(),dialogue=h.dialogue().createOnboardingDialogue(p,new AbortController().signal,h.request);
 h.replaceEpoch();await a.rejects(dialogue.connect());a.equal(h.calls.length,4);dialogue.dispose();
});
for(const stage of['awaiting_approval','approved','succeeded'])test('operation construction rejects old '+stage+' provenance',async()=>{
 const f=fixture(),ref=`onboarding:${f.view.id.replaceAll('-','')}:1`;
 f.view.status={awaiting_approval:'Prepared',approved:'Authorized',succeeded:'Succeeded'}[stage];Object.assign(f.current.snapshot.draft,{status:stage,operation_ref:ref});
 const h=harness(f),p=await h.run();h.replaceEpoch();a.throws(()=>h.operation().createOnboardingOperationChannel(p,h.request));a.equal(h.calls.length,4);
});
for(const stage of['awaiting_approval','approved','succeeded'])test('constructed '+stage+' operation rejects missed replacement before review',async()=>{
 const f=fixture(),ref=`onboarding:${f.view.id.replaceAll('-','')}:1`;
 f.view.status={awaiting_approval:'Prepared',approved:'Authorized',succeeded:'Succeeded'}[stage];Object.assign(f.current.snapshot.draft,{status:stage,operation_ref:ref});
 const h=harness(f),p=await h.run(),channel=h.operation().createOnboardingOperationChannel(p,h.request);
 h.replaceEpoch();await a.rejects(channel.review());a.equal(h.calls.length,4);channel.dispose();
});
test('pending recovery handle rejects missed replacement before read',async()=>{
 const f=fixture();Object.assign(f.current,{status:'pending',snapshot:null,pendingRequestId:'66666666-6666-4666-8666-666666666666'});
 const h=harness(f);let failure;try{await h.run();}catch(error){failure=error;}a.equal(failure.reason,'pending');a.ok(failure.recovery);
 h.replaceEpoch();await a.rejects(failure.recovery.check());a.equal(h.calls.length,2);
});
test('epoch-less reviewed session cannot be adopted by a later explicit login',async()=>{
 const h=harness();h.setEpoch(null);const p=await h.run();h.replaceEpoch();a.throws(()=>h.lib.verifiedOnboardingReviewBinding(p));a.equal(h.calls.length,4);
});
test('same login retains provenance and permits independent fresh review',async()=>{
 const h=harness(),p=await h.run();a.ok(h.lib.verifiedOnboardingReviewBinding(p));a.ok(h.lib.presentOnboardingDialogue(p,[],false));a.equal(h.calls.length,4);
});
