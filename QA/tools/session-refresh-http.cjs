// Actual api.ts and real loopback Identity/SQL; VM provides synthetic workspace storage, not browser UI.
const fs=require('node:fs'),vm=require('node:vm'),a=require('node:assert/strict');
const ts=require('../../apps/web/node_modules/typescript');
const {safeClientFailure}=require('./safe-client-failure.cjs');
(async()=>{
 let input='';for await(const chunk of process.stdin)input+=chunk;const fixture=JSON.parse(input);
 const origin=new URL(fixture.origin);a.equal(origin.hostname,'127.0.0.1');a.equal(origin.protocol,'http:');a.equal(origin.pathname,'/');
 const calls=[],storage=new Map(),events=[],eventTarget=new EventTarget(),window={location:{pathname:'/portal-accounts'},localStorage:{getItem:k=>storage.get(k)??null,setItem:(k,v)=>storage.set(k,String(v)),removeItem:k=>storage.delete(k)},addEventListener:eventTarget.addEventListener.bind(eventTarget),removeEventListener:eventTarget.removeEventListener.bind(eventTarget),dispatchEvent:event=>{events.push({type:event.type,workspace:event.detail});return eventTarget.dispatchEvent(event);}};
 const fetch=async(url,init={})=>{a.equal(new URL(url).origin,origin.origin);const r=await global.fetch(url,init);calls.push({path:new URL(url).pathname,method:init.method??'GET',status:r.status});return r;};
 const module={exports:{}};const code=ts.transpileModule(fs.readFileSync('apps/web/src/lib/api.ts','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
 vm.runInNewContext(code,{module,exports:module.exports,process:{env:{NEXT_PUBLIC_API_URL:origin.origin}},window,Headers,AbortController,CustomEvent,crypto:globalThis.crypto,fetch});const api=module.exports;
 for(const item of fixture.sessions){window.location.pathname=item.workspace==='Teacher'?'/teacher':'/portal-accounts';api.beginPortalSession(item.workspace,item.expiredAccess,item.refreshToken);const epoch=api.portalSessionEpoch(item.workspace);a.match(epoch,/^[0-9a-f-]{36}$/);const other=item.workspace==='Teacher'?'AcademyAdmin':'Teacher',otherEpoch=api.portalSessionEpoch(other),before=calls.length;
  const response=await api.academyApi('/api/auth/session',{headers:api.apiHeaders(true)});a.equal(response.status,200);const session=await response.json();a.equal(session.workspace,item.workspace);a.equal(session.academyId,fixture.academyId);a.ok(session.roles.includes(item.workspace));a.deepEqual(calls.slice(before).map(x=>x.status),[401,200,200]);
  a.equal(api.portalSessionEpoch(item.workspace),epoch);a.equal(api.portalSessionEpoch(other),otherEpoch);
  console.log('SESSIONREFRESH HTTP '+JSON.stringify({label:'expired-'+item.workspace,statuses:[401,200,200],workspace:session.workspace,exactAcademy:true,loginEpochPreserved:true,otherWorkspaceEpochUnchanged:true}));
 }
 window.location.pathname='/portal-accounts';const admin=fixture.sessions.find(x=>x.workspace==='AcademyAdmin');
 const adminEpoch=api.portalSessionEpoch('AcademyAdmin'),teacherEpoch=api.portalSessionEpoch('Teacher');api.savePortalTokens('AcademyAdmin',admin.expiredAccess,storage.get('academydesk.refreshToken.AcademyAdmin'));a.equal(api.portalSessionEpoch('AcademyAdmin'),adminEpoch);let before=calls.length;
 const created=await api.academyApi(`/api/academies/${fixture.academyId}/portal-accounts`,{method:'POST',headers:api.apiHeaders(true),body:JSON.stringify({role:'Student',email:'qa-refresh-student@example.invalid',password:'Synthetic!39Ab',displayName:'Synthetic Refresh Student',studentId:fixture.studentId,guardianId:null,teacherId:null})});
 a.equal(created.status,200);const user=await created.json();a.equal(user.role,'Student');a.deepEqual(calls.slice(before).map(x=>x.status),[401,200,200]);
 console.log('SESSIONREFRESH HTTP '+JSON.stringify({label:'expired-admin-create',statuses:[401,200,200],role:user.role,successfulMutationResponses:1}));
 api.savePortalTokens('AcademyAdmin',admin.expiredAccess,admin.expiredRefresh);before=calls.length;
 const rejected=await api.academyApi('/api/auth/session',{headers:api.apiHeaders(true)});a.equal(rejected.status,401);a.deepEqual(calls.slice(before).map(x=>x.status),[401,401]);a.equal(storage.get('academydesk.accessToken.AcademyAdmin'),admin.expiredAccess);
 console.log('SESSIONREFRESH HTTP '+JSON.stringify({label:'expired-refresh',statuses:[401,401],replayed:false}));
 a.equal(api.portalSessionEpoch('AcademyAdmin'),adminEpoch);a.equal(api.portalSessionEpoch('Teacher'),teacherEpoch);a.deepEqual(events,[{type:api.portalSessionChangedEvent,workspace:'AcademyAdmin'},{type:api.portalSessionChangedEvent,workspace:'Teacher'}]);
 a.equal(calls.length,11);console.log('SESSIONREFRESH CLIENT PASS actual TypeScript helper,11 native HTTP requests; two renewed exact-workspace sessions,one successful portal create,expired refresh no replay; explicit-login epochs/events and role isolation retained. Controlled VM, no browser/Web Locks acceptance.');
})().catch(error=>{console.error('SESSIONREFRESH CLIENT FAIL assertion/transport; credentials omitted');for(const line of safeClientFailure(error))console.error(line);process.exitCode=1;});
