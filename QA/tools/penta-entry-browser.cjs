// Production-export UI with synthetic auth/session responses. Not real auth/SQL/model proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const { assertManual, verifyVendor } = require('./penta-foundation-contract.cjs'); verifyVendor();
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE/penta-entry-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => {
  try {
    assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    if (!path.extname(file)) file += '.html'; const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
    file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
const academyId = '11111111-1111-4111-8111-111111111111';
const cases = [
  { role: 'PlatformOwner', workspace: 'Platform', ai: '/platform/ai', manual: '/platform', isPlatformOwner: true },
  { role: 'AcademyAdmin', workspace: 'AcademyAdmin', ai: '/penta', manual: '/penta', isPlatformOwner: false },
  { role: 'Owner', workspace: 'AcademyAdmin', ai: '/penta', manual: '/penta', isPlatformOwner: false },
  { role: 'Teacher', workspace: 'Teacher', ai: '/teacher/ai', manual: '/teacher', isPlatformOwner: false },
  { role: 'Student', workspace: 'Portal', ai: '/portal/ai', manual: '/portal', isPlatformOwner: false },
  { role: 'Guardian', workspace: 'Portal', ai: '/portal/ai', manual: '/portal', isPlatformOwner: false },
];
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser, currentPage;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    async function fixture(c, width, theme, status = 200) {
      const context = await browser.newContext({ viewport: { width, height: 800 }, reducedMotion: 'reduce' }), page = currentPage = await context.newPage(), errors = [], calls = [];
      page.on('pageerror', e => errors.push(e.message)); await context.addInitScript(theme => localStorage.setItem('academydesk.theme', theme), theme);
      const session = { workspace: c.workspace, roles: [c.role], isPlatformOwner: c.isPlatformOwner, academyId, displayName: 'Synthetic ' + c.role };
      const data = {
        '/api/auth/session': status === 'malformed' ? { ...session, workspace: 'Unknown' } : status === 'mismatch' ? { ...session, workspace: 'AcademyAdmin', roles: ['AcademyAdmin'], isPlatformOwner: false } : session,
        '/api/academies': [{ id: academyId, name: 'Synthetic academy', enabledModulesJson: '["Core","Finance"]' }],
        [`/api/academies/${academyId}/penta/academy-context`]: { academyId, name: 'Synthetic academy', timeZone: 'Asia/Kolkata' },
        '/api/teacher/me': { userId: 'synthetic', firstName: 'Synthetic', lastName: 'Teacher', batches: [], sessions: [] },
        '/api/teacher/calendar': { sessions: [], holidays: [] }, '/api/teacher/payments': { payslips: [] },
        '/api/portal/me': { role: c.role === 'Guardian' ? 'Parent' : 'Student', displayName: 'Synthetic student', studentId: 'student', children: [{ id: 'student', name: 'Synthetic Student', canViewAcademicProgress: true, canViewFinance: false, canViewDocuments: true, canManageLeave: false }] },
        '/api/portal/students/student': { name: 'Synthetic Student', firstName: 'Synthetic', lastName: 'Student', schedule: [], assignments: [], attendanceSummary: { total: 0, present: 0, late: 0 }, practiceSummary: { logCount: 0, totalMinutes: 0 }, practiceLogs: [], music: [], resources: [], assessmentResults: [], certificates: [], invoices: [], classHistory: [], cycleProgress: [] },
        '/api/platform/overview': { recentAudit: [] }, '/api/platform/health': { api: 'Healthy', database: 'Healthy' },
      };
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          calls.push({ path: url.pathname, method: req.method(), authorization: req.headers().authorization });
          if (req.method() === 'OPTIONS') return route.fulfill({ status: 204, headers: { 'Access-Control-Allow-Origin': origin, 'Access-Control-Allow-Headers': 'authorization,content-type' } });
          let body = data[url.pathname] || [], code = 200;
          if (url.pathname === '/api/auth/login') { assert.equal(req.method(), 'POST'); body = { accessToken: 'synthetic-entry-token', refreshToken: 'synthetic-refresh' }; }
          else assert.equal(req.method(), 'GET', 'no domain/model mutation permitted');
          if (url.pathname === '/api/auth/session') code = typeof status === 'number' ? status : 200;
          if (url.pathname.endsWith('/chat/health')) { code = 503; body = {}; }
          return route.fulfill({ status: code, headers: { 'Access-Control-Allow-Origin': origin }, contentType: 'application/json', body: JSON.stringify(body) });
        }
        return url.origin === origin || url.protocol === 'data:' ? route.continue() : route.abort('blockedbyclient');
      });
      return { context, page, calls, errors };
    }
    for (const width of [320, 768, 1440]) for (const theme of ['dark', 'light']) for (const c of cases) {
      const { context, page, calls, errors } = await fixture(c, width, theme);
      await page.goto(origin + '/login'); await page.getByLabel('User name', { exact: true }).fill('synthetic@example.invalid'); await page.getByLabel('Password', { exact: true }).fill('synthetic-preview-only'); await page.getByRole('button', { name: 'Sign in', exact: true }).click();
      await page.waitForURL(origin + c.ai);
      const saved = await page.evaluate(() => Object.fromEntries(Object.keys(localStorage).filter(k => k.startsWith('academydesk.accessToken')).map(k => [k, localStorage.getItem(k)])));
      assert.deepEqual(saved, { ['academydesk.accessToken.' + c.workspace]: 'synthetic-entry-token' }, 'session saved only in resolved workspace');
      if (c.workspace === 'AcademyAdmin') {
        await page.getByText('Mini · Unavailable', { exact: false }).waitFor();
        assert.equal(await page.getByRole('textbox', { name: 'Message PENTA AI', exact: true }).isDisabled(), true, 'outage cannot submit');
        const switcher = page.getByRole('group', { name: 'Work view', exact: true }); await switcher.getByRole('button', { name: /Workspace/ }).click(); await page.getByRole('heading', { name: 'Your academy workspace', exact: true }).waitFor(); await switcher.getByRole('button', { name: 'PENTA AI', exact: true }).click();
      } else {
        await page.getByRole('heading', { name: /conversation is not available yet/ }).waitFor();
        await assertManual(page, theme);
        const contrast = await page.getByRole('link', { name: 'Open Manual workspace', exact: true }).evaluate(e => { const s = getComputedStyle(e), luma = text => { const rgb = text.match(/[\d.]+/g).slice(0, 3).map(Number).map(x => { x /= 255; return x <= .04045 ? x / 12.92 : ((x + .055) / 1.055) ** 2.4; }); return rgb[0] * .2126 + rgb[1] * .7152 + rgb[2] * .0722; }; const a = luma(s.color), b = luma(s.backgroundColor); return (Math.max(a, b) + .05) / (Math.min(a, b) + .05); });
        assert.ok(contrast >= 4.5, 'Manual call-to-action readable in actual theme');
        assert.ok(calls.every(call => ['/api/auth/login', '/api/auth/session'].includes(call.path)), 'unavailable role never requests academy data or PENTA tools');
        assert.ok(calls.filter(call => call.path === '/api/auth/session').every(call => call.authorization === 'Bearer synthetic-entry-token'), 'correct workspace token used for session verification');
        await page.getByText('Pulse ⓘ', { exact: false }).click(); await page.getByText('Proactive briefings, alerts and priorities.', { exact: true }).waitFor();
        await page.getByRole('navigation', { name: 'Work view', exact: true }).getByRole('link', { name: 'Manual workspace', exact: true }).click(); await page.waitForURL(origin + c.manual);
        const bar = page.getByRole('navigation', { name: 'Work view', exact: true });
        if (c.role === 'Teacher' && width === 320) {
          const nav = page.locator('.learner-bottom-nav'); await nav.locator('summary').click(); await nav.getByRole('button', { name: 'Profile & leave', exact: true }).click();
          const draft = page.locator('input[name="firstName"]'); await draft.fill('Preserved Manual Draft');
          await bar.getByRole('link', { name: 'PENTA AI', exact: true }).click(); const dialog = page.getByRole('dialog', { name: 'Leave Manual workspace?', exact: true }); await dialog.waitFor();
          await dialog.getByRole('button', { name: 'Stay in Manual', exact: true }).click(); await dialog.waitFor({ state: 'detached' }); assert.equal(await draft.inputValue(), 'Preserved Manual Draft', 'stay preserves draft');
          await bar.getByRole('link', { name: 'PENTA AI', exact: true }).click(); await dialog.waitFor(); await page.keyboard.press('Escape'); await dialog.waitFor({ state: 'detached' }); assert.equal(await draft.inputValue(), 'Preserved Manual Draft', 'Escape preserves draft');
          await bar.getByRole('link', { name: 'PENTA AI', exact: true }).click(); await dialog.getByRole('button', { name: 'Leave and open PENTA AI', exact: true }).click();
        } else await bar.getByRole('link', { name: 'PENTA AI', exact: true }).click();
        await page.waitForURL(origin + c.ai); await page.getByRole('heading', { name: /conversation is not available yet/ }).waitFor();
        assert.equal(await page.locator('textarea').count(), 0, 'no usable role conversation or fabricated model');
      }
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true, 'page remains viewport-contained');
      assert.deepEqual(errors, []); assert.ok(calls.filter(c => c.method === 'POST').every(c => c.path === '/api/auth/login'));
      if (width === 320) await page.screenshot({ path: path.join(evidence, theme + '-' + c.role + '.png'), fullPage: true });
      checks.push({ role: c.role, width, theme, kind: 'login-mode-outage' }); console.log('PASS entry ' + [width, theme, c.role].join(' ')); await context.close();
    }
    for (const c of cases.filter(c => c.workspace !== 'AcademyAdmin')) for (const status of [401, 403, 503, 'mismatch']) {
      const { context, page, calls, errors } = await fixture(c, 390, 'dark', status);
      await context.addInitScript(workspace => localStorage.setItem('academydesk.accessToken.' + workspace, 'synthetic-entry-token'), c.workspace);
      await page.goto(origin + c.ai); await page.getByRole('alert').waitFor(); assert.ok(calls.every(call => call.path === '/api/auth/session'), 'negative entry requests session only'); assert.equal(await page.locator('textarea').count(), 0); assert.deepEqual(errors, []);
      checks.push({ role: c.role, status, kind: 'negative-entry' }); console.log('PASS entry denial ' + c.role + ' ' + status); await context.close();
    }
    // A failed authoritative profile must not default to an Admin token or redirect.
    for (const status of [401, 403, 503, 'malformed']) {
      const c = cases[0], { context, page } = await fixture(c, 390, 'light', status);
      await page.goto(origin + '/login'); await page.getByLabel('User name', { exact: true }).fill('synthetic@example.invalid'); await page.getByLabel('Password', { exact: true }).fill('synthetic-preview-only'); await page.getByRole('button', { name: 'Sign in', exact: true }).click(); await page.getByText('Login failed.', { exact: false }).waitFor();
      assert.equal(new URL(page.url()).pathname, '/login'); assert.equal(await page.evaluate(() => Object.keys(localStorage).some(k => k.startsWith('academydesk.accessToken'))), false);
      checks.push({ status, kind: 'failed-profile-no-session-save' }); await context.close();
    }
    const sources = Object.fromEntries(['apps/web/src/app/login/page.tsx', 'apps/web/src/lib/workspace-entry.ts', 'apps/web/src/components/workspace-frame.tsx', 'apps/web/src/components/penta/role-penta-entry.tsx', 'apps/web/src/components/penta/role-penta-entry.module.css'].map(file => [file, crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex')]));
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, sources, transport: 'synthetic auth/session/reads with exported UI; no live auth/SQL/provider/device acceptance' }, null, 2)); console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (error) { if (currentPage && !currentPage.isClosed()) await currentPage.screenshot({ path: path.join(evidence, 'failed.png') }); fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, failure: error.message }, null, 2)); throw error; }
  finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(error => { console.error(error); process.exitCode = 1; server.close(); });
