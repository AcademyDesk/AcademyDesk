// Actual six exported pages/shared controls; synthetic auth and API transport.
// No server permission acceptance, real SQL, writes, inference or physical device.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE','portal-read-state-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
const academies=['a','b'].map(id=>({id,name:'Synthetic Academy '+id.toUpperCase(),countryCode:'IN',timeZone:'Asia/Kolkata',isActive:true,branches:0,students:1,subscriptionPlan:id==='a'?'Growth':'Enterprise',subscriptionStatus:'Active',studentLimit:100,staffLimit:20,enabledModulesJson:JSON.stringify(['Core','AcademicGovernance','AccessGovernance'])}));
const student=id=>({id:'student-'+id,firstName:'Synthetic',lastName:'Learner '+id.toUpperCase(),isActive:true});
const teacher={userId:'teacher',firstName:'Synthetic',lastName:'Teacher',batches:['a','b'].map(id=>({id:'class-'+id,name:'Class '+id.toUpperCase(),capacity:1,deliveryMode:'InPerson'})),sessions:['a','b'].map(id=>({id:'session-'+id,batchId:'class-'+id,startUtc:'2026-10-21T09:00:00Z',deliveryMode:'InPerson',status:'Scheduled'}))};
const payout={id:'pay',payrollProfileId:'profile',workerName:'Synthetic Teacher',workerType:'Teacher',payslipNumber:'QA-PAY',periodLabel:'Synthetic period',grossAmount:100,deductions:0,netAmount:100,currency:'INR',status:'Paid',paymentMethod:'BankTransfer',paidAtUtc:null,identitySource:'PayoutSnapshot'};
function responseData(url,route){
 const p=url.pathname;
 if(p==='/api/auth/session')return{workspace:route.startsWith('platform')?'Platform':route==='teacher'?'Teacher':'AcademyAdmin',roles:[route.startsWith('platform')?'PlatformOwner':route==='teacher'?'Teacher':'AcademyAdmin'],isPlatformOwner:route.startsWith('platform'),academyId:'a',displayName:'Synthetic User'};
 if(p==='/api/academies'||p==='/api/platform/academies')return academies;
 if(p.endsWith('/settings'))return{platformName:'Synthetic',defaultCurrency:'INR',defaultTrialDays:14,dataRetentionDays:30,maintenanceMode:false};
 if(p.endsWith('/health'))return{api:'Healthy',database:'Healthy',backgroundJobs:'Healthy',communicationProviders:'Not configured',maintenanceMode:false};
 if(p.endsWith('/overview'))return{openSupportCases:0,totalBilled:0,collectedBilling:0,outstandingBilling:0,overdueInvoices:0,recentAudit:[]};
 if(p.endsWith('/onboarding'))return{status:'Draft',currentSection:'Personal',primaryContactName:p.includes('/b/')?'Contact B':'Contact A'};
 if(p==='/api/teacher/me')return teacher;
 if(p==='/api/teacher/payments')return{paymentModel:'Monthly',payslips:url.search?[]:[payout]};
 if(p==='/api/teacher/classroom-activity')return{resources:[],homework:[]};
 if(p==='/api/teacher/calendar')return{sessions:teacher.sessions,holidays:[]};
 if(p.endsWith('/staff'))return[{id:'staff',displayName:'Synthetic Staff',email:'synthetic@example.invalid',roles:['Teacher'],isActive:true}];
 if(p.endsWith('/students'))return[student('a'),student('b')];
 if(p.endsWith('/roster'))return[student(p.includes('session-b')?'b':'a')];
 if(p.includes('/students/')&&p.endsWith('/guardians'))return[{guardianId:'parent',firstName:p.includes('student-b')?'Parent B':'Parent A',lastName:'Synthetic',isPrimary:true}];
 if(p.endsWith('/guardians'))return[{id:'parent',firstName:'Synthetic',lastName:'Parent',isActive:true}];
 if(/^\/api\/(platform\/(admins|support-cases|billing-invoices|audit|announcements|activity-logs)|portal\/(announcements|notifications)|academies\/a\/(access-grants|batches)|teacher\/sessions\/session-[ab]\/attendance)$/.test(p))return[];
 throw Error('Unexpected API '+p);
}
(async()=>{let browser;const checks=[],failures=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 const allRoutes=['access-review','students','guardians','platform','platform/control','teacher'];
 const selectedRoutes=process.env.QA_PORTAL_READ_ROUTES?process.env.QA_PORTAL_READ_ROUTES.split(','):allRoutes;
 assert.ok(selectedRoutes.length&&new Set(selectedRoutes).size===selectedRoutes.length&&selectedRoutes.every(route=>allRoutes.includes(route)),'Explicit known route subset only');
 try{browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const routeName of selectedRoutes)for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of ['loaded','denied','malformed']){
   const context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage(),errors=[],writes=[];
   try{
    page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(message.type()==='error'&&/hydrat/i.test(message.text()))errors.push(message.text());});
    const workspace=routeName.startsWith('platform')?'Platform':routeName==='teacher'?'Teacher':'AcademyAdmin';
    await context.addInitScript(({theme,workspace})=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.'+workspace,'synthetic-read-state');},{theme,workspace});
    const primary=routeName.startsWith('platform')?'/api/platform/academies':routeName==='teacher'?'/api/teacher/me':'/api/academies';
    await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url());if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const headers={'Access-Control-Allow-Origin':origin};if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:{...headers,'Access-Control-Allow-Headers':'authorization,content-type','Access-Control-Allow-Methods':'GET'}});
     if(req.method()!=='GET'){writes.push(url.pathname);return route.abort('blockedbyclient');}
     let body;try{body=responseData(url,routeName);}catch(error){errors.push(error.message);return route.fulfill({status:500,headers,contentType:'application/json',body:'{}'});}
     if(url.pathname===primary&&scenario!=='loaded')return route.fulfill({status:scenario==='denied'?403:200,headers,contentType:'application/json',body:'{}'});
     return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(body)});
    }return url.origin===origin||url.protocol==='data:'?route.continue():route.abort('blockedbyclient');});
    await page.goto(origin+'/'+routeName+(routeName==='platform/control'?'?tab=Tenant information':''));
    if(scenario==='denied'&&routeName.startsWith('platform'))await page.waitForURL('**/login?**');
    else if(scenario!=='loaded')await page.getByText(routeName==='access-review'?'Access controls could not be loaded.':routeName==='students'?'The student register could not be loaded.':routeName==='guardians'?'Parent contacts could not be loaded. Confirm the API is running on port 5092.':routeName==='platform'?'The Platform Owner dashboard could not be loaded.':routeName==='platform/control'?'The Platform Owner controls could not be loaded.':scenario==='denied'?/active teacher profile/:/could not load|could not be loaded|try again/i,{exact:false}).first().waitFor();
    else if(routeName==='students')await page.getByRole('table').getByText('Synthetic Learner A',{exact:true}).waitFor();
    else if(routeName==='access-review'){await page.getByRole('button',{name:'Select staff member',exact:true}).click();await page.getByRole('option',{name:/Synthetic Staff/}).waitFor();await page.keyboard.press('Escape');}
    else if(routeName==='guardians'){await page.getByText('Parent A Synthetic',{exact:false}).first().waitFor();const select=page.locator('form').filter({has:page.getByRole('heading',{name:'Link parent to student',exact:true})}).locator('select').first();await select.selectOption('student-b');await page.getByText('Parent B Synthetic',{exact:false}).first().waitFor();assert.equal(await page.getByText('Parent A Synthetic',{exact:false}).count(),0);}
    else if(routeName==='platform'){await page.getByText('Synthetic Academy A',{exact:true}).first().waitFor();}
    else if(routeName==='platform/control'){await page.getByText('Contact A',{exact:true}).waitFor();await page.locator('.tenant-information-panel .standard-select-trigger').click();await page.getByRole('option',{name:'Synthetic Academy B',exact:true}).click();await page.getByText('Contact B',{exact:true}).waitFor();assert.equal(await page.getByText('Contact A',{exact:true}).count(),0);}
    else if(routeName==='teacher'){
     await page.getByRole('heading',{name:'Welcome, Synthetic',exact:true}).waitFor();
     const choose=async name=>{if(width===320){const nav=page.getByRole('navigation',{name:'Teacher section navigation',exact:true});await nav.locator('summary').click();await nav.getByRole('button',{name:new RegExp(name+'$')}).click();}else await page.locator('.teacher-portal-sidebar').getByRole('button',{name:new RegExp(name+'$')}).click();};
     await choose('Classroom');await page.getByRole('button',{name:'Class',exact:true}).click();await page.getByRole('option',{name:'Class B · 1:1',exact:true}).click();await page.getByText('Synthetic Learner B',{exact:true}).first().waitFor();assert.equal(await page.getByText('Synthetic Learner A',{exact:true}).count(),0);
     await page.getByRole('button',{name:'Class',exact:true}).click();await page.getByRole('option',{name:'Select class',exact:true}).click();assert.equal(await page.getByText('Synthetic Learner B',{exact:true}).count(),0);assert.equal(await page.getByText('Teaching focus',{exact:true}).count(),0);
     await choose('Pay slip');await page.getByRole('button',{name:'Preview / Print',exact:true}).click();await page.getByRole('dialog',{name:'Recorded payslip',exact:true}).waitFor();await page.getByRole('button',{name:'Close preview',exact:true}).click();await page.getByLabel('Month',{exact:true}).fill('2026-01');assert.equal(await page.getByRole('dialog').count(),0);
    }
    assert.deepEqual(errors,[]);assert.deepEqual(writes,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'No document horizontal overflow');
    if(width===320&&scenario==='loaded')await page.screenshot({path:path.join(evidence,routeName.replace('/','-')+'-'+theme+'.png'),fullPage:true});
    checks.push({route:routeName,width,theme,scenario,writes:0,page_errors:0,synthetic_API:true});
   }catch(error){failures.push({route:routeName,width,theme,scenario,error:error.message,url:page.url()});await page.screenshot({path:path.join(evidence,'incomplete.png'),fullPage:true}).catch(()=>{});fs.writeFileSync(path.join(evidence,'incomplete-body.txt'),await page.locator('body').innerText().catch(()=>''));throw error;}finally{await context.close();}
  }
 }finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({at:new Date().toISOString(),selected_routes:selectedRoutes,checks,failures,scope:'exported React/shared controls with synthetic APIs;not SQL/role/device acceptance',production_approved:false},null,2));console.log(JSON.stringify({evidence,passed:checks.length,failed:failures.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
