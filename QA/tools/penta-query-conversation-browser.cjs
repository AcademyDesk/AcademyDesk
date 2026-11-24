'use strict';
// Actual exported components/browser, intercepted synthetic APIs. This is not
// model acceptance or an SQL/browser combined claim; SQL is verified separately.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { start } = require('./penta-browser-harness.cjs');
const root = path.resolve(__dirname, '../..');
const id = n => String(n).padStart(8,'0') + '-1111-4111-8111-111111111111';
const academy = id(1), session = id(2), teacher = id(3), student = id(4), expiry = '2030-01-01T00:00:00Z';
const base = `/api/academies/${academy}/penta/queries`;
const finance = process.env.QA_QUERY_FINANCE_EXPECTED === 'true';
const calendar = process.env.QA_QUERY_CALENDAR_EXPECTED === 'true';
const catalog = ['teacher', 'student', 'batch', ...(finance ? ['student-fees'] : []), ...(calendar ? ['session'] : [])].flatMap(entity => ['search', 'open'].map(kind => ({ key: `AcademyDesk.${entity}.${kind}`,
  entity, kind, readOnly: true, fields: entity === 'teacher' ? ['name', 'subject', 'status'] : entity === 'student-fees' ? ['name', 'subject', 'balance_status'] : entity === 'session' ? ['name','teacher','from','through','status'] : ['name', 'status'] })));
function receipt(body, entity) {
  const record = entity === 'teacher' ? teacher : student;
  return { protocol: 'academy-query.v1', conversationId: session, requestId: body.requestId, version: body.expectedVersion + 1,
    kind: 'RESULT', message: `Verified ${entity} source`, inputText: body.text, context: { entity, filters: entity === 'session' ? {from:'2026-11-24',through:'2026-11-24'} : {}, resultIds: [record] },
    result: { entity, ...(entity === 'student-fees' ? { balanceScope: 'All outstanding fees for each student; subject filters select students, not course-specific invoices.' } : {}),
      ...(entity === 'session' ? {calendarScope:{timeZone:'Asia/Kolkata',from:'2026-11-24',through:'2026-11-24'}} : {}),
      records: [{ id: record, label: entity === 'session' ? 'Synthetic Calendar Batch' : entity === 'teacher' ? 'Nisha Teacher' : 'Mira Student', code: null, active: true,
      ...(entity === 'session' ? {schedule:{startUtc:'2026-11-23T18:30:00Z',endUtc:'2026-11-23T19:30:00Z',deliveryMode:'Hybrid',status:'Scheduled',teacher:'Nisha Teacher'}} : {}),
      ...(entity === 'student-fees' ? { balances: [{ currency: 'INR', outstandingExact: '400.00' }, { currency: 'USD', outstandingExact: '0.01' }, { currency: 'EUR', outstandingExact: '90071992547409.91' }] } : {}),
      subjects: entity === 'teacher' ? ['Piano'] : [], sourcePath: entity === 'session' ? '/schedule' : entity === 'teacher' ? `/teacher-profile?teacherId=${record}` : `/student-management?studentId=${record}` }],
      hasMore: false, asOfUtc: '2026-11-24T01:00:00Z' } };
}
async function main() {
  const browser = await start({ academyId: academy, evidence: path.join(root, calendar ? '.build-check/d69-calendar/browser' : finance ? '.build-check/d68-finance/browser' : '.build-check/d68-finance/directory-browser'), prefix: 'penta-query-' });
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
            const value = receipt(req.body, calendar && req.body.text.includes('classes') ? 'session' : posts === 1 ? 'teacher' : posts === 3 && finance ? 'student-fees' : 'student'); stored.set(value.requestId, value);
            return posts === 2 ? { fail: 'ConnectionReset' } : { status: 201, body: value };
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
        if (finance) {
          await p.submit('#penta-academy-query', 'What does she owe?');
          await p.wait(`document.body?.innerText.includes('EUR 90071992547409.91')`);
          const exact = await p.evaluate(`document.querySelector('[aria-label="Exact outstanding balances"]')?.textContent`);
          a.ok(exact.includes('INR 400.00') && exact.includes('USD 0.01') && exact.includes('EUR 90071992547409.91'));
          await p.reload(); await p.wait(`document.body?.innerText.includes('EUR 90071992547409.91')`);
          await p.wait(`(()=>{const area=document.querySelector('[aria-label="Conversation messages"]').getBoundingClientRect();return [...document.querySelectorAll('[aria-label="Exact outstanding balances"] li')].every(n=>{const r=n.getBoundingClientRect();return r.top>=area.top-1&&r.bottom<=area.bottom+1;});})()`);
          a.equal(posts, 3); a.equal(recovery, 3);
          a.equal(await p.evaluate(`JSON.stringify(sessionStorage).includes('90071992547409.91')`), false);
        }
        if (calendar) {
          const beforePosts=posts;
          await p.submit('#penta-academy-query','Show Nisha classes on 2026-11-24');
          await p.wait(`document.body?.innerText.includes('Synthetic Calendar Batch')`);
          a.equal(await p.evaluate(`document.querySelector('[aria-label="Verified class time"] time')?.getAttribute('datetime')`),'2026-11-23T18:30:00Z');
          a.ok((await p.evaluate(`document.querySelector('[aria-label="Verified class time"]')?.textContent`)).includes('Asia/Kolkata · Hybrid · Scheduled'));
          await p.reload(); await p.wait(`document.body?.innerText.includes('Synthetic Calendar Batch')`);
          a.equal(posts,beforePosts+1);
          await p.wait(`(()=>{const area=document.querySelector('[aria-label="Conversation messages"]').getBoundingClientRect();const r=document.querySelector('[aria-label="Verified class time"]').getBoundingClientRect();return r.top>=area.top-1&&r.bottom<=area.bottom+1;})()`);
          a.equal(await p.evaluate(`document.querySelector('a[href="/schedule"]')?.textContent`),'Open Manual schedule');
        }
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
    finance, scope: 'Actual browser/export, synthetic APIs only. No model, production data, physical device or combined SQL/browser qualification.' };
  fs.writeFileSync(path.join(browser.run,'result.json'),JSON.stringify(result,null,2));
  console.log(JSON.stringify({ status: result.status, cases: results.length, failure, evidence: browser.run, cleanup }));
  if (result.status !== 'PASS') process.exitCode = 1;
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
