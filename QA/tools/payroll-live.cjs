// Production static export -> loopback QA relay -> unchanged Identity/API -> owned SQL.
// No API mocks. Profile edits use real APIs because the current Payroll page has no edit UI.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api,academy,teacher,staff,nonce]=process.argv.slice(2),target=new URL(api);
assert.equal(target.hostname,'127.0.0.1');assert.equal(target.protocol,'http:');assert.ok(target.port);
for(const id of [academy,teacher,staff])assert.match(id,/^[a-f0-9-]{36}$/);assert.match(nonce,/^[a-f0-9]{32}$/);
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','payroll-live-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const server=http.createServer((req,res)=>{
 try{assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
  const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
  file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':({'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
 }catch{res.writeHead(404).end();}
});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  let i=0;
  for(const width of [320,1440])for(const theme of ['light','dark']){
   const index=i++,session=index%2===1,context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage();
   const login=await context.request.post(api+'/api/auth/login',{data:{email:'qa-admin-a@example.invalid',password:'Synthetic!39Ab'}});assert.equal(login.status(),200);const token=(await login.json()).accessToken;
   const headers={Authorization:'Bearer '+token},base=`/api/academies/${academy}/payroll`,errors=[],consoleErrors=[],writes=[],reads=[];
   const real=async(method,route,data,status=200)=>{const response=await context.request.fetch(api+route,{method,headers,data});assert.equal(response.status(),status,route);return status===204?null:response.json();};
   await context.addInitScript(({theme,token})=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);},{theme,token});
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')consoleErrors.push(m.text());});
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url());
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const response=await route.fetch({url:api+url.pathname+url.search});assert.notEqual(response.status(),429);
     if(req.method()==='POST'){assert.ok([base+'/profiles',base+'/payouts'].includes(url.pathname),'Unexpected UI write');writes.push({path:url.pathname,status:response.status(),body:req.postDataJSON(),result:await response.json()});}
     else if(url.pathname.startsWith(base)){assert.equal(response.status(),200);reads.push({path:url.pathname,body:await response.json()});}
     return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();errors.push('Unexpected external request');return route.abort('blockedbyclient');
   });
   const notice=page.locator('.payroll-notice'),profileForm=page.locator('form').nth(0),payoutForm=page.locator('form').nth(1);
   const choose=async(form,name,label)=>{await form.locator(`.standard-select-field:has(input[name="${name}"]) button`).click();await page.getByRole('option',{name:label,exact:true}).click();assert.equal(await page.getByRole('listbox').count(),0,'Menu should collapse');};
   const loaded=async()=>{await page.waitForFunction(()=>{const fields=document.querySelectorAll('.payroll-fields');return fields.length===2&&[...fields].every(f=>!f.disabled);});};
   const verifyRegister=async(stage,expected)=>{
    const rows=await real('GET',base+'/payouts');assert.ok(rows.length>=4);assert.ok(!rows.some(x=>x.payslipNumber==='QA-FOREIGN-HISTORY'));
    for(const row of rows){const tr=page.locator('.payroll-register tbody tr').filter({hasText:row.periodLabel}).filter({hasText:row.workerName});assert.ok(await tr.count()>0,stage+' missing row');}
    for(const old of expected||[]){assert.deepEqual(rows.find(x=>x.id===old.id),old);const tr=page.locator('.payroll-register tbody tr').filter({hasText:old.periodLabel});assert.equal(await tr.locator('td').nth(0).innerText(),old.workerName+'\n'+old.workerType);}
    const legacy=rows.filter(x=>x.identitySource==='LegacyUnverified');assert.equal(legacy.length,4);assert.ok(legacy.every(x=>x.workerName==='Historical identity not recorded'&&x.workerType==='Unknown (legacy)'));
    if(stage==='initial'){
     const cents=rows.filter(x=>x.netAmount===0.01);assert.equal(cents.length,2);
     for(const row of cents){const displayed=await page.locator('.payroll-register tbody tr').filter({hasText:row.periodLabel}).locator('td').nth(3).innerText();assert.equal(displayed,'₹0');}
     console.log(`PAYROLL MONEY OBSERVATION / NOT ACCEPTED: ${width}px/${theme}, two SQL0.01 payouts render ₹0. Separate precision repair required; no calculation or current acceptance assertion relaxed.`);
    }
    assert.equal(await page.locator('.payroll-kpis article').filter({hasText:'Recorded payouts'}).locator('b').innerText(),String(rows.length));
    assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'Page horizontal overflow');
    const table=page.locator('.payroll-table-wrap');assert.ok(await table.evaluate(el=>el.scrollWidth>=el.clientWidth));
    await page.screenshot({path:path.join(evidence,`${width}-${theme}-${stage}.png`),fullPage:true});checks.push({width,theme,stage,rows:rows.length});
   };
   await page.goto(origin+'/payroll');await loaded();const old=await real('GET',base+'/payouts');await verifyRegister('initial');
   const workers=await real('GET',`/api/academies/${academy}/${session?'staff':'teachers'}`),worker=workers.find(x=>x.id===(session?staff:teacher));assert.ok(worker);
   if(session)await choose(profileForm,'workerType','Staff');
   const name=session?worker.displayName:worker.firstName+' '+worker.lastName;await choose(profileForm,'workerId',name);
   if(session){await choose(profileForm,'paymentModel','Completed-session cycle');await profileForm.locator('[name=sessionsPerCycle]').fill('4');await profileForm.locator('[name=amountPerCycle]').fill('1000');}
   else await profileForm.locator('[name=monthlyAmount]').fill('1000');
   await profileForm.locator('.standard-date-trigger').click();await page.getByRole('dialog',{name:'Effective from calendar'}).getByRole('button',{name:'Today',exact:true}).click();
   await profileForm.getByRole('button',{name:'Save payout profile',exact:true}).click();await page.waitForFunction(()=>document.querySelector('form')?.getAttribute('aria-busy')==='true');
   await profileForm.evaluate(f=>f.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true})));await payoutForm.evaluate(f=>f.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true})));
   await notice.getByText('Payout profile saved.',{exact:true}).waitFor();await loaded();assert.equal(writes.length,1);assert.equal(writes[0].status,201);
   const profile=writes[0].result;assert.equal(profile.workerName,name);assert.equal(profile.workerType,session?'Staff':'Teacher');assert.equal(profile.staffUserId,session?staff:null);assert.equal(profile.teacherId,session?null:teacher);
   const profiles=await real('GET',base+'/profiles');assert.deepEqual(profiles.find(x=>x.id===profile.id),profile);assert.equal(await profileForm.locator('[name=workerId]').inputValue(),'');assert.equal(await profileForm.locator('[name=effectiveFrom]').inputValue(),'');
   await choose(payoutForm,'payrollProfileId',session?`${name} · 4 sessions · ₹1,000`:`${name} · Monthly ₹1,000`);
   const period=`QA-UI-${index}-FIRST`;await payoutForm.locator('[name=periodLabel]').fill(period);await payoutForm.locator('[name=deductions]').fill('200');if(session){await payoutForm.locator('[name=sessionsCovered]').fill('4');await payoutForm.locator('[name=grossAmount]').fill('1000');}
   if(index===0){
    await real('POST','/__qa/audit/'+nonce,true,204);
    try{await payoutForm.getByRole('button',{name:'Record payout & generate payslip',exact:true}).click();await notice.getByText('Payroll save could not be confirmed. Refresh and check the register before retrying.',{exact:true}).waitFor();assert.equal(writes.at(-1).status,500);assert.equal(await payoutForm.locator('[name=periodLabel]').inputValue(),period);assert.equal(await payoutForm.locator('fieldset').evaluate(el=>el.disabled),true);assert.equal(await payoutForm.locator('[name=periodLabel]').isDisabled(),true);await payoutForm.evaluate(f=>f.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true})));assert.equal(writes.length,2);assert.deepEqual(await real('GET',base+'/payouts'),old);}
    finally{await real('POST','/__qa/audit/'+nonce,false,204);}
    await page.getByRole('button',{name:'Refresh payout register',exact:true}).click();await loaded();await notice.getByText('Payout register refreshed. Check for the previous profile or payout before recording it again.',{exact:true}).waitFor();
   }
   const submit=async()=>{const n=writes.length;await payoutForm.getByRole('button',{name:'Record payout & generate payslip',exact:true}).click();await page.waitForFunction(()=>[...document.querySelectorAll('form')].every(f=>f.getAttribute('aria-busy')==='true'));await payoutForm.evaluate(f=>f.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true})));await notice.getByText('Payout recorded and payslip generated.',{exact:true}).waitFor();await loaded();assert.equal(writes.length,n+1);assert.equal(writes.at(-1).status,201);};
   await submit();const first=writes.at(-1).result;assert.equal(first.netAmount,800);assert.equal(first.reference,null);assert.equal(first.identitySource,'PayoutSnapshot');assert.equal(await payoutForm.locator('[name=periodLabel]').inputValue(),'');
   await verifyRegister('saved',[first]);await page.reload();await loaded();await verifyRegister('reloaded',[first]);
   const label=session?`${name} · 4 sessions · ₹1,000`:`${name} · Monthly ₹1,000`;await choose(payoutForm,'payrollProfileId',label);
   await payoutForm.locator('[name=periodLabel]').fill(`QA-UI-${index}-RECOVERED`);await payoutForm.locator('[name=deductions]').fill('2000');if(session){await payoutForm.locator('[name=sessionsCovered]').fill('4');await payoutForm.locator('[name=grossAmount]').fill('1000');}
   await payoutForm.getByRole('button',{name:'Record payout & generate payslip',exact:true}).click();await notice.getByText('Deductions cannot exceed the gross payout amount.',{exact:true}).waitFor();await loaded();assert.equal(writes.at(-1).status,400);assert.equal(await payoutForm.locator('[name=deductions]').inputValue(),'2000');
   await payoutForm.locator('[name=deductions]').fill('200');await submit();const second=writes.at(-1).result;assert.equal(second.netAmount,800);await verifyRegister('denied-recovered',[first,second]);
   const edited={...profile,workerName:'Synthetic renamed '+index,workerType:session?'Teacher':'Staff',teacherId:session?teacher:null,staffUserId:session?null:staff};
   await real('PUT',base+'/profiles/'+profile.id,edited);await page.reload();await loaded();await verifyRegister('renamed',[first,second]);
   await real('PUT',base+'/profiles/'+profile.id,{...edited,isActive:false});await page.reload();await loaded();await verifyRegister('inactive',[first,second]);
   await payoutForm.locator('.standard-select-field:has(input[name=payrollProfileId]) button').click();assert.equal(await page.getByRole('option',{name:new RegExp('Synthetic renamed '+index)}).count(),0);await page.keyboard.press('Escape');await page.locator('.payroll-heading').click();
   assert.deepEqual(errors,[]);const unexpected=consoleErrors.filter(x=>!/^Failed to load resource: the server responded with a status of (400|500) /.test(x));assert.deepEqual(unexpected,[]);assert.equal(consoleErrors.length,index===0?2:1);
   assert.equal(writes.filter(x=>x.status===201).length,3);assert.equal(writes.filter(x=>x.status===400).length,1);assert.equal(writes.filter(x=>x.status===500).length,index===0?1:0);assert.ok(reads.length>0);
   await context.close();console.log(`PAYROLL BROWSER PASS: ${width}px/${theme}/${session?'staff-session':'teacher-monthly'}, six register stages, real UI saved/400-recovered/history/legacy/no duplicate/null.`);
  }
  assert.equal(checks.length,24);fs.writeFileSync(path.join(evidence,'receipt.json'),JSON.stringify({classification:'real browser/Identity/API/owned SQL; physical devices NOT RUN',checks},null,2));console.log('PAYROLL BROWSER PASS: 4 journeys /24 register states; screenshots '+evidence);
 }finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;});
