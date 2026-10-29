'use strict';
const{test}=require('node:test'),a=require('node:assert/strict');const{artifactRisk}=require('./api-image-artifacts.cjs');
test('published payload rejects local private and development paths',()=>{
 for(const file of['.env','.env.local','nested/secrets.json','NuGet.Config','appsettings.Local.json','nested/appsettings.qa.local.json','private.pfx','private.pem','private.key','local.log','Properties/PublishProfiles/local.pubxml','obj/project.assets.json','node_modules/package.json','.git/config','QA/evidence.json','OBJ/project.assets.json','NODE_MODULES/package.json','.GIT/config','qa/evidence.json','Program.cs','project.csproj','request.http'])a.ok(artifactRisk(file,Buffer.from('{}')),file);
});
test('public settings and compiled dependency manifests remain eligible',()=>{
 for(const file of['appsettings.json','appsettings.Production.json','appsettings.Development.json','AcademyDesk.Api.deps.json','AcademyDesk.Api.runtimeconfig.json'])a.equal(artifactRisk(file,Buffer.from('{"AllowedHosts":"*","Password":"","Enabled":false}')),null,file);
 a.equal(artifactRisk('runtimes/linux-x64/native/library.so',Buffer.from([0,1,2])),null);
});
test('nonempty JSON credentials and inline key/credential material fail without echoing values',()=>{
 a.ok(artifactRisk('appsettings.json',Buffer.from('{"ClientSecret":"synthetic-private-sentinel"}')));
 a.ok(artifactRisk('web.config',Buffer.from('Password = "synthetic-private-sentinel"')));
 a.ok(artifactRisk('notes.txt',Buffer.from('-----BEGIN PRIVATE KEY-----')));
 a.throws(()=>artifactRisk('bad.json',Buffer.from('synthetic-private-sentinel')),error=>!error.message.includes('private-sentinel')&&error.message.includes('withheld'));
});
test('token-named dependencies are metadata only when they contain valid package versions',()=>{
 const manifest=dependencies=>Buffer.from(JSON.stringify({targets:{'net10.0':{'Example/1.0.0':{dependencies}}},libraries:{}}));
 a.equal(artifactRisk('AcademyDesk.Api.deps.json',manifest({'Microsoft.IdentityModel.Tokens':'8.14.0','System.IdentityModel.Tokens.Jwt':'8.14.0'})),null);
 a.ok(artifactRisk('AcademyDesk.Api.deps.json',manifest({'Microsoft.IdentityModel.Tokens':'synthetic-private-sentinel'})));
 a.ok(artifactRisk('AcademyDesk.Api.deps.json',Buffer.from('{"ClientSecret":"synthetic-private-sentinel","targets":{},"libraries":{}}')));
 a.ok(artifactRisk('appsettings.json',Buffer.from('{"AccessToken":"10.0.13"}')),'Configuration credentials never get dependency-map exemption');
});
