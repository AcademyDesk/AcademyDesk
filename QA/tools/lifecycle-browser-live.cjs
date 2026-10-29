// Production export -> loopback relay -> real Identity/API -> disposable SQL.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api,run,child]=process.argv.slice(2),target=new URL(api);assert.equal(target.hostname,'127.0.0.1');assert.equal(target.protocol,'http:');assert.ok(target.port);assert.match(run,/^[a-f0-9]{32}$/);assert.match(child,/^[a-f0-9-]{36}$/);
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE/lifecycle-browser',run);fs.mkdirSync(evidence,{recursive:true});
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':({'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const actor of ['teacher','guardian']){
   const email=actor==='teacher'?'qa-teacher-a@example.invalid':'qa-browser-parent@example.invalid',workspace=actor==='teacher'?'Teacher':'Portal',route=actor==='teacher'?'/teacher':'/portal';
   const auth=await browser.newContext();const login=await auth.request.post(api+'/api/auth/login',{data:{email,password:'Synthetic!39Ab'}});assert.equal(login.status(),200);const token=(await login.json()).accessToken;
   for(const width of [320,1440])for(const theme of ['light','dark']){
    const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),errors=[],responses=[];let expectedDenial=false;
    await context.addInitScript(({theme,token,workspace})=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.'+workspace,token);},{theme,token,workspace});
    page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'&&!(expectedDenial&&/Failed to load resource:.*403/.test(m.text())))errors.push(m.text());});
    await context.route('**/*',async r=>{const req=r.request(),url=new URL(req.url());try{
     if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
      assert.equal(req.method(),'GET','Portal must not mutate during access checks');const response=await r.fetch({url:api+url.pathname+url.search});assert.notEqual(response.status(),429);assert.ok([200,403].includes(response.status()),'Unexpected response '+url.pathname+' '+response.status());responses.push({path:url.pathname,status:response.status()});
      return r.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
     }
     if(url.origin===origin||url.protocol==='data:')return r.continue();errors.push('Unexpected external request');return r.abort('blockedbyclient');
    }catch(e){errors.push('Route '+url.pathname+' failed: '+e.name);try{await r.abort('failed');}catch{errors.push('Route teardown failed');}}});
    const active=async()=>{await page.getByRole('heading',{name:actor==='teacher'?'Welcome, Synthetic':'Good day, Browser',exact:true}).waitFor();await page.waitForLoadState('networkidle');assert.equal(await page.locator('.learner-state').count(),0);assert.ok(!/Lifecycle foreign sentinel|Lifecycle unassigned sentinel/.test(await page.locator('.learner-content').innerText()));};
    const control=async(state)=>{const response=await auth.request.post(api+`/__qa/lifecycle/${actor}/${state}`,{headers:{'X-QA-Run':run}});assert.equal(response.status(),200);};
    await page.goto(origin+route);await active();await page.screenshot({path:path.join(evidence,`${actor}-${width}-${theme}-active.png`),fullPage:true});
    await control('revoked');expectedDenial=true;await page.reload();
    const message=actor==='teacher'?'This account is not linked to an active teacher profile. Please contact your Academy Admin.':'This account is not linked to a student or Parent portal.';
    await page.getByRole('status').filter({hasText:message}).waitFor();await page.waitForLoadState('networkidle');assert.equal(await page.locator('.learner-heading').count(),0);assert.equal(await page.locator('.learner-kpis').count(),0);assert.equal(await page.locator('.learner-switcher').count(),0);assert.equal(await page.locator('form').count(),0);
    const denied=await context.request.get(api+(actor==='teacher'?'/api/teacher/me':`/api/portal/students/${child}`),{headers:{Authorization:'Bearer '+token}});assert.equal(denied.status(),403);const body=await denied.text();assert.ok(!/Browser Permitted Minor|Lifecycle own sentinel|Lifecycle foreign sentinel/.test(body));
    await page.screenshot({path:path.join(evidence,`${actor}-${width}-${theme}-revoked.png`),fullPage:true});
    await control('active');await page.reload();await active();await page.waitForTimeout(900);expectedDenial=false;
    assert.equal(await page.evaluate(workspace=>localStorage.getItem('academydesk.accessToken.'+workspace),workspace),token,'Restoration must retain original signed token');assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
    assert.ok(responses.filter(x=>x.path===(actor==='teacher'?'/api/teacher/me':'/api/portal/me')&&x.status===200).length>=2);if(actor==='teacher')assert.ok(responses.some(x=>x.path==='/api/teacher/me'&&x.status===403));
    checks.push({actor,width,theme,states:['active','revoked-reload','restored-same-token-reload'],responses});await context.unrouteAll({behavior:'wait'});await context.close();console.log(`REVOCATION BROWSER PASS ${actor}/${width}/${theme}: own workspace, real revoke/denial/no protected UI, same-token restore/no writes/errors.`);
   }
   await auth.close();
  }
  assert.equal(checks.length,8);fs.writeFileSync(path.join(evidence,'receipt.json'),JSON.stringify({classification:'real Identity/API/disposable SQL linked browser; reload boundary only; physical devices NOT RUN',checks},null,2));console.log('REVOCATION BROWSER RECEIPT '+evidence);
 }finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{const summary=String(e.message).split(/\r?\n/)[0].replace(/Bearer\s+\S+/gi,'Bearer [REDACTED]');console.error('Lifecycle browser failed: '+e.name+': '+summary+'; request logs/stacks omitted.');process.exitCode=1;});
