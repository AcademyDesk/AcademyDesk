'use strict';
// Real product/SQL reads; separate secret-controlled QA completion, no model.
const a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const{chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const[api,run]=process.argv.slice(2);a.match(api||'',/^http:\/\/127\.0\.0\.1:\d+$/);a.match(run||'',/^[0-9a-f]{32}$/);
const web=path.resolve(__dirname,'../../apps/web/out'),evidence=path.resolve(__dirname,'../EVIDENCE','penta-onboarding-pending-live-'+run);fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.json':'application/json','.woff2':'font/woff2','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));a.ok(file.startsWith(web+path.sep));if(!path.extname(file))file+='.html';const segment=/^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');file=fs.realpathSync(file);a.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
async function main(){let input='';for await(const chunk of process.stdin){input+=chunk;a.ok(input.length<65536);}const fixture=JSON.parse(input);input='';a.equal(fixture.fixtures.length,2);a.match(fixture.controllerSecret,/^[0-9a-f]{64}$/);
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin=`http://127.0.0.1:${server.address().port}`;
  const browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'}),checks=[];
  try{for(const f of fixture.fixtures){for(const key of['conversation','draft','requestId'])a.match(f[key],/^[0-9a-f-]{36}$/);const width=f.name==='light-mobile'?320:1440;
    const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
    try{await context.addInitScript(({token,theme})=>{localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);localStorage.setItem('academydesk.theme',theme);},{token:fixture.token,theme:width===320?'light':'dark'});
      page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
      await context.route('**/*',async route=>{const request=route.request(),url=new URL(request.url());if(url.pathname.startsWith('/api/')){
        a.equal(request.method(),'GET');a.ok(['localhost','127.0.0.1'].includes(url.hostname));const response=await route.fetch({url:api+url.pathname,maxRedirects:0,maxRetries:0});a.notEqual(response.status(),429);calls.push({path:url.pathname,method:request.method(),status:response.status()});
        return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Accept'}});
      }if(url.origin===origin||url.protocol==='data:')return route.continue();throw Error('Unexpected external request');});
      await page.goto(`${origin}/penta/onboarding?conversationId=${f.conversation}&draftId=${f.draft}`);
      await page.getByText('Onboarding actions are unavailable',{exact:true}).waitFor();a.equal(calls.length,2);
      a.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);a.ok(!(await page.locator('body').innerText()).includes('Synthetic Pending'));
      const check=page.getByRole('button',{name:'Check pending request',exact:true});await check.click();
      await page.getByRole('status').filter({hasText:'The request is still pending.'}).waitFor();a.equal(calls.length,4);
      a.equal(calls[3].status,202);a.ok(calls[3].path.endsWith(`/claims/${f.requestId}/result`));
      a.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);a.ok(!(await page.locator('body').innerText()).includes('Synthetic Pending'));
      await page.screenshot({path:path.join(evidence,f.name+'-pending.png')});
      // Node test controller, NOT page JS/storage/API interception. Completes the
      // already-claimed fixture via real protected API after real202 was observed.
      const completed=await fetch(api+'/qa/complete/'+f.conversation,{method:'POST',redirect:'error',headers:{'X-QA-Control':fixture.controllerSecret}});a.equal(completed.status,204);
      await check.click();const canvas=page.getByRole('button',{name:/Work canvas/});await canvas.waitFor();a.equal(calls.length,11);
      a.equal(calls[5].status,200);a.equal(calls[5].path,calls[3].path);
      a.ok(calls.every(x=>x.method==='GET'));a.equal(calls.filter(x=>x.path.endsWith('/result')).length,2);
      a.equal(await page.getByRole('textbox',{name:'Ask PENTA',exact:true}).isDisabled(),true);
      a.equal(await page.getByRole('button',{name:'Send',exact:true}).isDisabled(),true);
      a.equal(await page.getByRole('button',{name:'Review prepared operation',exact:true}).count(),0);
      a.equal(await page.getByRole('button',{name:'Create approved student',exact:true}).count(),0);
      a.equal(await page.getByText('Student created',{exact:true}).count(),0);
      await canvas.click();const dialog=page.getByRole('dialog',{name:'Work canvas',exact:true});
      const first=dialog.getByRole('textbox',{name:'Student First Name (required)',exact:true});a.equal(await first.inputValue(),'Synthetic Pending');a.equal(await first.isDisabled(),true);
      await dialog.getByRole('heading',{name:'Draft · revision 2',exact:true}).waitFor();a.equal(await dialog.getByRole('button',{name:'Confirm exact operation',exact:true}).count(),0);
      await page.getByRole('button',{name:'Close Work canvas',exact:true}).click();
      a.equal(await page.getByRole('link',{name:'Manual workspace',exact:true}).getAttribute('href'),'/dashboard');a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);a.deepEqual(errors,[]);
      await page.screenshot({path:path.join(evidence,f.name+'-completed.png')});checks.push({name:f.name,width,passed:true,calls,errors,observedPending202:true,completedSameRequest:true,browserWrites:0});console.log(`PASS: PENTAONBOARDINGPENDING browser ${f.name};${calls.length} actual host GETs;202 then200 same request`);
    }catch(error){await page.screenshot({path:path.join(evidence,'failed-'+f.name+'.png')}).catch(()=>{});fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({name:f.name,calls,errors,error:error.message},null,2));throw error;}finally{await context.close();}}
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({run,checks,sqlBacked:true,syntheticApiResponses:false,interpreterConfigured:false,modelInference:false,browserWrites:0,testControllerCompletions:2,physicalDevices:'NOT RUN'},null,2));
  }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}}
main().catch(e=>{console.error('SQL pending browser failed: '+e.name+': '+e.message);process.exitCode=1;server.close();});
