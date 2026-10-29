// Production-export presentation checks; synthetic reads, never live RBAC/SQL.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {verifyVendor,assertManual}=require('./penta-foundation-contract.cjs');verifyVendor();
const root = path.resolve(__dirname, '../..'), web = require('./web-candidate-export.cjs').resolveExport(root);
const evidence = path.join(root, 'QA/EVIDENCE', 'admin-manual-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => { try {
  assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html';
  const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
  if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
  file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' }); if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
} catch { res.writeHead(404).end(); } });

(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser, currentPage;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const [width, height] of [[320,700], [390,700], [768,700], [1024,700], [1440,700], [768,360]]) for (const theme of ['light','dark']) for (const modules of ['Core', 'Full']) {
      const context = await browser.newContext({ viewport: { width, height }, reducedMotion: 'reduce' }), page = await context.newPage(), errors = [], writes = [];
      currentPage = page;
      page.on('pageerror', error => errors.push(error.message));
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-admin-token'); }, theme);
      const id = '11111111-1111-4111-8111-111111111111', name = 'Synthetic administrator with a deliberately long name for header wrapping';
      const records = Array.from({ length: 35 }, (_, index) => ({ id: 'synthetic-' + index, firstName: 'Synthetic', lastName: 'Person ' + index, isActive: true }));
      const data = {
        '/api/academies': [{ id, name: 'Synthetic academy', subscriptionPlan: 'Synthetic', enabledModulesJson: JSON.stringify(modules === 'Core' ? ['Core'] : ['Core','Engagement','Sales','Finance','AcademicGovernance','Certificates']) }],
        '/api/auth/session': { academyId: id, displayName: name, roles: ['AcademyAdmin'], isPlatformOwner: false },
        '/api/portal/announcements': [{ id:'synthetic-notice', title:'Synthetic announcement', message:'Long safe notification text '.repeat(25) }],
        [`/api/academies/${id}/students`]: records, [`/api/academies/${id}/teachers`]: records,
        [`/api/academies/${id}/students/overview`]: { totalStudents:35, activeStudents:35, inactiveStudents:0, totalAdmissionFees:0, totalSubjectFees:0, outstandingFees:0, overdueFees:0, activeSubjectFeeArrangements:0 },
        [`/api/academies/${id}/penta/academy-context`]: { academyId:id, name:'Synthetic academy', timeZone:'Asia/Kolkata' }
      };
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost','127.0.0.1'].includes(url.hostname)) {
          if (!['GET','OPTIONS'].includes(req.method())) writes.push(req.method() + ' ' + url.pathname);
          return route.fulfill({ status: url.pathname.endsWith('/chat/health') ? 503 : 200, headers: { 'Access-Control-Allow-Origin':origin }, contentType:'application/json', body:JSON.stringify(data[url.pathname] || []) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/student-overview');
      await assertManual(page,theme);
      await page.waitForFunction(name => document.querySelector('.enterprise-admin-context strong')?.textContent === name, name);
      const primaryContrast=await page.getByRole('link',{name:'Add student',exact:true}).evaluate(e=>{const s=getComputedStyle(e),luma=color=>{const rgb=color.match(/[\d.]+/g).slice(0,3).map(Number).map(x=>{x/=255;return x<=.04045?x/12.92:((x+.055)/1.055)**2.4;});return rgb[0]*.2126+rgb[1]*.7152+rgb[2]*.0722;};const a=luma(s.color),b=luma(s.backgroundColor);return (Math.max(a,b)+.05)/(Math.min(a,b)+.05);});assert.ok(primaryContrast>=4.5,'primary action link label contrast>=4.5');
      const shell = page.locator('.enterprise-app-shell'), navigation = page.locator(width <= 900 ? '.enterprise-mobile-workspace-nav' : '.enterprise-sidebar');
      const bounds = async () => assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true, 'no page horizontal overflow');
      await bounds();
      assert.equal(await shell.evaluate(element => getComputedStyle(element).getPropertyValue('--manual-card-radius').trim()), '18px', 'Manual foundation active');
      assert.equal(await page.locator('.enterprise-admin-context strong').evaluate(element => element.scrollWidth <= element.clientWidth + 1 && getComputedStyle(element).whiteSpace !== 'nowrap'), true, 'full Admin name wraps');
      const utilities = page.locator('.enterprise-utilities');
      assert.equal(await utilities.locator(':scope > button,:scope > a,:scope > .enterprise-notifications > button,:scope > .theme-toggle button,:scope > .enterprise-profile > button').evaluateAll(elements => elements.every(element => { const r = element.getBoundingClientRect(); return r.width >= 44 && r.height >= 44; })), true, '44px header targets');
      if (width <= 360) assert.equal(await utilities.locator(':scope > button,:scope > a,:scope > .enterprise-notifications > button,:scope > .theme-toggle button,:scope > .enterprise-profile > button').evaluateAll(elements => { const centres = elements.map(element => { const r=element.getBoundingClientRect(); return r.y + r.height/2; }); return Math.max(...centres)-Math.min(...centres)<=2; }),true,'320px utilities stay on one row');
      const openMenu = async () => {
        if (width <= 900) { const summary = navigation.locator('summary'); await page.keyboard.press('Tab'); await summary.focus(); assert.notEqual(await summary.evaluate(element => getComputedStyle(element).outlineStyle),'none'); await page.keyboard.press('Enter'); }
      };
      if (width <= 900) {
        assert.equal(await navigation.locator('summary span').textContent(),'Students · Overview');
        await openMenu(); await page.keyboard.press('Escape'); assert.equal(await navigation.locator('details').getAttribute('open'),null); assert.equal(await navigation.locator('summary').evaluate(element => element === document.activeElement),true);
        await openMenu(); await page.locator('.enterprise-admin-context').click(); assert.equal(await navigation.locator('details').getAttribute('open'),null);
      }
      await openMenu();
      const preservedLink = navigation.locator('a[href="/student-overview"]');
      await page.keyboard.press('Tab'); await preservedLink.focus();
      const preservedHandle = await preservedLink.elementHandle();
      const refresh = page.waitForResponse(response => new URL(response.url()).pathname === '/api/portal/announcements');
      await page.evaluate(() => window.dispatchEvent(new Event('focus'))); await refresh;
      await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
      assert.equal(await preservedHandle.evaluate(element => element.isConnected && element === document.activeElement),true,'announcement refresh preserves focused navigation DOM');
      if (width <= 900) await page.keyboard.press('Escape');
      for (const [route,label] of [['/teacher-overview','Teachers · Overview'],['/student-overview','Students · Overview']]) {
        await openMenu(); const link = navigation.locator(`a[href="${route}"]`);
        if (width > 900) { const group = navigation.locator('details').filter({has:page.locator(`a[href="${route}"]`)}); if (await group.getAttribute('open') === null) await group.locator(':scope > summary').click(); }
        await page.keyboard.press('Tab'); await link.focus(); await page.keyboard.press('Enter');
        try { await page.waitForURL(url => url.pathname === route); }
        catch (error) { error.message += '\n' + JSON.stringify({ width,height,theme,modules,expected:route,actual:new URL(page.url()).pathname,active:await page.evaluate(() => ({tag:document.activeElement.tagName,href:document.activeElement.getAttribute('href')})) }); throw error; }
        if (width <= 900) { assert.equal(await navigation.locator('details').getAttribute('open'),null); assert.equal(await navigation.locator('summary span').textContent(),label); }
        await bounds();
      }
      await page.getByRole('button',{name:'Open account menu',exact:true}).click();
      const profile = page.locator('.enterprise-profile-menu'); const profileBox = await profile.boundingBox(); assert.ok(profileBox.x >= 0 && profileBox.x + profileBox.width <= width + 1); assert.equal(await profile.evaluate(element => element.scrollWidth <= element.clientWidth + 1),true);
      await page.keyboard.press('Escape'); await profile.waitFor({state:'detached'});
      await page.getByRole('button',{name:'Open notifications',exact:true}).click(); const notice = page.locator('.enterprise-notification-menu'); await notice.getByText('Synthetic announcement',{exact:true}).waitFor(); const noticeBox = await notice.boundingBox(); assert.ok(noticeBox.x >= 0 && noticeBox.x + noticeBox.width <= width + 1,'notification horizontal bounds'); await page.keyboard.press('Escape'); await notice.waitFor({state:'detached'});
      await page.getByRole('button',{name:'Search AcademyDesk',exact:true}).click(); const search = page.getByRole('textbox',{name:'Search AcademyDesk modules',exact:true}); await search.fill('Leads'); const searchPanel = page.locator('.enterprise-search-panel'); const searchBox = await searchPanel.boundingBox(); assert.ok(searchBox.x >= 0 && searchBox.x + searchBox.width <= width + 1,'search horizontal bounds'); assert.equal(await search.evaluate(element => element.getBoundingClientRect().height >= 44),true); if (width <= 900) assert.equal(await search.evaluate(element => getComputedStyle(element).fontSize),'16px');
      if (modules === 'Core') { assert.equal(await searchPanel.locator('a[href="/leads"]').count(),0,'locked module is not a usable link'); await searchPanel.getByRole('button',{name:/Leads/}).click(); await page.getByRole('dialog',{name:'Unlock Sales & Marketing'}).waitFor(); await page.getByRole('button',{name:'Keep browsing',exact:true}).click(); assert.equal(new URL(page.url()).pathname,'/student-overview','upgrade prompt did not navigate'); }
      else { await searchPanel.locator('a[href="/leads"]').waitFor(); await page.getByRole('button',{name:'Search AcademyDesk',exact:true}).click(); }
      const switcher = page.getByRole('group',{name:'Work view',exact:true}); await switcher.getByRole('button',{name:'PENTA AI',exact:true}).click(); await page.getByText('Mini · Unavailable',{exact:false}).waitFor(); assert.equal(await shell.evaluate(element => getComputedStyle(element).getPropertyValue('--manual-card-radius').trim()),'','Manual class absent from AI mode');
      await switcher.getByRole('button',{name:/Workspace/}).click(); assert.equal(await shell.evaluate(element => getComputedStyle(element).getPropertyValue('--manual-card-radius').trim()),'18px','Manual restored during AI outage'); await page.locator('.student-overview-standard').waitFor();
      const scroller = width <= 900 ? page.locator('html') : page.locator('.enterprise-workspace'); assert.equal(await scroller.evaluate(element => { const target = element === document.documentElement ? document.scrollingElement : element; target.scrollTop = target.scrollHeight; return target.scrollTop > 0; }),true,'workspace scroll retained');
      await bounds(); if (width === 320) { await page.evaluate(() => { document.scrollingElement.scrollTop = 0; }); await page.screenshot({path:path.join(evidence,`${theme}-${modules}.png`)}); }
      assert.deepEqual(errors,[]); assert.deepEqual(writes,[]); checks.push({width,height,theme,modules}); console.log('PASS ' + [width,height,theme,modules].join(' ')); await context.close();
    }
    assert.equal(checks.length,24); const sourceHashes = Object.fromEntries(['apps/web/src/components/enterprise-shell.tsx','apps/web/src/components/manual-workspace.module.css'].map(file => [file,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex')])); fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,sourceHashes,transport:'Synthetic read fixtures / production export; no live RBAC/SQL/device acceptance'},null,2)); console.log(JSON.stringify({passed:checks.length,evidence}));
  } catch(error) { if (currentPage && !currentPage.isClosed()) await currentPage.screenshot({path:path.join(evidence,'failed.png')}); fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:error.message},null,2)); throw error; }
  finally { if(browser) await browser.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error => { console.error(error); process.exitCode=1; server.close(); });
