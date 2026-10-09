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
const comparison = [
  {sourceId:learner,displayName:'Synthetic duplicate',recordCode:'SYN-002',subjects:['Piano','Voice'],balances:[{currency:'INR',outstanding:400.5},{currency:'USD',outstanding:12.25}]},
  {sourceId:'44444444-4444-4444-8444-444444444444',displayName:'Synthetic duplicate',recordCode:'SYN-001',subjects:['Piano'],balances:[{currency:'EUR',outstanding:300.75}]},
  {sourceId:'55555555-5555-4555-8555-555555555555',displayName:'<img src=x onerror=alert(1)>',recordCode:null,subjects:[],balances:[]},
].map(row=>({...row,sourcePath:`/student-management?studentId=${row.sourceId}`}));
const capped = [...comparison,...Array.from({length:7},(_,i)=>{
  const sourceId=`66666666-6666-4666-8666-${String(i+1).padStart(12,'0')}`;
  return {sourceId,displayName:`Synthetic extra ${i+1}`,recordCode:`SYN-X${i+1}`,subjects:[],balances:[],sourcePath:`/student-management?studentId=${sourceId}`};
})];
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
      const context = await browser.newContext({ viewport:{width, height:1000}, reducedMotion:'reduce' });
      const page = currentPage = await context.newPage();
      page.on('pageerror', error => errors.push({width,theme,message:error.message}));
      page.on('console', message => { if (message.type() === 'error') errors.push({width,theme,message:message.text()}); });
      await context.addInitScript(theme => {
        localStorage.setItem('academydesk.theme', theme);
        localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-ui-token');
      }, theme);
      let mode = 'result', calls = 0, holdNext = false, releaseTurn;
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
            if (holdNext) { holdNext=false; await new Promise(resolve=>{releaseTurn=resolve;}); }
            const rows = mode === 'comparison' ? comparison : mode === 'capped' ? capped : mode === 'result' ? [{sourceId:learner,displayName:'Synthetic learner with a deliberately long readable display name',recordCode:'SYN-001',subjects:['Piano'],balances:[{currency:'INR',outstanding:400}],sourcePath:`/student-management?studentId=${learner}`}] : [];
            return respond({conversationId:conversation,requestId:input.requestId,version:input.expectedVersion+1,kind:mode === 'error' ? 'ERROR' : mode === 'comparison' ? 'CLARIFICATION_REQUIRED' : 'RESULT',message:mode === 'error' ? 'Synthetic provider unavailable. Use the manual workspace.' : rows.length ? 'Synthetic verified read.' : 'No matching active students.',capability:input.capability,provider:'PENTA Mini',protocol:'0.1',context:{filters:{},sort_by:null,current_learner_id:null,current_result_ids:rows.map(row=>row.sourceId)},result:mode === 'error' ? null : {count:mode === 'capped'?14:rows.length,hasMore:mode === 'capped',rows,asOfUtc:'2026-10-09T10:00:00Z',source:'Synthetic ledger',balanceScope:'Synthetic student fees; currencies kept separate. No combined total.'}});
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
        assert.equal(await panel.getByRole('group',{name:'Result display'}).count(),0,'No view switch for empty/error');
        await assertBounds();
        await page.screenshot({path:path.join(evidence,`${next}-${width}-${theme}.png`),fullPage:true});
      }
      mode='comparison';
      await panel.getByRole('button',{name:'New conversation',exact:true}).click();
      await prompt.fill('Find synthetic duplicates');
      await send.click();
      const display=panel.getByRole('group',{name:'Result display',exact:true});
      await display.waitFor();
      const cards=display.getByRole('button',{name:'Cards',exact:true}), tableButton=display.getByRole('button',{name:'Table',exact:true});
      assert.equal(await cards.getAttribute('aria-pressed'),'true','Cards default for each receipt');
      const cardBalanceText=await panel.locator('ol li').evaluateAll(rows=>rows.map(row=>[...row.querySelectorAll('strong')].map(el=>el.textContent.replace(/Outstanding/g,'')).join('')));
      assert.deepEqual(cardBalanceText,['INR 400.50USD 12.25','EUR 300.75','No outstanding fees']);
      assert.deepEqual(await panel.getByRole('link',{name:'Open Student 360 ↗'}).evaluateAll(links=>links.map(link=>link.getAttribute('href'))),comparison.map(row=>row.sourcePath));
      await tableButton.click();
      const table=panel.getByRole('table',{name:'Verified students · Source order',exact:true});
      await table.waitFor();
      assert.equal(await tableButton.getAttribute('aria-pressed'),'true');
      assert.equal(await cards.getAttribute('aria-pressed'),'false');
      assert.deepEqual(await table.getByRole('columnheader').allTextContents(),['Reference','Student','Courses','Outstanding','Source & follow-up']);
      assert.deepEqual(await table.getByRole('rowheader').allTextContents(),['1','2','3']);
      const tableRows=table.locator('tbody tr');
      assert.deepEqual(await tableRows.locator('td:nth-child(2) strong').allTextContents(),comparison.map(row=>row.displayName));
      assert.deepEqual(await tableRows.locator('td:nth-child(4)').allTextContents(),['INR 400.50USD 12.25','EUR 300.75','No outstanding fees']);
      assert.deepEqual(await tableRows.locator('td:nth-child(4)').allTextContents(),cardBalanceText,'Cards/table balance parity');
      assert.deepEqual(await table.getByRole('link',{name:'Open Student 360 ↗'}).evaluateAll(links=>links.map(link=>link.getAttribute('href'))),comparison.map(row=>row.sourcePath),'Same source links/order');
      assert.equal(await table.locator('img,script').count(),0,'Source text never becomes executable markup');
      assert.ok((await tableRows.nth(2).textContent()).includes('Not assigned'));
      await tableRows.nth(1).getByText('Record reference',{exact:true}).click();
      await tableRows.nth(1).getByText(comparison[1].sourceId,{exact:true}).waitFor();
      const scroll=panel.getByRole('region',{name:'Scrollable verified results',exact:true});
      const dimensions=await scroll.evaluate(el=>({width:el.clientWidth,scrollWidth:el.scrollWidth}));
      if(dimensions.scrollWidth>dimensions.width) {
        await scroll.focus();
        await scroll.press('ArrowRight');
        await page.waitForFunction(()=>document.querySelector('[aria-label="Scrollable verified results"]').scrollLeft>0);
      }
      await assertBounds();
      for(const control of [cards,tableButton,tableRows.nth(1).getByRole('button',{name:'Choose student 2: Synthetic duplicate (SYN-001)',exact:true})]) assert.ok((await control.boundingBox()).height>=44,'44px result actions');
      const choose=tableRows.nth(1).getByRole('button',{name:'Choose student 2: Synthetic duplicate (SYN-001)',exact:true});
      await choose.click();
      assert.equal(await prompt.inputValue(),'Show the second one.');
      assert.equal(await prompt.evaluate(el=>el===document.activeElement),true,'Choice focuses composer');
      assert.equal(calls,4,'View/choice must not send tools or replay');
      await page.screenshot({path:path.join(evidence,`table-${width}-${theme}.png`),fullPage:true});
      await scroll.evaluate(el=>{el.scrollLeft=0;});
      await tableButton.scrollIntoViewIfNeeded();
      await page.screenshot({path:path.join(evidence,`table-start-${width}-${theme}.png`),fullPage:true});
      await cards.click();
      assert.equal(await table.count(),0);
      assert.deepEqual(await panel.getByRole('link',{name:'Open Student 360 ↗'}).evaluateAll(links=>links.map(link=>link.getAttribute('href'))),comparison.map(row=>row.sourcePath));
      assert.equal(await prompt.inputValue(),'Show the second one.','Display switch retains prepared follow-up');
      await page.screenshot({path:path.join(evidence,`comparison-cards-${width}-${theme}.png`),fullPage:true});
      mode='capped';
      await panel.getByRole('button',{name:'New conversation',exact:true}).click();
      await prompt.fill('Synthetic capped read'); await send.click();
      await panel.getByText('Showing the first 10. Refine your prompt to find fewer students.',{exact:true}).waitFor();
      assert.equal(await panel.getByRole('link',{name:'Open Student 360 ↗'}).count(),10);
      assert.equal(await display.getByRole('button',{name:'Cards',exact:true}).getAttribute('aria-pressed'),'true','New receipt resets display');
      await display.getByRole('button',{name:'Table',exact:true}).click();
      assert.equal(await table.getByRole('rowheader').count(),10,'Partial list never invents missing rows');
      assert.equal(await table.getByRole('button',{name:/Choose student/}).count(),0,'RESULT is not a disambiguation action');
      assert.equal(calls,5,'No automatic retries or view requests');
      await assertBounds();
      // An actual late reply must not move a reader or steal keyboard focus.
      const transcript=panel.getByRole('region',{name:'Conversation messages',exact:true});
      assert.equal(await transcript.count(),1,'Named keyboard-scrollable transcript');
      assert.equal(await transcript.getAttribute('tabindex'),'0');
      assert.equal(await transcript.getAttribute('aria-live'),null,'Result controls are not a live announcement');
      const announcement=panel.getByRole('status');
      assert.equal(await announcement.count(),1,'One concise polite status, no nested live regions');
      assert.equal(await announcement.getAttribute('aria-atomic'),'true');
      assert.equal(await announcement.textContent(),'Reply 1 ready. Verified read. 10 students displayed.');
      assert.equal(await transcript.evaluate(el=>getComputedStyle(el).scrollBehavior),'auto','No animated auto-scroll under reduced motion');
      holdNext=true;
      await prompt.fill('Synthetic held follow-up'); await send.click();
      await panel.getByRole('button',{name:'Stop waiting',exact:true}).waitFor();
      await announcement.filter({hasText:/^PENTA is preparing a verified read\.$/}).waitFor();
      await transcript.focus(); await transcript.press('Control+Home');
      await page.waitForFunction(()=>document.querySelector('[aria-label="Conversation messages"]').scrollTop<1);
      const jump=panel.getByRole('button',{name:'Jump to latest ↓',exact:true});
      await jump.waitFor();
      const beforeReading=await transcript.evaluate(el=>({top:el.scrollTop,pageTop:document.scrollingElement.scrollTop,ancestors:(()=>{const values=[];for(let p=el.parentElement;p;p=p.parentElement)values.push(p.scrollTop);return values;})()}));
      assert.ok(beforeReading.top<1,'Reading older messages');
      assert.ok(releaseTurn,'Held synthetic response reached fixture'); releaseTurn();
      await announcement.filter({hasText:/^Reply 2 ready\. Verified read\. 10 students displayed\.$/}).waitFor();
      assert.equal(await transcript.evaluate(el=>el===document.activeElement),true,'Reply does not steal reading focus');
      const afterReading=await transcript.evaluate(el=>({top:el.scrollTop,pageTop:document.scrollingElement.scrollTop,ancestors:(()=>{const values=[];for(let p=el.parentElement;p;p=p.parentElement)values.push(p.scrollTop);return values;})()}));
      assert.deepEqual(afterReading,beforeReading,'Reply preserves transcript and outer-page reading position');
      assert.ok((await jump.boundingBox()).height>=44,'Jump touch target');
      await jump.focus(); await jump.press('Enter');
      assert.equal(await transcript.evaluate(el=>el===document.activeElement),true,'Jump returns keyboard focus to reading region');
      assert.ok(await transcript.evaluate(el=>el.scrollHeight-el.clientHeight-el.scrollTop<=64),'Explicit jump reaches latest reply');
      await jump.waitFor({state:'detached'});
      assert.equal(await jump.count(),0,'Following latest hides jump control');
      const announced=await announcement.textContent();
      await prompt.fill('Unsent synthetic draft');
      await panel.getByRole('group',{name:'Result display',exact:true}).last().getByRole('button',{name:'Table',exact:true}).click();
      assert.equal(await announcement.textContent(),announced,'Typing and changing views do not announce entire result again');
      assert.equal(calls,6,'Reading, jump, typing and table display never dispatch a tool');
      await assertBounds();
      await page.screenshot({path:path.join(evidence,`reading-${width}-${theme}.png`),fullPage:true});
      await panel.getByRole('button',{name:'New conversation',exact:true}).click();
      assert.equal(await announcement.textContent(),'','New conversation clears reply announcement');
      assert.equal(await panel.getByRole('link',{name:'Open Student 360 ↗'}).count(),0,'New conversation clears source results');
      await page.getByRole('button',{name:/^Workspace/}).click();
      assert.equal(await system.isVisible(),false,'Manual switch hides AI workspace');
      await page.getByRole('button',{name:'PENTA AI',exact:true}).click();
      assert.equal(await system.isVisible(),true);
      checks.push({width,theme,helpChecks,ratio,palette,focus,states:['result','empty','error','comparison','capped'],table:{sourceOrder:true,separateCurrencies:true,escapedText:true,preparedChoiceOnly:true,dimensions},reading:{positionPreserved:true,focusPreserved:true,keyboardJump:true,conciseAnnouncements:true,reducedMotion:true,noToolDispatch:true},manualSwitch:true,synthetic:true});
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
