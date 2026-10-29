'use strict';
// Additive exact-image proof; existing SqlHarness race and runner remain unchanged.
const a=require('node:assert/strict');
const message='Payment exceeds the invoice balance.';
function guardedPair(responses,snapshot){
 a.deepEqual(responses.map(row=>row.status).sort((x,y)=>x-y),[201,400],'Exactly one201 and one400;429 is never acceptable.');
 a.equal(JSON.parse(responses.find(row=>row.status===400).body).message,message);
 a.deepEqual(snapshot,{total:1000,adjusted:0,status:'Paid',rows:2,collected:1000,reconciled:1,completed:1});
}
async function verifyFinance({call,pair,snapshot,result}){
 const student=call('POST','students',{firstName:'Synthetic',lastName:'ImageRace',dateOfBirth:null,email:null,phone:null,branchId:null},201);
 const invoice=()=>call('POST','invoices',{studentId:student.id,feePlanId:null,amount:1000,dueDate:null},201);
 const pay=(id,amount)=>call('POST','payments',{invoiceId:id,amount,method:'Offline',reference:'QA-only'},201);
 const reconcile=id=>call('PATCH','payments/'+id+'/reconcile',{reference:'QA-image-reconciled'},200);
 const sequential=invoice(),first=pay(sequential.id,600);reconcile(first.id);
 const before=call('GET','invoices',undefined,200).find(row=>row.id===sequential.id);
 a.equal(before.paidAmount,600);a.equal(before.balance,400);
 a.equal(call('POST','payments',{invoiceId:sequential.id,amount:500},400).message,message);
 const after=snapshot(sequential.id);a.deepEqual(after,{total:1000,adjusted:0,status:'PartiallyPaid',rows:1,collected:600,reconciled:1,completed:0});
 result.finance={sequential:{invoice:1000,reconciled:600,balance:400,excess500Status:400,rows:1},races:[]};
 for(let attempt=1;attempt<=5;attempt++){
  const current=invoice(),initial=pay(current.id,600);reconcile(initial.id);
  const responses=await pair({invoiceId:current.id,amount:400,method:'Offline',reference:'QA-image-pair'},attempt),stored=snapshot(current.id);
  guardedPair(responses,stored);
  const ledger=call('GET','invoices',undefined,200).find(row=>row.id===current.id);a.equal(ledger.paidAmount,1000);a.equal(ledger.balance,0);a.equal(ledger.status,'Paid');
  result.finance.races.push({attempt,statuses:responses.map(row=>row.status).sort((x,y)=>x-y),rejectionMessage:true,bothReadyBeforeRelease:true,...stored});
 }
 a.equal(result.finance.races.length,5);
}
module.exports={guardedPair,verifyFinance};
