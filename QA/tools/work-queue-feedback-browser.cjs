// Production-export UI with synthetic API transport; no customer data or SQL.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','work-queue-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{console.error('Synthetic static server missing path:',req.url);res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;const checks=[];
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of[320,1440])for(const theme of['light','dark'])for(const kind of['create','move'])for(const scenario of['normal','refresh-failure','rejected','uncertain-committed']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-work-queue-token');},theme);
   let rows=[{id:'existing',title:'Existing action',type:'Operations',priority:'Normal',status:'Open'}];
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:req.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(['POST','PATCH'].includes(method)){
      assert.equal(url.pathname,kind==='create'?'/api/academies/owned/admin-work-items':'/api/academies/owned/admin-work-items/existing/status');
      const body=req.postDataJSON();if(scenario!=='rejected'){if(kind==='create')rows.push({...body,id:'created',status:'Open'});else rows[0]={...rows[0],status:body.status};}
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:kind==='create'?201:200,headers,contentType:'application/json',body:'{}'});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/admin-work-items')&&scenario==='refresh-failure'&&calls.some(c=>['POST','PATCH'].includes(c.method)))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","AcademicGovernance","Certificates","Communication","Operations"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/staff':[], '/api/academies/owned/admin-work-items':rows};
     assert.ok(Object.hasOwn(data,url.pathname),url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/work-queue');await page.getByRole('heading',{name:'Existing action',exact:true}).waitFor();
   const title=page.getByPlaceholder('Action title'),description=page.getByPlaceholder('Context, owner expectation and next action');
   await title.fill('Synthetic action');await description.fill('Synthetic context');
   if(kind==='create')await page.getByRole('button',{name:'Add to queue',exact:true}).click();
   else {await page.locator('.work-queue-status button').click();await page.getByRole('option',{name:'Completed',exact:true}).click();}
   const notice=page.getByRole('status'),failed=['rejected','uncertain-committed'].includes(scenario);
   await notice.filter({hasText:failed?(scenario==='rejected'?'could not be saved':'could not be confirmed'):(kind==='create'?'Work item added':'Work item moved')}).waitFor();
   await page.waitForFunction(()=>!document.querySelector('.work-queue-fields').disabled);
   assert.equal(await title.inputValue(),kind==='create'&&!failed?'':'Synthetic action');
   assert.equal(await description.inputValue(),kind==='create'&&!failed?'':'Synthetic context');
   const writes=calls.filter(c=>['POST','PATCH'].includes(c.method));assert.equal(writes.length,1);
   assert.deepEqual(writes[0].body,kind==='create'?{type:'Operations',title:'Synthetic action',description:'Synthetic context',priority:'Normal',entityType:null,entityId:null,assignedUserId:null,dueAtUtc:null}:{status:'Completed'});
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*do not repeat/);
   if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/before retrying/);
   assert.equal(rows.length,kind==='create'&&scenario!=='rejected'?2:1);
   assert.equal(await notice.getAttribute('aria-live'),'polite');assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   const code=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   assert.deepEqual(errors.filter(x=>!code||!x.startsWith('Failed to load resource:')||!x.includes(code)),[]);
   await notice.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,`${width}-${theme}-${kind}-${scenario}.png`),fullPage:true});
   checks.push({width,theme,kind,scenario,calls,notice:await notice.textContent()});console.log(`PASS WORK QUEUE ${width} ${theme} ${kind} ${scenario}`);await context.close();
  }
  assert.equal(checks.length,32);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API; no live Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
