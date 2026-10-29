'use strict';
// Read-only heuristic of the exact exported client payload. Never prints matched
// values, scans credentials outside the export, or substitutes for security review.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const publicExtensions=new Set(['.html','.txt','.ico','.svg','.js','.css','.woff2','.png']);
function risks(relative,bytes){
 a.equal(typeof relative,'string');a.ok(Buffer.isBuffer(bytes));
 const found=new Set(),parts=relative.split(/[\\/]/);
 if(!relative||path.isAbsolute(relative)||/^[A-Za-z]:/.test(relative)||parts.some(x=>!x||x==='.'||x==='..'))found.add('unsafe artifact path');
 if(parts.some(x=>/^\./.test(x)||/^(?:node_modules|bin|obj|uploads|publishprofiles|qa)$/i.test(x)||/^(?:secrets\.json|appsettings(?:\..*)?\.json|nuget\.config|package(?:-lock)?\.json)$/i.test(x)))found.add('local, source or configuration artifact');
 if(!publicExtensions.has(path.extname(relative).toLowerCase()))found.add('unexpected export type / review required');
 // Match likely credentials, not ordinary login labels, password fields,
 // authorization code, empty configuration, public certificates or placeholders.
 const text=bytes.toString('utf8');
 if(/-----BEGIN (?:RSA |EC |OPENSSH |ENCRYPTED )?PRIVATE KEY-----/.test(text))found.add('private key material');
 if(/\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,}|AKIA[A-Z0-9]{16})\b/.test(text))found.add('probable access-key/token literal');
 if(/(?:AccountKey|SharedAccessSignature)\s*=\s*[^;\s"'<>]{16,}/i.test(text)||/(?:Server|Data Source)\s*=\s*[^;\r\n]{1,160};[^\r\n]{0,500}(?:Password|Pwd)\s*=\s*[^;\s"'<>]{6,}/i.test(text))found.add('probable credential connection string');
 if(/(?:[?&]|&amp;)sv=\d{4}-\d{2}-\d{2}[^\s"'<>]{0,1500}(?:[?&]|&amp;)sig=[A-Za-z0-9%+/=_-]{16,}/.test(text))found.add('probable signed storage URL');
 const tokens=text.match(/\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{16,}\b/g)||[];
 for(const token of tokens){try{const header=JSON.parse(Buffer.from(token.split('.')[0],'base64url').toString('utf8'));if(typeof header.alg==='string')found.add('probable JWT literal');}catch{}}
 return [...found].sort();
}
function scan(directory,manifest){
 const base=fs.realpathSync(directory),actual=[],findings=[];let total=0;
 a.ok(Array.isArray(manifest)&&manifest.length>0&&manifest.length<=2000);
 function walk(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){
  a.equal(entry.isSymbolicLink(),false,'Export links are not admissible.');const file=path.join(dir,entry.name);a.ok(entry.isDirectory()||entry.isFile());
  if(entry.isDirectory()){walk(file);continue;}const relative=path.relative(base,file).replaceAll('\\','/'),length=fs.statSync(file).size;a.ok(length<=32*1024**2,'Export file exceeds bounded scan size.');total+=length;a.ok(total<=256*1024**2,'Export exceeds bounded scan size.');
  const bytes=fs.readFileSync(file),risk=risks(relative,bytes);actual.push({path:relative,sha256:hash(bytes)});for(const type of risk)findings.push({path:relative,type});
 }}walk(base);
 const sort=rows=>rows.map(x=>({path:x.path,sha256:x.sha256})).sort((x,y)=>x.path.localeCompare(y.path));a.deepEqual(sort(actual),sort(manifest),'Exact manifest coverage and bytes are required; extra files cannot be ignored.');
 return {files:actual.length,bytes:total,findings,manifestSha256:hash(JSON.stringify(sort(actual)))};
}
function run(){
 const root=path.resolve(__dirname,'../..');a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');a.equal(process.argv.length,3);const id=process.argv[2];a.match(id,/^[a-f0-9]{32}$/);
 const helper=require('./web-candidate-export.cjs'),out=helper.resolveExport(root,id),context=require('./web-dependency-candidate.cjs').open(id),result=scan(out,context.result.exportManifest);helper.resolveExport(root,id);
 const run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/web-export-safety-'+run);fs.mkdirSync(evidence);
 const receipt={run,candidate:id,exportRelative:path.relative(root,out).replaceAll('\\','/'),completed:true,passed:result.findings.length===0,...result,sourceUnchanged:true,readOnlyExport:true,networkRequests:0,dockerMutation:false,productionApproved:false,limitations:['Heuristic likely-literal/private-path detection, not a complete secret, malware, vulnerability, licence or privacy certification.','Cannot rule out obfuscated, encrypted, split, unknown-format or externally loaded secrets.','Checks exact local export, not deployed Azure payload, base image filesystem, model or production configuration.']};fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(receipt,null,2));
 console.log(JSON.stringify({run,passed:receipt.passed,files:receipt.files,bytes:receipt.bytes,findings:receipt.findings,evidence}));if(!receipt.passed)process.exitCode=1;
}
module.exports={risks,scan};if(require.main===module)try{run();}catch{console.error('Exact-export safety scan incomplete; no clean result inferred, input values withheld.');process.exitCode=1;}
