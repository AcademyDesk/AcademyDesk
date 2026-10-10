// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'subject-fee-amount-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
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

    const baseline=process.argv.includes('--baseline');
    for(const screen of ['student-fees','student-profile'])for(const width of [320,1440])for(const theme of ['light','dark']){
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),writes=[],errors=[],items=[];
      page.on('pageerror',e=>errors.push(e.message));
      await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-feedback-token');},theme);
      const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],
        '/api/academies/owned/students':[{id:'student',firstName:'Synthetic',lastName:'Learner',isActive:true}],
        '/api/academies/owned/students/student/profile':{},'/api/academies/owned/students/student/fee-arrangements/admission-fee':{}};
      await context.route('**/*',async route=>{
        const req=route.request(),url=new URL(req.url());
        if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
          const headers={'Access-Control-Allow-Origin':origin};
          if(url.pathname==='/api/academies/owned/students/student/fee-arrangements'){
            if(req.method()==='POST'){const body=JSON.parse(req.postData());writes.push(body);items.push({...body,id:'fee-'+items.length,isActive:true});return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(items.at(-1))});}
            assert.equal(req.method(),'GET');return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(items)});
          }
          assert.equal(req.method(),'GET');assert.ok(Object.hasOwn(data,url.pathname),url.pathname);
          return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
        }
        if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
      });
      await page.goto(origin+'/'+screen);
      const form=page.locator('.student-fee-arrangements-form'),amount=form.getByPlaceholder('Amount',{exact:true}),subject=form.getByPlaceholder('Subject, e.g. Piano');
      await amount.waitFor();await subject.fill('Synthetic Piano');
      assert.equal(await amount.getAttribute('min'),'1');assert.equal(await amount.getAttribute('step'),baseline?null:'0.01');
      for(const value of ['1','1.01','1250.50','0','-1','0.50','1.001','']){
        await amount.fill(value);
        const validity=await amount.evaluate(e=>({valid:e.checkValidity(),stepMismatch:e.validity.stepMismatch,rangeUnderflow:e.validity.rangeUnderflow,valueMissing:e.validity.valueMissing}));
        const expected=baseline?value==='1':['1','1.01','1250.50'].includes(value);
        assert.equal(validity.valid,expected,screen+' '+value);
        if(['1.01','1250.50'].includes(value))assert.equal(validity.stepMismatch,baseline);
        if(['0','-1','0.50'].includes(value))assert.equal(validity.rangeUnderflow,true);
        if(value==='1.001')assert.equal(validity.stepMismatch,true);
        if(value==='')assert.equal(validity.valueMissing,true);
        if(!expected){const before=writes.length;await form.getByRole('button',{name:'Add',exact:true}).click();assert.equal(writes.length,before);}
        checks.push({screen,width,theme,value,validity,baseline});
      }
      for(const value of baseline?['1']:['1','1.01','1250.50']){
        await subject.fill('Synthetic Piano');await amount.fill(value);
        const before=writes.length;await form.getByRole('button',{name:'Add',exact:true}).click();
        await page.waitForFunction(()=>document.querySelector('.student-fee-arrangements-form input[placeholder="Amount"]').value==='');
        assert.equal(writes.length,before+1);
        assert.deepEqual(writes.at(-1),{subjectName:'Synthetic Piano',amount:Number(value),frequency:'Monthly',effectiveFrom:new Date().toISOString().slice(0,10)});
        await page.getByText('Fee arrangement added.',{exact:true}).waitFor();
        checks.push({screen,width,theme,submitted:value,payload:writes.at(-1),baseline});
      }
      assert.deepEqual(errors,[]);
      await page.screenshot({path:path.join(evidence,[screen,width,theme].join('-')+'.png'),fullPage:true});
      console.log('PASS '+[screen,width,theme,baseline?'baseline':'fixed'].join(' '));await context.close();
    }
    assert.equal(checks.length,baseline?72:88);
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'Synthetic API; no live SQL/Identity/device acceptance'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
  }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
  finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
