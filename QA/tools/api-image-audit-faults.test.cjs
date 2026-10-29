'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{failedAtomic,verifyAuditFaults}=require('./api-image-audit-faults.cjs');
test('audit faults need a real500 and unchanged domain, Identity and audit state',()=>{
 const before={Students:'same-student-hash',Payments:'same-payment-hash',AspNetUsers:'same-identity-hash',AuditLogs:'same-audit-hash'};failedAtomic({status:500},before,{...before});
 for(const status of[200,201,400,403,429,503])a.throws(()=>failedAtomic({status},before,{...before}));for(const key of Object.keys(before))a.throws(()=>failedAtomic({status:500},before,{...before,[key]:'changed'}));
});
test('audit permission is restored even after an unexpected domain success; no recovery pass is fabricated',()=>{
 let denied=false;const toggles=[],result={};
 a.throws(()=>verifyAuditFaults({result,call:()=>({id:'synthetic'}),raw:()=>({status:201}),snapshot:()=>({table:'same'}),auditCount:()=>0,setDenied:value=>{denied=value;toggles.push(value);}}));
 a.equal(denied,false);a.deepEqual(toggles,[true,false]);a.equal(result.auditFaults.passed,false);a.equal(result.auditFaults.faultRemoved,true);a.equal(result.auditFaults.recovery,undefined);
});
test('five fault cases must complete before positive recovery; linked onboarding and one audit per recovery are mandatory',()=>{
 const result={},counts={},onboardingIdentity={userName:'synthetic-user',email:'synthetic@qa.invalid',password:'synthetic-private-sentinel'};let denied=false,links=0;
 const action=(method,url)=>method+' '+(url.startsWith('payments')?'Payments':url.startsWith('finance-adjustments')?'FinanceAdjustments':url.startsWith('payroll')?'Payroll':url==='student-onboarding'?'StudentOnboarding':url==='students'?'Students':'Teachers');
 verifyAuditFaults({result,onboardingIdentity,setDenied:value=>{denied=value;},snapshot:()=>({domain:'same',identity:'same',audit:'same'}),auditCount:key=>counts[key]||0,raw:()=>({status:denied?500:201}),validateOnboarding:id=>{links++;return id==='new-linked-student';},call:(method,url,payload)=>{a.equal(denied,false);const key=action(method,url);counts[key]=(counts[key]||0)+1;return url==='student-onboarding'?{id:'new-linked-student',studentAccountCreated:true}:{id:'fixture'};}});
 a.equal(denied,false);a.equal(links,1);a.equal(result.auditFaults.passed,true);a.equal(result.auditFaults.cases.length,5);a.equal(result.auditFaults.recovery.length,5);a.equal(result.auditFaults.recoveredStudentIdentityLinkVerified,true);a.equal(JSON.stringify(result).includes(onboardingIdentity.password),false);
});
