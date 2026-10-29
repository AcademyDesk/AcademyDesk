'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{encryptedFiles,verifyKeys}=require('./api-image-keys.cjs');
const key={name:'key-aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa.xml',text:'<key><encryptedSecret decryptorType="EncryptedXmlDecryptor"><EncryptedData Type="x"><CipherValue>synthetic-encrypted-bytes</CipherValue></EncryptedData></encryptedSecret></key>'};
test('encrypted manifest retains only bounded file names and hashes, rejects plaintext/unprotected keys',()=>{
 const rows=encryptedFiles([key]);a.equal(rows.length,1);a.match(rows[0].sha256,/^[0-9a-f]{64}$/);a.equal(JSON.stringify(rows).includes('synthetic-encrypted-bytes'),false);
 for(const text of['<key><masterKey><value>private-sentinel</value></masterKey></key>',key.text+'<masterKey><value>secret</value></masterKey>'])a.throws(()=>encryptedFiles([{...key,text}]));
 a.throws(()=>encryptedFiles([]));a.throws(()=>encryptedFiles([{...key,name:'../key.xml'}]));
});
test('key recovery requires HTTP session/refresh recovery and current revocation, role, tenant, environment denials',async()=>{
 const phases=[],calls=[],result={},accounts=['a','b','teacher'].map(email=>({email})),tenants=[{id:'tenant-a',token:'old-deactivated'},{id:'tenant-b'}];
 await verifyKeys({result,accounts,tenants,password:'synthetic',inspectKeys:()=>[key],replace:async environment=>{phases.push(environment);},call:(method,url,body,bearer,status,label)=>{calls.push({status,label});if(url==='/api/auth/login'||url==='/api/auth/refresh')return{accessToken:'synthetic',refreshToken:'synthetic'};return{academyId:'tenant-b'};}});
 a.deepEqual(phases,['Production','Staging','Production']);a.equal(calls.length,12);a.equal(result.keyRecovery.passed,true);a.ok(calls.some(row=>row.label.includes('deactivated')&&row.status===403));a.ok(calls.some(row=>row.label.includes('wrong environment')&&row.status===401));a.equal(JSON.stringify(result).includes('old-deactivated'),false);
});
test('missing encrypted material or failed real HTTP callback never accepts key recovery',async()=>{
 const f={result:{},accounts:[{email:'a'},{email:'b'},{email:'t'}],tenants:[{id:'a'},{id:'b'}],password:'synthetic',call:()=>({accessToken:'a',refreshToken:'r'}),replace:async()=>{},inspectKeys:()=>[]};
 await a.rejects(verifyKeys(f));a.equal(f.result.keyRecovery.passed,false);
 f.inspectKeys=()=>[key];f.call=()=>{throw Error('HTTP assertion failed');};await a.rejects(verifyKeys(f));a.equal(f.result.keyRecovery.passed,false);
});
test('separate synthetic key backup must recover tokens without resurrecting deactivated access',async()=>{
 const result={},calls=[];let restored=false;
 const f={result,accounts:[{email:'a'},{email:'b'},{email:'t'}],tenants:[{id:'a'},{id:'b'}],password:'synthetic',call:(method,url,body,bearer,status,label)=>{calls.push({status,label});return{accessToken:'a',refreshToken:'r',academyId:'b'};},replace:async()=>{},inspectKeys:()=>[key],recoverBackup:async()=>{restored=true;}};
 await verifyKeys(f);a.equal(restored,true);a.equal(result.keyRecovery.separateSyntheticKeyBackupTested,true);a.equal(calls.length,15);a.equal(calls.at(-1).status,403);
 f.recoverBackup=async()=>{throw Error('owned key copy failed');};await a.rejects(verifyKeys(f));a.equal(result.keyRecovery.passed,false);
});
