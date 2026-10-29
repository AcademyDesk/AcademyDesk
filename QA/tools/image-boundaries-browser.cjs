// Exported application + synthetic API/media only. No customer credentials/data.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const pick=require('./shared-picker-actions.cjs');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE','image-boundaries-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const png=Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aWQAAAABJRU5ErkJggg==','base64');
const picture='/api/qa-profile.png',content='/api/class-media/33333333-3333-4333-8333-333333333333/content';
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
const person={id:'s',firstName:'Synthetic',lastName:'Learner',isActive:true};
const academy={id:'a',name:'Synthetic Academy',countryCode:'IN',timeZone:'Asia/Kolkata',isActive:true,subscriptionPlan:'Growth',subscriptionStatus:'Active',enabledModulesJson:JSON.stringify(['Core','AcademicGovernance','Finance','FinanceControls','Certificates'])};
const batch={id:'b',name:'Synthetic Class',capacity:1,deliveryMode:'Online',isActive:true};
const certificate={id:'c',studentId:'s',batchId:'b',certificateNumber:'CERT-SYNTHETIC',verificationCode:'QA-VERIFY',title:'Saved achievement',templateKey:'school-merit',issuedDate:'2026-10-21',status:'Issued',notes:null};
const invoice={id:'i',studentId:'s',invoiceNumber:'QA-I',totalAmount:1000,paidAmount:600,adjustedAmount:0,balance:400,currency:'INR',issuedDate:'2026-10-21',dueDate:'2026-10-30',status:'PartiallyPaid'};
function data(p,route){
 if(p==='/api/auth/session')return{workspace:route.startsWith('platform')?'Platform':route==='teacher'?'Teacher':route==='portal'?'Portal':'AcademyAdmin',roles:[route.startsWith('platform')?'PlatformOwner':route==='teacher'?'Teacher':route==='portal'?'Student':'AcademyAdmin'],isPlatformOwner:route.startsWith('platform'),academyId:'a',displayName:'Synthetic User',profileImageUrl:picture};
 if(p==='/api/academies'||p==='/api/platform/academies')return[academy];
 if(p.endsWith('/overview'))return{openSupportCases:0,totalBilled:0,collectedBilling:0,outstandingBilling:0,overdueInvoices:0,recentAudit:[]};
 if(p.endsWith('/health'))return{api:'Healthy',database:'Healthy',backgroundJobs:'Healthy',communicationProviders:'Not configured',maintenanceMode:false};
 if(p==='/api/platform/settings')return{platformName:'Synthetic',defaultCurrency:'INR',defaultTrialDays:14,dataRetentionDays:30,maintenanceMode:false};
 if(p.endsWith('/onboarding'))return{};
 if(p==='/api/teacher/me')return{userId:'t',firstName:'Synthetic',lastName:'Teacher',batches:[batch],sessions:[{id:'session',batchId:'b',startUtc:'2026-10-21T09:00:00Z',deliveryMode:'Online',status:'Scheduled'}]};
 if(p==='/api/teacher/calendar')return{sessions:[],holidays:[]};
 if(p==='/api/teacher/payments')return{paymentModel:'Monthly',payslips:[]};
 if(p==='/api/teacher/classroom-activity')return{resources:[{id:'r',title:'Synthetic private image',url:content,type:'Image',createdAtUtc:'2026-10-21T09:00:00Z'}],homework:[]};
 if(p==='/api/portal/me')return{role:'Student',displayName:'Synthetic Learner',studentId:'s'};
 if(p==='/api/portal/students/s')return{name:'Synthetic Learner',firstName:'Synthetic',lastName:'Learner',schedule:[],assignments:[],attendanceSummary:{total:0,present:0,late:0},practiceSummary:{logCount:0,totalMinutes:0},practiceLogs:[],music:[],resources:[],assessmentResults:[],certificates:[],invoices:[],classHistory:[],cycleProgress:[]};
 if(p.endsWith('/students')||p.endsWith('/roster')||p.endsWith('/student-options'))return[person];
 if(p.endsWith('/batches'))return[batch];
 if(p.endsWith('/certificates/branding'))return{academyName:'Synthetic Academy',accentColor:'#0F6CBD',signatoryName:'Saved Signatory',logoUrl:picture};
 if(p.endsWith('/certificates'))return[certificate];
 if(p.endsWith('/enrollments'))return[{studentId:'s',batchId:'b',status:'Active'}];
 if(p.endsWith('/invoices/document-settings'))return{invoiceTemplateKey:'Classic',invoiceLogoUrl:'data:image/png;base64,'+png.toString('base64'),invoiceSignatureUrl:'data:image/png;base64,'+png.toString('base64')};
 if(p.endsWith('/invoices'))return[invoice];
 if(p.endsWith('/fee-plans')||p.endsWith('/attendance')||p.endsWith('/leave-requests')||p==='/api/portal/notifications'||p==='/api/portal/announcements'||/^\/api\/platform\/(admins|support-cases|billing-invoices|audit|announcements)$/.test(p))return[];
 throw Error('Unexpected API '+p);
}
async function checkImage(image){await image.waitFor();await image.evaluate(img=>img.decode());assert.equal(await image.getAttribute('data-nimg')!==null,true);assert.equal(await image.getAttribute('loading'),'eager');assert.equal(await image.getAttribute('srcset'),null);const box=await image.boundingBox();assert.ok(box&&box.width>0&&box.height>0,'visible decoded image');return box;}
(async()=>{let browser;const checks=[],failures=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 for(const routeName of ['platform','platform/control','teacher','portal','certificates','invoices'])for(const width of[320,1440])for(const theme of['light','dark']){
 const context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage(),errors=[],requests=[],writes=[];
 try{
 page.on('pageerror',error=>errors.push(error.message));page.on('console',msg=>{if(msg.type()==='error'&&/hydrat|Image with src/i.test(msg.text()))errors.push(msg.text());});
 await context.addInitScript(({theme,route})=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.'+(route.startsWith('platform')?'Platform':route==='teacher'?'Teacher':route==='portal'?'Portal':'AcademyAdmin'),'synthetic-image-token');window.qaPrints=0;window.print=()=>window.qaPrints++;window.qaRevoked=[];const revoke=URL.revokeObjectURL.bind(URL);URL.revokeObjectURL=url=>{window.qaRevoked.push(url);revoke(url);};},{theme,route:routeName});
 await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url());requests.push(url.pathname);if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
 const headers={'Access-Control-Allow-Origin':origin,'Access-Control-Allow-Headers':'authorization,content-type','Access-Control-Expose-Headers':'Content-Length,Content-Disposition'};
 if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:{...headers,'Access-Control-Allow-Methods':'GET,HEAD'}});
 if(!['GET','HEAD'].includes(req.method())){writes.push(url.pathname);return route.abort('blockedbyclient');}
 if(url.pathname===picture||url.pathname===content)return route.fulfill({status:200,headers:{...headers,'Content-Length':String(png.length),'Content-Disposition':'attachment; filename="synthetic.png"'},contentType:'image/png',body:req.method()==='HEAD'?undefined:png});
 let body;try{body=data(url.pathname,routeName);}catch(error){errors.push(error.message);return route.fulfill({status:500,headers,body:'{}'});}return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(body)});
 }return url.origin===origin||['data:','blob:'].includes(url.protocol)?route.continue():route.abort('blockedbyclient');});
 await page.goto(origin+'/'+routeName+(routeName==='platform/control'?'?tab=Settings':''));
 await checkImage(page.locator('.enterprise-profile-trigger img').first());
 if(routeName==='platform/control')await checkImage(page.locator('.platform-owner-photo img'));
 if(routeName==='certificates'){
 await checkImage(page.locator('.certificate-logo-slot img'));await pick.select(page,'printCertificateId','CERT-SYNTHETIC · Saved achievement · Issued');
 await page.getByRole('button',{name:'Print / save PDF',exact:true}).click();await page.waitForFunction(()=>window.qaPrints===1);await checkImage(page.locator('.certificate-preview img'));assert.match(await page.locator('.certificate-preview').innerText(),/CERT-SYNTHETIC/);await page.evaluate(()=>dispatchEvent(new Event('afterprint')));
 }
 if(routeName==='invoices'){await page.getByRole('button',{name:'Preview / Print',exact:true}).click();await checkImage(page.locator('.invoice-paper-logo'));await checkImage(page.locator('.invoice-paper-signature img'));await page.emulateMedia({media:'print'});assert.ok((await page.locator('.invoice-paper-logo').boundingBox()).width>0);await page.emulateMedia({media:'screen'});}
 if(routeName==='teacher'){
 if(width===320){const nav=page.getByRole('navigation',{name:'Teacher section navigation',exact:true});await nav.locator('summary').click();await nav.getByRole('button',{name:/Classroom$/}).click();}else await page.locator('.teacher-portal-sidebar').getByRole('button',{name:/Classroom$/}).click();
 await page.getByRole('button',{name:'Class',exact:true}).click();await page.getByRole('option',{name:'Synthetic Class · 1:1',exact:true}).click();
 await page.getByText('Synthetic private image',{exact:true}).waitFor();const actions=page.locator('.private-material-actions');await actions.getByRole('button',{name:'Preview',exact:true}).click();const preview=page.locator('.private-material-image img');await checkImage(preview);const blob=await preview.getAttribute('src');assert.ok(blob.startsWith('blob:'));await actions.getByRole('button',{name:'Close preview',exact:true}).click();assert.ok(await page.evaluate(url=>window.qaRevoked.includes(url),blob));
 await page.locator('.teacher-material-uploader input[type="file"], .teacher-upload-form input[type="file"], input[accept]:not(.sr-only)').last().setInputFiles({name:'synthetic.png',mimeType:'image/png',buffer:png});await checkImage(page.locator('.teacher-file-image img'));
 }
 assert.deepEqual(errors,[]);assert.deepEqual(writes,[]);assert.ok(!requests.some(p=>p.startsWith('/_next/image')),'no optimizer request');
 checks.push({route:routeName,width,theme,decoded:true,optimizer_requests:0,writes:0,synthetic_API:true});if(width===320)await page.screenshot({path:path.join(evidence,routeName.replace('/','-')+'-'+theme+'.png'),fullPage:true});
 }catch(error){failures.push({route:routeName,width,theme,error:error.message});await page.screenshot({path:path.join(evidence,'incomplete.png'),fullPage:true}).catch(()=>{});fs.writeFileSync(path.join(evidence,'incomplete-body.txt'),await page.locator('body').innerText().catch(()=>''));throw error;}finally{await context.close();}
 }
 }finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,failures,scope:'synthetic browser transport, not SQL/role/device/PDF acceptance'},null,2));console.log(JSON.stringify({evidence,passed:checks.length,failed:failures.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
