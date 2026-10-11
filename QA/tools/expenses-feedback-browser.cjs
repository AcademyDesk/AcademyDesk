// Exported expense UI/controlled API. Not real SQL or physical-device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','expenses-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;const checks=[],errors=[];
 try {
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of [320,768,1440])for(const theme of ['light','dark'])for(const scenario of ['normal','refresh-failure','rejected','uncertain-committed','network','initial-failure','pending','branch-denied','exact-money']){
   const context=await browser.newContext({viewport:{width,height:900},timezoneId:'Asia/Kolkata'}),page=await context.newPage(),calls=[],pageErrors=[];
   page.on('pageerror',e=>pageErrors.push(e.message));page.on('console',m=>{if(m.type()==='error')pageErrors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-expense-token');},theme);
   let rows=[{id:'existing',description:'USD sentinel',amount:10.25,currency:'USD',category:'Rent',branchId:null,expenseDate:'2026-10-01',status:'Recorded'},{id:'local',description:'INR sentinel',amount:5.50,currency:'INR',category:'General',branchId:null,expenseDate:'2026-10-01',status:'Recorded'}];
   await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url()),method=request.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const headers={'Access-Control-Allow-Origin':origin,'Access-Control-Allow-Headers':'Authorization,Content-Type','Access-Control-Allow-Methods':'GET,POST,OPTIONS'};
     if(method==='OPTIONS')return route.fulfill({status:204,headers});
     calls.push({path:url.pathname,method,body:request.postDataJSON(),rawBody:request.postData()});
     if(method==='POST'){
      assert.equal(url.pathname,'/api/academies/owned/expenses');if(scenario==='pending')await new Promise(r=>setTimeout(r,400));
      if(scenario!=='rejected')rows.push({...request.postDataJSON(),amountExact:JSON.parse(request.postData()).amount>Number.MAX_SAFE_INTEGER?'9999999999999999.99':undefined,id:'created',status:'Recorded'});
      if(scenario==='network')return route.abort('failed');
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:201,headers,contentType:'application/json',body:scenario==='rejected'?JSON.stringify({message:'The selected branch does not belong to this academy.'}):scenario==='uncertain-committed'?'{}':''});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/expenses')&&(scenario==='initial-failure'||scenario==='refresh-failure'&&calls.some(x=>x.method==='POST')))return route.fulfill({status:503,headers,body:'{}'});
     if(url.pathname.endsWith('/branches')&&scenario==='branch-denied')return route.fulfill({status:403,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Finance","FinanceControls"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/expenses':rows,'/api/academies/owned/branches':[{id:'branch',name:'Local branch'}]};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/expenses');const description=page.getByPlaceholder('e.g. August piano instructor payment'),amount=page.getByPlaceholder('Amount in INR'),notice=page.getByRole('status'),submit=page.getByRole('button',{name:'Record expense',exact:true});
   if(scenario==='initial-failure'){
    await notice.filter({hasText:'Expenses could not be loaded.'}).waitFor();assert.equal(await description.isDisabled(),true);assert.equal(await submit.isDisabled(),true);assert.equal(calls.filter(x=>x.method==='POST').length,0);
   } else {
    await description.waitFor();await page.waitForFunction(()=>!document.querySelector('input[placeholder="Amount in INR"]').disabled);
    if(scenario==='branch-denied')await page.getByText('You can record an expense without a branch.',{exact:false}).waitFor();
    const kpis=page.locator('.expenses-kpis');assert.match(await kpis.textContent(),/10\.25/);assert.match(await kpis.textContent(),/5\.50/);assert.doesNotMatch(await kpis.textContent(),/15\.75/);
    await description.fill('Synthetic expense');await amount.fill(scenario==='exact-money'?'9999999999999999.99':'12.50');await submit.click();
    if(scenario==='pending'){
     await page.locator('form[aria-busy="true"]').waitFor();assert.equal(await description.isDisabled(),true);assert.equal(await page.locator('input[name="expenseDate"]').isDisabled(),true);
     await page.locator('form').evaluate(form=>{form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));});
     await page.getByRole('button',{name:'Refresh expense register',exact:true}).dispatchEvent('click');
    }
    const failed=['rejected','uncertain-committed','network'].includes(scenario),uncertain=['uncertain-committed','network'].includes(scenario);
    await notice.filter({hasText:scenario==='rejected'?'The selected branch':uncertain?'could not be confirmed':'Expense recorded.'}).waitFor();await page.waitForFunction(()=>document.querySelector('form').getAttribute('aria-busy')==='false');
    assert.equal(await description.inputValue(),failed?'Synthetic expense':'');assert.equal(await amount.inputValue(),failed?'12.50':'');assert.equal(calls.filter(x=>x.method==='POST').length,1);
    assert.deepEqual(calls.find(x=>x.method==='POST').body,{description:'Synthetic expense',amount:scenario==='exact-money'?10000000000000000:12.50,currency:'INR',category:'General',branchId:null,expenseDate:null});
    if(scenario==='exact-money'){assert.match(calls.find(x=>x.method==='POST').rawBody,/,"amount":9999999999999999\.99\}$/);assert.equal(await page.locator('.expenses-list li').filter({hasText:'Synthetic expense'}).locator('strong').textContent(),'₹9,99,99,99,99,99,99,999.99');}
    if(scenario==='refresh-failure')assert.match(await notice.textContent(),/Expense recorded.*could not be refreshed.*do not repeat/);
    if(uncertain){assert.match(await notice.textContent(),/before retrying/);assert.equal(await description.isDisabled(),true);await page.locator('form').evaluate(form=>form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true})));assert.equal(calls.filter(x=>x.method==='POST').length,1);
     await page.getByRole('button',{name:'Refresh expense register',exact:true}).click();await notice.filter({hasText:'Check for the previous expense'}).waitFor();assert.equal(await description.isDisabled(),false);assert.equal(await description.inputValue(),'Synthetic expense');assert.match(await page.locator('.expenses-register').textContent(),/Synthetic expense/);
    }
   }
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   await page.screenshot({path:path.join(evidence,`${width}-${theme}-${scenario}.png`),fullPage:true});
   const expectedCode=scenario==='branch-denied'?'403':scenario==='rejected'?'400':scenario==='refresh-failure'||scenario==='initial-failure'?'503':scenario==='uncertain-committed'?'500':scenario==='network'?'ERR_FAILED':null;
   const unexpected=pageErrors.filter(x=>!expectedCode||!x.startsWith('Failed to load resource:')||!x.includes(expectedCode));assert.deepEqual(unexpected,[]);errors.push(...unexpected);
   checks.push({width,theme,scenario,calls,notice:await notice.textContent(),expectedResourceErrors:pageErrors.length});console.log(`PASS EXPENSE ${width} ${theme} ${scenario}`);await context.close();
  }
  assert.equal(checks.length,54);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,transport:'controlled API, not real Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 } catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
