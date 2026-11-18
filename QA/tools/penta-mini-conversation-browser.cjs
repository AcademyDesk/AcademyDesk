// Actual React/export in Chromium; synthetic host receipts, no model or SQL.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { start, sleep } = require('./penta-browser-harness.cjs');
const root = path.resolve(__dirname, '../..'), id = n => String(n).padStart(8, '0') + '-1111-4111-8111-111111111111';
const academy = id(1), conversation = id(2), mira = id(3), leo = id(4), expiry = '2030-01-01T00:00:00Z';
const prefix = '/api/academies/' + academy + '/penta', base = prefix + '/chat';
const contains = text => `document.body?.innerText.includes(${JSON.stringify(text)})`;
const input = '#private-mini-prompt', pointerKey = 'academydesk.penta.mini-read-recovery.v1';
const enabled = process.env.QA_MINI_EVALUATION_EXPECTED !== 'false';
const variants = enabled ? ['start-only','result','clarification','empty','refusal','error','unsupported','health-unavailable','health-degraded',
 'health-malformed','health-denied','malformed','foreign-request','wrong-version','private-extra','lost-recover','lost-reload','offline-recovery',
 'pending-recovery','missing-recovery','expired-recovery','storage-blocked','session-clear','duplicate-submit','create-malformed','create-lost','html-label'] : ['default-off'];
