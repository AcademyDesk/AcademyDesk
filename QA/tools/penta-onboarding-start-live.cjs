'use strict';
// Real exported product + real host/Identity/SQL, injected interpreter never called.
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const{chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const[api,run]=process.argv.slice(2);assert.match(api||'',/^http:\/\/127\.0\.0\.1:\d+$/);assert.match(run||'',/^[0-9a-f]{32}$/);
const root=path.resolve(__dirname,'../..');
let web=path.join(root,'apps/web/out');
if(process.env.QA_ONBOARDING_START_EXPORT){
  const build=process.env.QA_ONBOARDING_START_EXPORT;assert.match(build,/^[0-9a-f]{32}$/);
  const proof=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/default-web-build-'+build+'/result.json')));
  assert.equal(proof.passed,true);assert.equal(proof.downloaded,false);assert.equal(proof.sourceUnchanged,true);
  assert.deepEqual(require('./default-web-build.cjs').sourcePins(path.join(root,'apps/web')),proof.sourcePins);
  web=path.join(root,'.build-check/default-web-build',build,'app/out');assert.equal(fs.realpathSync(web).toLowerCase(),web.toLowerCase());
}
const evidence=path.resolve(__dirname,'../EVIDENCE','penta-onboarding-start-live-'+run);fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.json':'application/json','.woff2':'font/woff2','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));assert.ok(file.startsWith(web+path.sep));if(!path.extname(file))file+='.html';
  const segment=/^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));
  res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
async function main(){let input='';for await(const chunk of process.stdin){input+=chunk;assert.ok(input.length<65536);}const fixture=JSON.parse(input);input='';
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin=`http://127.0.0.1:${server.address().port}`;
  const browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'}),checks=[];
  try{for(const name of['light-mobile','dark-desktop','lost-response','offline','teacher']){const width=name==='dark-desktop'?1440:name==='light-mobile'?320:390;
    const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];let lost=false;
    try{await context.addInitScript(({token,theme})=>{localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);localStorage.setItem('academydesk.theme',theme);},{token:name==='teacher'?fixture.teacher:fixture.token,theme:name==='light-mobile'?'light':'dark'});
      page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'&&!(name==='lost-response'&&/net::ERR_FAILED/.test(m.text())))errors.push(m.text());});
      await context.route('**/*',async route=>{const request=route.request(),url=new URL(request.url());if(url.pathname.startsWith('/api/')){
        assert.ok(['localhost','127.0.0.1'].includes(url.hostname));const response=await route.fetch({url:api+(name==='offline'?'/offline':'')+url.pathname,maxRedirects:0,maxRetries:0});assert.notEqual(response.status(),429);
        calls.push({path:url.pathname,method:request.method(),status:response.status()});
        if(name==='lost-response'&&request.method()==='POST'){assert.equal(lost,false);lost=true;assert.equal(response.status(),201);return route.abort('failed');}
        return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Accept,Content-Type'}});
      }if(url.origin===origin||url.protocol==='data:')return route.continue();throw Error('Unexpected external request');});
      await page.goto(origin+'/penta/onboarding');
      if(['offline','teacher'].includes(name)){
        await page.getByText('Protected start is unavailable',{exact:true}).waitFor();assert.equal(await page.getByRole('button',{name:'Start onboarding conversation',exact:true}).count(),0);assert.ok(calls.every(x=>x.method==='GET'));assert.equal(calls.length,name==='teacher'?1:2);
      }else{
        const button=page.getByRole('button',{name:'Start onboarding conversation',exact:true});await button.waitFor();assert.equal(calls.length,2);assert.ok(calls.every(x=>x.method==='GET'));await button.click();
        if(name==='lost-response'){
          await page.getByText('Conversation start is unconfirmed',{exact:true}).waitFor();assert.equal(lost,true);
          const recoveryUrl=page.url(),requestId=new URL(recoveryUrl).searchParams.get('startRequestId');assert.match(requestId,/^[0-9a-f-]{36}$/);
          await page.reload();await page.getByRole('button',{name:'Check conversation start',exact:true}).waitFor();
          assert.equal(page.url(),recoveryUrl);assert.equal(await page.getByRole('button',{name:'Start onboarding conversation',exact:true}).count(),0);
          assert.equal(calls.filter(x=>x.method==='POST').length,1);assert.equal(calls.filter(x=>x.path.includes('/starts/')).length,0);
          await page.screenshot({path:path.join(evidence,'lost-response-after-reload.png')});
          // A different signed-in actor cannot recover the URL's lookup key. The
          // same response comes from the real host, never a browser fixture body.
          const denied=await context.request.get(api+`/api/academies/${fixture.academy}/penta/onboarding/starts/${requestId}`,{headers:{Authorization:'Bearer '+fixture.teacher},maxRedirects:0});
          assert.equal(denied.status(),403);calls.push({path:'foreign-actor-start-recovery',method:'GET',status:denied.status()});
          await page.getByRole('button',{name:'Check conversation start',exact:true}).click();
        }
        await page.getByRole('button',{name:/Work canvas/}).waitFor();assert.match(page.url(),/conversationId=[0-9a-f-]{36}&draftId=[0-9a-f-]{36}$/);
        assert.equal(calls.filter(x=>x.method==='POST').length,1);assert.equal(calls.find(x=>x.method==='POST').status,201);
        assert.equal(await page.getByRole('button',{name:'Send',exact:true}).isDisabled(),true);assert.equal(await page.getByRole('button',{name:'Review prepared operation',exact:true}).count(),0);
        if(name==='lost-response')assert.equal(calls.filter(x=>x.path.includes('/starts/')&&x.method==='GET').length,1);
      }
      assert.equal(await page.getByRole('link',{name:'Manual workspace',exact:true}).getAttribute('href'),'/dashboard');assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);assert.deepEqual(errors,[]);
      await page.screenshot({path:path.join(evidence,name+'.png')});checks.push({name,width,passed:true,calls,errors,lostResponseAfterCommit:lost});console.log(`PASS: PENTAONBOARDINGSTARTBROWSER ${name};${calls.length} actual HTTP requests`);
    }catch(error){await page.screenshot({path:path.join(evidence,'failed-'+name+'.png')}).catch(()=>{});fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({name,calls,errors,error:error.message},null,2));throw error;}finally{await context.close();}}
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({run,checks,sqlBacked:true,syntheticApiResponses:false,syntheticInterpreter:true,interpreterInvoked:false,reloadRecovery:true,foreignActorRecoveryDenied:true,webBuild:process.env.QA_ONBOARDING_START_EXPORT||null,physicalDevices:'NOT RUN'},null,2));
  }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}}
main().catch(e=>{console.error('SQL start browser failed: '+e.name+': '+e.message);process.exitCode=1;server.close();});
