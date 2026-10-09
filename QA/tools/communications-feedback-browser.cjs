// Exported Communications UI/synthetic transport; not live Identity/SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','communications-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
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
  for(const width of[320,1440])for(const theme of['light','dark'])for(const banner of[false,true])for(const scenario of['normal','refresh-failure','rejected','uncertain-committed']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];let rows=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-communications-token');},theme);
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:req.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='POST'){
      assert.equal(url.pathname,'/api/academies/owned/notifications');const body=req.postDataJSON();
      if(scenario!=='rejected')rows.push({...body,id:'created',status:'Queued'});
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:201,headers,contentType:'application/json',body:scenario==='rejected'||scenario==='uncertain-committed'?JSON.stringify({message:'Synthetic guidance'}):JSON.stringify(rows[0])});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/notifications')&&scenario==='refresh-failure'&&calls.some(x=>x.method==='POST'))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Engagement"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],'/api/academies/owned/students':[],'/api/academies/owned/teachers':[],'/api/academies/owned/guardians':[{id:'recipient',firstName:'Synthetic',lastName:'Parent'}],'/api/academies/owned/communication-templates':[],'/api/academies/owned/notifications':rows};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/communications');await page.getByText('No messages yet.',{exact:true}).waitFor();
   async function choose(name,label){await page.locator('input[name="'+name+'"]').locator('..').getByRole('button').click();await page.getByRole('option',{name:label,exact:true}).click();}
   if(banner)await choose('recipientType','Portal banner message');else await choose('recipientId','Synthetic Parent');
   const title=page.getByLabel('Title',{exact:true}),body=page.getByLabel('Message',{exact:true});await title.fill('Synthetic title');await body.fill('Synthetic body');
   await page.getByRole('button',{name:banner?'Publish announcement':'Queue message',exact:true}).click();
   const notice=page.getByRole('status'),failed=['rejected','uncertain-committed'].includes(scenario),success=banner?'Important announcement will run for 4 hours.':'Message queued.';
   await notice.filter({hasText:failed?'Synthetic guidance':success}).waitFor();
   await page.waitForFunction(()=>!document.querySelector('.messages-fields').disabled);
   if(await body.count()!==1){console.log(JSON.stringify(await page.evaluate(()=>({labels:[...document.querySelectorAll('label')].map(x=>x.textContent),textareas:[...document.querySelectorAll('textarea')].map(x=>x.outerHTML)}))));await page.screenshot({path:path.join(evidence,'locator-diagnostic.png'),fullPage:true});}
   assert.equal(await title.inputValue(),failed?'Synthetic title':'');assert.equal(await body.inputValue(),failed?'Synthetic body':'');
   assert.equal(calls.filter(x=>x.method==='POST').length,1);
   assert.deepEqual(calls.find(x=>x.method==='POST').body,{recipientId:banner?null:'recipient',recipientType:banner?'Academy':'Guardian',title:'Synthetic title',message:'Synthetic body',channel:'InApp',scheduledAtUtc:null,templateId:null,variables:banner?{audiences:'Student,Teacher'}:{},isImportant:banner,displayHours:banner?4:null});
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*do not repeat/);
   if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/could not be confirmed.*before retrying/);
   assert.equal(rows.length,scenario==='rejected'?0:1);assert.equal(await notice.getAttribute('aria-live'),'polite');
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   const status=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   assert.deepEqual(errors.filter(x=>!status||!x.startsWith('Failed to load resource:')||!x.includes(status)),[]);
   await page.screenshot({path:path.join(evidence,width+'-'+theme+'-'+(banner?'banner':'direct')+'-'+scenario+'.png'),fullPage:true});
   checks.push({width,theme,banner,scenario,notice:await notice.textContent()});console.log('PASS MESSAGES '+width+' '+theme+' '+banner+' '+scenario);await context.close();
  }
  assert.equal(checks.length,32);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API only, no sends/Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:32,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
