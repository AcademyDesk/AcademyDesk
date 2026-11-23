'use strict';
// Real exported React/CSS in sandboxed Chromium; intercepted synthetic HTTP only.
// Presentation proof, not model, native SQL, device or human visual acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { start } = require('./penta-browser-harness.cjs');
const root = path.resolve(__dirname, '../..');
const academy = '00000001-1111-4111-8111-111111111111';
const session = '00000002-1111-4111-8111-111111111111';
const baseline = process.env.QA_VISUAL_BASELINE === '1';
async function main() {
  const browser = await start({ academyId: academy, evidence: path.join(root, '.build-check/penta-visual-alignment'), prefix: 'penta-visual-' });
  const cases = []; let failure = null, cleanup;
  try {
    for (const width of [320, 768, 1280]) for (const theme of ['light', 'dark']) {
      let p;
      try {
        p = await browser.page({ role: 'Owner', workspace: 'AcademyAdmin', route: '/penta', manual: '/dashboard' }, width, async req => {
          if (req.path.endsWith('/lookup-readiness')) return { body: { protocol: 'academy-lookup.v1', academyId: academy } };
          if (req.path.endsWith('/penta/chat/health')) return { body: { status: 'Available', provider: 'PENTA Mini', protocol: '0.1', readOnly: true } };
          if (req.path.endsWith('/penta/chat/conversations')) return { status: 201, body: { conversationId: session, version: 0, expiresAtUtc: '2030-01-01T00:00:00Z' } };
          return { status: 404 };
        }, `localStorage.setItem('academydesk.theme',${JSON.stringify(theme)});`);
        await p.wait('document.getElementById("mini-evaluation-title") !== null');
        await p.wait(`document.querySelector('[data-penta-theme="${theme === 'dark' ? 'orbit' : 'prism'}"]') !== null`);
        await p.click('Start Mini conversation');
        await p.wait('document.getElementById("private-mini-prompt") !== null');
        await p.fill('#private-mini-prompt', 'Find learners with pending fees');
        await p.wait('document.getElementById("private-mini-prompt").closest("form").querySelector("button[type=submit]").disabled === false');
        await p.evaluate('Promise.all(document.getAnimations().filter(animation => animation.effect?.getComputedTiming().iterations !== Infinity).map(animation => animation.finished.catch(() => null)))');
        const measured = await p.evaluate(`(() => {
          const field = document.getElementById('private-mini-prompt');
          const panel = field.closest('section');
          const submit = panel.querySelector('button[type="submit"]');
          const available = [...panel.querySelectorAll('button')].find(x => x.textContent === 'Check Mini availability');
          const shell = document.querySelector('main[data-penta-theme]');
          const expected = getComputedStyle(shell);
          const values = element => { const c = getComputedStyle(element), r = element.getBoundingClientRect();
            return { height:r.height, background:c.backgroundColor, color:c.color, font:c.fontFamily,
              radius:c.borderRadius, border:c.borderTopWidth, left:r.left, right:r.right }; };
          const colour = token => { const element = document.createElement('span'); element.style.color = expected.getPropertyValue(token);
            shell.appendChild(element); const c = getComputedStyle(element).color; element.remove(); return c; };
          return { field:values(field), submit:values(submit), available:values(available),
            expectedAccent:colour('--pui-accent'), expectedOnAccent:colour('--pui-on-accent'),
            expectedControl:colour('--pui-control'), expectedText:colour('--pui-text'),
            legacyControl:expected.getPropertyValue('--ad-control-surface').trim(),
            control:expected.getPropertyValue('--pui-control').trim(),
            titleSize:getComputedStyle(document.querySelector('main h1')).fontSize,
            overflow:document.documentElement.scrollWidth>innerWidth+1 };
        })()`);
        const row = { width, theme, measured }; cases.push(row);
        await p.screenshot((baseline ? 'before' : 'after') + '-' + theme + '-' + width);
        if (!baseline) {
          for (const value of [measured.field, measured.submit, measured.available]) {
            a.ok(value.height >= 44, 'Touch target below44px'); a.ok(value.radius !== '0px', 'Unstyled control');
            a.match(value.font, /Geist/i, 'Control bypasses approved font');
            a.ok(value.left >= -1 && value.right <= width + 1, 'Control outside viewport');
          }
          a.equal(measured.field.background, measured.expectedControl);
          a.equal(measured.submit.background, measured.expectedAccent);
          a.equal(measured.submit.color, measured.expectedOnAccent);
          a.equal(measured.available.color, measured.expectedText);
          a.equal(measured.legacyControl, measured.control, 'Manual control token differs from PENTA');
          a.equal(measured.titleSize, width <= 760 ? '28px' : '30px');
          a.equal(measured.overflow, false);
        }
        await p.assertClean();
      } finally { if (p) await p.close(); }
    }
  } catch (error) { failure = error.message; }
  finally { cleanup = await browser.close(); }
  const result = { status: failure || cleanup.cleanupError ? 'FAIL' : 'PASS', baseline,
    cases, failure, cleanup, scope: 'Actual exported CSS, synthetic intercepted APIs; owner visual acceptance pending.' };
  fs.writeFileSync(path.join(browser.run, 'result.json'), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ status: result.status, cases: cases.length, baseline, evidence: browser.run, failure }));
  if (result.status !== 'PASS') process.exitCode = 1;
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
