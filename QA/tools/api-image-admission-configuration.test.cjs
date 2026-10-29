'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{expected,coupled}=require('./api-image-admission-configuration.cjs');
test('configuration/admission proof requires actual application blocking, current denial, no changed enrollment and one audit',()=>{
 for(const kind of['closed','waitlist-disabled','course-prerequisite']){const row={kind,writerBlockedOnBatch:true,admissionBlockedOnApplication:true,earlyWriterStatus:null,earlyAdmissionStatus:null,statuses:[200,409],exactMessage:true,configurationCommitted:true,enrollmentsUnchanged:true,auditDelta:1};coupled(row);a.equal(expected(kind).status,409);for(const edit of[x=>x.writerBlockedOnBatch=false,x=>x.admissionBlockedOnApplication=false,x=>x.earlyAdmissionStatus=200,x=>x.statuses[1]=200,x=>x.exactMessage=false,x=>x.configurationCommitted=false,x=>x.enrollmentsUnchanged=false,x=>x.auditDelta=2]){const bad=structuredClone(row);edit(bad);a.throws(()=>coupled(bad));}}
 a.throws(()=>expected('unknown'));
});
