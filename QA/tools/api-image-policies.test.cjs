'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{unchangedDenial,validateStatement}=require('./api-image-policies.cjs');
test('financial rejection requires exact reason and unchanged SQL/audit state',()=>{
 unchangedDenial(()=>({message:'expected'}),()=>({hash:'same'}),'POST','synthetic',{},400,'expected');
 a.throws(()=>unchangedDenial(()=>({message:'other'}),()=>({hash:'same'}),'POST','synthetic',{},400,'expected'));
 let count=0;a.throws(()=>unchangedDenial(()=>({message:'expected'}),()=>({hash:count++}),'POST','synthetic',{},400,'expected'));
});
test('zero-net is a snapshot-owned NoCashDue statement, never a bank payout',()=>{
 const stored={Status:'NoCashDue',PaymentMethod:'NoCashDue',PaidAtUtc:null,Reference:null,GrossAmount:1000,Deductions:1000,NetAmount:0,TeacherIdSnapshot:'synthetic-teacher',IdentityRecordedAtUtc:'2026-10-01T00:00:00Z'},summary={netAmountExact:'0.00',settlementKind:'NoCashDue'};
 validateStatement(stored,summary,'synthetic-teacher');
 validateStatement({...stored,TeacherIdSnapshot:'SYNTHETIC-TEACHER'},summary,'synthetic-teacher');
 for(const patch of[{Status:'Paid'},{PaymentMethod:'BankTransfer'},{PaidAtUtc:'2026-10-01T00:00:00Z'},{Reference:'bank'},{NetAmount:1},{TeacherIdSnapshot:'other'},{IdentityRecordedAtUtc:null}])a.throws(()=>validateStatement({...stored,...patch},summary,'synthetic-teacher'));
});
