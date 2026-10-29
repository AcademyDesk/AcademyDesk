'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{allowed,waitForJourneyRead,verifyJourney}=require('./api-image-entry-browser.cjs'),{encoded}=require('./native-browser-transport.cjs');
test('entry/fallback relay permits real authentication and reads only; no domain action or model execution',()=>{
 for(const[method,url]of[['POST','/api/auth/login?useCookies=false'],['POST','/api/auth/refresh'],['GET','/api/auth/session'],['GET','/api/teacher/me']])a.equal(allowed(method,url),true);
 for(const[method,url]of[['POST','/api/academies/owned/payments'],['POST','/api/academies/owned/penta/chat/sessions'],['POST','/api/auth/login?useCookies=true'],['DELETE','/api/record'],['GET','https://external.invalid/api/records']])a.equal(allowed(method,url),false);
});
test('private native browser wire rejects header/path injection and oversized bodies',()=>{
 const wire=Buffer.from(encoded('POST','/api/auth/login?useCookies=false','{"synthetic":true}'), 'base64').toString('utf8');a.ok(wire.startsWith('POST /api/auth/login?useCookies=false HTTP/1.1\r\n'));a.ok(wire.includes('Content-Length: 18'));
 for(const args of[['DELETE','/api/records'],['GET','/api/records\r\nOther: injected'],['GET','/api/records',null,'Bearer token\r\nOther: injected'],['POST','/api/auth/refresh','x'.repeat(8192)]])a.throws(()=>encoded(...args));
});
test('each role/theme/viewport requires its own core read and both actual document themes',()=>{
 const valid=()=>({requests:[{method:'GET',path:'/api/teacher/me',status:200},{method:'GET',path:'/api/teacher/me',status:200}],requestStart:1,core:'/api/teacher/me',expectedTheme:'dark',aiTheme:'dark',manualTheme:'dark',errors:[]});
 a.equal(verifyJourney(valid()),1);
 for(const mutate of[f=>f.requestStart=2,f=>f.requestStart=-1,f=>f.requests[1].status=403,f=>f.requests[1].method='POST',f=>f.requests[1].path='/api/portal/me',f=>f.aiTheme='light',f=>f.manualTheme='light',f=>f.errors.push('uncaught page error'),f=>f.requests.push({method:'GET',path:'/api/other',status:429}),f=>f.requests.push({method:'GET',path:'/api/other',status:500})]){const f=valid();mutate(f);a.throws(()=>verifyJourney(f));}
});
test('real core evidence may arrive after a heading; missing, denied and stale reads fail within a fixed bound',async()=>{
 const requests=[{method:'GET',path:'/api/teacher/me',status:200}];let polls=0;
 await waitForJourneyRead({requests,requestStart:1,core:'/api/teacher/me',pause:async ms=>{a.equal(ms,100);if(++polls===3)requests.push({method:'GET',path:'/api/teacher/me',status:200});}});a.equal(polls,3);
 let expired=0;await a.rejects(waitForJourneyRead({requests:[{method:'GET',path:'/api/teacher/me',status:200},{method:'GET',path:'/api/teacher/me',status:403}],requestStart:1,core:'/api/teacher/me',pause:async()=>{expired++;}}));a.equal(expired,150);
});
