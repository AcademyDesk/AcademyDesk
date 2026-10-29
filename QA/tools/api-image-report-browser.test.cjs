'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{allowed,verifyLedger,verifyCsv,qualifyReportProof}=require('./api-image-report-browser.cjs');
test('native reports relay admits real login/refresh and reads, never financial or arbitrary writes',()=>{
 a.equal(allowed('POST','/api/auth/login?useCookies=false'),true);a.equal(allowed('GET','/api/academies/owned/invoices'),true);
 for(const[method,url]of[['POST','/api/academies/owned/payments'],['PATCH','/api/academies/owned/invoices'],['DELETE','/api/academies/owned'],['GET','/api/valid\r\nInjected: value'],['POST','/api/auth/login?other=true']])a.equal(allowed(method,url),false);
});
test('Reports qualification requires every current native journey and its own non-reused reads, exact CSV and unchanged domain',()=>{
 const academyId='11111111-1111-4111-8111-111111111111',web={build:'candidate',files:627},requests=[{method:'POST',path:'/api/auth/login',status:200}],cases=[];
 for(const width of[390,1440])for(const theme of['light','dark']){const requestStart=requests.length;for(const endpoint of['students','teachers','batches','enrollments','sessions','invoices','payments','expenses','payroll/payouts'])requests.push({method:'GET',path:`/api/academies/${academyId}/${endpoint}`,status:200});cases.push({width,theme,collected:700,balance:200,ownCoreReads:9,pageErrors:0,reconciledIncluded:true,voidedExcluded:true,noCashDueExcluded:true,currenciesSeparated:true,csvExact:true,csvFormulaTextNeutralized:true,noHorizontalOverflow:true,csvSha256:'a'.repeat(64),requestStart,requestEnd:requests.length});}
 const good={passed:true,domainStateUnchanged:true,phase:'complete',apiResponseMocks:false,tokenInjection:false,modelActivated:false,physicalDevicesTested:false,exportBuild:'candidate',verifiedExportFiles:627,academyId,invoiceNumber:'QA-INVOICE',requests,cases,domainTableHashes:Object.fromEntries(['Students','Teachers','Invoices','Payments','FinanceAdjustments','PayrollProfiles','PayrollPayouts','Expenses','AuditLogs'].map(name=>[name,'B'.repeat(64)]))};a.equal(qualifyReportProof(good,web),true);
 for(const mutate of[p=>p.apiResponseMocks=true,p=>p.exportBuild='stale',p=>p.domainStateUnchanged=false,p=>p.cases[1].theme='light',p=>p.cases[0].collected=100,p=>p.cases[0].csvExact=false,p=>p.cases[0].noCashDueExcluded=false,p=>p.requests[1].status=403,p=>p.requests[1].status=429,p=>p.requests.push({method:'POST',path:'/api/academies/owned/payments',status:201}),p=>{p.cases[1].requestStart=p.cases[0].requestStart;p.cases[1].requestEnd=p.cases[0].requestEnd;},p=>delete p.domainTableHashes.AuditLogs]){const proof=structuredClone(good);mutate(proof);a.throws(()=>qualifyReportProof(proof,web));}
});
test('report source-of-truth requires reconciled inclusion, terminal void exclusion and exact NoCashDue/expense state',()=>{
 const good={total:1000,adjusted:100,collected:700,balance:200,paymentRows:3,voided:1,reconciled:1,noCashRows:1,noCashTotal:0,expenses:2};verifyLedger(good);
 for(const patch of[{collected:100},{collected:750},{balance:300},{paymentRows:4},{noCashRows:0},{noCashTotal:1}])a.throws(()=>verifyLedger({...good,...patch}));
});
test('actual invoice CSV must keep all canonical amounts, neutralized formula text and no foreign row',()=>{
 const csv='"Invoice","Student","Currency","Gross","Approved reductions","Collected","Outstanding","Due date"\r\n"QA-INVOICE","\t=1+1 NativeReport","INR","1000.00","100.00","700.00","200.00","2026-10-01"\r\n';verifyCsv(csv,'QA-INVOICE');
 for(const changed of[csv.replace('\t=1+1','=1+1'),csv.replace('"700.00"','"100.00"'),csv+'OtherTenantNativeReport',csv+'extra\r\n'])a.throws(()=>verifyCsv(changed,'QA-INVOICE'));
});
