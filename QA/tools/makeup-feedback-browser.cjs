// Exported Make-up UI/synthetic transport; not live Identity/SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','makeup-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
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
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of ['normal','refresh-failure','rejected','uncertain-committed']){
   const context=await browser.newContext({viewport:{width,height:900},timezoneId:'America/Los_Angeles'}),page=await context.newPage(),calls=[],pageErrors=[];
   page.on('pageerror',e=>pageErrors.push(e.message));page.on('console',m=>{if(m.type()==='error')pageErrors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-makeup-token');},theme);
   const rows=[{id:'existing',studentId:'student',batchId:'batch',startUtc:'2026-10-31T03:30:00Z',deliveryMode:'Offline',venue:'Original room',usesNextScheduledClass:false,status:'Scheduled'}];
   await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url()),method=request.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:request.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(['POST','PATCH'].includes(method)){
      assert.equal(url.pathname,'/api/academies/owned/makeup-classes'+(method==='PATCH'?'/existing':''));
      const body=request.postDataJSON();if(scenario!=='rejected'){if(method==='POST')rows.push({...body,id:'created',usesNextScheduledClass:body.useNextScheduledClass,status:'Scheduled'});else Object.assign(rows[0],body);}
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:method==='POST'?201:204,headers,body:scenario==='rejected'||scenario==='uncertain-committed'?JSON.stringify({message:'Synthetic service guidance'}):''});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/makeup-classes')&&scenario==='refresh-failure'&&calls.some(x=>['POST','PATCH'].includes(x.method)))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/students':[{id:'student',firstName:'Synthetic',lastName:'Student'}],'/api/academies/owned/batches':[{id:'batch',name:'Fixture batch'}],'/api/academies/owned/teachers':[], '/api/academies/owned/makeup-classes':rows};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   const choose=async(name,label)=>{await page.locator(`input[name="${name}"]`).locator('..').getByRole('button').click();await page.getByRole('option',{name:label,exact:true}).click();};
   await page.goto(origin+'/makeup');await page.locator('input[name="makeup-status-existing"]').waitFor({state:'attached'});
   await choose('student','Synthetic Student');await choose('batch','Fixture batch');
   await page.locator('.standard-date-trigger').click();await page.getByLabel('Select year').selectOption('2026');await page.getByLabel('Select month').selectOption('9');await page.locator('.standard-date-days button[data-current="true"]').filter({hasText:/^31$/}).click();
   const room=page.getByPlaceholder('Room or venue (optional)');await room.fill('Synthetic room');await page.getByRole('button',{name:'Schedule make-up',exact:true}).click();
   const notice=page.getByRole('status'),failed=scenario==='rejected'||scenario==='uncertain-committed';
   await notice.filter({hasText:failed?'Synthetic service guidance':'Make-up class scheduled.'}).waitFor();await page.waitForFunction(()=>!document.querySelector('.makeup-create').disabled);
   assert.equal(await room.inputValue(),failed?'Synthetic room':'');assert.equal(await page.locator('input[name="makeup-date"]').inputValue(),failed?'2026-10-31':'');
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*do not submit/);
   if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/could not be confirmed.*before retrying/);
   assert.equal(calls.filter(x=>x.method==='POST').length,1);
   assert.deepEqual(calls.find(x=>x.method==='POST').body,{studentId:'student',batchId:'batch',teacherId:null,startUtc:'2026-10-31T03:30:00.000Z',deliveryMode:'Offline',venue:'Synthetic room',meetingLink:null,useNextScheduledClass:false,notes:null});
   await choose('makeup-status-existing','Cancelled');await notice.filter({hasText:failed?'Synthetic service guidance':'Make-up status updated to Cancelled.'}).waitFor();await page.waitForFunction(()=>!document.querySelector('.makeup-create').disabled);
   assert.equal(calls.filter(x=>x.method==='PATCH').length,1);assert.deepEqual(calls.find(x=>x.method==='PATCH').body,{status:'Cancelled'});
   assert.equal(rows[0].status,scenario==='rejected'?'Scheduled':'Cancelled');assert.equal(rows.length,scenario==='rejected'?1:2);
   if(scenario==='uncertain-committed')assert.doesNotMatch(await notice.textContent(),/updated to Cancelled/);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await notice.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,`${width}-${theme}-${scenario}.png`),fullPage:true});
   const code=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   const unexpected=pageErrors.filter(x=>!code||!x.startsWith('Failed to load resource:')||!x.includes(code));assert.deepEqual(unexpected,[]);errors.push(...unexpected);
   checks.push({width,theme,scenario,calls,notice:await notice.textContent(),expectedResourceErrors:pageErrors.length});console.log(`PASS MAKEUP ${width} ${theme} ${scenario}`);await context.close();
  }
  assert.equal(checks.length,16);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,transport:'synthetic API, not live Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
