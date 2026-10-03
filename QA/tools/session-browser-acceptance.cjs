// Owned headless browser exercises real local UI/API. No mocked responses or token injection.
const fs=require('node:fs'),p=require('node:path'),a=require('node:assert/strict'),crypto=require('node:crypto');
const root=p.resolve(__dirname,'../..');
const playwright=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const origin='http://127.0.0.1:49261',api='http://127.0.0.1:49262';
const run='4c94a112b34845099d770bd55b8500b9';
const out=p.join(root,'QA/EVIDENCE');
const before=p.join(root,'QA/EVIDENCE/logs/phase-2b-session-browser-sql.log');
const log=fs.readFileSync(before,'utf8');
const resume=process.argv.includes('--resume');
a.ok(log.includes(`SESSIONBROWSER READY run=${run} api=49262 origin=49261`));
a.ok(!log.includes('SESSIONBROWSER PASSWORD SQL PASS'),'Refuse repeated password mutation');
if(resume)a.ok(log.includes('SESSIONBROWSER DISABLE SQL PASS'),'Resume requires accepted native disable');
else a.ok(!log.includes('SESSIONBROWSER DISABLE SQL PASS'),'Refuse repeated disable mutation');
const marker=fs.readFileSync(p.join(process.env.TEMP,'AcademyDesk-QA',run,'.qa-owner'),'utf8');
a.ok(marker.split(/\r?\n/)[0]===run,'Exact fixture ownership required');
const copy=p.join(root,'.build-check/browser-web-458cfacbfafd4a8782ea70b1dc4fb464');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for(const rel of ['src/app/platform/control/page.tsx','src/app/login/page.tsx','src/lib/api.ts'])a.equal(hash(p.join(root,'apps/web',rel)),hash(p.join(copy,rel)),'Current product source '+rel);
const events=[],checks=[],browserErrors=[];
if(resume){
  const partial=JSON.parse(fs.readFileSync(p.join(out,'session-browser-incomplete-observations.json'),'utf8'));
  a.equal(partial.run,run);
  a.deepEqual(partial.checks.map(x=>x.id),['active-admin-real-login','platform-disable-visible-success','platform-disable-readback']);
  checks.push(...partial.checks.map(x=>({...x,retainedFrom:'initial partial run; observed before rate limit'})));
}
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
    browser=await playwright.chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
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
    if(!resume){
    await login(admin,'qa-admin-a@example.invalid','Synthetic!39Ab','/dashboard');
    a.deepEqual((await storage(admin)).AcademyAdmin,{access:true,refresh:true});
    pass('active-admin-real-login','native200 and dashboard route; only synthetic credentials');
    await login(owner,'qa-session-owner@example.invalid','Synthetic!39Ab','/platform');
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
    const inactiveResponse=admin.waitForResponse(r=>new URL(r.url()).origin===api&&r.status()===403);
    await admin.reload();
    await inactiveResponse;
    await textSnapshot(admin,'inactive-existing-session');
    pass('disabled-existing-admin-browser','pre-existing real Admin session receives native403 after reload');
    }
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

    if(resume)await login(owner,'qa-session-owner@example.invalid','Synthetic!39Ab','/platform');
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
    a.equal(browserErrors.length,0,'Uncaught browser page errors');
    pass('browser-no-uncaught-errors','no pageerror events during tested journeys; not all console warnings checked');
    fs.writeFileSync(p.join(out,'session-browser-observations.json'),JSON.stringify({at:new Date().toISOString(),run,origin,api,browser:await browser.version(),mode:'owned headless Edge, desktop viewport1440x1000',checks,events,browserErrors,productChanged:false,resumed:resume,existingAdminReloadBrowserAcceptance:'NOT ACCEPTED: initial request rate-limited; native access403 control accepted separately'},null,2));
    console.log('SESSIONBROWSER UI SUMMARY '+JSON.stringify({checks:checks.length,accepted:true,productChanged:false,realBrowser:true}));
  }catch(error){console.error('SESSIONBROWSER UI FAILED '+error.message);process.exitCode=1;
    fs.writeFileSync(p.join(out,resume?'session-browser-resume-incomplete-observations.json':'session-browser-incomplete-observations.json'),JSON.stringify({at:new Date().toISOString(),run,checks,events,browserErrors,error:error.message},null,2));
  }finally{if(browser)await browser.close();}
})();
