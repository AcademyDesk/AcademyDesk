'use strict';
// Real host rules only. Owner23Oct approved atomic source-course completion
// credit for promotions and rejection of duplicate Active/Waitlisted admissions.
const a=require('node:assert/strict');
function denied(response,before,after,message){a.equal(response.status,409);a.equal(JSON.parse(response.body).message,message);a.deepEqual(after,before);}
function raceState(group,statuses,s,audits){
 a.equal(audits,1);
 if(group==='waitlist-create'){a.deepEqual([...statuses].sort(),[201,409]);a.equal(s.targetRows,1);a.equal(s.waitlisted,1);a.equal(s.active,0);}
 else {a.deepEqual(statuses,[409,200]);a.equal(s.targetRows,1);a.equal(s.waitlisted,0);a.equal(s.active,1);if(group==='transfer')a.equal(s.sourceStatus,'Active');if(group==='promotion'){a.equal(s.sourceStatus,'Active');a.equal(s.promotionStatus,'Pending');}}
}
const prerequisiteMessage='The learner has not completed all course prerequisites for this batch.',duplicateMessage='The student already has an active or waitlisted enrollment in this batch. Activate the existing waitlist entry instead of creating another enrollment.';
async function verifyRules({result,call,raw,race,state,auditCount,snapshot,setDenied,pause,checkpoint=()=>{}}){
 const f=result.admissionRules={passed:false,policy:'owner-approved-23oct-source-credit-and-no-duplicate-waitlist',cases:[],races:[],rollbacks:[]};let serial=0;
 let chunk=0;
 // At most four bounded synthetic fixtures per interval (<80 HTTP requests),
 // below the unchanged real120/min limiter. Never retry429 as a passing case.
 async function separate(label){f.phase=label;checkpoint();if(chunk++%4===0)await pause();}
 const course=name=>call('POST','courses',{name:'Synthetic rules '+name+' '+(++serial),academyType:'Music'},201);
 const student=()=>call('POST','students',{firstName:'SyntheticRules'+(++serial),lastName:'Native'},201);
 const batch=(c,waitlistCapacity=4)=>call('POST','batches',{name:'Synthetic rules batch '+(++serial),courseId:c.id,capacity:8,waitlistCapacity,deliveryMode:'InPerson',enrollmentStatus:'Open'},201);
 const enroll=(s,b,status='Active')=>call('POST','enrollments',{studentId:s.id,batchId:b.id,status},201);
 const complete=e=>call('PUT','enrollments/'+e.id,{status:'Completed',endDate:'2026-10-23',lifecycleReason:'Synthetic prerequisite completion'},200);
 function pending(s,source,target){call('POST','batch-promotions',{studentId:s.id,sourceBatchId:source.id,targetBatchId:target.id,effectiveDate:'2026-10-23'},200);const p=call('GET','batch-promotions',undefined,200).filter(x=>x.studentId===s.id&&x.targetBatchId===target.id&&x.status==='Pending');a.equal(p.length,1);return p[0];}
 function setup(path,{waiting=false,existing=null,required=false,otherRequired=false,otherCompleted=false,history=false}={}){
  const c=course('source'),targetCourse=course('target'),s=student(),source=batch(c),target=batch(targetCourse),sourceRow=enroll(s,source);let targetRow,promotion;
  if(history){const old=enroll(s,target);complete(old);}
  if(waiting||path==='activate')targetRow=enroll(s,target,'Waitlisted');
  if(existing)enroll(s,target,existing);
  if(required)call('POST','academic-governance/prerequisites',{courseId:targetCourse.id,requiredCourseId:c.id},200);
  if(otherRequired){const extra=course('other-required');if(otherCompleted)complete(enroll(s,batch(extra)));call('POST','academic-governance/prerequisites',{courseId:targetCourse.id,requiredCourseId:extra.id},200);}
  let request;
  if(path==='create'||path==='waitlist-create')request={method:'POST',relative:'enrollments',body:{studentId:s.id,batchId:target.id,status:path==='waitlist-create'?'Waitlisted':'Active'}};
  if(path==='activate')request={method:'PUT',relative:'enrollments/'+targetRow.id,body:{status:'Active',endDate:null,lifecycleReason:null}};
  if(path==='transfer')request={method:'POST',relative:'enrollments/'+sourceRow.id+'/transfer',body:{targetBatchId:target.id}};
  if(path==='promotion'){promotion=pending(s,source,target);request={method:'PATCH',relative:'batch-promotions/'+promotion.id+'/decision',body:{status:'Approved'}};}
  return{request,targetRow,ids:{target:target.id,source:sourceRow.id,student:s.id,promotionId:promotion?.id||null},s,source,target,sourceRow};
 }
 async function rejection(label,x,message){const prior=snapshot();denied(raw(x.request),prior,snapshot(),message);f.cases.push({label,status:409,noWriteOrAudit:true});checkpoint();}
 for(const path of['create','activate','transfer','promotion']){
  await separate('prerequisites '+path);const x=setup(path,{required:true,otherRequired:path==='promotion'});await rejection('unmet prerequisite '+path,x,prerequisiteMessage);
 }
 await separate('approved source-course credit');
 const credit=setup('promotion',{required:true,otherRequired:true,otherCompleted:true}),creditBefore=auditCount();a.equal(raw(credit.request).status,200);const credited=state(credit.ids);a.equal(credited.sourceStatus,'Completed');a.equal(credited.promotionStatus,'Approved');a.equal(credited.active,1);a.equal(auditCount()-creditBefore,1);f.cases.push({label:'same-transaction source completion credit plus earlier other completion',status:200,sourceCompleted:true,targetActive:true,auditDelta:1});checkpoint();
 // Corrupt pre-existing duplicate rows are covered by direct-controller tests;
 // native fixtures intentionally never bypass the now-enforced admission API.
 for(const existing of['Active','Waitlisted'])for(const path of['create','transfer','promotion']){
  await separate('duplicate '+existing+' '+path);await rejection('duplicate '+existing+' '+path,setup(path,{existing}),duplicateMessage);
 }
 await separate('repeat waitlist and waitlist capacity');
 await rejection('repeat Waitlisted create',setup('waitlist-create',{existing:'Waitlisted'}),duplicateMessage);
 const full=setup('transfer'),other=student();enroll(other,full.source,'Waitlisted'); // Source defaults4; use explicit capacity edit to1.
 call('PUT','batches/'+full.source.id,{name:full.source.name,courseId:full.source.courseId,capacity:8,waitlistCapacity:1,deliveryMode:'InPerson',enrollmentStatus:'Open',isActive:true},200);
 await rejection('Active to full waitlist',{request:{method:'PUT',relative:'enrollments/'+full.sourceRow.id,body:{status:'Waitlisted',endDate:null,lifecycleReason:'Synthetic waitlist transition'}}},'This batch waitlist is full or not enabled.');
 for(const path of['create','activate','transfer','promotion']){
  await separate('completed history '+path);const x=setup(path,{history:true}),before=auditCount(),response=raw(x.request);a.equal(response.status,path==='create'?201:200);const s=state(x.ids);a.equal(s.active,1);a.equal(s.targetRows,2);a.equal(s.completed,1);a.equal(auditCount()-before,1);f.cases.push({label:'historical Completed retained '+path,status:response.status,historicalRows:1,targetActive:1,auditDelta:1});checkpoint();
 }
 for(const group of['waitlist-create','activate','transfer','promotion']){
  for(let attempt=1;attempt<=3;attempt++){
   await separate('duplicate race '+group+' '+attempt);const x=setup(group,{waiting:group!=='waitlist-create'}),right=group==='waitlist-create'?x.request:{method:'PUT',relative:'enrollments/'+x.targetRow.id,body:{status:'Active',endDate:null,lifecycleReason:null}};
   // The activation group races a new Active create against the existing row.
   const left=group==='activate'?{method:'POST',relative:'enrollments',body:{studentId:x.s.id,batchId:x.target.id,status:'Active'}}:x.request;
   const prior=auditCount(),responses=await race(left,right),statuses=responses.responses.map(x=>x.status),s=state(x.ids);a.equal(responses.bothReadyBeforeRelease,true);raceState(group,statuses,s,auditCount()-prior);f.races.push({group,attempt,statuses,bothReadyBeforeRelease:true,sql:s,auditDelta:1});checkpoint();
  }
 }
 await separate('atomic prerequisite credit required-audit rollback');
 const x=setup('promotion',{required:true}),before=snapshot(),prior=auditCount();try{setDenied(true);a.equal(raw(x.request).status,500);a.deepEqual(snapshot(),before);a.equal(auditCount(),prior);}finally{setDenied(false);}
 a.equal(raw(x.request).status,200);const s=state(x.ids);a.equal(s.sourceStatus,'Completed');a.equal(s.promotionStatus,'Approved');a.equal(s.active,1);a.equal(auditCount()-prior,1);f.rollbacks.push({label:'source credit + target admission + approval + required audit',denied:500,noWrite:true,recovered:200,auditDelta:1});
 a.equal(f.cases.length,17);a.equal(f.races.length,12);a.equal(f.rollbacks.length,1);f.phase='complete';f.passed=true;checkpoint();
}
module.exports={denied,raceState,verifyRules,prerequisiteMessage,duplicateMessage};
