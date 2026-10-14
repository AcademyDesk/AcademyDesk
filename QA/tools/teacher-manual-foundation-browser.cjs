// Production export and synthetic reads only; no writes/private provider/devices.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {verifyVendor,assertManual}=require('./penta-foundation-contract.cjs');verifyVendor();
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE','teacher-manual-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser,currentPage;
 try{browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 for(const[width,height]of[[320,700],[390,700],[768,700],[1024,700],[1440,700],[768,360]])for(const theme of['light','dark'])for(const mode of['full','empty','denied']){
  const context=await browser.newContext({viewport:{width,height},reducedMotion:'reduce'}),page=currentPage=await context.newPage(),errors=[],writes=[];
  page.on('pageerror',e=>errors.push(e.message));await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.Teacher','synthetic-teacher-token');},theme);
  const full=mode==='full',teacherName='Synthetic teacher with an intentionally long family name for wrapping';
  const batches=full?[{id:'owned',name:'Synthetic teaching batch with an intentionally long title for readable wrapping',capacity:2,deliveryMode:'Online'}]:[];
  const sessions=full?[{id:'session',batchId:'owned',startUtc:new Date().toISOString(),status:'Scheduled',deliveryMode:'Online'}]:[];
  const payslips=Array.from({length:full?20:0},(_,i)=>({id:'payout-'+i,payrollProfileId:'synthetic-profile',workerName:teacherName,workerType:'Teacher',payslipNumber:'SYNTHETIC-'+i,periodLabel:'Synthetic payment period with a long descriptive title '+i,grossAmount:1000,deductions:0,netAmount:1000,currency:'INR',grossAmountExact:'1000.00',deductionsExact:'0.00',netAmountExact:'1000.00',status:'Paid',paidAtUtc:'2026-10-13T10:00:00Z',createdAtUtc:'2026-10-13T09:00:00Z',identitySource:'PayoutSnapshot',settlementKind:'CashPayment',sessionsCovered:4}));
  const resources=Array.from({length:full?25:0},(_,i)=>({id:'resource-'+i,title:'Synthetic long class history title '+('LongTitle'.repeat(8))+' '+i,type:'Note',description:'Synthetic class notes '.repeat(8),url:'note://synthetic-'+i,createdAtUtc:'2026-10-13T09:00:00Z'}));
  const data={
   '/api/auth/session':{displayName:teacherName,roles:['Teacher']},'/api/teacher/me':{userId:'synthetic-teacher',firstName:'Synthetic',lastName:teacherName,batches,sessions},
   '/api/portal/announcements':[], '/api/portal/notifications':[], '/api/teacher/payments':{paymentModel:'Monthly',payslips}, '/api/teacher/calendar':{sessions,holidays:[]},'/api/teacher/progress':{},'/api/teacher/batch-progress':[], '/api/teacher/practice-logs':[], '/api/teacher/leave-requests':[], '/api/teacher/profile':{firstName:'Synthetic',lastName:teacherName},'/api/teacher/classroom-activity':{resources,homework:[]}
  };
  await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url());if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
   if(!['GET','OPTIONS'].includes(req.method()))writes.push(req.method()+' '+url.pathname);
   return route.fulfill({status:mode==='denied'&&url.pathname==='/api/teacher/me'?403:200,headers:{'Access-Control-Allow-Origin':origin},contentType:'application/json',body:JSON.stringify(data[url.pathname]||[])});
  }if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');});
  await page.goto(origin+'/teacher');
  await assertManual(page,theme);
  const nav=page.locator(width<=900?'.learner-bottom-nav':'.teacher-portal-nav');
  const bounds=async()=>assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),true,'no page horizontal overflow');
  if(mode==='denied'){await page.getByRole('status').filter({hasText:'active teacher profile'}).waitFor();assert.equal(await page.locator('.teacher-portal-content form').count(),0,'no protected form after denial');assert.equal(await page.locator('.teacher-activity-list').count(),0);await bounds();}
  else{
   await page.getByRole('heading',{name:'Welcome, Synthetic',exact:true}).waitFor();await bounds();
   assert.equal(await page.locator('.teacher-portal-context strong').evaluate(e=>e.scrollWidth<=e.clientWidth+1&&getComputedStyle(e).whiteSpace!=='nowrap'),true,'Teacher identity wraps');
   assert.equal(await page.locator('.enterprise-utilities button').evaluateAll(es=>es.every(e=>{const r=e.getBoundingClientRect();return r.height>=44&&r.width>=44;})),true,'44px header controls');
   const select=async label=>{if(width<=900){await nav.locator('summary').click();assert.equal(await nav.locator('button').count(),7,'all sections available');}await nav.getByRole('button',{name:label,exact:true}).click();if(width<=900){assert.equal(await nav.locator('details').getAttribute('open'),null,'selection collapses');assert.equal(await nav.locator('summary').locator('span').nth(1).textContent(),label);}await bounds();};
   if(width<=900){await nav.locator('summary').click();await page.keyboard.press('Escape');assert.equal(await nav.locator('details').getAttribute('open'),null);assert.equal(await nav.locator('summary').evaluate(e=>e===document.activeElement),true);await nav.locator('summary').click();await page.locator('.teacher-portal-context').click();assert.equal(await nav.locator('details').getAttribute('open'),null);assert.equal(await nav.evaluate(e=>getComputedStyle(e).position),'static','no fixed overlay bar');}
   for(const label of['My classes','Classroom','Homework','My progress','Pay slip','Profile & leave','Today']){
    await select(label);
    if(label==='Classroom'&&full){await page.getByRole('button',{name:'Class',exact:true}).click();await page.getByRole('option',{name:batches[0].name,exact:true}).click();await page.locator('.teacher-activity-list article').last().waitFor();const last=page.locator('.teacher-activity-list article').last();await last.scrollIntoViewIfNeeded();assert.equal(await last.evaluate(e=>{const r=e.getBoundingClientRect();return e.contains(document.elementFromPoint(r.left+8,r.top+8));}),true,'last history row not behind navigation');assert.equal(await last.evaluate(e=>e.scrollWidth<=e.clientWidth+1),true,'long history text wraps');}
    if(label==='Pay slip'&&full){await page.locator('.teacher-payslip-list article').last().waitFor();assert.equal(await page.locator('.teacher-payslip-list article').count(),20,'all payouts retained');const last=page.locator('.teacher-payslip-list article').last();await last.scrollIntoViewIfNeeded();assert.equal(await last.evaluate(e=>e.scrollWidth<=e.clientWidth+1),true,'payout labels/actions fit');}
    if(label==='Profile & leave'){const field=page.locator('input[name="firstName"]');await field.waitFor();assert.equal(await field.evaluate(e=>e.getBoundingClientRect().height>=44),true);if(width<=900)assert.equal(await field.evaluate(e=>getComputedStyle(e).fontSize),'16px');}
   }
   if(width===320&&full){await page.evaluate(()=>document.scrollingElement.scrollTop=0);await page.screenshot({path:path.join(evidence,theme+'-today.png')});await select('Pay slip');await page.locator('.teacher-payslip-list article').last().scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,theme+'-payslips-end.png')});}
  }
  assert.deepEqual(errors,[]);assert.deepEqual(writes,[]);checks.push({width,height,theme,mode});console.log('PASS '+[width,height,theme,mode].join(' '));await context.close();
 }
 assert.equal(checks.length,36);const sourceHashes=Object.fromEntries(['apps/web/src/app/teacher/page.tsx','apps/web/src/components/manual-workspace.module.css','apps/web/src/components/manual-section-navigation.tsx','apps/web/src/components/platform-mobile-navigation.module.css'].map(file=>[file,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex')]));fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,sourceHashes,transport:'Synthetic production-export reads; no live SQL/RBAC/media/device acceptance'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(error){if(currentPage&&!currentPage.isClosed())await currentPage.screenshot({path:path.join(evidence,'failed.png')});fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:error.message},null,2));throw error;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(error=>{console.error(error);process.exitCode=1;server.close();});
