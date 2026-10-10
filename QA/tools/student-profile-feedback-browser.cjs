// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'student-profile-feedback-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => { try {
  assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html';
  const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file)); if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
  file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
  if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
} catch { res.writeHead(404).end(); } });
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });

    const profile={studentNumber:'SYN-001',preferredName:'Synthetic',gender:'Prefer not to say',dateOfBirth:'2010-01-02',admissionDate:'2026-10-01',addressLine1:'Synthetic address',city:'Synthetic city',state:'Synthetic state',postalCode:'000000',emergencyContactName:'Synthetic contact',emergencyContactPhone:'0000000000',medicalOrAccessibilityNotes:'Synthetic accessibility note',adminNotes:'Synthetic internal note'};
    const scenarios=['success','blank','400','403','409','500','network','malformed','scope-write','scope-load','initial-failure'];
    for(const width of[320,1440])for(const theme of['light','dark'])for(const scenario of scenarios){
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),writes=[],errors=[];
      let release,started;const gate=new Promise(r=>release=r),called=new Promise(r=>started=r);
      page.on('pageerror',e=>errors.push(e.message));
      await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-feedback-token');},theme);
      const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],
        '/api/academies/owned/students':[{id:'student',firstName:'Synthetic',lastName:'Learner',isActive:true},{id:'other',firstName:'Other',lastName:'Learner',isActive:true},{id:'third',firstName:'Third',lastName:'Learner',isActive:true}]};
      await context.route('**/*',async route=>{
        const req=route.request(),url=new URL(req.url()),headers={'Access-Control-Allow-Origin':origin};
        if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
          if(url.pathname.endsWith('/profile')){
            if(req.method()==='PUT'){
              writes.push({path:url.pathname,body:JSON.parse(req.postData())});started();await gate;
              if(scenario==='network')return route.abort('failed');
              return route.fulfill({status:['400','403','409','500'].includes(scenario)?Number(scenario):200,headers,contentType:'application/json',body:JSON.stringify(scenario==='malformed'?{}:JSON.parse(req.postData()))});
            }
            assert.equal(req.method(),'GET');
            if(scenario==='scope-load'&&url.pathname.includes('/other/')){started();await gate;}
            if(scenario==='initial-failure')return route.fulfill({status:503,headers,contentType:'application/json',body:'{}'});
            return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(scenario==='blank'?{}:{...profile,preferredName:url.pathname.includes('/third/')?'Third':url.pathname.includes('/other/')?'Other':'Synthetic'})});
          }
          if(url.pathname.endsWith('/fee-arrangements'))return route.fulfill({status:200,headers,contentType:'application/json',body:'[]'});
          assert.equal(req.method(),'GET');assert.ok(Object.hasOwn(data,url.pathname),url.pathname);
          return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
        }
        if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
      });
      await page.goto(origin+'/student-profile');
      const form=page.locator('.student-admin-profile-editor'),name=form.getByPlaceholder('Preferred name'),save=form.getByRole('button',{name:'Save profile',exact:true});
      async function switchStudent(id,label){
        const native=page.locator('select[name="student-record"]');
        if(await native.count())await native.selectOption(id);
        else{await page.locator('.student-360-toolbar').getByRole('button').first().click();await page.getByRole('option',{name:label,exact:true}).click();}
      }
      if(scenario==='initial-failure'){
        await page.getByRole('status').filter({hasText:'Student records could not be loaded'}).waitFor();assert.equal(await form.count(),0);assert.equal(writes.length,0);
      }else{
        await name.waitFor();
        if(scenario==='scope-load'){
          await switchStudent('other','Other Learner');await called;assert.equal(await form.count(),0);
          await switchStudent('third','Third Learner');await name.waitFor();await name.fill('Third draft');release();await page.waitForTimeout(150);
          assert.equal(await name.inputValue(),'Third draft');assert.equal(await form.getByRole('status').count(),0);
        }else{
          await name.fill('Retained draft');await save.click();await called;
          assert.equal(await name.isDisabled(),true);assert.equal(await form.getByPlaceholder('Internal admin notes — never shown in portals').isDisabled(),true);
          await form.locator('header button').evaluate(e=>{e.click();e.click();});assert.equal(writes.length,1);
          if(scenario==='scope-write'){
            await switchStudent('other','Other Learner');await name.waitFor();await name.fill('Other draft');release();await page.waitForTimeout(150);
            assert.equal(await name.inputValue(),'Other draft');assert.equal(await form.getByRole('status').count(),0);
          }else{
            release();const expected=['500','network'].includes(scenario)?'could not be confirmed':['400','403','409'].includes(scenario)?'could not be saved':scenario==='malformed'?'do not repeat':'Administrative profile saved.';
            await form.getByRole('status').filter({hasText:expected}).waitFor();await page.waitForFunction(()=>!document.querySelector('.student-admin-profile-editor fieldset').disabled);
            assert.equal(await name.inputValue(),'Retained draft');assert.equal(await save.isDisabled(),false);assert.equal(await form.getByRole('status').getAttribute('aria-live'),'polite');
          }
          assert.equal(writes.length,1);assert.equal(writes[0].path,'/api/academies/owned/students/student/profile');
          assert.deepEqual(writes[0].body,scenario==='blank'?Object.fromEntries(Object.keys(profile).map(k=>[k,k==='preferredName'?'Retained draft':['dateOfBirth','admissionDate'].includes(k)?null:''])):{...profile,preferredName:'Retained draft'});
        }
      }
      assert.deepEqual(errors,[]);
      if(['success','403','scope-write'].includes(scenario))await page.screenshot({path:path.join(evidence,[width,theme,scenario].join('-')+'.png'),fullPage:true});
      checks.push({width,theme,scenario,writes});console.log('PASS '+[width,theme,scenario].join(' '));release();await context.close();
    }
    assert.equal(checks.length,44);
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'Synthetic API; not live SQL/Identity/device acceptance'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
  }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
  finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
