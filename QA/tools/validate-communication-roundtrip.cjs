const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),read=f=>fs.readFileSync(path.join(root,f),'utf8').replace(/^\uFEFF/,''),sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex');
const snapshot=JSON.parse(read('QA/REPORTS/PHASE_2B_COMMUNICATION_ROUNDTRIP_SOURCE_SNAPSHOT.json'));
assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),snapshot.commit);
for(const x of [...snapshot.sources,...snapshot.binaries,...snapshot.evidence,...snapshot.normalBinaries])assert.equal(sha(x.file),x.sha256,x.file);
for(const name of ['MAKEUP_LOCATION','LEAVE_IDENTITY','SCHEDULE_DEFAULTS','CALENDAR_DETAILS','ATTENDANCE_NOTES','ASSESSMENT_OPTIONS']){
 const prior=JSON.parse(read(`QA/REPORTS/PHASE_2B_${name}_SOURCE_SNAPSHOT.json`));
 for(const x of [...prior.sources,...(prior.unchangedSources??[])].filter(x=>x.file.startsWith('apps/')&&!['apps/web/src/app/calendar/page.tsx','apps/web/src/app/schedule/page.tsx'].includes(x.file)))assert.equal(sha(x.file),x.sha256,'Prior application '+x.file);
 for(const x of snapshot.normalBinaries)assert.equal(x.sha256,prior.normalBinaries.find(y=>y.file===x.file).sha256,'Normal assembly changed');
}
const prior=JSON.parse(read('QA/REPORTS/PHASE_2B_MAKEUP_LOCATION_SOURCE_SNAPSHOT.json'));
for(const file of ['apps/web/src/app/calendar/page.tsx','apps/web/src/app/schedule/page.tsx'])assert.equal(sha(file),prior.sources.find(x=>x.file===file).sha256);
for(const file of ['apps/api/Controllers/CommunicationSettingsController.cs','apps/api/Domain/Entities/CommunicationChannel.cs','apps/web/src/components/meeting-provider-settings.tsx'])assert.equal(cp.execFileSync('git',['diff','HEAD','--',file],{cwd:root,encoding:'utf8'}),'','Unchanged contract '+file);
const ui=read('QA/EVIDENCE/logs/phase-2b-communication-roundtrip-ui.log');assert.match(ui,/tests 39[\s\S]*pass 39[\s\S]*fail 0/);
const pre=read('QA/EVIDENCE/logs/phase-2b-communication-roundtrip-ui-pre-sql.log');assert.match(pre,/tests 36[\s\S]*pass 36[\s\S]*fail 0/);
const payloads=text=>[...text.matchAll(/^CHANNELROUNDTRIP PAYLOAD (.+)$/gm)].map(x=>JSON.parse(x[1]));
const rows=payloads(ui);assert.equal(rows.length,24);assert.equal(new Set(rows.map(x=>x.label)).size,24);assert.deepEqual(rows,payloads(pre));
const keys=['provider','status','senderName','senderAddress','replyToAddress','phoneNumber','externalAccountReference','messagesEnabled'].sort();
for(const x of rows){assert.deepEqual(Object.keys(x.payload).sort(),keys);assert.deepEqual(Object.keys(x.expected).sort(),keys);if(x.payload.status!=='Configured')assert.equal(x.expected.messagesEnabled,false);}
for(const channel of ['Email','WhatsApp','Meeting']){
 assert.match(ui,new RegExp(channel+' captured real HTTP settings retain hidden fields in actual editor payload'));
 for(const suffix of ['name','provider','address','null-optionals','new-defaults','explicit-clear','NotConfigured','Disabled'])assert.ok(rows.some(x=>x.label===channel+'-'+suffix));
}
const log=read('QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log'),cases=[...log.matchAll(/^CHANNELROUNDTRIP CASE (.+) PASS\.$/gm)].map(x=>x[1]);assert.equal(cases.length,48);assert.equal(new Set(cases).size,48);
for(const x of rows)assert.ok(cases.includes(x.label));
for(const label of ['owner-existing-access','foreign-route','foreign-actor','anonymous-save','teacher-existing-denial','communications-grant-does-not-broaden-owner-rule','foreign-read','anonymous-read','engagement-disabled','academy-inactive','owned-full-list-readback','foreign-owned-list-preserved-configuration'])assert.ok(cases.includes(label),label);
const fixture=JSON.parse(log.match(/^CHANNELROUNDTRIP FIXTURE (.+)$/m)[1]);assert.equal(fixture.rows.length,3);for(const x of fixture.rows){assert.equal(x.hasSecureConnection,true);for(const key of keys)assert.ok(Object.hasOwn(x,key));}
assert.match(log,/CHANNELROUNDTRIP REGRESSION PASS:48 cases/);assert.match(log,/QA CommunicationRoundtrip exit=0/);
assert.match(log,/routes=312, controller method\/routes=301, framework Identity method\/routes=10, SHA256=74CB2F226EBAFF8881F6CF80D6F648310A78B07D79A0A6D9E04D4ED858544FF3/);
assert.match(log,/PASS: application migrations=81, identity migrations=7, scoped runtime login verified; run-owned database and login removed/);assert.match(log,/Negative cleanup check refused a mismatched database marker/);
assert.match(read('QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql-build.log'),/0 Warning\(s\)[\s\S]*0 Error\(s\)/);assert.match(read('QA/EVIDENCE/logs/phase-2b-communication-roundtrip-typecheck.log'),/TypeScript PASS/);
for(const suffix of ['lint','baseline-lint'])assert.match(read('QA/EVIDENCE/logs/phase-2b-communication-roundtrip-'+suffix+'.log'),/0 errors, 1 warning/);
const id=log.match(/run=([a-f0-9]{32}) container=academydesk-qa-\1 /)[1],port=Number(log.match(/^QA SQL port=(\d+)/m)[1]);
assert.equal(cp.spawnSync('docker',['inspect',`academydesk-qa-${id}`],{encoding:'utf8'}).status,1);assert.equal(fs.existsSync(path.join(os.tmpdir(),'AcademyDesk-QA',id)),false);
assert.equal(cp.execFileSync('powershell',['-NoProfile','-Command',`@(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).Count`],{encoding:'utf8'}).trim(),'0');
assert.equal(snapshot.checks.actualPayloads,24);assert.equal(snapshot.checks.httpSqlCases,48);assert.equal(snapshot.checks.controlledTsxCases,39);assert.equal(snapshot.checks.closure,'OPEN');assert.equal(snapshot.checks.browserDevice,'NOT RUN');
assert.match(read('QA/ISSUES/BUG-DATA-0040.md'),/\| Status \| OPEN \|/);
for(const f of ['QA/REPORTS/PHASE_2B_COMMUNICATION_ROUNDTRIP_REPAIR.md','QA/ISSUES/BUG-DATA-0040.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(target&&!/^https?:/.test(target))assert.ok(fs.existsSync(path.resolve(root,path.dirname(f),target)),target);}
cp.execFileSync('git',['diff','--check'],{cwd:root,stdio:'pipe'});
console.log('PASS Communication preservation:39 controlled actual TSX,24 captured payloads,48 real Identity/HTTP/SQL; full replacement/status normalization/security/secure marker preserved; prior product sources and normal assemblies unchanged; owned cleanup verified. Browser/drafts/concurrency/audit rollback/critical/release OPEN.');
