'use strict';
// Private owned-container HTTP transport. No mocked API bodies or public API port.
const a=require('node:assert/strict'),{spawn}=require('node:child_process');
function encoded(method,url,payload,authorization){
 a.ok(['GET','POST','PUT','PATCH'].includes(method));a.match(url,/^\/api\/[A-Za-z0-9_./?=&%-]+$/);if(method==='PUT')a.match(url,/^\/api\/academies\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\/(?:enrollments|batches)\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);if(method==='PATCH')a.match(url,/^\/api\/academies\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\/(?:access-grants\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\/revoke|staff\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\/role)$/i);a.ok(!authorization||/^Bearer [A-Za-z0-9_.~-]+$/.test(authorization));a.ok(!payload||Buffer.byteLength(payload)<8192);
 const body=payload||'';return Buffer.from(`${method} ${url} HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n${authorization?'Authorization: '+authorization+'\r\n':''}Content-Type: application/json\r\nContent-Length: ${Buffer.byteLength(body)}\r\n\r\n${body}`).toString('base64');
}
async function exchange(name,method,url,payload,authorization){
 a.match(name,/^academydesk-image-sql-[0-9a-f]{32}-api$/);const wire=encoded(method,url,payload,authorization);
 return new Promise((resolve,reject)=>{
  const child=spawn('docker',['exec',name,'/bin/bash','-c',`exec 3<>/dev/tcp/127.0.0.1/8080; printf '%s' '${wire}' | base64 -d >&3; cat <&3`],{windowsHide:true,stdio:['ignore','pipe','ignore']});let size=0,overflow=false;const chunks=[];
  const timer=setTimeout(()=>child.kill(),15000);child.stdout.on('data',bytes=>{size+=bytes.length;if(size>1024**2){overflow=true;child.kill();}else chunks.push(bytes);});
  child.on('error',()=>{clearTimeout(timer);reject(Error('Owned native browser relay failed; values withheld.'));});child.on('close',code=>{clearTimeout(timer);if(code!==0||overflow)return reject(Error('Owned native browser relay failed; values withheld.'));try{resolve(require('./api-image-sql.cjs').decodeHttp(Buffer.concat(chunks).toString('utf8')));}catch{reject(Error('Owned native browser relay malformed; values withheld.'));}});
 });
}
module.exports={encoded,exchange};
