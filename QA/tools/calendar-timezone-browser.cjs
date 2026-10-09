// Real production-export DOM with synthetic read-only HTTP responses only.
// No Mini, external meetings, SQL, credentials or physical-device claims.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE',`calendar-timezone-browser-${Date.now()}`);
fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{
 try{
  assert.ok(['GET','HEAD'].includes(req.method));
  let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
  if(!path.extname(file))file+='.html';
  const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
  if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
  file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));
  res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});
  if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
 }catch{res.writeHead(404).end();}
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const origin=`http://127.0.0.1:${server.address().port}`;
 const checks=[],errors=[];let browser,currentPage;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const timezoneId of ['UTC','Asia/Kolkata','America/Los_Angeles','Australia/Sydney'])for(const width of [320,1440])for(const boundary of [
   {now:'2026-09-30T19:00:00Z',heading:'October 2026',previous:'September 2026',next:'November 2026',date:'Thu, 01 Oct, 2026'},
   {now:'2026-12-31T19:00:00Z',heading:'January 2027',previous:'December 2026',next:'February 2027',date:'Fri, 01 Jan, 2027'},
  ]){
   const context=await browser.newContext({timezoneId,viewport:{width,height:900}});
   const page=currentPage=await context.newPage(),calls=[];
   page.on('pageerror',e=>errors.push({timezoneId,width,error:e.message}));
   page.on('console',m=>{if(m.type()==='error')errors.push({timezoneId,width,error:m.text()});});
   await page.clock.setFixedTime(new Date(boundary.now));
   await context.addInitScript(()=>{
    localStorage.setItem('academydesk.theme','light');
    localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-calendar-token');
   });
   await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url());
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     assert.equal(request.method(),'GET','Only read-only requests');calls.push(url.pathname);
     const data={
      '/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],
      '/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},
      '/api/portal/announcements':[],
      '/api/academies/owned/batches':[{id:'b',name:'Boundary class',courseId:'c'}],
      '/api/academies/owned/sessions':[{id:'s',batchId:'b',teacherId:null,startUtc:boundary.now,deliveryMode:'InPerson',roomName:'Studio A'}],
      '/api/academies/owned/events':[{id:'e',title:'Boundary recital',type:'Recital',startUtc:boundary.now}],
      '/api/academies/owned/makeup-classes':[{id:'m',studentId:'student',batchId:'b',startUtc:boundary.now,deliveryMode:'Offline',venue:'Studio B'}],
      '/api/academies/owned/students':[{id:'student',firstName:'Synthetic',lastName:'Student'}],
      '/api/academies/owned/teachers':[],
      '/api/academies/owned/courses':[{id:'c',name:'Piano'}],
      '/api/academies/owned/enrollments':[],
     };
     assert.ok(Object.hasOwn(data,url.pathname),'Unexpected endpoint '+url.pathname);
     return route.fulfill({status:200,contentType:'application/json',headers:{'Access-Control-Allow-Origin':origin},body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();
    return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/calendar');
   await page.getByRole('heading',{name:boundary.heading,exact:true}).waitFor();
   await page.locator('.workspace-calendar-agenda-item').nth(2).waitFor();
   assert.equal(await page.locator('.calendar-day').count(),42);
   const cell=page.locator('.calendar-day').filter({hasText:'Boundary recital'});
   assert.equal(await cell.count(),1);assert.equal(await cell.locator('.calendar-date').textContent(),'1');
   assert.ok((await cell.getAttribute('class')).includes('today'));assert.ok(!(await cell.getAttribute('class')).includes('outside'));
   assert.equal(await cell.locator('.calendar-event').count(),3);
   assert.ok((await page.locator('.workspace-calendar-agenda-item').allTextContents()).every(x=>x.includes('12:30 am')));
   for(const title of ['Boundary recital','Synthetic Student · Boundary class']){
    const row=page.locator('.workspace-calendar-agenda-item').filter({has:page.getByRole('heading',{name:title,exact:true})});
    assert.ok((await row.textContent()).includes(boundary.date));
   }
   assert.equal(await page.locator('.workspace-calendar-title').getByText(/India time \(IST\)/).count(),1);
   assert.ok(await page.getByText('Month view · IST',{exact:true}).isVisible(),'Timezone remains visible on mobile');
   await page.getByRole('button',{name:'Previous month',exact:true}).click();
   await page.getByRole('heading',{name:boundary.previous,exact:true}).waitFor();
   assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),0);
   assert.ok((await cell.getAttribute('class')).includes('outside'));
   await page.getByRole('button',{name:'Next month',exact:true}).click();
   await page.getByRole('button',{name:'Next month',exact:true}).click();
   await page.getByRole('heading',{name:boundary.next,exact:true}).waitFor();
   await page.getByRole('button',{name:'Today',exact:true}).click();
   await page.getByRole('heading',{name:boundary.heading,exact:true}).waitFor();
   for(const type of ['Class','Make-up','Event']){
    await page.getByRole('button',{name:type,exact:true}).click();
    assert.equal(await page.locator('.calendar-day.today .calendar-event').count(),1);assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),1);
   }
   await page.getByRole('button',{name:'All',exact:true}).click();
   await page.getByRole('button',{name:'Collapse',exact:true}).click();
   assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),0);
   await page.getByRole('button',{name:'Expand',exact:true}).click();
   assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),3);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'No whole-page horizontal overflow');
   if(width===320)assert.ok(await page.locator('.calendar-grid').evaluate(grid=>{grid.scrollLeft=grid.scrollWidth;return grid.scrollLeft>0;}),'Mobile grid scrolls horizontally without changing dates');
   for(const endpoint of ['sessions','events','makeup-classes'])assert.equal(calls.filter(x=>x.endsWith('/'+endpoint)).length,1,'Navigation/filter only changes presentation');
   const label=`${timezoneId.replaceAll('/','-')}-${width}-${boundary.heading.replaceAll(' ','-')}`;
   await page.screenshot({path:path.join(evidence,label+'.png'),fullPage:true});
   checks.push({timezoneId,width,boundary:boundary.now,gridDay:1,agendaCount:3,today:true,initialMonth:true,navigation:true,filterParity:true,collapse:true,synthetic:true});
   console.log('PASS CALENDAR '+label);await context.close();
  }
  assert.deepEqual(errors,[],'No console/hydration errors');
  fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,sql:'NOT RUN',physicalDevices:'NOT RUN'},null,2));
  console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){
  if(currentPage&&!currentPage.isClosed())await currentPage.screenshot({path:path.join(evidence,'failure.png'),fullPage:true}).catch(()=>{});
  fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:e.message},null,2));throw e;
 }finally{if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
