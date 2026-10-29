'use strict';
// Same complete row JSON/hash as serial readers, one bounded JSON result.
const a=require('node:assert/strict');
const allowed=new Set(['AccessReviews','AdminWorkItems','AuditLogs','Courses','Batches','Students','Enrollments','Invoices','Payments','AspNetUsers','AspNetRoles','AspNetUserRoles','AccessGrants']);
const expression=table=>`CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM [${table}] ORDER BY ${table==='AspNetUserRoles'?'UserId,RoleId':'Id'} FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)`;
function fingerprint(sql,database,tables,verifySerial=false){
 a.ok(tables.length>0&&tables.length<=13);a.equal(new Set(tables).size,tables.length);a.ok(tables.every(t=>allowed.has(t)));
 const query=`SELECT (SELECT tableName,fingerprint FROM (${tables.map((t,i)=>`SELECT ${i} AS [ordinal], N'${t}' AS tableName, ${expression(t)} AS fingerprint`).join(' UNION ALL ')}) AS fingerprints ORDER BY [ordinal] FOR JSON PATH);`;
 const rows=JSON.parse(sql(query,database,true).trim());a.equal(rows.length,tables.length);
 a.deepEqual(rows.map(r=>r.tableName),tables);a.ok(rows.every(r=>/^[0-9A-F]{64}$/.test(r.fingerprint)));
 const result=Object.fromEntries(rows.map(r=>[r.tableName,r.fingerprint]));
 if(verifySerial)a.deepEqual(result,Object.fromEntries(tables.map(t=>[t,sql(`SELECT ${expression(t)};`,database).trim()])));
 return result;
}
module.exports={fingerprint};
