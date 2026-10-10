// Exported Submission Review UI/synthetic transport; not live Identity/SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','review-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
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
  const seed=[{id:'one',studentId:'student-one',assignmentId:'work-one',studentName:'Synthetic learner with a long display name',studentNumber:'QA-1',assignmentTitle:'Synthetic practice assignment with a long title',batchName:'First batch',submittedAtUtc:'2026-10-01T09:00:00',status:'Submitted',responseText:'Identical'},
   {id:'two',studentId:'student-two',assignmentId:'work-two',studentName:'Synthetic learner with a long display name',studentNumber:'QA-2',assignmentTitle:'Synthetic practice assignment with a long title',batchName:'Second batch',submittedAtUtc:'2026-10-01T10:00:00Z',status:'Submitted',responseText:'Identical'}];
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of ['normal','cancel','rejected','uncertain-committed','malformed','retry-success']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],pageErrors=[],prompts=[];
   let rows=seed.map(x=>({...x})),release,patchCount=0;
   const gate=new Promise(r=>release=r);
   page.on('pageerror',e=>pageErrors.push(e.message));page.on('console',m=>{if(m.type()==='error')pageErrors.push(m.text());});
   page.on('dialog',async dialog=>{prompts.push(dialog.message());if(scenario==='cancel')await dialog.dismiss();else await dialog.accept('Target feedback');});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-review-token');},theme);
   await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url()),method=request.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:request.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='PATCH'){
      patchCount++;assert.equal(url.pathname,'/api/academies/owned/assignment-submissions/two/review');assert.deepEqual(request.postDataJSON(),{feedback:'Target feedback'});
      if(scenario==='retry-success'&&patchCount===2)await gate;
      const denied=scenario==='rejected'||(scenario==='retry-success'&&patchCount===1);
      if(!denied)rows[1]={...rows[1],status:'Reviewed',teacherFeedback:'Target feedback'};
      return route.fulfill({status:denied?400:scenario==='uncertain-committed'?500:200,headers,contentType:'application/json',body:JSON.stringify(denied||scenario==='uncertain-committed'?{message:'Synthetic rejection'}:scenario==='malformed'?{...rows[1],studentId:'wrong-student'}:rows[1])});
     }
     assert.equal(method,'GET');
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","AcademicGovernance"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],'/api/academies/owned/assignment-submissions':rows};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/submission-review');
   const actions=page.getByRole('button',{name:/^Review .*submission/}),target=page.getByRole('button',{name:/submission two\).*student student-two/});
   await target.waitFor();assert.equal(await actions.count(),2);assert.equal(await page.locator('time').count(),2);
   await target.focus();await page.keyboard.press('Enter');
   const notice=page.getByRole('status');
   if(scenario==='retry-success'){
    await notice.filter({hasText:'could not be saved'}).waitFor();
    await target.click();await page.waitForFunction(()=>document.querySelector('button[aria-label*="submission two)"]')?.disabled===true);
    assert.equal(await notice.count(),0);
    assert.equal(await actions.nth(0).isDisabled(),true);release();
   }
   const succeeded=scenario==='normal'||scenario==='retry-success';
   if(scenario==='cancel'){
    assert.equal(await actions.count(),2);assert.equal(patchCount,0);assert.equal(await notice.count(),0);
   }else{
    await notice.filter({hasText:succeeded?'Review saved':'could not be saved'}).waitFor();
    await page.waitForFunction(()=>document.querySelector('button[aria-label*="submission one)"]')?.disabled===false);
    assert.equal(await notice.getAttribute('aria-live'),'polite');assert.equal(await actions.count(),succeeded?1:2);
    if(!succeeded)assert.match(await notice.textContent(),/Check the saved record before retrying/);
    assert.equal(await actions.nth(0).isDisabled(),false);
   }
   assert.equal(patchCount,scenario==='cancel'?0:scenario==='retry-success'?2:1);
   for(const prompt of prompts){assert.ok(prompt.includes('student-two'));assert.ok(prompt.includes('work-two'));assert.ok(prompt.includes('Second batch'));}
   assert.equal(rows[0].status,'Submitted');
   assert.equal(rows[1].status,['cancel','rejected'].includes(scenario)?'Submitted':'Reviewed');
   assert.equal(calls.filter(x=>x.path.endsWith('/assignment-submissions')&&x.method==='GET').length,1);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   await page.screenshot({path:path.join(evidence,`${width}-${theme}-${scenario}.png`),fullPage:true});
   const code=scenario==='uncertain-committed'?'500':['rejected','retry-success'].includes(scenario)?'400':null;
   const unexpected=pageErrors.filter(x=>!code||!x.startsWith('Failed to load resource:')||!x.includes(code));assert.deepEqual(unexpected,[]);errors.push(...unexpected);
   checks.push({width,theme,scenario,calls,prompts,notice:await notice.count()?await notice.textContent():null,expectedResourceErrors:pageErrors.length});
   console.log(`PASS REVIEW ${width} ${theme} ${scenario}`);await context.close();
  }
  assert.equal(checks.length,24);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,transport:'synthetic API, not live Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
