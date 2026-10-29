// Exported real React login with synthetic native-shaped transport, no SQL/real MFA claim.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=require('./web-candidate-export.cjs').resolveExport(root);
const evidence=path.join(root,'QA/EVIDENCE','login-challenge-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{let browser;const checks=[],failures=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of ['authenticator','recovery','invalid','limited','network','malformed','missing-proof']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),errors=[];let loginCalls=0,profileCalls=0;
   try{
    page.on('pageerror',error=>errors.push(error.message));
    await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-retained-admin');localStorage.setItem('academydesk.refreshToken.AcademyAdmin','synthetic-retained-refresh');},theme);
    await context.route('**/*',route=>{const req=route.request(),url=new URL(req.url());
     if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
      const headers={'Access-Control-Allow-Origin':origin};
      if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:{...headers,'Access-Control-Allow-Headers':'authorization,content-type'}});
      if(url.pathname==='/api/auth/login'){
       loginCalls++;assert.equal(req.method(),'POST');const body=JSON.parse(req.postData());assert.equal(body.email,'synthetic@example.invalid');assert.equal(body.password,'Synthetic!UiOnly748');
       if(loginCalls===1){assert.deepEqual(Object.keys(body).sort(),['email','password']);return route.fulfill({status:401,headers,contentType:'application/problem+json',body:JSON.stringify({detail:'RequiresTwoFactor'})});}
       const proof=scenario==='recovery'?'twoFactorRecoveryCode':'twoFactorCode';assert.equal(body[proof],scenario==='recovery'?'synthetic-recovery':'123456');assert.equal(Object.keys(body).length,3);
       if(scenario==='network')return route.abort('failed');
       const status=scenario==='invalid'?401:scenario==='limited'?429:200;
       const payload=status!==200?{detail:'internal-detail-must-not-render'}:scenario==='malformed'?{accessToken:' ',refreshToken:'bad'}:{accessToken:'synthetic-teacher-access',refreshToken:'synthetic-teacher-refresh'};
       return route.fulfill({status,headers,contentType:'application/json',body:JSON.stringify(payload)});
      }
      assert.equal(url.pathname,'/api/auth/session','No domain/model requests allowed');assert.equal(req.method(),'GET');profileCalls++;assert.equal(req.headers().authorization,'Bearer synthetic-teacher-access');
      return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify({workspace:'Teacher',roles:['Teacher'],isPlatformOwner:false,displayName:'Synthetic teacher',academyId:'synthetic'})});
     }
     return url.origin===origin||url.protocol==='data:'?route.continue():route.abort('blockedbyclient');
    });
    await page.goto(origin+'/login');await page.getByRole('button',{name:'Show password'}).click();await page.getByRole('button',{name:'Hide password'}).click();
    await page.getByLabel('User name',{exact:true}).fill('synthetic@example.invalid');await page.getByLabel('Password',{exact:true}).fill('Synthetic!UiOnly748');await page.getByRole('button',{name:'Sign in',exact:true}).click();
    await page.getByLabel('Authenticator code',{exact:true}).waitFor();assert.equal(loginCalls,1);assert.equal(profileCalls,0);await page.getByRole('alert').filter({hasText:'authenticator'}).waitFor();
    if(scenario==='missing-proof'){
     await page.getByRole('button',{name:'Sign in',exact:true}).click();assert.equal(loginCalls,1);assert.equal(await page.getByLabel('Authenticator code',{exact:true}).evaluate(input=>input.validity.valueMissing),true);
    }else{
     if(scenario==='recovery'){
      await page.getByLabel('Authenticator code',{exact:true}).fill('discard-this');await page.getByRole('button',{name:'Use a recovery code',exact:true}).click();
      assert.equal(await page.getByLabel('Recovery code',{exact:true}).inputValue(),'');await page.getByLabel('Recovery code',{exact:true}).fill('synthetic-recovery');
     }else await page.getByLabel('Authenticator code',{exact:true}).fill('123456');
     if(width===320&&['authenticator','recovery'].includes(scenario))await page.screenshot({path:path.join(evidence,`${scenario}-${theme}.png`),fullPage:true});
     await page.getByRole('button',{name:'Sign in',exact:true}).click();
     if(['authenticator','recovery'].includes(scenario)){await page.waitForURL(origin+'/teacher/ai');await page.getByRole('heading',{name:'Teacher conversation is not available yet',exact:true}).waitFor();assert.ok(profileCalls>=1);}
     else{await page.getByRole('alert').filter({hasText:scenario==='limited'?'Too many sign-in attempts':'Login failed'}).waitFor();assert.equal(new URL(page.url()).pathname,'/login');assert.equal(profileCalls,0);assert.equal(await page.getByLabel('Password',{exact:true}).inputValue(),'Synthetic!UiOnly748');assert.equal(await page.getByLabel('Authenticator code',{exact:true}).inputValue(),'123456');assert.equal(await page.getByRole('button',{name:'Sign in',exact:true}).isEnabled(),true);assert.equal(await page.getByText('internal-detail-must-not-render',{exact:false}).count(),0);}
     assert.equal(loginCalls,2,'No automatic retry');
    }
    const present=await page.evaluate(()=>({admin:localStorage.getItem('academydesk.accessToken.AcademyAdmin')==='synthetic-retained-admin',adminRefresh:localStorage.getItem('academydesk.refreshToken.AcademyAdmin')==='synthetic-retained-refresh',teacher:!!localStorage.getItem('academydesk.accessToken.Teacher'),teacherRefresh:!!localStorage.getItem('academydesk.refreshToken.Teacher')}));
    const success=['authenticator','recovery'].includes(scenario);assert.deepEqual(present,{admin:true,adminRefresh:true,teacher:success,teacherRefresh:success});assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
    checks.push({width,theme,scenario,loginCalls,profileCalls,page_errors:0,no_credential_values_saved:true});
   }catch(error){failures.push({width,theme,scenario,error:error.message});throw error;}finally{await context.close();}
  }
 }finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({at:new Date().toISOString(),scope:'actual exported login, synthetic APIs/credentials; not linked Identity/SQL/device',checks,failures,production_approved:false},null,2));console.log(JSON.stringify({evidence,passed:checks.length,failed:failures.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
