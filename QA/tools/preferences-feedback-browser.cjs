// Exported Preferences UI/synthetic transport; not live Identity/SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','preferences-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;const checks=[];
 try{
  browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of[320,1440])for(const theme of['light','dark'])for(const type of['Guardian','Student'])for(const scenario of['normal','refresh-failure','rejected','uncertain-committed']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];let prefs=[{recipientType:'Guardian',recipientId:'guardian-id',emailAllowed:true,whatsAppAllowed:false,marketingAllowed:true,notes:'Existing consent'}];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-preferences-token');},theme);
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:req.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='PUT'){
      assert.equal(url.pathname,'/api/academies/owned/communication-preferences/'+type+'/'+(type==='Guardian'?'guardian-id':'student-id'));
      const saved={...req.postDataJSON(),recipientType:type,recipientId:type==='Guardian'?'guardian-id':'student-id'};
      if(scenario!=='rejected')prefs=prefs.filter(x=>x.recipientType!==type).concat(saved);
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:200,headers,contentType:'application/json',body:scenario==='rejected'||scenario==='uncertain-committed'?JSON.stringify({message:'Synthetic guidance'}):JSON.stringify(saved)});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/communication-preferences')&&scenario==='refresh-failure'&&calls.some(x=>x.method==='PUT'))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Engagement"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],'/api/academies/owned/communication-preferences/recipients':{students:[{id:'student-id',firstName:'Synthetic',lastName:'Student'}],guardians:[{id:'guardian-id',firstName:'Synthetic',lastName:'Parent'}]},'/api/academies/owned/communication-preferences':prefs};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/communication-preferences');
   async function choose(name,label){await page.locator('input[name="'+name+'"]').locator('..').getByRole('button').click();await page.getByRole('option',{name:label,exact:true}).click();}
   await page.locator('input[name="recipientType"]').waitFor({state:'attached'});
   if(type==='Student')await choose('recipientType','Student');
   await choose('recipientId','Synthetic '+(type==='Guardian'?'Parent':'Student'));
   const email=page.getByRole('checkbox',{name:/^Email/}),whatsApp=page.getByRole('checkbox',{name:/^WhatsApp/}),marketing=page.getByRole('checkbox',{name:/^Marketing/}),notes=page.getByLabel('Consent notes',{exact:true});
   assert.equal(await email.isChecked(),type==='Guardian');assert.equal(await marketing.isChecked(),type==='Guardian');
   await email.uncheck();await whatsApp.check();await marketing.uncheck();await notes.fill('Synthetic opt-out notes');
   await page.getByRole('button',{name:'Save preferences',exact:true}).click();
   const notice=page.getByRole('status'),failed=['rejected','uncertain-committed'].includes(scenario);
   await notice.filter({hasText:failed?'Synthetic guidance':'Contact preferences saved.'}).waitFor();
   await page.waitForFunction(()=>!document.querySelector('.preferences-fields').disabled);
   assert.equal(await notes.inputValue(),'Synthetic opt-out notes');assert.equal(await email.isChecked(),false);assert.equal(await whatsApp.isChecked(),true);assert.equal(await marketing.isChecked(),false);
   assert.equal(calls.filter(x=>x.method==='PUT').length,1);assert.deepEqual(calls.find(x=>x.method==='PUT').body,{emailAllowed:false,whatsAppAllowed:true,marketingAllowed:false,notes:'Synthetic opt-out notes'});
   assert.equal(await page.locator('input[name="recipientId"]').inputValue(),type==='Guardian'?'guardian-id':'student-id');
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*do not repeat/);
   if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/could not be confirmed.*before retrying/);
   if(scenario!=='rejected')assert.equal(prefs.find(x=>x.recipientType===type).whatsAppAllowed,true);
   assert.equal(await notice.getAttribute('aria-live'),'polite');assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   const status=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   assert.deepEqual(errors.filter(x=>!status||!x.startsWith('Failed to load resource:')||!x.includes(status)),[]);
   assert.equal(calls.some(x=>x.path.endsWith('/students')||x.path.endsWith('/guardians')),false);
   await page.screenshot({path:path.join(evidence,width+'-'+theme+'-'+type+'-'+scenario+'.png'),fullPage:true});
   checks.push({width,theme,type,scenario,notice:await notice.textContent()});console.log('PASS PREFERENCES '+width+' '+theme+' '+type+' '+scenario);await context.close();
  }
  assert.equal(checks.length,32);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API only, not live SQL/auth/consent delivery',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:32,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
