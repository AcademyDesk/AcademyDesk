// Actual FinancePolicy TSX handler, synthetic hooks/transport; not SQL/browser proof.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),cp=require('node:child_process');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const file='apps/web/src/app/finance-policy/page.tsx';
const source=process.env.QA_SOURCE_REF?cp.execFileSync('git',['show',process.env.QA_SOURCE_REF+':'+file],{encoding:'utf8'}):fs.readFileSync(file,'utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const nodes=n=>Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];
const base={taxRegistrationNumber:'Synthetic',taxLabel:'GST',taxRatePercent:18,defaultPaymentTermsDays:7,taxInclusivePricing:false,invoiceLogoUrl:null,invoiceAuthorityName:'Synthetic signatory',invoiceAuthorityTitle:'Finance',invoiceSignatureUrl:null,invoiceTemplateKey:'Modern',settingsVersion:'A'.repeat(64)};
async function page(theme,rejected=false,reason){let index=0;const state=[],effects=[],calls=[],module={exports:{}};
 const pending={current:false};const react={useRef:()=>pending,useState:v=>{const k=index++;if(!(k in state))state[k]=v;return[state[k],v=>state[k]=v];},useEffect:f=>effects.push(f)};
 const api=async(url,init={})=>{calls.push({url,...init});return{ok:!init.method||!rejected,status:init.method&&rejected?400:200,json:async()=>init.method?(rejected?{message:reason}:JSON.parse(init.body)):url==='/api/academies'?[{id:'owned'}]:{...base,payslipTemplateKey:theme}};};
 function FixtureFormData(){const data=new FormData();for(const [k,v]of Object.entries({...base,taxLabel:'Edited tax label'})){if(v!==null&&k!=='taxInclusivePricing')data.set(k,String(v));}return data;}
 new Function('require','module','exports','FormData',code)(n=>n==='react'?react:n==='react/jsx-runtime'?jsx:n==='@/lib/api'?{academyApi:api,apiHeaders:()=>({'Content-Type':'application/json'})}:(()=>{throw Error(n)})(),module,module.exports,FixtureFormData);
 const render=()=>{index=0;return module.exports.default();};render();effects.splice(0).forEach(f=>f());await new Promise(r=>setImmediate(r));
 return{state,calls,render,save:()=>nodes(render()).find(n=>n.type==='form').props.onSubmit({preventDefault(){},currentTarget:{}})};
}
for(const reason of['Tax label must not exceed 30 characters. Your draft has not been saved.','Tax registration number must not exceed 50 characters. Your draft has not been saved.',null,{unexpected:true},'Untrusted server detail'])test('400 reason is allowlisted and settings retained: '+JSON.stringify(reason),async()=>{
 const p=await page('Professional',true,reason);await p.save();assert.equal(p.state[2],typeof reason==='string'&&reason.startsWith('Tax ')?reason:'Finance policy could not be saved.');assert.equal(p.state[1].settingsVersion,base.settingsVersion);assert.equal(p.state[1].taxLabel,'GST');assert.equal(p.state[1].payslipTemplateKey,'Professional');assert.equal(p.calls.filter(x=>x.method==='PUT').length,1);assert.equal(p.state[3],false);
});
for(const theme of['Standard','Compact','Professional']){
 test(theme+': policy edit preserves hidden payslip theme and exact visible payload',async()=>{const p=await page(theme);await p.save();const writes=p.calls.filter(x=>x.method==='PUT');assert.equal(writes.length,1);assert.equal(writes[0].url,'/api/academies/owned/finance-governance/settings');assert.deepEqual(JSON.parse(writes[0].body),{...base,taxLabel:'Edited tax label',payslipTemplateKey:theme});assert.equal(p.state[1].payslipTemplateKey,theme);assert.equal(p.state[2],'Finance policy saved.');assert.equal(nodes(p.render()).filter(n=>n.props?.name==='payslipTemplateKey').length,0);});
 test(theme+': rejected write preserves loaded settings and reports no success',async()=>{const p=await page(theme,true);await p.save();assert.equal(p.state[1].payslipTemplateKey,theme);assert.equal(p.state[1].taxLabel,'GST');assert.equal(p.state[2],'Finance policy could not be saved.');assert.equal(JSON.parse(p.calls.find(x=>x.method==='PUT').body).payslipTemplateKey,theme);});
}
