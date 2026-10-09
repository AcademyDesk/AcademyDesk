// Real exported Schedule UI, synthetic API transport. No live Identity/SQL acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','schedule-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;
 const checks=[],errors=[];
 try {
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of ['normal','refresh-failure','rejected']){
   const context=await browser.newContext({viewport:{width,height:900},timezoneId:'America/Los_Angeles'}),page=await context.newPage(),calls=[],pageErrors=[];
   page.on('pageerror',e=>pageErrors.push(e.message));page.on('console',m=>{if(m.type()==='error')pageErrors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-schedule-token');},theme);
   const sessions=[{id:'existing',batchId:'b',startUtc:'2026-10-15T04:30:00Z',endUtc:'2026-10-15T05:30:00Z',deliveryMode:'InPerson',roomName:null,status:'Scheduled'}];
   await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url());
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const method=request.method();calls.push({path:url.pathname,method,body:request.postDataJSON()});
     if(['POST','PUT'].includes(method)){
      assert.match(url.pathname,/^\/api\/academies\/owned\/sessions(\/existing)?$/);
      if(scenario==='rejected')return route.fulfill({status:400,contentType:'application/json',body:'{}',headers:{'Access-Control-Allow-Origin':origin}});
      const body=request.postDataJSON();if(method==='POST')sessions.push({...body,id:'created',status:'Scheduled'});else Object.assign(sessions[0],body);
      return route.fulfill({status:method==='POST'?201:204,body:'',headers:{'Access-Control-Allow-Origin':origin}});
     }
     assert.equal(method,'GET');
     if(scenario==='refresh-failure'&&calls.some(x=>['POST','PUT'].includes(x.method))&&url.pathname.endsWith('/sessions'))return route.fulfill({status:503,body:'{}',headers:{'Access-Control-Allow-Origin':origin}});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/batches':[{id:'b',name:'Fixture batch'}],'/api/academies/owned/teachers':[], '/api/academies/owned/branches':[], '/api/academies/owned/sessions':sessions};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,contentType:'application/json',body:JSON.stringify(data[url.pathname]),headers:{'Access-Control-Allow-Origin':origin}});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/schedule');await page.getByLabel('Status for Fixture batch').waitFor();
   const start=page.getByLabel('Start time (IST)'),end=page.getByLabel('End time (IST)');
   await start.fill('2026-10-16T10:00');await end.fill('2026-10-16T11:00');await page.getByLabel('Room',{exact:true}).fill('Synthetic room');
   await page.getByRole('button',{name:'Schedule class',exact:true}).click();
   const notice=page.getByRole('status');await notice.filter({hasText:scenario==='rejected'?'could not be saved':'Class scheduled successfully.'}).waitFor();
   await page.getByRole('button',{name:'Schedule class',exact:true}).waitFor();await page.waitForFunction(()=>!document.querySelector('form button').disabled);
   assert.equal(await start.inputValue(),scenario==='rejected'?'2026-10-16T10:00':'');assert.equal(calls.filter(x=>x.method==='POST').length,1);
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*do not submit/);
   if(scenario==='normal')assert.equal(await page.getByLabel('Status for Fixture batch').count(),2);
   await page.getByLabel('Status for Fixture batch').first().selectOption('Cancelled');
   await notice.filter({hasText:scenario==='rejected'?'could not be updated':'Class status updated to Cancelled.'}).waitFor();await page.waitForFunction(()=>!document.querySelector('form button').disabled);
   assert.equal(calls.filter(x=>x.method==='PUT').length,1);
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*do not submit/);
   const payload=calls.find(x=>x.method==='POST').body;assert.equal(payload.startUtc,'2026-10-16T04:30:00.000Z');assert.equal(payload.endUtc,'2026-10-16T05:30:00.000Z');assert.equal(payload.teacherId,null);assert.equal(payload.branchId,null);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await notice.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,`${width}-${theme}-${scenario}.png`),fullPage:true});
   // Deliberate 400/503 resource errors are expected; JS/hydration/other errors never are.
   const expectedCode=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':null;
   const unexpected=pageErrors.filter(x=>!expectedCode||!x.startsWith('Failed to load resource:')||!x.includes(expectedCode));assert.deepEqual(unexpected,[]);errors.push(...unexpected);
   checks.push({width,theme,scenario,calls,notice:await notice.textContent(),expectedResourceErrors:pageErrors.length});console.log(`PASS SCHEDULE ${width} ${theme} ${scenario}`);await context.close();
  }
  assert.equal(checks.length,12);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,transport:'synthetic API; not live SQL/Identity',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
