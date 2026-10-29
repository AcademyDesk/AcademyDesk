// Real product static export + explicitly synthetic intercepted host responses.
// No real login/credentials, SQL, model calls, provider resources or external pages.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), a = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const { fixture, conversation, draft } = require('./penta-onboarding-review.test.cjs');
const root = path.resolve(__dirname, '../..'), web = path.join(root, 'apps/web/out');
const evidence = path.join(root, 'QA/EVIDENCE', `penta-onboarding-review-browser-${Date.now()}`);
a.ok(fs.existsSync(path.join(web, 'penta/onboarding.html')), 'Build the actual export first');
fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html':'text/html', '.js':'application/javascript', '.css':'text/css', '.txt':'text/plain', '.json':'application/json', '.woff2':'font/woff2', '.ico':'image/x-icon' };
const server = http.createServer((req, res) => {
  if (!['GET', 'HEAD'].includes(req.method)) { res.writeHead(405).end(); return; }
  try {
    let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    a.ok(file.startsWith(web + path.sep)); if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) file = path.join(path.dirname(file), '__next.' + segment[1], '__PAGE__.txt');
    file = fs.realpathSync(file); a.ok(file.startsWith(web + path.sep));
    res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control':'no-store' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
const checks = [], errors = [], expectedHttp = [], requests = [];
let browser;
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = `http://127.0.0.1:${server.address().port}`;
  browser = await chromium.launch({ headless:true, executablePath:process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const cases = [];
  for (const theme of ['dark', 'light']) for (const width of [320, 390, 768, 1440]) cases.push({ name:`valid-${theme}-${width}`, theme, width, scenario:'valid' });
  for (const scenario of ['missing', 'pending', 'denied', 'unavailable', 'bad-hash', 'stale', 'logout', 'token-change', 'role-change', 'recheck'])
    cases.push({ name:scenario, theme:'dark', width:390, scenario });
  for (const scenario of ['pending-recovery','pending-completed','pending-denied','pending-malformed'])
    cases.push({ name:scenario, theme:'light', width:390, scenario });
  try {
    for (const c of cases) {
      const context = await browser.newContext({ viewport:{ width:c.width, height:900 } });
      try {
        const f = fixture();
        // Exercise the actual host's complete descriptor keys/kinds, no data import.
        const source = fs.readFileSync(path.join(root, 'apps/api/Intelligence/Penta/PentaOnboardingFields.cs'), 'utf8');
        f.current.snapshot.tools[0].fields = [...source.matchAll(/\["([^"]+)"\]=\("([^"]+)",(\d+)\)/g)].map(([,key,kind,max]) => ({
          key, kind, label:key.replace(/(?<=[a-z])(?=[A-Z])/g,' '), required:key==='StudentFirstName', clearable:key!=='StudentFirstName',
          choices:[], columns:[], max_length:Math.max(1, Number(max)) }));
        a.equal(f.current.snapshot.tools[0].fields.length, 29);
        const recoveryCase = c.scenario.startsWith('pending-');
        if (c.scenario === 'pending') { f.current.status='pending'; f.current.pendingRequestId=conversation; f.current.snapshot=null; }
        if (c.scenario === 'bad-hash') f.view.payloadHash='a'.repeat(64);
        let currentReads = 0, serviceEnabled = c.scenario !== 'recheck';
        await context.addInitScript(({ theme }) => {
          localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-review-session');
          localStorage.setItem('academydesk.theme',theme);
        }, { theme:c.theme });
        const page = await context.newPage();
        page.on('pageerror', e => errors.push({ scenario:c.name, type:e.name, message:e.message }));
        page.on('console', message => {
          if (message.type() !== 'error') return;
          if (/^Failed to load resource: the server responded with a status of (403|404) \(/.test(message.text()) && ['denied','unavailable','recheck','pending-denied'].includes(c.scenario)) expectedHttp.push({ scenario:c.name, message:message.text() });
          else errors.push({ scenario:c.name, message:message.text() });
        });
        await page.route('**/*', async route => {
          const req = route.request(), url = new URL(req.url());
          if (url.pathname.startsWith('/api/')) {
            if (req.method() === 'OPTIONS') return route.fulfill({ status:204, headers:{
              'Access-Control-Allow-Origin':origin, 'Access-Control-Allow-Methods':'GET', 'Access-Control-Allow-Headers':'authorization,accept' } });
            requests.push({ scenario:c.name, method:req.method(), path:url.pathname });
            a.equal(req.method(), 'GET'); a.equal(req.headers()['authorization'], 'Bearer synthetic-review-session');
            const respond = (value, status=200) => route.fulfill({ status, contentType:'application/json', body:JSON.stringify(value), headers:{'Access-Control-Allow-Origin':origin} });
            if (url.pathname === '/api/auth/session') return respond(f.account);
            if (!serviceEnabled) return respond({},404);
            if (c.scenario === 'denied') return respond({},403);
            if (c.scenario === 'unavailable') return respond({},404);
            if (url.pathname.endsWith('/current')) {
              currentReads++; const value=structuredClone(f.current);
              if (recoveryCase && (currentReads === 1 || c.scenario !== 'pending-completed')) {
                if (c.scenario === 'pending-denied' && currentReads > 1) return respond({},403);
                value.status='pending'; value.pendingRequestId=conversation; value.snapshot=null;
              }
              if (c.scenario === 'stale' && currentReads===2) value.version++;
              return respond(value);
            }
            if (url.pathname.endsWith(`/drafts/${draft}`)) return respond(f.view);
            if (recoveryCase && url.pathname.endsWith(`/claims/${conversation}/result`)) {
              if (c.scenario === 'pending-completed') return respond({ conversationId:conversation, requestId:conversation,
                version:1, canExecute:false, draftId:draft, draftRevision:1, kind:'unsupported', status:'unsupported',
                operations:[], questions:[], snapshot:f.current.snapshot });
              return respond({ conversationId:conversation, requestId:c.scenario === 'pending-malformed' ? draft : conversation,
                version:1, status:'pending', canExecute:false },202);
            }
            throw Error('Unexpected API route');
          }
          if (url.origin === origin) return route.continue();
          return route.abort();
        });
        // A completely unbound route now opens explicit Start. An incomplete
        // review binding must still fail before fetching/adopting private context.
        const target = c.scenario === 'missing' ? `/penta/onboarding?draftId=${draft}` : `/penta/onboarding?conversationId=${conversation}&draftId=${draft}`;
        await page.goto(origin+target);
        if (c.scenario === 'recheck') {
          await page.getByText('Onboarding actions are unavailable',{exact:true}).waitFor();
          serviceEnabled = true;
          await page.getByRole('button',{name:'Recheck host state',exact:true}).click();
        }
        if (recoveryCase) {
          await page.getByRole('button',{name:'Check pending request',exact:true}).waitFor();
          a.equal(requests.filter(r=>r.scenario===c.name).length,2);
          await page.getByRole('button',{name:'Check pending request',exact:true}).click();
          if (c.scenario === 'pending-recovery') await page.getByRole('status').filter({hasText:'The request is still pending.'}).waitFor();
          else if (c.scenario === 'pending-denied') await page.getByText(/Current Academy Owner\/Admin access could not be verified/).waitFor();
          else if (c.scenario === 'pending-malformed') await page.getByText(/The draft changed, expired or could not be verified/).waitFor();
        }
        if (['valid','logout','token-change','role-change','recheck','pending-completed'].includes(c.scenario)) {
          await page.getByRole('button',{name:/Work canvas/}).waitFor();
          a.equal(await page.getByRole('textbox',{name:'Ask PENTA',exact:true}).isDisabled(),true);
          a.equal(await page.getByRole('button',{name:'Send',exact:true}).isDisabled(),true);
          await page.screenshot({ path:path.join(evidence,`${c.name}-workspace.png`) });
          a.equal(await page.locator('.op-message.op-status').isVisible(),true,'Status replies must stay visible on narrow screens');
          await page.getByRole('button',{name:/Work canvas/}).click();
          const dialog=page.getByRole('dialog',{name:'Work canvas',exact:true}); await dialog.waitFor();
          const input=dialog.getByRole('textbox',{name:'Student First Name (required)',exact:true});
          a.equal(await input.inputValue(),'SYNTHETIC-REVIEW-ONLY'); a.equal(await input.isDisabled(),true);
          const last=dialog.getByLabel('Allow Leave',{exact:true}); await last.scrollIntoViewIfNeeded(); a.equal(await last.isDisabled(),true);
          const revise=dialog.locator('.op-draft-form > button[type="submit"]');
          a.equal(await revise.isDisabled(),true);
          a.equal(await revise.isVisible(),false);
          a.equal(await page.getByRole('button',{name:'Confirm exact operation'}).count(),0);
          const close=dialog.getByRole('button',{name:'Close Work canvas',exact:true});
          a.equal(await close.isVisible(),true);
          const closeBounds=await close.boundingBox(); a.ok(closeBounds.y>=0 && closeBounds.y+closeBounds.height<=900);
          await page.screenshot({ path:path.join(evidence,`${c.name}.png`) });
          await close.click();
          if (c.scenario === 'logout') {
            await page.evaluate(() => window.dispatchEvent(new CustomEvent('academydesk:portal-sign-out',{detail:'AcademyAdmin'})));
            await page.getByText(/Current Academy Owner\/Admin access could not be verified/).waitFor();
            a.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);
            a.equal(await page.locator('input').count(),0);
          } else if (c.scenario === 'token-change') {
            await page.evaluate(() => {
              localStorage.setItem('academydesk.accessToken.AcademyAdmin','replacement-synthetic');
              window.dispatchEvent(new StorageEvent('storage',{ key:'academydesk.accessToken.AcademyAdmin',
                newValue:'replacement-synthetic',storageArea:localStorage }));
            });
            await page.getByText(/Current Academy Owner\/Admin access could not be verified/).waitFor();
            a.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);
          } else if (c.scenario === 'role-change') {
            f.account.roles=['Teacher']; f.account.workspace='Teacher';
            await page.getByRole('button',{name:'Recheck host state',exact:true}).click();
            await page.getByText(/Current Academy Owner\/Admin access could not be verified/).waitFor();
            a.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);
            a.equal(await page.locator('input').count(),0);
          } else {
            await page.getByRole('button',{name:'Capability help',exact:true}).click();
            await page.getByRole('dialog',{name:'Your capabilities',exact:true}).waitFor();
            await page.keyboard.press('Escape');
          }
        } else {
          await page.getByText('Onboarding actions are unavailable',{exact:true}).waitFor();
          a.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);
          a.equal(await page.locator('input').count(),0);
          await page.screenshot({ path:path.join(evidence,`${c.name}.png`) });
        }
        if (recoveryCase) a.equal(requests.filter(r=>r.scenario===c.name).length,
          c.scenario === 'pending-completed' ? 9 : c.scenario === 'pending-denied' ? 3 : 4);
        a.equal(await page.getByRole('link',{name:'Manual workspace',exact:true}).getAttribute('href'),'/dashboard');
        a.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth),false);
        checks.push({ scenario:c.name, pass:true, apiGets:requests.filter(r=>r.scenario===c.name).length });
      } finally { await context.close(); }
    }
    a.equal(errors.length,0); a.ok(requests.every(r=>r.method==='GET'));
    console.log(JSON.stringify({ passed:checks.length, failed:0, evidence, synthetic:true, writes:0 }));
  } finally {
    fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({ checks, errors, expectedHttp, requests, synthetic:true, sqlAcceptance:false, modelInference:false },null,2));
    await browser.close(); await new Promise(resolve => server.close(resolve));
  }
})().catch(e => { console.error(`Review browser failed: ${e.name}: ${e.message}`); process.exitCode=1; });
