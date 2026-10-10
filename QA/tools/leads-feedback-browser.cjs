// Production-export UI with synthetic API transport; no customer data or SQL.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','leads-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
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
  for(const width of[320,1440])for(const theme of['light','dark'])for(const kind of['create','stage','convert'])for(const scenario of(kind==='convert'?['normal','refresh-failure','rejected','uncertain-committed','cancel']:['normal','refresh-failure','rejected','uncertain-committed'])){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[],dialogs=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   page.on('dialog',async dialog=>{dialogs.push({type:dialog.type(),message:dialog.message()});if(scenario==='cancel')await dialog.dismiss();else await dialog.accept();});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-leads-token');},theme);
   const leads=[{id:'lead',fullName:'Synthetic lead',source:'Website',stage:'New',followUpAtUtc:null,convertedStudentId:null},{id:'converted',fullName:'Already converted',source:'Website',stage:'Converted',convertedStudentId:'student'}];
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:req.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(['POST','PATCH'].includes(method)){
      assert.equal(url.pathname,'/api/academies/owned/leads'+(kind==='create'?'':kind==='stage'?'/lead/stage':'/lead/convert'));assert.equal(method,kind==='stage'?'PATCH':'POST');const body=req.postDataJSON();
      if(scenario!=='rejected'){if(kind==='create')leads.push({...body,id:'created',stage:'New'});else if(kind==='stage')leads[0].stage=body.stage;else{leads[0].stage='Converted';leads[0].convertedStudentId='new-student';}}
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:kind==='create'?201:200,headers,contentType:'application/json',body:'{}'});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/leads')&&scenario==='refresh-failure'&&calls.some(c=>['POST','PATCH'].includes(c.method)))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Sales","AcademicGovernance"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/leads':leads};
     assert.ok(Object.hasOwn(data,url.pathname),url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/leads');await page.getByText('Synthetic lead',{exact:true}).waitFor();
   const name=page.getByPlaceholder('Full name'),program=page.getByPlaceholder('e.g. Guitar'),notes=page.getByPlaceholder('Enquiry notes'),converted=page.getByRole('button',{name:'Converted to student',exact:true});
   assert.equal(await converted.count(),1);assert.ok(await converted.isDisabled());assert.ok(await page.locator('.lead-record-header .standard-select-trigger').nth(1).isDisabled());
   await name.fill(' Draft lead ');await program.fill('Guitar');await notes.fill(' Synthetic notes ');
   await page.locator('.leads-fields .standard-select-trigger').click();await page.getByRole('option',{name:'Instagram',exact:true}).click();
   if(kind==='create')await page.getByRole('button',{name:'Add lead',exact:true}).click();
   else if(kind==='stage'){await page.locator('.lead-record-header .standard-select-trigger').first().click();await page.getByRole('option',{name:'Follow-up',exact:true}).click();}
   else await page.getByRole('button',{name:'Convert to student',exact:true}).click();
   const notice=page.getByRole('status'),failed=['rejected','uncertain-committed'].includes(scenario),success=kind==='create'?'Lead added to the pipeline.':kind==='stage'?'Synthetic lead moved to Follow-up.':'Synthetic lead was converted to a student. You can now enrol them in a batch.';
   if(scenario!=='cancel')await notice.filter({hasText:failed?(scenario==='rejected'?'could not':'could not be confirmed'):success}).waitFor();
   await page.waitForFunction(()=>!document.querySelector('.leads-fields').disabled);
   assert.equal(await name.inputValue(),kind==='create'&&!failed?'':' Draft lead ');assert.equal(await notes.inputValue(),kind==='create'&&!failed?'':' Synthetic notes ');assert.equal(await program.inputValue(),kind==='create'&&!failed?'':'Guitar');
   assert.equal(await page.locator('input[name="source"]').inputValue(),'Instagram');assert.equal(await page.locator('input[name="follow-up-time"]').inputValue(),'10:00');
   const writes=calls.filter(c=>['POST','PATCH'].includes(c.method));assert.equal(writes.length,scenario==='cancel'?0:1);
   if(scenario!=='cancel')assert.deepEqual(writes[0].body,kind==='stage'?{stage:'FollowUp',followUpAtUtc:null}:kind==='convert'?{firstName:null,lastName:null}:{fullName:' Draft lead ',email:null,phone:null,dateOfBirth:null,parentName:null,programInterest:'Guitar',source:'Instagram',followUpAtUtc:null,notes:' Synthetic notes '});
   if(kind==='convert')assert.deepEqual(dialogs,[{type:'confirm',message:'Convert Synthetic lead into a student record?'}]);else assert.equal(dialogs.length,0);
   if(scenario==='cancel')assert.equal(await notice.count(),0);
   else{
    if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed; do not repeat/);
    if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/before retrying/);
    assert.equal(await notice.getAttribute('aria-live'),'polite');await notice.scrollIntoViewIfNeeded();
   }
   assert.equal(leads.length,kind==='create'&&scenario!=='rejected'?3:2);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   const status=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   assert.deepEqual(errors.filter(x=>!status||!x.startsWith('Failed to load resource:')||!x.includes(status)),[]);
   await page.screenshot({path:path.join(evidence,width+'-'+theme+'-'+kind+'-'+scenario+'.png'),fullPage:true});
   checks.push({width,theme,kind,scenario,calls,dialogs,notice:scenario==='cancel'?null:await notice.textContent()});console.log('PASS LEADS '+width+' '+theme+' '+kind+' '+scenario);await context.close();
  }
  assert.equal(checks.length,52);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API; no live Identity/SQL/admissions acceptance',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
