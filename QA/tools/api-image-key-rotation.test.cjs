'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{tokenKeyId,verifyRotation}=require('./api-image-key-rotation.cjs');
function token(id){const b=Buffer.alloc(25);b.write('09f0c9f0',0,'hex');b.writeUInt32LE(id,4);return b.toString('base64url');}
test('actual opaque ticket key ID requires DP magic and GUID byte order, not substring inference',()=>{
 a.equal(tokenKeyId(token(0x12345678)),'12345678-0000-0000-0000-000000000000');a.throws(()=>tokenKeyId('jwt.like.token'));a.throws(()=>tokenKeyId(Buffer.alloc(25).toString('base64url')));
});
test('rotation requires actual new writer key, retained generation, retirement denial and recovery',async()=>{
 const result={},replacements=[],calls=[];let rotated=false;
 const f={result,accounts:[{email:'a'},{email:'b'}],tenants:[{id:'a'}],password:'synthetic',rotate:async()=>{rotated=true;return'00000002-0000-0000-0000-000000000000';},replace:async keep=>{replacements.push(keep);},call:(method,url,payload,bearer,status,label)=>{calls.push({status,label});return{accessToken:token(rotated?2:1),refreshToken:token(rotated?2:1)};}};
 await verifyRotation(f);a.equal(result.keyRotation.passed,true);a.deepEqual(replacements,[true,false,true]);a.equal(calls.length,11);a.equal(calls.at(-1).status,403);
 const failed={...f,result:{},call:()=>({accessToken:token(1),refreshToken:token(1)})};await a.rejects(verifyRotation(failed));a.equal(failed.result.keyRotation.passed,false);
});
test('read-only candidate receives only three hash-verified maintenance artifacts through owned writable tmpfs',()=>{
 const fs=require('node:fs'),path=require('node:path'),source=fs.readFileSync(path.join(__dirname,'api-image-sql.cjs'),'utf8');
 a.ok(source.includes("['KeyMaintenance.dll','KeyMaintenance.deps.json','KeyMaintenance.runtimeconfig.json'].includes(file.file)"));
 a.ok(source.includes("base64 -d > /tmp/qa-key-maintenance/"));a.equal(source.includes("docker(['cp'"),false);
 const program=fs.readFileSync(path.join(__dirname,'KeyMaintenance/Program.cs'),'utf8');a.ok(program.includes('File.ReadAllText("/qa-private/owner") != run'));a.ok(program.includes('certificate.Subject != "CN=AcademyDesk-QA-" + run'));a.ok(program.includes('.ProtectKeysWithCertificate(certificate)'));a.equal(/UseSqlServer|HttpClient|Process\.Start/.test(program),false);
});
