'use strict';
// Native-image successors to existing ApprovalRace/PaymentVoidRace assertions.
// No new finance rule or modification to the original SqlHarness regressions.
const a=require('node:assert/strict');
const paymentMessage='Payment exceeds the invoice balance.';
const approvalMessage='Adjustment exceeds the remaining invoice balance.';
const terminalMessage='Voided payments cannot be restored. Record a new verified payment.';
const groups=['adjusted-collection','approval-payment','two-voids','create-void','reconcile-void'];
function sameId(left,right){return typeof left==='string'&&typeof right==='string'&&left.toLowerCase()===right.toLowerCase();}
function readSnapshot(text){
 const value=JSON.parse(text);a.ok(Number.isInteger(value.adjustmentRows)&&value.adjustmentRows>=0);a.ok(Array.isArray(value.payments)&&value.payments.length>0);
 const emptyAdjustmentsOmitted=!Object.hasOwn(value,'adjustments');
 if(emptyAdjustmentsOmitted){a.equal(value.adjustmentRows,0,'Only an independently counted empty SQL result may omit its nested array.');value.adjustments=[];}
 a.ok(Array.isArray(value.adjustments));a.equal(value.adjustments.length,value.adjustmentRows,'Nested adjustments must match the independent SQL row count.');return{value,emptyAdjustmentsOmitted};
}
function verifyPair(group,responses,stored,{adjusted,paymentId,secondId,reference}){
 a.ok(groups.includes(group));a.equal(responses.length,2);a.equal(stored.total,1000);
 const codes=responses.map(row=>row.status);a.ok(codes.every(code=>[200,201,400].includes(code)),'429/5xx cannot satisfy a financial race.');
 const collected=stored.payments.filter(row=>['Completed','Reconciled'].includes(row.status)).reduce((sum,row)=>sum+row.amount,0);
 const current=stored.payments.find(row=>sameId(row.id,paymentId));a.ok(current);a.equal(collected<=stored.total-stored.adjusted,true);
 const rejection=(index,message)=>a.equal(JSON.parse(responses[index].body).message,message);
 if(group==='approval-payment'){
  a.equal(stored.adjustments.length,1);const item=stored.adjustments[0];a.equal(item.amount,200);
  if(codes[0]===200){a.deepEqual(codes,[200,400]);rejection(1,paymentMessage);a.equal(stored.adjusted,200);a.equal(item.status,'Approved');a.ok(item.approvedAtUtc&&item.appliedAtUtc);a.equal(stored.payments.length,1);a.equal(collected,600);a.equal(stored.status,'PartiallyPaid');}
  else{a.deepEqual(codes,[400,201]);rejection(0,approvalMessage);a.equal(stored.adjusted,0);a.equal(item.status,'PendingApproval');a.equal(item.approvedAtUtc,null);a.equal(item.appliedAtUtc,null);a.equal(stored.payments.length,2);a.equal(collected,1000);a.equal(stored.status,'Paid');}
  a.equal(current.status,'Reconciled');a.equal(current.amount,600);a.equal(current.reconciliationReference,reference);a.ok(current.reconciledAtUtc);
 }else{
  a.equal(stored.adjusted,adjusted);a.equal(stored.adjustments.length,adjusted?1:0);
  if(adjusted){const item=stored.adjustments[0];a.equal(item.amount,adjusted);a.equal(item.status,'Approved');a.ok(item.approvedAtUtc&&item.appliedAtUtc);}
  if(group==='adjusted-collection'){
   a.deepEqual([...codes].sort((x,y)=>x-y),[201,400]);rejection(codes.indexOf(400),paymentMessage);a.equal(stored.payments.length,2);a.equal(current.status,'Reconciled');a.equal(current.amount,600);a.equal(collected,800);a.equal(stored.status,'Paid');
   const next=stored.payments.find(row=>!sameId(row.id,paymentId));a.equal(next.status,'Completed');a.equal(next.amount,200);
  }else if(group==='two-voids'){
   a.deepEqual(codes,[200,200]);a.equal(stored.payments.length,2);a.equal(stored.payments.every(row=>row.status==='Voided'),true);a.equal(current.amount,600);const second=stored.payments.find(row=>sameId(row.id,secondId));a.ok(second);a.equal(second.amount,400-adjusted);a.equal(collected,0);a.equal(stored.status,'Overdue');
  }else if(group==='create-void'){
   a.deepEqual(codes,[200,201]);a.equal(stored.payments.length,2);a.equal(current.status,'Voided');a.equal(current.amount,600);const next=stored.payments.find(row=>!sameId(row.id,paymentId));a.equal(next.status,'Completed');a.equal(next.amount,400-adjusted);a.equal(collected,400-adjusted);a.equal(stored.status,'PartiallyPaid');
  }else{
   a.equal(codes[1],200);a.ok(codes[0]===200||codes[0]===400);a.equal(stored.payments.length,1);a.equal(current.status,'Voided');a.equal(current.amount,1000-adjusted);a.equal(collected,0);a.equal(stored.status,'Overdue');
   if(codes[0]===200){a.equal(current.reconciliationReference,reference);a.ok(current.reconciledAtUtc);}else{rejection(0,terminalMessage);a.equal(current.reconciliationReference,null);a.equal(current.reconciledAtUtc,null);}
  }
  if(group!=='reconcile-void'){a.equal(current.reconciliationReference,reference);a.ok(current.reconciledAtUtc);}
 }
 return{statuses:codes,collected,adjusted:stored.adjusted,balance:1000-stored.adjusted-collected,rows:stored.payments.length,status:stored.status,bothReadyBeforeRelease:true,guarded:true};
}
async function verifyTransitions({call,pair,snapshot,pause,result,checkpoint=()=>{},selectedGroups=groups}){
 a.ok(JSON.stringify(selectedGroups)===JSON.stringify(groups)||JSON.stringify(selectedGroups)===JSON.stringify(groups.slice(2)));
 const facts=result.transitions={passed:false,expectedGroups:[...selectedGroups],groups:[],originalHarnessUnchanged:false,policy:'Existing FP1/FP3 only',rateLimiterModified:false};
 const student=call('POST','students',{firstName:'Synthetic',lastName:'Transitions'},201);
 for(const group of selectedGroups){
  // Respect the real 120/minute limiter. No sleeping between pair members: both
  // clients must be ready before a shared barrier release. Slow only fixture groups.
  if(facts.groups.length)await pause(61000);
  const currentGroup={name:group,races:[]};facts.groups.push(currentGroup);
  for(let attempt=1;attempt<=5;attempt++){
   facts.currentCase={group,attempt,phase:'fixtures'};
   const adjusted=group==='adjusted-collection'?200:group==='approval-payment'?0:attempt%2===0?200:0;
   const invoice=call('POST','invoices',{studentId:student.id,amount:1000,dueDate:'2020-01-08'},201);
   let proposal;
   if(adjusted||group==='approval-payment'){
    proposal=call('POST','finance-adjustments',{invoiceId:invoice.id,type:'Discount',amount:200,reason:'Synthetic '+group},200);
    if(adjusted)call('PATCH','finance-adjustments/'+proposal.id+'/approval',{approve:true,notes:'Synthetic pre-approved reduction'},200);
   }
   const amount=group==='reconcile-void'?1000-adjusted:600,reference='QA-native-'+group;
   const first=call('POST','payments',{invoiceId:invoice.id,amount,method:'Offline'},201);
   if(group!=='reconcile-void')call('PATCH','payments/'+first.id+'/reconcile',{reference},200);
   let second;
   if(group==='two-voids')second=call('POST','payments',{invoiceId:invoice.id,amount:400-adjusted,method:'Offline'},201);
   const payment={method:'POST',relative:'payments',body:{invoiceId:invoice.id,amount:group==='adjusted-collection'?200:400-adjusted,method:'Offline'}};
   const voidFirst={method:'PATCH',relative:'payments/'+first.id+'/status',body:{status:'Voided'}};
   const requests=group==='adjusted-collection'?[payment,payment]:group==='approval-payment'?[{method:'PATCH',relative:'finance-adjustments/'+proposal.id+'/approval',body:{approve:true,notes:'Synthetic simultaneous decision'}},payment]:group==='two-voids'?[voidFirst,{method:'PATCH',relative:'payments/'+second.id+'/status',body:{status:'Voided'}}]:group==='create-void'?[voidFirst,payment]:[{method:'PATCH',relative:'payments/'+first.id+'/reconcile',body:{reference}},voidFirst];
   facts.currentCase.phase='barrier pair';const responses=await pair(requests,attempt,group);facts.currentCase.statuses=responses.map(row=>row.status);
   facts.currentCase.phase='SQL snapshot';const stored=snapshot(invoice.id);facts.currentCase.snapshot={total:stored.total,adjusted:stored.adjusted,status:stored.status,payments:stored.payments.map(row=>({amount:row.amount,status:row.status,referenceMatches:row.reconciliationReference===reference,hasReconciledAt:!!row.reconciledAtUtc,isFirst:sameId(row.id,first.id),isSecond:sameId(row.id,second?.id)})),adjustments:stored.adjustments.map(row=>({amount:row.amount,status:row.status,hasApproval:!!row.approvedAtUtc,hasApplied:!!row.appliedAtUtc}))};
   facts.currentCase.phase='pair assertions';const proof=verifyPair(group,responses,stored,{adjusted,paymentId:first.id,secondId:second?.id,reference});
   const view=call('GET','invoices',undefined,200).find(row=>sameId(row.id,invoice.id));a.ok(view);a.equal(view.paidAmount,proof.collected);a.equal(view.adjustedAmount,proof.adjusted);a.equal(view.balance,proof.balance);a.equal(view.status,proof.status);
   currentGroup.races.push({attempt,...proof});checkpoint();
  }
  a.equal(currentGroup.races.length,5);console.log(JSON.stringify({phase:'native-finance-transitions',group,guardedPairs:5}));
 }
 a.equal(facts.groups.length,selectedGroups.length);delete facts.currentCase;facts.totalPaymentRows=facts.groups.flatMap(group=>group.races).reduce((sum,row)=>sum+row.rows,0);facts.passed=true;
}
module.exports={groups,readSnapshot,verifyPair,verifyTransitions};
