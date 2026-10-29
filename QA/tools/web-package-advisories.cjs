'use strict';
// Public lockfile package/version metadata only; no npm config, auth or install.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),semver=require(path.join(root,'apps/web/node_modules/semver'));
const endpoint='https://registry.npmjs.org/-/npm/v1/security/advisories/bulk';
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const namePattern=/^(?:@[a-z0-9_.-]+\/)?[a-z0-9_.-]+$/;
function packagePayload(lock){
 a.equal(lock.lockfileVersion,3);a.ok(lock.packages&&typeof lock.packages==='object');const payload=Object.create(null),entries=[];
 for(const [location,entry]of Object.entries(lock.packages)){
  if(location==='')continue;a.ok(location.startsWith('node_modules/')&&!location.includes('\\')&&!location.split('/').some(part=>['.','..',''].includes(part)));
  a.equal(entry.link,undefined);const name=entry.name||location.slice(location.lastIndexOf('node_modules/')+13);a.match(name,namePattern);a.equal(semver.valid(entry.version),entry.version);
  let provenance=entry;
  if(entry.inBundle===true&&entry.resolved===undefined){const marker=location.lastIndexOf('/node_modules/');a.ok(marker>0);const ancestor=lock.packages[location.slice(0,marker)];a.ok(ancestor&&Array.isArray(ancestor.bundleDependencies)&&ancestor.bundleDependencies.includes(name),'Bundled package requires exact declared public parent.');provenance=ancestor;}
  const uri=new URL(provenance.resolved);a.equal(uri.origin,'https://registry.npmjs.org');a.ok(!uri.username&&!uri.password&&!uri.search&&!uri.hash&&uri.pathname.endsWith('.tgz'));a.match(provenance.integrity,/^sha512-[A-Za-z0-9+/]{86}==$/);a.equal(Buffer.from(provenance.integrity.slice(7),'base64').length,64);
  payload[name]??=[];if(!payload[name].includes(entry.version))payload[name].push(entry.version);entries.push({location,name,version:entry.version,devOnly:entry.dev===true});
 }
 a.ok(entries.length>0&&entries.length<5000);for(const versions of Object.values(payload))versions.sort();return{payload,entries};
}
function matchAdvisories(payload,response){
 a.ok(response&&typeof response==='object'&&!Array.isArray(response));const findings=[],seen=new Set();
 for(const [name,advisories]of Object.entries(response)){
  a.ok(Object.hasOwn(payload,name));a.ok(Array.isArray(advisories));
  for(const advisory of advisories){
   a.ok(Number.isSafeInteger(advisory.id)&&advisory.id>0);a.ok(['low','moderate','high','critical'].includes(advisory.severity));a.match(advisory.url,/^https:\/\/(?:github\.com\/advisories\/GHSA-[A-Za-z0-9-]+|www\.npmjs\.com\/advisories\/\d+)$/);
   if(advisory.name!==undefined)a.equal(advisory.name,name);a.equal(typeof advisory.vulnerable_versions,'string');a.ok(semver.validRange(advisory.vulnerable_versions,{includePrerelease:true,loose:true})!==null);
   for(const version of payload[name])if(semver.satisfies(version,advisory.vulnerable_versions,{includePrerelease:true,loose:true})){
    const key=[name,version,advisory.id,advisory.vulnerable_versions].join('|');if(seen.has(key))continue;seen.add(key);findings.push({package:name,version,id:advisory.id,severity:advisory.severity,advisory:advisory.url,affectedRange:advisory.vulnerable_versions});
   }
  }
 }
 return findings;
}
function parseAdvisoryResponse(bytes,contentType){
 // The bulk API defines a JSON response, but may omit Content-Type. Trust neither
 // a header nor a200 alone: require bounded strict UTF-8/JSON and schema below.
 a.ok(bytes.length>0&&bytes.length<=16*1024**2);if(contentType!==null)a.match(contentType,/^application\/json(?:;|$)/i);
 const value=JSON.parse(new TextDecoder('utf-8',{fatal:true}).decode(bytes));a.ok(value&&typeof value==='object'&&!Array.isArray(value));return value;
}
async function run(){
 const candidate=process.argv[2]==='--candidate';a.ok(process.argv.length===2||(candidate&&process.argv.length===4));a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');
 const candidateContext=candidate?require('./web-dependency-candidate.cjs').open(process.argv[3]):null;
 const verifySource=()=>candidate?(candidateContext.result.validated?require('./api-image-payment-browser.cjs').verifyExport(process.argv[3]):require('./web-dependency-candidate.cjs').open(process.argv[3])):require('./api-image-payment-browser.cjs').verifyExport();const verified=candidate?{build:candidateContext.result.validated?candidateContext.result.run:null}:verifySource();verifySource();
 if(candidate){a.ok(candidateContext.result.steps.some(step=>step.name==='resolve'&&step.passed));require('./web-dependency-candidate.cjs').validateManifest(candidateContext.result.originalManifest,JSON.parse(fs.readFileSync(path.join(candidateContext.app,'package.json'))));}
 const lockFile=path.join(candidate?candidateContext.app:path.join(root,'apps/web'),'package-lock.json'),bytes=fs.readFileSync(lockFile),{payload,entries}=packagePayload(JSON.parse(bytes)),body=JSON.stringify(payload);a.ok(Buffer.byteLength(body)<1024**2);
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/web-package-advisories-'+run);fs.mkdirSync(evidence,{recursive:true});
 const result={run,exportBuild:verified.build,candidateRun:candidate?process.argv[3]:null,scope:candidate?'isolated candidate lock; exact original/applied source lineage and built export when available':'current accepted export lock',lockSha256:hash(bytes),completed:false,packageEntries:entries.length,distinctPackageNames:Object.keys(payload).length,endpoint,retrievedAtUtc:new Date().toISOString(),projectNameSent:false,academyDataSent:false,credentialsUsed:false,packageDownloads:false,dockerMutation:false,productionApproved:false,wholeImageScanComplete:false,metaVulnerabilityCalculation:false};
 try{
  const response=await fetch(endpoint,{method:'POST',body,redirect:'error',credentials:'omit',signal:AbortSignal.timeout(20000),headers:{'content-type':'application/json','accept':'application/json','cache-control':'no-cache'}});result.httpStatus=response.status;result.contentType=response.headers.get('content-type');a.equal(response.status,200);
  const chunks=[];let total=0;for await(const chunk of response.body){total+=chunk.length;a.ok(total<=16*1024**2);chunks.push(chunk);}const raw=Buffer.concat(chunks);fs.writeFileSync(path.join(evidence,'advisories.json'),raw);
  const findings=matchAdvisories(payload,parseAdvisoryResponse(raw,result.contentType));
  a.equal(hash(fs.readFileSync(lockFile)),result.lockSha256);verifySource();
  Object.assign(result,{completed:true,responseSha256:hash(raw),httpDate:response.headers.get('date'),findings,findingCount:findings.length,noKnownAffectedLockPackageVersions:findings.length===0,sourceUnchanged:true,rangeEngine:{version:require(path.join(root,'apps/web/node_modules/semver/package.json')).version,entrypointSha256:hash(fs.readFileSync(require.resolve(path.join(root,'apps/web/node_modules/semver'))))},limitations:['All lockfile entries including dev/platform-specific packages, not proof each package is shipped or executed.','Direct advisory range matching, not npm meta-vulnerability propagation, exploitability, malicious package or licence review.','Does not cover OS/shared framework/API/model/container/runtime configuration or approve release.']});
 }catch(error){result.failure='Public package advisory verification incomplete; values withheld. No clean result permitted.';result.errorType=['AssertionError','TimeoutError','TypeError'].includes(error.name)?error.name:'operation error';}
 finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({run,evidence,completed:result.completed,packageEntries:entries.length,distinctPackageNames:Object.keys(payload).length,findingCount:result.findingCount,productionApproved:false}));if(!result.completed||result.findingCount>0)process.exitCode=1;}
}
module.exports={packagePayload,matchAdvisories,parseAdvisoryResponse};if(require.main===module)run().catch(()=>{console.error('Candidate preflight failed; values withheld.');process.exitCode=1;});
