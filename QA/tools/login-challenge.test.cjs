// Execute the actual login handler/render with controlled transport; no native auth claim.
const fs=require('node:fs'),assert=require('node:assert/strict'),{test}=require('node:test');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=fs.readFileSync('apps/web/src/app/login/page.tsx','utf8'),tree=ts.createSourceFile('page.tsx',source,ts.ScriptTarget.Latest,true,ts.ScriptKind.TSX);
let handler;function visit(node){if(ts.isFunctionDeclaration(node)&&node.name?.text==='login')handler=node.getText(tree);ts.forEachChild(node,visit);}visit(tree);assert.ok(handler);
const code=ts.transpileModule(handler,{compilerOptions:{target:ts.ScriptTarget.ES2022}}).outputText;
const account={workspace:'Teacher',roles:['Teacher'],isPlatformOwner:false};
function fixture(config={}){
 const calls=[],saves=[],routes=[],messages=[],challengeStates=[],codes=[],pending={current:false};
 const fetch=async(url,init)=>{calls.push({url,init});if(config.hold)await config.hold;
  if(config.network)throw Error('Synthetic network failure');
  if(url.includes('/login'))return {ok:!config.status,status:config.status||200,json:async()=>{if(config.badJson)throw Error('synthetic JSON');return config.status?config.problem??{}:Object.hasOwn(config,'tokens')?config.tokens:{accessToken:'synthetic-access',refreshToken:'synthetic-refresh'};}};
  return {ok:!config.profileDenied,json:async()=>config.account??account};
 };
 const fn=new Function('fetch','apiUrl','userName','password','twoFactorCode','recoveryMode','twoFactorRequired','setTwoFactorRequired','setTwoFactorCode','pending','setBusy','setMessage','beginPortalSession','validWorkspaceAccount','workspaceLanding','router','searchParams',code+'\nreturn login;')(
  fetch,'http://localhost:5092',' synthetic@example.invalid ','synthetic-password',config.code??'',!!config.recoveryMode,!!config.required,value=>challengeStates.push(value),value=>codes.push(value),pending,()=>{},value=>messages.push(value),(...args)=>saves.push(args),value=>value?.workspace==='Teacher'&&value?.roles?.includes('Teacher'),()=>'/teacher/ai',{push:value=>routes.push(value)},new URLSearchParams());
 return {fn,calls,saves,routes,messages,challengeStates,codes,pending};
}
const event=()=>({preventDefault(){}});
test('native RequiresTwoFactor exposes challenge without storing credentials or profile read',async()=>{const f=fixture({status:401,problem:{detail:'RequiresTwoFactor'}});await f.fn(event());assert.deepEqual(f.challengeStates,[true]);assert.equal(f.calls.length,1);assert.equal(f.saves.length,0);assert.match(f.messages.at(-1),/authenticator|recovery/i);});
for(const recoveryMode of [false,true])test('explicit '+(recoveryMode?'recovery':'authenticator')+' proof uses only the native field',async()=>{const f=fixture({required:true,recoveryMode,code:recoveryMode?' recovery-example ':' 123456 '});await f.fn(event());const body=JSON.parse(f.calls[0].init.body);assert.deepEqual(body,{email:'synthetic@example.invalid',password:'synthetic-password',[recoveryMode?'twoFactorRecoveryCode':'twoFactorCode']:recoveryMode?'recovery-example':'123456'});assert.equal(f.saves.length,1);assert.deepEqual(f.routes,['/teacher/ai']);assert.deepEqual(f.codes,['']);});
test('visible challenge without proof makes no request',async()=>{const f=fixture({required:true,code:' '});await f.fn(event());assert.equal(f.calls.length,0);assert.match(f.messages.at(-1),/code/i);});
test('same-tick duplicate submit issues only one login/profile pair',async()=>{let release;const hold=new Promise(resolve=>release=resolve),f=fixture({hold});const first=f.fn(event()),second=f.fn(event());release();await Promise.all([first,second]);assert.equal(f.calls.filter(call=>call.url.includes('/login')).length,1);assert.equal(f.saves.length,1);assert.equal(f.pending.current,false);});
for(const tokens of [null,[],{},{accessToken:{}},{accessToken:' '},{accessToken:'synthetic',refreshToken:' '},{accessToken:'synthetic',refreshToken:123}])test('malformed tokens rejected before profile '+JSON.stringify(tokens),async()=>{const f=fixture({tokens});
 await f.fn(event());assert.equal(f.calls.length,1);assert.equal(f.saves.length,0);assert.equal(f.routes.length,0);
});
for(const config of [{status:401},{status:429},{status:500},{network:true},{badJson:true},{profileDenied:true},{account:{workspace:'Platform',roles:['Teacher']}}])test('failure preserves workspace tokens/no routing '+JSON.stringify(config),async()=>{const f=fixture(config);await f.fn(event());assert.equal(f.saves.length,0);assert.equal(f.routes.length,0);assert.equal(f.pending.current,false);assert.ok(f.messages.at(-1));});
test('unrecognized server detail is never displayed',async()=>{const f=fixture({status:401,problem:{detail:'untrusted-internal-detail'}});await f.fn(event());assert.ok(!f.messages.join(' ').includes('untrusted-internal-detail'));assert.deepEqual(f.challengeStates,[]);});
test('ordinary password login sends no second-factor properties',async()=>{const f=fixture();await f.fn(event());assert.deepEqual(JSON.parse(f.calls[0].init.body),{email:'synthetic@example.invalid',password:'synthetic-password'});assert.equal(f.saves.length,1);});
test('login failure feedback is an accessible alert',()=>{let index=0;
 const mod={exports:{}};const output=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022,jsx:ts.JsxEmit.ReactJSX}}).outputText;
 new Function('require','module','exports',output)(name=>name==='react/jsx-runtime'?jsx:name==='react'?{useState:initial=>[index++===3?'Synthetic sign-in failure':initial,()=>{}],useRef:value=>({current:value})}:name==='next/navigation'?{useRouter:()=>({}),useSearchParams:()=>new URLSearchParams()}: {},mod,mod.exports);
 const walk=node=>node==null?[]:Array.isArray(node)?node.flatMap(walk):typeof node==='object'?[node,...walk(node.props?.children)]:[node];
 // The retained first four useState declarations must keep the message as fourth.
 assert.ok(walk(mod.exports.default()).some(node=>node?.props?.role==='alert'));
});
