// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'assessment-create-feedback-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
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

    for (const width of [320,1440]) for (const theme of ['light','dark']) for (const existing of [true,false]) for (const scenario of ['normal','refresh-failure','rejected','uncertain-committed']) {
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
      page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
      await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-feedback-token');},theme);
      const assessments=existing?[{id:'assessment',batchId:'batch',title:'Existing assessment',type:'Performance',maxScore:100,isPublished:true}]:[];
      const options={batches:[{id:'batch',name:'Synthetic batch'}],students:[{id:'student',firstName:'Synthetic',lastName:'Learner'}],enrollments:[{studentId:'student',batchId:'batch',status:'Active'}],gradingSchemes:[{id:'scheme',name:'Synthetic scheme',passingPercent:50}]};
      await context.route('**/*',async route=>{
        const req=route.request(),url=new URL(req.url()),method=req.method();
        if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
          const body=req.postData()?JSON.parse(req.postData()):undefined;calls.push({path:url.pathname,method,body});const headers={'Access-Control-Allow-Origin':origin};
          if(method==='POST'){
            assert.equal(url.pathname,'/api/academies/owned/assessments');if(scenario!=='rejected')assessments.push({...body,id:'created'});
            return route.fulfill({status:scenario==='rejected'?400:scenario==='uncertain-committed'?500:201,headers,contentType:'application/json',body:JSON.stringify({message:'Synthetic validation refusal.'})});
          }
          assert.equal(method,'GET');
          if(scenario==='refresh-failure'&&url.pathname.startsWith('/api/academies/owned/assessments')&&!url.pathname.endsWith('/results')&&calls.some(c=>c.method==='POST'))return route.fulfill({status:503,headers,body:'{}'});
          const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","AcademicGovernance"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],'/api/academies/owned/assessments/options':options,'/api/academies/owned/assessments':assessments,'/api/academies/owned/assessments/assessment/results':[],'/api/academies/owned/assessments/created/results':[]};
          assert.ok(Object.hasOwn(data,url.pathname),url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
        }
        if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
      });
      await page.goto(origin+'/assessments');await page.waitForFunction(()=>{const button=document.querySelector('form button:not([type="button"])');return button&&!button.disabled;});
      const form=page.locator('form');await page.getByPlaceholder('Assessment title',{exact:true}).fill(' Draft assessment ');await form.locator('input[type="number"]').fill('125.5');
      await form.locator('.standard-select-trigger').nth(1).click();await page.getByRole('option',{name:'Recital',exact:true}).click();
      await form.locator('.standard-select-trigger').nth(2).click();await page.getByRole('option',{name:'Synthetic scheme · pass 50%',exact:true}).click();
      await form.locator('.standard-date-trigger').click();await page.getByLabel('Select year').selectOption('2026');await page.getByLabel('Select month').selectOption('9');await page.locator('.standard-date-days button[data-current="true"]').filter({hasText:/^20$/}).click();
      if(existing){await page.getByLabel('Assessment score',{exact:true}).fill('80');await page.getByPlaceholder('Remarks',{exact:true}).fill('Unrelated result draft');}
      const time=await form.locator('input[name="scheduled-time"]').inputValue();
      await page.getByRole('button',{name:'Create assessment',exact:true}).click();
      const notice=page.getByRole('status'),failed=['rejected','uncertain-committed'].includes(scenario);
      await notice.filter({hasText:failed?scenario==='rejected'?'Synthetic validation refusal.':'could not be confirmed':'Assessment created.'}).waitFor();await page.waitForFunction(()=>!document.querySelector('fieldset').disabled);
      assert.equal(await page.getByPlaceholder('Assessment title',{exact:true}).inputValue(),failed?' Draft assessment ':'');
      assert.equal(await form.locator('input[name="scheduled-date"]').inputValue(),failed?'2026-10-20':'');
      assert.equal(await form.locator('input[name="grading-scheme"]').inputValue(),failed?'scheme':'');
      assert.equal(await form.locator('input[name="assessment-type"]').inputValue(),'Recital');
      assert.equal(await form.locator('input[name="batch"]').inputValue(),'batch');
      assert.equal(await form.locator('input[type="number"]').inputValue(),'125.5');
      assert.equal(await form.locator('input[name="scheduled-time"]').inputValue(),time);
      if(existing){assert.equal(await page.getByLabel('Assessment score',{exact:true}).inputValue(),'80');assert.equal(await page.getByPlaceholder('Remarks',{exact:true}).inputValue(),'Unrelated result draft');assert.equal(await page.locator('input[name="assessment"]').inputValue(),'assessment');}
      const writes=calls.filter(c=>c.method==='POST');assert.equal(writes.length,1);
      const utc=await page.evaluate(time=>new Date('2026-10-20T'+time+':00').toISOString(),time);
      assert.deepEqual(writes[0].body,{batchId:'batch',title:' Draft assessment ',type:'Recital',maxScore:125.5,gradingSchemeId:'scheme',scheduledAtUtc:utc,isPublished:true});
      if(scenario==='refresh-failure')assert.match(await notice.textContent(),/could not be refreshed.*Do not repeat/);
      if(!existing&&scenario==='normal')await page.locator('input[name="assessment"]').evaluate(el=>{if(el.value!=='created')throw Error('Created assessment not selected');});
      assert.equal(await notice.getAttribute('aria-live'),'polite');await notice.scrollIntoViewIfNeeded();assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
      const status=scenario==='rejected'?'400':scenario==='refresh-failure'?'503':scenario==='uncertain-committed'?'500':null;assert.deepEqual(errors.filter(x=>!status||!x.startsWith('Failed to load resource:')||!x.includes(status)),[]);
      await page.screenshot({path:path.join(evidence,[width,theme,existing?'existing':'empty',scenario].join('-')+'.png'),fullPage:true});checks.push({width,theme,existing,scenario,calls,notice:await notice.textContent()});console.log('PASS '+[width,theme,existing?'existing':'empty',scenario].join(' '));await context.close();
    }
    assert.equal(checks.length,32);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API only; no live SQL/domain/auth acceptance',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
  }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
  finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
