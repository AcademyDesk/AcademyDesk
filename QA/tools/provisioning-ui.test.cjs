// Actual page handlers with controlled hooks/transport. These are not browser or SQL tests.
const fs=require('node:fs'),assert=require('node:assert/strict'),{test}=require('node:test');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const nodes=x=>!x||typeof x!=='object'?[]:Array.isArray(x)?x.flatMap(nodes):[x,...nodes(x.props?.children)];
const text=x=>x==null?'':Array.isArray(x)?x.map(text).join(''):typeof x==='object'?text(x.props?.children):String(x);
async function page(kind='portal',config={}) {
 const state=[],refs=[],effects=[],calls=[];let index=0,refIndex=0,loaded=false,mutated=false;const mod={exports:{}};
 const response=(ok,data,status=ok?200:400)=>({ok,status,json:async()=>{if(config.invalidJson&&mutated)throw Error('invalid JSON');return data;}});
 const api=async(url,init={})=>{
  calls.push({url,...init});
  if(init.method==='POST') {mutated=true;if(config.hold)await config.hold;if(config.network)throw Error('transport lost');return response(!config.denied,config.denied?{message:config.message}:null,config.status);}
  if(mutated&&config.refreshFailure)throw Error('refresh failed');
  if(url==='/api/academies'||url==='/api/platform/academies'){loaded=true;return response(true,[{id:'owned',name:'Synthetic',isActive:true,students:0}]);}
  if(url.endsWith('/students')||url.endsWith('/guardians')||url.endsWith('/teachers'))return response(!config.lookupDenied,config.malformed?{}:[{id:'person',firstName:'Synthetic',lastName:'Person'}],config.lookupDenied?403:200);
  if(url.endsWith('/session'))return response(true,{displayName:'Synthetic'});
  return response(true,{recentAudit:[],outstandingBilling:0});
 };
 const react={useState:v=>{const k=index++;if(!(k in state))state[k]=v;return[state[k],v=>state[k]=typeof v==='function'?v(state[k]):v];},useRef:v=>{const k=refIndex++;return refs[k]??= {current:v};},useMemo:f=>f(),useEffect:fn=>{if(!loaded)effects.push(fn);}};
 const controls={StandardSelectField(){}};
 const file=kind==='portal'?'portal-accounts':'platform';
 const code=ts.transpileModule(fs.readFileSync(`apps/web/src/app/${file}/page.tsx`,'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX,target:ts.ScriptTarget.ES2022}}).outputText;
 new Function('require','module','exports',code)(name=>name==='react/jsx-runtime'?jsx:name==='react'?react:name==='@/lib/api'?{academyApi:api,apiHeaders:()=>({}),clearPortalTokens(){}}:name==='next/navigation'?{useRouter:()=>({replace(){},push(){}})}:name==='next/link'?()=>null:name==='@/components/theme-toggle'?{ThemeToggle(){}}:name==='@/components/workspace-nav'?{WorkspaceNav(){}}:name==='@/components/design-system/controls'?controls:(()=>{throw Error(name);})(),mod,mod.exports);
 const render=()=>{index=refIndex=0;return mod.exports.default();};
 render();effects.splice(0).forEach(fn=>fn());await new Promise(r=>setImmediate(r));
 const find=p=>nodes(render()).find(p);
 const input=(label,value)=>{const n=find(n=>n.type==='label'&&text(n).includes(label));nodes(n).find(n=>n.type==='input').props.onChange({target:{value}});};
 const select=(name,value)=>find(n=>n.type===controls.StandardSelectField&&n.props.name===name).props.onChange(value);
 if(kind==='portal'){select('portal-person','person');input('Display name','Synthetic Person');input('Portal email','synthetic@example.invalid');input('Temporary password','Synthetic!39Ab');}
 else {find(n=>n.type==='button'&&text(n).includes('＋ Onboard academy')).props.onClick();input('Academy name','Synthetic Academy');input('Academy Admin email','synthetic@example.invalid');input('Temporary Academy Admin password','Synthetic!39Ab');}
 const submit=()=>find(n=>n.type==='form'&&n.props.onSubmit).props.onSubmit({preventDefault(){}});
 return{render,find,select,submit,calls,state,input};
}
test('portal success announces and clears draft without mandatory success body',async()=>{const h=await page();await h.submit();assert.match(text(h.render()),/Created student portal account/);assert.equal(h.find(n=>n.props?.role==='status').props['data-tone'],'success');assert.equal(h.find(n=>n.type==='input'&&n.props.type==='password').props.value,'');assert.deepEqual(JSON.parse(h.calls.find(c=>c.method==='POST').body),{role:'Student',email:'synthetic@example.invalid',password:'Synthetic!39Ab',displayName:'Synthetic Person',studentId:'person',guardianId:null,teacherId:null});});
test('synchronous duplicate submit is blocked; fields stay disabled until settle',async()=>{let finish;const h=await page('portal',{hold:new Promise(r=>finish=r)});const submit=h.submit();await h.submit();assert.equal(h.calls.filter(c=>c.method==='POST').length,1);assert.equal(h.find(n=>n.type==='fieldset').props.disabled,true);finish();await submit;assert.equal(h.find(n=>n.type==='fieldset').props.disabled,false);});
for(const config of [{denied:true,status:409,message:'Another request is initializing the portal role. Please retry. No changes were saved.'},{denied:true,status:400,message:'Login already exists. No changes were saved.'},{denied:true,status:401},{denied:true,status:403},{network:true}])test('portal non-success retains fields and accessible message '+JSON.stringify(config),async()=>{const h=await page('portal',config);await h.submit();assert.equal(h.find(n=>n.type==='input'&&n.props.type==='password').props.value,'Synthetic!39Ab');assert.equal(h.find(n=>n.props?.role==='alert').props['data-tone'],'error');assert.equal(h.find(n=>n.type==='fieldset').props.disabled,false);assert.equal(h.calls.filter(c=>c.method==='POST').length,1);assert.doesNotMatch(text(h.render()),/Created student portal account/);if(config.message)assert.ok(text(h.render()).includes(config.message));if(config.network)assert.match(text(h.render()),/could not be confirmed/);});
for(const config of [{lookupDenied:true},{malformed:true}])test('failed person lookup leaves form unavailable '+JSON.stringify(config),async()=>{const h=await page('portal',config);await h.submit();assert.equal(h.find(n=>n.type==='fieldset').props.disabled,true);assert.equal(h.calls.some(c=>c.method==='POST'),false);assert.match(text(h.render()),/Check your connection and access/);});
for(const role of ['Student','Guardian','Teacher'])test('role selection clears stale linked identity '+role,async()=>{const h=await page();h.select('portal-role',role);await h.submit();assert.equal(h.calls.some(c=>c.method==='POST'),false);assert.match(text(h.render()),/Select the .* first/);});
for(const config of [{},{refreshFailure:true},{denied:true,message:'No changes were saved.'},{network:true}])test('platform create result separated from refresh '+JSON.stringify(config),async()=>{const h=await page('platform',config);await h.submit();if(config.denied||config.network)assert.ok(h.find(n=>n.props?.role==='dialog'));else{assert.equal(h.find(n=>n.props?.role==='dialog'),undefined);assert.match(text(h.render()),/Academy and Academy Admin created/);if(config.refreshFailure){assert.match(text(h.render()),/do not submit it again/);assert.doesNotMatch(text(h.render()),/academy could not be created/);}}});
