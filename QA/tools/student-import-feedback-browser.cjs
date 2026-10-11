// Production-export browser proof with synthetic transport, not linked SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','student-import-feedback-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of[320,1440])for(const theme of['light','dark'])for(const scenario of['normal','duplicate','rejected','conflict','denied','uncertain','malformed','pending','replace']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];let release;
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-student-import-token');},theme);
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const body=req.postDataJSON();calls.push({path:url.pathname,method,body});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='POST'){
      assert.match(url.pathname,/^\/api\/academies\/owned\/imports\/students(?:\/validate)?$/);assert.equal(body.rows.length,2);
      const validation=url.pathname.endsWith('/validate');
      if(scenario==='pending'&&!validation)await new Promise(r=>{release=r;});
      const rowErrors=[{row:1,message:'Email address is repeated in this file (rows 1, 2).'},{row:2,message:'Email address is repeated in this file (rows 1, 2).'}];
      const status=validation?(scenario==='denied'?403:200):scenario==='rejected'?400:scenario==='conflict'?409:scenario==='uncertain'?500:200;
      const result=validation?{rowCount:2,errors:scenario==='duplicate'?rowErrors:[]}:scenario==='rejected'?{errors:rowErrors}:scenario==='conflict'?{message:'Some email addresses already exist.'}:scenario==='malformed'?{imported:1}:{imported:2};
      return route.fulfill({status,headers,contentType:'application/json',body:JSON.stringify(result)});
     }
     assert.equal(method,'GET');const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","AccessGovernance","AcademicGovernance","Certificates","Communication","Operations"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[]};
     assert.ok(Object.hasOwn(data,url.pathname),url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/data-operations');const file=page.locator('.data-file-field input[type=file]'),validate=page.getByRole('button',{name:'Validate CSV',exact:true}),importButton=page.getByRole('button',{name:'Import validated rows',exact:true}),notice=page.getByRole('status');
   try { await page.waitForFunction(()=>!document.querySelector('.data-export-grid button').disabled, null, {timeout:5000}); }
   catch(error){fs.writeFileSync(path.join(evidence,'setup-failure.json'),JSON.stringify({calls,errors,text:await page.locator('main').allTextContents()},null,2));throw error;}
   const csv='FirstName,LastName,StudentNumber,Email,Phone\nFirst,Student,,first@example.invalid,\nSecond,Student,,'+(scenario==='duplicate'?'FIRST@example.invalid':'second@example.invalid')+',\n';
   await file.setInputFiles({name:'synthetic.csv',mimeType:'text/csv',buffer:Buffer.from(csv)});await notice.filter({hasText:'CSV loaded.'}).waitFor();assert.equal(await importButton.isDisabled(),true);await validate.click();
   const blocked=['duplicate','denied'].includes(scenario);
   await notice.filter({hasText:scenario==='duplicate'?'row 1':scenario==='denied'?'could not be confirmed':'validation passed'}).waitFor();
   if(blocked){assert.equal(await importButton.isDisabled(),true);assert.equal(calls.filter(c=>c.method==='POST').length,1);if(scenario==='duplicate')assert.match(await notice.textContent(),/row 2/);}
   else if(scenario==='replace'){
    await file.setInputFiles({name:'replacement.csv',mimeType:'text/csv',buffer:Buffer.from(csv)});await notice.filter({hasText:'CSV loaded.'}).waitFor();assert.equal(await importButton.isDisabled(),true);assert.equal(calls.filter(c=>c.method==='POST').length,1);
   }else{
    await importButton.click();
    if(scenario==='pending'){
     await page.getByRole('button',{name:'Importing…',exact:true}).waitFor();await page.waitForFunction(()=>document.querySelector('.data-file-field input[type=file]').disabled);assert.equal(await validate.isDisabled(),true);
     await page.evaluate(()=>{document.querySelector('.data-import-actions button').click();document.querySelectorAll('.data-import-actions button')[1].click();});
     assert.equal(calls.filter(c=>c.method==='POST').length,2);while(!release)await new Promise(r=>setTimeout(r,20));release();
    }
    const success=['normal','pending'].includes(scenario);await notice.filter({hasText:success?'2 students imported.':scenario==='rejected'?'row 1':scenario==='conflict'?'already exist':'Review the student register'}).waitFor();
    assert.equal(calls.filter(c=>c.method==='POST').length,2);assert.equal(await page.locator('.data-file-field b').textContent(),success?'Choose a CSV file':'synthetic.csv');
    if(success)assert.equal(await importButton.isDisabled(),true);else assert.match(await notice.textContent(),/file has been retained/);
   }
   const posts=calls.filter(c=>c.method==='POST');assert.equal(posts[0].body.rows[0].phone,null);assert.equal(posts[0].body.rows[0].studentNumber,null);assert.equal(posts[0].body.rows[0].admissionDate,null);if(posts.length===2)assert.deepEqual(posts[0].body,posts[1].body);
   assert.equal(await notice.getAttribute('aria-live'),'polite');assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   const code={denied:'403',rejected:'400',conflict:'409',uncertain:'500'}[scenario];assert.deepEqual(errors.filter(x=>!code||!x.startsWith('Failed to load resource:')||!x.includes(code)),[]);
   await notice.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,`${width}-${theme}-${scenario}.png`),fullPage:true});checks.push({width,theme,scenario,calls,notice:await notice.textContent()});console.log(`PASS IMPORT UI ${width} ${theme} ${scenario}`);await context.close();
  }
  assert.equal(checks.length,36);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API; no linked SQL/Identity',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
