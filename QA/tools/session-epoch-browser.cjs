'use strict';
// Actual exported pages, actual login handler and native same-origin StorageEvents.
// API/credentials are synthetic: not server Identity/revocation/SQL/device proof.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),a=require('node:assert/strict'),crypto=require('node:crypto');
const ts=require('../../apps/web/node_modules/typescript');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const source=path.join(root,'apps/web/src/lib/api.ts'),frame=path.join(root,'apps/web/src/components/workspace-frame.tsx'),login=path.join(root,'apps/web/src/app/login/page.tsx');
const helper='const process={env:{}};\n'+ts.transpileModule(fs.readFileSync(source,'utf8'),{compilerOptions:{module:ts.ModuleKind.ES2022,target:ts.ScriptTarget.ES2022}}).outputText;
const evidence=path.join(root,'QA/EVIDENCE','session-epoch-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.woff2':'font/woff2','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 a.ok(['GET','HEAD'].includes(req.method));const pathname=new URL(req.url,'http://localhost').pathname;
 if(pathname==='/qa/session-api.js'){res.writeHead(200,{'Content-Type':'application/javascript','Cache-Control':'no-store'});return res.end(helper);}
 let file=path.resolve(web,'.'+decodeURIComponent(pathname));a.ok(file.startsWith(web+path.sep));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);a.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
