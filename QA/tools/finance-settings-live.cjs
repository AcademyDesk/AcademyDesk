// Export -> loopback relay -> real Identity/ASP.NET/owned SQL. No API responses mocked.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api,academy,casesJson]=process.argv.slice(2),target=new URL(api),cases=JSON.parse(casesJson);
assert.equal(target.hostname,'127.0.0.1');assert.equal(target.protocol,'http:');assert.ok(target.port);assert.equal(cases.length,8);assert.match(academy,/^[a-f0-9-]{36}$/);
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE','finance-settings-live-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
const conflict='Settings changed in another window. Your draft has not been saved. Refresh this page and review the latest settings before saving again.';
(async()=>{await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;const checks=[];
try{
 browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 for(const {width,theme,page:routeName,email} of cases){
  assert.match(email,/^qa-settings-ui-(320|1440)-(light|dark)-finance-(policy|governance)@example\.invalid$/);assert.ok(['finance-policy','finance-governance'].includes(routeName));
  const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
  let releaseInitial,seenInitial,initialHeld=false;const initialGate=new Promise(r=>releaseInitial=r),initialSeen=new Promise(r=>seenInitial=r);
  const login=await context.request.post(api+'/api/auth/login',{data:{email,password:'Synthetic!39Ab'}});assert.equal(login.status(),200);const {accessToken}=await login.json();assert.ok(accessToken);
  const headers={Authorization:'Bearer '+accessToken},settingsPath=`/api/academies/${academy}/finance-governance/settings`;
  const read=async()=>{const r=await context.request.get(api+settingsPath,{headers});assert.equal(r.status(),200);const data=await r.json();assert.match(data.settingsVersion,/^[A-F0-9]{64}$/);return data;};
  await context.addInitScript(({theme,accessToken})=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin',accessToken);},{theme,accessToken});
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'&&!/409 \(Conflict\)/.test(m.text()))errors.push(m.text());});
  await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url());if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
   const response=await route.fetch({url:api+url.pathname+url.search});assert.notEqual(response.status(),429);calls.push({path:url.pathname,method:req.method(),status:response.status(),...(req.method()==='PUT'?{body:req.postDataJSON(),response:await response.json()}: {})});
   if(routeName==='finance-governance'&&req.method()==='GET'&&url.pathname===settingsPath&&!initialHeld){initialHeld=true;seenInitial();await initialGate;}
   return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
  }
  // Existing SQL fixtures use example.invalid branding images. Render a local
  // synthetic image only; API responses remain real and public network blocked.
  if(url.origin==='https://example.invalid'&&['/logo.png','/sign.png'].includes(url.pathname))return route.fulfill({status:200,contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="80" height="32"><text x="2" y="22">QA</text></svg>'});
  if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');});
  const fieldName=routeName==='finance-policy'?'taxLabel':'invoiceAuthorityName',saveName=routeName==='finance-policy'?'Save invoice settings':'Save document setup',success=routeName==='finance-policy'?'Finance policy saved.':'Invoice and payslip settings saved.';
  async function editor(){if(routeName==='finance-governance')await page.getByRole('button',{name:'Edit document setup',exact:true}).click();const input=page.locator(`input[name="${fieldName}"]`);await input.waitFor();return input;}
  try{
   await page.goto(origin+'/'+routeName);
   if(routeName==='finance-governance'){
    await Promise.race([initialSeen,new Promise((_,reject)=>setTimeout(()=>reject(Error('Initial real settings GET not observed')),15000))]);
    const edit=page.getByRole('button',{name:'Edit document setup',exact:true});assert.equal(await edit.isDisabled(),true);await edit.evaluate(button=>button.click());assert.equal(await page.locator('input[name="invoiceAuthorityName"]').count(),0);releaseInitial();
   }
   let input=await editor();const loaded=await read();const first=`UI ${routeName==='finance-policy'?'P':'G'} ${width} ${theme}`;assert.ok(('Newer tax '+first).length<=30);await input.fill(first);await page.getByRole('button',{name:saveName,exact:true}).click();await page.getByText(success,{exact:true}).waitFor();
   const saved=await read();assert.equal(saved[fieldName],first);assert.equal(saved.payslipTemplateKey,loaded.payslipTemplateKey);assert.notEqual(saved.settingsVersion,loaded.settingsVersion);assert.equal(calls.filter(x=>x.method==='PUT').length,1);
   if(routeName==='finance-governance')input=await editor();await input.fill('Stale '+first);
   const competing=await context.request.put(api+settingsPath,{headers,data:{...saved,payslipTemplateKey:saved.payslipTemplateKey==='Compact'?'Professional':'Compact',taxLabel:'Newer tax '+first,invoiceAuthorityName:'Newer signatory '+first}});assert.equal(competing.status(),200);const newer=await competing.json();
   await page.getByRole('button',{name:saveName,exact:true}).click();await page.getByText(conflict,{exact:true}).waitFor();assert.equal(await input.inputValue(),'Stale '+first);assert.deepEqual(await read(),newer);assert.equal(calls.filter(x=>x.method==='PUT').length,2);assert.equal(calls.filter(x=>x.method==='PUT')[1].status,409);
   await page.screenshot({path:path.join(evidence,`${routeName}-${width}-${theme}-conflict.png`),fullPage:true});
   // Refresh is explicit; no stale draft is automatically retried or rebased.
   await page.reload();input=await editor();assert.equal(await input.inputValue(),newer[fieldName]);const finalDraft='Recovered '+first;await input.fill(finalDraft);await page.getByRole('button',{name:saveName,exact:true}).click();await page.getByText(success,{exact:true}).waitFor();const final=await read();assert.equal(final[fieldName],finalDraft);assert.equal(final.payslipTemplateKey,newer.payslipTemplateKey);
   const puts=calls.filter(x=>x.method==='PUT');assert.deepEqual(puts.map(x=>x.status),[200,409,200]);assert.deepEqual(puts.map(x=>x.body.settingsVersion),[loaded.settingsVersion,saved.settingsVersion,newer.settingsVersion]);assert.notEqual(final.settingsVersion,newer.settingsVersion);assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   await page.screenshot({path:path.join(evidence,`${routeName}-${width}-${theme}-recovery.png`),fullPage:true});checks.push({routeName,width,theme,calls,final});console.log(`PASS: SETTINGS LIVE ${routeName} ${width} ${theme} save/stale409/draft/explicit-refresh/recovery`);
  }catch(e){releaseInitial();fs.writeFileSync(path.join(evidence,`${routeName}-${width}-${theme}-failed.json`),JSON.stringify({calls,errors,failure:e.message},null,2));await page.screenshot({path:path.join(evidence,`${routeName}-${width}-${theme}-failed.png`),fullPage:true});throw e;}finally{releaseInitial();await context.close();}
 }
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({journeys:checks.length,checks,transport:'real Identity/HTTP/owned SQL with loopback relay',physicalDevices:'NOT RUN'},null,2));console.log('PASS: SETTINGS LIVE evidence='+evidence);
}finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}})().catch(e=>{console.error(e.message);process.exitCode=1;server.close();});
