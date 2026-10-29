'use strict';
// Existing BUG-DATA-0006 negative case. A reproduced bypass must FAIL the gate.
// Disposable fixture only; not a new admission policy or an accepted repair.
const a=require('node:assert/strict');
function inspect(r){
 a.equal(r.capacity,1);a.equal(r.targetStatus,'Active');a.equal(r.targetRows,1);a.equal(r.activeRows,2);return true;
}
function verifyAdmission({result,call,raw,state,checkpoint=()=>{}}){
 const f=result.admissionReproduction={issue:'BUG-DATA-0006',passed:false,phase:'seed',syntheticOnly:true,productRulesChanged:false};checkpoint();
 const course=call('POST','courses',{name:'Synthetic admission negative',academyType:'Music'},201),batch=call('POST','batches',{name:'Synthetic capacity one',courseId:course.id,capacity:1,waitlistCapacity:4,deliveryMode:'InPerson',enrollmentStatus:'Open'},201),students=[0,1].map(i=>call('POST','students',{firstName:'SyntheticAdmission'+i,lastName:'NegativeCase'},201));
 const target=call('POST','enrollments',{studentId:students[0].id,batchId:batch.id,status:'Waitlisted'},201);call('POST','enrollments',{studentId:students[1].id,batchId:batch.id,status:'Active'},201);
 const before=state(target.id,batch.id);a.deepEqual(before,{capacity:1,targetStatus:'Waitlisted',targetRows:1,activeRows:1});f.phase='activate waitlisted into full batch';f.before=before;f.expectedGuardStatus=409;checkpoint();
 const response=raw('PUT','enrollments/'+target.id,{status:'Active',endDate:null,lifecycleReason:null}),after=state(target.id,batch.id);f.observedStatus=response.status;f.after=after;f.invariantPreserved=after.activeRows<=after.capacity;f.runtimeReproduced=response.status===200&&!f.invariantPreserved;if(f.runtimeReproduced)inspect(after);f.phase=f.runtimeReproduced?'reproduced admission bypass':'negative result recorded';checkpoint();
 // Keep the expected guard strict. A source repair needs a fresh candidate/run.
 a.equal(response.status,409,'BUG-DATA-0006 activation must reject a full batch.');a.deepEqual(after,before,'Rejected activation must not change committed admission state.');f.passed=true;f.phase='guarded';checkpoint();
}
module.exports={inspect,verifyAdmission};
