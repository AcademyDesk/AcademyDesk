'use strict';
const{test}=require('node:test'),a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),{risks,scan}=require('./web-export-safety.cjs');
test('ordinary client UI and authentication code are not credential literals',()=>{
 for(const text of['password:"Password", clientSecret:"", Authorization:"Bearer "+token','-----BEGIN CERTIFICATE-----','const session={accessToken:null};','function validatePassword(value){return value.length>8;}'])a.deepEqual(risks('_next/static/chunks/public.js',Buffer.from(text)),[]);
});
test('private paths, source maps, PEM, storage keys, signed URLs, tokens and JWTs report only path-independent risk types',()=>{
 const jwt=Buffer.from(JSON.stringify({alg:'HS256',typ:'JWT'})).toString('base64url')+'.'+Buffer.from(JSON.stringify({sub:'synthetic'})).toString('base64url')+'.'+'x'.repeat(32);
 const sentinels=['-----BEGIN ENCRYPTED PRIVATE KEY-----','ghp_'+'x'.repeat(36),'AccountKey='+'x'.repeat(44),'Server=synthetic;Database=example;Password=synthetic-private-value','?sv=2026-01-01&sp=r&sig='+'x'.repeat(44),jwt];
 for(const text of sentinels){const found=risks('client.js',Buffer.from(text));a.ok(found.length>0);a.equal(JSON.stringify(found).includes(text),false);}
 for(const file of['.env','nested/secrets.json','publishprofiles/site.pubxml','nested/private.key','chunk.js.map','../secret.js','D:/private.js','node_modules/file.js','appsettings.Production.json'])a.ok(risks(file,Buffer.from('safe')).length>0,file);
});
test('scan covers every exact manifest byte and rejects missing, extra, altered and linked artifacts',()=>{
 const parent=path.resolve(__dirname,'../../.build-check'),dir=fs.mkdtempSync(path.join(parent,'web-export-safety-test-'));
 try{const bytes=Buffer.from('const publicUi=true;'),file=path.join(dir,'public.js'),manifest=[{path:'public.js',sha256:crypto.createHash('sha256').update(bytes).digest('hex')}];fs.writeFileSync(file,bytes);a.deepEqual(scan(dir,manifest).findings,[]);
  a.throws(()=>scan(dir,[{...manifest[0],sha256:'0'.repeat(64)}]));a.throws(()=>scan(dir,[...manifest,{path:'missing.js',sha256:'0'.repeat(64)}]));fs.writeFileSync(path.join(dir,'extra.js'),'extra');a.throws(()=>scan(dir,manifest));fs.rmSync(path.join(dir,'extra.js'));fs.symlinkSync(dir,path.join(dir,'linked'),'junction');a.throws(()=>scan(dir,manifest));
 }finally{a.equal(path.dirname(dir),parent);a.match(path.basename(dir),/^web-export-safety-test-[A-Za-z0-9]+$/);a.equal(fs.realpathSync(dir).toLowerCase(),dir.toLowerCase());fs.rmSync(dir,{recursive:true,force:true});}
});
