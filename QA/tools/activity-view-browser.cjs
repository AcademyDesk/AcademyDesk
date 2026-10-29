// Real exported Activity page, shared controls and shell; synthetic APIs only.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','activity-view-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
const records=Array.from({length:14},(_,i)=>({id:'row-'+i,action:i===0?'BoundaryStudentUpdated':'SyntheticRecordUpdated'+i,entityType:i===0?'Student':'Teacher',occurredAtUtc:i===0?'2000-01-01T19:00:00':'2000-01-02T19:00:00Z',metadataJson:i===0?'<img src=x onerror=alert(1)> Synthetic piano':null}));
(async()=>{let browser;const checks=[],failures=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const timezoneId of ['Asia/Kolkata','America/Los_Angeles'])for(const scenario of ['loaded','empty','denied','malformed','bad-timestamp']){
   const context=await browser.newContext({viewport:{width,height:900},timezoneId}),page=await context.newPage(),errors=[],writes=[];
   try{
    page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(message.type()==='error'&&/hydrat/i.test(message.text()))errors.push(message.text());});
    await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-activity');},theme);
    await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url());
     if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
      const headers={'Access-Control-Allow-Origin':origin};if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:{...headers,'Access-Control-Allow-Headers':'authorization,content-type','Access-Control-Allow-Methods':'GET'}});
      if(req.method()!=='GET'){writes.push(url.pathname);return route.abort('blockedbyclient');}
      const rows={'/api/academies':[{id:'owned',name:'Synthetic academy'}],'/api/auth/session':{workspace:'AcademyAdmin',roles:['AcademyAdmin'],isPlatformOwner:false,displayName:'Synthetic admin',academyId:'owned'},'/api/portal/announcements':[],'/api/academies/owned/audit-logs':records};
      assert.ok(Object.hasOwn(rows,url.pathname),'Unexpected API: '+url.pathname);
      const audit=url.pathname.endsWith('/audit-logs');
      return route.fulfill({status:audit&&scenario==='denied'?403:200,headers,contentType:'application/json',body:JSON.stringify(audit?(scenario==='empty'?[]:scenario==='malformed'?{}:scenario==='bad-timestamp'?[{...records[0],occurredAtUtc:'not-a-date'}]:records):rows[url.pathname])});
     }
     return url.origin===origin||url.protocol==='data:'?route.continue():route.abort('blockedbyclient');
    });
    await page.goto(origin+'/activity');
    const list=page.locator('.workspace-activity-list li'),empty=page.getByText('No activity recorded for this view.',{exact:true});
    if(['denied','malformed','bad-timestamp'].includes(scenario)){
     await page.getByRole('alert').filter({hasText:'Activity could not be loaded'}).waitFor();assert.equal(await empty.count(),0);assert.equal(await page.getByRole('textbox',{name:'Search activity',exact:true}).isDisabled(),true);assert.equal(await list.count(),0);
    }else if(scenario==='empty'){
     await empty.waitFor();assert.equal(await page.getByRole('textbox',{name:'Search activity',exact:true}).isEnabled(),true);assert.equal(await list.count(),0);
    }else{
     await page.getByText('14 matching events',{exact:true}).waitFor();assert.equal(await list.count(),14);
     // Desktop scrolls the workspace region; mobile may scroll the document.
     // Require actual last-row reachability, not one assumed scroll owner.
     await list.last().scrollIntoViewIfNeeded();
     assert.ok(await list.last().evaluate(el=>{const rect=el.getBoundingClientRect();let parent=el.parentElement,scrolled=false;while(parent){scrolled||=parent.scrollTop>0;parent=parent.parentElement;}return scrolled&&rect.top>=0&&rect.bottom<=innerHeight;}),'Last audit row must be reachable by scrolling');
     await page.getByRole('textbox',{name:'Search activity',exact:true}).fill('PIANO');await page.getByText('1 matching events',{exact:true}).waitFor();assert.equal(await list.count(),1);
     await page.getByText('View details',{exact:true}).click();assert.equal(await list.locator('pre img').count(),0);assert.match(await list.locator('pre').innerText(),/<img src=x onerror=alert\(1\)>/);
     await page.getByRole('button',{name:'Clear filters',exact:true}).click();
     const date=page.locator('.workspace-activity-filters .standard-date-field');await date.locator('button.standard-date-trigger').click();
     const dialog=page.getByRole('dialog',{name:'Activity date (IST) calendar',exact:true});await dialog.getByLabel('Select year').selectOption('2000');await dialog.getByLabel('Select month').selectOption('0');
     await dialog.locator('.standard-date-days button[data-current="true"]').filter({hasText:/^2$/}).click();
     assert.equal(await page.locator('input[name="activity-date"]').inputValue(),'2000-01-02');await page.getByText('1 matching events',{exact:true}).waitFor();assert.equal(await list.count(),1);assert.match(await list.innerText(),/2 Jan 2000/);
     await page.getByRole('button',{name:'Clear filters',exact:true}).click();await page.getByRole('button',{name:'Record type',exact:true}).click();await page.getByRole('option',{name:'Student',exact:true}).click();await page.getByText('1 matching events',{exact:true}).waitFor();
     await page.getByRole('button',{name:'Clear filters',exact:true}).click();await page.getByRole('button',{name:'Time range',exact:true}).click();await page.getByRole('option',{name:'Last 24 hours',exact:true}).click();await empty.waitFor();
     await page.getByRole('button',{name:'Clear filters',exact:true}).click();await page.getByText('14 matching events',{exact:true}).waitFor();
     if(width===320&&timezoneId==='America/Los_Angeles')await page.screenshot({path:path.join(evidence,theme+'-mobile.png'),fullPage:true});
    }
    assert.deepEqual(errors,[]);assert.deepEqual(writes,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));checks.push({width,theme,timezoneId,scenario,page_errors:0,writes:0,synthetic_API:true});
   }catch(error){failures.push({width,theme,timezoneId,scenario,error:error.message});await page.screenshot({path:path.join(evidence,'incomplete.png'),fullPage:true}).catch(()=>{});throw error;}finally{await context.close();}
  }
 }finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({at:new Date().toISOString(),checks,failures,scope:'exported Activity React/shared controls; synthetic APIs,not SQL/role/physical-device acceptance',production_approved:false},null,2));console.log(JSON.stringify({evidence,passed:checks.length,failed:failures.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
