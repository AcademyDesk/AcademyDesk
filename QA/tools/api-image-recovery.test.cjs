'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{snapshot,verifyRecovery}=require('./api-image-recovery.cjs');
const run='a'.repeat(32),database='AcademyDesk_QA_Image_'+run,digest='B'.repeat(64);
function fixture(overrides={}){
 const queries=[],state={quiesced:false,exercised:false},result={checks:[]};
 function sql(query,db='master'){
  queries.push({query,db});
  if(query.includes('sys.master_files'))return JSON.stringify([{name:database,type:0},{name:database+'_log',type:1}]);
  const table=/FROM dbo\.\[([A-Za-z_]+)\] FOR JSON/.exec(query)?.[1];
  if(table)return JSON.stringify({RowCount:table==='__EFMigrationsHistory'?109:table==='AspNetUsers'?5:2,Digest:db.endsWith('_Recovered')&&overrides.changed?'C'.repeat(64):digest});
  if(overrides.backupFails&&query.startsWith('BACKUP'))throw Error('synthetic SQL backup failure');return '';
 }
 return{queries,state,result,sql,run,database,quiesce:async()=>{state.quiesced=true;},exercise:async db=>{a.equal(db,database+'_Recovered');state.exercised=true;if(overrides.apiFails)throw Error('synthetic API failure');}};
}
test('recovery requires separate restore, checksum, consistency, exact table snapshots and real API callback',async()=>{
 const f=fixture();await verifyRecovery(f);a.equal(f.result.recovery.passed,true);a.equal(f.state.exercised,true);a.equal(f.state.quiesced,true);a.equal(f.result.checks.length,1);
 const commands=f.queries.map(row=>row.query).join('\n');a.match(commands,/AS \[RowCount\]/);a.equal(/AS RowCount\b/.test(commands),false);a.match(commands,/COPY_ONLY,CHECKSUM,COMPRESSION/);a.match(commands,/RESTORE VERIFYONLY/);a.match(commands,/DBCC CHECKDB/);a.equal(/\bREPLACE\b|DROP DATABASE|SINGLE_USER/.test(commands),false);a.equal(f.result.recovery.backupExported,false);a.equal(Object.keys(f.result.recovery.tablesAfter).length,8);
});
test('changed restored data, failed backup or failed restored API never qualifies recovery',async()=>{
 for(const flags of[{changed:true},{backupFails:true},{apiFails:true}]){const f=fixture(flags);await a.rejects(verifyRecovery(f));a.equal(f.result.recovery.passed,false);if(!flags.apiFails)a.equal(f.state.exercised,false);}
});
test('no non-owned database or malformed logical filename is accepted',async()=>{
 const f=fixture();await a.rejects(verifyRecovery({...f,database:'customer_db'}));a.equal(f.queries.length,0);
 const original=f.sql;f.sql=(query,db)=>query.includes('sys.master_files')?'[{"name":"unexpected-name","type":0},{"name":"log","type":1}]':original(query,db);
 await a.rejects(verifyRecovery(f));a.equal(f.queries.some(row=>row.query.startsWith('RESTORE DATABASE')),false);
});
test('snapshot rejects empty fixture or missing history rather than certifying an empty backup',()=>{
 a.throws(()=>snapshot(()=>JSON.stringify({RowCount:0,Digest:digest}),database));a.throws(()=>snapshot(()=>JSON.stringify({RowCount:2,Digest:digest}),database));
});
test('disposable-key recovery distinguishes fresh inactive login denial from invalid prior bearer',()=>{
 const source=require('node:fs').readFileSync(require('node:path').join(__dirname,'api-image-sql.cjs'),'utf8');
 a.ok(source.includes("check('prior container bearer is not recovered',http(apiName,'GET','/api/auth/session',undefined,tenants[1].token),401)"));
 a.ok(source.includes("check('restored inactive identity fresh login denied',http(apiName,'POST','/api/auth/login',{email:accounts[0].email,password:loginPassword}),401)"));
 // The original same-container revocation proof must still require403.
 a.ok(source.includes("check('current database deactivation overrides existing bearer',http(apiName,'GET','/api/auth/session',undefined,tenants[0].token),403)"));
});
test('combined recovery independently verifies persisted bearer and revoked authority and keeps replacements on restored SQL',()=>{
 const source=require('node:fs').readFileSync(require('node:path').join(__dirname,'api-image-sql.cjs'),'utf8');
 a.ok(source.includes('runtimeDatabase=restored;'));
 a.ok(source.includes("check('matching encrypted keys recover bearer after SQL restore',http(apiName,'GET','/api/auth/session',undefined,tenants[1].token),200)"));
 a.ok(source.includes("check('SQL restore and recovered keys preserve inactive identity denial',http(apiName,'GET','/api/auth/session',undefined,tenants[0].token),403)"));
 a.match(source,/replace:async environment=>\{remove\(apiName\);api\(apiName,false,`Server=\$\{sqlName\};Database=\$\{runtimeDatabase\}/);
 a.match(source,/activeKeyVolume=destination;api\(apiName,false,`Server=\$\{sqlName\};Database=\$\{runtimeDatabase\}/);a.ok(source.includes('result.jointSyntheticDatabaseAndKeyRecovery=true'));
});
