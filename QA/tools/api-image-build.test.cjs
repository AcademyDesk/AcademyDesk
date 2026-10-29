'use strict';
const{test}=require('node:test'),a=require('node:assert/strict');const{buildNetwork,inspectBases,configurationRisk,parseConfiguration,inlineRisk,skip,inputs}=require('./api-image-build.cjs');
const fs=require('node:fs'),path=require('node:path');
const{digestReference}=require('./api-image-acquire.cjs');
const{status,patchedPkcs}=require('./api-image-runtime.cjs');
const dockerfile='FROM local/sdk:10 AS build\nWORKDIR /src\nFROM local/runtime:10 AS runtime\n';
test('runtime proof requires real HTTP status lines rather than matching a body number',()=>{a.equal(status('HTTP/1.1 401 Unauthorized\r\n'),401);a.equal(status('HTTP/1.0 503 Service Unavailable\r\n'),503);for(const raw of['body401','',undefined])a.throws(()=>status(raw));});
test('shipped PKCS product version enforces net10 security patch, not file-version numbering or package presence',()=>{
 for(const version of['10.0.13','10.0.13+4099f83977f4d29604a9aeb971fb0a7bc1d5f699','10.0.14'])a.equal(patchedPkcs(version),true);
 for(const version of['10.0.12','9.0.13','11.0.13','10.0.1326.50504','unknown','',undefined])a.equal(patchedPkcs(version),false);
});
test('Docker canonical digest identity omits the mutable version tag',()=>{a.equal(digestReference({reference:'mcr.microsoft.com/dotnet/sdk:10.0',digest:'sha256:'+'a'.repeat(64)}),'mcr.microsoft.com/dotnet/sdk@sha256:'+'a'.repeat(64));});
test('only the explicit approved restore option enables build networking',()=>{a.equal(buildNetwork([]),'none');a.equal(buildNetwork(['--approved-project-restore']),'default');for(const args of[['--network=host'],['--approved-project-restore','extra'],['yes']])a.throws(()=>buildNetwork(args),/Unknown/);});
test('both SDK and runtime must be real locally inspectable image identities',()=>{
 const result=inspectBases(dockerfile,()=> 'sha256:'+'a'.repeat(64));a.equal(result.length,2);a.ok(result.every(base=>base.available));
});
test('BuildKit layer cache alone cannot admit missing SDK or runtime images',()=>{
 const result=inspectBases(dockerfile,reference=>reference.includes('/sdk')?null:'sha256:'+'b'.repeat(64));a.equal(result[0].available,false);a.equal(result[1].available,true);
});
test('unknown IDs, dynamic FROM and unexpected stages fail closed',()=>{
 a.ok(inspectBases(dockerfile,()=> 'synthetic-not-an-image').every(base=>!base.available));
 a.throws(()=>inspectBases(dockerfile.replace('local/sdk:10','$SDK'),()=>null),/Unresolved base/);
 a.throws(()=>inspectBases('FROM local/sdk:10\n',()=>null),/stages/);
});
test('nonempty credential configuration fails without needing to echo its value',()=>{
 for(const key of['Password','ClientSecret','AccessToken','ConnectionString','AccountKey','Credentials'])a.equal(configurationRisk({nested:{[key]:'synthetic-only'}}),true);
 a.equal(configurationRisk({Default:'Server=synthetic;Pwd=synthetic-only'}),true);
});
test('empty credential placeholders and ordinary public settings are permitted',()=>{
 a.equal(configurationRisk({Logging:{LogLevel:{Default:'Warning'}},AllowedHosts:'*',MediaStorage:{ServiceUri:'https://storage.invalid',Enabled:false},Password:'',ClientSecret:null}),false);
});
test('private artifacts and local outputs never enter the bounded source context',()=>{
 for(const name of['bin','obj','uploads','.env','.env.local','appsettings.Local.json','appsettings.dev.local.json','secrets.json','NuGet.Config','local.pfx','key.pem','private.key','local.log'])a.equal(skip(name),true,name);
 for(const name of['Program.cs','Dockerfile','appsettings.Production.json'])a.equal(skip(name),false,name);
});
test('ordinary C# password property forwarding is not a credential literal',()=>{
 a.equal(inlineRisk('Password = request.Password;'),false);a.equal(inlineRisk('var hasPassword = !string.IsNullOrEmpty(input.Password);'),false);
});
test('literal passwords, connection credentials and private keys are rejected',()=>{
 for(const text of['Password = "synthetic-only";','var connection = "Server=synthetic;Pwd=synthetic-only";','-----BEGIN PRIVATE KEY-----'])a.equal(inlineRisk(text),true);
});
test('BOM JSON is supported and parser failures never echo input values',()=>{
 a.deepEqual(parseConfiguration(Buffer.from('\uFEFF{"Enabled":false}'),'synthetic.json'),{Enabled:false});
 a.throws(()=>parseConfiguration(Buffer.from('synthetic-private-sentinel'),'synthetic.json'),error=>error.message.includes('contents withheld')&&!error.message.includes('private-sentinel'));
});
test('current API inputs pass the bounded no-value secret heuristic',()=>{
 const files=inputs();a.ok(files.length>100);a.ok(files.some(file=>file.path==='apps/api/Dockerfile'));
 for(const file of files){a.match(file.sha256,/^[0-9a-f]{64}$/);a.ok(file.path.startsWith('apps/api/'));}
});
test('production Docker context explicitly excludes local credentials and IDE state',()=>{
 const patterns=fs.readFileSync(path.resolve(__dirname,'../../.dockerignore'),'utf8').split(/\r?\n/).map(line=>line.trim());
 for(const pattern of['QA','.build-check','**/.env','**/.env.*','**/secrets.json','**/appsettings.Local.json','**/appsettings.local.json','**/appsettings.*.local.json','**/NuGet.Config','**/nuget.config','**/Properties/PublishProfiles','**/.vs','**/.idea','**/.vscode'])a.ok(patterns.includes(pattern),pattern);
 a.equal(patterns.includes('**/appsettings.Production.json'),false,'Required public production settings remain inputs');
});
