// Production calendar TSX regressions under independent host timezones.
// Generated evidence only; does not write application files or contact an API.
const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process');
const root=path.resolve(__dirname,'../..');
const evidence=path.join(root,'QA/EVIDENCE',`calendar-timezone-${Date.now()}`);
fs.mkdirSync(evidence,{recursive:true});
const runs=[];
for(const zone of ['UTC','Asia/Kolkata','America/Los_Angeles','Australia/Sydney']){
 const result=cp.spawnSync(process.execPath,['--test','QA/tools/calendar-details.test.cjs'],{cwd:root,encoding:'utf8',env:{...process.env,TZ:zone}});
 fs.writeFileSync(path.join(evidence,zone.replaceAll('/','-')+'.log'),result.stdout+result.stderr);
 const summary=result.stdout.match(/(?:ℹ|#) tests (\d+)[\s\S]*(?:ℹ|#) pass (\d+)[\s\S]*(?:ℹ|#) fail (\d+)/);
 runs.push({zone,exit:result.status,tests:summary?Number(summary[1]):null,passed:summary?Number(summary[2]):null,failed:summary?Number(summary[3]):null});
}
const capture=process.env.QA_CALENDAR_CANCELLATION_LOG?require('./calendar-sql-fixture.cjs')(process.env.QA_CALENDAR_CANCELLATION_LOG):null;
fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({runs,browser:'NOT RUN',sql:capture?{completedRun:capture.runId,logSha256:capture.logSha256,transport:'captured response replay'}:'NOT RUN',physicalDevices:'NOT RUN'},null,2));
console.log(JSON.stringify({runs,evidence},null,2));
if(runs.some(x=>x.exit!==0))process.exitCode=1;
