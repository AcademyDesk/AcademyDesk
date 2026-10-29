'use strict';
// Missing waitlist lifecycle proof on the existing immutable API; no new policy.
const a=require('node:assert/strict');
function exactRow(row,expected){for(const key of['id','studentId','batchId']){a.equal(typeof row[key],'string');a.equal(row[key].toLowerCase(),expected[key].toLowerCase());}for(const key of['startDate','status','endDate','lifecycleReason'])a.equal(row[key],expected[key]);a.equal(row.rows,1);}
function raceProof(statuses,s,audits){a.equal(audits,1);a.equal(s.waitlisted,1);a.ok(statuses[0]===201&&statuses[1]===409||statuses[0]===409&&statuses[1]===200);a.equal(s.active,statuses[1]===200?0:1);a.equal(s.rows,statuses[0]===201?2:1);a.equal(s.sourceStatus,statuses[1]===200?'Waitlisted':'Active');}
async function verifyLifecycle({result,call,raw,race,row,counts,snapshot,auditCount,setDenied,pause,checkpoint=()=>{}}){
 const f=result.admissionLifecycle={passed:false,phase:'rate separation',policyChanged:false,applicationChanged:false,cases:[],races:[],rollbacks:[]};checkpoint();await pause();let n=0;
 const course=call('POST','courses',{name:'Synthetic lifecycle course',academyType:'Music'},201);
 const batch=(waitlistCapacity=1,enrollmentStatus='Open')=>call('POST','batches',{name:'Synthetic lifecycle '+(++n),courseId:course.id,capacity:8,waitlistCapacity,deliveryMode:'InPerson',enrollmentStatus},201);
 const student=()=>call('POST','students',{firstName:'SyntheticLifecycle'+(++n),lastName:'Native'},201);
 function fixture(status='Active',capacity=1){const b=batch(capacity),s=student(),e=call('POST','enrollments',{studentId:s.id,batchId:b.id,status,startDate:'2026-10-01'},201);return{b,s,e};}
 const request=(x,status,reason=null)=>({method:'PUT',relative:'enrollments/'+x.e.id,body:{status,endDate:null,lifecycleReason:reason}});
 function stored(x,status,reason=null){const actual=row(x.e.id);exactRow(actual,{id:x.e.id,studentId:x.s.id,batchId:x.b.id,startDate:'2026-10-01',status,endDate:null,lifecycleReason:reason});return actual;}
 function change(label,x,status,reason=null){const before=auditCount(),response=raw(request(x,status,reason));a.equal(response.status,200);const canonical=status.trim()==='waitlisted'?'Waitlisted':status.trim();stored(x,canonical,reason?.trim()||null);a.equal(auditCount()-before,1);f.cases.push({label,status:200,sameId:true,startDateRetained:true,exactFields:true,auditDelta:1});checkpoint();}
 function deny(label,x,status,reason,expected){const before=snapshot(),response=raw(request(x,status,reason));a.equal(response.status,expected);a.deepEqual(snapshot(),before);f.cases.push({label,status:expected,noWriteOrAudit:true});checkpoint();}
 f.phase='same-row round trip';const x=fixture();change('Active to canonical Waitlisted',x,' waitlisted ','  Synthetic waitlist reason  ');a.equal(counts(x).waitlisted,1);
 change('reaffirm own full waitlist row',x,'Waitlisted','Synthetic reaffirmation');a.equal(counts(x).rows,1);
 change('Waitlisted to Active same row',x,'Active');a.equal(counts(x).waitlisted,0);a.equal(counts(x).active,1);
 const paused=fixture();call('PUT','enrollments/'+paused.e.id,{status:'Paused',endDate:null,lifecycleReason:'Synthetic pause seed'},200);
 change('Paused to Waitlisted same row',paused,'Waitlisted','Synthetic waiting');change('Waitlisted to Paused same row',paused,'Paused','Synthetic pause');
 f.phase='no-write transition rejections';const disabled=fixture('Active',0);deny('disabled waitlist rejects entry',disabled,'Waitlisted','Synthetic waiting',409);
 const closed=fixture();call('PUT','batches/'+closed.b.id,{name:closed.b.name,courseId:course.id,capacity:8,waitlistCapacity:1,deliveryMode:'InPerson',enrollmentStatus:'Closed',isActive:true},200);deny('closed batch rejects waitlist entry',closed,'Waitlisted','Synthetic waiting',409);
 const unmet=fixture(),required=call('POST','courses',{name:'Synthetic later prerequisite',academyType:'Music'},201);call('POST','academic-governance/prerequisites',{courseId:course.id,requiredCourseId:required.id},200);deny('current prerequisite rejects waitlist entry',unmet,'Waitlisted','Synthetic waiting',409);
 // All later batches use a separate prerequisite-free course; never remove policy
 // data or fake completion to make an unrelated fixture pass.
 const clear=call('POST','courses',{name:'Synthetic lifecycle race course',academyType:'Music'},201);course.id=clear.id;
 const invalid=fixture();deny('waitlist blank reason rejects without write',invalid,'Waitlisted','   ',400);deny('unknown lifecycle status rejects without write',invalid,'Unknown','Synthetic invalid',400);
 f.phase='last waitlist place races';for(let attempt=1;attempt<=3;attempt++){
  const r=fixture(),other=student(),prior=auditCount(),responses=await race({method:'POST',relative:'enrollments',body:{studentId:other.id,batchId:r.b.id,status:'Waitlisted'}},request(r,'Waitlisted','Synthetic concurrent waiting')),statuses=responses.responses.map(x=>x.status),sql=counts(r);a.equal(responses.bothReadyBeforeRelease,true);raceProof(statuses,sql,auditCount()-prior);f.races.push({attempt,statuses,bothReadyBeforeRelease:true,sql,auditDelta:1});checkpoint();
 }
 f.phase='required audit rollback/recovery';for(const source of['Active','Waitlisted']){
  const x=fixture(source),target=source==='Active'?'Waitlisted':'Active',reason=target==='Waitlisted'?'Synthetic audited waiting':null,prior=auditCount(),before=snapshot();try{setDenied(true);a.equal(raw(request(x,target,reason)).status,500);a.deepEqual(snapshot(),before);a.equal(auditCount(),prior);}finally{setDenied(false);}
  a.equal(raw(request(x,target,reason)).status,200);stored(x,target,reason);a.equal(auditCount()-prior,1);f.rollbacks.push({source,target,denied:500,unchanged:true,recovered:200,sameId:true,auditDelta:1});checkpoint();
 }
 a.equal(f.cases.length,10);a.equal(f.races.length,3);a.equal(f.rollbacks.length,2);f.phase='complete';f.passed=true;checkpoint();
}
module.exports={exactRow,raceProof,verifyLifecycle};
