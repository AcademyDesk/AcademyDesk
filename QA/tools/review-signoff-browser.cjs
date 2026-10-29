'use strict';
// Actual exported page with controlled replies: frontend acceptance, not SQL/RBAC proof.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),a=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),academy='11111111-1111-4111-8111-111111111111',actor='33333333-3333-4333-8333-333333333333';
async function run(){
 a.equal(process.argv.length,3);const ui=process.argv[2],verified=require('./review-signoff-ui-export.cjs').verify(ui),run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/review-signoff-browser-'+run);
 fs.mkdirSync(evidence,{recursive:true});const result={run,ui,webBuild:verified.build,verifiedExportFiles:627,passed:false,apiResponseMocks:true,syntheticToken:true,actualSql:false,serverAuthorizationTested:false,physicalDevicesTested:false,productionApproved:false,cases:[]},save=()=>fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));save();
 const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));const file=require('./api-image-payment-browser.cjs').staticFile(verified.web,new URL(req.url,'http://localhost').pathname);res.writeHead(200,{'content-type':({'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.svg':'image/svg+xml','.woff2':'font/woff2','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;
 try{
  browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of[320,390,768,1440])for(const theme of['light','dark']){
   const context=await browser.newContext({viewport:{width,height:900},reducedMotion:'reduce',timezoneId:'Asia/Kolkata'}),page=await context.newPage(),errors=[],writes=[],rows=[];let mode='valid',readFail=false,phase='initial',release,held;
   try{
    await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-signoff-token');},theme);
    page.on('pageerror',()=>errors.push('uncaught signoff error'));
    await context.route('**/*',async route=>{
     const req=route.request(),u=new URL(req.url());if(!u.pathname.startsWith('/api/')){if(u.origin===origin||u.protocol==='data:')return route.continue();errors.push('external request refused');return route.abort();}
     if(!['localhost','127.0.0.1'].includes(u.hostname))return route.abort();
     const reply=(status,body)=>route.fulfill({status,contentType:'application/json',headers:{'access-control-allow-origin':origin},body:JSON.stringify(body)});
     if(req.method()==='POST'){
      a.equal(u.pathname,'/api/academies/'+academy+'/access-reviews');const payload=req.postDataJSON();writes.push(payload);
      if(mode==='400')return reply(400,{message:'Synthetic validation refusal'});
      if(mode==='500')return reply(500,{message:'Synthetic unknown outcome'});
      if(mode==='malformed')return reply(200,{id:'not-a-receipt'});
      const receipt={id:crypto.randomUUID(),reviewedAtUtc:'2026-10-26T00:00:00Z',reviewerUserId:actor,followUpId:payload.createFollowUp?crypto.randomUUID():null};rows.push({...receipt,reviewedAtUtc:receipt.reviewedAtUtc.slice(0,-1),notes:payload.notes});
      return reply(200,receipt);
     }
     a.equal(req.method(),'GET');
     if(u.pathname==='/api/academies/'+academy+'/access-reviews'){if(held)await held;if(readFail)return reply(503,{message:'Synthetic history unavailable'});return reply(200,rows);}
     const data={'/api/auth/session':{academyId:academy,displayName:'Synthetic Reviewer',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/academies':[{id:academy,name:'Synthetic Review Academy',enabledModulesJson:'["Core","AccessGovernance"]'}],'/api/portal/announcements':[]};
     return reply(u.pathname.endsWith('/chat/health')?503:200,data[u.pathname]??[]);
    });
    await page.goto(origin+'/access-review/sign-off');
    const notes=page.getByRole('textbox',{name:'Review notes',exact:true}),submit=page.getByRole('button',{name:'Record sign-off',exact:true}),refresh=page.getByRole('button',{name:'Refresh review history',exact:true}),notice=page.locator('main.enterprise-settings [role="status"]');
    const record=async()=>{const reply=page.waitForResponse(r=>r.request().method()==='POST'&&new URL(r.url()).pathname==='/api/academies/'+academy+'/access-reviews');await submit.click();return(await reply).status();};
    const reread=async(status)=>{const reply=page.waitForResponse(r=>r.request().method()==='GET'&&new URL(r.url()).pathname==='/api/academies/'+academy+'/access-reviews');await refresh.click();a.equal((await reply).status(),status);await page.waitForFunction(()=>{const button=[...document.querySelectorAll('main.enterprise-settings button')].find(b=>b.textContent==='Refresh review history');return button&&!button.disabled;});};
    await submit.waitFor();await page.waitForFunction(()=>!document.querySelector('main.enterprise-settings fieldset')?.disabled);
    await notes.fill('Synthetic review '+width+' '+theme);await page.getByRole('checkbox',{name:'Create high-priority remediation task'}).check();
    a.ok((await refresh.boundingBox()).height>=44);a.ok((await submit.boundingBox()).height>=44);
    phase='known refusal';mode='400';a.equal(await record(),400);await notice.filter({hasText:'entries have been retained'}).waitFor();a.equal(await notes.inputValue(),'Synthetic review '+width+' '+theme);a.equal(await submit.isEnabled(),true);
    phase='uncertain write';mode='500';a.equal(await record(),500);await notice.filter({hasText:'could not be confirmed'}).waitFor();a.equal(await submit.isDisabled(),true);a.equal(writes.length,2);a.equal(await notes.inputValue(),'Synthetic review '+width+' '+theme);
    phase='failed manual recovery';readFail=true;await reread(503);await notice.filter({hasText:'could not be refreshed'}).waitFor();a.equal(await submit.isDisabled(),true);a.equal(writes.length,2);
    readFail=false;await reread(200);await notice.filter({hasText:/^Review history refreshed\. Check the register/}).waitFor();a.equal(await submit.isEnabled(),true);a.equal(writes.length,2);
    phase='malformed success';mode='malformed';a.equal(await record(),200);await notice.filter({hasText:'could not be confirmed'}).waitFor();a.equal(await submit.isDisabled(),true);a.equal(await notes.inputValue(),'Synthetic review '+width+' '+theme);await reread(200);await notice.filter({hasText:/^Review history refreshed\. Check the register/}).waitFor();a.equal(await submit.isEnabled(),true);
    phase='confirmed receipt and followup';mode='valid';held=new Promise(r=>release=r);a.equal(await record(),200);await notice.filter({hasText:'Access review signed off.'}).waitFor();a.equal(await notes.inputValue(),'');a.equal(await page.getByRole('checkbox',{name:'Create high-priority remediation task'}).isChecked(),false);a.equal(await page.getByRole('button',{name:'Recording…',exact:true}).isDisabled(),true);release();held=null;await submit.waitFor();a.equal(await submit.isEnabled(),true);a.equal(rows.length,1);a.equal(writes.length,4);a.equal(rows[0].followUpId!==null,true);
    phase='saved but read unavailable';await notes.fill('Second synthetic review');readFail=true;a.equal(await record(),200);await notice.filter({hasText:'Access review signed off. Review history could not be refreshed'}).waitFor();a.equal(await submit.isDisabled(),true);a.equal(await notes.inputValue(),'');a.equal(rows.length,2);a.equal(rows[1].followUpId,null);a.equal(writes.length,5);
    readFail=false;await reread(200);await notice.filter({hasText:/^Review history refreshed\. Check the register/}).waitFor();a.equal(await submit.isEnabled(),true);a.equal(writes.length,5);a.equal(await notice.getAttribute('aria-live'),'polite');
    a.equal(await page.locator('main.enterprise-settings b').filter({hasText:'5:30:00'}).count(),2);a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false);a.deepEqual(errors,[]);
    await page.screenshot({path:path.join(evidence,'signoff-'+width+'-'+theme+'.png'),fullPage:true});
    result.cases.push({width,theme,knownRefusalRetainsDraft:true,uncertainLocksRepeat:true,failedRefreshRetainsLock:true,manualReadOnlyRecovery:true,malformedReceiptRejected:true,confirmedReceiptResetsOnce:true,followupChoicePreserved:true,readbackPendingLocked:true,savedReadFailureKeepsSuccess:true,politeNotice:true,sqlHistoryUtcPreserved:true,recoveryTouchTarget:true,signoffTouchTarget:true,writes:5,savedRows:2,noHorizontalOverflow:true,pageErrors:0});save();
   }catch(error){result.failedCase={width,theme,phase,type:error.name,sourceLines:[...String(error.stack||'').matchAll(/review-signoff-browser\.cjs:(\d+):(\d+)/g)].map(m=>({line:Number(m[1]),column:Number(m[2])}))};await page.screenshot({path:path.join(evidence,'failed.png'),fullPage:true}).catch(()=>{});throw error;}finally{if(release)release();await context.close();}
  }
  a.equal(result.cases.length,8);require('./review-signoff-ui-export.cjs').verify(ui);result.passed=true;
 }catch{result.failure='Signoff frontend journey failed; inspect bounded retained evidence.';process.exitCode=1;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));save();console.log(JSON.stringify({run,passed:result.passed,journeys:result.cases.length,evidence,serverAuthorizationTested:false,productionApproved:false}));}
}
if(require.main===module)run().catch(()=>{console.error('Signoff frontend preflight refused.');process.exitCode=1;});
