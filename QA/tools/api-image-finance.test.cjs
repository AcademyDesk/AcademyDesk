'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{guardedPair}=require('./api-image-finance.cjs');
const good=()=>[{status:201,body:'{}'},{status:400,body:'{"message":"Payment exceeds the invoice balance."}'}];
const snapshot=()=>({total:1000,adjusted:0,status:'Paid',rows:2,collected:1000,reconciled:1,completed:1});
test('exact-image race requires one201,one400,message,stored ledger and two rows',()=>{guardedPair(good(),snapshot());guardedPair(good().reverse(),snapshot());});
test('double successes,rate limiting,wrong messages and stored overcollection never pass',()=>{
 for(const statuses of[[201,201],[400,400],[201,429],[201,500]])a.throws(()=>guardedPair(statuses.map(status=>({status,body:good()[1].body})),snapshot()));
 a.throws(()=>guardedPair([{status:201,body:'{}'},{status:400,body:'{"message":"other"}'}],snapshot()));
 for(const patch of[{collected:1400},{rows:3},{status:'PartiallyPaid'},{reconciled:0},{completed:2},{adjusted:200}])a.throws(()=>guardedPair(good(),{...snapshot(),...patch}));
});
