// Production export + real loopback API/Identity/SQL. No mocked API bodies or injected tokens.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
assert.ok(process.env.QA_CALENDAR_LIVE_LOG,'Exact live SQL log required');
const log=fs.readFileSync(process.env.QA_CALENDAR_LIVE_LOG,'utf8'),matches=[...log.matchAll(/^CALENDARDETAILS BROWSER READY (.+)$/gm)];assert.equal(matches.length,1);
assert.ok(!log.includes('CALENDARDETAILS BROWSER FINAL'),'Refuse reuse of completed fixture');
const fixture=JSON.parse(matches[0][1]);assert.match(fixture.runId,/^[a-f0-9]{32}$/);assert.equal(fixture.cases.length,16);
assert.match(log,/CALENDARDETAILS REGRESSION PASS:19 cases;/);
assert.equal(new Set(fixture.cases.map(x=>x.sessionId)).size,16);
const ownedRoot=path.resolve(process.env.TEMP,'AcademyDesk-QA',fixture.runId),stop=path.resolve(fixture.stop);
assert.equal(stop,path.join(ownedRoot,'stop-browser'));assert.equal(fs.readFileSync(path.join(ownedRoot,'.qa-owner'),'utf8').split(/\r?\n/)[0],fixture.runId);
assert.ok(!fs.existsSync(stop),'Refuse stopped fixture');
assert.equal(fixture.api,49662);assert.equal(fixture.origin,49661);
const origin=`http://127.0.0.1:${fixture.origin}`,api=`http://127.0.0.1:${fixture.api}`;
const evidence=path.join(root,'QA/EVIDENCE',`calendar-live-browser-${Date.now()}`);fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,origin).pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});
 if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise((resolve,reject)=>{server.once('error',reject);server.listen(fixture.origin,'127.0.0.1',resolve);});
 let browser,currentPage;const checks=[],errors=[],requests=[],requestTimes=[];
 // Limiter currently runs before authentication. Pace the whole test, not identities.
 async function pace(){
  while(true){const now=Date.now();while(requestTimes.length&&now-requestTimes[0]>=61000)requestTimes.shift();
   if(requestTimes.length<95){requestTimes.push(now);return;}await new Promise(resolve=>setTimeout(resolve,Math.max(1,61000-(now-requestTimes[0]))));}
 }
 try{
  // Let fixture setup's original limiter window expire without altering its policy.
  await new Promise(resolve=>setTimeout(resolve,61000));
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const timezoneId of ['UTC','Asia/Kolkata','America/Los_Angeles','Australia/Sydney'])for(const width of [320,1440])for(const theme of ['light','dark']){
   const label=`${timezoneId.replaceAll('/','-')}-${width}-${theme}`,target=fixture.cases.find(x=>x.label===label);assert.ok(target);
   const context=await browser.newContext({timezoneId,viewport:{width,height:900}}),page=currentPage=await context.newPage();
   // Unchanged rate-limiter pacing can legitimately pause requests for one minute.
   page.setDefaultTimeout(90000);page.setDefaultNavigationTimeout(120000);
   page.on('pageerror',e=>errors.push({label,error:e.message}));page.on('console',m=>{if(m.type()==='error')errors.push({label,error:m.text()});});
   await page.clock.setFixedTime(new Date('2026-09-30T19:00:00Z'));
   await context.addInitScript(theme=>localStorage.setItem('academydesk.theme',theme),theme);
   await context.route('**/*',async route=>{
    try{
    const request=route.request(),url=new URL(request.url());
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     // Transparent port forwarding of a real response, not fixture fulfillment.
     await pace();const response=await route.fetch({url:api+url.pathname+url.search,headers:{...request.headers(),origin},maxRedirects:0,maxRetries:0});
     requests.push({label,method:request.method(),path:url.pathname,status:response.status()});
     if(response.status()===429)errors.push({label,error:'HTTP 429: No rate-limited acceptance'});
     await route.fulfill({response});return;
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
    }catch(e){errors.push({label,error:e.message});await route.abort().catch(()=>{});}
   });
   assert.match(target.username,/^qa-live-[a-z0-9_-]+@example\.invalid$/);
   await page.goto(origin+'/login');await page.getByLabel('User name',{exact:true}).fill(target.username);await page.getByLabel('Password',{exact:true}).fill('Synthetic!39Ab');
   await page.getByRole('button',{name:'Sign in',exact:true}).click();await page.waitForURL('**/dashboard');
   await page.goto(origin+'/calendar');await page.getByRole('heading',{name:'October 2026',exact:true}).waitFor();
   await page.waitForFunction(expected=>document.documentElement.dataset.theme===expected,theme);
   const agenda=()=>page.locator('.workspace-calendar-agenda-item').filter({has:page.getByRole('heading',{name:target.batchName,exact:true})});
   const grid=()=>page.locator('.calendar-event.class').filter({hasText:target.batchName});
   await agenda().waitFor();assert.equal(await agenda().getAttribute('data-session-status'),'Scheduled');assert.equal(await grid().getAttribute('href'),target.roomName);
   assert.equal(await agenda().getByRole('link').getAttribute('href'),target.roomName);assert.match(await agenda().textContent(),/12:30 am/);
   const beforeCount=await page.locator('.workspace-calendar-agenda-item').count();assert.equal(beforeCount,21);
   const day=page.locator('.calendar-day').filter({has:grid()});assert.equal(await day.locator('.calendar-date').textContent(),String(target.day));
   const recital=page.locator('.workspace-calendar-agenda-item').filter({has:page.getByRole('heading',{name:'Live boundary recital 2026-9',exact:true})});
   await recital.waitFor();assert.match(await recital.textContent(),/01 Oct,? 2026/);assert.match(await recital.textContent(),/12:30 am/);
   const makeups=page.locator('.workspace-calendar-agenda-item').filter({hasText:'Make-up'});assert.equal(await makeups.count(),1);assert.match(await makeups.textContent(),/01 Oct,? 2026/);assert.match(await makeups.textContent(),/12:30 am/);
   await page.goto(origin+'/schedule');const schedule=page.locator('li').filter({hasText:target.batchName}).filter({hasText:target.roomName});await schedule.waitFor();assert.equal(await schedule.count(),1);
   const put=page.waitForResponse(r=>new URL(r.url()).pathname===`/api/academies/${fixture.academyId}/sessions/${target.sessionId}`&&r.request().method()==='PUT');
   await schedule.locator('select').selectOption('Cancelled');const response=await put;assert.equal(response.status(),200);const saved=await response.json();assert.equal(saved.status,'Cancelled');assert.equal(saved.startUtc,target.startUtc);assert.equal(saved.roomName,target.roomName);
   await schedule.locator('select').waitFor({state:'visible'});await page.waitForFunction(name=>{const row=[...document.querySelectorAll('li')].find(x=>x.textContent.includes(name));return row?.querySelector('select')?.value==='Cancelled'&&!row.querySelector('select').disabled;},target.batchName);
   await page.goto(origin+'/calendar');await agenda().waitFor();assert.equal(await agenda().getAttribute('data-session-status'),'Cancelled');
   assert.equal(await agenda().getByRole('link').count(),0);assert.equal(await grid().getAttribute('href'),null);assert.ok(await agenda().getByText('This class is cancelled. Meeting access is unavailable.',{exact:true}).isVisible());
   assert.equal(await page.locator('a').evaluateAll((links,url)=>links.filter(x=>x.href===url).length,target.roomName),0);assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),beforeCount);
   assert.match(await agenda().textContent(),/12:30 am/);assert.equal(await day.locator('.calendar-date').textContent(),String(target.day));
   await page.reload();await agenda().waitFor();assert.equal(await agenda().getAttribute('data-session-status'),'Cancelled');assert.equal(await agenda().getByRole('link').count(),0);
   await page.getByRole('button',{name:'Class',exact:true}).click();assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),19);
   await page.getByRole('button',{name:'Collapse',exact:true}).click();assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),0);await page.getByRole('button',{name:'Expand',exact:true}).click();assert.equal(await agenda().getByRole('link').count(),0);
   await page.getByRole('button',{name:'All',exact:true}).click();await agenda().scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,label+'-cancelled.png'),fullPage:false});
   for(let i=0;i<3;i++)await page.getByRole('button',{name:'Next month',exact:true}).click();await page.getByRole('heading',{name:'January 2027',exact:true}).waitFor();assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),2);
   for(const row of await page.locator('.workspace-calendar-agenda-item').all()){assert.match(await row.textContent(),/01 Jan,? 2027/);assert.match(await row.textContent(),/12:30 am/);}
   const january=page.locator('.calendar-day').filter({hasText:'Live boundary recital 2026-12'});assert.equal(await january.locator('.calendar-date').textContent(),'1');
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   assert.deepEqual(errors,[]);
   checks.push({label,timezoneId,width,theme,sessionId:target.sessionId,realLogin:true,realSchedulePut:200,cancelledReload:true,historyCount:beforeCount,meetingDisabled:true,allSourcesUtc:true,monthYearBoundary:true});
   console.log('PASS LIVE CALENDAR '+label);await context.unrouteAll({behavior:'wait'});await context.close();
   // Do not disable/raise the limiter or retry failed mutations.
  }
  assert.equal(checks.length,16);assert.deepEqual(errors,[]);assert.equal(requests.filter(x=>x.method==='PUT').length,16);assert.ok(requests.filter(x=>x.method==='PUT').every(x=>x.status===200));
  fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({runId:fixture.runId,checks,errors,requests,transport:'real loopback forwarding, no API fixture bodies',physicalDevices:'NOT RUN'},null,2));
  console.log(JSON.stringify({passed:checks.length,evidence,runId:fixture.runId}));
 }catch(e){if(currentPage&&!currentPage.isClosed())await currentPage.screenshot({path:path.join(evidence,'failed.png'),fullPage:true}).catch(()=>{});fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,requests,failure:e.message},null,2));throw e;}
 finally{
  if(browser){for(const context of browser.contexts())await context.unrouteAll({behavior:'ignoreErrors'});await browser.close();}await new Promise(resolve=>server.close(resolve));
  assert.equal(fs.readFileSync(path.join(ownedRoot,'.qa-owner'),'utf8').split(/\r?\n/)[0],fixture.runId);fs.writeFileSync(stop,'calendar browser finished');
 }
})().catch(e=>{console.error(e.message);process.exitCode=1;server.close();});
