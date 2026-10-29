'use strict';
// Real export/Identity/SQL. Interpretation is explicitly scripted QA, NOT Mini.
const a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const{chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const[api,run]=process.argv.slice(2);a.match(api||'',/^http:\/\/127\.0\.0\.1:\d+$/);a.match(run||'',/^[0-9a-f]{32}$/);
const web=path.resolve(__dirname,'../../apps/web/out'),evidence=path.resolve(__dirname,'../EVIDENCE','penta-onboarding-dialogue-live-'+run);fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.json':'application/json','.woff2':'font/woff2','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));a.ok(file.startsWith(web+path.sep));if(!path.extname(file))file+='.html';
  const segment=/^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');file=fs.realpathSync(file);a.ok(file.startsWith(web+path.sep));
  res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
async function main(){let input='';for await(const chunk of process.stdin){input+=chunk;a.ok(input.length<65536);}const fixture=JSON.parse(input);input='';
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin=`http://127.0.0.1:${server.address().port}`;
  const browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'}),checks=[];
  try{for(const name of['light-mobile','lost-response','unmarked-runtime']){const width=name==='light-mobile'?320:name==='lost-response'?1440:390;
    const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];let lost=false;
    try{await context.addInitScript(({token,theme})=>{localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);localStorage.setItem('academydesk.theme',theme);},{token:fixture.token,theme:name==='light-mobile'?'light':'dark'});
      page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'&&!(name==='lost-response'&&/net::ERR_FAILED/.test(m.text())))errors.push(m.text());});
      await context.route('**/*',async route=>{const request=route.request(),url=new URL(request.url());if(url.pathname.startsWith('/api/')){
        a.ok(['localhost','127.0.0.1'].includes(url.hostname));const response=await route.fetch({url:api+(name==='unmarked-runtime'?'/offline':'')+url.pathname,maxRedirects:0,maxRetries:0});a.notEqual(response.status(),429);
        const id=url.pathname.endsWith('/browser-turns')?JSON.parse(request.postData()).requestId:undefined;
        calls.push({path:url.pathname,method:request.method(),status:response.status(),...(id?{requestId:id}:{})});
        if(name==='lost-response'&&url.pathname.endsWith('/browser-turns')&&!lost){lost=true;a.equal(response.status(),201);return route.abort('failed');}
        return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Accept,Content-Type'}});
      }if(url.origin===origin||url.protocol==='data:')return route.continue();throw Error('Unexpected external request');});
      await page.goto(origin+'/penta/onboarding');const start=page.getByRole('button',{name:'Start onboarding conversation',exact:true});await start.waitFor();a.equal(calls.length,2);a.ok(calls.every(x=>x.method==='GET'));await start.click();
      const availability=page.getByRole('button',{name:'Check conversational availability',exact:true});await availability.waitFor();a.equal(calls.length,10);
      const composer=page.getByRole('textbox',{name:'Ask PENTA',exact:true});a.equal(await composer.isDisabled(),true);await availability.click();
      if(name==='unmarked-runtime'){
        await page.getByText('Conversational sending is unavailable',{exact:true}).waitFor();a.equal(await composer.isDisabled(),true);a.equal(calls.filter(x=>x.path.endsWith('/browser-turns')).length,0);
      }else{
        await page.getByText('Disposable test interpreter — not qualified AI',{exact:true}).waitFor();
        const beforeChoice=calls.length;await page.getByRole('button',{name:'Executor',exact:true}).click();
        await page.getByRole('button',{name:/^Executor\s*Disposable testing interpreter only/}).click();
        a.equal(calls.length,beforeChoice);await composer.fill('SYNTHETIC provide first name');await page.getByRole('button',{name:'Send',exact:true}).click();
        if(name==='lost-response'){
          await page.getByText('Conversation outcome is unconfirmed',{exact:true}).waitFor();a.equal(lost,true);a.equal(await composer.count(),0);
          await page.getByRole('button',{name:'Check message result',exact:true}).click();
        }
        await page.getByText(/^Please provide:/).waitFor();a.equal(await page.getByRole('button',{name:'Create approved student',exact:true}).count(),0);
        if(name==='lost-response')a.equal(calls.filter(x=>/\/claims\/[0-9a-f-]{36}\/result$/.test(x.path)).length,1);
        a.equal(calls.filter(x=>/\/(challenge|confirm|execute)$/.test(x.path)).length,0);
        await composer.fill('SYNTHETIC complete student details');await page.getByRole('button',{name:'Send',exact:true}).click();
        const review=page.getByRole('button',{name:'Review prepared operation',exact:true});await review.waitFor();a.equal(await composer.isDisabled(),true);
        a.equal(await page.getByRole('region',{name:'Conversation',exact:true}).getByText('SYNTHETIC provide first name',{exact:true}).count(),1);
        const turns=calls.filter(x=>x.path.endsWith('/browser-turns'));a.equal(turns.length,2);a.ok(turns.every(x=>x.status===201));a.equal(new Set(turns.map(x=>x.requestId)).size,2);
        if(name==='light-mobile'){
          await review.click();await page.getByText('Review before approval',{exact:true}).waitFor();await page.getByRole('button',{name:/Work canvas/}).click();
          const canvas=page.getByRole('dialog',{name:'Work canvas',exact:true});const confirm=canvas.getByRole('button',{name:'Confirm exact operation',exact:true});await confirm.scrollIntoViewIfNeeded();await confirm.click();
          await page.getByText('Approved, not yet confirmed as created',{exact:true}).waitFor();await page.getByRole('button',{name:'Create approved student',exact:true}).click();
          await page.getByText('Student created',{exact:true}).waitFor();a.match(await page.getByRole('link',{name:'Open Student 360',exact:true}).getAttribute('href'),/^\/student-management\?studentId=[0-9a-f-]{36}$/);
          for(const action of['challenge','confirm','execute'])a.equal(calls.filter(x=>x.path.endsWith('/core/'+action)).length,1);
          a.equal(await page.getByRole('region',{name:'Conversation',exact:true}).getByText('SYNTHETIC provide first name',{exact:true}).count(),1);
        }else a.equal(calls.filter(x=>/\/(challenge|confirm|execute)$/.test(x.path)).length,0);
      }
      a.equal(await page.getByRole('link',{name:'Manual workspace',exact:true}).getAttribute('href'),'/dashboard');a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);a.deepEqual(errors,[]);
      await page.screenshot({path:path.join(evidence,name+'.png')});checks.push({name,width,passed:true,calls,errors,lostResponseAfterCompletion:lost});console.log(`PASS: PENTAONBOARDINGDIALOGUE browser ${name};${calls.length} actual HTTP requests`);
    }catch(error){await page.screenshot({path:path.join(evidence,'failed-'+name+'.png')}).catch(()=>{});fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({name,calls,errors,error:error.message},null,2));throw error;}finally{await context.close();}}
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({run,checks,sqlBacked:true,syntheticApiResponses:false,syntheticInterpreter:true,realModelInference:false,realModelQualified:false,physicalDevices:'NOT RUN'},null,2));
  }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}}
main().catch(e=>{console.error('SQL dialogue browser failed: '+e.name+': '+e.message);process.exitCode=1;server.close();});
