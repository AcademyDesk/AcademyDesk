'use strict';
// Read-only byte-for-byte current static export vs an owned loopback container.
// Not an image build, authenticated API path, model or production acceptance.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict');
const[origin,run,exportPath]=process.argv.slice(2),url=new URL(origin);a.equal(url.protocol,'http:');a.equal(url.hostname,'127.0.0.1');a.ok(Number(url.port)>=1024);a.equal(url.origin,origin);a.match(run,/^[0-9a-f]{32}$/);
const root=path.resolve(__dirname,'../..'),out=fs.realpathSync(exportPath||path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE','static-web-runtime-'+run);
const exportRelative=path.relative(root,out).replaceAll('\\','/'),candidate=/^\.build-check\/web-dependency-candidate\/([0-9a-f]{32})\/app\/out$/.exec(exportRelative);
a.ok(exportRelative==='apps/web/out'||/^\.build-check\/default-web-build\/[0-9a-f]{32}\/app\/out$/.test(exportRelative)||candidate,'Only current or owned isolated exports allowed');
if(candidate)a.equal(require('./web-candidate-export.cjs').resolveExport(root,candidate[1]),out);
fs.mkdirSync(evidence);
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(entry=>{const file=path.join(dir,entry.name);a.ok(file.startsWith(out+path.sep));a.equal(entry.isSymbolicLink(),false);a.ok(entry.isDirectory()||entry.isFile());return entry.isDirectory()?walk(file):[file];});}
(async()=>{
 const checks=[],flights=[];
 for(const file of walk(out).filter(file=>/\.(html|js|css|woff2|svg|png|ico|txt)$/.test(file))){
  const relative=path.relative(out,file).replaceAll('\\','/');let route='/'+relative;
  if(relative.endsWith('.html')&&relative!=='404.html'&&relative!=='_not-found.html')route=relative==='index.html'?'/':'/'+relative.slice(0,-5);
  const response=await fetch(origin+route,{redirect:'error',signal:AbortSignal.timeout(5000),headers:{'Accept-Encoding':'identity'}});
  a.equal(response.status,200,'Static path '+route);const bytes=Buffer.from(await response.arrayBuffer()),source=fs.readFileSync(file);a.equal(hash(bytes),hash(source),'Exact current exported bytes '+route);
  const contentType=response.headers.get('content-type')||'';
  if(relative.endsWith('.html'))a.match(contentType,/^text\/html/);else if(relative.endsWith('.js'))a.match(contentType,/(javascript|ecmascript)/);else if(relative.endsWith('.css'))a.match(contentType,/^text\/css/);
  checks.push({path:route,bytes:bytes.length,sha256:hash(bytes),contentType,passed:true});
  if(relative.includes('/__next.')&&relative.endsWith('/__PAGE__.txt')){
   const marker=relative.indexOf('/__next.'),prefix=relative.slice(0,marker),suffix=relative.slice(marker+1).replaceAll('/','.');
   const flightPath='/'+prefix+'/'+suffix,flight=await fetch(origin+flightPath,{redirect:'error',signal:AbortSignal.timeout(5000)});
   a.equal(flight.status,200,'Flattened Next segment '+flightPath);a.equal(hash(Buffer.from(await flight.arrayBuffer())),hash(source));
   a.ok(!(flight.headers.get('content-type')||'').startsWith('text/html'));flights.push({path:flightPath,sha256:hash(source),passed:true});
  }
 }
 const absent=await fetch(origin+'/qa-missing-'+run,{redirect:'error',signal:AbortSignal.timeout(5000)});a.equal(absent.status,404);
 for(const absentPath of['/dashboard/__next.missing.__PAGE__.txt','/penta/onboarding/__next.penta.missing.__PAGE__.txt','/platform/ai/__next.platform.ai.fake.__PAGE__.txt']){
  const response=await fetch(origin+absentPath,{redirect:'error',signal:AbortSignal.timeout(5000)});a.equal(response.status,404,'Absent segment must not serve HTML');
 }
 const navigations=[];
 for(const route of['/login?returnTo=%2Fpenta','/penta?qa=runtime','/penta/onboarding?conversationId=11111111-1111-4111-8111-111111111111&draftId=22222222-2222-4222-8222-222222222222','/teacher/ai','/portal/ai','/platform/ai','/student-management?studentId=33333333-3333-4333-8333-333333333333']){
  const response=await fetch(origin+route,{redirect:'error',signal:AbortSignal.timeout(5000)});a.equal(response.status,200);a.match(response.headers.get('content-type'),/^text\/html/);navigations.push({path:route,status:200});
 }
 if(candidate){a.equal(require('./web-candidate-export.cjs').resolveExport(root,candidate[1]),out);const manifest=require('./web-dependency-candidate.cjs').open(candidate[1]).result.exportManifest;a.equal(checks.length,manifest.length,'Every candidate export file must be tested, not silently filtered.');}
 const result={run,exportRelative,candidateRun:candidate?.[1]||null,passed:checks.length,html:checks.filter(x=>x.contentType.startsWith('text/html')).length,checks,flights,navigations,missingRoute404:true,missingSegments404:3,productionApproved:false,scope:'current exported files in cached Nginx runtime via read-only bind; no image build/native API/model/device acceptance'};
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({evidence,passed:result.passed,html:result.html,flightSegments:flights.length,navigations:navigations.length,missingRoute404:true}));
})().catch(error=>{console.error(error);process.exitCode=1;});
