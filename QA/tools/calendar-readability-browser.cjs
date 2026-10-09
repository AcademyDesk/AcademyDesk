// Production export + deliberately long synthetic read-only fixtures. No live SQL/API proof.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','calendar-readability-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
const batchName='Advanced Carnatic and Contemporary Music — International Weekend Ensemble '+'LongUnbrokenBatchName'.repeat(4);
const subject='Comprehensive Piano, Improvisation and Performance Development '+'LongUnbrokenSubject'.repeat(4);
const teacher='An exceptionally long synthetic instructor display name '+'LongUnbrokenTeacher'.repeat(4);
const student='Synthetic student with a lengthy family name '+'LongUnbrokenStudent'.repeat(4);
const meeting='https://meeting.example.invalid/'+('long-synthetic-path-'.repeat(15))+'?lesson=boundary';
const now='2026-09-30T19:00:00Z';
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 let browser,page;const checks=[],errors=[];
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of [320,375,390,768,1440])for(const theme of ['light','dark']){
   const context=await browser.newContext({viewport:{width,height:900},timezoneId:'Australia/Sydney'});page=await context.newPage();const calls=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await page.clock.setFixedTime(new Date(now));await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-calendar-token');},theme);
   await context.route('**/*',async route=>{const request=route.request(),url=new URL(request.url());
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     assert.equal(request.method(),'GET');calls.push(url.pathname);
     const data={
      '/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],
      '/api/academies/owned/batches':[{id:'b',name:batchName,courseId:'c',teacherId:'t'}],
      '/api/academies/owned/sessions':[{id:'s',batchId:'b',teacherId:'t',startUtc:now,deliveryMode:'Online',roomName:meeting,status:'Cancelled'}],
      '/api/academies/owned/events':[{id:'e',title:'Synthetic international recital '+batchName,type:'Recital',startUtc:now,venue:'LongVenue'.repeat(12)}],
      '/api/academies/owned/makeup-classes':[{id:'m',studentId:'student',batchId:'b',startUtc:now,deliveryMode:'Hybrid',meetingLink:meeting}],
      '/api/academies/owned/students':[{id:'student',firstName:student,lastName:'Fixture'}],'/api/academies/owned/teachers':[{id:'t',firstName:teacher,lastName:'Fixture'}],
      '/api/academies/owned/courses':[{id:'c',name:subject}],'/api/academies/owned/enrollments':[{studentId:'student',batchId:'b',status:'Active'}],
     };assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,contentType:'application/json',headers:{'Access-Control-Allow-Origin':origin},body:JSON.stringify(data[url.pathname])});
    }if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/calendar');await page.locator('.workspace-calendar-agenda-item').nth(2).waitFor();await page.waitForFunction(t=>document.documentElement.dataset.theme===t,theme);
   const classRow=page.locator('.workspace-calendar-agenda-item[data-session-status="Cancelled"]');assert.equal(await classRow.count(),1);assert.equal(await classRow.getByRole('link').count(),0);assert.match(await classRow.textContent(),/Thu, 01 Oct, 2026/);assert.match(await classRow.textContent(),/12:30 am/);
   const geometry=await page.locator('.workspace-calendar-agenda').evaluate(agenda=>{
    const ar=agenda.getBoundingClientRect();const box=el=>{const r=el.getBoundingClientRect(),style=getComputedStyle(el);return{tag:el.tagName,class:el.className,text:el.textContent,client:el.clientWidth,scroll:el.scrollWidth,left:r.left,right:r.right,whiteSpace:style.whiteSpace,overflow:style.overflow,textOverflow:style.textOverflow,contained:r.left>=ar.left-1&&r.right<=ar.right+1};};
    return{agenda:box(agenda),rows:[...agenda.querySelectorAll('.workspace-calendar-agenda-item')].map(box),content:[...agenda.querySelectorAll('h3,dd,.workspace-calendar-agenda-summary,.workspace-calendar-student-list li,.workspace-calendar-agenda-action,.workspace-calendar-cancelled-note')].map(box),ancestors:(()=>{const out=[];for(let p=agenda.parentElement;p;p=p.parentElement)out.push(box(p));return out;})()};
   });
   fs.writeFileSync(path.join(evidence,width+'-'+theme+'-geometry.json'),JSON.stringify(geometry,null,2));await classRow.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,width+'-'+theme+'.png'),fullPage:true});
   assert.ok(geometry.rows.every(x=>x.contained&&x.scroll<=x.client+1),'Agenda row exceeds its card bounds');
   assert.ok(geometry.content.every(x=>x.contained&&x.scroll<=x.client+1),'Useful agenda text is clipped or overflowing');
   assert.ok(geometry.content.filter(x=>x.tag==='H3'||x.tag==='DD').every(x=>x.whiteSpace!=='nowrap'&&x.textOverflow!=='ellipsis'),'Useful details must wrap, not truncate');
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   for(const kind of ['Class','Make-up','Event']){await page.getByRole('button',{name:kind,exact:true}).click();assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),1);}
   await page.getByRole('button',{name:'All',exact:true}).click();await page.getByRole('button',{name:'Collapse',exact:true}).click();assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),0);await page.getByRole('button',{name:'Expand',exact:true}).click();assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),3);
   for(const endpoint of ['sessions','events','makeup-classes'])assert.equal(calls.filter(x=>x.endsWith('/'+endpoint)).length,1);
   await page.reload();await classRow.waitFor();assert.equal(await classRow.getByRole('link').count(),0);assert.equal(await page.locator('.workspace-calendar-agenda-item').count(),3);assert.deepEqual(errors,[]);
   checks.push({width,theme,geometry,readOnly:true,cancelledNonactionable:true,filtersAndReload:true});console.log('PASS READABILITY '+width+' '+theme);await context.close();
  }
  assert.equal(checks.length,10);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,transport:'synthetic GET fixture; not live API/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:e.message},null,2));throw e;}finally{if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(e=>{console.error(e.message);process.exitCode=1;server.close();});
