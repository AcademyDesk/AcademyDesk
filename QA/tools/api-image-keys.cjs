'use strict';
// QA-only real HTTP key recovery; host authorization remains authoritative.
const a=require('node:assert/strict'),crypto=require('node:crypto');
function encryptedFiles(rows){
 a.ok(rows.length>0&&rows.length<=8,'Bounded nonempty key ring required.');
 return rows.map(({name,text})=>{
  a.match(name,/^key-[0-9a-f-]{36}\.xml$/i);a.ok(text.includes('<encryptedSecret ')&&text.includes('EncryptedXmlDecryptor')&&text.includes('<EncryptedData ')&&text.includes('<CipherValue>'),'Keys must contain certificate-encrypted secrets.');
  a.equal(/<masterKey\b[\s\S]*?<value>[^<]+<\/value>/.test(text),false,'Plaintext master key must not persist.');
  return{name,sha256:crypto.createHash('sha256').update(text).digest('hex')};
 });
}
async function verifyKeys({result,call,replace,inspectKeys,accounts,tenants,password,recoverBackup}){
 const valid=call('POST','/api/auth/login',{email:accounts[1].email,password},undefined,200,'key-ring login before replacement');
 a.ok(typeof valid.accessToken==='string'&&typeof valid.refreshToken==='string');
 const teacher=call('POST','/api/auth/login',{email:accounts[2].email,password},undefined,200,'key-ring restricted role login');
 const facts=result.keyRecovery={passed:false,phase:'encrypted-at-rest',syntheticOnly:true,productionStorageTested:false,rotationTested:false,sharedReplicaConcurrencyTested:false};
 facts.encryptedBefore=encryptedFiles(inspectKeys());
 facts.phase='replacement-same-environment';await replace('Production');
 const session=call('GET','/api/auth/session',undefined,valid.accessToken,200,'persisted old bearer recovered after replacement');a.equal(session.academyId,tenants[1].id);
 const refreshed=call('POST','/api/auth/refresh',{refreshToken:valid.refreshToken},undefined,200,'persisted old refresh ticket recovered');a.ok(refreshed.accessToken);
 call('GET','/api/auth/session',undefined,refreshed.accessToken,200,'refreshed bearer usable');
 call('GET',`/api/academies/${tenants[0].id}/payments`,undefined,valid.accessToken,403,'recovered owner cross-tenant denied');
 call('GET',`/api/academies/${tenants[0].id}/payments`,undefined,teacher.accessToken,403,'recovered teacher financial access denied');
 call('GET','/api/auth/session',undefined,tenants[0].token,403,'recovered bearer does not resurrect deactivated user');
 call('POST','/api/auth/login',{email:accounts[0].email,password},undefined,401,'deactivated user fresh login denied with durable keys');
 facts.phase='different-environment';await replace('Staging');
 call('GET','/api/auth/session',undefined,valid.accessToken,401,'wrong environment cannot decrypt original bearer');
 call('POST','/api/auth/refresh',{refreshToken:valid.refreshToken},undefined,401,'wrong environment cannot decrypt original refresh');
 facts.phase='return-original-environment';await replace('Production');
 call('GET','/api/auth/session',undefined,valid.accessToken,200,'original environment recovers bearer after negative environment probe');
 const after=encryptedFiles(inspectKeys());a.deepEqual(after,facts.encryptedBefore,'Replacement must not silently replace original encrypted keys.');facts.encryptedAfter=after;
 if(recoverBackup){
  facts.phase='separate-synthetic-key-backup';await recoverBackup();
  a.deepEqual(encryptedFiles(inspectKeys()),facts.encryptedBefore,'Restored encrypted key bytes must match original snapshot.');
  call('GET','/api/auth/session',undefined,valid.accessToken,200,'separate key backup recovers original bearer');
  call('POST','/api/auth/refresh',{refreshToken:valid.refreshToken},undefined,200,'separate key backup recovers original refresh');
  call('GET','/api/auth/session',undefined,tenants[0].token,403,'restored key backup does not restore revoked user authority');
  facts.separateSyntheticKeyBackupTested=true;
 }
 facts.passed=true;facts.phase='complete';
}
module.exports={encryptedFiles,verifyKeys};
