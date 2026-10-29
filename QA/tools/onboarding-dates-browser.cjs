// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'onboarding-dates-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
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

    const cases=[];for(const width of[320,390,768,1440])for(const theme of['light','dark'])for(const kind of['teacher','student'])for(const dates of['blank','valid'])for(const status of[200,400,500])cases.push([width,theme,kind,dates,status]);
    async function runCase(width,theme,kind,dates,status){
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),writes=[],errors=[];
      let releasePost;const postGate=new Promise(resolve=>{releasePost=resolve;});
      page.on('pageerror',e=>errors.push(e.message));
      await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-date-token');},theme);
      const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[]};
      await context.route('**/*',async route=>{
        const req=route.request(),url=new URL(req.url()),headers={'Access-Control-Allow-Origin':origin};
        if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
          if(req.method()==='POST'){
            assert.equal(url.pathname,'/api/academies/owned/'+(kind==='teacher'?'teachers':'student-onboarding'));writes.push(JSON.parse(req.postData()));
            await postGate;
            return route.fulfill({status:kind==='teacher'&&status===200?201:status,headers,contentType:'application/json',body:JSON.stringify(status===400?{message:'Synthetic date rejection'}:status===500?{message:'Synthetic failure'}:{id:'synthetic-created'})});
          }
          return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname]||[])});
        }
        if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
      });
      await page.goto(origin+'/'+kind+'-onboarding');
      await page.locator('input[name="'+(kind==='teacher'?'firstName':'studentFirstName')+'"]').fill('Synthetic');
      const main=page.locator('main.enterprise-settings');
      const {assertManual}=require('./penta-foundation-contract.cjs'); await assertManual(page,theme);
      for(const field of await main.locator('input:not([type="hidden"]), textarea').all()){
        assert.ok(await field.evaluate(el=>Boolean(el.labels?.length||el.getAttribute('aria-label'))),'Every visible native field needs a persistent accessible label');
      }
      assert.equal(await main.locator('input[type="time"]').count(),0,'No browser-native time popup');
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'No horizontal page overflow');
      assert.ok(await main.locator('h1').evaluate(el=>parseFloat(getComputedStyle(el).fontSize)<=30),'Standard heading scale');
      if(kind==='teacher'){
        await page.getByRole('button',{name:'Add subject',exact:true}).click();
        await page.getByRole('textbox',{name:'Subject 2',exact:true}).fill('Violin');
        await page.getByRole('button',{name:'Remove subject 2',exact:true}).click();
        assert.equal(await page.getByRole('textbox',{name:'Subject 2',exact:true}).count(),0);
        if(dates==='valid'){
          await page.locator('input[name="available-Monday"]').check();
          for(const edge of ['from','to']){
            const field=main.locator('.standard-time-field').filter({has:page.locator(`input[name="${edge}-Monday"]`)});
            await field.getByRole('button',{name:`Monday ${edge==='from'?'start':'end'} time`,exact:true}).click();
            const dialog=field.getByRole('dialog');
            const columns=dialog.locator(':scope > div');
            await columns.nth(0).getByRole('button',{name:edge==='from'?'09':'10',exact:true}).click();
            await columns.nth(1).getByRole('button',{name:'37',exact:true}).click();
            await dialog.getByRole('button',{name:'AM',exact:true}).click();
            await dialog.getByRole('button',{name:'Done',exact:true}).click();
            assert.equal(await field.locator('input').inputValue(),edge==='from'?'09:37':'10:37');
            await field.getByRole('button').click(); await page.keyboard.press('Escape');
            assert.equal(await field.getByRole('dialog').count(),0); assert.ok(await field.getByRole('button').evaluate(el=>el===document.activeElement));
          }
        }
      }
      await page.locator('input[name="'+(kind==='teacher'?'lastName':'studentLastName')+'"]').fill('Learner');
      if(kind==='teacher'){await page.getByPlaceholder('Email',{exact:true}).fill('qa@example.invalid');await page.getByRole('textbox',{name:'Subject 1',exact:true}).fill('Piano');}
      async function chooseDate(name){
        const field=page.locator('.standard-date-field').filter({has:page.locator('input[name="'+name+'"]')});
        await field.locator('button.standard-date-trigger').click();const dialog=field.getByRole('dialog');
        await dialog.getByLabel('Select year').selectOption('2000');await dialog.getByLabel('Select month').selectOption('0');
        await dialog.locator('button[data-current="true"]').filter({hasText:/^1$/}).click();
      }
      if(kind==='student')await chooseDate('dateOfBirth');
      if(dates==='valid')for(const name of kind==='teacher'?['dateOfBirth','joiningDate']:['admissionDate'])await chooseDate(name);
      await page.getByRole('button',{name:kind==='teacher'?'Complete teacher onboarding':'Complete onboarding',exact:true}).click();
      await main.locator('form[aria-busy="true"]').waitFor();assert.equal(await main.locator('fieldset').evaluate(el=>el.disabled),true);assert.equal(await main.locator('input:not([type="hidden"])').first().isDisabled(),true);assert.equal(writes.length,1);releasePost();
      if(status===400){await page.getByText('Synthetic date rejection Your details have been retained.',{exact:true}).waitFor();assert.equal(await page.locator('input[name="'+(kind==='teacher'?'firstName':'studentFirstName')+'"]').inputValue(),'Synthetic');}
      else if(status===500){await page.getByText(new RegExp((kind==='teacher'?'Teacher':'Student')+' creation could not be confirmed')).waitFor();assert.equal(await page.locator('input[name="'+(kind==='teacher'?'firstName':'studentFirstName')+'"]').inputValue(),'Synthetic');}
      else if(kind==='teacher'){await page.getByText('Teacher onboarded. Add payment details from the Teacher payment details section.',{exact:true}).waitFor();assert.equal(await page.locator('input[name="firstName"]').inputValue(),'');}
      else await page.waitForURL('**/student-management?studentId=synthetic-created&notice=student-created');
      assert.equal(writes.length,1);
      for(const name of kind==='teacher'?['dateOfBirth','joiningDate']:['admissionDate'])assert.equal(writes[0][name],dates==='blank'?null:'2000-01-01');
      if(kind==='student')assert.equal(writes[0].dateOfBirth,'2000-01-01');
      if(kind==='teacher'){
        assert.deepEqual(JSON.parse(writes[0].availabilityJson),dates==='valid'?[{day:'Monday',available:true,from:'09:37',to:'10:37'}]:[]);
        const times=main.locator('.standard-time-field input[name="from-Monday"]');assert.equal(await times.inputValue(),status===200?'':dates==='valid'?'09:37':'');
      }
      assert.deepEqual(errors,[]);checks.push({width,theme,kind,dates,status});
      if(dates==='blank'&&kind==='teacher')await page.screenshot({path:path.join(evidence,[width,theme,kind,status].join('-')+'.png'),fullPage:true});
      console.log('PASS '+[width,theme,kind,dates,status].join(' '));await context.close();
    }
    let nextCase=0;await Promise.all(Array.from({length:3},async()=>{while(nextCase<cases.length){const current=cases[nextCase++];await runCase(...current);}}));
    assert.equal(checks.length,96);
    const sources=Object.fromEntries(['apps/web/src/app/student-onboarding/page.tsx','apps/web/src/app/teacher-onboarding/page.tsx','apps/web/src/components/design-system/onboarding-field.tsx','apps/web/src/components/design-system/onboarding.module.css','apps/web/src/components/standard-select-field.tsx','apps/web/src/components/standard-time-field.tsx'].map(file=>[file,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex')]));
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,sources,transport:'Synthetic API, shared date/time controls; not browser-to-SQL/device proof'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
  }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
  finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
