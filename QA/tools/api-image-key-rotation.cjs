'use strict';
const a=require('node:assert/strict');
function tokenKeyId(token){
 a.ok(typeof token==='string'&&/^[A-Za-z0-9_-]+$/.test(token));const bytes=Buffer.from(token,'base64url');a.ok(bytes.length>20);a.equal(bytes.subarray(0,4).toString('hex'),'09f0c9f0');
 return[bytes.readUInt32LE(4).toString(16).padStart(8,'0'),bytes.readUInt16LE(8).toString(16).padStart(4,'0'),bytes.readUInt16LE(10).toString(16).padStart(4,'0'),bytes.subarray(12,14).toString('hex'),bytes.subarray(14,20).toString('hex')].join('-');
}
async function verifyRotation({result,call,rotate,replace,accounts,tenants,password}){
 const facts=result.keyRotation={passed:false,syntheticOnly:true,productionRotationApproved:false},email=accounts[1].email;
 const old=call('POST','/api/auth/login',{email,password},undefined,200,'rotation old generation login');facts.oldKeyId=tokenKeyId(old.accessToken);
 const created=await rotate();a.match(created,/^[0-9a-f-]{36}$/);a.notEqual(created,facts.oldKeyId);facts.newKeyId=created;
 await replace(true);
 call('GET','/api/auth/session',undefined,old.accessToken,200,'rotation retained certificate recovers old access');
 call('POST','/api/auth/refresh',{refreshToken:old.refreshToken},undefined,200,'rotation retained certificate recovers old refresh');
 const current=call('POST','/api/auth/login',{email,password},undefined,200,'rotation replacement writer login');a.equal(tokenKeyId(current.accessToken),created,'Replacement writer must actually encrypt with the new key.');a.equal(tokenKeyId(current.refreshToken),created);
 call('GET',`/api/academies/${tenants[0].id}/payments`,undefined,current.accessToken,403,'rotated key does not grant cross-tenant finance');
 await replace(false);
 call('GET','/api/auth/session',undefined,old.accessToken,401,'retired certificate rejects old access');
 call('POST','/api/auth/refresh',{refreshToken:old.refreshToken},undefined,401,'retired certificate rejects old refresh');
 call('GET','/api/auth/session',undefined,current.accessToken,200,'new certificate still decrypts current access');
 call('POST','/api/auth/refresh',{refreshToken:current.refreshToken},undefined,200,'new certificate still decrypts current refresh');
 await replace(true);
 call('GET','/api/auth/session',undefined,old.accessToken,200,'restored authorized decryption material recovers old generation');
 call('GET','/api/auth/session',undefined,tenants[0].token,403,'rotated and restored keys still deny deactivated original identity');
 facts.passed=true;
}
module.exports={tokenKeyId,verifyRotation};
