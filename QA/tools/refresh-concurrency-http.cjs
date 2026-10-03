// Real responses only; deterministic buffering of original401s controls overlap/late delivery.
const fs=require('node:fs'),vm=require('node:vm'),a=require('node:assert/strict');
const ts=require('../../apps/web/node_modules/typescript'),defer=()=>{let resolve;const promise=new Promise(r=>resolve=r);return{promise,resolve};};
(async()=>{
 let input='';for await(const chunk of process.stdin)input+=chunk;const fixture=JSON.parse(input),origin=new URL(fixture.origin);a.equal(origin.hostname,'127.0.0.1');a.equal(origin.protocol,'http:');
 let phase;const calls=[],storage=new Map(),window={location:{pathname:'/portal-accounts'},localStorage:{getItem:k=>storage.get(k)??null,setItem:(k,v)=>storage.set(k,String(v)),removeItem:k=>storage.delete(k)}};
 const fetch=async(url,init={})=>{
  a.equal(new URL(url).origin,origin.origin);const path=new URL(url).pathname,response=await global.fetch(url,init);calls.push({path,method:init.method??'GET',status:response.status});
  if(!path.endsWith('/refresh')&&response.status===401){const number=++phase.originals;if(number===3)phase.allOriginals.resolve();await phase.allOriginals.promise;if(phase.late&&number===3)await phase.firstReplay.promise;}
  if(!path.endsWith('/refresh')&&response.status===200)phase.firstReplay.resolve();
  return response;
 };
 const mod={exports:{}};const code=ts.transpileModule(fs.readFileSync('apps/web/src/lib/api.ts','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
 vm.runInNewContext(code,{module:mod,exports:mod.exports,process:{env:{NEXT_PUBLIC_API_URL:origin.origin}},window,Headers,AbortController,fetch});const api=mod.exports;
 async function group(item,label,{late=false,mutation=false,denied=false}={}) {
  window.location.pathname=item.workspace==='Teacher'?'/teacher':'/portal-accounts';
  const current=storage.get('academydesk.refreshToken.'+item.workspace)??item.refreshToken;
  api.savePortalTokens(item.workspace,item.expiredAccess,denied?item.expiredRefresh:current);
  phase={originals:0,allOriginals:defer(),firstReplay:defer(),late};const before=calls.length;
  const get=()=>api.academyApi('/api/auth/session',{headers:api.apiHeaders(true)});
  const requests=[get(),get(),mutation?api.academyApi(`/api/academies/${fixture.academyId}/portal-accounts`,{method:'POST',headers:api.apiHeaders(true),body:JSON.stringify({role:'Student',email:'qa-refresh-student@example.invalid',password:'Synthetic!39Ab',displayName:'Synthetic Concurrent Refresh Student',studentId:fixture.studentId,guardianId:null,teacherId:null})}):get()];
  const responses=await Promise.all(requests);a.ok(responses.every(r=>r.status===(denied?401:200)));const observed=calls.slice(before),refresh=observed.filter(x=>x.path.endsWith('/refresh'));
  a.equal(refresh.length,1);a.equal(observed.length,denied?4:7);a.equal(observed.filter(x=>!x.path.endsWith('/refresh')&&x.status===401).length,3);
  if(!denied){for(let i=0;i<responses.length;i++){const data=await responses[i].json();if(mutation&&i===2)a.equal(data.role,'Student');else{a.equal(data.workspace,item.workspace);a.equal(data.academyId,fixture.academyId);a.ok(data.roles.includes(item.workspace));}}}
  console.log('REFRESHFLIGHT HTTP '+JSON.stringify({label,requests:observed.length,initial401s:3,refreshes:1,refreshStatus:refresh[0].status,replays:denied?0:3,workspace:item.workspace,late401:late,successfulMutations:mutation?1:0}));
 }
 const admin=fixture.sessions.find(x=>x.workspace==='AcademyAdmin'),teacher=fixture.sessions.find(x=>x.workspace==='Teacher');
 await group(admin,'admin-late401',{late:true});await group(teacher,'teacher-overlap');await group(admin,'admin-read-create-overlap',{mutation:true});await group(admin,'expired-refresh-overlap',{denied:true});
 a.equal(calls.length,25);console.log('REFRESHFLIGHT CLIENT PASS25 native HTTP requests/four controlled overlaps;one refresh per group,late401 receipt reused,one successful mutation,expired-refresh waiters401/no replay;no browser acceptance.');
})().catch(()=>{console.error('REFRESHFLIGHT CLIENT FAIL native assertion/transport;credentials omitted');process.exitCode=1;});
