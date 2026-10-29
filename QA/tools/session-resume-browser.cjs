// Actual exported React pages. Synthetic credentials/API and controlled lifecycle
// dispatch simulate missed events; this is NOT physical suspension/BFCache/SQL proof.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=require('./web-candidate-export.cjs').resolveExport(root);
const evidence=path.join(root,'QA/EVIDENCE','session-resume-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const source=path.join(root,'apps/web/src/components/workspace-frame.tsx');
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
 if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});
 if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
const routes={AcademyAdmin:'/reports',Teacher:'/teacher/ai',Portal:'/portal/ai',Platform:'/platform/ai'};
(async()=>{let browser;const checks=[],failures=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const [workspace,urlPath]of Object.entries(routes))for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of ['focus','pageshow','visibilitychange','local','rotation']){
   const context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage(),errors=[],requests=[];
   try{
    page.on('pageerror',error=>errors.push(error.message));
    await context.addInitScript(({workspace,theme})=>{localStorage.setItem('academydesk.theme',theme);for(const w of ['AcademyAdmin','Teacher','Portal','Platform']){localStorage.setItem('academydesk.accessToken.'+w,'synthetic-'+w);localStorage.setItem('academydesk.refreshToken.'+w,'synthetic-refresh-'+w);}}, {workspace,theme});
    await context.route('**/*',route=>{const request=route.request(),url=new URL(request.url());
     if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
      assert.equal(request.method(),'GET');requests.push({path:url.pathname,authorized:Boolean(request.headers().authorization)});
      const rows={'/api/auth/session':{workspace,academyId:'synthetic',displayName:'Synthetic account',roles:[workspace==='Portal'?'Student':workspace==='Platform'?'PlatformOwner':workspace],isPlatformOwner:workspace==='Platform'},'/api/academies':[{id:'synthetic',name:'Synthetic academy'}],'/api/portal/announcements':[]};
      let body=rows[url.pathname];
      if(body===undefined&&workspace==='AcademyAdmin'&&/^\/api\/academies\/synthetic\/(students|teachers|batches|enrollments|sessions|invoices|payments|expenses|payroll\/payouts)$/.test(url.pathname))body=[];
      assert.notEqual(body,undefined,'Unexpected API: '+url.pathname);
      return route.fulfill({status:200,headers:{'Access-Control-Allow-Origin':origin},contentType:'application/json',body:JSON.stringify(body)});
     }
     if(url.origin===origin||url.protocol==='data:'||url.protocol==='blob:')return route.continue();return route.abort('blockedbyclient');
    });
    await page.goto(origin+urlPath);
    if(workspace==='AcademyAdmin'){
     await page.locator('.workspace-reports-export-card').first().waitFor();
     // Export cards are server-rendered before the Reports effect finishes. Wait
     // for its actual loaded state before attributing traffic to the resume event.
     await page.locator('.workspace-reports-message').waitFor({state:'hidden'});
     await page.getByText('Synthetic account',{exact:true}).first().waitFor();
    }
    else await page.getByRole('heading',{name:workspace==='Portal'?'Student / Guardian conversation is not available yet':workspace==='Platform'?'Platform Owner conversation is not available yet':'Teacher conversation is not available yet'}).waitFor();
    // Let initial page/shell reads settle before measuring resume-only traffic.
    await page.waitForLoadState('networkidle');
    // Role status/Reports populated by an effect ensures client hydration happened.
    const readsBefore=requests.length;
    await page.evaluate(({workspace,scenario})=>{
     if(scenario==='rotation') { localStorage.setItem('academydesk.accessToken.'+workspace,'rotated-synthetic');localStorage.setItem('academydesk.refreshToken.'+workspace,'rotated-refresh'); }
     else {localStorage.removeItem('academydesk.accessToken.'+workspace);localStorage.removeItem('academydesk.refreshToken.'+workspace);}
     // Same-document removal does not emit StorageEvent: intentionally missed delivery.
     if(scenario==='visibilitychange')document.dispatchEvent(new Event('visibilitychange'));
     else if(scenario==='local')window.dispatchEvent(new CustomEvent('academydesk:portal-sign-out',{detail:workspace}));
     else window.dispatchEvent(new Event(scenario==='rotation'?'focus':scenario));
    },{workspace,scenario});
    if(scenario==='rotation')assert.equal(await page.getByRole('heading',{name:'Session ended',exact:true}).count(),0);
    else{
     await page.getByRole('heading',{name:'Session ended',exact:true}).waitFor();
     await page.getByRole('alert').filter({hasText:'Sign in again'}).waitFor();
     assert.ok(await page.getByRole('link',{name:'Sign in again',exact:true}).isVisible());
     assert.equal(await page.locator('.workspace-reports-export-card').count(),0);
     assert.equal(await page.getByRole('heading',{name:/conversation is not available yet/}).count(),0);
     assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
     if(width===320&&scenario==='pageshow')await page.screenshot({path:path.join(evidence,`${workspace}-${theme}.png`),fullPage:true});
    }
    const pairs=await page.evaluate(()=>Object.fromEntries(['AcademyAdmin','Teacher','Portal','Platform'].map(w=>[w,[!!localStorage.getItem('academydesk.accessToken.'+w),!!localStorage.getItem('academydesk.refreshToken.'+w)]])));
    for(const [other,pair]of Object.entries(pairs))assert.deepEqual(pair,other===workspace&&scenario!=='rotation'?[false,false]:[true,true]);
    const resumedReads=requests.slice(readsBefore);
    if(scenario==='rotation'&&workspace==='AcademyAdmin')assert.ok(resumedReads.every(read=>read.path==='/api/portal/announcements'&&read.authorized),'Only the retained signed-in shell announcement read is allowed: '+JSON.stringify(resumedReads));
    else assert.deepEqual(resumedReads,[],'Signed-out resume must not start API retries or reads');
    assert.deepEqual(errors,[]);
    checks.push({workspace,width,theme,scenario,page_errors:0,credential_values_recorded:false});
   }catch(error){failures.push({workspace,width,theme,scenario,error:error.message});throw error;}finally{await context.close();}
  }
 }finally{
  fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({at:new Date().toISOString(),source_sha256:crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex'),scope:'actual exported React pages, synthetic APIs/credentials and controlled lifecycle events; not real suspension/BFCache/Identity/SQL/device',checks,failures,production_approved:false},null,2));
  console.log(JSON.stringify({evidence,passed:checks.length,failed:failures.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));
 }
})().catch(error=>{console.error(error);process.exitCode=1;});
