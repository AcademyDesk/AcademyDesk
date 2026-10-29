// Production export + controlled HTTP responses: UI proof, not SQL/RBAC/device proof.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','payroll-precision-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':({'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const model of ['Monthly','SessionBlock']){
   const context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage(),errors=[],writes=[];
   const session=model==='SessionBlock',profiles=[],payouts=[],maximum='9999999999999999.99';
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-precision-token');},theme);
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const headers={'Access-Control-Allow-Origin':origin,'Access-Control-Allow-Headers':'Authorization,Content-Type','Access-Control-Allow-Methods':'GET,POST,OPTIONS'};
     if(method==='OPTIONS')return route.fulfill({status:204,headers});
     if(method==='POST'){
      writes.push({path:url.pathname,raw:req.postData()});const data=req.postDataJSON();let row;
      if(url.pathname==='/api/academies/owned/payroll/profiles'){
       assert.match(req.postData(),session?/"amountPerCycle":9999999999999999\.99(?:,|})/:/"monthlyAmount":9999999999999999\.99(?:,|})/);
       row={...data,id:'profile',monthlyAmountExact:session?null:maximum,amountPerCycleExact:session?maximum:null};profiles.push(row);
      }else{
       assert.equal(url.pathname,'/api/academies/owned/payroll/payouts');assert.match(req.postData(),/"deductions":9999999999999999\.98(?:,|})/);
       if(session)assert.match(req.postData(),/"grossAmount":9999999999999999\.99(?:,|})/);
       row={...data,id:'payout',workerName:'Synthetic Worker',workerType:'Teacher',payslipNumber:'QA-EXACT',grossAmount:Number(maximum),grossAmountExact:maximum,deductionsExact:'9999999999999999.98',netAmount:0.01,netAmountExact:'0.01',currency:'INR',status:'Paid',paidAtUtc:'2026-10-12T00:00:00Z',identitySource:'PayoutSnapshot'};payouts.push(row);
      }
      return route.fulfill({status:201,headers,contentType:'application/json',body:JSON.stringify(row)});
     }
     assert.equal(method,'GET');const data={
      '/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},
      '/api/academies':[{id:'owned',name:'Synthetic Academy',enabledModulesJson:'["Core","Finance","FinanceControls"]'}],
      '/api/portal/announcements':[], '/api/academies/owned/teachers':[{id:'teacher',firstName:'Synthetic',lastName:'Worker'}],
      '/api/academies/owned/staff':[], '/api/academies/owned/payroll/profiles':profiles,'/api/academies/owned/payroll/payouts':payouts};
     assert.ok(Object.hasOwn(data,url.pathname),'Unexpected route '+url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/payroll');await page.waitForFunction(()=>document.querySelectorAll('.payroll-fields').length===2&&[...document.querySelectorAll('.payroll-fields')].every(f=>!f.disabled));
   const profile=page.locator('form').nth(0),payout=page.locator('form').nth(1),notice=page.locator('.payroll-notice');
   const choose=async(form,name,label)=>{await form.locator(`.standard-select-field:has(input[name="${name}"]) button`).click();await page.getByRole('option',{name:label,exact:true}).click();assert.equal(await page.getByRole('listbox').count(),0);};
   await choose(profile,'workerId','Synthetic Worker');if(session){await choose(profile,'paymentModel','Completed-session cycle');await profile.locator('[name=sessionsPerCycle]').fill('4');}
   await profile.locator('.standard-date-trigger').click();await page.getByRole('dialog',{name:'Effective from calendar'}).getByRole('button',{name:'Today',exact:true}).click();
   const amount=profile.locator(`[name=${session?'amountPerCycle':'monthlyAmount'}]`);await amount.fill('1000.001');await profile.getByRole('button',{name:'Save payout profile',exact:true}).click();assert.equal(writes.length,0);assert.equal(await amount.inputValue(),'1000.001');
   await amount.fill(maximum);await profile.getByRole('button',{name:'Save payout profile',exact:true}).click();await notice.getByText('Payout profile saved.',{exact:true}).waitFor();assert.equal(writes.length,1);
   const formatted='₹9,99,99,99,99,99,99,999.99';await choose(payout,'payrollProfileId',session?`Synthetic Worker · 4 sessions · ${formatted}`:`Synthetic Worker · Monthly ${formatted}`);
   await payout.locator('[name=periodLabel]').fill('Synthetic exact period');if(session){await payout.locator('[name=sessionsCovered]').fill('4');await payout.locator('[name=grossAmount]').fill(maximum);}
   await payout.locator('[name=deductions]').fill('9999999999999999.98');await payout.getByRole('button',{name:'Record payout & generate payslip',exact:true}).click();await notice.getByText('Payout recorded and payslip generated.',{exact:true}).waitFor();assert.equal(writes.length,2);
   assert.equal(await page.locator('.payroll-register tbody tr').locator('td').nth(3).innerText(),'₹0.01');assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   await page.screenshot({path:path.join(evidence,`${width}-${theme}-${model}.png`),fullPage:true});checks.push({width,theme,model,writes});await context.close();
  }
  assert.equal(checks.length,8);fs.writeFileSync(path.join(evidence,'receipt.json'),JSON.stringify({classification:'controlled production-DOM proof, not SQL/RBAC/physical devices',checks},null,2));console.log('PAYROLL PRECISION BROWSER PASS:8 journeys/16 exact raw writes; native sub-cent no-submit/draft retention; exact profile labels and cents; '+evidence);
 }finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;});
