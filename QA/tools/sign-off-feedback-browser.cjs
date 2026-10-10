// Production-export UI with synthetic API transport; no customer data or SQL.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','sign-off-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
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
  for(const width of[320,1440])for(const theme of['light','dark'])for(const scenario of['normal','refresh-failure','rejected','uncertain-committed']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-sign-off-token');},theme);
   let rows=[];
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:req.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='POST'){
      assert.equal(url.pathname,'/api/academies/owned/access-reviews');
      if(scenario!=='rejected')rows.push({...req.postDataJSON(),id:'created',reviewedAtUtc:'2026-10-10T12:00:00Z'});
      return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:201,headers,contentType:'application/json',body:'{}'});
     }
     assert.equal(method,'GET');
     if(url.pathname.endsWith('/access-reviews')&&scenario==='refresh-failure'&&calls.some(c=>c.method==='POST'))return route.fulfill({status:503,headers,body:'{}'});
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","AcademicGovernance","Certificates","Communication","Operations"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/access-reviews':rows};
     assert.ok(Object.hasOwn(data,url.pathname),url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/access-review/sign-off');await page.getByText('No access reviews have been signed off.',{exact:true}).waitFor();
   const notes=page.getByRole('textbox',{name:'Review notes'}),followUp=page.getByRole('checkbox',{name:'Create high-priority remediation task'}),checked=['normal','uncertain-committed'].includes(scenario);
   await notes.fill('  Synthetic review notes  ');if(checked)await followUp.check();
   await page.getByRole('button',{name:'Record sign-off',exact:true}).click();
   const notice=page.getByRole('status'),failed=['rejected','uncertain-committed'].includes(scenario);
   await notice.filter({hasText:failed?(scenario==='rejected'?'could not be saved':'could not be confirmed'):'Access review signed off.'}).waitFor();
   await page.getByRole('button',{name:'Record sign-off',exact:true}).waitFor();await page.waitForFunction(()=>!document.querySelector('form fieldset').disabled);
   assert.equal(await notes.inputValue(),failed?'  Synthetic review notes  ':'');assert.equal(await followUp.isChecked(),failed&&checked);
   const writes=calls.filter(c=>c.method==='POST');assert.equal(writes.length,1);assert.deepEqual(writes[0].body,{notes:'  Synthetic review notes  ',createFollowUp:checked});
   assert.equal(rows.length,scenario==='rejected'?0:1);
   if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed; do not repeat/);
   if(scenario==='uncertain-committed')assert.match(await notice.textContent(),/before retrying/);
   assert.equal(await notice.getAttribute('aria-live'),'polite');assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   const code=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;
   assert.deepEqual(errors.filter(x=>!code||!x.startsWith('Failed to load resource:')||!x.includes(code)),[]);
   await notice.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,width+'-'+theme+'-'+scenario+'.png'),fullPage:true});
   checks.push({width,theme,scenario,calls,notice:await notice.textContent()});console.log('PASS SIGN-OFF '+width+' '+theme+' '+scenario);await context.close();
  }
  assert.equal(checks.length,16);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API; no live Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
