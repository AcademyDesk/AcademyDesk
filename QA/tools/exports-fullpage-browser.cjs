// Actual exported React pages, synthetic API responses. Not real SQL/Identity/device acceptance.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'exports-fullpage-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req,res) => { try {
  assert.ok(['GET','HEAD'].includes(req.method));
  let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url,'http://localhost').pathname));
  if (!path.extname(file)) file += '.html';
  const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
  if (!fs.existsSync(file) && segment) { const parts=segment[1].split('.'); file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt'); }
  file=fs.realpathSync(file); assert.ok(file.startsWith(web+path.sep));
  res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});
  if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 let browser;const checks=[];
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{
  browser=await chromium.launch({headless:true,downloadsPath:evidence,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const routeName of ['reports','data-operations','finance'])for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of (routeName==='reports'?['normal']:['normal','denied','network','wrong-type'])){
   const context=await browser.newContext({viewport:{width,height:1000},acceptDownloads:true});
   try{
    const page=await context.newPage(),errors=[],calls=[],downloads=[];
    page.on('pageerror',e=>errors.push(e.message));page.on('download',d=>downloads.push(d));
    await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-export-token');},theme);
    await context.route('**/*',async route=>{
     const request=route.request(),url=new URL(request.url());
     if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
      assert.equal(request.method(),'GET');calls.push(url.pathname);const headers={'Access-Control-Allow-Origin':origin};
      if(url.pathname.includes('/exports/')){
       if(scenario==='network')return route.abort('failed');
       return route.fulfill({status:scenario==='denied'?403:200,headers,contentType:scenario==='wrong-type'?'text/html':'text/csv;charset=utf-8',body:'"Label"\r\n"\t=1+2"'});
      }
      const rows={
       '/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Finance","FinanceControls","AccessGovernance"]'}],
       '/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},
       '/api/portal/announcements':[],
       '/api/academies/owned/students':[{id:'student',firstName:'=1+2',lastName:'Synthetic',isActive:true}],
       '/api/academies/owned/teachers':[], '/api/academies/owned/batches':[], '/api/academies/owned/enrollments':[],
       '/api/academies/owned/sessions':[],
       '/api/academies/owned/invoices':[{id:'invoice',studentId:'student',invoiceNumber:'DISPLAY-ONLY',totalAmount:1000,adjustedAmount:200,paidAmount:600,balance:200,currency:'INR',dueDate:'2026-10-25',status:'PartiallyPaid'}],
       '/api/academies/owned/payments':[{invoiceId:'invoice',amount:600,currency:'INR',status:'Reconciled'}],
       '/api/academies/owned/expenses':[], '/api/academies/owned/payroll/payouts':[],
       '/api/academies/owned/finance-governance/summary':{grossBilled:1000,approvedAdjustments:200,collected:600,outstanding:200,reconciled:600,overdueInvoices:0},
       '/api/academies/owned/finance-governance/collections':[],
      };
      assert.ok(Object.hasOwn(rows,url.pathname),'Unexpected API: '+url.pathname);
      return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(rows[url.pathname])});
     }
     if(url.origin===origin||url.protocol==='data:'||url.protocol==='blob:')return route.continue();
     return route.abort('blockedbyclient');
    });
    await page.goto(origin+'/'+routeName);
    let button;
    if(routeName==='reports'){
     button=page.locator('.workspace-reports-export-card').filter({hasText:'Student contact register'}).getByRole('button');
     await page.waitForFunction(()=>document.querySelector('.workspace-reports-kpis')?.textContent.includes('INR ₹600.00'));
     const kpis=await page.locator('.workspace-reports-kpis').innerText();
     assert.match(kpis,/Fees: INR ₹200\.00/);assert.doesNotMatch(kpis,/Net cash/);
     await page.getByRole('button',{name:/Collected/}).click();
     await page.getByRole('dialog').filter({hasText:'Collected fee payments'}).waitFor();
     assert.match(await page.getByRole('dialog').innerText(),/600\.00/);
     await page.keyboard.press('Escape');
    }else button=routeName==='finance'?page.getByRole('button',{name:'Invoice ledger CSV',exact:true}):page.getByRole('button',{name:'Download CSV',exact:true}).first();
    await button.waitFor();await page.waitForFunction(()=>[...document.querySelectorAll('button')].some(b=>/Download CSV|Invoice ledger CSV/.test(b.textContent)&&!b.disabled));
    await button.click();
    if(scenario==='normal'){
     await page.waitForEvent('download',{timeout:1500}).catch(()=>{});
     await page.waitForFunction(()=>true);
     assert.equal(downloads.length,1);const download=downloads[0];assert.equal(await download.failure(),null);
     const file=path.join(evidence,`${routeName}-${width}-${theme}.csv`);await download.saveAs(file);
     const content=fs.readFileSync(file,'utf8');
     if(routeName==='reports'){assert.match(content,/"\t=1\+2"/);assert.equal(download.suggestedFilename(),'academydesk-students.csv');}
     else{assert.equal(content,'"Label"\r\n"\t=1+2"');await page.getByText(/CSV download started/).waitFor();}
    }else{
     await page.getByText(/could not be downloaded|could not be created/).waitFor();
     assert.equal(downloads.length,0);assert.equal(calls.filter(path=>path.includes('/exports/')).length,1);
    }
    assert.deepEqual(errors,[]);
    assert.ok(await button.isEnabled());
    checks.push({route:routeName,width,theme,scenario,downloads:downloads.length,page_errors:0,synthetic_API:true});
   }finally{await context.close();}
  }
  fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({scope:'actual exported React pages with synthetic APIs, not SQL/Identity/device',checks,production_approved:false},null,2));
  console.log(JSON.stringify({evidence,passed:checks.length}));
 }finally{if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
