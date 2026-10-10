// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'admission-fee-feedback-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
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

    const scenarios=['success','blank','zero','400','403','500','network','initial-failure','malformed','scope-load','scope-write','academy-failure','student-malformed'];
    for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of scenarios){
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),writes=[],errors=[];
      let release,started;const gate=new Promise(r=>release=r),called=new Promise(r=>started=r);
      page.on('pageerror',e=>errors.push(e.message));
      await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-feedback-token');},theme);
      const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],
        '/api/academies/owned/students':[{id:'student',firstName:'Synthetic',lastName:'Learner',isActive:true},{id:'other',firstName:'Other',lastName:'Learner',isActive:true}]};
      await context.route('**/*',async route=>{
        const req=route.request(),url=new URL(req.url()),headers={'Access-Control-Allow-Origin':origin};
        if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
          if(url.pathname.endsWith('/admission-fee')){
            if(req.method()==='PUT'){
              writes.push({path:url.pathname,body:JSON.parse(req.postData())});started();await gate;
              if(scenario==='network')return route.abort('failed');
              return route.fulfill({status:['400','403','500'].includes(scenario)?Number(scenario):200,headers,contentType:'application/json',body:'{}'});
            }
            assert.equal(req.method(),'GET');
            if(scenario==='scope-load'&&url.pathname.includes('/student/')){started();await gate;}
            if(scenario==='initial-failure')return route.fulfill({status:503,headers,body:'{}'});
            const details=scenario==='malformed'?null:['blank','zero'].includes(scenario)?{amount:null,dueDate:null}:url.pathname.includes('/other/')?{amount:432.1,dueDate:'2026-12-01'}:{amount:1200.25,dueDate:'2026-11-01'};
            return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(details)});
          }
          if(url.pathname.endsWith('/fee-arrangements'))return route.fulfill({status:200,headers,contentType:'application/json',body:'[]'});
          assert.equal(req.method(),'GET');assert.ok(Object.hasOwn(data,url.pathname),url.pathname);
          if(scenario==='academy-failure'&&url.pathname==='/api/academies')return route.fulfill({status:403,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
          return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(scenario==='student-malformed'&&url.pathname.endsWith('/students')?{}:data[url.pathname])});
        }
        if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
      });
      await page.goto(origin+'/student-fees');
      const form=page.locator('.admission-fee-editor'),amount=form.getByPlaceholder('Optional amount'),save=form.getByRole('button',{name:'Save admission fee',exact:true});
      async function switchStudent(){
        const select=page.locator('select[name="fee-student"]');
        if(await select.count())await select.selectOption('other');
        else{await page.locator('.student-fees-toolbar').getByRole('button').first().click();await page.getByRole('option',{name:'Other Learner',exact:true}).click();}
      }
      if(['academy-failure','student-malformed'].includes(scenario)){
        await page.getByRole('status').filter({hasText:'Student fee details could not be loaded'}).waitFor();assert.equal(await form.count(),0);assert.equal(writes.length,0);
      }else{
        await amount.waitFor();
        if(['initial-failure','malformed'].includes(scenario)){
          await form.getByRole('status').filter({hasText:'could not be loaded'}).waitFor();assert.equal(await amount.isDisabled(),true);assert.equal(await save.isDisabled(),true);
          await form.evaluate(e=>e.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true})));assert.equal(writes.length,0);
        }else if(scenario==='scope-load'){
          await called;assert.equal(await save.isDisabled(),true);await switchStudent();await amount.fill('1500.25');release();await page.waitForTimeout(150);
          assert.equal(await amount.inputValue(),'1500.25');assert.equal(await form.getByRole('status').count(),0);assert.equal(writes.length,0);
        }else{
          await amount.fill(scenario==='blank'?'':scenario==='zero'?'0':'1250.50');
          assert.equal(await amount.getAttribute('min'),'0');assert.equal(await amount.getAttribute('step'),'0.01');
          await save.click();await called;
          await page.waitForFunction(()=>document.querySelector('.admission-fee-editor button').textContent.includes('Saving'));
          assert.equal(await amount.isDisabled(),true);assert.equal(await form.locator('fieldset').getAttribute('disabled'),'');
          await form.evaluate(e=>{e.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));e.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));});assert.equal(writes.length,1);
          if(scenario==='scope-write'){
            await switchStudent();await amount.fill('1500.25');release();await page.waitForTimeout(150);
            assert.equal(await amount.inputValue(),'1500.25');assert.equal(await form.getByRole('status').count(),0);
          }else{
            release();await form.getByRole('status').filter({hasText:['500','network'].includes(scenario)?'could not be confirmed':['400','403'].includes(scenario)?'could not be saved':'Admission fee saved.'}).waitFor();
            assert.equal(await amount.inputValue(),scenario==='blank'?'':scenario==='zero'?'0':'1250.50');await save.waitFor();assert.equal(await save.isDisabled(),false);
            assert.equal(await form.getByRole('status').getAttribute('aria-live'),'polite');
          }
          assert.equal(writes.length,1);assert.equal(writes[0].path,'/api/academies/owned/students/student/fee-arrangements/admission-fee');
          assert.deepEqual(writes[0].body,{amount:scenario==='blank'?null:scenario==='zero'?0:1250.5,dueDate:['blank','zero'].includes(scenario)?null:'2026-11-01'});
        }
      }
      assert.deepEqual(errors,[]);
      if(['success','403','scope-write'].includes(scenario))await page.screenshot({path:path.join(evidence,[width,theme,scenario].join('-')+'.png'),fullPage:true});
      checks.push({width,theme,scenario,writes});console.log('PASS '+[width,theme,scenario].join(' '));release();await context.close();
    }
    assert.equal(checks.length,52);
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'Synthetic API; not live SQL/Identity/device acceptance'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
  }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
  finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
