'use strict';
const a=require('node:assert/strict');
async function verify({result,guard,tenants,accounts,platform,teacherToken,studentToken,apiName,sqlName,sqlImageId,database,http,check,sql,docker,save}){
 const tenant=tenants[0],run=result.run,route='/api/academies/'+tenant.id+'/access-reviews',f=result.reviewProvenance={complete:false,guardExpected:guard,cases:[],faults:[],protectionAccepted:false};
 const owned=()=>{const d=JSON.parse(docker(['inspect',sqlName]))[0];a.equal(d.Config.Labels['academydesk.qa.run'],run);a.equal(d.Image,sqlImageId);a.equal(sql('SELECT DB_NAME();',database).trim(),database);};owned();
 const guid=x=>{a.match(x,/^[0-9a-f-]{36}$/i);return x.toLowerCase();},ownerId=guid(sql(`SELECT Id FROM AspNetUsers WHERE Email='${accounts[0].email}';`,database).trim()),platformId=guid(sql(`SELECT Id FROM AspNetUsers WHERE Email='platform-${run}@qa.invalid' AND IsPlatformOwner=1;`,database).trim());
 const tables=['AccessReviews','AdminWorkItems','AuditLogs','Courses','Batches','Students','Enrollments','Invoices','Payments','AspNetUsers','AspNetRoles','AspNetUserRoles','AccessGrants'];
 require('./native-sql-fingerprints.cjs').fingerprint(sql,database,tables,true);f.fingerprintsBatchSerialEquivalent=true;
 const snapshot=()=>require('./native-sql-fingerprints.cjs').fingerprint(sql,database,tables);
 const rows=(table,where='')=>JSON.parse(sql(`SELECT COALESCE((SELECT * FROM [${table}] ${where} ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]');`,database,true).trim());
 const total=()=>({reviews:rows('AccessReviews').length,tasks:rows('AdminWorkItems').length,audits:rows('AuditLogs',`WHERE Action IN ('POST AccessReviews','AccessReviewSignedOff')`).length});
 const checked=(label,method,path,body,bearer,status)=>check('review provenance '+label,http(apiName,method,path,body,bearer),status);
 let before=snapshot();checked('absent entitlement','GET',route,undefined,tenant.token,403);a.deepEqual(snapshot(),before);
 owned();sql(`UPDATE Academies SET EnabledModulesJson='["Core","Finance","FinanceControls","AccessGovernance"]' WHERE Id='${tenant.id}';`,database);
 before=snapshot();for(const[label,bearer]of[['foreign',tenants[1].token],['teacher',teacherToken],['student',studentToken],['anonymous',undefined]])checked(label+' signoff denied','POST',route,{notes:'Denied synthetic',createFollowUp:true},bearer,label==='anonymous'?401:403);a.deepEqual(snapshot(),before);f.refusalsNoWrite=true;
 const normal=(label,bearer,actorId,followUp)=>{
  const prior=total(),notes='Synthetic review '+label,spoof='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
  const receipt=checked(label,'POST',route,{notes,createFollowUp:followUp,reviewerUserId:spoof,academyId:tenants[1].id,id:spoof,followUpId:spoof},bearer,200),after=total();
  const review=rows('AccessReviews').find(r=>r.Notes===notes);a.ok(review);a.equal(review.AcademyId.toLowerCase(),tenant.id.toLowerCase());
  const task=rows('AdminWorkItems').find(t=>t.Description===notes);a.equal(Boolean(task),followUp);
  const domainAudit=rows('AuditLogs',`WHERE Action='AccessReviewSignedOff'`).find(r=>r.EntityId?.toLowerCase()===review.Id.toLowerCase());
  a.equal(after.reviews-prior.reviews,1);a.equal(after.tasks-prior.tasks,followUp?1:0);
  if(guard){a.equal(review.ReviewerUserId.toLowerCase(),actorId);a.equal(receipt.id.toLowerCase(),review.Id.toLowerCase());a.equal(receipt.reviewerUserId.toLowerCase(),actorId);a.equal(receipt.followUpId?.toLowerCase()??null,task?.Id.toLowerCase()??null);a.ok(domainAudit);a.equal(domainAudit.ActorUserId.toLowerCase(),actorId);const metadata=JSON.parse(domainAudit.MetadataJson);a.equal(metadata.ReviewId.toLowerCase(),review.Id.toLowerCase());a.equal(metadata.FollowUpId?.toLowerCase()??null,task?.Id.toLowerCase()??null);if(task){a.equal(task.EntityType,'AccessReview');a.equal(task.EntityId.toLowerCase(),review.Id.toLowerCase());a.equal(task.AssignedUserId,null);}a.equal(after.audits-prior.audits,2);}
  else{a.equal(review.ReviewerUserId,null);a.equal(domainAudit,undefined);if(task){a.equal(task.EntityId,null);a.equal(task.AssignedUserId,null);}a.equal(after.audits-prior.audits,label.startsWith('platform')?0:1);}
  f.cases.push({label,followUp,status:200,serverActorRecorded:guard,exactTaskCorrelation:guard,spoofIgnored:guard,successAudits:after.audits-prior.audits});save();
 };
 normal('owner-no-followup',tenant.token,ownerId,false);normal('owner-followup',tenant.token,ownerId,true);normal('platform-followup',platform,platformId,true);
 const trigger='qa_review_audit_'+run;
const setAuditFault=action=>{owned();a.ok(action===null||['POST AccessReviews','AccessReviewSignedOff'].includes(action));sql(action?`GO\nCREATE OR ALTER TRIGGER [${trigger}] ON dbo.AuditLogs AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted WHERE Action=N'${action}') THROW 51042, 'Synthetic required audit failure.', 1; END;\nGO`:`DROP TRIGGER IF EXISTS [${trigger}];`,database);};
 const fault=(label,bearer,action)=>{const before=snapshot(),prior=total();let response;try{setAuditFault(action);response=http(apiName,'POST',route,{notes:'Synthetic fault '+label,createFollowUp:true},bearer);a.equal(response.status,guard||!label.startsWith('platform')?500:200);}finally{setAuditFault(null);}const after=snapshot(),unchanged=JSON.stringify(after)===JSON.stringify(before);a.equal(unchanged,guard);for(const table of tables.filter(t=>!['AccessReviews','AdminWorkItems','AuditLogs'].includes(t)))a.equal(after[table],before[table]);const count=total();a.equal(count.reviews-prior.reviews,guard?0:1);a.equal(count.tasks-prior.tasks,guard?0:1);a.equal(count.audits-prior.audits,0);f.faults.push({label,status:response.status,allTablesUnchanged:unchanged,noSuccessAudit:true,unrelatedStateUnchanged:true});save();};
 fault('owner-generic-audit',tenant.token,'POST AccessReviews');fault('platform-generic-audit',platform,'POST AccessReviews');
 if(guard){fault('owner-domain-audit',tenant.token,'AccessReviewSignedOff');const prior=snapshot();try{owned();sql(`DENY INSERT ON OBJECT::dbo.AdminWorkItems TO [qa_${run}];`,database);a.equal(http(apiName,'POST',route,{notes:'Synthetic work fault',createFollowUp:true},tenant.token).status,500);}finally{owned();sql(`REVOKE INSERT ON OBJECT::dbo.AdminWorkItems FROM [qa_${run}];`,database);}a.deepEqual(snapshot(),prior);f.faults.push({label:'followup-insert',status:500,allTablesUnchanged:true,noSuccessAudit:true,unrelatedStateUnchanged:true});
  const stable=snapshot();checked('overlong notes','POST',route,{notes:'x'.repeat(4001),createFollowUp:true},tenant.token,400);a.deepEqual(snapshot(),stable);f.overlongNoWrite=true;
  normal('owner-recovery',tenant.token,ownerId,true);normal('platform-recovery',platform,platformId,false);
 }
 const listed=checked('tenant history','GET',route,undefined,tenant.token,200);a.equal(listed.length,total().reviews);a.ok(listed.every(r=>r.academyId.toLowerCase()===tenant.id.toLowerCase()));
 const foreign=checked('foreign history','GET','/api/academies/'+tenants[1].id+'/access-reviews',undefined,platform,200);a.deepEqual(foreign,[]);
 owned();a.equal(sql(`SELECT COUNT(*) FROM sys.triggers WHERE name='${trigger}';`,database).trim(),'0');
 a.equal(sql(`SELECT COUNT(*) FROM sys.database_permissions WHERE grantee_principal_id=DATABASE_PRINCIPAL_ID('qa_${run}') AND major_id=OBJECT_ID('dbo.AdminWorkItems') AND permission_name='INSERT' AND state_desc='DENY';`,database).trim(),'0');
 f.faultRemoved=true;f.complete=true;f.protectionAccepted=guard;f.missingProvenanceReproduced=!guard;f.auditRollbackReproduced=!guard;f.finalCounts=total();f.concurrentSignoffsTested=false;save();
}
module.exports={verify};