const routes={AcademyAdmin:'/reports',Teacher:'/teacher/ai',Portal:'/portal/ai',Platform:'/platform/ai'};
(async()=>{let browser;const checks=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const[workspace,route]of Object.entries(routes))for(const[width,theme]of [[320,'light'],[1440,'dark']])for(const scenario of ['login-peer','refresh-peer','pending-identical']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),errors=[],calls=[];let releaseRefresh,refreshObserved;
   const hold=new Promise(resolve=>releaseRefresh=resolve),observed=new Promise(resolve=>refreshObserved=resolve);
   try{
    await page.addInitScript(({workspace,theme})=>{localStorage.setItem('academydesk.theme',theme);for(const w of ['AcademyAdmin','Teacher','Portal','Platform']){
     localStorage.setItem('academydesk.accessToken.'+w,'synthetic-'+w);localStorage.setItem('academydesk.refreshToken.'+w,'synthetic-refresh-'+w);localStorage.setItem('academydesk.sessionEpoch.'+w,'synthetic-original-'+w);
    }}, {workspace,theme});
    const watch=p=>{p.on('pageerror',error=>errors.push(error.message));p.on('console',message=>{if(message.type()==='error'&&!/^Failed to load resource: the server responded with a status of 401 \(/.test(message.text()))errors.push(message.text());});};watch(page);
    await context.route('**/*',async intercepted=>{const request=intercepted.request(),url=new URL(request.url());
     if(url.pathname.startsWith('/api/')){
      a.ok(['localhost','127.0.0.1'].includes(url.hostname));calls.push({path:url.pathname,method:request.method(),page:request.frame().page()===page?'old':'new'});
      const headers={'Access-Control-Allow-Origin':origin,'Access-Control-Allow-Headers':'Authorization,Content-Type','Access-Control-Allow-Methods':'GET,POST,OPTIONS'};
      if(request.method()==='OPTIONS')return intercepted.fulfill({status:204,headers});
      let body,status=200;
      if(url.pathname==='/api/qa/session-epoch'){status=request.headers().authorization==='Bearer synthetic-renewed'?200:401;body={};}
      else if(url.pathname==='/api/auth/refresh'){refreshObserved();if(scenario==='pending-identical')await hold;body={accessToken:'synthetic-renewed',refreshToken:'synthetic-renewed-refresh'};}
      else if(url.pathname==='/api/auth/login')body={accessToken:'synthetic-'+workspace,refreshToken:'synthetic-refresh-'+workspace};
      else if(url.pathname==='/api/auth/session')body={workspace,academyId:'synthetic',displayName:'Synthetic account',roles:[workspace==='Portal'?'Student':workspace==='Platform'?'PlatformOwner':workspace],isPlatformOwner:workspace==='Platform'};
      else if(url.pathname==='/api/academies')body=[{id:'synthetic',name:'Synthetic academy'}];
      else if(url.pathname==='/api/portal/announcements')body=[];
      else if(workspace==='AcademyAdmin'&&/^\/api\/academies\/synthetic\/(students|teachers|batches|enrollments|sessions|invoices|payments|expenses|payroll\/payouts)$/.test(url.pathname))body=[];
      a.notEqual(body,undefined,'Unexpected API '+url.pathname);return intercepted.fulfill({status,headers,contentType:'application/json',body:JSON.stringify(body)});
     }
     if(url.origin===origin||['data:','blob:'].includes(url.protocol))return intercepted.continue();throw Error('External request forbidden');
    });
    await page.goto(origin+route);if(workspace==='AcademyAdmin')await page.locator('.workspace-reports-export-card').first().waitFor();
    else await page.getByRole('heading',{name:/conversation is not available yet/}).waitFor();await page.waitForLoadState('networkidle');
    const before=calls.length,oldEpoch=await page.evaluate(w=>localStorage.getItem('academydesk.sessionEpoch.'+w),workspace);
    if(scenario!=='login-peer'){
     await page.evaluate(async()=>{const api=await import('/qa/session-api.js');window.qaRequest=api.academyApi('/api/qa/session-epoch').then(response=>response.status);});
     await observed;
    }
    if(scenario==='refresh-peer'){
     a.equal(await page.evaluate(()=>window.qaRequest),200);await page.evaluate(()=>window.dispatchEvent(new Event('focus')));
     a.equal(await page.getByRole('heading',{name:'Session ended',exact:true}).count(),0);
     a.equal(await page.evaluate(w=>localStorage.getItem('academydesk.sessionEpoch.'+w),workspace),oldEpoch);
    }else{
     const sourcePage=await context.newPage();watch(sourcePage);await sourcePage.goto(origin+'/login?returnTo='+encodeURIComponent(route));
     await sourcePage.getByLabel('User name',{exact:true}).fill('synthetic-session@example.invalid');await sourcePage.getByLabel('Password',{exact:true}).fill('synthetic-password');
     await sourcePage.getByRole('button',{name:'Sign in',exact:true}).click();await sourcePage.waitForURL(origin+route);await sourcePage.waitForLoadState('networkidle');
     await page.getByRole('heading',{name:'Session ended',exact:true}).waitFor();a.ok(await page.getByRole('link',{name:'Sign in again',exact:true}).isVisible());
     a.notEqual(await page.evaluate(w=>localStorage.getItem('academydesk.sessionEpoch.'+w),workspace),oldEpoch);
     if(scenario==='pending-identical'){releaseRefresh();a.equal(await page.evaluate(()=>window.qaRequest),401);
      a.equal(calls.filter(x=>x.path==='/api/qa/session-epoch').length,1,'No replay after replacement');}
     a.equal(await page.evaluate(w=>localStorage.getItem('academydesk.accessToken.'+w),workspace),'synthetic-'+workspace);
     a.equal(calls.slice(before).filter(x=>x.page==='old'&&!['/api/qa/session-epoch','/api/auth/refresh'].includes(x.path)).length,0,'Old view starts no replacement reads');
     if(scenario==='login-peer')await page.screenshot({path:path.join(evidence,workspace+'-'+theme+'.png')});
     a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
    }
    const other=await page.evaluate(w=>['AcademyAdmin','Teacher','Portal','Platform'].filter(x=>x!==w).every(x=>localStorage.getItem('academydesk.accessToken.'+x)==='synthetic-'+x&&localStorage.getItem('academydesk.sessionEpoch.'+x)==='synthetic-original-'+x),workspace);a.equal(other,true);
    a.deepEqual(errors,[]);checks.push({workspace,width,theme,scenario,passed:true,calls,errors,credentialValuesRecorded:false});
   }catch(error){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({workspace,width,theme,scenario,error:error.message,errors,calls},null,2));await page.screenshot({path:path.join(evidence,'failed.png')}).catch(()=>{});throw error;}finally{releaseRefresh();await context.close();}
  }
 }finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,passed:checks.length,sourcePins:Object.fromEntries([source,frame,login].map(file=>[path.relative(root,file),crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')])),scope:'exported React/login plus native same-origin storage; synthetic API/credentials, not Identity/SQL/device/server revocation',productionApproved:false},null,2));console.log(JSON.stringify({evidence,passed:checks.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
