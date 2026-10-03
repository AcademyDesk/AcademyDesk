const fs=require('node:fs'),p=require('node:path'),a=require('node:assert/strict'),c=require('node:crypto');
const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=p.resolve(__dirname,'../..'),out=p.join(root,'QA/EVIDENCE'),origin='http://127.0.0.1:49421',api='http://127.0.0.1:49422',run='f197585f3eb94984903b10f1b76b03f9';
const host=fs.readFileSync(p.join(out,'logs/phase-2b-session-recovery-sql.log'),'utf8');a.ok(host.includes(`SESSIONBROWSER READY run=${run} api=49422 origin=49421`));a.ok(!host.includes('DISABLE SQL PASS'));
a.equal(fs.readFileSync(p.join(process.env.TEMP,'AcademyDesk-QA',run,'.qa-owner'),'utf8').split(/\r?\n/)[0],run);
const hash=f=>c.createHash('sha256').update(fs.readFileSync(f)).digest('hex');a.equal(hash(p.join(root,'apps/web/src/app/dashboard/page.tsx')),hash(p.join(root,'.build-check/browser-web-907ec13ff9c847cfab8ac65a2dea010f/src/app/dashboard/page.tsx')));
const checks=[],events=[],errors=[];let controlled429=false;const mocked=[];
const pass=(id,native)=>{checks.push({id,native});console.log('SESSIONRECOVERY PASS '+id+' native='+native);};
const storage=page=>page.evaluate(()=>Object.fromEntries(['AcademyAdmin','Teacher','Platform','Portal'].map(w=>[w,{access:!!localStorage.getItem('academydesk.accessToken.'+w),refresh:!!localStorage.getItem('academydesk.refreshToken.'+w)}])));
async function snap(page,id){fs.writeFileSync(p.join(out,'session-recovery-'+id+'.txt'),page.url()+'\n'+await page.locator('main').innerText());await page.screenshot({path:p.join(out,'session-recovery-'+id+'.png'),fullPage:true});}
async function login(page,email,password,path){await page.goto(origin+'/login');await page.getByRole('button',{name:'Show password'}).click();await page.getByRole('button',{name:'Hide password'}).click();await page.getByLabel('User name',{exact:true}).fill(email);await page.getByLabel('Password',{exact:true}).fill(password);const response=page.waitForResponse(r=>r.url().startsWith(api+'/api/auth/login')&&r.request().method()==='POST');await page.getByRole('button',{name:'Sign in',exact:true}).click();a.equal((await response).status(),200);await page.waitForURL(u=>u.pathname===path);}
(async()=>{let browser;try{
 browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 const userContext=await browser.newContext({viewport:{width:1440,height:1000}}),ownerContext=await browser.newContext({viewport:{width:1440,height:1000}});
 for(const [label,context]of [['user',userContext],['owner',ownerContext]]){
  await context.route('**/*',route=>{const u=new URL(route.request().url());if(u.origin!==origin&&u.origin!==api&&u.protocol!=='data:')return route.abort();if(label==='user'&&controlled429&&u.origin===api&&u.pathname==='/api/academies'){mocked.push({path:u.pathname,status:429});return route.fulfill({status:429,contentType:'application/json',body:'{"message":"Synthetic controlled rate limit"}',headers:{'Access-Control-Allow-Origin':origin}});}return route.continue();});
  context.on('page',page=>{page.on('pageerror',e=>errors.push({label,name:e.name,message:e.message}));page.on('response',r=>{const u=new URL(r.url());if(u.origin===api)events.push({label,path:u.pathname,method:r.request().method(),status:r.status(),controlled429:label==='user'&&controlled429&&u.pathname==='/api/academies'});});});
 }
 const user=await userContext.newPage(),owner=await ownerContext.newPage();
 await login(user,'qa-teacher-a@example.invalid','Synthetic!39Ab','/teacher');
 await login(user,'qa-admin-b@example.invalid','Synthetic!39Ab','/dashboard');
 await user.getByRole('region',{name:'Academy operating indicators'}).waitFor();
 const initial=await storage(user);a.deepEqual(initial.Teacher,{access:true,refresh:true});a.deepEqual(initial.AcademyAdmin,{access:true,refresh:true});pass('active-admin-dashboard-control',true);
 await login(owner,'qa-session-owner@example.invalid','Synthetic!39Ab','/platform');
 // Wait for the login router's document load before starting the next navigation.
 await owner.waitForLoadState('load');
 await owner.getByRole('heading',{name:'Overview',exact:true}).waitFor();
 await owner.locator('a[href="/platform/control?tab=Admins"]').first().click();
 await owner.waitForURL(u=>u.pathname==='/platform/control'&&u.searchParams.get('tab')==='Admins');
 const rowB=owner.getByRole('row').filter({hasText:'qa-admin-b@example.invalid'});await rowB.getByRole('button',{name:'Reset password'}).waitFor();
 owner.once('dialog',dialog=>dialog.accept('Synthetic!Recovery58Cd'));
 const reset=owner.waitForResponse(r=>new URL(r.url()).pathname.endsWith('/reset-password')&&r.request().method()==='POST');await rowB.getByRole('button',{name:'Reset password'}).click();a.equal((await reset).status(),200);await owner.getByText('Administrator password reset.',{exact:true}).waitFor();
 await user.reload();await user.getByRole('alert').filter({hasText:'Your session has ended.'}).waitFor();a.ok(await user.getByRole('link',{name:'Sign in again'}).isVisible());a.equal(await user.locator('[aria-label="Academy operating indicators"]').count(),0);
 const after=await storage(user);a.deepEqual(after.AcademyAdmin,{access:false,refresh:false});a.deepEqual(after.Teacher,initial.Teacher);await snap(user,'revoked-401');pass('revoked-existing-admin-401-signin-other-workspace-preserved',true);
 console.log('SESSIONRECOVERY PACING normal quota unchanged; waiting60 seconds before remaining independent checks');
 await new Promise(r=>setTimeout(r,60000));
 await login(user,'qa-admin-a@example.invalid','Synthetic!39Ab','/dashboard');await user.getByRole('region',{name:'Academy operating indicators'}).waitFor();
 const rowA=owner.getByRole('row').filter({hasText:'qa-admin-a@example.invalid'});
 const disable=owner.waitForResponse(r=>new URL(r.url()).pathname.endsWith('/active')&&r.request().method()==='PATCH');await rowA.getByRole('button',{name:'Deactivate',exact:true}).click();a.equal((await disable).status(),200);await rowA.getByRole('button',{name:'Activate',exact:true}).waitFor();
 await user.reload();await user.getByRole('alert').filter({hasText:'You do not have access to this workspace.'}).waitFor();a.ok(await user.getByRole('link',{name:'Use another account'}).isVisible());a.equal(await user.locator('[aria-label="Academy operating indicators"]').count(),0);a.deepEqual((await storage(user)).AcademyAdmin,{access:true,refresh:true});await snap(user,'disabled-403');pass('disabled-existing-admin-reload-403-clear-guidance-no-false-totals',true);
 // Do not re-enable a disabled user to test temporary network failure. Active AdminB is independent.
 await login(user,'qa-admin-b@example.invalid','Synthetic!Recovery58Cd','/dashboard');await user.getByRole('region',{name:'Academy operating indicators'}).waitFor();const tokens=await storage(user);
 controlled429=true;await user.reload();await user.getByRole('alert').filter({hasText:'Too many requests.'}).waitFor();a.deepEqual(await storage(user),tokens);a.equal(await user.locator('[aria-label="Academy operating indicators"]').count(),0);await snap(user,'controlled-429');pass('controlled429-retry-guidance-preserves-session',false);
 controlled429=false;await user.getByRole('button',{name:'Try again',exact:true}).click();await user.getByRole('region',{name:'Academy operating indicators'}).waitFor();a.deepEqual(await storage(user),tokens);pass('manual-read-retry-native-dashboard-success',true);
 await user.setViewportSize({width:390,height:844});controlled429=true;await user.reload();await user.getByRole('alert').filter({hasText:'Too many requests.'}).waitFor();await snap(user,'controlled-429-mobile');a.ok(await user.getByRole('button',{name:'Try again'}).isVisible());pass('mobile390-controlled-error-recovery-visible',false);
 a.equal(errors.length,0);a.ok(!events.some(x=>x.status===429&&!x.controlled429),'Unexpected real limiter response');a.ok(events.some(x=>x.path==='/api/academies'&&x.status===403));a.ok(events.some(x=>x.path==='/api/auth/refresh'&&x.status===401));
 fs.writeFileSync(p.join(out,'session-recovery-observations.json'),JSON.stringify({at:new Date().toISOString(),run,browser:await browser.version(),checks,events,mocked,errors,productScope:'Dashboard only',nativeRateLimitAcceptance:false},null,2));console.log('SESSIONRECOVERY SUMMARY '+JSON.stringify({checks:checks.length,accepted:true,nativeRateLimitAcceptance:false}));
}catch(e){console.error('SESSIONRECOVERY FAILED '+e.message);if(browser)for(const context of browser.contexts())for(const page of context.pages())await page.screenshot({path:p.join(out,'session-recovery-failure-'+browser.contexts().indexOf(context)+'.png'),fullPage:true}).catch(()=>{});fs.writeFileSync(p.join(out,'session-recovery-incomplete.json'),JSON.stringify({checks,events,mocked,errors,error:e.message},null,2));process.exitCode=1;}finally{if(browser)await browser.close();}})();
