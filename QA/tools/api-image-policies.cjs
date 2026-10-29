'use strict';
// Existing owner-approved FP1/FP2/FP3, verified on the shipped API rather than redefined.
const a=require('node:assert/strict');
function unchangedDenial(call,state,method,url,payload,status,message){const before=state(),response=call(method,url,payload,status);a.equal(response.message,message);a.deepEqual(state(),before,'Rejected operation must not change six financial/audit tables.');}
function validateStatement(stored,summary,teacher,evidence={}){
 const checks=evidence.checks={status:stored.Status==='NoCashDue',method:stored.PaymentMethod==='NoCashDue',paidAtNull:stored.PaidAtUtc===null,referenceNull:stored.Reference===null,gross:stored.GrossAmount===1000,deductions:stored.Deductions===1000,net:stored.NetAmount===0,workerId:typeof stored.TeacherIdSnapshot==='string'&&stored.TeacherIdSnapshot.toLowerCase()===teacher.toLowerCase(),recordedIdentity:!!stored.IdentityRecordedAtUtc,exactNet:summary.netAmountExact==='0.00',settlement:summary.settlementKind==='NoCashDue'};
 evidence.workerIdExactCaseMatches=stored.TeacherIdSnapshot===teacher;
 a.ok(Object.values(checks).every(Boolean),'NoCashDue invariant fields must all pass; field booleans retained, values withheld.');
}
function verifyPolicies({result,call,state,seedLegacy,payoutRow,paymentRow,invoiceRow}){
 const facts=result.policies={passed:false,noWriteGuards:0,approvedProductRules:['FP1','FP2','FP3'],accountingTaxPayrollReviewComplete:false};
 function denied(method,url,payload,status,message){unchangedDenial(call,state,method,url,payload,status,message);facts.noWriteGuards++;}
 const student=call('POST','students',{firstName:'Synthetic',lastName:'Policies'},201),teacher=call('POST','teachers',{firstName:'Synthetic',lastName:'Policies'},201),invoice=call('POST','invoices',{studentId:student.id,amount:1000},201);
 const restricted='Refunds and credit notes require dedicated reviewed workflows; submit only invoice reductions here.';
 for(const type of['Refund','CreditNote'])denied('POST','finance-adjustments',{invoiceId:invoice.id,type,amount:100,reason:'Synthetic policy'},400,restricted);
 for(const type of['Refund','CreditNote']){
  const id=seedLegacy(invoice.id,type);
  for(const approve of[true,false])denied('PATCH','finance-adjustments/'+id+'/approval',{approve,notes:'Synthetic reviewed disposition'},409,'Legacy refund/credit-note requests require reviewed disposition; no decision has been applied.');
 }
 const reduction=call('POST','finance-adjustments',{invoiceId:invoice.id,type:'Discount',amount:100,reason:'Explicit synthetic reduction'},200);
 call('PATCH','finance-adjustments/'+reduction.id+'/approval',{approve:true,notes:'Synthetic approved reduction'},200);
 a.deepEqual(invoiceRow(invoice.id),{TotalAmount:1000,AdjustedAmount:100,PaymentRows:0,Collected:0});facts.reductionWithoutSettlement=true;
 for(const model of['Monthly','SessionBlock']){
  const profile=call('POST','payroll/profiles',{workerType:'Teacher',teacherId:teacher.id,staffUserId:null,workerName:'Synthetic '+model,paymentModel:model,monthlyAmount:model==='Monthly'?1000:null,amountPerCycle:model==='SessionBlock'?1000:null,sessionsPerCycle:model==='SessionBlock'?4:null,effectiveFrom:'2026-10-01',isActive:true},201);
  const pay={payrollProfileId:profile.id,periodLabel:'QA-'+model,deductions:1000,paymentMethod:'BankTransfer',reference:null,paidAtUtc:null};
  for(const patch of[{reference:'Synthetic bank reference'},{paidAtUtc:'2026-10-01T00:00:00Z'}])denied('POST','payroll/payouts',{...pay,...patch},400,'No-cash-due statements cannot include a payment reference or paid-at date.');
  denied('POST','payroll/payouts',{...pay,deductions:1001},400,'Deductions cannot exceed the gross payout amount.');
  const summary=call('POST','payroll/payouts',pay,201);facts.statements??=[];const proof={model};facts.statements.push(proof);validateStatement(payoutRow(summary.id),summary,teacher.id,proof);
 }
 facts.noCashDueModels=2;
 const payment=call('POST','payments',{invoiceId:invoice.id,amount:100,method:'Offline'},201);
 call('PATCH','payments/'+payment.id+'/reconcile',{reference:'Original synthetic void evidence'},200);
 call('PATCH','payments/'+payment.id+'/status',{status:'Voided'},200);const before=paymentRow(payment.id);a.equal(before.Status,'Voided');a.ok(before.ReconciledAtUtc);a.equal(before.ReconciliationReference,'Original synthetic void evidence');
 for(const status of['Completed','Reconciled'])denied('PATCH','payments/'+payment.id+'/status',{status},400,'Voided payments cannot be restored. Record a new verified payment.');
 denied('PATCH','payments/'+payment.id+'/reconcile',{reference:'New synthetic evidence'},400,'Voided payments cannot be restored. Record a new verified payment.');
 a.deepEqual(paymentRow(payment.id),before);a.deepEqual(invoiceRow(invoice.id),{TotalAmount:1000,AdjustedAmount:100,PaymentRows:1,Collected:0});a.equal(facts.noWriteGuards,15);facts.voidedTerminal=true;facts.passed=true;
}
module.exports={unchangedDenial,validateStatement,verifyPolicies};
