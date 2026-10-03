// Actual helper/frame in separate JS realms; synthetic shared storage and delivered
// StorageEvents. Not native browser, server revocation or cross-tab refresh locking.
const fs=require('node:fs'),vm=require('node:vm'),a=require('node:assert/strict'),{test}=require('node:test');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const old=process.env.QA_LOGOUT_BASELINE==='1',read=f=>fs.readFileSync(f,'utf8'),compile=f=>ts.transpileModule(read(f),{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const apiCode=compile(old?'QA/EVIDENCE/session-logout-api-before.ts':'apps/web/src/lib/api.ts');
const frameCode=compile(old?'QA/EVIDENCE/session-logout-frame-before.tsx':'apps/web/src/components/workspace-frame.tsx');
const tick=()=>new Promise(r=>setImmediate(r)),workspaces=['AcademyAdmin','Teacher','Portal','Platform'];
const paths={AcademyAdmin:'/dashboard',Teacher:'/teacher',Portal:'/portal',Platform:'/platform/control'};
function tabs(){const data=new Map(),realms=[];return{data,realms,tab(workspace){
 const listeners=new Map(),calls=[],pending=[],state=[];let i=0,cleanup,mounted=false;
 const storage={getItem:k=>data.get(k)??null,setItem:(k,v)=>data.set(k,String(v)),removeItem:k=>data.delete(k)};
 const window={location:{pathname:paths[workspace]},localStorage:storage,addEventListener:(name,fn)=>{if(!listeners.has(name))listeners.set(name,new Set());listeners.get(name).add(fn);},removeEventListener:(name,fn)=>listeners.get(name)?.delete(fn)};
 const fetch=async(url,init={})=>{calls.push({url,headers:new Headers(init.headers)});if(url.endsWith('/refresh')){await new Promise(r=>pending.push(r));return{ok:true,json:async()=>({accessToken:'renewed',refreshToken:'renewed-refresh'})};}return{status:calls.at(-1).headers.get('Authorization')==='Bearer renewed'?200:401};};
 const mod={exports:{}};vm.runInNewContext(apiCode,{module:mod,exports:mod.exports,process:{env:{}},window,Headers,AbortController,fetch});
 const frame={exports:{}};new Function('require','module','exports','window',frameCode)(name=>name==='react/jsx-runtime'?jsx:name==='react'?{useState:x=>{const n=i++;if(!(n in state))state[n]=x;return[state[n],v=>state[n]=v];},useEffect:fn=>{if(!mounted){cleanup=fn();mounted=true;}}}:name==='next/navigation'?{usePathname:()=>window.location.pathname}:name==='next/link'?{default:'a'}:name==='@/lib/api'?mod.exports:{EnterpriseShell:'shell'},frame,frame.exports,window);
 const resolve=x=>x&&typeof x.type==='function'?resolve(x.type(x.props)):x;
 const render=()=>{i=0;return resolve(frame.exports.WorkspaceFrame({children:'PRIVATE-SYNTHETIC-DATA'}));};
 const emit=(key,newValue=null,storageArea=storage)=>{for(const fn of [...listeners.get('storage')??[]])fn({key,newValue,storageArea});};
 const api=mod.exports,realm={api,window,storage,calls,pending,render,emit,dispose:()=>cleanup?.()};realms.push(realm);return realm;
}};}
function flatten(x){if(x==null)return[];if(Array.isArray(x))return x.flatMap(flatten);if(typeof x!=='object')return[x];return[x,...flatten(x.props?.children)];}
for(const workspace of workspaces)test('peer logout gates old '+workspace+' page, retains other workspace pairs',()=>{
 const h=tabs(),peer=h.tab(workspace),source=h.tab(workspace);for(const w of workspaces)source.api.savePortalTokens(w,'old-'+w,'refresh-'+w);peer.render();
 source.api.clearPortalTokens(workspace);peer.emit('academydesk.accessToken.'+workspace);
 const nodes=flatten(peer.render());a.ok(!nodes.includes('PRIVATE-SYNTHETIC-DATA'));a.ok(nodes.some(x=>x?.props?.role==='alert'));a.ok(nodes.some(x=>x?.props?.href?.startsWith('/login')));for(const other of workspaces.filter(x=>x!==workspace))a.equal(h.data.get('academydesk.accessToken.'+other),'old-'+other);
});
for(const workspace of workspaces)test('delivered peer logout prevents old pending renewal after identical re-login '+workspace,async()=>{
 const h=tabs(),peer=h.tab(workspace),source=h.tab(workspace);source.api.savePortalTokens(workspace,'old','refresh-old');const request=peer.api.academyApi('/api/synthetic');await tick();a.equal(peer.pending.length,1);
 source.api.clearPortalTokens(workspace);source.api.savePortalTokens(workspace,'old','refresh-old');peer.emit('academydesk.accessToken.'+workspace);peer.pending.forEach(r=>r());a.equal((await request).status,401);a.equal(h.data.get('academydesk.accessToken.'+workspace),'old');a.equal(peer.calls.length,2);
});
for(const key of ['academydesk.theme','academydesk.accessToken.Teacher','unrelated'])test('Admin page unaffected by unrelated removal '+key,()=>{const h=tabs(),peer=h.tab('AcademyAdmin');peer.render();peer.emit(key);a.ok(flatten(peer.render()).includes('PRIVATE-SYNTHETIC-DATA'));});
test('normal refresh rotation event does not sign out the peer',()=>{const h=tabs(),peer=h.tab('AcademyAdmin');peer.render();peer.emit('academydesk.accessToken.AcademyAdmin','renewed');a.ok(flatten(peer.render()).includes('PRIVATE-SYNTHETIC-DATA'));});
test('sessionStorage event does not sign out localStorage session',()=>{const h=tabs(),peer=h.tab('AcademyAdmin');peer.render();peer.emit('academydesk.accessToken.AcademyAdmin',null,{});a.ok(flatten(peer.render()).includes('PRIVATE-SYNTHETIC-DATA'));});
test('legacy Admin token removal gates its workspace',()=>{const h=tabs(),peer=h.tab('AcademyAdmin');peer.render();peer.emit('academydesk.accessToken');a.ok(!flatten(peer.render()).includes('PRIVATE-SYNTHETIC-DATA'));});
test('storage clear gates open workspace',()=>{const h=tabs(),peer=h.tab('Teacher');peer.render();peer.emit(null);a.ok(!flatten(peer.render()).includes('PRIVATE-SYNTHETIC-DATA'));});
test('public login page is not gated by logout event',()=>{const h=tabs(),peer=h.tab('AcademyAdmin');peer.window.location.pathname='/login';peer.render();peer.emit(null);a.ok(flatten(peer.render()).includes('PRIVATE-SYNTHETIC-DATA'));});
test('unmounted frame listener removed',()=>{const h=tabs(),peer=h.tab('AcademyAdmin');peer.render();peer.dispose();peer.emit(null);a.ok(flatten(peer.render()).includes('PRIVATE-SYNTHETIC-DATA'));});
test('other-workspace pending renewal survives Admin logout',async()=>{const h=tabs(),peer=h.tab('Teacher'),source=h.tab('AcademyAdmin');source.api.savePortalTokens('Teacher','old','refresh-old');source.api.savePortalTokens('AcademyAdmin','admin','admin-refresh');const request=peer.api.academyApi('/api/synthetic');await tick();source.api.clearPortalTokens('AcademyAdmin');peer.emit('academydesk.accessToken.AcademyAdmin');peer.pending.forEach(r=>r());a.equal((await request).status,200);});
