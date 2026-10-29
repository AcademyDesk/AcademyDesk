'use strict';
// Real Identity/HTTP/SQL application reads. Separate Node-only controller expires
// exact synthetic fixtures; no model, fabricated API bodies or browser writes.
const a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const{chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const[api,run]=process.argv.slice(2);a.match(api||'',/^http:\/\/127\.0\.0\.1:\d+$/);a.match(run||'',/^[0-9a-f]{32}$/);
const web=path.resolve(__dirname,'../../apps/web/out'),evidence=path.resolve(__dirname,'../EVIDENCE','penta-onboarding-expiry-live-'+run);fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.json':'application/json','.woff2':'font/woff2','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));a.ok(file.startsWith(web+path.sep));if(!path.extname(file))file+='.html';const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}file=fs.realpathSync(file);a.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
async function main(){let input='';for await(const chunk of process.stdin){input+=chunk;a.ok(input.length<65536);}const fixture=JSON.parse(input);input='';a.equal(fixture.fixtures.length,4);a.match(fixture.controllerSecret,/^[0-9a-f]{64}$/);
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin=`http://127.0.0.1:${server.address().port}`;
 const browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'}),checks=[];
 try{for(const[index,f]of fixture.fixtures.entries()){
  for(const key of['conversation','draft'])a.match(f[key],/^[0-9a-f-]{36}$/);
  const width=index%2?1440:320,theme=index%2?'dark':'light';
  const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[],expectedHttp=[];
  try{
   a.ok(Number.isFinite(Date.parse(f.expiresAtUtc)));
   if(!f.pending)await page.clock.install({time:Date.now()});
   await context.addInitScript(({token,theme})=>{localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);localStorage.setItem('academydesk.theme',theme);}, {token:fixture.token,theme});
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'){
    if(/^Failed to load resource: the server responded with a status of 410 \(/.test(m.text()))expectedHttp.push(m.text());else errors.push(m.text());
   }});
   await context.route('**/*',async route=>{const request=route.request(),url=new URL(request.url());if(url.pathname.startsWith('/api/')){
    a.equal(request.method(),'GET');a.ok(['localhost','127.0.0.1'].includes(url.hostname));const response=await route.fetch({url:api+url.pathname,maxRedirects:0,maxRetries:0});a.notEqual(response.status(),429);calls.push({path:url.pathname,method:request.method(),status:response.status()});
    return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Accept'}});
   }if(url.origin===origin||url.protocol==='data:')return route.continue();throw Error('Unexpected external request');});
   const target=`${origin}/penta/onboarding?conversationId=${f.conversation}&draftId=${f.draft}`;
   await page.goto(target);
   if(f.pending){await page.getByRole('button',{name:'Check pending request',exact:true}).waitFor();a.equal(calls.length,2);}
   else{
    const canvas=page.getByRole('button',{name:/Work canvas/});await canvas.waitFor();a.equal(calls.length,4);
    await canvas.click();const first=page.getByRole('dialog',{name:'Work canvas',exact:true}).getByRole('textbox',{name:'Student First Name (required)',exact:true});
    a.equal(await first.inputValue(),'Synthetic Expiry Private');a.equal(await first.isDisabled(),true);await page.getByRole('button',{name:'Close Work canvas',exact:true}).click();
   }
   const beforeExpiry=calls.length;
   const hidden=async()=>{a.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);a.equal(await page.getByRole('button',{name:'Check pending request',exact:true}).count(),0);a.equal(await page.getByRole('button',{name:'Create approved student',exact:true}).count(),0);a.equal(await page.getByRole('textbox',{name:'Ask PENTA',exact:true}).count(),0);a.ok(!(await page.locator('body').innerText()).includes('Synthetic Expiry Private'));a.equal(await page.getByRole('link',{name:'Open Manual student onboarding',exact:true}).getAttribute('href'),'/student-onboarding');};
   // Node-only control secret never enters page code, localStorage, URLs or logs.
   const expired=await fetch(api+'/qa/expire/'+f.conversation,{method:'POST',redirect:'error',headers:{'X-QA-Control':fixture.controllerSecret}});a.equal(expired.status,204);
   if(!f.pending){
    // Browser clock drives the actual mounted timer; separate host clock still
    // independently denies the subsequent HTTP reads. No network polling here.
    await page.clock.fastForward(Math.max(0,Date.parse(f.expiresAtUtc)-Date.now())+1000);
    await page.getByText('Onboarding actions are unavailable',{exact:true}).waitFor();
    await hidden();a.equal(calls.length,beforeExpiry);
    await page.screenshot({path:path.join(evidence,f.name+'-idle-expired.png')});
   }
   await page.getByRole('button',{name:'Recheck host state',exact:true}).click();
   await page.getByText('Onboarding actions are unavailable',{exact:true}).waitFor();
   a.equal(calls.length,beforeExpiry+2);a.equal(calls.at(-1).status,410);a.ok(calls.at(-1).path.endsWith('/current'));
   await hidden();await page.screenshot({path:path.join(evidence,f.name+'-expired.png')});
   await page.reload();await page.getByText('Onboarding actions are unavailable',{exact:true}).waitFor();a.equal(calls.length,beforeExpiry+4);a.equal(calls.at(-1).status,410);await hidden();
   a.ok(calls.every(x=>x.method==='GET'));a.equal(calls.filter(x=>x.path.includes('/result')).length,0);a.deepEqual(errors,[]);
   a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
   a.ok(expectedHttp.length<=2,'Only the two explicitly checked expired HTTP responses may produce expected browser resource diagnostics');
   checks.push({name:f.name,width,theme,passed:true,calls,errors,expectedHttp,expired410:2,initialPending:f.pending,idleExpiryVerified:!f.pending,reloadVerified:true,browserWrites:0});
   console.log(`PASS: PENTAONBOARDINGEXPIRY ${f.name};${calls.length} actual GETs;expired410 twice;no resubmission`);
  }catch(error){await page.screenshot({path:path.join(evidence,'failed-'+f.name+'.png')}).catch(()=>{});fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({name:f.name,calls,errors,error:error.message},null,2));throw error;}finally{await context.close();}
 }
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({run,checks,sqlBacked:true,syntheticApiResponses:false,interpreterConfigured:false,modelInference:false,browserWrites:0,testControllerExpirations:4,physicalDevices:'NOT RUN'},null,2));
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
}
main().catch(e=>{console.error('SQL expiry browser failed: '+e.name+': '+e.message);process.exitCode=1;server.close();});
