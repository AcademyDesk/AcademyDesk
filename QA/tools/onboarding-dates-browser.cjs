// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
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

    for(const width of[320,1440])for(const theme of['light','dark'])for(const kind of['teacher','student'])for(const dates of['blank','valid'])for(const status of[200,400]){
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),writes=[],errors=[];
      page.on('pageerror',e=>errors.push(e.message));
      await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-date-token');},theme);
      const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[]};
      await context.route('**/*',async route=>{
        const req=route.request(),url=new URL(req.url()),headers={'Access-Control-Allow-Origin':origin};
        if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
          if(req.method()==='POST'){
            assert.equal(url.pathname,'/api/academies/owned/'+(kind==='teacher'?'teachers':'student-onboarding'));writes.push(JSON.parse(req.postData()));
            return route.fulfill({status:kind==='teacher'&&status===200?201:status,headers,contentType:'application/json',body:JSON.stringify(status===400?{message:'Synthetic date rejection'}:{id:'synthetic-created'})});
          }
          return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname]||[])});
        }
        if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
      });
      await page.goto(origin+'/'+kind+'-onboarding');
      await page.locator('input[name="'+(kind==='teacher'?'firstName':'studentFirstName')+'"]').fill('Synthetic');
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
      if(status===400){await page.getByText('Synthetic date rejection',{exact:true}).waitFor();assert.equal(await page.locator('input[name="'+(kind==='teacher'?'firstName':'studentFirstName')+'"]').inputValue(),'Synthetic');}
      else if(kind==='teacher'){await page.getByText('Teacher onboarded. Add payment details from the Teacher payment details section.',{exact:true}).waitFor();assert.equal(await page.locator('input[name="firstName"]').inputValue(),'');}
      else await page.waitForURL('**/student-management?studentId=synthetic-created&notice=student-created');
      assert.equal(writes.length,1);
      for(const name of kind==='teacher'?['dateOfBirth','joiningDate']:['admissionDate'])assert.equal(writes[0][name],dates==='blank'?null:'2000-01-01');
      if(kind==='student')assert.equal(writes[0].dateOfBirth,'2000-01-01');
      assert.deepEqual(errors,[]);checks.push({width,theme,kind,dates,status});
      if(dates==='blank'&&kind==='teacher')await page.screenshot({path:path.join(evidence,[width,theme,kind,status].join('-')+'.png'),fullPage:true});
      console.log('PASS '+[width,theme,kind,dates,status].join(' '));await context.close();
    }
    assert.equal(checks.length,32);
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'Synthetic API, native date controls; not browser-to-SQL/device proof'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
  }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
  finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
