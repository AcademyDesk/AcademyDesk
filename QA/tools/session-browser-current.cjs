// Current built UI / real disposable Identity and SQL. No response mocks or token injection.
const fs=require('node:fs'),p=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto');
const root=p.resolve(__dirname,'../..');
const required=name=>{const value=process.env[name];a.ok(value,`Missing ${name}`);return value;};
function loopback(name){const value=required(name),url=new URL(value);a.equal(url.protocol,'http:');a.equal(url.hostname,'127.0.0.1');a.ok(Number(url.port)>=1024);a.equal(url.origin,value);return value;}
const origin=loopback('QA_SESSION_BROWSER_ORIGIN'),api=loopback('QA_SESSION_BROWSER_API_ORIGIN');a.notEqual(origin,api);
const run=required('QA_SESSION_BROWSER_RUN');a.match(run,/^[a-f0-9]{32}$/);
const out=p.resolve(required('QA_SESSION_BROWSER_OUTPUT')),before=p.resolve(required('QA_SESSION_BROWSER_SQL_LOG'));
const fixture=p.resolve(required('QA_SESSION_BROWSER_FIXTURE_ROOT')),proof=JSON.parse(fs.readFileSync(required('QA_SESSION_BROWSER_PROOF'),'utf8'));
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
a.equal(proof.run,run);a.equal(proof.origin,origin);a.equal(proof.api,api);a.equal(proof.responsesMocked,false);
a.match(proof.sourceSha,/^[a-f0-9]{40}$/);a.equal(proof.sourceSha,require('node:child_process').execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim());
const expected=['src/app/platform/control/page.tsx','src/app/login/page.tsx','src/lib/api.ts'];
for(const rel of expected){a.equal(hash(p.join(root,'apps/web',rel)),proof.sourceHashes[rel]);a.equal(hash(p.join(proof.webSource,rel)),proof.sourceHashes[rel]);}
a.ok(proof.artifacts.length>100);for(const artifact of proof.artifacts){const file=p.resolve(proof.artifactRoot,artifact.path);a.ok(file.startsWith(p.resolve(proof.artifactRoot)+p.sep));a.equal(hash(file),artifact.sha256);}
a.equal(fs.readFileSync(p.join(fixture,'.qa-owner'),'utf8').split(/\r?\n/)[0],run,'Exact fixture ownership required');
const log=fs.readFileSync(before,'utf8');a.ok(log.includes(`SESSIONBROWSER READY run=${run} api=${new URL(api).port} origin=${new URL(origin).port}`));
a.ok(!log.includes('SESSIONBROWSER DISABLE SQL PASS')&&!log.includes('SESSIONBROWSER PASSWORD SQL PASS'),'Fresh fixture required; do not repeat mutations');
a.ok(fs.existsSync(out)&&fs.readdirSync(out).length===0,'New owned empty evidence directory required');
const playwright=require(required('QA_SESSION_BROWSER_PLAYWRIGHT')),executablePath=required('QA_BROWSER_EXECUTABLE');
a.ok(fs.existsSync(executablePath));
const events=[],checks=[],browserErrors=[];
function pass(id,detail){checks.push({id,accepted:true,detail});console.log('SESSIONBROWSER UI PASS '+id+' '+detail);}
async function textSnapshot(page,name){fs.writeFileSync(p.join(out,`session-browser-${name}.txt`),page.url()+'\n'+await page.locator('main').innerText());}
async function storage(page){return page.evaluate(()=>Object.fromEntries(['Platform','AcademyAdmin','Teacher','Portal'].map(w=>[w,{access:Boolean(localStorage.getItem('academydesk.accessToken.'+w)),refresh:Boolean(localStorage.getItem('academydesk.refreshToken.'+w))}])));}
async function login(page,email,password,pathname){
  await page.goto(origin+'/login');
  // A real state change confirms hydration before submit, rather than a pre-hydration GET form.
  await page.getByRole('button',{name:'Show password'}).click();
  await page.getByRole('button',{name:'Hide password'}).click();
  await page.getByLabel('User name',{exact:true}).fill(email);
  await page.getByLabel('Password',{exact:true}).fill(password);
  const native=page.waitForResponse(r=>r.url().startsWith(api+'/api/auth/login')&&r.request().method()==='POST');
  await page.getByRole('button',{name:'Sign in',exact:true}).click();
  a.equal((await native).status(),200);
  await page.waitForURL(u=>u.pathname===pathname);
}
(async()=>{
  let browser;
  try{
    browser=await playwright.chromium.launch({headless:true,chromiumSandbox:true,executablePath});
    const ownerContext=await browser.newContext({viewport:{width:1440,height:1000}});
    const adminContext=await browser.newContext({viewport:{width:1440,height:1000}});
    for(const [name,context] of [['owner',ownerContext],['admin',adminContext]]){
      await context.route('**/*',route=>{
        const url=new URL(route.request().url());
        if(url.origin===origin||url.origin===api||url.protocol==='data:')return route.continue();
        return route.abort('blockedbyclient');
      });
      context.on('page',page=>{
        page.on('response',response=>{const url=new URL(response.url());if(url.origin===api)events.push({context:name,method:response.request().method(),path:url.pathname,status:response.status(),at:new Date().toISOString()});});
        page.on('pageerror',error=>browserErrors.push({context:name,name:error.name,message:error.message.replace(/Synthetic![^\s]+/g,'[redacted]')}));
      });
    }
    const owner=await ownerContext.newPage(),admin=await adminContext.newPage();
    await login(admin,'qa-admin-a@example.invalid','Synthetic!39Ab','/penta');
    a.deepEqual((await storage(admin)).AcademyAdmin,{access:true,refresh:true});
    pass('active-admin-real-login','native200 and PENTA-first route; only synthetic credentials');
    await login(owner,'qa-session-owner@example.invalid','Synthetic!39Ab','/platform/ai');
    a.deepEqual((await storage(owner)).Platform,{access:true,refresh:true});
    await owner.goto(origin+'/platform/control?tab=Admins');
    const row=owner.getByRole('row').filter({hasText:'qa-admin-a@example.invalid'});
    await row.getByRole('button',{name:'Deactivate',exact:true}).waitFor();
    const disable=owner.waitForResponse(r=>new URL(r.url()).pathname.endsWith('/active')&&r.request().method()==='PATCH');
    await row.getByRole('button',{name:'Deactivate',exact:true}).click();
    a.equal((await disable).status(),200);
    await row.getByRole('button',{name:'Activate',exact:true}).waitFor();
    a.ok(await row.getByText('Deactivated',{exact:true}).isVisible());
    await owner.getByText('Synthetic Academy Admin deactivated.',{exact:true}).waitFor();
    await textSnapshot(owner,'disable-success');
    await owner.screenshot({path:p.join(out,'session-browser-disable-success.png'),fullPage:true});
    pass('platform-disable-visible-success','nativePATCH200; status Deactivated; action Activate; success text');
    await owner.reload();
    await row.getByRole('button',{name:'Activate',exact:true}).waitFor();
    a.ok(await row.getByText('Deactivated',{exact:true}).isVisible());
    await textSnapshot(owner,'disable-reloaded');
    pass('platform-disable-readback','deactivated state survives real UI reload');
    const inactiveResponse=admin.waitForResponse(r=>new URL(r.url()).origin===api&&new URL(r.url()).pathname==='/api/auth/session'&&r.status()===403);
    await admin.reload();
    await inactiveResponse;
    await textSnapshot(admin,'inactive-existing-session');
    pass('disabled-existing-admin-browser','pre-existing real Admin session receives native403 after reload');
    await admin.goto(origin+'/login');
    await admin.getByRole('button',{name:'Show password'}).click();
    await admin.getByRole('button',{name:'Hide password'}).click();
    await admin.getByLabel('User name',{exact:true}).fill('qa-admin-a@example.invalid');
    await admin.getByLabel('Password',{exact:true}).fill('Synthetic!39Ab');
    const denied=admin.waitForResponse(r=>r.url().startsWith(api+'/api/auth/login')&&r.request().method()==='POST');
    await admin.getByRole('button',{name:'Sign in',exact:true}).click();
    a.equal((await denied).status(),401);
    await admin.getByText('Login failed. Check the user name and password, then try again.',{exact:true}).waitFor();
    a.equal(new URL(admin.url()).pathname,'/login');
    await textSnapshot(admin,'inactive-login-error');
    pass('disabled-login-visible-error','native401 and stable login error; not routed into portal');

    await owner.goto(origin+'/platform/control?tab=Settings');
    const form=owner.locator('form').filter({has:owner.getByRole('heading',{name:'Change password',exact:true})});
    await form.getByLabel('Current password',{exact:true}).fill('Synthetic!39Ab');
    await form.getByLabel('New password',{exact:true}).fill('Synthetic!Changed48Cd');
    await form.getByLabel('Confirm new password',{exact:true}).fill('Synthetic!Different48Cd');
    const startMismatch=events.filter(x=>x.path==='/api/auth/session/change-password'&&x.method==='POST').length;
    await form.getByRole('button',{name:'Update password',exact:true}).click();
    await owner.getByText('New password and confirmation must match.',{exact:true}).waitFor();
    a.equal(events.filter(x=>x.path==='/api/auth/session/change-password'&&x.method==='POST').length,startMismatch);
    a.ok(await form.getByLabel('Current password',{exact:true}).inputValue());
    pass('password-mismatch-local','visible mismatch; zero native password POST; input retained');
    await form.getByLabel('Current password',{exact:true}).fill('Synthetic!WrongOnly');
    await form.getByLabel('Confirm new password',{exact:true}).fill('Synthetic!Changed48Cd');
    const rejected=owner.waitForResponse(r=>new URL(r.url()).pathname==='/api/auth/session/change-password'&&r.request().method()==='POST');
    await form.getByRole('button',{name:'Update password',exact:true}).click();
    a.equal((await rejected).status(),400);
    await owner.getByText('The current password is incorrect or the new password does not meet the security rules.',{exact:true}).waitFor();
    a.equal(await form.getByLabel('Current password',{exact:true}).inputValue(),'Synthetic!WrongOnly');
    a.equal(await form.getByLabel('New password',{exact:true}).inputValue(),'Synthetic!Changed48Cd');
    a.deepEqual((await storage(owner)).Platform,{access:true,refresh:true});
    await textSnapshot(owner,'password-rejected');
    pass('password-rejected-retains-form-session','native400; visible error; inputs and Platform tokens retained');

    await form.getByLabel('Current password',{exact:true}).fill('Synthetic!39Ab');
    const accepted=owner.waitForResponse(r=>new URL(r.url()).pathname==='/api/auth/session/change-password'&&r.request().method()==='POST');
    await form.getByRole('button',{name:'Update password',exact:true}).click();
    a.equal((await accepted).status(),200);
    await owner.waitForURL(u=>u.pathname==='/login'&&u.searchParams.get('passwordChanged')==='1');
    await owner.getByRole('status').filter({hasText:'Password changed. Sign in with your new password.'}).waitFor();
    a.deepEqual((await storage(owner)).Platform,{access:false,refresh:false});
    a.equal(await owner.getByLabel('Password',{exact:true}).inputValue(),'');
    await textSnapshot(owner,'password-success-login');
    await owner.screenshot({path:p.join(out,'session-browser-password-success-login.png'),fullPage:true});
    pass('password-success-recovery','native200; login replacement with accessible notice; Platform pair removed; password input empty');
    // Use the visible returnTo URL without injected browser session/auth storage.
    await owner.getByRole('button',{name:'Show password'}).click();
    await owner.getByRole('button',{name:'Hide password'}).click();
    await owner.getByLabel('User name',{exact:true}).fill('qa-session-owner@example.invalid');
    await owner.getByLabel('Password',{exact:true}).fill('Synthetic!Changed48Cd');
    const renewed=owner.waitForResponse(r=>r.url().startsWith(api+'/api/auth/login')&&r.request().method()==='POST');
    await owner.getByRole('button',{name:'Sign in',exact:true}).click();
    a.equal((await renewed).status(),200);
    await owner.waitForURL(u=>u.pathname==='/platform/control');
    await owner.getByRole('heading',{name:'Tenant Management',exact:true}).waitFor();
    a.deepEqual((await storage(owner)).Platform,{access:true,refresh:true});
    await textSnapshot(owner,'new-password-return');
    pass('new-password-real-login-return','native200; returnTo Platform control; usable restored Platform pair');
    a.equal(events.filter(event=>event.status===429).length,0,'No HTTP 429 during this acceptance run');
    pass('no-http-rate-limit','No 429 responses in the observed UI journeys');
    a.equal(browserErrors.length,0,'Uncaught browser page errors');
    pass('browser-no-uncaught-errors','no pageerror events during tested journeys; not all console warnings checked');
    fs.writeFileSync(p.join(out,'session-browser-observations.json'),JSON.stringify({at:new Date().toISOString(),run,origin,api,browser:await browser.version(),mode:'owned headless Edge, desktop viewport1440x1000',checks,events,browserErrors,productChanged:false,responsesMocked:false,sessionTokensInjected:false,sourceSha:proof.sourceSha,existingAdminReloadBrowserAcceptance:'PASS: native403 observed after real reload'},null,2));
    console.log('SESSIONBROWSER UI SUMMARY '+JSON.stringify({checks:checks.length,accepted:true,productChanged:false,realBrowser:true}));
  }catch(error){console.error('SESSIONBROWSER UI FAILED '+error.message);process.exitCode=1;
    fs.writeFileSync(p.join(out,'session-browser-incomplete-observations.json'),JSON.stringify({at:new Date().toISOString(),run,checks,events,browserErrors,error:error.message},null,2));
  }finally{if(browser)await browser.close();}
})();
