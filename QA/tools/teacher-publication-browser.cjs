// Real production-export forms; controlled synthetic HTTP, not linked SQL/device proof.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','teacher-publication-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser;
 try{browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 for(const width of[320,1440])for(const theme of['light','dark'])for(const item of[
  {kind:'assignments',title:'Assign homework'}, {kind:'lesson-plans',title:'Create lesson plan'},
  {kind:'assessments',title:'Create assessment'}, {kind:'assessments',title:'Create assessment',date:'2026-10-11T13:30'}
 ]){
  const context=await browser.newContext({viewport:{width,height:900},timezoneId:'Asia/Kolkata'}),page=await context.newPage(),calls=[],errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.Teacher','synthetic-publication-token');},theme);
  await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url()),method=req.method();
   if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
    const headers={'Access-Control-Allow-Origin':origin};calls.push({path:url.pathname,method,body:req.postDataJSON()});
    if(method==='POST'){
     assert.equal(url.pathname,'/api/teacher/'+item.kind);const body=req.postDataJSON();
     const expected={batchId:'owned',title:'Synthetic teaching title'};
     if(item.kind==='assignments')Object.assign(expected,{description:'',type:'Homework',isPublished:true});
     else if(item.kind==='assessments')Object.assign(expected,{type:'Performance',maxScore:12,scheduledAtUtc:item.date?'2026-10-11T08:00:00.000Z':null,isPublished:false});
     else expected.objectives='';
     assert.deepEqual(body,expected);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify({id:'created',...body})});
    }
    assert.equal(method,'GET');let data;
    if(url.pathname==='/api/teacher/me')data={userId:'synthetic',firstName:'Synthetic',lastName:'Teacher',batches:[{id:'owned',name:'Synthetic batch',capacity:1,deliveryMode:'Online'}],sessions:[]};
    else if(url.pathname==='/api/auth/session')data={displayName:'Synthetic teacher',roles:['Teacher'],academyId:'synthetic'};
    else if(url.pathname==='/api/teacher/calendar')data={sessions:[],holidays:[]};
    else if(url.pathname==='/api/teacher/payments')data={payslips:[]};
    else if(['/api/portal/announcements','/api/portal/notifications','/api/teacher/practice-logs'].includes(url.pathname))data=[];
    else throw Error('Unexpected API '+url.pathname);
    return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data)});
   }
   if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
  });
  await page.goto(origin+'/teacher');await page.locator((width===320?'.learner-bottom-nav':'.teacher-portal-nav')+' button').filter({hasText:'Homework'}).click();
  const panel=page.locator('.teacher-action-panel').filter({has:page.getByRole('heading',{name:item.title,exact:true})}),form=panel.locator('form');
  await form.locator('input[name="title"]').fill('Synthetic teaching title');
  await form.getByRole('button',{name:'Assigned batch',exact:true}).click();await form.getByRole('option',{name:'Synthetic batch',exact:true}).click();
  if(item.kind==='assessments'){await form.locator('input[name="maxScore"]').fill('12');if(item.date)await form.locator('input[name="scheduledAtUtc"]').fill(item.date);}
  await form.locator('button:not([type="button"])').click();
  await page.getByText('Saved for your assigned batch.',{exact:true}).waitFor();
  assert.equal(calls.filter(x=>x.method==='POST').length,1);assert.equal(await form.locator('input[name="title"]').inputValue(),'');
  assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
  await page.screenshot({path:path.join(evidence,`${width}-${theme}-${item.kind}-${item.date?'dated':'blank'}.png`),fullPage:true});
  checks.push({width,theme,kind:item.kind,date:item.date??null,calls});console.log(`PASS PUBLICATION UI ${width} ${theme} ${item.kind} ${item.date?'dated':'blank'}`);await context.close();
 }
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({passed:checks.length,checks},null,2));console.log('PUBLICATION BROWSER PASS '+checks.length+' evidence='+evidence);
 }finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(error=>{fs.writeFileSync(path.join(evidence,'failure.txt'),error.stack);console.error(error);process.exitCode=1;});
