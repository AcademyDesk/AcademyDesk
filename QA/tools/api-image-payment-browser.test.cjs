'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),{allowedApi,staticFile}=require('./api-image-payment-browser.cjs');
test('browser relay allows only real login/refresh and exact owned academy payment writes',()=>{
 a.equal(allowedApi('POST','/api/auth/login','owned'),true);a.equal(allowedApi('POST','/api/academies/owned/payments','owned'),true);
 a.equal(allowedApi('POST','/api/auth/login?useCookies=false','owned'),true);
 for(const url of['/api/auth/login?useCookies=true','/api/auth/login?useCookies=false&other=1','/api/auth/refresh?other=1'])a.equal(allowedApi('POST',url,'owned'),false);
 for(const [method,url] of[['POST','/api/academies/foreign/payments'],['DELETE','/api/academies/owned/payments'],['POST','/api/academies/owned/payroll/payouts'],['GET','https://external.invalid/api/records']])a.equal(allowedApi(method,url,'owned'),false);
});
test('static relay never follows an escape or serves arbitrary parent files',()=>{
 const root=path.resolve(__dirname,'../../apps/web/out');a.ok(fs.existsSync(root));a.throws(()=>staticFile(root,'/../../AGENTS.md'));a.throws(()=>staticFile(root,'/..\\AGENTS.md'));a.throws(()=>staticFile(root,'/%2e%2e/%2e%2e/AGENTS.md'));
});

test('explicit candidate selection cannot fall back to a stale original export when malformed',()=>{
 const previous=process.env.QA_WEB_CANDIDATE;try{process.env.QA_WEB_CANDIDATE='../invalid';a.throws(()=>require('./api-image-payment-browser.cjs').verifyExport());}finally{if(previous===undefined)delete process.env.QA_WEB_CANDIDATE;else process.env.QA_WEB_CANDIDATE=previous;}
});
