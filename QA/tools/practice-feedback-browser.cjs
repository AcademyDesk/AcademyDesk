// Actual exported React practice page and shared calendar/theme controls.
// Synthetic APIs/credentials; not native SQL/role/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','practice-feedback-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
const base={id:'old',studentId:'active',practiceDate:'2000-01-01',minutesPracticed:30,focusArea:'Original focus',notes:'Original notes',status:'Logged',teacherFeedback:null};
(async()=>{let browser;const checks=[],failures=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of ['create-success','create-denied','create-server','create-network','create-malformed','review-success','review-denied','review-network','load-denied','load-malformed']){
   const context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage(),errors=[],writes=[];let stored=[base],release;
   const gate=new Promise(resolve=>release=resolve);let observedWrite;const writeStarted=new Promise(resolve=>observedWrite=resolve);
   try{
    page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(message.type()==='error'&&/hydrat/i.test(message.text()))errors.push(message.text());});
    await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-practice');},theme);
    await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url());
     if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
      const headers={'Access-Control-Allow-Origin':origin};
      if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:{...headers,'Access-Control-Allow-Headers':'authorization,content-type','Access-Control-Allow-Methods':'GET,POST,PATCH'}});
      if(req.method()!=='GET'){
       const review=scenario.startsWith('review'),payload=JSON.parse(req.postData());assert.equal(req.method(),review?'PATCH':'POST');assert.equal(url.pathname,'/api/academies/owned/practice-logs'+(review?'/old/review':''));writes.push({method:req.method(),payload});observedWrite();await gate;
       if(scenario.endsWith('network'))return route.abort('failed');
       const row=review?{...base,status:'Reviewed',teacherFeedback:payload.teacherFeedback.trim()}:{...base,...payload,id:'created',teacherFeedback:null};
       if(!scenario.endsWith('denied'))stored=review?[row]:[row,base];
       return route.fulfill({status:scenario.endsWith('denied')?403:scenario.endsWith('server')?500:200,headers,contentType:'application/json',body:scenario.endsWith('malformed')?'bad-json':JSON.stringify(row)});
      }
      const rows={'/api/academies':[{id:'owned',name:'Synthetic academy'}],'/api/auth/session':{workspace:'AcademyAdmin',roles:['AcademyAdmin'],isPlatformOwner:false,displayName:'Synthetic admin',academyId:'owned'},'/api/portal/announcements':[],
       '/api/academies/owned/students':[{id:'active',firstName:'Synthetic',lastName:'Active',isActive:true},{id:'inactive',firstName:'Synthetic',lastName:'Inactive',isActive:false}],'/api/academies/owned/practice-logs':stored};
      assert.ok(Object.hasOwn(rows,url.pathname),'Unexpected API: '+url.pathname);
      if(scenario==='load-denied'&&url.pathname.endsWith('/students'))return route.fulfill({status:403,headers,contentType:'application/json',body:'[]'});
      return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(scenario==='load-malformed'&&url.pathname.endsWith('/practice-logs')?{}:rows[url.pathname])});
     }
     return url.origin===origin||url.protocol==='data:'?route.continue():route.abort('blockedbyclient');
    });
    await page.goto(origin+'/practice-logs');await page.getByRole('button',{name:'Student',exact:true}).waitFor();
    if(scenario.startsWith('load-')){
     await page.getByText('Practice records are unavailable until the list loads.',{exact:true}).waitFor();assert.equal(await page.getByText('No practice logs are available in this view.',{exact:true}).count(),0);assert.equal(await page.getByRole('button',{name:'Save log',exact:true}).isDisabled(),true);assert.equal(writes.length,0);assert.deepEqual(errors,[]);checks.push({width,theme,scenario,writes:0,page_errors:0,false_empty_prevented:true,synthetic_API:true});continue;
    }
    await page.waitForFunction(()=>!document.querySelector('form fieldset')?.disabled);
    const themeButtons=page.getByRole('button',{name:theme==='light'?'Use light theme':'Use dark theme',exact:true});assert.ok(await themeButtons.first().getAttribute('aria-pressed')==='true');
    await page.getByRole('button',{name:'Student',exact:true}).click();assert.equal(await page.getByRole('option',{name:'Synthetic Inactive',exact:true}).count(),0);await page.getByRole('option',{name:'Synthetic Active',exact:true}).click();
    const date=page.locator('.standard-date-field');await date.locator('button.standard-date-trigger').click();
    const dialog=page.getByRole('dialog',{name:'Practice date (UTC) calendar',exact:true});await dialog.getByLabel('Select year').selectOption('2000');await dialog.getByLabel('Select month').selectOption('0');
    await dialog.locator('.standard-date-days button[data-current="true"]').filter({hasText:/^1$/}).click();
    assert.equal(await page.locator('input[name="practiceDate"]').inputValue(),'2000-01-01');
    await date.locator('button.standard-date-trigger').click();assert.equal(await dialog.getByLabel('Select year').inputValue(),'2000');await page.keyboard.press('Escape');
    await page.getByLabel('Focus area (optional)',{exact:true}).fill('Draft focus');await page.getByLabel('Notes (optional)',{exact:true}).fill('Draft notes');
    const review=scenario.startsWith('review');
    if(review)await page.getByLabel('Teacher feedback (optional)',{exact:true}).fill('  Draft feedback  ');
    const form=review?page.getByRole('region',{name:'Practice log records'}).locator('form'):page.locator('form').first();
    await form.evaluate(form=>{form.requestSubmit();form.requestSubmit();});
    let writeTimeout;try{await Promise.race([writeStarted,new Promise((_,reject)=>{writeTimeout=setTimeout(()=>reject(Error('Expected practice write was not emitted')),15000);})]);}finally{clearTimeout(writeTimeout);}
    await page.waitForFunction(()=>[...document.querySelectorAll('button')].some(button=>button.textContent==='Reload practice logs'&&button.disabled));
    assert.equal(writes.length,1);release();
    if(scenario.endsWith('success')){
     await page.getByRole('status').filter({hasText:review?'Practice log reviewed.':'Practice log saved.'}).waitFor();
     if(review)await page.getByText('Teacher feedback: Draft feedback',{exact:true}).waitFor();
     else{assert.equal(await page.locator('textarea[name="notes"]').inputValue(),'');assert.equal(await page.locator('input[name="studentId"]').inputValue(),'active');assert.equal(await page.locator('input[name="practiceDate"]').inputValue(),'2000-01-01');}
    }else{
     await page.getByRole('status').filter({hasText:scenario.endsWith('denied')?'permission':/could not be confirmed/}).waitFor();
     assert.equal(await page.locator('textarea[name="notes"]').inputValue(),'Draft notes');
     if(review)assert.equal(await page.getByRole('region',{name:'Practice log records'}).locator('textarea').inputValue(),'  Draft feedback  ');
     if(!scenario.endsWith('denied')){
      assert.equal(await page.getByRole('button',{name:'Save log',exact:true}).isDisabled(),true);
      await page.getByRole('button',{name:'Reload practice logs',exact:true}).click();await page.getByRole('status').filter({hasText:'Practice logs refreshed.'}).waitFor();assert.equal(writes.length,1,'Reload never repeats a write');
     }
    }
    assert.equal(writes.length,1);assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
    if(width===320&&scenario.endsWith('success'))await page.screenshot({path:path.join(evidence,`${theme}-${scenario}.png`),fullPage:true});
    checks.push({width,theme,scenario,writes:writes.length,page_errors:0,direct_year_selection:true,synthetic_API:true});
   }catch(error){failures.push({width,theme,scenario,error:error.message,url:page.url()});await page.screenshot({path:path.join(evidence,'incomplete.png'),fullPage:true}).catch(()=>{});fs.writeFileSync(path.join(evidence,'incomplete-body.txt'),await page.locator('body').innerText().catch(()=>''));throw error;}finally{release();await context.close();}
  }
 }finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({at:new Date().toISOString(),checks,failures,scope:'actual exported React/shared date+theme controls; synthetic APIs, not SQL/device/role acceptance',production_approved:false},null,2));console.log(JSON.stringify({evidence,passed:checks.length,failed:failures.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
