// Post-login visual review of real exported UI with explicit synthetic fixtures.
// No real login, credentials, academy data or backend writes. Not auth/SQL proof.
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const { verifyVendor, assertManual } = require('./penta-foundation-contract.cjs');
verifyVendor();
const origin = process.env.QA_REVIEW_ORIGIN || 'http://localhost:3000';
assert.ok(['localhost', '127.0.0.1'].includes(new URL(origin).hostname), 'loopback only');
const root = path.resolve(__dirname, '../..'), evidence = path.join(root, 'QA/EVIDENCE/manual-review-' + Date.now());
fs.mkdirSync(evidence, { recursive: true });
const academy = { id: 'preview-academy', name: 'Design Review Academy', subscriptionPlan: 'Preview', enabledModulesJson: JSON.stringify(['Core', 'Engagement', 'Sales', 'Finance', 'AcademicGovernance', 'Certificates']) };
const students = ['Asha', 'Ravi', 'Mira'].map((name, i) => ({ id: 'preview-student-' + i, firstName: name, lastName: 'Example', isActive: true }));
const schedule = ['Piano foundations', 'Contemporary vocals'].map((batchName, i) => ({ id: 'preview-session-' + i, batchName, teacherName: 'Demo teacher', startUtc: `2026-10-14T${i ? '12' : '10'}:30:00Z`, endUtc: `2026-10-14T${i ? '13' : '11'}:30:00Z`, status: 'Scheduled', deliveryMode: i ? 'Online' : 'Offline', roomName: i ? null : 'Studio 1' }));
const data = {
  '/api/academies': [academy],
  '/api/auth/session': { academyId: academy.id, displayName: 'Demo Administrator', roles: ['AcademyAdmin'], isPlatformOwner: false },
  '/api/portal/announcements': [],
  [`/api/academies/${academy.id}/dashboard`]: { activeStudents: 3, activeTeachers: 2, activeCourses: 4, activeBatches: 2, openLeads: 5, attendanceRecordsLast30Days: 10, presentAttendanceLast30Days: 9, outstandingBalance: 1000, classesToday: 2, todaySchedule: schedule, recentActivity: [{ id: 'preview-activity', action: 'StudentCreated', entityType: 'Student', occurredAtUtc: new Date().toISOString() }] },
  [`/api/academies/${academy.id}/students`]: students,
  [`/api/academies/${academy.id}/invoices`]: [{ id: 'preview-invoice', studentId: students[0].id, invoiceNumber: 'DEMO-001', totalAmount: 1000, paidAmount: 0, dueDate: '2026-10-20', status: 'Issued' }],
  [`/api/academies/${academy.id}/payroll/payouts`]: [{ id: 'preview-payout', workerName: 'Demo teacher', periodLabel: 'October', netAmount: 200, status: 'PendingApproval' }],
  [`/api/academies/${academy.id}/sessions`]: [],
};
(async () => {
  const checks = [], browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  try {
    for (const width of [1440, 768, 390]) for (const theme of ['dark', 'light']) {
      const context = await browser.newContext({ viewport: { width, height: 1000 }, reducedMotion: 'reduce' }), page = await context.newPage(), errors = [], writes = [];
      page.on('pageerror', e => errors.push(e.message));
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-review-only'); }, theme);
      await context.route('**/*', route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          if (!['GET', 'OPTIONS'].includes(req.method())) { writes.push(req.method() + ' ' + url.pathname); return route.abort('blockedbyclient'); }
          return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': origin }, contentType: 'application/json', body: JSON.stringify(data[url.pathname] || []) });
        }
        return url.origin === origin || url.protocol === 'data:' ? route.continue() : route.abort('blockedbyclient');
      });
      await page.goto(origin + '/dashboard'); await assertManual(page, theme);
      await page.getByRole('heading', { name: /Demo Administrator/ }).waitFor();
      await page.getByRole('button', { name: /^Active students/ }).locator('strong').filter({ hasText: /^3$/ }).waitFor();
      await page.getByRole('button', { name: /^Attendance rate/ }).locator('strong').filter({ hasText: /^90%$/ }).waitFor();
      await page.getByRole('button', { name: /^Active students/ }).click();
      const modal = page.getByRole('dialog', { name: 'Active students', exact: true });
      await modal.waitFor(); await modal.getByText('Asha Example', { exact: true }).waitFor();
      await page.keyboard.press('Escape'); await modal.waitFor({ state: 'detached' });
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true, 'dashboard page width');
      // Watermark only this isolated screenshot context, never production source.
      await page.evaluate(() => { const note = document.createElement('div'); note.textContent = 'DESIGN REVIEW · SAMPLE DATA · NOT A LIVE LOGIN'; note.style.cssText = 'position:fixed;bottom:8px;left:8px;z-index:99999;background:#07111f;color:#fff;padding:8px 12px;border:1px solid #51c6f4;border-radius:8px;font:11px Arial;pointer-events:none'; document.body.append(note); document.scrollingElement.scrollTop = 0; });
      await page.screenshot({ path: path.join(evidence, `${theme}-${width}.png`), fullPage: true });
      assert.deepEqual(errors, []); assert.deepEqual(writes, []);
      checks.push({ width, theme }); console.log('PASS dashboard review ' + width + ' ' + theme); await context.close();
    }
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, origin, synthetic: true, liveLogin: false, backendWrites: 0 }, null, 2));
    console.log(JSON.stringify({ passed: checks.length, evidence }));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
