// Exported Events UI/synthetic transport; not live Identity/SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','events-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;const checks=[],errors=[];
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const timezoneId of ['Asia/Kolkata','America/Los_Angeles','Europe/London'])for(const width of [320,768,1440])for(const theme of ['light','dark'])for(const scenario of ['normal','refresh-failure','rejected','uncertain-committed']){
   const context=await browser.newContext({viewport:{width,height:900},timezoneId}),page=await context.newPage(),calls=[],pageErrors=[];
   page.on('pageerror',e=>pageErrors.push(e.message));page.on('console',m=>{if(m.type()==='error')pageErrors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-events-token');},theme);
   const legacy={id:'legacy',title:'Existing UTC event',type:'Recital',branchId:null,startUtc:'2026-10-30T18:47:00Z',endUtc:'2026-10-30T19:47:00Z',status:'Planned',venue:null};const originalLegacy=JSON.stringify(legacy);let rows=[legacy];
   await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url()),method=request.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:request.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='POST'){
      assert.equal(url.pathname,'/api/academies/owned/events');
      const body=request.postDataJSON();
      if(scenario!=='rejected')rows.push({...body,id:'created',status:'Planned'});
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:201,headers,contentType:'application/json',body:scenario==='rejected'||scenario==='uncertain-committed'?JSON.stringify({message:'Synthetic service guidance'}):JSON.stringify(rows[0])});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/events')&&scenario==='refresh-failure'&&calls.some(x=>['POST','DELETE'].includes(x.method)))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Certificates"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/branches':[], '/api/academies/owned/events':rows};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/events');await page.getByText('Existing UTC event',{exact:true}).waitFor();const oldCard=page.locator('.events-register li').filter({hasText:'Existing UTC event'});assert.match(await oldCard.innerText(),/31 Oct 2026, 12:17 am IST/);await page.getByText('Event dates and times use IST (UTC+05:30), regardless of your device timezone.',{exact:true}).waitFor();
   const title=page.getByPlaceholder('Event title');await title.fill('Synthetic event');await page.getByRole('button',{name:'Plan event',exact:true}).click();assert.equal(calls.filter(x=>x.method==='POST').length,0);await page.getByRole('alert').filter({hasText:'Start date (required) is required.'}).waitFor();
   for(const i of[0,1]){
    await page.locator('.standard-date-trigger').nth(i).click();
    await page.getByLabel('Select year').selectOption('2026');await page.getByLabel('Select month').selectOption('9');
    await page.locator('.standard-date-days button[data-current="true"]').filter({hasText:/^31$/}).click();
   }
   await require('./shared-picker-actions.cjs').time(page,'start-time','10:37');await require('./shared-picker-actions.cjs').time(page,'end-time','11:37');
   await page.getByRole('button',{name:'Plan event',exact:true}).click();
   const notice=page.getByRole('status'),failed=scenario==='rejected'||scenario==='uncertain-committed';
   await notice.filter({hasText:failed?'Synthetic service guidance':'Event planned.'}).waitFor();
   await page.waitForFunction(()=>!document.querySelector('.events-action').disabled);
   assert.equal(await title.inputValue(),failed?'Synthetic event':'');
   for(const name of['start-date','end-date'])assert.equal(await page.locator('input[name="'+name+'"]').inputValue(),failed?'2026-10-31':'');
   assert.equal(calls.filter(x=>x.method==='POST').length,1);
   // Explicit IST input -> UTC is identical for every device timezone.
   assert.deepEqual(calls.find(x=>x.method==='POST').body,{title:'Synthetic event',type:'Recital',branchId:null,startUtc:'2026-10-31T05:07:00.000Z',endUtc:'2026-10-31T06:07:00.000Z',venue:null,capacity:null,notes:null});
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*do not repeat/);
   if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/could not be confirmed.*before retrying/);
   const reportedNotice=await notice.textContent();assert.equal(rows.length,scenario==='rejected'?1:2);assert.equal(JSON.stringify(legacy),originalLegacy,'Existing stored UTC values are never rewritten');if(scenario==='normal'){const card=page.locator('.events-register li').filter({hasText:'Synthetic event'});assert.match(await card.innerText(),/31 Oct 2026, 10:37 am IST/);await page.reload();await page.getByText('Existing UTC event',{exact:true}).waitFor();assert.match(await page.locator('.events-register li').filter({hasText:'Synthetic event'}).innerText(),/31 Oct 2026, 10:37 am IST/);assert.equal(calls.filter(x=>x.method==='POST').length,1,'Reload is read only');assert.equal(JSON.stringify(legacy),originalLegacy);}
   assert.equal(await page.locator('form').getByRole('alert').count(),0,'Resolved required alerts remain cleared');for(const input of await page.locator('main input:not([type="hidden"]),main textarea').all())assert.ok(await input.evaluate(e=>e.labels.length||e.getAttribute('aria-label')),'Persistent native labels');assert.ok(await page.locator('main button:visible').evaluateAll(es=>es.every(e=>e.getBoundingClientRect().height>=44)),'Settled actions44px');assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   if(scenario!=='normal')await notice.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,`${timezoneId.replaceAll('/','-')}-${width}-${theme}-${scenario}.png`),fullPage:true});
   const code=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   const unexpected=pageErrors.filter(x=>!code||!x.startsWith('Failed to load resource:')||!x.includes(code));assert.deepEqual(unexpected,[]);errors.push(...unexpected);
   checks.push({timezoneId,width,theme,scenario,calls,notice:reportedNotice,expectedResourceErrors:pageErrors.length});console.log(`PASS EVENTS ${timezoneId} ${width} ${theme} ${scenario}`);await context.close();
  }
  assert.equal(checks.length,72);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,transport:'synthetic API, not live Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
