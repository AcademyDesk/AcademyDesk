'use strict';
const a=require('node:assert/strict');
function verifyCoupled(row){a.equal(row.writerBlockedOnGraph,true);a.equal(row.admissionBlockedOnWriter,true);a.deepEqual(row.statuses,[200,409]);a.equal(row.after.prerequisites,1);a.equal(row.after.enrollments,0);a.equal(row.auditDelta,1);}
async function verifyPolicy({result,call,send,hold,blocked,state,snapshot,auditCount,setDenied,checkpoint=()=>{}}){
 const f=result.admissionPolicy={passed:false,phase:'coupled prerequisite/admission',cases:[]};checkpoint();
 function fixture(){const target=call('POST','courses',{name:'Synthetic policy target '+f.cases.length,academyType:'Music'},201),required=call('POST','courses',{name:'Synthetic policy required '+f.cases.length,academyType:'Music'},201),batch=call('POST','batches',{name:'Synthetic policy batch '+f.cases.length,courseId:target.id,capacity:4,deliveryMode:'InPerson',enrollmentStatus:'Open'},201),student=call('POST','students',{firstName:'SyntheticPolicy'+f.cases.length,lastName:'Native'},201);return{target,required,batch,student,edge:{method:'POST',relative:'academic-governance/prerequisites',body:{courseId:target.id,requiredCourseId:required.id}},admit:{method:'POST',relative:'enrollments',body:{studentId:student.id,batchId:batch.id,status:'Active'}}};}
 // Force the graph writer to pause before its policy commit. A correct writer
 // already owns the admission lock; the new learner must wait and then see it.
 const x=fixture(),beforeAudit=auditCount();f.phase='acquiring synthetic graph gate';checkpoint();const gate=await hold('CoursePrerequisites');f.phase='verifying real graph waiter';f.graphGateAcquired=true;checkpoint();let edge,admission;let edgeResponse,admissionResponse;
 try{
  edge=send(x.edge).then(r=>{edgeResponse=r;return r;});const graphWait=await blocked(gate.spid,()=>!!edgeResponse);f.graphWaitObserved=!!graphWait;f.earlyPolicyStatus=edgeResponse?.status??null;checkpoint();a.ok(graphWait,'Actual policy writer must wait on the held graph lock.');
  const writer=await blocked(gate.spid,()=>!!edgeResponse);a.ok(writer);
  admission=send(x.admit).then(r=>{admissionResponse=r;return r;});const waiting=await blocked(writer,()=>!!admissionResponse);
  f.observed={writerBlockedOnGraph:true,admissionBlockedOnWriter:!!waiting,admissionStatusWhileGraphHeld:admissionResponse?.status??null,whileGraphHeld:state(x)};checkpoint();
  a.ok(waiting,'Admission escaped while prerequisite policy was pending.');
 }finally{await gate.release();if(edge)await edge;if(admission)await admission;}
 const row={writerBlockedOnGraph:true,admissionBlockedOnWriter:true,statuses:[edgeResponse.status,admissionResponse.status],after:state(x),auditDelta:auditCount()-beforeAudit};verifyCoupled(row);f.cases.push({label:'pending policy commits before waiting learner is validated',...row});checkpoint();
 // Directly prove the prerequisite writer also waits on the same admission lock.
 f.phase='shared admission lock';const y=fixture(),shared=await hold('EnrollmentAdmission');let pending,response;
 try{pending=send(y.edge).then(r=>{response=r;return r;});a.ok(await blocked(shared.spid,()=>!!response),'Prerequisite write did not respect admission boundary.');}
 finally{await shared.release();if(pending)await pending;}
 a.equal(response.status,200);a.equal(state(y).prerequisites,1);f.cases.push({label:'prerequisite writer waits for shared admission transaction',blocked:true,status:200});checkpoint();
 // Existing required-audit boundary must roll the graph change back as well.
 f.phase='policy audit rollback/recovery';const z=fixture(),before=snapshot(),prior=auditCount();try{setDenied(true);a.equal((await send(z.edge)).status,500);a.deepEqual(snapshot(),before);a.equal(auditCount(),prior);}finally{setDenied(false);}
 a.equal((await send(z.edge)).status,200);a.equal(auditCount()-prior,1);a.equal(state(z).prerequisites,1);const post=snapshot();a.equal((await send(z.admit)).status,409);a.deepEqual(snapshot(),post);
 f.cases.push({label:'policy required-audit rollback and recovery',denied:500,unchanged:true,recovered:200,admissionDenied:409,auditDelta:1});f.phase='complete';f.passed=true;checkpoint();
}
module.exports={verifyCoupled,verifyPolicy};
