// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'subject-fee-feedback-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
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

    const scenarios=['success','400','403','500','network','refresh-failure','initial-failure','malformed','scope-change'];
    for(const screen of ['student-fees','student-profile'])for(const width of [320,1440])for(const theme of ['light','dark'])for(const scenario of scenarios){
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),writes=[],errors=[],items=[];
      let release;const gate=new Promise(r=>release=r);
      page.on('pageerror',e=>errors.push(e.message));
      await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-feedback-token');},theme);
      const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],
        '/api/academies/owned/students':[{id:'student',firstName:'Synthetic',lastName:'Learner',isActive:true},{id:'other',firstName:'Other',lastName:'Learner',isActive:true}]};
      await context.route('**/*',async route=>{
        const req=route.request(),url=new URL(req.url()),headers={'Access-Control-Allow-Origin':origin};
        if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
          if(url.pathname.endsWith('/profile')||url.pathname.endsWith('/admission-fee'))return route.fulfill({status:200,headers,contentType:'application/json',body:'{}'});
          if(url.pathname.endsWith('/fee-arrangements')){
            if(req.method()==='POST'){
              writes.push({path:url.pathname,body:JSON.parse(req.postData())});await gate;
              if(scenario==='network')return route.abort('failed');
              if(['400','403','500'].includes(scenario))return route.fulfill({status:Number(scenario),headers,body:'{}'});
              items.push({...writes.at(-1).body,id:'fee',isActive:true});
              return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(items.at(-1))});
            }
            assert.equal(req.method(),'GET');
            if(scenario==='initial-failure'||scenario==='refresh-failure'&&writes.length)return route.fulfill({status:503,headers,body:'{}'});
            return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(scenario==='malformed'?{}:url.pathname.includes('/other/')?[]:items)});
          }
          assert.equal(req.method(),'GET');assert.ok(Object.hasOwn(data,url.pathname),url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
        }
        if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
      });
      await page.goto(origin+'/'+screen);
      const section=page.locator('.student-fee-arrangements'),form=section.locator('form'),amount=form.getByPlaceholder('Amount',{exact:true}),subject=form.getByPlaceholder('Subject, e.g. Piano');
      await amount.waitFor();
      if(['initial-failure','malformed'].includes(scenario)){
        await section.getByRole('status').filter({hasText:'could not be loaded'}).waitFor();
        assert.equal(await amount.isDisabled(),true);assert.equal(await form.getByRole('button',{name:'Add',exact:true}).isDisabled(),true);assert.equal(writes.length,0);
      }else{
        await subject.fill('Synthetic Piano');await amount.fill('1250.50');
        await form.getByRole('button',{name:'Add',exact:true}).click();
        await page.waitForFunction(()=>document.querySelector('.student-fee-add-button').textContent.includes('Adding'));
        assert.equal(await amount.isDisabled(),true);assert.equal(await subject.isDisabled(),true);
        // Native dispatch bypasses disabled controls to test the handler's own synchronous guard.
        await form.evaluate(e=>{e.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));e.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));});
        assert.equal(writes.length,1);
        if(scenario==='scope-change'){
          const select=page.locator('select[name="'+(screen==='student-fees'?'fee-student':'student-record')+'"]');
          if(await select.count())await select.selectOption('other');
          else{
            const toolbar=page.locator(screen==='student-fees'?'.student-fees-toolbar':'.student-360-toolbar');
            await toolbar.getByRole('button').first().click();await page.getByRole('option',{name:'Other Learner',exact:true}).click();
          }
          await page.waitForFunction(()=>document.querySelector('.student-fee-add-button').textContent.trim()==='Add'&&!document.querySelector('.student-fee-add-button').disabled);
          await subject.fill('Other draft');await amount.fill('1500.25');release();
          await page.waitForTimeout(150);
          assert.equal(await subject.inputValue(),'Other draft');assert.equal(await amount.inputValue(),'1500.25');assert.equal(await section.getByRole('status').count(),0);
          assert.equal(writes[0].path,'/api/academies/owned/students/student/fee-arrangements');
        }else{
          release();const failure=['400','403','500','network'].includes(scenario);
          await section.getByRole('status').filter({hasText:failure?['500','network'].includes(scenario)?'could not be confirmed':'could not be added':'Fee arrangement added.'}).waitFor();
          await page.waitForFunction(()=>!document.querySelector('.student-fee-add-button').disabled);
          assert.equal(await subject.inputValue(),failure?'Synthetic Piano':'');assert.equal(await amount.inputValue(),failure?'1250.50':'');
          if(scenario==='refresh-failure')assert.match(await section.getByRole('status').textContent(),/could not be refreshed.*do not repeat/);
          assert.equal(await section.getByRole('status').getAttribute('aria-live'),'polite');
        }
        assert.equal(writes.length,1);
        assert.deepEqual(writes[0].body,{subjectName:'Synthetic Piano',amount:1250.5,frequency:'Monthly',effectiveFrom:new Date().toISOString().slice(0,10)});
      }
      assert.deepEqual(errors,[]);
      if(['success','refresh-failure','scope-change'].includes(scenario))await page.screenshot({path:path.join(evidence,[screen,width,theme,scenario].join('-')+'.png'),fullPage:true});
      checks.push({screen,width,theme,scenario,writes});console.log('PASS '+[screen,width,theme,scenario].join(' '));release();await context.close();
    }
    assert.equal(checks.length,72);
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'Synthetic API; not live SQL/Identity/device acceptance'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
  }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
  finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
