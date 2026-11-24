'use strict';
// Actual exported components/browser, intercepted synthetic APIs. This is not
// model acceptance or an SQL/browser combined claim; SQL is verified separately.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { start } = require('./penta-browser-harness.cjs');
const root = path.resolve(__dirname, '../..');
const id = n => String(n).padStart(8,'0') + '-1111-4111-8111-111111111111';
const academy = id(1), session = id(2), teacher = id(3), student = id(4), expiry = '2030-01-01T00:00:00Z';
const base = `/api/academies/${academy}/penta/queries`;
const catalog = ['teacher', 'student', 'batch'].flatMap(entity => ['search', 'open'].map(kind => ({ key: `AcademyDesk.${entity}.${kind}`,
  entity, kind, readOnly: true, fields: entity === 'teacher' ? ['name', 'subject', 'status'] : ['name', 'status'] })));
function receipt(body, entity) {
  const record = entity === 'teacher' ? teacher : student;
  return { protocol: 'academy-query.v1', conversationId: session, requestId: body.requestId, version: body.expectedVersion + 1,
    kind: 'RESULT', message: `Verified ${entity} source`, inputText: body.text, context: { entity, filters: {}, resultIds: [record] },
    result: { entity, records: [{ id: record, label: entity === 'teacher' ? 'Nisha Teacher' : 'Mira Student', code: null, active: true,
      subjects: entity === 'teacher' ? ['Piano'] : [], sourcePath: entity === 'teacher' ? `/teacher-profile?teacherId=${record}` : `/student-management?studentId=${record}` }],
      hasMore: false, asOfUtc: '2026-11-24T01:00:00Z' } };
}
async function main() {
  const browser = await start({ academyId: academy, evidence: path.join(root,'.build-check/d67-query/browser'), prefix: 'penta-query-' });
  const results = []; let failure = null, cleanup;
  try {
    for (const role of ['Owner', 'AcademyAdmin']) for (const width of [320, 1440]) for (const theme of ['light', 'dark']) {
      const stored = new Map(); let p, posts = 0, recovery = 0;
      try {
        p = await browser.page({ role, workspace: 'AcademyAdmin', route: '/penta', manual: '/dashboard' }, width, async req => {
          if (req.path === base + '/health') return { body: { protocol: 'academy-query.v1', readOnly: true, tools: catalog, status: 'Available' } };
          if (req.path === base + '/conversations') return { status: 201, body: { conversationId: session, version: 0, expiresAtUtc: expiry } };
          if (req.path === base + `/conversations/${session}/turns`) {
            posts++; a.deepEqual(Object.keys(req.body).sort(), ['expectedVersion','requestId','text']);
            const value = receipt(req.body, posts === 1 ? 'teacher' : 'student'); stored.set(value.requestId, value);
            return posts === 1 ? { status: 201, body: value } : { fail: 'ConnectionReset' };
          }
          if (req.path.startsWith(base + `/conversations/${session}/turns/`)) { recovery++; return { body: stored.get(req.path.split('/').at(-1)) }; }
          if (req.path === base + `/conversations/${session}`) return { body: { conversationId: session, version: stored.size, expiresAtUtc: expiry,
            turns: [...stored.values()], pendingRequestId: null, hasMore: false } };
          return { status: 404 };
        }, `localStorage.setItem('academydesk.theme',${JSON.stringify(theme)});`);
        await p.click('Start conversation');
        await p.submit('#penta-academy-query', 'Show the teachers in my academy');
        await p.wait(`document.body?.innerText.includes('Nisha Teacher')`);
        a.equal(await p.evaluate(`document.querySelector('a[href="/teacher-profile?teacherId=${teacher}"]')?.textContent`), 'Nisha Teacher');
        a.doesNotMatch(await p.text(), /No students match/);
        await p.submit('#penta-academy-query', 'I said students');
        await p.wait(`document.body?.innerText.includes('The response is unconfirmed')`);
        await p.click('Recover saved response');
        await p.wait(`document.body?.innerText.includes('Mira Student')`);
        a.equal(posts, 2); a.equal(recovery, 1);
        await p.reload(); await p.wait(`document.body?.innerText.includes('Mira Student') && document.body?.innerText.includes('Nisha Teacher')`);
        a.equal(posts, 2); a.equal(recovery, 2);
        a.equal(await p.evaluate(`JSON.stringify(sessionStorage).includes('Nisha Teacher')`), false);
        const mode = await p.evaluate(`document.documentElement.getAttribute('data-theme')`); a.equal(mode, theme);
        const geometry = await p.evaluate(`(()=>{const t=document.querySelector('[aria-label="Conversation messages"]'),f=document.querySelector('#penta-academy-query').closest('form');return {separate:t.getBoundingClientRect().bottom<=f.getBoundingClientRect().top+1,scrollable:getComputedStyle(t).overflowY==='auto'};})()`);
        a.equal(geometry.separate, true, 'Composer must never overlap the transcript'); a.equal(geometry.scrollable, true);
        await p.assertClean(); await p.screenshot(`${role}-${width}-${theme}`);
        results.push({ role, width, theme, passed: true, turnPosts: posts, getRecoveries: recovery });
      } finally { if (p) await p.close(); }
    }
  } catch (error) { failure = error.message; }
  finally { cleanup = await browser.close(); }
  const result = { status: failure || cleanup.cleanupError ? 'FAIL' : 'PASS', results, failure, cleanup,
    scope: 'Actual browser/export, synthetic APIs only. No model, production data, physical device or combined SQL/browser qualification.' };
  fs.writeFileSync(path.join(browser.run,'result.json'),JSON.stringify(result,null,2));
  console.log(JSON.stringify({ status: result.status, cases: results.length, failure, evidence: browser.run, cleanup }));
  if (result.status !== 'PASS') process.exitCode = 1;
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
