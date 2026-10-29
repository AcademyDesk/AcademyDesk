'use strict';
// Real server-resolved role entry + Manual fallback with private PENTA disabled.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),a=require('node:assert/strict');
const{chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const{verifyExport,staticFile}=require('./api-image-payment-browser.cjs');
function allowed(method,url){return method==='GET'&&/^\/api\/[A-Za-z0-9_./?=&%-]+$/.test(url)||method==='POST'&&['/api/auth/login?useCookies=false','/api/auth/refresh'].includes(url);}
async function waitForJourneyRead({requests,requestStart,core,pause=ms=>new Promise(resolve=>setTimeout(resolve,ms))}){
 // Headings can render before the authenticated read; wait for real evidence.
 for(let attempt=0;attempt<150;attempt++){
  if(requests.slice(requestStart).some(row=>row.method==='GET'&&row.path===core&&row.status===200))return;
  await pause(100);
 }
 a.fail('Current journey did not complete its authorized core read within15seconds.');
}
function verifyJourney({requests,requestStart,core,expectedTheme,aiTheme,manualTheme,errors}){
 a.ok(Number.isInteger(requestStart)&&requestStart>=0&&requestStart<=requests.length);
 a.ok(['light','dark'].includes(expectedTheme));a.equal(aiTheme,expectedTheme);a.equal(manualTheme,expectedTheme);a.deepEqual(errors,[]);
 const journeyRequests=requests.slice(requestStart);
 a.ok(journeyRequests.some(row=>row.method==='GET'&&row.path===core&&row.status===200),'This journey must consume its own real authorized core read; an earlier viewport/theme cannot qualify it.');
 a.equal(journeyRequests.some(row=>row.status===429||row.status>=500),false);
 return journeyRequests.length;
}
async function verifyEntryBrowser({result,identities,password,exchange,snapshot,pause,evidence,checkpoint=()=>{}}){
 const verified=verifyExport(),facts=result.entryBrowser={passed:false,phase:'launch',exportBuild:verified.build,verifiedExportFiles:verified.verifiedExportFiles,apiResponseMocks:false,tokenInjection:false,modelActivated:false,physicalDevicesTested:false,cases:[]},before=snapshot();
 const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));const file=staticFile(verified.web,new URL(req.url,'http://localhost').pathname);res.writeHead(200,{'Content-Type':({'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.png':'image/png','.ico':'image/x-icon'})[path.extname(file)]||'application/octet-stream'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
 let browser;await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',args:['--disable-dev-shm-usage']});
  for(const [index,identity]of identities.entries()){
   if(index)await pause(61000);const context=await browser.newContext({viewport:{width:1440,height:900}}),page=await context.newPage(),errors=[],requests=[];facts.currentCase={role:identity.role,phase:'visible login',requests,errors};
   try{
    page.on('pageerror',()=>errors.push('uncaught page error'));
    await context.route('**/*',async route=>{
     const request=route.request(),url=new URL(request.url());
     if(url.pathname.startsWith('/api/')){
      if(!['127.0.0.1','localhost'].includes(url.hostname)||!allowed(request.method(),url.pathname+url.search)){errors.push('unapproved request refused');return route.abort();}
      let response;try{response=await exchange(request.method(),url.pathname+url.search,request.postData(),request.headers().authorization);}catch{errors.push('native relay failure');return route.abort();}requests.push({method:request.method(),path:url.pathname,status:response.status});if(response.status===429||response.status>=500)errors.push('rate limit/server failure');
      return route.fulfill({status:response.status,body:response.body,headers:{'content-type':'application/json','access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
     }
     if(url.origin===origin||url.protocol==='data:')return route.continue();errors.push('external request refused');return route.abort();
    });
    await page.goto(origin+'/login');await page.getByLabel('User name',{exact:true}).fill(identity.email);await page.getByLabel('Password',{exact:true}).fill(password);await page.getByRole('button',{name:'Sign in',exact:true}).click();await page.waitForURL(origin+identity.ai);
    for(const width of[390,1440])for(const theme of['light','dark']){
     const requestStart=requests.length;facts.currentCase.width=width;facts.currentCase.theme=theme;facts.currentCase.phase='AI entry';await page.setViewportSize({width,height:900});await page.goto(origin+identity.ai);
     await page.getByRole('button',{name:'Use '+theme+' theme',exact:true}).click();await page.getByRole('button',{name:'Use '+theme+' theme',exact:true,pressed:true}).waitFor();
     if(identity.role==='AcademyAdmin'){
      await page.getByRole('alert').filter({hasText:'PENTA Mini is not enabled here'}).waitFor();a.equal(await page.locator('#penta-prompt').isDisabled(),true);
     }else{await page.getByRole('heading',{name:identity.unavailable,exact:true}).waitFor();await page.getByText('No AI actions enabled for this role',{exact:true}).waitFor();a.equal(await page.locator('textarea').count(),0);}
     const aiTheme=await page.evaluate(()=>document.documentElement.dataset.theme);a.equal(aiTheme,theme);
     a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false);await page.screenshot({path:path.join(evidence,`entry-${identity.role}-${width}-${theme}.png`),fullPage:true});facts.currentCase.phase='Manual fallback';
     if(identity.role==='AcademyAdmin'){await page.getByRole('group',{name:'Work view',exact:true}).getByRole('button',{name:/Workspace.*Manual/}).click();await page.getByRole('heading',{name:'Your academy workspace',exact:true}).waitFor();}
     else{await page.getByRole('navigation',{name:'Work view',exact:true}).getByRole('link',{name:'Manual workspace',exact:true}).click();await page.waitForURL(origin+identity.manual);await page.getByRole('heading',{name:identity.manualHeading,exact:true}).waitFor();}
     await waitForJourneyRead({requests,requestStart,core:identity.core});
     const manualTheme=await page.evaluate(()=>document.documentElement.dataset.theme),journeyRequestCount=verifyJourney({requests,requestStart,core:identity.core,expectedTheme:theme,aiTheme,manualTheme,errors});a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false);
     await page.screenshot({path:path.join(evidence,`manual-${identity.role}-${width}-${theme}.png`),fullPage:true});facts.cases.push({role:identity.role,width,theme,aiTheme,manualTheme,journeyRequestCount,aiLanding:identity.ai,conversationUnavailableTruthful:true,manualUsable:true,realCoreRead:true,noHorizontalOverflow:true,pageErrors:0});checkpoint();
    }
    a.ok(requests.some(row=>row.method==='POST'&&row.path==='/api/auth/login'&&row.status===200));a.ok(requests.some(row=>row.method==='GET'&&row.path==='/api/auth/session'&&row.status===200));facts.authenticatedRoles??=[];facts.authenticatedRoles.push({role:identity.role,requests});console.log(JSON.stringify({phase:'native-role-entry',role:identity.role,journeys:4}));
   }catch(error){facts.errorType=['TimeoutError','AssertionError'].includes(error.name)?error.name:'operation error';throw error;}finally{await context.close();}
  }
  a.equal(facts.cases.length,16);a.deepEqual(snapshot(),before);facts.domainStateUnchanged=true;facts.domainTableHashes=before;delete facts.currentCase;facts.phase='complete';facts.passed=true;
 }finally{if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
}
module.exports={allowed,waitForJourneyRead,verifyJourney,verifyEntryBrowser};
