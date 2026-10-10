// Production-export UI with synthetic API transport; no customer data or SQL.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','platform-services-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
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
  for(const width of[320,1440])for(const theme of['light','dark'])for(const kind of['payment','create','reply'])for(const scenario of['normal','refresh-failure','rejected','uncertain-committed']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-platform-services-token');},theme);
   const invoices=[{id:'invoice',invoiceNumber:'Synthetic invoice',amount:1000,currency:'INR',status:'Open',periodStart:'2026-10-01',periodEnd:'2026-10-31',dueDate:'2026-10-31'}];
   const cases=[{id:'case',subject:'Synthetic case',priority:'Normal',status:'Open',description:'Existing case',createdAtUtc:'2026-10-10T12:00:00Z'},{id:'other',subject:'Other case',priority:'Normal',status:'Open',description:'Other case',createdAtUtc:'2026-10-10T12:00:00Z'}];
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:req.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='POST'){
      const expected='/api/academies/owned/platform-services/'+(kind==='payment'?'billing-invoices/invoice/payment-submission':kind==='reply'?'support-cases/case/response':'support-cases');
      assert.equal(url.pathname,expected);const body=req.postDataJSON();
      if(scenario!=='rejected'){if(kind==='payment'){invoices[0].status='Payment submitted';invoices[0].paymentReference=body.reference;}else if(kind==='reply')cases[0].academyResponse=body.message;else cases.push({...body,id:'created',status:'Open',createdAtUtc:'2026-10-10T12:00:00Z'});}
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:kind==='create'?201:200,headers,contentType:'application/json',body:'{}'});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/billing-invoices')&&scenario==='refresh-failure'&&calls.some(c=>c.method==='POST'))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","AcademicGovernance","Certificates","Communication","Operations"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/platform-services/billing-invoices':invoices,'/api/academies/owned/platform-services/support-cases':cases};
     assert.ok(Object.hasOwn(data,url.pathname),url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/platform-services');await page.getByText('Synthetic invoice',{exact:true}).waitFor();
   const subject=page.getByPlaceholder('What do you need help with?'),description=page.getByPlaceholder('Describe the issue, context, and expected outcome.'),reference=page.getByRole('textbox',{name:'Payment reference for Synthetic invoice'}),reply=page.getByRole('textbox',{name:'Update for Synthetic case'}),other=page.getByRole('textbox',{name:'Update for Other case'});
   await subject.fill(' Synthetic subject ');await description.fill(' Synthetic description ');await reference.fill(' Reference ');await reply.fill(' Synthetic update ');await other.fill('Other draft');
   await page.locator('.platform-support-form .standard-select-trigger').click();await page.getByRole('option',{name:'High',exact:true}).click();
   await page.getByRole('button',{name:kind==='payment'?'Submit payment':kind==='create'?'Send support request':'Send update',exact:true}).first().click();
   const notice=page.getByRole('status'),failed=['rejected','uncertain-committed'].includes(scenario),success=kind==='payment'?'Payment submitted for Platform Owner review.':kind==='create'?'Support request sent to the Platform Owner.':'Response sent to the Platform Owner.';
   await notice.filter({hasText:failed?(scenario==='rejected'?'entries have been retained':'could not be confirmed'):success}).waitFor();await page.waitForFunction(()=>!document.querySelector('input[name="subject"]').disabled);
   assert.equal(await subject.inputValue(),kind==='create'&&!failed?'':' Synthetic subject ');assert.equal(await description.inputValue(),kind==='create'&&!failed?'':' Synthetic description ');
   assert.equal(await reply.inputValue(),kind==='reply'&&!failed?'':' Synthetic update ');assert.equal(await other.inputValue(),'Other draft');
   assert.equal(await page.locator('.platform-support-form input[name="priority"]').inputValue(),kind==='create'&&!failed?'Normal':'High');
   if(kind!=='payment'||scenario!=='normal')assert.equal(await reference.inputValue(),' Reference ');else await page.getByText('Submitted · Reference',{exact:false}).waitFor();
   const writes=calls.filter(c=>c.method==='POST');assert.equal(writes.length,1);assert.deepEqual(writes[0].body,kind==='payment'?{reference:' Reference '}:kind==='reply'?{message:' Synthetic update '}:{subject:' Synthetic subject ',priority:'High',description:' Synthetic description '});
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed; do not repeat/);
   if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/before retrying/);
   assert.equal(cases.length,kind==='create'&&scenario!=='rejected'?3:2);assert.equal(await notice.getAttribute('aria-live'),'polite');assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   const code=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   assert.deepEqual(errors.filter(x=>!code||!x.startsWith('Failed to load resource:')||!x.includes(code)),[]);
   await notice.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,width+'-'+theme+'-'+kind+'-'+scenario+'.png'),fullPage:true});
   checks.push({width,theme,kind,scenario,calls,notice:await notice.textContent()});console.log('PASS PLATFORM '+width+' '+theme+' '+kind+' '+scenario);await context.close();
  }
  assert.equal(checks.length,48);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API; no live Identity/SQL/payment/provider acceptance',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
