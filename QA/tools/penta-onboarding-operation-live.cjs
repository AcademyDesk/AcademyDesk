'use strict';
// Actual exported product + real guarded HTTP/Identity/disposable SQL. No inference.
const assert = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), http = require('node:http');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api, run] = process.argv.slice(2);
assert.match(api || '', /^http:\/\/127\.0\.0\.1:\d+$/); assert.match(run || '', /^[0-9a-f]{32}$/);
const web = path.resolve(__dirname, '../../apps/web/out');
assert.ok(fs.existsSync(path.join(web, 'penta/onboarding.html')));
const evidence = path.resolve(__dirname, '../EVIDENCE', `penta-onboarding-operation-live-${run}`);
fs.mkdirSync(evidence, { recursive:true });
const mime = { '.html':'text/html', '.js':'application/javascript', '.css':'text/css', '.txt':'text/plain', '.json':'application/json', '.woff2':'font/woff2', '.ico':'image/x-icon' };
const server = http.createServer((req, res) => {
  try {
    assert.ok(['GET','HEAD'].includes(req.method));
    let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    assert.ok(file.startsWith(web + path.sep)); if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) file = path.join(path.dirname(file), '__next.' + segment[1], '__PAGE__.txt');
    file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep));
    res.writeHead(200, { 'Content-Type':mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control':'no-store' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
const checks=[];
async function main() {
  let input=''; for await (const chunk of process.stdin) { input+=chunk; assert.ok(input.length<65536); }
  const fixture=JSON.parse(input); input=''; // Synthetic bearer stays in memory/stdin only.
  assert.equal(fixture.cases.length,4);
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const origin=`http://127.0.0.1:${server.address().port}`;
  const browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  try {
    for(const c of fixture.cases) {
      assert.match(c.conversation,/^[0-9a-f-]{36}$/);assert.match(c.draft,/^[0-9a-f-]{36}$/);
      const width=c.name==='dark-desktop'?1440:c.name==='tampered-approval'?768:c.name==='lost-response'?390:320;
      const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage();
      const calls=[],errors=[];let lost=false,tampered=false,reentered=false;
      try {
        await context.addInitScript(({token,theme})=>{
          localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);localStorage.setItem('academydesk.theme',theme);
        },{token:fixture.token,theme:c.name==='light-mobile'||c.name==='tampered-approval'?'light':'dark'});
        page.on('pageerror',error=>errors.push(error.message));
        page.on('console',message=>{
          if(message.type()!=='error')return;
          const text=message.text();
          if(c.name==='lost-response'&&/Failed to load resource: net::ERR_FAILED/.test(text))return;
          if(c.name==='tampered-approval'&&/^Failed to load resource: the server responded with a status of 409/.test(text))return;
          errors.push(text);
        });
        await context.route('**/*',async route=>{
          const request=route.request(),url=new URL(request.url());
          if(url.pathname.startsWith('/api/')) {
            assert.ok(['localhost','127.0.0.1'].includes(url.hostname));
            assert.ok(['GET','POST'].includes(request.method()));
            const options={url:api+url.pathname+url.search,maxRedirects:0,maxRetries:0};
            if(c.name==='tampered-approval'&&url.pathname.endsWith('/core/confirm')) {
              assert.equal(tampered,false);tampered=true;
              const body=JSON.parse(request.postData());body.approval_ref='11111111-1111-4111-8111-111111111111';options.postData=JSON.stringify(body);
            }
            const response=await route.fetch(options);assert.notEqual(response.status(),429);
            calls.push({method:request.method(),path:url.pathname,status:response.status()});
            if(c.name==='lost-response'&&url.pathname.endsWith('/core/execute')) {
              assert.equal(lost,false);lost=true;assert.equal(response.status(),201);
              return route.abort('failed'); // Real SQL committed first; only browser delivery is lost.
            }
            return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Accept,Content-Type'}});
          }
          if(url.origin===origin||url.protocol==='data:')return route.continue();
          throw Error('Unexpected external request');
        });
        await page.goto(`${origin}/penta/onboarding?conversationId=${c.conversation}&draftId=${c.draft}`);
        const review=page.getByRole('button',{name:'Review prepared operation',exact:true});await review.waitFor();
        assert.equal(calls.length,4);assert.ok(calls.every(x=>x.method==='GET'&&x.status===200));
        assert.equal(await page.getByRole('button',{name:'Create approved student',exact:true}).count(),0);
        await review.click();await page.getByText('Review before approval',{exact:true}).waitFor();
        await page.getByRole('button',{name:/Work canvas/}).click();
        const dialog=page.getByRole('dialog',{name:'Work canvas',exact:true});await dialog.waitFor();
        assert.equal(await dialog.getByRole('textbox',{name:'Student First Name (required)',exact:true}).isDisabled(),true);
        const confirm=dialog.getByRole('button',{name:'Confirm exact operation',exact:true});await confirm.scrollIntoViewIfNeeded();
        await page.screenshot({path:path.join(evidence,c.name+'-review.png')});await confirm.click();
        if(c.name==='tampered-approval') {
          await page.getByText('Creation outcome is unconfirmed',{exact:true}).waitFor();
          assert.equal(tampered,true);assert.equal(calls.filter(x=>x.path.endsWith('/core/confirm'))[0].status,409);
          assert.equal(calls.filter(x=>x.path.endsWith('/core/execute')).length,0);
          await page.getByRole('button',{name:'Check saved result',exact:true}).click();
          // Reads cannot turn rejected/tampered approval into permission to execute.
          await page.waitForFunction(()=>!document.querySelector('[aria-label="Host creation controls"][aria-busy="true"]'));
          assert.equal(await page.getByRole('button',{name:'Create approved student',exact:true}).count(),0);
          assert.equal(await page.getByRole('button',{name:'Confirm exact operation',exact:true}).count(),0);
        } else {
          await page.getByText('Approved, not yet confirmed as created',{exact:true}).waitFor();
          const create=page.getByRole('button',{name:'Create approved student',exact:true});await create.click();
          if(c.name==='lost-response') {
            await page.getByText('Creation outcome is unconfirmed',{exact:true}).waitFor();assert.equal(lost,true);
            assert.equal(await page.getByRole('link',{name:'Open Student 360',exact:true}).count(),0);
            await page.getByRole('button',{name:'Check saved result',exact:true}).click();
          }
          await page.getByText('Student created',{exact:true}).waitFor();
          const link=page.getByRole('link',{name:'Open Student 360',exact:true});
          assert.match(await link.getAttribute('href'),/^\/student-management\?studentId=[0-9a-f-]{36}$/);
          assert.equal(await create.count(),0);
          assert.equal(calls.filter(x=>x.path.endsWith('/core/execute')).length,1);
          assert.equal(calls.find(x=>x.path.endsWith('/core/execute')).status,201);
          await page.getByRole('button',{name:/Work canvas/}).click();
          await page.getByRole('dialog',{name:'Work canvas',exact:true}).getByText('Host outcome · succeeded',{exact:true}).waitFor();
          await page.getByRole('button',{name:'Close Work canvas',exact:true}).click();
          // Full document reload discards the old channel and all in-memory
          // receipts. Re-entry may verify the existing result, never create again.
          const beforeReload=calls.length,studentHref=await link.getAttribute('href');
          await page.reload();
          const saved=page.getByRole('button',{name:'Verify saved result',exact:true});await saved.waitFor();
          assert.equal(calls.length,beforeReload+4);assert.ok(calls.slice(beforeReload).every(x=>x.method==='GET'&&x.status===200));
          assert.equal(await page.getByRole('link',{name:'Open Student 360',exact:true}).count(),0);
          assert.equal(await page.getByText('Student created',{exact:true}).count(),0);
          assert.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);
          assert.equal(await page.getByRole('button',{name:'Review prepared operation',exact:true}).count(),0);
          await saved.click();await page.getByText('Student created',{exact:true}).waitFor();
          assert.equal(await page.getByRole('link',{name:'Open Student 360',exact:true}).getAttribute('href'),studentHref);
          assert.equal(calls.length,beforeReload+8);
          assert.deepEqual(calls.slice(beforeReload+4).map(x=>x.path.split('/').pop()),['operation','status','session','current']);
          assert.ok(calls.slice(beforeReload).every(x=>!/(challenge|confirm|execute|turns)$/.test(x.path)));
          assert.equal(await page.getByRole('button',{name:'Create approved student',exact:true}).count(),0);
          await page.getByRole('button',{name:/Work canvas/}).click();
          const savedCanvas=page.getByRole('dialog',{name:'Work canvas',exact:true});
          await savedCanvas.getByText('Host outcome · succeeded',{exact:true}).waitFor();
          assert.equal(await savedCanvas.getByRole('button',{name:'Confirm exact operation',exact:true}).count(),0);
          await page.getByRole('button',{name:'Close Work canvas',exact:true}).click();reentered=true;
          assert.equal(calls.filter(x=>x.path.endsWith('/core/execute')).length,1);
        }
        if(c.name!=='tampered-approval') {
          const appearance=page.getByRole('button',{name:'Appearance',exact:true});await appearance.scrollIntoViewIfNeeded();
          assert.equal(await appearance.evaluate(node=>node.scrollWidth>node.clientWidth),false);
          const capability=page.getByRole('button',{name:'Executor',exact:true});await capability.scrollIntoViewIfNeeded();
          const capabilityBox=await capability.boundingBox();assert.ok(capabilityBox&&capabilityBox.x>=0&&capabilityBox.x+capabilityBox.width<=width);
        }
        for(const action of ['challenge','confirm'])assert.equal(calls.filter(x=>x.path.endsWith('/core/'+action)).length,1);
        assert.equal(await page.getByRole('link',{name:'Manual workspace',exact:true}).getAttribute('href'),'/dashboard');
        assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
        assert.deepEqual(errors,[]);await page.screenshot({path:path.join(evidence,c.name+'-result.png')});
        checks.push({scenario:c.name,width,pass:true,calls,errors,lostResponseAfterCommit:lost,approvalManipulated:tampered,
          terminalFullDocumentReentry:reentered,terminalReentryRequests:reentered?8:0});
        console.log(`PASS: PENTAONBOARDINGOPERATION browser ${c.name};${calls.length} actual host requests`);
      } catch(error) {
        await page.screenshot({path:path.join(evidence,'failed-'+c.name+'.png')}).catch(()=>{});
        fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({scenario:c.name,calls,errors,error:error.message},null,2));throw error;
      } finally {await context.close();}
    }
    assert.equal(checks.length,4);
    assert.equal(checks.filter(x=>x.terminalFullDocumentReentry).length,3);
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({run,checks,sqlBacked:true,syntheticIdentities:true,
      syntheticApiResponses:false,inference:false,physicalDevices:'NOT RUN',sqlPostconditions:'enforced independently by parent harness'},null,2));
  } finally {await browser.close();await new Promise(resolve=>server.close(resolve));}
}
main().catch(error=>{console.error(`SQL operation browser failed: ${error.name}: ${error.message}`);process.exitCode=1;server.close();});
