// Exported Holidays UI/synthetic transport; not live Identity/SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','holidays-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
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
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-holidays-token');},theme);
   let rows=[{id:'existing',name:'Existing holiday',holidayDate:'2026-10-31'}];
   await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url()),method=request.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:request.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(['POST','DELETE'].includes(method)){
      assert.ok(['/api/academies/owned/holidays','/api/academies/owned/holidays/india-2026-defaults','/api/academies/owned/holidays/existing'].includes(url.pathname));
      const body=request.postDataJSON();
      if(scenario!=='rejected'){if(method==='DELETE')rows=rows.filter(x=>x.id!=='existing');else if(url.pathname.endsWith('defaults'))rows.push({id:'default',name:'Fixture default',holidayDate:'2026-01-26'});else rows.push({...body,id:'created'});}
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:method==='DELETE'?204:200,headers,body:scenario==='rejected'||scenario==='uncertain-committed'?JSON.stringify({message:'Synthetic service guidance'}):''});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/holidays')&&scenario==='refresh-failure'&&calls.some(x=>['POST','DELETE'].includes(x.method)))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Certificates"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/holidays':rows};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/holidays');await page.getByRole('button',{name:'Remove',exact:true}).waitFor();
   const name=page.getByPlaceholder('Custom or regional holiday');await name.fill('Synthetic holiday');
   await page.locator('.standard-date-trigger').click();await page.getByLabel('Select year').selectOption('2026');await page.getByLabel('Select month').selectOption('9');await page.locator('.standard-date-days button[data-current="true"]').filter({hasText:/^31$/}).click();
   await page.getByRole('button',{name:'Add holiday',exact:true}).click();
   const notice=page.getByRole('status'),failed=scenario==='rejected'||scenario==='uncertain-committed';
   await notice.filter({hasText:failed?'Synthetic service guidance':'Holiday added.'}).waitFor();await page.waitForFunction(()=>!document.querySelector('.holidays-action').disabled);
   assert.equal(await name.inputValue(),failed?'Synthetic holiday':'');assert.equal(await page.locator('input[name="holiday-date"]').inputValue(),failed?'2026-10-31':'');
   assert.equal(calls.filter(x=>x.method==='POST').length,1);assert.deepEqual(calls.find(x=>x.method==='POST').body,{name:'Synthetic holiday',holidayDate:'2026-10-31',notes:'',isClosed:true,scope:'Custom'});
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*do not repeat/);
   if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/could not be confirmed.*before retrying/);
   await page.getByRole('button',{name:'Add India holidays 2026',exact:true}).click();await notice.filter({hasText:failed?'Synthetic service guidance':'Official India holidays added.'}).waitFor();await page.waitForFunction(()=>!document.querySelector('.holidays-action').disabled);
   assert.equal(calls.filter(x=>x.method==='POST').length,2);assert.equal(calls.find(x=>x.path.endsWith('defaults')).body,null);
   await page.getByRole('button',{name:'Remove',exact:true}).first().click();await notice.filter({hasText:failed?'Synthetic service guidance':'Holiday removed.'}).waitFor();await page.waitForFunction(()=>!document.querySelector('.holidays-action').disabled);
   assert.equal(calls.filter(x=>x.method==='DELETE').length,1);assert.equal(rows.some(x=>x.id==='existing'),scenario==='rejected');assert.equal(rows.length,scenario==='rejected'?1:2);
   if(scenario==='uncertain-committed')assert.doesNotMatch(await notice.textContent(),/Holiday removed/);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await notice.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,`${width}-${theme}-${scenario}.png`),fullPage:true});
   const code=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   const unexpected=pageErrors.filter(x=>!code||!x.startsWith('Failed to load resource:')||!x.includes(code));assert.deepEqual(unexpected,[]);errors.push(...unexpected);
   checks.push({width,theme,scenario,calls,notice:await notice.textContent(),expectedResourceErrors:pageErrors.length});console.log(`PASS HOLIDAYS ${width} ${theme} ${scenario}`);await context.close();
  }
  assert.equal(checks.length,16);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,transport:'synthetic API, not live Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
