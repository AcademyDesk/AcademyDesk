// Only accept a completed run-owned SQL/HTTP capture. No credentials are read/output.
const fs=require('node:fs'),assert=require('node:assert/strict'),crypto=require('node:crypto');
module.exports=function loadCalendarCancellation(logPath){
 const log=fs.readFileSync(logPath,'utf8');
 assert.match(log,/CALENDARDETAILS REGRESSION PASS:19 cases;/);
 assert.match(log,/^PASS: application migrations=\d+, identity migrations=\d+, scoped runtime login verified; run-owned database and login removed\.$/m);
 assert.match(log,/^QA CalendarDetails exit=0 /m);
 for(const name of ['foreign-cancellation-no-write','anonymous-cancellation-no-write','unassigned-teacher-cancellation-no-write','invalid-cancellation-status-no-write','owned-cancellation-response-fresh-SQL-preserved-history','cancelled-owned-sessions-read','cancelled-substitute-teacher-calendar','cancelled-admin-teacher-projections-match-fresh-SQL'])assert.match(log,new RegExp('^CALENDARDETAILS CASE '+name+' PASS\\.$','m'));
 const run=log.match(/^QA CalendarDetails run=([a-f0-9]{32}) container=academydesk-qa-\1 /m);assert.ok(run,'Owned run identity');
 assert.equal([...log.matchAll(/^QA CalendarDetails run=/gm)].length,1,'Exactly one captured run');
 const ownedName='academydesk-qa-'+run[1];assert.deepEqual(log.trim().split(/\r?\n/).slice(-2),[ownedName,ownedName],'Runner confirms owned container stop and removal');
 const captures=[...log.matchAll(/^CALENDARDETAILS CANCELLATION FIXTURE (.+)$/gm)];assert.equal(captures.length,1);
 const fixture=JSON.parse(captures[0][1]),e=fixture.expected;
 assert.equal(e.cancelHttpStatus,200);assert.equal(e.freshSqlVerified,true);assert.equal(e.beforeStatus,'Scheduled');assert.equal(e.afterStatus,'Cancelled');
 for(const field of ['beforeSessions','afterSessions','batches','teachers'])assert.ok(Array.isArray(fixture[field]));
 assert.equal(fixture.afterSessions.length,fixture.beforeSessions.length);
 assert.equal(new Set(fixture.beforeSessions.map(x=>x.id)).size,fixture.beforeSessions.length);
 assert.equal(new Set(fixture.afterSessions.map(x=>x.id)).size,fixture.afterSessions.length);
 for(const row of fixture.beforeSessions){
  const after=fixture.afterSessions.find(x=>x.id===row.id);assert.ok(after);
  for(const field of ['startUtc','endUtc'])assert.match(row[field],/Z$/,'SQL UTC timestamp must be explicitly marked');
  assert.deepEqual(after,row.id===e.sessionId?{...row,status:'Cancelled'}:row);
 }
 const before=fixture.beforeSessions.find(x=>x.id===e.sessionId);assert.ok(before);assert.equal(before.status,'Scheduled');assert.equal(before.roomName,e.location);assert.equal(before.deliveryMode,'Online');
 const teacher=fixture.teachers.find(x=>x.id===before.teacherId);assert.ok(teacher);assert.equal(`${teacher.firstName} ${teacher.lastName}`,e.teacher);
 assert.ok(fixture.batches.some(x=>x.id===before.batchId));
 return{fixture,runId:run[1],logSha256:crypto.createHash('sha256').update(log).digest('hex')};
};
