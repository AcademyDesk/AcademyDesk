'use strict';
// Native mixed-path capacity and required-audit proof; no prerequisite-policy claim.
const a=require('node:assert/strict');
function verifyRace(group,statuses,s,auditDelta){
 a.equal(auditDelta,1);a.equal(s.active,s.capacity);a.ok(s.capacity===1||group==='capacity-edit'&&s.capacity===2);a.equal(s.targetRows,group==='activation-pair'||group==='capacity-edit'?2:statuses[0]<300&&group!=='create'?2:group==='create'&&statuses[0]===201?2:1);
 if(group==='create')a.ok(statuses[0]===201&&statuses[1]===409||statuses[0]===409&&statuses[1]===200);
 else if(group==='capacity-edit')a.ok(statuses[0]===200&&statuses[1]===409||statuses[0]===400&&statuses[1]===200);
 else a.ok(statuses[0]===200&&statuses[1]===409||statuses[0]===409&&statuses[1]===200);
 if(group==='transfer')a.equal(s.sourceStatus,statuses[0]===200?'Transferred':'Active');
 if(group==='promotion'){a.equal(s.sourceStatus,statuses[0]===200?'Completed':'Active');a.equal(s.promotionStatus,statuses[0]===200?'Approved':'Pending');}
}
async function verifyRepair({result,call,raw,race,state,auditCount,snapshot,setDenied,pause,checkpoint=()=>{}}){
 const f=result.admissionRepair={passed:false,phase:'rate separation',syntheticOnly:true,prerequisitePolicyChanged:false,races:[],rollbacks:[]};checkpoint();await pause();const course=call('POST','courses',{name:'Synthetic shared admission course',academyType:'Music'},201);
 let serial=0;function fixture(group){const tag=++serial,target=call('POST','batches',{name:'Synthetic target '+tag,courseId:course.id,capacity:group==='capacity-edit'?2:1,waitlistCapacity:8,deliveryMode:'InPerson',enrollmentStatus:'Open'},201),student=()=>call('POST','students',{firstName:'SyntheticAdmission'+serial,lastName:'Native'},201),aStudent=student(),bStudent=student(),enroll=(s,b,status)=>call('POST','enrollments',{studentId:s.id,batchId:b.id,status},201),waiting=enroll(aStudent,target,group==='capacity-edit'?'Active':'Waitlisted');let second,source,promotion;
  const activation=()=>({method:'PUT',relative:'enrollments/'+waiting.id,body:{status:'Active',endDate:null,lifecycleReason:null}});
  let left,right=activation();
  if(group==='create')left={method:'POST',relative:'enrollments',body:{studentId:bStudent.id,batchId:target.id,status:'Active'}};
  if(group==='activation-pair'){second=enroll(bStudent,target,'Waitlisted');left=activation();right={...activation(),relative:'enrollments/'+second.id};}
  if(group==='transfer'||group==='promotion'){const batch=call('POST','batches',{name:'Synthetic source '+tag,courseId:course.id,capacity:4,deliveryMode:'InPerson',enrollmentStatus:'Open'},201);source=enroll(bStudent,batch,'Active');if(group==='transfer')left={method:'POST',relative:'enrollments/'+source.id+'/transfer',body:{targetBatchId:target.id}};else{call('POST','batch-promotions',{studentId:bStudent.id,sourceBatchId:batch.id,targetBatchId:target.id,effectiveDate:'2026-10-23'},200);promotion=true;left={method:'PATCH',relative:'pending-promotion',body:{status:'Approved'}};}}
  if(group==='capacity-edit'){second=enroll(bStudent,target,'Waitlisted');left={method:'PUT',relative:'batches/'+target.id,body:{name:target.name,courseId:course.id,capacity:1,waitlistCapacity:8,deliveryMode:'InPerson',enrollmentStatus:'Open',isActive:true}};right={...activation(),relative:'enrollments/'+second.id};}
  const ids={target:target.id,source:source?.id||null,promotion:promotion?{student:bStudent.id,target:target.id}:null};if(promotion){const current=state(ids);a.match(current.promotionId,/^[0-9a-f-]{36}$/i);ids.promotionId=current.promotionId;left.relative='batch-promotions/'+ids.promotionId+'/decision';}return{ids,left,right};
 }
 for(const[groupIndex,group]of['create','activation-pair','transfer','promotion','capacity-edit'].entries()){
  if(groupIndex===2||groupIndex===4)await pause();for(let attempt=1;attempt<=3;attempt++){f.phase=group+' '+attempt;checkpoint();const x=fixture(group),prior=auditCount(),responses=await race(x.left,x.right),statuses=responses.responses.map(r=>r.status),persisted=state(x.ids);verifyRace(group,statuses,persisted,auditCount()-prior);a.equal(responses.bothReadyBeforeRelease,true);f.races.push({group,attempt,statuses,bothReadyBeforeRelease:true,sql:persisted,auditDelta:1});checkpoint();}
 }
 await pause();for(const group of['create','activation-pair','transfer','promotion','capacity-edit']){f.phase='required audit '+group;checkpoint();const x=fixture(group),request=group==='activation-pair'?x.right:x.left,before=snapshot(),prior=auditCount();let denied;try{setDenied(true);denied=raw(request);a.equal(denied.status,500);a.deepEqual(snapshot(),before);a.equal(auditCount(),prior);}finally{setDenied(false);}const recovered=raw(request);a.equal(recovered.status,group==='create'?201:200);a.equal(auditCount()-prior,1);const s=state(x.ids);a.ok(s.active<=s.capacity);if(group==='transfer')a.equal(s.sourceStatus,'Transferred');if(group==='promotion'){a.equal(s.sourceStatus,'Completed');a.equal(s.promotionStatus,'Approved');}f.rollbacks.push({group,denied:500,unchanged:true,recovered:recovered.status,auditDelta:1});checkpoint();}
 a.equal(f.races.length,15);a.equal(f.rollbacks.length,5);f.phase='complete';f.passed=true;checkpoint();
}
module.exports={verifyRace,verifyRepair};
