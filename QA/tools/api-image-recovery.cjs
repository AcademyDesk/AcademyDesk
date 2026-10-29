'use strict';
// Disposable-only recovery drill. No backup leaves the run-owned SQL container.
const a=require('node:assert/strict');
const tables=[['Academies','Id'],['Students','Id'],['Invoices','Id'],['Payments','Id'],['AspNetUsers','Id'],['AspNetUserRoles','UserId,RoleId'],['__EFMigrationsHistory','MigrationId'],['AuditLogs','Id']];
function snapshot(sql,database){
 const rows={};
 for(const [table,order] of tables){
  const row=JSON.parse(sql(`SELECT COUNT_BIG(*) AS [RowCount],CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT * FROM dbo.[${table}] ORDER BY ${order} FOR JSON PATH, INCLUDE_NULL_VALUES)),2) AS [Digest] FROM dbo.[${table}] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;`,database).trim());
  a.ok(Number.isSafeInteger(row.RowCount)&&row.RowCount>=0);a.match(row.Digest,/^[0-9A-F]{64}$/);rows[table]=row;
 }
 a.ok(rows.Academies.RowCount>=2&&rows.AspNetUsers.RowCount>=5&&rows.Payments.RowCount>=1,'Recovery fixture must include real synthetic identity and financial rows.');
 a.equal(rows.__EFMigrationsHistory.RowCount,109);return rows;
}
async function verifyRecovery({run,database,sql,quiesce,exercise,result}){
 a.match(run,/^[0-9a-f]{32}$/);a.equal(database,'AcademyDesk_QA_Image_'+run);
 const restored=database+'_Recovered',backup='/var/opt/mssql/data/qa-recovery-'+run+'.bak';
 const facts=result.recovery={passed:false,phase:'quiesce-owned-api',syntheticOnly:true,backupExported:false,productionBackupTested:false,keyringRecoveryTested:false};
 await quiesce();const started=Date.now();facts.phase='snapshot-source';const before=snapshot(sql,database);facts.tablesBefore=before;
 facts.phase='backup-with-checksum';sql(`BACKUP DATABASE [${database}] TO DISK=N'${backup}' WITH COPY_ONLY,CHECKSUM,COMPRESSION;`);
 facts.phase='verify-backup';sql(`RESTORE VERIFYONLY FROM DISK=N'${backup}' WITH CHECKSUM;`);
 const files=JSON.parse(sql(`SELECT name,type FROM sys.master_files WHERE database_id=DB_ID(N'${database}') ORDER BY type FOR JSON PATH;`).trim());
 a.equal(files.length,2);a.deepEqual(files.map(file=>file.type),[0,1]);
 for(const file of files)a.match(file.name,/^[A-Za-z0-9_]+$/);
 facts.phase='restore-new-owned-database';
 // No REPLACE, source overwrite, DROP, SINGLE_USER, or production backup input.
 sql(`RESTORE DATABASE [${restored}] FROM DISK=N'${backup}' WITH CHECKSUM,MOVE N'${files[0].name}' TO N'/var/opt/mssql/data/${restored}.mdf',MOVE N'${files[1].name}' TO N'/var/opt/mssql/data/${restored}_log.ldf';`);
 facts.phase='restored-consistency';sql(`DBCC CHECKDB ([${restored}]) WITH NO_INFOMSGS, ALL_ERRORMSGS;`);
 const after=snapshot(sql,restored);a.deepEqual(after,before,'Restored canonical table hashes/counts must exactly match.');a.deepEqual(snapshot(sql,database),before,'Source database must remain unchanged during restore.');
 facts.tablesAfter=after;facts.migrationHistories=109;facts.databaseRecoveryMs=Date.now()-started;
 result.checks.push({label:'COPY_ONLY CHECKSUM backup, VERIFYONLY, separate restore, CHECKDB and eight table hash/count comparisons',kind:'sql',passed:true});
 facts.phase='authenticated-restored-api';await exercise(restored);
 facts.phase='complete';facts.passed=true;
}
module.exports={snapshot,verifyRecovery};
