// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'enrollment-lifecycle-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
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

    const scenarios=['Paused','Completed','Waitlisted','Withdrawn','Cancelled','Active','cancel','whitespace','denied','refresh-failure','uncertain'];
    for(const width of[320,1440])for(const theme of['light','dark'])for(const scenario of scenarios){
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
      page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
      await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-feedback-token');},theme);
      const enrollment={id:'enrollment',studentId:'student',batchId:'source',startDate:'2026-10-01',endDate:null,status:scenario==='Active'?'Paused':'Active'};
      const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],
        '/api/academies/owned/students':[{id:'student',firstName:'Synthetic',lastName:'Learner'}],'/api/academies/owned/batches':[{id:'source',name:'Synthetic batch'}],'/api/academies/owned/enrollments':[enrollment]};
      await context.route('**/*',async route=>{
        const req=route.request(),url=new URL(req.url()),method=req.method();
        if(url.pathname.startsWith('/api/')&&['127.0.0.1','localhost'].includes(url.hostname)){
          const body=req.postData()?JSON.parse(req.postData()):undefined;calls.push({path:url.pathname,method,body});
          const headers={'Access-Control-Allow-Origin':origin};
          if(method==='PUT'){
            assert.equal(url.pathname,'/api/academies/owned/enrollments/enrollment');
            if(scenario!=='denied')Object.assign(enrollment,body);
            return route.fulfill({status:scenario==='denied'?403:scenario==='uncertain'?500:200,headers,contentType:'application/json',body:JSON.stringify({message:'Synthetic policy refusal.'})});
          }
          assert.equal(method,'GET');
          if(scenario==='refresh-failure'&&calls.some(c=>c.method==='PUT')&&url.pathname.startsWith('/api/academies/owned/'))return route.fulfill({status:503,headers,body:'{}'});
          assert.ok(Object.hasOwn(data,url.pathname),url.pathname);
          return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
        }
        if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
      });
      await page.goto(origin+'/enrollments?studentId=student');
      await page.waitForFunction(()=>{const s=document.querySelector('li select');return s&&!s.disabled;});
      const create=page.locator('form').first();await create.locator('select').nth(2).selectOption('Waitlisted');
      const target=['Active','Completed','Waitlisted','Withdrawn','Cancelled'].includes(scenario)?scenario:'Paused';
      await page.getByLabel('Lifecycle status',{exact:true}).selectOption(target);
      assert.equal(calls.filter(c=>c.method==='PUT').length,0);
      if(target!=='Active')await page.getByLabel('Lifecycle reason (required)',{exact:true}).fill(scenario==='whitespace'?'   ':'  Synthetic lifecycle explanation  ');
      if(scenario==='cancel'){
        await page.getByRole('button',{name:'Cancel change',exact:true}).click();
        assert.equal(await page.getByLabel('Lifecycle status',{exact:true}).inputValue(),'Active');
        assert.equal(await page.getByRole('button',{name:'Save status',exact:true}).count(),0);
      }else{
        await page.getByRole('button',{name:'Save status',exact:true}).click();
        const expected=scenario==='whitespace'?'reason is required':scenario==='denied'?'Synthetic policy refusal.':scenario==='uncertain'?'could not be confirmed':'Enrolment updated.';
        await page.getByRole('status').filter({hasText:expected}).waitFor();
        await page.waitForFunction(()=>!document.querySelector('fieldset').disabled);
        if(['whitespace','denied','uncertain'].includes(scenario)){
          assert.equal(await page.getByLabel('Lifecycle status',{exact:true}).inputValue(),target);
          assert.equal(await page.getByLabel('Lifecycle reason (required)',{exact:true}).inputValue(),scenario==='whitespace'?'   ':'  Synthetic lifecycle explanation  ');
        }else{
          assert.equal(await page.getByRole('button',{name:'Save status',exact:true}).count(),0);
          if(scenario==='refresh-failure')assert.match(await page.getByRole('status').textContent(),/could not be refreshed.*Do not repeat/);
          else assert.equal(await page.getByLabel('Lifecycle status',{exact:true}).inputValue(),target);
        }
      }
      const writes=calls.filter(c=>c.method==='PUT');assert.equal(writes.length,['cancel','whitespace'].includes(scenario)?0:1);
      if(writes.length)assert.deepEqual(writes[0].body,{status:target,endDate:null,lifecycleReason:target==='Active'?null:'Synthetic lifecycle explanation'});
      assert.equal(await create.locator('select').nth(2).inputValue(),'Waitlisted');
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
      const status=scenario==='denied'?'403':scenario==='uncertain'?'500':scenario==='refresh-failure'?'503':null;
      assert.deepEqual(errors.filter(x=>!status||!x.startsWith('Failed to load resource:')||!x.includes(status)),[]);
      if(scenario!=='cancel')assert.equal(await page.getByRole('status').getAttribute('aria-live'),'polite');
      await page.screenshot({path:path.join(evidence,[width,theme,scenario].join('-')+'.png'),fullPage:true});
      checks.push({width,theme,scenario,calls});console.log('PASS '+[width,theme,scenario].join(' '));await context.close();
    }
    assert.equal(checks.length,44);
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'Synthetic API only; live SQL/Identity/tenant/device acceptance NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
  }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
  finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
