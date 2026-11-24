'use strict';
// Real exported shell and native dialogs with intercepted synthetic APIs only.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { start } = require('./penta-browser-harness.cjs');
const root = path.resolve(__dirname, '../..'), academy = '00000001-1111-4111-8111-111111111111';
const roles = [
  { role: 'Owner', workspace: 'AcademyAdmin', route: '/penta', manual: '/dashboard' },
  { role: 'AcademyAdmin', workspace: 'AcademyAdmin', route: '/penta', manual: '/dashboard' },
  { role: 'Teacher', workspace: 'Teacher', route: '/teacher/ai', manual: '/teacher' },
  { role: 'Student', workspace: 'Portal', route: '/portal/ai', manual: '/portal' },
  { role: 'Guardian', workspace: 'Portal', route: '/portal/ai', manual: '/portal' },
  { role: 'PlatformOwner', workspace: 'Platform', route: '/platform/ai', manual: '/platform' },
];
async function main() {
  const browser = await start({ academyId: academy, evidence: path.join(root, '.build-check/penta-workspace-shell'), prefix: 'penta-shell-' });
  const cases = []; let failure = null, cleanup;
  try {
    for (const actor of roles) for (const width of [320, 768, 1440]) for (const theme of ['dark', 'light']) {
      let p;
      try {
        p = await browser.page(actor, width, async req => {
          if (req.path.endsWith('/lookup-readiness')) return { body: { protocol: 'academy-lookup.v1', academyId: academy } };
          if (req.path.endsWith('/penta/chat/health')) return { body: { status: 'Available', provider: 'PENTA Mini', protocol: '0.1', readOnly: true } };
          if (req.path.endsWith('/penta/chat/conversations')) return { status: 201, body: { conversationId: '00000002-1111-4111-8111-111111111111', version: 0, expiresAtUtc: '2030-01-01T00:00:00Z' } };
          return { status: 404 };
        }, `localStorage.setItem('academydesk.theme',${JSON.stringify(theme)});`);
        await p.wait(`Boolean(document.querySelector('nav[aria-label="PENTA capabilities"]'))`);
        await p.wait(`Boolean(document.querySelector('[data-penta-theme="${theme === 'dark' ? 'orbit' : 'prism'}"]'))`);
        const before = p.requests.length;
        await p.evaluate(`document.querySelector('button[aria-label="About Pulse"]').click()`);
        await p.wait('Boolean(document.querySelector("dialog[open]"))');
        const popup = await p.evaluate('(()=>{const d=document.querySelector("dialog[open]"),r=d.getBoundingClientRect();return{text:d.textContent,left:r.left,right:r.right,inside:d.contains(document.activeElement)};})()');
        a.match(popup.text, /not connected/); a.equal(popup.inside, true); a.ok(popup.left >= 0 && popup.right <= width + 1);
        await p.evaluate(`document.querySelector('dialog[open] button[aria-label="Close Pulse"]').click()`);
        await p.wait('!document.querySelector("dialog[open]")');
        a.equal(await p.evaluate('document.activeElement?.getAttribute("aria-label")'), 'About Pulse');
        a.equal(p.requests.slice(before).filter(req => req.method === 'POST').length, 0, 'Capability help dispatched a tool');
        if(width===320){
          await p.evaluate(`document.querySelector('button[aria-label="Open workspace navigation"]').click()`);
          await p.wait('Boolean(document.querySelector("dialog[open]"))');
          await p.click('Model · MINI');
        }else await p.evaluate(`document.querySelector('button[aria-label="Choose PENTA model"]').click()`);
        await p.wait(`document.querySelector('dialog[open]')?.textContent.includes('ULTRA')`);
        const tiers=await p.evaluate(`Array.from(document.querySelectorAll('dialog[open] button[aria-pressed]')).map(n=>({text:n.textContent,disabled:n.disabled}))`);
        a.equal(tiers.length,4);a.ok(tiers.filter(t=>!t.text.startsWith('MINI')).every(t=>t.disabled));
        a.match((await p.text()),/Not connected/);a.match((await p.text()),/Deferred/);
        await p.evaluate(`document.querySelector('dialog[open] button[aria-label="Close Choose your PENTA model"]').click()`);
        await p.wait('!document.querySelector("dialog[open]")');
        a.equal(p.requests.slice(before).filter(req=>req.method==='POST').length,0,'Model selection dispatched or activated a model');
        const capabilityState = await p.evaluate(`Array.from(document.querySelectorAll('nav[aria-label="PENTA capabilities"] > div > button:first-child')).map(n=>({name:n.textContent,disabled:n.disabled}))`);
        a.equal(capabilityState.length, 5);
        a.ok(capabilityState.filter(n => !n.name.startsWith('Executor')).every(n => n.disabled));
        a.equal(await p.evaluate(`document.querySelector('nav[aria-label="Work view"] a').getAttribute('href')`), actor.manual);
        if (width === 320) {
          await p.evaluate(`document.querySelector('button[aria-label="Open workspace navigation"]').click()`);
          await p.wait('Boolean(document.querySelector("dialog[open]"))');
          a.equal(await p.evaluate(`document.querySelector('dialog[open] a').getAttribute('href')`), actor.manual);
          await p.evaluate(`document.querySelector('dialog[open] button[aria-label="Close Your workspace"]').click()`);
          await p.wait('!document.querySelector("dialog[open]")');
        }
        if (actor.workspace === 'AcademyAdmin') {
          await p.click('Start Mini conversation');
          await p.wait('Boolean(document.querySelector("textarea#private-mini-prompt"))');
          a.equal(await p.evaluate('Array.from(document.querySelectorAll("#penta-conversation form")).filter(n=>n.checkVisibility()).length'), 1, 'Competing visible composers');
        } else a.equal(p.requests.filter(req => req.path.includes('/penta/chat/')).length, 0, 'Mini contacted for an excluded role');
        const layout = await p.evaluate('(()=>{const main=document.querySelector("main"),h=document.querySelector("main h1");return{pageFits:document.documentElement.scrollWidth<=innerWidth+1,viewport:main.getBoundingClientRect().height<=innerHeight+1,font:getComputedStyle(h).fontFamily};})()');
        a.equal(layout.pageFits, true); a.equal(layout.viewport, true); a.match(layout.font, /Geist/);
        if(width===1440){
          const columns=await p.evaluate(`(()=>{const l=document.querySelector('aside[aria-label="Workspace navigation"]').getBoundingClientRect(),c=document.querySelector('section[aria-label="PENTA workspace conversation"]').getBoundingClientRect(),r=document.querySelector('aside[aria-label="Workspace context"]').getBoundingClientRect();return{left:l.width,centre:c.width,right:r.width,ordered:l.right<=c.left+1&&c.right<=r.left+1};})()`);
          a.ok(columns.left>=200&&columns.centre>=400&&columns.right>=300&&columns.ordered,'Enterprise three-panel layout must remain usable');
        }
        await p.assertClean();
        await p.screenshot(actor.role + '-' + theme + '-' + width);
        cases.push({ role: actor.role, width, theme, passed: true });
      } finally { if (p) await p.close(); }
    }
  } catch (error) { failure = error.message; }
  finally { cleanup = await browser.close(); }
  const result = { status: failure || cleanup.cleanupError ? 'FAIL' : 'PASS', cases, failure, cleanup,
    scope: '36 synthetic role/theme/viewport shell and dialog cases; no model/SQL/phone or owner visual acceptance.' };
  fs.writeFileSync(path.join(browser.run, 'result.json'), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ status: result.status, cases: cases.length, evidence: browser.run, failure }));
  if (result.status !== 'PASS') process.exitCode = 1;
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
