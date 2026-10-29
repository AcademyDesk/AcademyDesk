'use strict';
// Real exported page -> private in-memory HTTP relay -> native candidate -> owned SQL.
// No API response mocks, token injection, public port, model call or customer data.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),crypto=require('node:crypto'),a=require('node:assert/strict');
const{chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),build='bad444b6c61e45f680ed62f76d35dcf4',exportRelative='.build-check/default-web-build/'+build+'/app/out';
function staticFile(base,urlPath){
 const decoded=decodeURIComponent(urlPath);a.ok(!decoded.includes('\\'));let file=path.resolve(base,'.'+(decoded==='/'?'/index.html':decoded));if(!path.extname(file))file+='.html';const real=fs.realpathSync(file);a.ok(real.startsWith(base+path.sep));a.equal(fs.statSync(real).isFile(),true);return real;
}
function allowedApi(method,url,academy){
 if(method==='GET')return /^\/api\/[A-Za-z0-9_./?=&%-]+$/.test(url);
 return method==='POST'&&(url==='/api/auth/login'||url==='/api/auth/login?useCookies=false'||url==='/api/auth/refresh'||url===`/api/academies/${academy}/payments`);
}
function verifyExport(id=process.env.QA_WEB_CANDIDATE){
 if(process.env.QA_COMPACT_THEME_UI){
  const source=process.env.QA_COMPACT_THEME_UI;a.match(source,/^[0-9a-f]{32}$/);
  const receipt=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/compact-theme-ui-'+source+'/result.json')));
  a.equal(receipt.passed,true);a.equal(receipt.steps.length,5);
  a.ok(receipt.steps.every(s=>s.exitCode===0&&s.timedOut===false));
  return require('./compact-theme-ui-export.cjs').verify(source);
 }
 if(process.env.QA_STAFF_UI)return require('./staff-ui-export.cjs').verify(process.env.QA_STAFF_UI);
 if(process.env.QA_ACCESS_REVIEW_UI)return require('./access-review-ui-export.cjs').verify(process.env.QA_ACCESS_REVIEW_UI);
 if(process.env.QA_GUARDIAN_UI)return require('./guardian-context-ui-export.cjs').verify(process.env.QA_GUARDIAN_UI);
 if(process.env.QA_ZOD_CANDIDATE)return require('./zod-security-candidate.cjs').verify(process.env.QA_ZOD_CANDIDATE);
 if(process.env.QA_ENROLLMENT_UI)return require('./enrollment-conflict-ui-export.cjs').verify(process.env.QA_ENROLLMENT_UI);
 if(id){const web=require('./web-candidate-export.cjs').resolveExport(root,id),context=require('./web-dependency-candidate.cjs').open(id);return{web,build:id,exportRelative:path.relative(root,web).replaceAll('\\','/'),verifiedExportFiles:context.result.exportManifest.length};}
 const web=fs.realpathSync(path.join(root,exportRelative));
 const buildReceipt=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/default-web-build-'+build+'/result.json')));a.equal(buildReceipt.passed,true);a.deepEqual(require('./default-web-build.cjs').sourcePins(path.join(root,'apps/web')),buildReceipt.sourcePins);
 const accepted=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/static-web-runtime-6ee3936711bd4217b57f52b0747b5942/result.json')));a.equal(accepted.exportRelative,exportRelative);
 for(const file of accepted.checks){const local=staticFile(web,file.path);a.equal(crypto.createHash('sha256').update(fs.readFileSync(local)).digest('hex'),file.sha256);}
 return{web,build,exportRelative,verifiedExportFiles:accepted.checks.length};
}
async function verifyPaymentBrowser({run,result,exchange,accounts,tenants,password,call,snapshot,evidence}){
 a.match(run,/^[0-9a-f]{32}$/);const tenant=tenants[1],verified=verifyExport(),web=verified.web;
 const student=call('POST','students',{firstName:'Synthetic',lastName:'Browser'},201),fixtures=[];
 for(let i=0;i<4;i++){const invoice=call('POST','invoices',{studentId:student.id,amount:1000},201),payment=call('POST','payments',{invoiceId:invoice.id,amount:600,method:'Offline'},201);call('PATCH','payments/'+payment.id+'/reconcile',{reference:'Synthetic browser baseline'},200);fixtures.push(invoice);}
 const facts=result.paymentBrowser={passed:false,phase:'launch',exportBuild:verified.build,exportRelative:verified.exportRelative,verifiedExportFiles:verified.verifiedExportFiles,apiResponseMocks:false,tokenInjection:false,physicalDevicesTested:false,cases:[]};
 const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));const file=staticFile(web,new URL(req.url,'http://localhost').pathname);res.writeHead(200,{'Content-Type':({'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.png':'image/png','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
 let browser;await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',args:['--disable-dev-shm-usage']});
  let index=0;
  for(const width of[390,1440])for(const theme of['light','dark']){
   const invoice=fixtures[index],stale=index++%2===1,context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),errors=[],requests=[];
   facts.currentCase={width,theme,scenario:stale?'stale balance rejected and reset':'successful payment and reset',requests,errors};
   try{
    await context.addInitScript(mode=>localStorage.setItem('academydesk.theme',mode),theme);
    page.on('pageerror',()=>errors.push('uncaught page error'));
    await context.route('**/*',async route=>{
     const request=route.request(),url=new URL(request.url());
     if(url.pathname.startsWith('/api/')){
      if(!['127.0.0.1','localhost'].includes(url.hostname)||!allowedApi(request.method(),url.pathname+url.search,tenant.id)){errors.push('unapproved API request');return route.abort();}
      const actual=await exchange(request.method(),url.pathname+url.search,request.postData(),request.headers().authorization);
      requests.push({method:request.method(),path:url.pathname,status:actual.status});a.notEqual(actual.status,429);
      return route.fulfill({status:actual.status,body:actual.body,headers:{'content-type':actual.contentType||'application/json','access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
     }
     if(url.origin===origin||url.protocol==='data:')return route.continue();errors.push('external request refused');return route.abort();
    });
    facts.phase='visible login';await page.goto(origin+'/login?returnTo=%2Fpayments');await page.getByLabel('User name',{exact:true}).fill(accounts[1].email);await page.getByLabel('Password',{exact:true}).fill(password);
    const login=page.waitForResponse(response=>new URL(response.url()).pathname==='/api/auth/login'&&response.request().method()==='POST');await page.getByRole('button',{name:'Sign in',exact:true}).click();a.equal((await login).status(),200);await page.waitForURL('**/payments');
    facts.phase='invoice picker';const form=page.locator('form.payments-create');await form.getByRole('button',{name:'Invoice',exact:false}).waitFor();
    const picker=form.locator('.standard-select-field').filter({has:page.locator('input[name="invoiceId"]')});await picker.locator('.standard-select-trigger').click();const option=page.getByRole('option').filter({hasText:invoice.invoiceNumber});await option.click();a.equal(await page.getByRole('listbox').count(),0);
    a.equal(await form.locator('input[type="number"]').inputValue(),'400');
    if(stale)call('POST','payments',{invoiceId:invoice.id,amount:400,method:'Offline'},201);
    facts.phase='visible submit and result';const submitted=page.waitForResponse(response=>new URL(response.url()).pathname===`/api/academies/${tenant.id}/payments`&&response.request().method()==='POST');await form.getByRole('button',{name:'Record payment',exact:true}).click();a.equal((await submitted).status(),stale?400:201);
    if(stale)await page.getByRole('alert').filter({hasText:'Payment exceeds the invoice balance.'}).waitFor();else await page.getByRole('status').filter({hasText:'Payment recorded successfully. Invoice balances are up to date.'}).waitFor();
    await page.waitForFunction(()=>document.querySelector('input[name="invoiceId"]')?.value==='');
    a.equal(await form.locator('input[type="number"]').inputValue(),'');a.ok(await form.getByRole('button',{name:'Record payment',exact:true}).isDisabled());
    facts.phase='SQL and layout assertions';const stored=snapshot(invoice.id);a.deepEqual(stored,{rows:2,collected:1000,status:'Paid',balance:0});
    a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false);a.deepEqual(errors,[]);await page.screenshot({path:path.join(evidence,`payments-${width}-${theme}.png`),fullPage:true});
    facts.cases.push({width,theme,scenario:stale?'stale balance rejected and reset':'successful payment and reset',submittedStatus:stale?400:201,sql:stored,visiblePickerCollapsed:true,noHorizontalOverflow:true,pageErrors:0,requests});
   }catch(error){facts.errorType=['TimeoutError','AssertionError'].includes(error.name)?error.name:'operation error';throw error;}finally{await context.close();}
  }
  a.equal(facts.cases.length,4);delete facts.currentCase;facts.phase='complete';facts.passed=true;
 }finally{if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
}
module.exports={allowedApi,staticFile,verifyExport,verifyPaymentBrowser};if(require.main===module){try{const r=verifyExport();console.log(JSON.stringify({build:r.build,exportRelative:r.exportRelative,verifiedExportFiles:r.verifiedExportFiles,browserStarted:false,sqlStarted:false}));}catch{console.error('Pinned browser export preflight failed; no SQL or browser started.');process.exitCode=1;}}
