// Controlled execution of the actual make-up TSX handlers, not live React/browser evidence.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=fs.readFileSync('apps/web/src/app/makeup/page.tsx','utf8'),code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
async function page(config={}){
 let index=0,refIndex=0,effectIndex=0;const state=new Map(),refs=[],deps=[],effects=[],calls=[],module={exports:{}};
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,typeof v==='function'?v():v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useRef:v=>{const k=refIndex++;return refs[k]??(refs[k]={current:v});},useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const Controls={StandardSelectField(){},StandardDateField(){},StandardTimeField(){}};
 const data={students:[{id:'student',firstName:'Synthetic',lastName:'Student'}],batches:[{id:'batch',name:'Synthetic batch'}],teachers:[], 'makeup-classes':config.rows??[]};
 const api=async(url,init={})=>{calls.push({url,...init});return{ok:true,status:200,json:async()=>init.method?{}:url==='/api/academies'?[{id:'owned'}]:data[url.split('/').at(-1)]};};
 new Function('require','module','exports',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({})}:n==='@/components/workspace-nav'?{WorkspaceNav(){}}:n==='@/components/design-system/controls'?Controls:(()=>{throw Error(n)})(),module,module.exports);
 const render=()=>{index=refIndex=effectIndex=0;return module.exports.default();};
 render();for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await new Promise(r=>setImmediate(r));render();}
 const control=name=>nodes(render()).find(n=>n.props?.name===name),input=placeholder=>nodes(render()).find(n=>n.props?.placeholder===placeholder);
 control('student').props.onChange('student');control('batch').props.onChange('batch');
 return{state,calls,render,control,input,submit:()=>nodes(render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){}})};
}
for(const mode of ['Offline','Online','Hybrid'])test(`Manual ${mode} displays and submits only its relevant location`,async()=>{
 const p=await page();p.control('makeup-date').props.onChange('2026-10-31');p.control('delivery-mode').props.onChange(mode);
 p.state.set(11,' Studio A ');p.state.set(12,' https://meeting.example.invalid/makeup ');
 assert.equal(Boolean(p.input('Room or venue (optional)')),mode==='Offline');assert.equal(Boolean(p.input('Meeting link')),mode!=='Offline');
 await p.submit();const body=JSON.parse(p.calls.find(x=>x.method==='POST').body);
 assert.equal(body.deliveryMode,mode);assert.equal(body.venue,mode==='Offline'?'Studio A':null);assert.equal(body.meetingLink,mode==='Offline'?null:'https://meeting.example.invalid/makeup');assert.equal(body.useNextScheduledClass,false);assert.equal(body.startUtc,'2026-10-31T03:30:00.000Z');
});
for(const previous of ['Offline','Online','Hybrid'])test(`Next scheduled mode ignores hidden ${previous} fields and lets server inherit`,async()=>{
 const p=await page();p.control('delivery-mode').props.onChange(previous);p.control('scheduling-mode').props.onChange('NextScheduled');
 assert.equal(p.control('delivery-mode'),undefined);assert.equal(p.input('Meeting link'),undefined);assert.equal(p.input('Room or venue (optional)'),undefined);
 await p.submit();const body=JSON.parse(p.calls.find(x=>x.method==='POST').body);assert.deepEqual([body.deliveryMode,body.venue,body.meetingLink,body.startUtc,body.useNextScheduledClass],['Offline',null,null,null,true]);
});
for(const mode of ['Online','Hybrid'])test(`Manual ${mode} missing link still stops submit`,async()=>{const p=await page();p.control('makeup-date').props.onChange('2026-10-31');p.control('delivery-mode').props.onChange(mode);await p.submit();assert.equal(p.calls.filter(x=>x.method==='POST').length,0);assert.match(text(p.render()),/meeting link is required/);});
test('Manual Offline without room remains optional',async()=>{const p=await page();p.control('makeup-date').props.onChange('2026-10-31');await p.submit();assert.equal(JSON.parse(p.calls.find(x=>x.method==='POST').body).venue,null);});
test('Return from inherited mode restores manual fields and manual link requirement',async()=>{const p=await page();p.control('delivery-mode').props.onChange('Hybrid');p.control('scheduling-mode').props.onChange('NextScheduled');p.control('scheduling-mode').props.onChange('Manual');p.control('makeup-date').props.onChange('2026-10-31');assert.ok(p.input('Meeting link'));await p.submit();assert.equal(p.calls.filter(x=>x.method==='POST').length,0);});
test('Directory displays separate Offline room and Online/Hybrid meeting links',async()=>{const rows=['Offline','Online','Hybrid'].map((deliveryMode,i)=>({id:String(i),studentId:'student',batchId:'batch',startUtc:'2026-10-31T03:30:00Z',deliveryMode,venue:deliveryMode==='Offline'?'Studio A':null,meetingLink:deliveryMode==='Offline'?null:'https://meeting.example.invalid/'+deliveryMode,usesNextScheduledClass:true,status:'Scheduled'}));const p=await page({rows});const content=text(p.render());for(const x of ['Studio A','https://meeting.example.invalid/Online','https://meeting.example.invalid/Hybrid'])assert.ok(content.includes(x));});
if(process.env.QA_MAKEUP_SQL==='1')test('Actual directory renders captured HTTP/SQL mode-specific locations',async()=>{const log=fs.readFileSync('QA/EVIDENCE/logs/phase-2b-makeup-location-sql.log','utf8'),fixture=JSON.parse(log.match(/^MAKEUPLOCATION FIXTURE (.+)$/m)[1]);const p=await page({rows:fixture.makeups});for(const row of fixture.makeups){assert.ok(text(p.render()).includes(row.deliveryMode));const location=row.deliveryMode==='Offline'?row.venue:row.meetingLink;if(location)assert.ok(text(p.render()).includes(location));}});
