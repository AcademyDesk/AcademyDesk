// Capture provenance/shape guards are QA checks, never SQL or browser acceptance.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const load=require('./calendar-sql-fixture.cjs');
const dir=path.resolve(__dirname,`../EVIDENCE/calendar-capture-guards-${Date.now()}`);fs.mkdirSync(dir,{recursive:true});
const run='12345678901234567890123456789012';
const before={id:'s',batchId:'b',teacherId:'t',startUtc:'2026-10-08T10:00:00Z',endUtc:'2026-10-08T11:00:00Z',status:'Scheduled',deliveryMode:'Online',roomName:'https://session.example.invalid/meeting'};
const fixture={academyId:'a',beforeSessions:[before],afterSessions:[{...before,status:'Cancelled'}],batches:[{id:'b'}],teachers:[{id:'t',firstName:'Session',lastName:'Teacher'}],expected:{sessionId:'s',teacher:'Session Teacher',location:before.roomName,beforeStatus:'Scheduled',afterStatus:'Cancelled',cancelHttpStatus:200,freshSqlVerified:true}};
const cases=['foreign-cancellation-no-write','anonymous-cancellation-no-write','unassigned-teacher-cancellation-no-write','invalid-cancellation-status-no-write','owned-cancellation-response-fresh-SQL-preserved-history','cancelled-owned-sessions-read','cancelled-substitute-teacher-calendar','cancelled-admin-teacher-projections-match-fresh-SQL'];
function log(f=fixture){return[`QA CalendarDetails run=${run} container=academydesk-qa-${run} started=synthetic`,...cases.map(x=>`CALENDARDETAILS CASE ${x} PASS.`),'CALENDARDETAILS CANCELLATION FIXTURE '+JSON.stringify(f),'CALENDARDETAILS REGRESSION PASS:19 cases;','PASS: application migrations=88, identity migrations=7, scoped runtime login verified; run-owned database and login removed.','QA CalendarDetails exit=0 elapsedSeconds=1',`academydesk-qa-${run}`,`academydesk-qa-${run}`].join('\n');}
let n=0;function read(content){const file=path.join(dir,`${++n}.log`);fs.writeFileSync(file,content);return load(file);}
test('Accept complete synthetic guard fixture with run and SHA receipt',()=>{const result=read(log());assert.equal(result.runId,run);assert.match(result.logSha256,/^[a-f0-9]{64}$/);});
for(const [label,change]of [
 ['missing cleanup',s=>s.replace(/^PASS:.*$/m,'')],['failed exit',s=>s.replace('exit=0','exit=1')],
 ['missing denied control',s=>s.replace(/^CALENDARDETAILS CASE foreign.*$/m,'')],
 ['wrong container ownership',s=>s.replace(`container=academydesk-qa-${run}`,'container=academydesk-qa-other')],
 ['missing exact count',s=>s.replace('PASS:19 cases','PASS:18 cases')],
 ['duplicate captures',s=>s+'\nCALENDARDETAILS CANCELLATION FIXTURE '+JSON.stringify(fixture)],
 ['missing container cleanup',s=>s.split('\n').slice(0,-1).join('\n')],
 ['mixed runs',s=>s+`\nQA CalendarDetails run=${run} container=academydesk-qa-${run} started=synthetic`],
])test('Reject '+label,()=>assert.throws(()=>read(change(log()))));
for(const [label,change]of [
 ['unverified SQL',f=>f.expected.freshSqlVerified=false],['wrong HTTP status',f=>f.expected.cancelHttpStatus=400],
 ['lost history',f=>f.afterSessions=[]],['wrong cancellation',f=>f.afterSessions[0].status='Scheduled'],
 ['changed teacher',f=>f.afterSessions[0].teacherId='other'],['changed location',f=>f.afterSessions[0].roomName='other'],
 ['duplicate identity',f=>{f.beforeSessions.push({...before});f.afterSessions.push({...before,status:'Cancelled'});}],
 ['wrong teacher label',f=>f.expected.teacher='Other Teacher'],
 ['unmarked SQL UTC instant',f=>{f.beforeSessions[0].startUtc=f.beforeSessions[0].startUtc.slice(0,-1);f.afterSessions[0].startUtc=f.beforeSessions[0].startUtc;}],
])test('Reject '+label,()=>{const f=structuredClone(fixture);change(f);assert.throws(()=>read(log(f)));});
