'use strict';
const a=require('node:assert/strict');
async function verify({result,evidence,tenants,accounts,loginPassword,apiName,sqlName,sqlImageId,database,http,check,sql,docker,save}){
 const {run}=result,tenant=tenants[0],owned=()=>{const d=JSON.parse(docker(['inspect',sqlName]))[0];a.equal(d.Config.Labels['academydesk.qa.run'],run);a.equal(d.Image,sqlImageId);a.equal(sql('SELECT DB_NAME();',database).trim(),database);};owned();
 const snapshot=()=>Object.fromEntries(['Courses','Batches','Students','Enrollments','Invoices','Payments','AuditLogs','AspNetUsers','AspNetRoles','AspNetUserRoles','AccessGrants'].map(table=>[table,sql(`SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM [${table}] ORDER BY ${table==='AspNetUserRoles'?'UserId,RoleId':'Id'} FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2);`,database).trim()]));
 await require('./api-image-staff-browser.cjs').verify({result,evidence,email:accounts[0].email,password:loginPassword,academy:tenant.id,checkpoint:save,exchange:(...args)=>require('./native-browser-transport.cjs').exchange(apiName,...args),snapshot,
 auditCount:()=>Number(sql(`SELECT COUNT(*) FROM AuditLogs WHERE AcademyId='${tenant.id}' AND Action IN ('POST Staff','PATCH Staff');`,database).trim()),
 row:id=>{a.match(id,/^[0-9a-f-]{36}$/i);const r=JSON.parse(sql(`SELECT DisplayName AS displayName,Email AS email,CAST(IsActive AS bit) AS isActive,JSON_QUERY(COALESCE((SELECT r.Name AS name FROM AspNetUserRoles ur JOIN AspNetRoles r ON ur.RoleId=r.Id WHERE ur.UserId=u.Id ORDER BY r.Name FOR JSON PATH),N'[]')) AS roles FROM AspNetUsers u WHERE Id='${id}' AND AcademyId='${tenant.id}' AND Email LIKE 'staff-browser-${run}-%@qa.invalid' FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;`,database,true).trim());return{...r,roles:r.roles.map(v=>v.name)};},
 login:email=>{a.match(email,new RegExp('^staff-browser-'+run+'-[0-3]@qa\\.invalid$'));const r=check('staff browser actual target login',http(apiName,'POST','/api/auth/login',{email,password:loginPassword}),200);a.ok(r.accessToken);return r.accessToken;},session:bearer=>http(apiName,'GET','/api/auth/session',undefined,bearer).status,
 roleCount:()=>Number(sql("SELECT COUNT(*) FROM AspNetRoles WHERE Name='FrontDesk' AND AcademyId IS NULL;",database).trim()),
 enrollment:(bearer,status)=>check('staff browser current enrollment authority',http(apiName,'GET',`/api/academies/${tenant.id}/enrollments`,undefined,bearer),status),
 setDenied:denied=>{owned();sql(`${denied?'DENY':'REVOKE'} INSERT ON OBJECT::dbo.AuditLogs ${denied?'TO':'FROM'} [qa_${run}];`,database);}});
}
module.exports={verify};
