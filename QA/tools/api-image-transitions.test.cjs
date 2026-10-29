'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{verifyPair,readSnapshot}=require('./api-image-transitions.cjs');
const reference='QA-native',first={id:'first',amount:600,status:'Reconciled',reconciliationReference:reference,reconciledAtUtc:'recorded'},second={id:'second',amount:200,status:'Completed'},adjustment={amount:200,status:'Approved',approvedAtUtc:'recorded',appliedAtUtc:'recorded'},options={adjusted:200,paymentId:'first',secondId:'second',reference};
const r=(status,message)=>({status,body:JSON.stringify({message})});
test('omitted nested SQL arrays require an independent zero count; hidden existing adjustments fail closed',()=>{
 const base={adjustmentRows:0,payments:[{amount:600}]};a.deepEqual(readSnapshot(JSON.stringify(base)),{value:{...base,adjustments:[]},emptyAdjustmentsOmitted:true});
 for(const patch of[{adjustmentRows:1},{adjustmentRows:undefined},{adjustments:null},{adjustmentRows:1,adjustments:[]},{adjustmentRows:0,adjustments:[{amount:200}]}])a.throws(()=>readSnapshot(JSON.stringify({...base,...patch})));
 a.equal(readSnapshot(JSON.stringify({...base,adjustmentRows:1,adjustments:[{amount:200}]})).emptyAdjustmentsOmitted,false);
});
test('approved reduction collection pair requires exact rejection and canonical two-row ledger',()=>{
 const responses=[r(201),r(400,'Payment exceeds the invoice balance.')],stored={total:1000,adjusted:200,status:'Paid',payments:[first,second],adjustments:[adjustment]};
 a.equal(verifyPair('adjusted-collection',responses,stored,options).guarded,true);
 for(const change of[{responses:[r(201),r(201)]},{responses:[r(201),r(429)]},{responses:[r(201),r(400,'wrong')]},{stored:{...stored,payments:[first,{...second,amount:400}]}},{stored:{...stored,status:'PartiallyPaid'}}])a.throws(()=>verifyPair('adjusted-collection',change.responses||responses,change.stored||stored,options));
});
test('approval/payment race permits only the two serializable outcomes with exact disposition',()=>{
 const approved={total:1000,adjusted:200,status:'PartiallyPaid',payments:[first],adjustments:[adjustment]},pending={total:1000,adjusted:0,status:'Paid',payments:[first,{...second,amount:400}],adjustments:[{...adjustment,status:'PendingApproval',approvedAtUtc:null,appliedAtUtc:null}]};
 a.equal(verifyPair('approval-payment',[r(200),r(400,'Payment exceeds the invoice balance.')],approved,options).balance,200);
 a.equal(verifyPair('approval-payment',[r(400,'Adjustment exceeds the remaining invoice balance.'),r(201)],pending,options).balance,0);
 a.throws(()=>verifyPair('approval-payment',[r(200),r(201)],approved,options));
 a.throws(()=>verifyPair('approval-payment',[r(400,'Adjustment exceeds the remaining invoice balance.'),r(201)],{...pending,adjustments:[adjustment]},options));
});
test('void pairs preserve reconciliation evidence and exact reopened/partially-paid state',()=>{
 const two={total:1000,adjusted:200,status:'Overdue',payments:[{...first,status:'Voided'},{...second,status:'Voided'}],adjustments:[adjustment]};
 a.equal(verifyPair('two-voids',[r(200),r(200)],two,options).collected,0);
 const unadjusted={...two,adjusted:0,adjustments:[],payments:[{...first,status:'Voided'},{...second,amount:400,status:'Voided'}]};a.equal(verifyPair('two-voids',[r(200),r(200)],unadjusted,{...options,adjusted:0}).balance,1000);
 const created={...two,status:'PartiallyPaid',payments:[{...first,status:'Voided'},second]};a.equal(verifyPair('create-void',[r(200),r(201)],created,options).balance,600);
 a.throws(()=>verifyPair('two-voids',[r(200),r(200)],{...two,payments:[{...first,status:'Voided',reconciliationReference:null},{...second,status:'Voided'}]},options));
 a.throws(()=>verifyPair('create-void',[r(200),r(201)],{...created,status:'Paid'},options));
});
test('reconcile/void races are terminal; evidence exists only if reconciliation won the lock',()=>{
 const stored={total:1000,adjusted:200,status:'Overdue',payments:[{...first,status:'Voided',amount:800}],adjustments:[adjustment]};
 a.equal(verifyPair('reconcile-void',[r(200),r(200)],stored,options).guarded,true);
 const voidFirst={...stored,payments:[{...stored.payments[0],reconciliationReference:null,reconciledAtUtc:null}]};a.equal(verifyPair('reconcile-void',[r(400,'Voided payments cannot be restored. Record a new verified payment.'),r(200)],voidFirst,options).guarded,true);
 a.throws(()=>verifyPair('reconcile-void',[r(400,'Voided payments cannot be restored. Record a new verified payment.'),r(200)],stored,options));
 a.throws(()=>verifyPair('reconcile-void',[r(200),r(200)],voidFirst,options));
});
