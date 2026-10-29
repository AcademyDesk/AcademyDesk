// Exported application -> loopback relay -> real Identity/API -> disposable SQL.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict'),cp=require('node:child_process');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api,academy,teacher]=process.argv.slice(2),target=new URL(api);assert.equal(target.hostname,'127.0.0.1');assert.equal(target.protocol,'http:');assert.ok(target.port);for(const id of [academy,teacher])assert.match(id,/^[a-f0-9-]{36}$/);
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE','payroll-document-live-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':({'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  const open=async(width,theme,role)=>{
   const context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage(),errors=[],writes=[];
   const login=await context.request.post(api+'/api/auth/login',{data:{email:role==='Teacher'?'qa-teacher-a@example.invalid':'qa-admin-a@example.invalid',password:'Synthetic!39Ab'}});assert.equal(login.status(),200);const token=(await login.json()).accessToken,headers={Authorization:'Bearer '+token};
   await context.addInitScript(({theme,token,role})=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.'+role,token);},{theme,token,role});
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url());
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const response=await route.fetch({url:api+url.pathname+url.search});assert.notEqual(response.status(),429);assert.ok(response.ok(),'Unexpected UI response '+url.pathname+' '+response.status());
     if(req.method()==='POST')writes.push({path:url.pathname,raw:req.postData(),result:await response.json()});
     return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();errors.push('Unexpected external request');return route.abort('blockedbyclient');
   });
   const real=async(route,status=200)=>{const response=await context.request.get(api+route,{headers});assert.equal(response.status(),status);return status===200?response.json():null;};
   return{context,page,errors,writes,real};
  };
  let index=0;
  for(const [width,theme] of [[320,'dark'],[1440,'light']])for(const model of ['Monthly','SessionBlock'])for(const amount of ['0.01','9999999999999999.99']){
   const {context,page,errors,writes,real}=await open(width,theme,'AcademyAdmin'),session=model==='SessionBlock';
   await page.goto(origin+'/payroll');await page.waitForFunction(()=>document.querySelectorAll('.payroll-fields').length===2&&[...document.querySelectorAll('.payroll-fields')].every(f=>!f.disabled));
   const profile=page.locator('form').nth(0),payout=page.locator('form').nth(1),notice=page.locator('.payroll-notice'),worker=(await real(`/api/academies/${academy}/teachers`)).find(x=>x.id===teacher);assert.ok(worker);const name=worker.firstName+' '+worker.lastName;
   const choose=async(form,field,label)=>{await form.locator(`.standard-select-field:has(input[name="${field}"]) button`).click();await page.getByRole('option',{name:label,exact:true}).click();assert.equal(await page.getByRole('listbox').count(),0);};
   await choose(profile,'workerId',name);if(session){await choose(profile,'paymentModel','Completed-session cycle');await profile.locator('[name=sessionsPerCycle]').fill('4');}
   await profile.locator('.standard-date-trigger').click();await page.getByRole('dialog',{name:'Effective from calendar'}).getByRole('button',{name:'Today',exact:true}).click();
   const money=profile.locator(`[name=${session?'amountPerCycle':'monthlyAmount'}]`);assert.equal(await money.getAttribute('min'),'0.01');await money.fill('0');await profile.getByRole('button',{name:'Save payout profile',exact:true}).click();assert.equal(writes.length,0);assert.equal(await money.inputValue(),'0');
   await money.fill(amount);await profile.getByRole('button',{name:'Save payout profile',exact:true}).click();await notice.getByText('Payout profile saved.',{exact:true}).waitFor();assert.equal(writes.length,1);
   const created=writes[0].result;assert.ok(writes[0].raw.includes(`"${session?'amountPerCycle':'monthlyAmount'}":${amount}`));assert.equal(created[session?'amountPerCycleExact':'monthlyAmountExact'],amount);
   const base=`/api/academies/${academy}/payroll`;assert.deepEqual((await real(base+'/profiles')).find(x=>x.id===created.id),created);
   const formatted=amount==='0.01'?'₹0.01':'₹9,99,99,99,99,99,99,999.99',label=session?`${name} · 4 sessions · ${formatted}`:`${name} · Monthly ${formatted}`;
   // Identical worker/rate labels may exist; select the exact newly persisted profile id.
   await payout.locator('.standard-select-field:has(input[name="payrollProfileId"]) button').click();const options=page.getByRole('option',{name:label,exact:true});await options.first().waitFor({state:'visible'});const count=await options.count();assert.ok(count>0);
   let selected=false;for(let n=0;n<count;n++){const option=options.nth(n);await option.click();if(await payout.locator('[name=payrollProfileId]').inputValue()===created.id){selected=true;break;}await payout.locator('.standard-select-field:has(input[name="payrollProfileId"]) button').click();}
   assert.ok(selected,'Created profile not selectable');assert.equal(await page.getByRole('listbox').count(),0);
   const period='QA-DOCUMENT-UI-'+index++;await payout.locator('[name=periodLabel]').fill(period);if(session){const gross=payout.locator('[name=grossAmount]');assert.equal(await gross.getAttribute('min'),'0.01');await payout.locator('[name=sessionsCovered]').fill('4');await gross.fill(amount);}
   const deduction=amount==='0.01'?'0.00':'9999999999999999.98';await payout.locator('[name=deductions]').fill(deduction);await payout.getByRole('button',{name:'Record payout & generate payslip',exact:true}).click();await notice.getByText('Payout recorded and payslip generated.',{exact:true}).waitFor();assert.equal(writes.length,2);assert.ok(writes[1].raw.includes('"deductions":'+deduction));if(session)assert.ok(writes[1].raw.includes('"grossAmount":'+amount));
   const row=writes[1].result;assert.equal(row.grossAmountExact,amount);assert.equal(row.netAmountExact,'0.01');assert.deepEqual((await real(base+'/payouts')).find(x=>x.id===row.id),row);
   await page.reload();await page.locator(`.payroll-register tr[data-payout-id="${row.id}"]`).waitFor();assert.equal(await page.locator(`.payroll-register tr[data-payout-id="${row.id}"] td`).nth(3).innerText(),'₹0.01');assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   checks.push({kind:'linked-precision',width,theme,model,amount,profileId:created.id,payoutId:row.id});await context.close();console.log(`PAYROLL DOCUMENT BROWSER precision PASS ${width}/${theme}/${model}/${amount}: real exact request/SQL/readback/reload, zero no-submit retained.`);
  }
  for(const width of [320,1440])for(const theme of ['light','dark']){
   const {context,page,errors,writes,real}=await open(width,theme,'Teacher');await real(`/api/academies/${academy}/payroll/payouts`,403);
   await page.goto(origin+'/teacher');await page.getByRole('heading',{name:/Welcome,/}).waitFor();const nav=page.locator(width===320?'.learner-bottom-nav':'.teacher-portal-nav');if(width===320)await nav.locator('summary').click();await nav.getByRole('button',{name:/Pay slip/}).click();await page.getByText('Payment model:',{exact:false}).waitFor();
   const current=new Date().toISOString().slice(0,7),[year,month]=current.split('-');const filter=page.locator('.teacher-payslip-filter input');await filter.fill(current);
   const rows=(await real(`/api/teacher/payments?year=${year}&month=${Number(month)}`)).payslips;assert.ok(rows.length>=12);await page.waitForFunction(count=>document.querySelectorAll('.teacher-payslip-list>article').length===count,rows.length);
   for(const row of rows)await page.locator('.teacher-payslip-list>article').filter({hasText:row.payslipNumber}).waitFor();
   const text=await page.locator('.teacher-payslip-list').innerText();assert.ok(!/Foreign synthetic record|Other recorded teacher|Historical identity not recorded/.test(text));
   const selections=[['session',rows.find(x=>x.sessionsCovered===7)],['monthly',rows.find(x=>x.sessionsCovered==null&&x.netAmountExact==='0.01')],['no-cash',rows.find(x=>x.status==='NoCashDue')],['maximum',rows.find(x=>x.grossAmountExact==='9999999999999999.99')]];
   for(const [kind,row] of selections){assert.ok(row,kind+' missing fixture');const trigger=page.locator('.teacher-payslip-list>article').filter({hasText:row.payslipNumber}).getByRole('button',{name:'Preview / Print',exact:true});await trigger.click();const paper=page.getByRole('article',{name:'Selected payroll document'});await paper.waitFor();assert.equal(await paper.getAttribute('data-payout-id'),row.id);assert.equal(await paper.locator('header strong').textContent(),row.payslipNumber);assert.equal(await paper.locator('dd').nth(0).textContent(),row.workerName);assert.equal(await paper.locator('dd').nth(2).textContent(),row.periodLabel);assert.equal(await paper.locator('dd').nth(3).textContent(),row.sessionsCovered==null?'Monthly':`${row.sessionsCovered} sessions`);
    if(kind==='no-cash'){await paper.getByText('No cash due',{exact:true}).waitFor();await paper.getByText('No cash payment date',{exact:true}).waitFor();}
    assert.ok(await paper.evaluate(el=>el.scrollWidth<=el.clientWidth+1),'Preview internal horizontal overflow');await page.evaluate(()=>{window.__qaPrints=0;window.print=()=>window.__qaPrints++;});await page.getByRole('dialog').getByRole('button',{name:'Print / Save PDF',exact:true}).click();assert.equal(await page.evaluate(()=>window.__qaPrints),1);
    const file=path.join(evidence,`${width}-${theme}-${kind}.pdf`);await page.pdf({path:file,format:'A4',printBackground:true});const python='C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe';const metadata=JSON.parse(cp.execFileSync(python,['-c','import json,sys;from pypdf import PdfReader;r=PdfReader(sys.argv[1]);print(json.dumps({"pages":len(r.pages),"text":"\\n".join(p.extract_text() or "" for p in r.pages)}))',file],{encoding:'utf8'}));
    // Embedded-font kerning can introduce extraction-only whitespace. Retain
    // exact DOM values and compare every non-whitespace document character.
    const compact=s=>s.replace(/\s+/g,''),printed=compact(metadata.text);assert.equal(metadata.pages,1);assert.ok(metadata.text.includes(row.id));assert.ok(printed.includes(compact(row.payslipNumber)));assert.ok(printed.includes(compact(row.workerName)));assert.ok(printed.includes(compact(row.periodLabel)));
    const expectedMoney=value=>{assert.match(value,/^\d+\.\d{2}$/);const [whole,cents]=value.split('.');return '₹'+BigInt(whole).toLocaleString('en-IN')+'.'+cents;};
    for(const [n,field] of ['grossAmountExact','deductionsExact','netAmountExact'].entries()){const expected=expectedMoney(row[field]);assert.equal(compact(await paper.locator('[aria-label="Payroll amount breakdown"] b').nth(n).textContent()),expected);assert.ok(printed.includes(expected),'Exact PDF amount missing: '+field);}
    if(['monthly','maximum'].includes(kind))assert.ok(printed.includes('0.01'));if(kind==='maximum')assert.ok(printed.includes('9,99,99,99,99,99,99,999.99'));if(kind==='session')assert.ok(printed.includes('7sessions'));if(kind==='no-cash')assert.ok(printed.includes('Nocashpaymentdate'));for(const other of rows.filter(x=>x.id!==row.id))assert.ok(!printed.includes(compact(other.payslipNumber)),'Other document in print');assert.ok(!printed.includes('Welcome,'));
    fs.writeFileSync(file+'.json',JSON.stringify({rowId:row.id,...metadata},null,2));await page.getByRole('button',{name:'Close preview',exact:true}).click();assert.equal(await page.locator('dialog[open]').count(),0);assert.ok(await trigger.evaluate(el=>document.activeElement===el));
   }
   await filter.fill('2020-01');await page.getByText('No verified payslips or earnings statements for this view.',{exact:true}).waitFor();assert.equal((await real('/api/teacher/payments?year=2020&month=1')).payslips.length,0);await page.getByRole('button',{name:'All months',exact:true}).click();const latest=(await real('/api/teacher/payments')).payslips;await page.waitForFunction(count=>document.querySelectorAll('.teacher-payslip-list>article').length===count,latest.length);await page.reload();await page.getByRole('heading',{name:/Welcome,/}).waitFor();if(width===320)await nav.locator('summary').click();await nav.getByRole('button',{name:/Pay slip/}).click();await page.waitForFunction(count=>document.querySelectorAll('.teacher-payslip-list>article').length===count,latest.length);
   assert.equal(writes.length,0);assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await page.screenshot({path:path.join(evidence,`${width}-${theme}-teacher.png`),fullPage:true});checks.push({kind:'linked-teacher',width,theme,rows:rows.map(x=>x.id),printed:selections.map(x=>x[1].id)});await context.close();console.log(`PAYROLL DOCUMENT TEACHER PASS ${width}/${theme}: real own-history/filter-empty/reload/finance403,4 actual selected one-page PDFs/session/monthly/NoCashDue/maximum/modal focus.`);
  }
  assert.equal(checks.length,12);fs.writeFileSync(path.join(evidence,'receipt.json'),JSON.stringify({classification:'real Identity/API/owned SQL linked browser; physical devices NOT RUN',checks},null,2));console.log('PAYROLL DOCUMENT RECEIPT '+evidence);
 }finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;});
