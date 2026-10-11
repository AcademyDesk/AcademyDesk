// Actual export -> loopback relay -> authenticated ASP.NET API -> run-owned SQL.
// API URLs are rewritten to the disposable relay; no response data is mocked.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api,academy,foreignBranch,casesJson]=process.argv.slice(2),apiOrigin=new URL(api),cases=JSON.parse(casesJson);
assert.equal(apiOrigin.hostname,'127.0.0.1');assert.equal(apiOrigin.protocol,'http:');assert.ok(apiOrigin.port);assert.equal(cases.length,4);
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE','expenses-live-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;const checks=[];
 try {
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const {width,theme,email} of cases){
   assert.match(email,/^qa-expense-ui-\d+-(light|dark)@example\.invalid$/);
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
   const login=await context.request.post(api+'/api/auth/login',{data:{email,password:'Synthetic!39Ab'}});assert.equal(login.status(),200);
   const {accessToken}=await login.json();assert.ok(accessToken);
   await context.addInitScript(({theme,accessToken})=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin',accessToken);},{theme,accessToken});
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url());
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const response=await route.fetch({url:api+url.pathname+url.search});assert.notEqual(response.status(),429);
     calls.push({method:request.method(),path:url.pathname,status:response.status()});
     return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/expenses');const description=page.getByPlaceholder('e.g. August piano instructor payment'),amount=page.getByPlaceholder('Amount in INR');
   await page.waitForFunction(()=>!document.querySelector('input[placeholder="Amount in INR"]').disabled);
   await page.getByText('You can record an expense without a branch.',{exact:false}).waitFor();
   await description.fill(`UI expense ${width} ${theme}`);await amount.fill('2.75');await page.getByRole('button',{name:'Record expense',exact:true}).click();
   try { await page.getByRole('status').filter({hasText:'Expense recorded.'}).waitFor(); }
   catch(error){fs.writeFileSync(path.join(evidence,`active-failure-${width}-${theme}.json`),JSON.stringify({calls,errors,form:await page.locator('form').evaluate(form=>({valid:form.checkValidity(),fields:[...form.querySelectorAll('input')].map(input=>({name:input.name,valid:input.validity.valid,disabled:input.disabled})),notice:document.querySelector('[role="status"]')?.textContent}))},null,2));await page.screenshot({path:path.join(evidence,`failed-${width}-${theme}.png`),fullPage:true});throw error;}
   await page.waitForFunction(()=>document.querySelector('form').getAttribute('aria-busy')==='false');
   assert.equal(await description.inputValue(),'');assert.equal(await amount.inputValue(),'');assert.match(await page.locator('.expenses-register').textContent(),new RegExp(`UI expense ${width} ${theme}`));
   assert.equal(calls.filter(x=>x.method==='POST').length,1);assert.equal(calls.find(x=>x.method==='POST').status,201);assert.ok(calls.some(x=>x.path.endsWith('/branches')&&x.status===403));assert.deepEqual(errors.filter(x=>!x.startsWith('Failed to load resource:')||!x.includes('403')),[]);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   await page.screenshot({path:path.join(evidence,`${width}-${theme}.png`),fullPage:true});
   if(width===320&&theme==='light'){
    await new Promise(r=>setTimeout(r,650));await description.fill('UI exact expense');await amount.fill('9999999999999999.99');await page.getByRole('button',{name:'Record expense',exact:true}).click();await page.getByRole('status').filter({hasText:'Expense recorded.'}).waitFor();await page.waitForFunction(()=>document.querySelector('form').getAttribute('aria-busy')==='false');
    const exact=page.locator('.expenses-list li').filter({hasText:'UI exact expense'});await exact.waitFor();assert.equal(await exact.locator('strong').textContent(),'₹9,99,99,99,99,99,99,999.99');assert.equal(calls.filter(x=>x.method==='POST').length,2);
    assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await page.screenshot({path:path.join(evidence,'320-light-exact-money.png'),fullPage:true});
   }
   // Invalid branch remains denied by the real host, not by this transport adapter.
   await new Promise(r=>setTimeout(r,650));const rejected=await context.request.post(api+`/api/academies/${academy}/expenses`,{headers:{Authorization:'Bearer '+accessToken},data:{description:'UI denied branch',amount:1.25,currency:'INR',category:'General',branchId:foreignBranch}});
   assert.equal(rejected.status(),400);assert.equal((await rejected.json()).message,'The selected branch does not belong to this academy.');
   checks.push({width,theme,calls,errors,rejectedStatus:400});console.log(`PASS: EXPENSE LIVE ${width} ${theme} real save/readback and foreign-branch rejection`);await context.close();
  }
  fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'real Identity/HTTP/run-owned SQL with loopback URL relay',physicalDevices:'NOT RUN'},null,2));console.log('PASS: EXPENSE LIVE evidence='+evidence);
 } catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e.message);process.exitCode=1;server.close();});
