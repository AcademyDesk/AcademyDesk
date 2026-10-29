'use strict';
// Actual Student portal export, synthetic reads/media only; no SQL/permission proof.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),a=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),deferred=()=>{let resolve;const promise=new Promise(r=>{resolve=r;});return{promise,resolve};};
async function run(){
 a.equal(process.argv.length,3);const ui=process.argv[2],verified=require('./material-scope-ui-export.cjs').verify(ui),run=crypto.randomUUID().replaceAll('-',''),evidence=path.join(root,'QA/EVIDENCE/material-scope-browser-'+run);
 fs.mkdirSync(evidence,{recursive:true});const result={run,ui,webBuild:verified.build,verifiedExportFiles:627,passed:false,apiResponseMocks:true,syntheticToken:true,actualSql:false,serverAuthorizationTested:false,physicalDevicesTested:false,productionApproved:false,cases:[]},save=()=>fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));save();
 const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));const file=require('./api-image-payment-browser.cjs').staticFile(verified.web,new URL(req.url,'http://localhost').pathname);res.writeHead(200,{'content-type':({'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.svg':'image/svg+xml','.woff2':'font/woff2','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,media='/api/class-media/11111111-1111-4111-8111-111111111111/content',png=Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a0gAAAABJRU5ErkJggg==','base64');let browser;
 try{
  browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of[320,390,768,1440])for(const theme of['light','dark']){
   const context=await browser.newContext({viewport:{width,height:900},reducedMotion:'reduce'}),page=await context.newPage(),errors=[],writes=[],calls=[],held=deferred(),headStarted=deferred();let hold=false,phase='load';
   try{
    await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.Portal','synthetic-material-scope-token');const made=[],revoked=[],create=URL.createObjectURL.bind(URL),revoke=URL.revokeObjectURL.bind(URL);window.__materialScope={made,revoked};URL.createObjectURL=blob=>{const url=create(blob);made.push(url);return url;};URL.revokeObjectURL=url=>{revoked.push(url);return revoke(url);};},theme);
    const student={name:'Synthetic Learner',firstName:'Synthetic',lastName:'Learner',schedule:[],assignments:[],attendanceSummary:{total:0,present:0,late:0},practiceSummary:{logCount:0,totalMinutes:0},practiceLogs:[],music:[],resources:[{title:'Synthetic private image',type:'Attachment',url:media}],assessmentResults:[],certificates:[],invoices:[],classHistory:[],cycleProgress:[]};
    const data={'/api/auth/session':{displayName:'Synthetic Learner',roles:['Student']},'/api/portal/me':{role:'Student',displayName:'Synthetic Learner',studentId:'student'},'/api/portal/students/student':student,'/api/portal/students/student/leave-requests':[],'/api/portal/notifications':[],'/api/portal/announcements':[]};
    page.on('pageerror',()=>errors.push('uncaught material portal error'));
    await context.route('**/*',async route=>{
     const req=route.request(),u=new URL(req.url());
     if(u.pathname.startsWith('/api/')){
      if(!['localhost','127.0.0.1'].includes(u.hostname))return route.abort();
      if(!['GET','HEAD'].includes(req.method())){writes.push(req.method());return route.abort();}
      if(u.pathname===media){calls.push(req.method());if(req.method()==='HEAD'&&hold){hold=false;headStarted.resolve();await held.promise;}
       return route.fulfill({status:200,headers:{'access-control-allow-origin':origin,'access-control-expose-headers':'Content-Length,Content-Disposition','content-type':'image/png','content-length':String(png.length),'content-disposition':'attachment; filename="synthetic.png"'},body:req.method()==='HEAD'?undefined:png}).catch(()=>{});}
      return route.fulfill({status:u.pathname.endsWith('/chat/health')?503:200,contentType:'application/json',headers:{'access-control-allow-origin':origin},body:JSON.stringify(data[u.pathname]??[])});
     }
     if(u.origin===origin||['data:','blob:'].includes(u.protocol))return route.continue();errors.push('external request refused');return route.abort();
    });
    await page.goto(origin+'/portal');await page.getByRole('heading',{name:'Good day, Synthetic',exact:true}).waitFor();
    const nav=page.locator(width<=900?'.learner-bottom-nav':'.learner-portal-nav'),select=async label=>{if(width<=900)await nav.locator('summary').click();await nav.getByRole('button',{name:label,exact:true}).click();};
    await select('Library');let actions=page.locator('.private-material-actions');
    phase='preview and explicit close';await actions.getByRole('button',{name:'Preview',exact:true}).click();await actions.getByRole('img',{name:'Synthetic private image',exact:true}).waitFor();await page.waitForFunction(()=>document.querySelector('.private-material-image img')?.naturalWidth===1);
    const first=await page.evaluate(()=>window.__materialScope.made[0]);a.match(first,/^blob:/);
    await actions.getByRole('button',{name:'Close preview',exact:true}).click();a.equal(await actions.locator('.private-material-preview').count(),0);a.equal(await page.evaluate(url=>window.__materialScope.revoked.filter(x=>x===url).length,first),1);
    phase='unmount pending read';hold=true;const gets=calls.filter(x=>x==='GET').length;await actions.getByRole('button',{name:'Preview',exact:true}).click();await headStarted.promise;await select('Tasks');
    a.equal(await page.locator('.private-material-actions').count(),0);held.resolve();await select('Library');actions=page.locator('.private-material-actions');a.equal(await actions.locator('.private-material-preview').count(),0);a.equal(await actions.getByRole('status').count(),0);a.equal(await actions.getByRole('button',{name:'Preview',exact:true}).isEnabled(),true);
    phase='fresh preview after old completion';await actions.getByRole('button',{name:'Preview',exact:true}).click();await actions.getByRole('img',{name:'Synthetic private image',exact:true}).waitFor();a.equal(calls.filter(x=>x==='GET').length,gets+1);a.equal(await page.evaluate(()=>window.__materialScope.made.length),2);
    await page.screenshot({path:path.join(evidence,'material-'+width+'-'+theme+'.png'),fullPage:true});
    phase='unmount releases live preview';await select('Tasks');a.deepEqual(await page.evaluate(()=>[...window.__materialScope.revoked].sort()),await page.evaluate(()=>[...window.__materialScope.made].sort()));
    await select('Library');a.equal(await page.locator('.private-material-preview').count(),0);a.deepEqual(errors,[]);a.deepEqual(writes,[]);a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false);
    result.cases.push({width,theme,previewAndClose:true,unmountAbortsPendingRead:true,noLateByteRequest:true,freshPreviewWorks:true,allObjectUrlsReleased:true,noStalePreviewOrNotice:true,noWrites:true,pageErrors:0,noHorizontalOverflow:true});save();
   }catch(error){result.failedCase={width,theme,phase,type:error.name,sourceLines:[...String(error.stack||'').matchAll(/private-material-scope-browser\.cjs:(\d+):(\d+)/g)].map(m=>({line:Number(m[1]),column:Number(m[2])}))};await page.screenshot({path:path.join(evidence,'failed.png'),fullPage:true}).catch(()=>{});throw error;}finally{held.resolve();await context.close();}
  }
  a.equal(result.cases.length,8);require('./material-scope-ui-export.cjs').verify(ui);result.passed=true;
 }catch{result.failure='Synthetic private-material scope browser failed; inspect retained bounded evidence.';process.exitCode=1;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));save();console.log(JSON.stringify({run,passed:result.passed,journeys:result.cases.length,evidence,syntheticReads:true,serverAuthorizationTested:false,productionApproved:false}));}
}
if(require.main===module)run().catch(()=>{console.error('Material scope browser preflight refused; no live API changes.');process.exitCode=1;});