const rawLabel = '<img src=x onerror="window.__c54HtmlExecuted=true">';
function receipt(requestId, version, scenario, second = false) {
 const noRows = ['refusal','error','unsupported'].includes(scenario), selected = second ? leo : mira;
 const row = value => ({sourceId:value,displayName:scenario==='html-label'?rawLabel:value===leo?'C54 Leo':'C54 Mira',recordCode:value===leo?'SYN-2':'SYN-1',subjects:['Piano'],balances:[{currency:'USD',outstanding:450.01}],sourcePath:'/student-management?studentId='+value});
 const rows = scenario === 'empty' ? [] : scenario === 'clarification' && !second ? [row(mira),row(leo)] : [row(selected)];
 return {conversationId:conversation,requestId,version,kind:scenario==='refusal'?'REFUSAL':scenario==='error'?'ERROR':scenario==='unsupported'?'UNSUPPORTED':scenario==='clarification'&&!second?'CLARIFICATION_REQUIRED':'RESULT',
  message:noRows?'This read-only pilot did not execute that operation.':scenario==='clarification'&&!second?'Choose the current learner.':'C54 verified learner response.',capability:'executor',provider:'PENTA Mini',protocol:'0.1',
  context:{filters:{},sort_by:null,current_learner_id:second?leo:null,current_result_ids:noRows?[]:rows.map(r=>r.sourceId)},
  result:noRows?null:{count:rows.length,hasMore:false,rows,asOfUtc:'2026-11-18T09:00:00Z',source:'Invoices',balanceScope:'All outstanding fees'}};
}
async function main() {
 const browser = await start({academyId:academy,evidence:path.join(root,'.build-check/penta-mini-conversation-browser'),prefix:'c54-mini-'}), results=[];
 let failure=null,cleanup;
 const matrix=['AcademyAdmin','Owner'].flatMap(role=>[1280,320].flatMap(width=>variants.map(scenario=>({role,width,scenario}))));
 if(enabled)for(const role of ['FinanceUser','Teacher','Student','Guardian','PlatformOwner'])for(const width of [1280,320])matrix.push({role,width,scenario:'role-excluded'});
 const filter=process.env.QA_C54_CASE_FILTER?new RegExp(process.env.QA_C54_CASE_FILTER):null;
 const planned=matrix.filter(x=>!filter||filter.test(x.role+'/'+x.width+'/'+x.scenario));a.ok(planned.length);
 try {
  for(const {role,width,scenario} of planned){
   let p,error,created=0,turns=0,gets=0,health=0,held=null,release;
   const stored=new Map(),name=role+'/'+width+'/'+scenario;
   const bootstrap=scenario==='storage-blocked'?`(()=>{const old=Storage.prototype.setItem;Storage.prototype.setItem=function(k,v){if(k===${JSON.stringify(pointerKey)})throw new DOMException('Synthetic storage refusal','QuotaExceededError');return old.call(this,k,v);};})()`:'';
   const actor=role==='Teacher'?{role,workspace:'Teacher',route:'/teacher/ai',manual:'/teacher'}:['Student','Guardian'].includes(role)?{role,workspace:'Portal',route:'/portal/ai',manual:'/portal'}:role==='PlatformOwner'?{role,workspace:'Platform',route:'/platform/ai',manual:'/platform'}:{role,workspace:'AcademyAdmin',route:'/penta',manual:'/dashboard'};
   try{
    p=await browser.page(actor,width,async req=>{
     if(scenario==='role-excluded'&&req.method==='GET'&&['/api/teacher/penta/attendance/readiness','/api/portal/penta/learners','/api/platform/penta/readiness'].includes(req.path))return {status:404};
     if(req.path===prefix+'/lookup-readiness')return {body:{protocol:'academy-lookup.v1',academyId:academy}};
     if([base+'/finance',prefix+'/class-scheduling/browser/readiness'].includes(req.path))return {status:404};
     if(req.path===base+'/health'){
      health++;a.equal(req.method,'GET');a.ok(enabled,'Default-off build must not contact Mini');
      if(scenario==='health-denied')return {status:403};
      if(scenario==='health-malformed')return {body:{status:'Available'}};
      return {body:{status:scenario==='health-unavailable'||scenario==='offline-recovery'&&health>1?'Unavailable':scenario==='health-degraded'?'Degraded':'Available',provider:'PENTA Mini',protocol:'0.1',readOnly:true}};
     }
     if(req.path===base+'/conversations'){
      a.equal(req.method,'POST');a.deepEqual(req.body,{});created++;
      if(scenario==='create-lost')return {fail:'ConnectionReset'};
      return {status:201,body:{conversationId:scenario==='create-malformed'?'00000000-0000-0000-0000-000000000000':conversation,version:0,expiresAtUtc:expiry}};
     }
     if(req.path===base+'/conversations/'+conversation+'/turns'){
      a.equal(req.method,'POST');turns++;a.equal(req.body.capability,'executor');a.equal(req.body.expectedVersion,turns-1);a.deepEqual(Object.keys(req.body).sort(),['capability','expectedVersion','requestId','text']);
      if(turns===2)a.equal(req.body.text,'Open result 2');else a.equal(req.body.text,'Find learners with pending fees');
      const value=receipt(req.body.requestId,turns,scenario,turns===2);stored.set(req.body.requestId,value);
      if(['lost-recover','lost-reload','offline-recovery','pending-recovery','missing-recovery','expired-recovery'].includes(scenario))return {fail:'ConnectionReset'};
      if(scenario==='duplicate-submit')await new Promise(resolve=>{const timer=setTimeout(resolve,8000);release=()=>{clearTimeout(timer);resolve();};held=true;});
      const data=JSON.parse(JSON.stringify(value));
      if(scenario==='malformed')data.result.rows[0].sourcePath='/platform';
      if(scenario==='foreign-request')data.requestId=id(99);
      if(scenario==='wrong-version')data.version=9;
      if(scenario==='private-extra')data.result.rows[0].privateContact='must not render';
      return {status:201,body:data};
     }
     const match=new RegExp('^'+base+'/conversations/'+conversation+'/turns/([^/]+)$').exec(req.path);
     if(match){a.equal(req.method,'GET');gets++;a.ok(stored.has(match[1]));return {status:scenario==='pending-recovery'?409:scenario==='missing-recovery'?404:scenario==='expired-recovery'?410:200,body:stored.get(match[1])};}
    },bootstrap);
    if(scenario==='role-excluded')await p.wait(role==='PlatformOwner'?contains('Platform conversation is unavailable'):`document.querySelector('#penta-role-status')?.textContent.includes('conversation is not available')`);
    else await p.wait(`Boolean(document.querySelector('#finance-chat-prompt'))`);
    if(scenario==='role-excluded'){a.equal(await p.evaluate(`Boolean(document.querySelector('#mini-evaluation-title'))`),false);a.equal(health+created+turns,0);}
    else if(!enabled){a.equal(await p.evaluate(`Boolean(document.querySelector('#mini-evaluation-title'))`),false);a.equal(health,0);}
    else{
     await p.wait(`Boolean(document.querySelector('#mini-evaluation-title'))`);
     if(scenario.startsWith('health-')){
      await p.wait(contains(scenario==='health-denied'?'This session changed':'Private Mini is unavailable'));
      a.equal(await p.evaluate(`(${p.button('Start Mini conversation')}).disabled`),true);a.equal(created+turns,0);
     }else{
      await p.click('Start Mini conversation');
      if(scenario.startsWith('create-')){await p.wait(contains('Private Mini is unavailable or could not verify this response'));a.equal(turns,0);a.equal(await p.evaluate(`Boolean(document.querySelector('${input}'))`),false);}
      else{
       await p.wait(`Boolean(document.querySelector('${input}'))`);a.equal(created,1);a.equal(turns,0);
       if(scenario!=='start-only'){
        await p.submit(input,'Find learners with pending fees','Send Mini request');
        if(scenario==='storage-blocked'){await p.wait(contains('No Mini prompt was sent'));a.equal(turns,0);a.equal(await p.evaluate(`document.querySelector('${input}').value`),'Find learners with pending fees');}
        else if(['session-clear','duplicate-submit'].includes(scenario)){
         if(scenario==='duplicate-submit'){for(let i=0;i<100&&!held;i++)await sleep(25);a.ok(held);}
         if(scenario==='session-clear'){
          await p.wait(contains('C54 verified learner response.'));await p.evaluate(`localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','replacement');window.dispatchEvent(new Event('focus'))`);await p.wait(contains('Session ended'));
          a.equal(await p.evaluate(`Boolean(document.querySelector('section[aria-label="Verified Mini response"]'))`),false);
         }else{await p.evaluate(`(()=>{const f=document.querySelector('${input}')?.closest('form');if(f)for(let i=0;i<5;i++)f.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));})()`);release();await p.wait(contains('C54 verified learner response.'));a.equal(turns,1);}
        }else if(['malformed','foreign-request','wrong-version','private-extra','lost-recover','lost-reload','offline-recovery','pending-recovery','missing-recovery','expired-recovery'].includes(scenario)){
         await p.wait(contains('The response is unconfirmed'));a.equal(await p.evaluate(`Boolean(document.querySelector('section[aria-label="Verified Mini response"]'))`),false);
         const before=p.requests.length;
         if(['lost-reload','offline-recovery'].includes(scenario))await p.reload();else await p.click('Check saved Mini response');
         if(['pending-recovery','missing-recovery','expired-recovery'].includes(scenario))await p.wait(contains('The response is unconfirmed'));
         else await p.wait(contains('C54 verified learner response.'));
         a.ok(p.requests.slice(before).every(r=>r.method==='GET'));a.equal(turns,1);a.equal(created,1);
         if(scenario==='offline-recovery'){a.equal(await p.evaluate(`(${p.button('Start separate Mini conversation')}).disabled`),true);a.equal(await p.evaluate(`(${p.button('Ask about result 1')}).disabled`),true);}
        }else{
         await p.wait(`Boolean(document.querySelector('section[aria-label="Verified Mini response"]'))`);
         if(scenario==='clarification'){await p.wait(contains('Clarification needed'));await p.click('Ask about result 2');await p.wait(contains('Verified learner read'));a.equal(await p.evaluate(contains('C54 Leo')),true);a.equal(await p.evaluate(contains('C54 Mira')),false);a.equal(turns,2);}
         else if(scenario==='empty')await p.wait(contains('No matching learners'));
         else if(['refusal','error','unsupported'].includes(scenario))await p.wait(contains('This read-only pilot did not execute'));
         else await p.wait(contains(scenario==='html-label'?rawLabel:'C54 Mira'));
         a.equal(await p.evaluate('Boolean(window.__c54HtmlExecuted)'),false);
         a.equal(await p.evaluate(`document.querySelector('section[aria-label="Verified Mini response"]')?.textContent.includes('450.01')`),false,'No approximate financial display');
         if(scenario==='result')await p.screenshot(role+'-'+width+'-mini');
        }
        if(turns){const raw=await p.evaluate(`sessionStorage.getItem(${JSON.stringify(pointerKey)})`);if(raw){a.doesNotMatch(raw,/Find learners|C54 Mira|450\.01/);a.equal(raw.includes('synthetic-c52-'+role),false);a.deepEqual(Object.keys(JSON.parse(raw)).sort(),['academyId','conversationId','epoch','expectedVersion','expiresAtUtc','requestId']);}}
       }
      }
     }
    }
    if(scenario!=='session-clear')a.equal(await p.evaluate(`Boolean(document.querySelector('a[href="${actor.manual}"]'))`),true);
    await p.assertClean();
   }catch(e){error=e.message;if(p)fs.writeFileSync(path.join(browser.run,name.replaceAll('/','-')+'.txt'),await p.text().catch(()=>''));}
   finally{release?.();if(p)try{await p.close();}catch(e){error=(error||'')+' Cleanup: '+e.message;}}
   results.push({case:name,pass:!error,error,created,turns,gets,health,requests:p?.requests,errors:p?.errors});fs.writeFileSync(path.join(browser.run,'progress.json'),JSON.stringify(results,null,2));console.log(name+': '+(error?'FAIL '+error:'PASS'));
  }
  a.equal(results.length,planned.length);a.equal(results.filter(x=>!x.pass).length,0);
 }catch(e){failure=e.message;}finally{
  cleanup=await browser.close();const report={status:failure||cleanup.cleanupError?'FAIL':'PASS',failure,...cleanup,evaluationBuild:enabled,filter:process.env.QA_C54_CASE_FILTER||null,cases:results.length,passed:results.filter(x=>x.pass).length,failed:results.filter(x=>!x.pass).length,results,scope:'Actual browser with intercepted synthetic Mini host replies only; no model/SQL qualification.'};
  fs.writeFileSync(path.join(browser.run,'result.json'),JSON.stringify(report,null,2));console.log(JSON.stringify({status:report.status,cases:report.cases,passed:report.passed,failed:report.failed,evidence:browser.run}));if(failure||cleanup.cleanupError)throw Error(failure||cleanup.cleanupError);
 }
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
