'use strict';
// Missing pending-configuration/admission proof. Existing immutable host only.
const a=require('node:assert/strict');
const kinds=['closed','waitlist-disabled','course-prerequisite'];
function expected(kind){a.ok(kinds.includes(kind));return{status:409,message:kind==='closed'?'This batch is not open for enrolment.':kind==='waitlist-disabled'?'This batch waitlist is full or not enabled.':'The learner has not completed all course prerequisites for this batch.'};}
function coupled(c){const e=expected(c.kind);a.equal(c.writerBlockedOnBatch,true);a.equal(c.admissionBlockedOnApplication,true);a.equal(c.earlyWriterStatus,null);a.equal(c.earlyAdmissionStatus,null);a.deepEqual(c.statuses,[200,e.status]);a.equal(c.exactMessage,true);a.equal(c.configurationCommitted,true);a.equal(c.enrollmentsUnchanged,true);a.equal(c.auditDelta,1);}
async function verifyConfiguration({result,call,send,hold,blocked,snapshot,enrollmentSnapshot,auditCount,configuration,setDenied,pause,checkpoint=()=>{}}){
 const f=result.admissionConfiguration={passed:false,phase:'rate separation',applicationChanged:false,policyChanged:false,cases:[],rollbacks:[]};checkpoint();await pause();let n=0;
 function fixture(kind){
  const c=call('POST','courses',{name:'Synthetic config course '+(++n),academyType:'Music'},201),s=call('POST','students',{firstName:'SyntheticConfig'+n,lastName:'Native'},201),b=call('POST','batches',{name:'Synthetic configuration batch '+n,courseId:c.id,capacity:8,waitlistCapacity:2,deliveryMode:'InPerson',enrollmentStatus:'Open'},201),e=call('POST','enrollments',{studentId:s.id,batchId:b.id,status:kind==='waitlist-disabled'?'Active':'Waitlisted'},201);
  const body={name:b.name,courseId:c.id,capacity:8,waitlistCapacity:2,deliveryMode:'InPerson',enrollmentStatus:'Open',isActive:true};
  if(kind==='closed')body.enrollmentStatus='Closed';
  if(kind==='waitlist-disabled')body.waitlistCapacity=0;
  if(kind==='course-prerequisite'){
   const next=call('POST','courses',{name:'Synthetic changed course '+n,academyType:'Music'},201),required=call('POST','courses',{name:'Synthetic required config course '+n,academyType:'Music'},201);call('POST','academic-governance/prerequisites',{courseId:next.id,requiredCourseId:required.id},200);body.courseId=next.id;
  }
  return{kind,b,body,edit:{method:'PUT',relative:'batches/'+b.id,body},admit:{method:'PUT',relative:'enrollments/'+e.id,body:{status:kind==='waitlist-disabled'?'Waitlisted':'Active',endDate:null,lifecycleReason:kind==='waitlist-disabled'?'Synthetic config waiting':null}}};
 }
 function denied(response,kind){const e=expected(kind);a.equal(response.status,e.status);a.equal(JSON.parse(response.body).message,e.message);}
 for(const kind of kinds){
  f.phase='pending '+kind;checkpoint();const x=fixture(kind),beforeEnrollments=enrollmentSnapshot(),prior=auditCount(),gate=await hold(x.b.id);let writer,admission,writeResponse,admissionResponse,writeError,admissionError;let writerBlocked=false,admissionBlocked=false;
  try{
   writer=send(x.edit).then(r=>{writeResponse=r;return r;},e=>{writeError=e;return null;});const writerSpid=await blocked(gate.spid,()=>!!writeResponse||!!writeError,false);writerBlocked=!!writerSpid;a.ok(writerBlocked,'Batch writer must be observed blocked on owned batch row.');
   admission=send(x.admit).then(r=>{admissionResponse=r;return r;},e=>{admissionError=e;return null;});admissionBlocked=!!await blocked(writerSpid,()=>!!admissionResponse||!!admissionError,true);
   f.observed={kind,writerBlockedOnBatch:writerBlocked,admissionBlockedOnApplication:admissionBlocked,earlyWriterStatus:writeResponse?.status??null,earlyAdmissionStatus:admissionResponse?.status??null};checkpoint();a.ok(admissionBlocked,'Admission must wait on writer application lock, not merely a row or elapsed time.');a.equal(writeResponse,undefined);a.equal(admissionResponse,undefined);
  }finally{await gate.release();if(writer)await writer;if(admission)await admission;}
  if(writeError)throw writeError;if(admissionError)throw admissionError;a.equal(writeResponse.status,200);denied(admissionResponse,kind);a.deepEqual(enrollmentSnapshot(),beforeEnrollments);configuration(x.b.id,x.body);a.equal(auditCount()-prior,1);
  const row={kind,writerBlockedOnBatch:writerBlocked,admissionBlockedOnApplication:admissionBlocked,earlyWriterStatus:null,earlyAdmissionStatus:null,statuses:[writeResponse.status,admissionResponse.status],exactMessage:true,configurationCommitted:true,enrollmentsUnchanged:true,auditDelta:1};coupled(row);f.cases.push(row);checkpoint();
 }
 for(const kind of kinds){
  f.phase='configuration audit '+kind;checkpoint();const x=fixture(kind),before=snapshot(),prior=auditCount();try{setDenied(true);a.equal((await send(x.edit)).status,500);a.deepEqual(snapshot(),before);a.equal(auditCount(),prior);}finally{setDenied(false);}
  a.equal((await send(x.edit)).status,200);configuration(x.b.id,x.body);a.equal(auditCount()-prior,1);const after=snapshot();denied(await send(x.admit),kind);a.deepEqual(snapshot(),after);f.rollbacks.push({kind,denied:500,unchanged:true,recovered:200,admissionDenied:409,noRejectedWriteOrAudit:true,auditDelta:1});checkpoint();
 }
 a.equal(f.cases.length,3);a.equal(f.rollbacks.length,3);f.phase='complete';f.passed=true;checkpoint();
}
module.exports={expected,coupled,verifyConfiguration};
