'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),{parseMigrationLines}=require('./api-migration-preflight.cjs');
test('source-bound migration parser joins per-file attributes and rejects unknown contexts',()=>{
 const ref='a'.repeat(40),text=`${ref}:apps/api/Migrations/A.cs: [Migration("20260101000000_App")]\n${ref}:apps/api/Migrations/A.cs: [DbContext(typeof(AcademyDeskDbContext))]\n${ref}:apps/api/Migrations/B.cs: [DbContext(typeof(IdentityDbContext))]\n${ref}:apps/api/Migrations/B.cs: [Migration("20260101000001_Identity")]`;
 a.deepEqual(parseMigrationLines(text),{AcademyDeskDbContext:['20260101000000_App'],IdentityDbContext:['20260101000001_Identity']});
 a.throws(()=>parseMigrationLines(text.replace('IdentityDbContext','UnknownContext')));a.throws(()=>parseMigrationLines('not a source-bound line'));
});
