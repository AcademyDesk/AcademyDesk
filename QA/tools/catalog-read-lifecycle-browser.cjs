// Real exported Courses/Batches and shell. Synthetic authorized academy list;
// no SQL, real tenant/role acceptance, writes or external service access.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','catalog-read-lifecycle-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
const academies=[{id:'a',name:'Synthetic academy A'},{id:'b',name:'Synthetic academy B'}].map(row=>({...row,enabledModulesJson:JSON.stringify(['Core','AcademicGovernance']),subscriptionStatus:'Active',subscriptionPlan:'QA synthetic'}));
function data(url){const id=url.includes('/b/')?'b':'a';return url.endsWith('/courses')?[{id:'course-'+id,name:'Course '+id,academyType:'Music',isActive:true}]:url.endsWith('/teachers')?[{id:'teacher-'+id,firstName:'Synthetic',lastName:id}]:url.endsWith('/branches')?[{id:'branch-'+id,name:'Branch '+id}]:[{id:'batch-'+id,name:'Batch '+id,courseId:'course-'+id,teacherId:'teacher-'+id,branchId:'branch-'+id,capacity:10,isActive:true}];}
(async()=>{let browser;const checks=[],failures=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const pageName of ['courses','batch-setup'])for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of ['switch','clear','late-failure']){
   const context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage(),errors=[],writes=[],reads=[],oldSettled=[];let release,oldStarted,oldCount=0;const gate=new Promise(resolve=>release=resolve),started=new Promise(resolve=>oldStarted=resolve);
   try{
    page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(message.type()==='error'&&/hydrat/i.test(message.text()))errors.push(message.text());});
    await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-catalog');},theme);
    await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url());
     if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
      const headers={'Access-Control-Allow-Origin':origin};if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:{...headers,'Access-Control-Allow-Headers':'authorization,content-type','Access-Control-Allow-Methods':'GET'}});
      if(req.method()!=='GET'){writes.push(url.pathname);return route.abort('blockedbyclient');}
      reads.push(url.pathname);let body;
      if(url.pathname==='/api/academies')body=academies;
      else if(url.pathname==='/api/auth/session')body={workspace:'AcademyAdmin',roles:['AcademyAdmin'],isPlatformOwner:false,displayName:'Synthetic admin',academyId:'a'};
      else if(url.pathname==='/api/portal/announcements')body=[];
      else{assert.match(url.pathname,/^\/api\/academies\/[ab]\/(courses|teachers|branches|batches)$/);if(url.pathname.includes('/a/')){
       let done;oldSettled.push(new Promise(resolve=>done=resolve));if(++oldCount===(pageName==='courses'?1:4))oldStarted();await gate;
       await route.fulfill({status:scenario==='late-failure'?500:200,headers,contentType:'application/json',body:JSON.stringify(scenario==='late-failure'?{}:data(url.pathname))}).catch(()=>{});done();return;
      }body=data(url.pathname);}
      await route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(body)}).catch(()=>{});return;
     }
     return url.origin===origin||url.protocol==='data:'?route.continue():route.abort('blockedbyclient');
    });
    await page.goto(origin+'/'+pageName);const selector=page.locator(pageName==='courses'?'.courses-academy .standard-select-trigger':'.batch-setup-academy .standard-select-trigger');await selector.waitFor();
    await page.waitForFunction(()=>document.querySelector('input[name="academy"]')?.value==='a');
    let deadline;try{await Promise.race([started,new Promise((_,reject)=>deadline=setTimeout(()=>reject(Error('Expected old-academy reads not issued')),15000))]);}finally{clearTimeout(deadline);}
    const draft=page.getByLabel('Course or subject name',{exact:true});
    if(pageName==='courses')await draft.fill('Preserved draft');
    await selector.click();await page.getByRole('option',{name:scenario==='clear'?'Select academy':'Synthetic academy B',exact:true}).click();
    if(scenario!=='clear')await page.getByText(pageName==='courses'?'Course b':'Batch b',{exact:true}).waitFor();
    release();await Promise.all(oldSettled);
    assert.equal(await page.getByText(pageName==='courses'?'Course a':'Batch a',{exact:true}).count(),0);
    if(scenario==='clear')assert.equal(await page.locator('input[name="academy"]').inputValue(),'');
    else{assert.equal(await page.locator('input[name="academy"]').inputValue(),'b');assert.equal(await page.getByText(pageName==='courses'?'Course b':'Batch b',{exact:true}).count(),1);}
    if(pageName==='courses')assert.equal(await draft.inputValue(),'Preserved draft');
    assert.equal(await page.getByText(pageName==='courses'?'Courses could not be loaded.':'Batch information could not be loaded.',{exact:true}).count(),0,'Obsolete read failure cannot overwrite current state');
    assert.deepEqual(writes,[]);assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
    if(width===320&&scenario==='switch')await page.screenshot({path:path.join(evidence,pageName+'-'+theme+'.png'),fullPage:true});
    checks.push({page:pageName,width,theme,scenario,writes:0,page_errors:0,synthetic_API:true,old_reads:reads.filter(url=>url.includes('/a/')).length});
   }catch(error){failures.push({page:pageName,width,theme,scenario,error:error.message});await page.screenshot({path:path.join(evidence,'incomplete.png'),fullPage:true}).catch(()=>{});throw error;}finally{release();await context.close();}
  }
 }finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({at:new Date().toISOString(),checks,failures,scope:'exported React synthetic transport;not SQL/tenant/role/device acceptance',production_approved:false},null,2));console.log(JSON.stringify({evidence,passed:checks.length,failed:failures.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
