// Actual api.ts with synthetic browser storage and transport; no credentials in callbacks.
const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const ts=require('../../apps/web/node_modules/typescript');
const code=ts.transpileModule(fs.readFileSync('apps/web/src/lib/api.ts','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
function fixture(){
  const data=new Map(),events=new Map(),calls=[];
  const storage={getItem:key=>data.get(key)??null,setItem:(key,value)=>data.set(key,value),removeItem:key=>data.delete(key)};
  const window={location:{pathname:'/payments'},localStorage:storage,addEventListener:(name,fn)=>events.set(name,fn)};
  const fetch=async(url,init={})=>{calls.push({url,init});if(url.endsWith('/refresh'))return{ok:true,json:async()=>({accessToken:'synthetic-renewed',refreshToken:'synthetic-refresh-new'})};return{status:new Headers(init.headers).get('Authorization')==='Bearer synthetic-renewed'?200:401};};
  const mod={exports:{}};vm.runInNewContext(code,{module:mod,exports:mod.exports,process:{env:{}},window,Headers,AbortController,fetch});
  return{api:mod.exports,data,calls,emit:(key,newValue=null,storageArea=storage)=>events.get('storage')({key,newValue,storageArea})};
}
test('Explicit login/replacement notifies once after storage, with no credential arguments',()=>{
  const f=fixture(),seen=[];f.api.subscribePortalSessionChanges('AcademyAdmin',(...args)=>seen.push({args,stored:f.data.has('academydesk.accessToken.AcademyAdmin')}));
  f.api.savePortalTokens('AcademyAdmin','synthetic-access','synthetic-refresh');assert.deepEqual(seen,[{args:[],stored:true}]);
});
test('Sign-out notifies only that workspace and listener disposal is effective',()=>{
  const f=fixture(),seen=[];const stop=f.api.subscribePortalSessionChanges('AcademyAdmin',()=>seen.push('admin'));
  f.api.subscribePortalSessionChanges('Teacher',()=>seen.push('teacher'));f.api.clearPortalTokens('AcademyAdmin');assert.deepEqual(seen,['admin']);
  stop();f.api.clearPortalTokens('AcademyAdmin');assert.deepEqual(seen,['admin']);f.api.clearPortalTokens('Teacher');assert.deepEqual(seen,['admin','teacher']);
});
test('Successful internal token refresh preserves the active view boundary',async()=>{
  const f=fixture();f.api.savePortalTokens('AcademyAdmin','synthetic-old','synthetic-refresh');let boundaries=0;
  f.api.subscribePortalSessionChanges('AcademyAdmin',()=>boundaries++);const result=await f.api.academyApi('/api/synthetic');
  assert.equal(result.status,200);assert.equal(boundaries,0);assert.equal(f.calls.length,3);
});
for(const [label,key,value]of[['replacement','academydesk.accessToken.AcademyAdmin','synthetic-new'],['signout','academydesk.refreshToken.AcademyAdmin',null],['legacy replacement','academydesk.accessToken','synthetic-new'],['storage clear',null,null]])test('Cross-tab '+label+' retires the admin view',()=>{
  const f=fixture();let boundaries=0;f.api.subscribePortalSessionChanges('AcademyAdmin',()=>boundaries++);f.emit(key,value);assert.equal(boundaries,1);
});
test('Unrelated workspace/preferences and sessionStorage events preserve the admin view',()=>{
  const f=fixture();let boundaries=0;f.api.subscribePortalSessionChanges('AcademyAdmin',()=>boundaries++);
  f.emit('academydesk.accessToken.Teacher','synthetic-new');f.emit('academydesk.theme','dark');f.emit('academydesk.accessToken.AcademyAdmin',null,{});assert.equal(boundaries,0);
});
test('A failing consumer cannot prevent sign-out or other consumer notifications',()=>{
  const f=fixture();f.api.savePortalTokens('AcademyAdmin','synthetic-access','synthetic-refresh');let notified=false;
  f.api.subscribePortalSessionChanges('AcademyAdmin',()=>{throw Error('synthetic subscriber failure');});f.api.subscribePortalSessionChanges('AcademyAdmin',()=>notified=true);
  f.api.clearPortalTokens('AcademyAdmin');assert.equal(notified,true);assert.equal(f.data.has('academydesk.accessToken.AcademyAdmin'),false);
});
for (const replacementRefresh of [undefined, '']) test('New explicit session cannot inherit a previous refresh token when replacement refresh is ' + String(replacementRefresh), async () => {
  const f=fixture(); f.api.savePortalTokens('AcademyAdmin','synthetic-old','synthetic-old-refresh');
  f.api.savePortalTokens('AcademyAdmin','synthetic-new',replacementRefresh);
  assert.equal(f.api.portalAccessToken('AcademyAdmin'),'synthetic-new');
  assert.equal((await f.api.academyApi('/api/synthetic')).status,401);
  assert.equal(f.calls.some(call=>call.url.endsWith('/refresh')),false);
});
test('Explicit admin login retires legacy credential fallback without touching Teacher tokens', () => {
  const f=fixture(); f.data.set('academydesk.accessToken','synthetic-legacy'); f.data.set('academydesk.refreshToken','synthetic-legacy-refresh');
  f.api.savePortalTokens('Teacher','synthetic-teacher','synthetic-teacher-refresh');
  f.api.savePortalTokens('AcademyAdmin','synthetic-new');
  assert.equal(f.data.has('academydesk.refreshToken.AcademyAdmin'),false);
  assert.equal(f.data.has('academydesk.accessToken'),false); assert.equal(f.data.has('academydesk.refreshToken'),false);
  assert.equal(f.data.get('academydesk.refreshToken.Teacher'),'synthetic-teacher-refresh');
});
