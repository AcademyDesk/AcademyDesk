'use strict';
// Real SQL permission denial at the existing required-audit boundary.
const a=require('node:assert/strict');
function failedAtomic(response,before,after){a.equal(response.status,500,'Audit failure must not report success.');a.deepEqual(after,before,'Audit failure must roll back all protected domain/Identity/audit tables.');}
function verifyAuditFaults({result,call,raw,snapshot,auditCount,setDenied,onboardingIdentity,validateOnboarding,checkpoint=()=>{}}){
 const facts=result.auditFaults={passed:false,syntheticOnly:true,cases:[],faultRemoved:false,applicationRulesChanged:false};
 const student=call('POST','students',{firstName:'Synthetic',lastName:'AuditBoundary'},201),teacher=call('POST','teachers',{firstName:'Synthetic',lastName:'AuditBoundary'},201),invoice=call('POST','invoices',{studentId:student.id,amount:1000,dueDate:'2020-01-08'},201);
 const payment=call('POST','payments',{invoiceId:invoice.id,amount:600,method:'Offline'},201);call('PATCH','payments/'+payment.id+'/reconcile',{reference:'Synthetic original audit evidence'},200);
 const adjustment=call('POST','finance-adjustments',{invoiceId:invoice.id,type:'Discount',amount:100,reason:'Synthetic audit boundary'},200);
 const profile=call('POST','payroll/profiles',{workerType:'Teacher',teacherId:teacher.id,workerName:'Synthetic AuditBoundary',paymentModel:'Monthly',monthlyAmount:1000,amountPerCycle:null,sessionsPerCycle:null,effectiveFrom:'2026-10-01',isActive:true},201);
 const operations=[
  {name:'payment-create',method:'POST',relative:'payments',payload:{invoiceId:invoice.id,amount:100,method:'Offline'},status:201,action:'POST Payments'},
  {name:'adjustment-approval',method:'PATCH',relative:'finance-adjustments/'+adjustment.id+'/approval',payload:{approve:true,notes:'Synthetic reviewed reduction'},status:200,action:'PATCH FinanceAdjustments'},
  {name:'payment-void',method:'PATCH',relative:'payments/'+payment.id+'/status',payload:{status:'Voided'},status:200,action:'PATCH Payments'},
  {name:'payroll-payout',method:'POST',relative:'payroll/payouts',payload:{payrollProfileId:profile.id,periodLabel:'Synthetic audit period',deductions:0,paymentMethod:'Offline'},status:201,action:'POST Payroll'},
  {name:'onboarding',method:'POST',relative:'student-onboarding',payload:{studentFirstName:'Synthetic',studentLastName:'AuditOnboarding',dateOfBirth:'2000-01-01',studentEmail:onboardingIdentity?.email,studentUserName:onboardingIdentity?.userName,studentTemporaryPassword:onboardingIdentity?.password},status:200,action:'POST StudentOnboarding'}
 ];
 const before=snapshot();facts.protectedTableCount=Object.keys(before).length;
 try{
  setDenied(true);facts.faultEnabled=true;checkpoint();
  for(const operation of operations){const state=snapshot(),response=raw(operation.method,operation.relative,operation.payload);failedAtomic(response,state,snapshot());facts.cases.push({name:operation.name,status:response.status,domainIdentityAuditUnchanged:true});checkpoint();}
 }finally{setDenied(false);facts.faultRemoved=true;checkpoint();}
 a.deepEqual(snapshot(),before);facts.allFaultsNoWrite=true;facts.recovery=[];
 for(const operation of operations){const prior=auditCount(operation.action),response=call(operation.method,operation.relative,operation.payload,operation.status);a.equal(auditCount(operation.action),prior+1,'Restored audit permission requires one real success audit.');if(operation.name==='onboarding'){a.equal(response.studentAccountCreated,true);a.equal(validateOnboarding(response.id),true);facts.recoveredStudentIdentityLinkVerified=true;}facts.recovery.push({name:operation.name,status:operation.status,successAudits:1});checkpoint();}
 a.equal(facts.cases.length,5);a.equal(facts.recovery.length,5);facts.passed=true;
}
module.exports={failedAtomic,verifyAuditFaults};
