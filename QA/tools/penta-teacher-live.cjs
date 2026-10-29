'use strict';
// Actual exported UI + host/Identity/SQL via a run-owned loopback bridge.
const a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const{chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const[api,run]=process.argv.slice(2),build=process.env.QA_PENTA_TEACHER_BROWSER_EXPORT;a.match(api||'',/^http:\/\/127\.0\.0\.1:\d+$/);a.match(run||'',/^[a-f0-9]{32}$/);a.match(build||'',/^[a-f0-9]{32}$/);
const root=path.resolve(__dirname,'../..'),proof=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/default-web-build-'+build+'/result.json')));a.equal(proof.passed,true);a.equal(proof.downloaded,false);
a.deepEqual(require('./default-web-build.cjs').sourcePins(path.join(root,'apps/web')),proof.sourcePins);
const web=path.join(root,'.build-check/default-web-build',build,'app/out'),evidence=path.join(root,'QA/EVIDENCE/penta-teacher-live-'+run);fs.mkdirSync(evidence,{recursive:true});a.equal(fs.realpathSync(web).toLowerCase(),web.toLowerCase());
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.json':'application/json','.woff2':'font/woff2','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));a.ok(file.startsWith(web+path.sep));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');file=fs.realpathSync(file);a.ok(file.startsWith(web+path.sep));
 res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
async function main(){let input='';for await(const chunk of process.stdin){input+=chunk;a.ok(input.length<65536);}const f=JSON.parse(input);input='';
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin=`http://127.0.0.1:${server.address().port}`;
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'}),checks=[];
 try{for(const name of['light-mobile','dark-desktop','lost-response','cancelled','teacher','unavailable']){
  const width=name==='light-mobile'?320:name==='dark-desktop'?1440:name==='unavailable'?768:390;
  const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];let lost=false;
  try{
   await context.addInitScript(({token,theme})=>{localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);localStorage.setItem('academydesk.theme',theme);},{token:name==='teacher'?f.teacher:f.token,theme:name==='light-mobile'?'light':'dark'});
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'&&!/Failed to load resource.*(?:403|404)|net::ERR_FAILED/.test(m.text()))errors.push(m.text());});
   await context.route('**/*',async route=>{const request=route.request(),url=new URL(request.url());if(url.pathname.startsWith('/api/')){
    a.ok(['localhost','127.0.0.1'].includes(url.hostname));const response=await route.fetch({url:api+(name==='unavailable'?'/offline':'')+url.pathname,maxRedirects:0,maxRetries:0});a.notEqual(response.status(),429);calls.push({path:url.pathname,method:request.method(),status:response.status()});
    if(name==='lost-response'&&url.pathname.endsWith('/execute')){a.equal(lost,false);lost=true;a.equal(response.status(),201);return route.abort('failed');}
    return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
   }if(url.origin===origin||url.protocol==='data:')return route.continue();throw Error('Unexpected external request');});
   const draft=f.drafts[['teacher','unavailable'].includes(name)?'light-mobile':name];
   await page.goto(`${origin}/penta/teachers?conversationId=${f.session}&draftId=${draft}`);
   if(name==='teacher'||name==='unavailable'){
    await page.getByText(name==='teacher'?'Current academy access could not be verified':'Protected teacher review is unavailable',{exact:true}).waitFor();
    a.equal(await page.getByText('BrowserSynthetic',{exact:true}).count(),0);a.equal(await page.getByRole('button',{name:'Create approved teacher',exact:true}).count(),0);a.ok(calls.every(x=>x.method==='GET'));
   }else{
    await page.getByRole('button',{name:'Review teacher draft',exact:true}).waitFor();a.ok(calls.every(x=>x.method==='GET'));a.equal(calls.length,3);
    if(name==='cancelled'){
     await page.getByRole('button',{name:'Cancel this draft',exact:true}).click();await page.getByText('Teacher draft cancelled',{exact:true}).waitFor();a.equal(calls.filter(x=>x.method==='POST').length,1);
    }else{
     await page.getByRole('button',{name:'Review teacher draft',exact:true}).click();await page.getByRole('button',{name:'Approve exact details',exact:true}).click();
     const dialog=page.getByRole('dialog',{name:'Approve this exact teacher draft'});await dialog.waitFor();a.equal(calls.filter(x=>x.path.endsWith('/approve')).length,0);
     if(name==='light-mobile')await page.screenshot({path:path.join(evidence,'light-mobile-approval.png')});
     await dialog.getByRole('button',{name:'Approve this teacher draft',exact:true}).click();await page.getByRole('button',{name:'Create approved teacher',exact:true}).waitFor();
     a.equal(calls.filter(x=>x.path.endsWith('/execute')).length,0);await page.getByRole('button',{name:'Create approved teacher',exact:true}).click();
     if(name==='lost-response'){
      await page.getByText('Teacher outcome is unconfirmed',{exact:true}).waitFor();await page.screenshot({path:path.join(evidence,'lost-response-pending.png')});
      a.equal(await page.getByText('BrowserSynthetic',{exact:true}).count(),0);await page.getByRole('button',{name:'Check saved teacher result',exact:true}).click();
     }
     await page.getByText('Teacher created',{exact:true}).waitFor();a.equal(calls.filter(x=>x.path.endsWith('/execute')).length,1);
     const link=await page.getByRole('link',{name:'Open teacher profile'}).getAttribute('href');a.match(link,/^\/teacher-profile\?teacherId=[a-f0-9-]{36}$/);
     await page.reload();await page.getByText('Teacher created',{exact:true}).waitFor();a.equal(calls.filter(x=>x.path.endsWith('/execute')).length,1);
     a.equal(await page.getByRole('button',{name:'Create approved teacher',exact:true}).count(),0);
    }
   }
   a.equal(await page.getByRole('link',{name:'Manual workspace',exact:true}).getAttribute('href'),'/teacher-onboarding');
   a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);a.deepEqual(errors,[]);
   await page.screenshot({path:path.join(evidence,name+'.png')});checks.push({name,width,passed:true,calls,errors,lostResponseAfterCommit:lost});console.log(`PASS: PENTATEACHERBROWSER ${name};${calls.length} actual HTTP requests`);
  }catch(error){await page.screenshot({path:path.join(evidence,'failed-'+name+'.png')}).catch(()=>{});fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({name,calls,errors,error:error.message},null,2));throw error;}finally{await context.close();}
 }
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({passed:true,run,webBuild:build,checks,sqlBacked:true,syntheticApiResponses:false,modelInference:false,physicalDevices:'NOT RUN',productionApproved:false},null,2));
 }finally{await browser.close();await new Promise(r=>server.close(r));}
}
main().catch(e=>{console.error('Teacher native browser failed: '+e.name+': '+e.message);process.exitCode=1;server.close();});
