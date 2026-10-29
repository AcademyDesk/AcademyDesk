'use strict';
// Test-only held SQL application lock. Only the current labelled disposable DB.
const a=require('node:assert/strict'),crypto=require('node:crypto'),{spawn}=require('node:child_process');
async function hold({run,name,database,resource,sql,docker,batchId}){
 a.match(run,/^[0-9a-f]{32}$/);a.equal(name,'academydesk-image-sql-'+run+'-db');a.equal(database,'AcademyDesk_QA_Image_'+run);a.equal(JSON.parse(docker(['inspect',name]))[0].Config.Labels['academydesk.qa.run'],run);
 a.match(resource,/^AcademyDesk:(EnrollmentAdmission|CoursePrerequisites|BatchConfiguration):[0-9a-f-]{36}$/i);
 const batchGate=resource.startsWith('AcademyDesk:BatchConfiguration:');
 if(batchGate)a.match(batchId,/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);else a.equal(batchId,undefined);
 const id=crypto.randomUUID();sql(`IF OBJECT_ID('dbo.QaAdmissionPolicyGate','U') IS NULL CREATE TABLE dbo.QaAdmissionPolicyGate(Id uniqueidentifier NOT NULL PRIMARY KEY,Released bit NOT NULL); INSERT dbo.QaAdmissionPolicyGate VALUES('${id}',0);`,database);
 let spid,buffer='',closed=false,timedOut=false;
 const p=spawn('docker',['exec','-i',name,'/bin/bash','-c','SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -h -1 -d '+database],{windowsHide:true,stdio:['pipe','pipe','pipe']});
 function output(b){buffer=(buffer+b.toString()).slice(-65536);}
 p.stdout.on('data',output);p.stderr.on('data',output);
 const done=new Promise(resolve=>{p.on('error',()=>{closed=true;resolve(-1);});p.on('close',code=>{closed=true;resolve(code);});});
 const timer=setTimeout(()=>{timedOut=true;p.kill();},25000);
 const batchLock=batchGate?`DECLARE @batch uniqueidentifier; SELECT @batch=Id FROM dbo.Batches WITH(XLOCK,ROWLOCK,HOLDLOCK) WHERE Id='${batchId}' AND AcademyId='${resource.slice(resource.lastIndexOf(':')+1)}'; IF @batch IS NULL THROW 51002,'Owned synthetic batch missing',1;`:'';
 p.stdin.end(require('./api-image-sql.cjs').sqlSession(`SET XACT_ABORT ON; BEGIN TRAN; DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'${resource}',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000; IF @r<0 THROW 51000,'QA gate lock denied',1; ${batchLock} DECLARE @message varchar(80)=CONCAT('QA_ADMISSION_GATE_READY:',@@SPID); RAISERROR(@message,0,1) WITH NOWAIT; DECLARE @until datetime2=DATEADD(second,20,SYSUTCDATETIME()); WHILE NOT EXISTS(SELECT 1 FROM dbo.QaAdmissionPolicyGate WITH(READUNCOMMITTED) WHERE Id='${id}' AND Released=1) BEGIN IF SYSUTCDATETIME()>@until THROW 51001,'QA gate bounded timeout',1; WAITFOR DELAY '00:00:00.050'; END; COMMIT;`));
 async function release(){try{if(!closed)sql(`UPDATE dbo.QaAdmissionPolicyGate SET Released=1 WHERE Id='${id}';`,database);const code=await done;a.equal(timedOut,false);a.equal(code,0,'Owned SQL gate did not release normally.');}finally{clearTimeout(timer);}}
 try{
  // sqlcmd may buffer redirected output despite TDS NOWAIT. Readiness is proved
  // directly by a granted SQL lock, not by a console message or elapsed time.
  const prefix=resource.slice(0,resource.lastIndexOf(':')+1),until=Date.now()+5000;
  while(!closed&&Date.now()<until){spid=Number(sql(`SELECT TOP(1) l.request_session_id FROM sys.dm_tran_locks l WHERE l.resource_type='APPLICATION' AND l.resource_database_id=DB_ID() AND l.request_status='GRANT' AND l.resource_description LIKE '%${prefix}%'${batchGate?" AND EXISTS(SELECT 1 FROM sys.dm_tran_locks b JOIN sys.partitions p ON p.hobt_id=b.resource_associated_entity_id WHERE b.request_session_id=l.request_session_id AND b.request_status='GRANT' AND b.request_mode='X' AND b.resource_type='KEY' AND p.object_id=OBJECT_ID('dbo.Batches'))":''};`,database).trim());if(spid>0)break;await new Promise(r=>setTimeout(r,50));}
  a.ok(spid>0&&!closed,'Owned SQL gate grant was not observed.');return{spid,release};
 }catch(error){await release().catch(()=>{});throw error;}
}
async function blocked({sql,database,blocker,settled,applicationOnly=false}){
 a.ok(Number.isInteger(blocker)&&blocker>0);const until=Date.now()+3500;
 while(Date.now()<until){const id=Number(sql(`SELECT TOP(1) session_id FROM sys.dm_exec_requests WHERE blocking_session_id=${blocker} AND database_id=DB_ID() AND wait_type LIKE 'LCK_M_%'${applicationOnly?" AND EXISTS(SELECT 1 FROM sys.dm_tran_locks l WHERE l.request_session_id=session_id AND l.resource_database_id=DB_ID() AND l.resource_type='APPLICATION' AND l.request_status='WAIT')":''};`,database).trim());if(id>0)return id;if(settled())return null;await new Promise(r=>setTimeout(r,50));}return null;
}
module.exports={hold,blocked};
