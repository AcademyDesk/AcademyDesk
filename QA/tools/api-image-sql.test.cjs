'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{decodeHttp,migrations,sqlSession,sqlOutputArgs,safeDockerFailure,encodedRequest}=require('./api-image-sql.cjs');
test('native negative admission transport encodes only GUID enrollment PUT, never arbitrary new write routes',()=>{const url='/api/academies/11111111-1111-4111-8111-111111111111/enrollments/22222222-2222-4222-8222-222222222222';a.ok(Buffer.from(encodedRequest('PUT',url,{status:'Active'},'synthetic'),'base64').toString().startsWith('PUT '+url+' HTTP/1.1\r\n'));for(const path of['/api/auth/roles',url+'/transfer',url.replace('22222222','not-guid'),'/api/path\r\nInjected'])a.throws(()=>encodedRequest('PUT',path,{}));a.throws(()=>encodedRequest('DELETE',url,{}));});
test('role audit transport admits only the exact academy/staff assignment GUID route',()=>{const url='/api/academies/11111111-1111-4111-8111-111111111111/roles/staff/22222222-2222-4222-8222-222222222222/assignment';a.ok(Buffer.from(encodedRequest('PUT',url,{roleId:'synthetic'},'synthetic'),'base64').toString().startsWith('PUT '+url+' HTTP/1.1\r\n'));for(const p of[url+'/extra',url.replace('/staff/','/users/'),url.replace('22222222','invalid'),'/api/auth/roles','/api/roles'])a.throws(()=>encodedRequest('PUT',p,{}));});
test('real HTTP parser supports content-length and chunked responses without body-code confusion',()=>{
 a.deepEqual(decodeHttp('HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\n[]'),{status:200,body:'[]'});
 a.deepEqual(decodeHttp('HTTP/1.1 403 Forbidden\r\nTransfer-Encoding: chunked\r\n\r\n2\r\n[]\r\n0\r\n\r\n'),{status:403,body:'[]'});
 a.deepEqual(decodeHttp('HTTP/1.1 401 Unauthorized\r\nContent-Length: 0\r\n\r\n'),{status:401,body:''});
 a.throws(()=>decodeHttp('body401'));a.throws(()=>decodeHttp('HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n9\r\n[]\r\n0\r\n\r\n'));
});
test('direct disposable fixtures set SQL options needed by filtered indexes without changing constraints',()=>{
 const text=sqlSession('SELECT 1;');for(const option of['ANSI_NULLS','QUOTED_IDENTIFIER','ANSI_PADDING','ANSI_WARNINGS','ARITHABORT','CONCAT_NULL_YIELDS_NULL'])a.ok(text.includes('SET '+option+' ON;'));
 a.ok(text.includes('SET NUMERIC_ROUNDABORT OFF;'));a.ok(text.endsWith('SELECT 1;\n'));a.equal(/DISABLE|NOCHECK/i.test(text),false);
});
test('full SQL JSON reads remove sqlcmd variable-text truncation without incompatible whitespace flag',()=>{
 a.deepEqual(sqlOutputArgs(false),['-W']);a.deepEqual(sqlOutputArgs(true),['-y','8000','-w','65535']);a.equal(sqlOutputArgs(true).includes('-W'),false);a.throws(()=>sqlOutputArgs('yes'));
 const full=JSON.stringify({synthetic:'a'.repeat(350)});a.throws(()=>JSON.parse(full.slice(0,256)));a.equal(JSON.parse(full).synthetic.length,350);
});
test('safe Docker diagnostics retain numeric error classification without SQL/credentials/arguments',()=>{
 const text=safeDockerFailure('exec',{status:1,stdout:'Msg 1934, Level 16, State 1\nSQL synthetic-private-sentinel',stderr:'Password=synthetic-private-sentinel'});
 a.ok(text.includes('sqlErrorNumbers=1934'));a.ok(text.includes('exit=1'));a.equal(text.includes('private-sentinel'),false);a.equal(text.includes('Password='),false);
});
test('migration inventory requires explicit known contexts and unique IDs',()=>{
 const source=(context,id)=>`[DbContext(typeof(${context}))][Migration("${id}")]`;
 const app=source('AcademyDeskDbContext','20260101000000_App'),identity=source('IdentityDbContext','20260101000001_Identity');
 a.deepEqual(migrations([app,identity,'snapshot without migration']),{AcademyDeskDbContext:['20260101000000_App'],IdentityDbContext:['20260101000001_Identity']});
 a.throws(()=>migrations([app,app,identity]));a.throws(()=>migrations([app,source('Unknown','20260101000001_Unknown')]));a.throws(()=>migrations([app]));
});
