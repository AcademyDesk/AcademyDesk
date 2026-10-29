'use strict';
// Required-audit faults against run-owned SQL only. Baseline is never protection acceptance.
const a=require('node:assert/strict');
function rowsFromSnapshot(value){a.ok(value&&Number.isInteger(value.count)&&value.count>=0);a.ok(Array.isArray(value.rows));a.equal(value.rows.length,value.count);for(const r of value.rows){a.match(r.id,/^[0-9a-f-]{36}$/i);a.equal(typeof r.revoked,'boolean');}return value.rows;}
function proof(f,protectedExpected){
 a.equal(f.complete,true);a.equal(f.protectedExpected,protectedExpected);a.equal(f.faultRemoved,true);a.equal(f.sameTeacherBearer,true);a.equal(f.rolesUnchanged,true);a.equal(f.cases.length,2);
 a.deepEqual(f.cases.map(c=>c.action),['create','revoke']);
 for(const c of f.cases){a.equal(c.status,500);a.equal(c.auditDelta,0);a.equal(c.nonGrantTablesUnchanged,true);a.equal(c.allTablesUnchanged,protectedExpected);a.equal(c.teacherStatus,c.action==='create'?(protectedExpected?403:200):(protectedExpected?200:403));}
 a.equal(f.recoveryAudits,protectedExpected?2:3);a.equal(f.finalActiveGrants,0);a.equal(f.protectionAccepted,protectedExpected);a.equal(f.reproducedUnsafePersistence,!protectedExpected);
}
async function verify({result,protectedExpected,teacherId,owner,teacher,raw,snapshot,roles,state,setDenied,auditCount,pause,checkpoint=()=>{}}){
 const f=result.grantAudit={complete:false,protectedExpected,sameTeacherBearer:true,faultRemoved:false,cases:[],protectionAccepted:false};
 const beforeRoles=roles(),priorAudit=auditCount(),body={userId:teacherId,permissions:['students.manage'],isPermanent:false,expiresAtUtc:new Date(Date.now()+3600000).toISOString(),reason:'Synthetic required audit fault'};
 owner('GET','access-grants',undefined,403);require('./api-image-access-grant-authority.cjs').enableOwnedAccessGovernance(result,teacherId);await pause();teacher('GET','enrollments',undefined,403);
 async function fault(action,method,path,payload,teacherStatus){
  const before=snapshot(),audits=auditCount();let response;
  try{setDenied(true);f.faultRemoved=false;checkpoint();response=raw(method,path,payload);a.equal(response.status,500);}finally{setDenied(false);f.faultRemoved=true;checkpoint();}
  const after=snapshot(),unchanged=JSON.stringify(before)===JSON.stringify(after);a.equal(unchanged,protectedExpected);for(const table of Object.keys(before).filter(t=>t!=='AccessGrants'))a.equal(after[table],before[table]);a.equal(auditCount(),audits);a.deepEqual(roles(),beforeRoles);teacher('GET','enrollments',undefined,teacherStatus);f.cases.push({action,status:response.status,auditDelta:0,nonGrantTablesUnchanged:true,allTablesUnchanged:unchanged,teacherStatus});checkpoint();
 }
 await fault('create','POST','access-grants',body,protectedExpected?403:200);
 f.phase='created-grant row readback';checkpoint();let rows=state();a.equal(rows.length,protectedExpected?0:1);
 if(!protectedExpected){a.equal(rows[0].revoked,false);owner('PATCH','access-grants/'+rows[0].id+'/revoke',{},204);}
 f.phase='recovery create';checkpoint();const granted=owner('POST','access-grants',body,200);a.match(granted.id,/^[0-9a-f-]{36}$/i);teacher('GET','enrollments',undefined,200);
 await fault('revoke','PATCH','access-grants/'+granted.id+'/revoke',{},protectedExpected?200:403);
 rows=state();const saved=rows.find(r=>r.id.toLowerCase()===granted.id.toLowerCase());a.ok(saved,'Granted row must be found independent of SQL GUID letter casing.');a.equal(saved.revoked,!protectedExpected);
 owner('PATCH','access-grants/'+granted.id+'/revoke',{},204);teacher('GET','enrollments',undefined,403);
 rows=state();a.equal(rows.length,protectedExpected?1:2);a.ok(rows.every(r=>r.revoked));a.deepEqual(roles(),beforeRoles);
 Object.assign(f,{complete:true,rolesUnchanged:true,recoveryAudits:auditCount()-priorAudit,finalActiveGrants:0,protectionAccepted:protectedExpected,reproducedUnsafePersistence:!protectedExpected});proof(f,protectedExpected);checkpoint();
}
module.exports={proof,verify,rowsFromSnapshot};
