// Actual exported UI with synthetic host decimal-string receipts. No model/SQL.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { start } = require('./penta-browser-harness.cjs');
const root = path.resolve(__dirname,'../..'), id = n => String(n).padStart(8,'0')+'-1111-4111-8111-111111111111';
const academy=id(1),conversation=id(2),learner=id(3),base='/api/academies/'+academy+'/penta',chat=base+'/chat';
const cases=['one-cent','zero','large','largest','mixed','empty','legacy','null-money','unsupported-precision','lost','reload','offline',
 'wrong-version','foreign-row','wrong-currency','numeric','noncanonical','overprecision','overflow','duplicate-row','extra-balance','private-field','money-on-refusal'];
const text = value => `document.body?.innerText.includes(${JSON.stringify(value)})`;
const clone = value => JSON.parse(JSON.stringify(value));
function value(requestId,scenario) {
 const amount=scenario==='one-cent'?'0.01':scenario==='zero'?'0.00':scenario==='large'?'9999999999999999.99':scenario==='largest'?'79228162514264337593543950335.00':'450.01';
 const balances=scenario==='empty'?[]:[{currency:'USD',outstandingExact:amount},...(scenario==='mixed'?[{currency:'INR',outstandingExact:'1000000000000000000000.99'}]:[])];
 // This Number simulates the retained legacy JSON value. Exact strings are
 // independent fixture literals and are never generated from that number.
 const numeric=balances.map(b=>({currency:b.currency,outstanding:Number(b.outstandingExact)}));
 if(scenario==='unsupported-precision')numeric[0].outstanding=1.001;
 const receipt={conversationId:conversation,requestId,version:1,kind:'RESULT',message:'C55 verified fee snapshot.',capability:'executor',provider:'PENTA Mini',protocol:'0.1',
  context:{filters:{},sort_by:null,current_learner_id:null,current_result_ids:[learner]},result:{count:1,hasMore:false,rows:[{sourceId:learner,displayName:'C55 Mira',recordCode:'SYN-C55',subjects:['Piano'],balances:numeric,sourcePath:'/student-management?studentId='+learner}],asOfUtc:'2026-11-19T09:00:00Z',source:'Invoices',balanceScope:'All outstanding fees for each learner; currencies are separate.'}};
 if(!['legacy','unsupported-precision'].includes(scenario))receipt.exactMoney=scenario==='null-money'?null:{version:'mini-exact-money.v1',rows:[{sourceId:learner,balances}]};
 return {receipt,balances};
}
async function main(){
 const browser=await start({academyId:academy,evidence:path.join(root,'.build-check/penta-mini-exact-money-browser'),prefix:'c55-money-'}),results=[];
 let failure=null,cleanup;
 const filter=process.env.QA_C55_CASE_FILTER?new RegExp(process.env.QA_C55_CASE_FILTER):null;
 const planned=['AcademyAdmin','Owner'].flatMap(role=>[1280,320].flatMap(width=>cases.map(scenario=>({role,width,scenario,name:role+'/'+width+'/'+scenario})))).filter(x=>!filter||filter.test(x.name));a.ok(planned.length);
 try{
  for(const {role,width,scenario,name}of planned){
   let p,error,turns=0,gets=0,created=0,health=0,saved,expected;
   try{
    p=await browser.page({role,workspace:'AcademyAdmin',route:'/penta',manual:'/dashboard'},width,req=>{
     if(req.path===base+'/lookup-readiness')return {body:{protocol:'academy-lookup.v1',academyId:academy}};
     if([chat+'/finance',base+'/class-scheduling/browser/readiness'].includes(req.path))return {status:404};
     if(req.path===chat+'/health'){a.equal(req.method,'GET');health++;return {body:{status:scenario==='offline'&&health>1?'Unavailable':'Available',provider:'PENTA Mini',protocol:'0.1',readOnly:true}};}
     if(req.path===chat+'/conversations'){a.equal(req.method,'POST');created++;return {status:201,body:{conversationId:conversation,version:0,expiresAtUtc:'2030-01-01T00:00:00Z'}};}
     if(req.path===chat+'/conversations/'+conversation+'/turns'){
      a.equal(req.method,'POST');a.deepEqual(Object.keys(req.body).sort(),['capability','expectedVersion','requestId','text']);a.equal(req.body.text,'Show pending fees');a.equal(req.body.capability,'executor');a.equal(req.body.expectedVersion,0);turns++;
      const v=value(req.body.requestId,scenario);saved=clone(v.receipt);expected=v.balances;
      if(['lost','reload','offline'].includes(scenario))return {fail:'ConnectionReset'};
      const body=clone(saved),m=body.exactMoney;
      if(scenario==='wrong-version')m.version='unqualified.v9';
      if(scenario==='foreign-row')m.rows[0].sourceId=id(99);
      if(scenario==='wrong-currency')m.rows[0].balances[0].currency='INR';
      if(scenario==='numeric')m.rows[0].balances[0].outstandingExact=450.01;
      if(scenario==='noncanonical')m.rows[0].balances[0].outstandingExact='0450.01';
      if(scenario==='overprecision')m.rows[0].balances[0].outstandingExact='450.011';
      if(scenario==='overflow')m.rows[0].balances[0].outstandingExact='79228162514264337593543950336.00';
      if(scenario==='duplicate-row')m.rows.push(clone(m.rows[0]));
      if(scenario==='extra-balance')m.rows[0].balances.push(clone(m.rows[0].balances[0]));
      if(scenario==='private-field')m.rows[0].privateContact='synthetic private value';
      if(scenario==='money-on-refusal'){body.kind='REFUSAL';body.result=null;}
      return {status:201,body};
     }
     if(req.path===chat+'/conversations/'+conversation+'/turns/'+saved?.requestId){a.equal(req.method,'GET');gets++;return {body:saved};}
    });
    await p.wait(`Boolean(document.querySelector('#mini-evaluation-title'))`);await p.click('Start Mini conversation');
    await p.wait(`Boolean(document.querySelector('#private-mini-prompt'))`);a.equal(turns,0);
    await p.submit('#private-mini-prompt','Show pending fees','Send Mini request');
    const malformed=['wrong-version','foreign-row','wrong-currency','numeric','noncanonical','overprecision','overflow','duplicate-row','extra-balance','private-field','money-on-refusal'].includes(scenario);
    if(malformed||['lost','reload','offline'].includes(scenario)){
     await p.wait(text('The response is unconfirmed'));a.equal(await p.evaluate(`Boolean(document.querySelector('section[aria-label="Verified Mini response"]'))`),false);
     const before=p.requests.length;
     if(['reload','offline'].includes(scenario))await p.reload();else await p.click('Check saved Mini response');
     await p.wait(text('C55 verified fee snapshot.'));a.ok(p.requests.slice(before).every(x=>x.method==='GET'));a.equal(gets,1);
    }else await p.wait(text('C55 verified fee snapshot.'));
    if(['legacy','null-money','unsupported-precision'].includes(scenario)){
     await p.wait(text('Exact amounts are unavailable in this response'));
     a.equal(await p.evaluate(`Boolean(document.querySelector('ul[aria-label="Exact outstanding balances for C55 Mira"]'))`),false);
     a.equal(await p.evaluate(`document.querySelector('section[aria-label="Verified Mini response"]').textContent.includes('USD 1.00')`),false);
    }else if(scenario==='empty')await p.wait(text('No outstanding balances in this snapshot'));
    else for(const balance of expected){
     a.equal(await p.evaluate(`document.querySelector('ul[aria-label="Exact outstanding balances for C55 Mira"]')?.textContent.includes(${JSON.stringify(balance.currency+' '+balance.outstandingExact)})`),true,'Exact host decimal text without numeric conversion');
    }
    if(scenario==='offline')a.equal(await p.evaluate(`(${p.button('Ask about result 1')}).disabled`),true);
    const pointer=await p.evaluate(`JSON.parse(sessionStorage.getItem('academydesk.penta.mini-read-recovery.v1'))`);a.deepEqual(Object.keys(pointer).sort(),['academyId','conversationId','epoch','expectedVersion','expiresAtUtc','requestId']);
    a.equal(turns,1);a.equal(created,1);a.equal(await p.evaluate(`Boolean(document.querySelector('a[href="/dashboard"]'))`),true);await p.assertClean();
    if(['large','mixed'].includes(scenario))await p.screenshot(role+'-'+width+'-'+scenario);
   }catch(e){error=e.message;if(p)fs.writeFileSync(path.join(browser.run,name.replaceAll('/','-')+'.txt'),await p.text().catch(()=>''));}
   finally{if(p)try{await p.close();}catch(e){error=(error||'')+' Cleanup: '+e.message;}}
   results.push({case:name,pass:!error,error,created,turns,gets,health,requests:p?.requests,errors:p?.errors});fs.writeFileSync(path.join(browser.run,'progress.json'),JSON.stringify(results,null,2));console.log(name+': '+(error?'FAIL '+error:'PASS'));
  }
  a.equal(results.length,planned.length);a.equal(results.filter(x=>!x.pass).length,0);
 }catch(e){failure=e.message;}finally{
  cleanup=await browser.close();const result={status:failure||cleanup.cleanupError?'FAIL':'PASS',failure,...cleanup,cases:results.length,passed:results.filter(x=>x.pass).length,failed:results.filter(x=>!x.pass).length,filter:process.env.QA_C55_CASE_FILTER||null,results,scope:'Actual Chromium, synthetic versioned host money; no model, native SQL or production qualification.'};
  fs.writeFileSync(path.join(browser.run,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({status:result.status,cases:result.cases,passed:result.passed,failed:result.failed,evidence:browser.run}));if(failure||cleanup.cleanupError)throw Error(failure||cleanup.cleanupError);
 }
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
