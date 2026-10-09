// Actual production-export UI, synthetic host only. No Mini, SQL or credentials.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http');
const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', `penta-design-${Date.now()}`);
fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html':'text/html', '.css':'text/css', '.js':'application/javascript', '.txt':'text/plain', '.woff2':'font/woff2', '.svg':'image/svg+xml', '.ico':'image/x-icon' };
const server = http.createServer((req, res) => {
  try {
    assert.ok(['GET', 'HEAD'].includes(req.method));
    let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) file = path.join(path.dirname(file), '__next.' + segment[1], '__PAGE__.txt');
    file = fs.realpathSync(file);
    assert.ok(file.startsWith(web + path.sep));
    res.writeHead(200, { 'Content-Type':mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control':'no-store' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
const id = '11111111-1111-4111-8111-111111111111';
const conversation = '22222222-2222-4222-8222-222222222222';
const learner = '33333333-3333-4333-8333-333333333333';
function luminance(rgb) {
  return rgb.map(n => { n /= 255; return n <= .04045 ? n / 12.92 : ((n + .055) / 1.055) ** 2.4; }).reduce((sum, n, i) => sum + n * [.2126,.7152,.0722][i], 0);
}
function contrast(a, b) {
  const channels = value => value.match(/[\d.]+/g).slice(0, 3).map(Number);
  const x = luminance(channels(a)), y = luminance(channels(b));
  return (Math.max(x, y) + .05) / (Math.min(x, y) + .05);
}
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = `http://127.0.0.1:${server.address().port}`;
  let browser, currentPage;
  const checks = [], errors = [];
  try {
    browser = await chromium.launch({ headless:true, executablePath:process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 768, 1440]) for (const theme of ['light', 'dark']) {
      const context = await browser.newContext({ viewport:{width, height:1000} });
      const page = currentPage = await context.newPage();
      page.on('pageerror', error => errors.push({width,theme,message:error.message}));
      page.on('console', message => { if (message.type() === 'error') errors.push({width,theme,message:message.text()}); });
      await context.addInitScript(theme => {
        localStorage.setItem('academydesk.theme', theme);
        localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-ui-token');
      }, theme);
      let mode = 'result', calls = 0;
      await context.route('**/*', async route => {
        const url = new URL(route.request().url());
        if (url.pathname.startsWith('/api/') && ['localhost','127.0.0.1'].includes(url.hostname)) {
          const respond = value => route.fulfill({status:200,contentType:'application/json',headers:{'Access-Control-Allow-Origin':origin},body:JSON.stringify(value)});
          if (url.pathname === '/api/auth/session') return respond({academyId:id,displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false});
          if (url.pathname === '/api/academies') return respond([{name:'Synthetic academy',enabledModulesJson:'["Core","Finance"]'}]);
          if (url.pathname === '/api/portal/announcements') return respond([]);
          if (url.pathname.endsWith('/academy-context')) return respond({academyId:id,name:'Synthetic academy',timeZone:'Asia/Kolkata'});
          if (url.pathname.endsWith('/chat/health')) return respond({status:'Available',provider:'PENTA Mini',protocol:'0.1',readOnly:true});
          if (url.pathname.endsWith('/chat/conversations')) return respond({conversationId:conversation,version:0,expiresAtUtc:'2099-10-09T10:00:00Z'});
          if (url.pathname.endsWith('/turns')) {
            calls++;
            const input = route.request().postDataJSON();
            const rows = mode === 'result' ? [{sourceId:learner,displayName:'Synthetic learner with a deliberately long readable display name',recordCode:'SYN-001',subjects:['Piano'],balances:[{currency:'INR',outstanding:400}],sourcePath:`/student-management?studentId=${learner}`}] : [];
            return respond({conversationId:conversation,requestId:input.requestId,version:input.expectedVersion+1,kind:mode === 'error' ? 'ERROR' : 'RESULT',message:mode === 'error' ? 'Synthetic provider unavailable. Use the manual workspace.' : rows.length ? 'Synthetic verified read.' : 'No matching active students.',capability:input.capability,provider:'PENTA Mini',protocol:'0.1',context:{filters:{balance_status:'Pending'},sort_by:null,current_learner_id:null,current_result_ids:rows.map(row=>row.sourceId)},result:mode === 'error' ? null : {count:rows.length,hasMore:false,rows,asOfUtc:'2026-10-09T10:00:00Z',source:'Synthetic ledger',balanceScope:'Synthetic student fees.'}});
          }
          throw Error('Unexpected synthetic API path ' + url.pathname);
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue();
        return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/penta');
      await page.getByText('● Mini connected').waitFor();
      const panel = page.getByRole('region',{name:'PENTA conversation'});
      const system = page.locator('[data-penta-ui="0.1"]');
      assert.equal(await system.count(), 1);
      const assertBounds = async () => assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth <= innerWidth+1), 'No page overflow');
      await assertBounds();
      const helpChecks = [];
      for (const title of ['Pulse','Executor','Navigator','Twin','Autopilot']) {
        const trigger = system.getByRole('button',{name:`About ${title}`,exact:true});
        await trigger.click();
        const note = system.getByRole('note');
        await note.waitFor();
        const box = await note.boundingBox();
        assert.ok(box.x >= 0 && box.x+box.width <= width+1, title+' help viewport bounds');
        assert.ok(await note.evaluate(el => {
          const r=el.getBoundingClientRect(); return el.contains(document.elementFromPoint(r.left+r.width/2,r.top+15));
        }), title+' help above page layer');
        await assertBounds();
        await page.keyboard.press('Escape');
        assert.equal(await note.count(), 0);
        assert.equal(await trigger.evaluate(el=>el===document.activeElement), true);
        helpChecks.push(title);
      }
      const prompt = panel.getByLabel('Message PENTA AI',{exact:true});
      await prompt.fill('Show students with pending fees.');
      const send = panel.getByRole('button',{name:'Send ↑',exact:true});
      const palette = await send.evaluate(el=>{const c=getComputedStyle(el);return {foreground:c.color,background:c.backgroundColor,height:el.getBoundingClientRect().height};});
      const ratio = contrast(palette.foreground,palette.background);
      assert.ok(ratio >= 4.5, 'Primary action normal text contrast');
      assert.ok(palette.height >= 44, 'Send touch target');
      // :focus-visible represents keyboard modality, not a mouse-initiated focus().
      await prompt.press('Tab');
      await page.keyboard.press('Shift+Tab');
      assert.equal(await prompt.evaluate(el=>el===document.activeElement),true);
      const focus = await prompt.evaluate(el=>{const c=getComputedStyle(el);return {style:c.outlineStyle,width:c.outlineWidth};});
      assert.notEqual(focus.style, 'none');
      assert.ok(parseFloat(focus.width)>=3);
      await send.click();
      await panel.getByRole('link',{name:'Open Student 360 ↗'}).waitFor();
      await assertBounds();
      await page.screenshot({path:path.join(evidence,`result-${width}-${theme}.png`),fullPage:true});
      for (const next of ['empty','error']) {
        mode=next;
        await panel.getByRole('button',{name:'New conversation',exact:true}).click();
        await prompt.fill('Synthetic next read');
        await send.click();
        await panel.getByText(next==='empty'?'No matching active students.':'Synthetic provider unavailable. Use the manual workspace.',{exact:true}).waitFor();
        assert.equal(await panel.getByRole('link',{name:'Open Student 360 ↗'}).count(),0);
        await assertBounds();
        await page.screenshot({path:path.join(evidence,`${next}-${width}-${theme}.png`),fullPage:true});
      }
      assert.equal(calls,3,'No automatic retries');
      await page.getByRole('button',{name:/^Workspace/}).click();
      assert.equal(await system.isVisible(),false,'Manual switch hides AI workspace');
      await page.getByRole('button',{name:'PENTA AI',exact:true}).click();
      assert.equal(await system.isVisible(),true);
      checks.push({width,theme,helpChecks,ratio,palette,focus,states:['result','empty','error'],manualSwitch:true,synthetic:true});
      console.log(`PASS PENTA DESIGN ${width} ${theme} contrast ${ratio.toFixed(2)}`);
      await context.close();
    }
    assert.deepEqual(errors,[],'No unexpected console/rendering errors');
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,errors,liveMini:'NOT RUN',sql:'NOT RUN',physicalDevices:'NOT RUN'},null,2));
    console.log(JSON.stringify({passed:checks.length,evidence}));
  } catch (error) {
    if(currentPage && !currentPage.isClosed()) await currentPage.screenshot({path:path.join(evidence,'failure.png'),fullPage:true}).catch(()=>{});
    fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,errors,failure:error.message},null,2));
    throw error;
  } finally { if(browser) await browser.close(); await new Promise(resolve=>server.close(resolve)); }
})().catch(error=>{console.error(error);process.exitCode=1;server.close();});
