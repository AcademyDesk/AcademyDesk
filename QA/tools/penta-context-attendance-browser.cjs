// Synthetic host effects in actual exported React/Chromium. No SQL/model acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { start, sleep } = require('./penta-browser-harness.cjs');
const root = path.resolve(__dirname, '../..');
const id = n => String(n).padStart(8, '0') + '-1111-4111-8111-111111111111';
const academy = id(1), mira = id(2), leo = id(3), lesson = id(4), batch = id(5);
const epoch = 'synthetic-c52-epoch', pointerKey = 'academydesk.penta.teacher-attendance-recovery.v1';
const asOf = '2026-11-18T09:00:00Z', future = '2026-11-20T09:00:00Z';
const actors = {
  AcademyAdmin: { role: 'AcademyAdmin', workspace: 'AcademyAdmin', route: '/penta', manual: '/dashboard' },
  Owner: { role: 'Owner', workspace: 'AcademyAdmin', route: '/penta', manual: '/dashboard' },
  Guardian: { role: 'Guardian', workspace: 'Portal', route: '/portal/ai', manual: '/portal' },
  Student: { role: 'Student', workspace: 'Portal', route: '/portal/ai', manual: '/portal' },
  Teacher: { role: 'Teacher', workspace: 'Teacher', route: '/teacher/ai', manual: '/teacher' },
};
const academyCases = ['single-pronoun', 'first-match', 'second-match', 'ambiguous-pronoun', 'out-of-range',
  'select-before-search', 'refine', 'switch-domain', 'empty-then-select', 'malformed-then-select',
  'denied-then-select', 'unsupported-resets-context', 'expired-context', 'duplicate-ids', 'invalid-source', 'html-as-text'];
const studentCases = ['single-class', 'exact-fees', 'due-work', 'finance-revoked', 'academic-revoked',
  'foreign-student', 'wrong-kind', 'invalid-money', 'invalid-currency', 'zero-money', 'bad-due-date', 'untrusted-html'];
const guardianCases = [...studentCases, 'ambiguous-choice', 'explicit-learner', 'unknown-name', 'duplicate-names',
  'selected-followup', 'corrected-name', 'link-revoked', 'choice-revoked'];
const teacherCases = ['present', 'absent', 'note', 'trim-note', 'prepare-only', 'prepared-reload', 'lost-prepare',
  'lost-confirm', 'receipt-reload', 'duplicate-confirm', 'prepare-storage-failure', 'prepare-wrong-learner',
  'prepare-wrong-class', 'prepare-wrong-status', 'confirm-wrong-hash', 'confirm-wrong-receipt',
  'confirm-wrong-notice-count', 'expired-review', 'recover-404', 'recover-409', 'recover-410',
  'foreign-epoch-pointer', 'foreign-academy-pointer', 'corrupt-pointer'];
const includes = text => `document.body?.innerText.includes(${JSON.stringify(text)})`;
const literal = '<img src=x onerror="window.__c52HtmlExecuted=true">';
const copy = value => JSON.parse(JSON.stringify(value));

async function academyCase(browser, actor, width, scenario) {
  const apiRoot = '/api/academies/' + academy + '/penta'; let searches = 0;
  const p = await browser.page(actor, width, req => {
    a.equal(req.method, 'GET', 'Lookup cannot mutate');
    if (req.path === apiRoot + '/lookup-readiness') return { body: { protocol: 'academy-lookup.v1', academyId: academy } };
    if ([apiRoot + '/chat/finance', apiRoot + '/class-scheduling/browser/readiness'].includes(req.path)) return { status: 404 };
    if ([apiRoot + '/student-search', apiRoot + '/batch-search'].includes(req.path)) {
      searches++; const kind = req.path.endsWith('/student-search') ? 'student' : 'batch';
      a.ok(['Mira', 'Leo', 'Piano'].includes(req.query.q));
      if (scenario === 'denied-then-select') return { status: 403 };
      const many = ['first-match', 'second-match', 'ambiguous-pronoun', 'out-of-range', 'refine', 'duplicate-ids'].includes(scenario);
      const student = (sourceId, displayName) => ({ sourceId, displayName, isActive: true, sourcePath: '/student-management?studentId=' + sourceId });
      let rows = kind === 'batch' ? [{ sourceId: batch, name: 'C52 Piano', sourcePath: '/batch-setup' }]
        : [student(req.query.q === 'Leo' ? leo : mira, scenario === 'html-as-text' ? literal : req.query.q === 'Leo' ? 'C52 Leo' : 'C52 Mira One')];
      if (kind === 'student' && req.query.q === 'Mira' && many) rows.push(student(scenario === 'duplicate-ids' ? mira : leo, 'C52 Mira Two'));
      if (scenario === 'empty-then-select') rows = [];
      if (scenario === 'invalid-source') rows[0].sourcePath = '/platform/control';
      return { body: { academyId: scenario === 'malformed-then-select' ? id(99) : academy, tool: kind + '.search.v1', capability: 'executor',
        source: kind === 'student' ? 'Students' : 'Batches', maxRows: 10, hasMore: false, asOfUtc: asOf, timeZone: 'UTC', rows } };
    }
  });
  try {
    await p.wait(`Boolean(document.querySelector('#finance-chat-prompt'))`);
    const ask = text => p.submit('#finance-chat-prompt', text);
    const selected = `document.body?.innerText.includes('Selected record')`;
    if (scenario === 'select-before-search') {
      await ask('Open the first result'); await p.wait(includes('Find a student or batch first')); a.equal(searches, 0);
    } else {
      await ask('Find student Mira');
      const bad = ['malformed-then-select', 'denied-then-select', 'duplicate-ids', 'invalid-source'].includes(scenario);
      await p.wait(includes(bad ? 'could not verify' : scenario === 'empty-then-select' ? 'No matching records' : 'Matching students'));
      if (bad || scenario === 'empty-then-select') {
        await ask('Open the first result'); await p.wait(includes('Find a student or batch first')); a.equal(await p.evaluate(selected), false);
      } else if (scenario === 'ambiguous-pronoun' || scenario === 'out-of-range') {
        await ask(scenario === 'ambiguous-pronoun' ? 'Open it' : 'Open the third result');
        await p.wait(includes('Which result did you mean')); a.equal(await p.evaluate(selected), false);
      } else if (scenario === 'unsupported-resets-context' || scenario === 'expired-context') {
        if (scenario === 'unsupported-resets-context') { await ask('Activate models'); await p.wait(includes('That request is unavailable here')); }
        else await p.evaluate('(()=>{const before=Date.now;Date.now=()=>before()+300001;})()');
        await ask('Open the first result'); await p.wait(includes('Find a student or batch first')); a.equal(await p.evaluate(selected), false);
      } else {
        let expected = mira, expectedLabel = 'C52 Mira One', expectedPath = '/student-management?studentId=' + mira;
        if (scenario === 'refine') { await ask('Try Leo'); await p.wait(includes('C52 Leo')); expected = leo; expectedLabel = 'C52 Leo'; }
        if (scenario === 'switch-domain') { await ask('Look up batch Piano'); await p.wait(includes('Matching batches')); expectedPath = '/batch-setup'; expectedLabel = 'C52 Piano'; }
        else expectedPath = '/student-management?studentId=' + expected;
        if (scenario === 'second-match') { expectedPath = '/student-management?studentId=' + leo; expectedLabel = 'C52 Mira Two'; }
        if (scenario === 'html-as-text') expectedLabel = literal;
        await ask(scenario === 'single-pronoun' ? 'Open it' : scenario === 'second-match' ? 'Open the second result' : 'Open the first result');
        await p.wait(selected);
        a.equal(await p.evaluate(`Array.from(document.querySelectorAll('a')).some(n=>n.getAttribute('href')===${JSON.stringify(expectedPath)}&&n.textContent===${JSON.stringify(expectedLabel)})`), true);
        a.equal(await p.evaluate('Boolean(window.__c52HtmlExecuted)'), false);
        if (scenario === 'single-pronoun') await p.screenshot(actor.role + '-' + width + '-context');
      }
      a.equal(searches, ['refine', 'switch-domain'].includes(scenario) ? 2 : 1);
    }
    return { p, facts: { searches, domainEffects: 0 } };
  } catch (e) { e.page = p; throw e; }
}

async function portalCase(browser, actor, width, scenario) {
  const initial = { id: mira, name: 'Mira Learner', academic: scenario !== 'academic-revoked', finance: scenario !== 'finance-revoked' };
  const second = { id: leo, name: scenario === 'duplicate-names' ? 'Mira Learner' : 'Leo Learner', academic: true, finance: true };
  const multiple = ['ambiguous-choice', 'explicit-learner', 'unknown-name', 'duplicate-names', 'selected-followup', 'corrected-name', 'link-revoked', 'choice-revoked'].includes(scenario);
  a.ok(!multiple || actor.role === 'Guardian', 'Do not invent multi-learner Student scope');
  let learners = multiple ? [initial, second] : [initial], listReads = 0; const reads = [];
  const p = await browser.page(actor, width, req => {
    a.equal(req.method, 'GET', 'Portal conversation cannot mutate');
    if (req.path === '/api/portal/penta/learners') { listReads++; return { body: { protocol: 'portal-reads.v1', academyId: academy, learners, hasMore: false } }; }
    const m = /^\/api\/portal\/penta\/students\/([^/]+)\/(next-class|due-work|outstanding-fees)$/.exec(req.path);
    if (m) {
      const student = m[1], kind = m[2]; a.ok(learners.some(x => x.id === student), 'Only currently linked student may be read'); reads.push({ student, kind });
      const name = student === mira ? 'Mira' : 'Leo';
      const row = kind === 'next-class' ? { batchName: scenario === 'untrusted-html' ? literal : 'C52 class ' + name, startUtc: future, endUtc: '2026-11-20T10:00:00Z', deliveryMode: 'InPerson' }
        : kind === 'due-work' ? { title: 'C52 practice ' + name, dueAtUtc: future }
          : { invoiceNumber: 'C52-INV-' + name, currency: 'USD', balance: '9999999999999999999999.99', dueDate: '2026-12-01' };
      if (scenario === 'invalid-money') row.balance = 123.45;
      if (scenario === 'zero-money') row.balance = '0.00';
      if (scenario === 'invalid-currency') row.currency = 'USD/INR';
      if (scenario === 'bad-due-date') row.dueDate = '2026-02-30';
      return { body: { academyId: academy, studentId: scenario === 'foreign-student' ? leo : student,
        kind: scenario === 'wrong-kind' ? 'due-work' : kind, asOfUtc: asOf, sourcePath: '/portal', rows: [row] } };
    }
  });
  try {
    await p.wait(`Boolean(document.querySelector('#portal-penta-prompt'))`);
    const ask = text => p.submit('#portal-penta-prompt', text);
    const choices = () => p.wait(includes('Which linked learner should I check'));
    const classFor = name => p.wait(includes('C52 class ' + name));
    if (['ambiguous-choice', 'unknown-name', 'duplicate-names', 'choice-revoked'].includes(scenario)) {
      await ask(scenario === 'unknown-name' ? 'When is my next class for Zara?' : scenario === 'duplicate-names' ? 'When is my next class for Mira?' : 'When is my next class?');
      await choices(); a.equal(reads.length, 0);
      if (scenario === 'ambiguous-choice') { await p.click('Choose Leo Learner'); await classFor('Leo'); a.deepEqual(reads, [{ student: leo, kind: 'next-class' }]); }
      if (scenario === 'choice-revoked') { learners = [second]; await p.click('Choose Mira Learner'); await choices(); a.equal(reads.length, 0); a.equal(await p.evaluate(includes('Choose Mira Learner')), false); }
    } else if (scenario === 'explicit-learner') {
      await ask('When is my next class for Leo?'); await classFor('Leo'); a.equal(reads[0].student, leo);
    } else if (['selected-followup', 'corrected-name', 'link-revoked'].includes(scenario)) {
      await ask('When is my next class for Mira?'); await classFor('Mira');
      if (scenario === 'selected-followup') { await ask('What work is due?'); await p.wait(includes('C52 practice Mira')); a.deepEqual(reads.map(x => x.student), [mira, mira]); }
      if (scenario === 'corrected-name') { await ask('When is my next class for Leo?'); await classFor('Leo'); a.equal(await p.evaluate(includes('C52 class Mira')), false); a.deepEqual(reads.map(x => x.student), [mira, leo]); }
      if (scenario === 'link-revoked') { learners = [second]; await ask('What work is due?'); await choices(); a.equal(reads.length, 1); a.equal(await p.evaluate(includes('C52 class Mira')), false); await p.click('Choose Leo Learner'); await p.wait(includes('C52 practice Leo')); a.deepEqual(reads.map(x => x.student), [mira, leo]); }
    } else {
      const fees = ['exact-fees', 'finance-revoked', 'invalid-money', 'invalid-currency', 'zero-money', 'bad-due-date'].includes(scenario);
      await ask(fees ? 'How much do I owe?' : scenario === 'due-work' ? 'What work is due?' : 'When is my next class?');
      if (scenario.endsWith('-revoked')) { await p.wait(includes('does not have access to that information')); a.equal(reads.length, 0); }
      else if (['foreign-student', 'wrong-kind', 'invalid-money', 'invalid-currency', 'zero-money', 'bad-due-date'].includes(scenario)) {
        await p.wait(includes('PENTA could not verify this information')); a.equal(await p.evaluate(`Boolean(document.querySelector('section[aria-label="Outstanding fees"],section[aria-label="Next class"]'))`), false); a.equal(reads.length, 1);
      } else {
        await p.wait(includes(scenario === 'exact-fees' ? 'USD 9999999999999999999999.99' : scenario === 'due-work' ? 'C52 practice Mira' : scenario === 'untrusted-html' ? literal : 'C52 class Mira'));
        a.equal(reads.length, 1); a.equal(reads[0].student, mira); a.equal(await p.evaluate('Boolean(window.__c52HtmlExecuted)'), false);
        if (scenario === 'exact-fees') await p.screenshot(actor.role + '-' + width + '-exact-fees');
      }
    }
    a.ok(listReads >= 2, 'Each turn rechecks current linkage');
    return { p, facts: { listReads, reads, domainEffects: 0 } };
  } catch (e) { e.page = p; throw e; }
}

async function teacherCase(browser, actor, width, scenario) {
  const base = '/api/teacher/penta/attendance', status = scenario === 'absent' ? 'Absent' : 'Present';
  const note = ['note', 'trim-note'].includes(scenario) ? 'C52 reviewed note' : null;
  const state = { prepares: 0, confirms: 0, effects: 0, queued: 0, recoveries: 0, draft: null, held: null };
  let requestId;
  const pointer = { epoch, academyId: academy, sessionId: lesson, requestId: id(90) };
  let bootstrap = '';
  if (scenario === 'foreign-epoch-pointer') pointer.epoch = 'foreign-epoch';
  if (scenario === 'foreign-academy-pointer') pointer.academyId = id(99);
  if (scenario.endsWith('-pointer')) bootstrap = `sessionStorage.setItem(${JSON.stringify(pointerKey)},${JSON.stringify(scenario === 'corrupt-pointer' ? '{broken' : JSON.stringify(pointer))});`;
  if (scenario === 'prepare-storage-failure') bootstrap = `(()=>{const old=Storage.prototype.setItem;Storage.prototype.setItem=function(k,v){if(k===${JSON.stringify(pointerKey)})throw new DOMException('Synthetic blocked storage','QuotaExceededError');return old.call(this,k,v);};})()`;
  const p = await browser.page(actor, width, async req => {
    if (req.path === base + '/readiness') { a.equal(req.method, 'GET'); return { body: { protocol: 'teacher-attendance.v1', academyId: academy, requiresConfirmation: true } }; }
    if (req.path === base + '/sessions') { a.equal(req.method, 'GET'); a.equal(req.query.q, 'Piano'); return { body: { academyId: academy, sessionId: null, hasMore: false, rows: [{ id: lesson, batchId: batch, batchName: 'C52 Piano', startUtc: future }] } }; }
    if (req.path === base + '/sessions/' + lesson + '/roster') { a.equal(req.method, 'GET'); a.equal(req.query.q, 'Mira'); return { body: { academyId: academy, sessionId: lesson, hasMore: false, rows: [{ id: mira, name: 'Mira Learner' }] } }; }
    const m = new RegExp('^' + base + '/sessions/([^/]+)/drafts/([^/]+)(/confirm)?$').exec(req.path);
    if (!m) return;
    a.equal(m[1], lesson); a.match(m[2], /^[0-9a-f-]{36}$/);
    if (req.method === 'PUT' && !m[3]) {
      state.prepares++; a.equal(state.prepares, 1); requestId = m[2];
      a.deepEqual(req.body, { payload: { tool_key: 'AcademyDesk.TeacherAttendance.Mark', tool_revision: 1,
        fields: { StudentId: { kind: 'reference', value: mira }, Status: { kind: 'text', value: status }, ...(note ? { Notes: { kind: 'text', value: note } } : {}) } } });
      state.draft = { requestId, classSessionId: lesson, batchId: batch, sessionStartUtc: future,
        expiresAtUtc: scenario === 'expired-review' ? '2020-01-01T00:00:00Z' : '2030-01-01T00:00:00Z', payloadHash: 'a'.repeat(64), status: 'Prepared', receipt: null,
        change: { studentId: mira, status, notes: note, inAppMessage: 'Your attendance for the class has been marked ' + status + '.' } };
      if (scenario === 'lost-prepare' || scenario.startsWith('recover-')) return { fail: 'ConnectionReset' };
      const data = copy(state.draft);
      if (scenario === 'prepare-wrong-learner') data.change.studentId = leo;
      if (scenario === 'prepare-wrong-class') data.classSessionId = id(99);
      if (scenario === 'prepare-wrong-status') { data.change.status = 'Absent'; data.change.inAppMessage = 'Your attendance for the class has been marked Absent.'; }
      return { status: 201, body: data };
    }
    a.equal(m[2], requestId, 'Recovery must bind the same saved request');
    if (req.method === 'POST' && m[3]) {
      state.confirms++; a.equal(state.confirms, 1); a.deepEqual(req.body, { payloadHash: 'a'.repeat(64), intent: 'ConfirmTeacherAttendanceAndQueueInApp' });
      state.effects++; state.queued++; state.draft = { ...state.draft, status: 'Succeeded', receipt: {
        id: id(6), auditId: id(7), attendanceId: id(8), markedAtUtc: asOf, inAppQueued: 1 } };
      if (scenario === 'lost-confirm') return { fail: 'ConnectionReset' };
      if (scenario === 'duplicate-confirm') await new Promise(resolve => { const timer = setTimeout(resolve, 8000); state.held = () => { clearTimeout(timer); resolve(); }; });
      const data = copy(state.draft);
      if (scenario === 'confirm-wrong-hash') data.payloadHash = 'b'.repeat(64);
      if (scenario === 'confirm-wrong-receipt') data.receipt.id = 'invalid';
      if (scenario === 'confirm-wrong-notice-count') data.receipt.inAppQueued = 2;
      return { status: 201, body: data };
    }
    a.equal(req.method, 'GET'); a.equal(Boolean(m[3]), false); state.recoveries++;
    return scenario.startsWith('recover-') ? { status: Number(scenario.slice(8)) } : { body: state.draft };
  }, bootstrap);
  try {
    await p.wait(`Boolean(document.querySelector('#penta-attendance-prompt'))`);
    if (scenario.endsWith('-pointer')) {
      a.equal(await p.evaluate(`sessionStorage.getItem(${JSON.stringify(pointerKey)})`), null);
      a.equal(state.prepares + state.confirms + state.recoveries, 0);
      return { p, facts: state };
    }
    await p.submit('#penta-attendance-prompt', 'Mark attendance for Piano');
    await p.wait(`Array.from(document.querySelectorAll('button')).some(n=>n.textContent.startsWith('Choose C52 Piano'))`);
    await p.evaluate(`Array.from(document.querySelectorAll('button')).find(n=>n.textContent.startsWith('Choose C52 Piano')).click()`);
    await p.submit('#penta-attendance-learner', 'Mira', 'Find learner'); await p.click('Choose Mira Learner');
    if (status === 'Absent') await p.evaluate(`(()=>{const n=document.querySelector('#penta-attendance-mark');n.value='Absent';n.dispatchEvent(new Event('change',{bubbles:true}));})()`);
    if (note) await p.fill('#penta-attendance-notes', scenario === 'trim-note' ? '  ' + note + '  ' : note);
    a.equal(state.prepares + state.confirms + state.effects, 0, 'Selection cannot prepare or apply an attendance mark');
    await p.click('Review attendance');
    if (scenario === 'prepare-storage-failure') { await p.wait(includes('Nothing was sent because recovery details could not be saved')); a.equal(state.prepares, 0); return { p, facts: state }; }
    const unknown = scenario === 'lost-prepare' || scenario.startsWith('recover-') || scenario.startsWith('prepare-wrong-');
    if (unknown) await p.wait(includes('The outcome is unconfirmed')); else await p.wait(includes('Review exact attendance'));
    a.equal(state.prepares, 1); a.equal(state.confirms + state.effects, 0, 'Preparation alone never applies attendance');
    const saved = await p.evaluate(`JSON.parse(sessionStorage.getItem(${JSON.stringify(pointerKey)}))`);
    a.deepEqual(saved, { epoch, academyId: academy, sessionId: lesson, requestId });
    if (scenario.startsWith('recover-')) {
      await p.click('Check saved attendance'); await p.wait(includes('This request could not be recovered'));
      a.equal(await p.evaluate(`(${p.button('Dismiss review')}).disabled`), true);
      await p.evaluate(`Array.from(document.querySelectorAll('input[type="checkbox"]')).find(n=>n.closest('label')?.textContent.includes('I checked Manual Attendance')).click()`);
      await p.click('Dismiss review'); await p.wait(`Boolean(document.querySelector('#penta-attendance-prompt'))`);
      a.equal(await p.evaluate(`sessionStorage.getItem(${JSON.stringify(pointerKey)})`), null); a.equal(state.effects + state.confirms, 0);
      return { p, facts: state };
    }
    if (unknown) { await p.click('Check saved attendance'); await p.wait(includes('Review exact attendance')); a.equal(state.recoveries, 1); }
    if (scenario === 'prepared-reload') {
      const before = p.requests.length; await p.reload(); await p.wait(includes('Review exact attendance'));
      a.ok(p.requests.slice(before).every(r => r.method === 'GET')); a.equal(state.prepares, 1); a.equal(state.confirms, 0);
    }
    if (scenario === 'prepare-only' || scenario === 'prepared-reload' || scenario.startsWith('prepare-wrong-') || scenario === 'lost-prepare') return { p, facts: state };
    await p.click('Confirm mark and queue notice');
    if (scenario === 'duplicate-confirm') {
      for (let i = 0; i < 100 && !state.held; i++) await sleep(25);
      a.ok(state.held); await p.evaluate(`(()=>{const b=Array.from(document.querySelectorAll('button')).find(n=>n.textContent==='Confirm mark and queue notice');if(b)for(let i=0;i<5;i++)b.click();})()`); state.held();
    }
    if (scenario === 'expired-review') { await p.wait(includes('This review has expired')); a.equal(state.confirms + state.effects, 0); return { p, facts: state }; }
    if (scenario === 'lost-confirm' || scenario.startsWith('confirm-wrong-')) {
      await p.wait(includes('The outcome is unconfirmed')); a.equal(await p.evaluate(includes('Attendance saved')), false);
      const before = p.requests.length; await p.click('Check saved attendance'); await p.wait(includes('Attendance saved'));
      a.ok(p.requests.slice(before).every(r => r.method === 'GET'));
    } else await p.wait(includes('Attendance saved'));
    if (scenario === 'receipt-reload') {
      const before = p.requests.length; await p.reload(); await p.wait(includes('Attendance saved'));
      a.ok(p.requests.slice(before).every(r => r.method === 'GET'));
    }
    a.equal(state.prepares, 1); a.equal(state.confirms, 1); a.equal(state.effects, 1); a.equal(state.queued, 1);
    await p.wait(includes('One in-app notice queued. Receipt')); a.equal(await p.evaluate(includes('delivered')), false);
    if (scenario === 'present') await p.screenshot(actor.role + '-' + width + '-receipt');
    return { p, facts: { ...state, held: undefined } };
  } catch (e) { state.held?.(); e.page = p; throw e; }
}

async function main() {
  const browser = await start({ academyId: academy, evidence: path.join(root, '.build-check/penta-context-attendance-browser'), prefix: 'c52-context-' });
  const results = []; let failure = null, cleanup;
  const matrix = [
    ...['AcademyAdmin', 'Owner'].flatMap(role => academyCases.map(scenario => ({ role, scenario, family: 'academy' }))),
    ...studentCases.map(scenario => ({ role: 'Student', scenario, family: 'portal' })),
    ...guardianCases.map(scenario => ({ role: 'Guardian', scenario, family: 'portal' })),
    ...teacherCases.map(scenario => ({ role: 'Teacher', scenario, family: 'teacher' })),
  ].flatMap(item => [1280, 320].map(width => ({ ...item, width, name: item.role + '/' + width + '/' + item.scenario })));
  const filter = process.env.QA_C52_CASE_FILTER ? new RegExp(process.env.QA_C52_CASE_FILTER) : null;
  const planned = matrix.filter(x => !filter || filter.test(x.name)); a.equal(matrix.length, 176); a.ok(planned.length);
  fs.writeFileSync(path.join(browser.run, 'result.json'), JSON.stringify({ status: 'RUNNING', planned: planned.length }));
  try {
    for (const item of planned) {
      let p, facts, error;
      try {
        const result = await ({ academy: academyCase, portal: portalCase, teacher: teacherCase }[item.family])(browser, actors[item.role], item.width, item.scenario);
        p = result.p; facts = result.facts;
        a.equal(await p.evaluate(`Boolean(document.querySelector('a[href="${actors[item.role].manual}"]'))`), true);
        a.equal(await p.evaluate(`document.querySelector('main[data-penta-theme]')?.getAttribute('data-penta-theme')`), item.width === 320 ? 'prism' : 'orbit');
        await p.assertClean();
      } catch (e) { error = e.message; p ||= e.page; if (p) fs.writeFileSync(path.join(browser.run, item.name.replaceAll('/', '-') + '.txt'), await p.text().catch(() => 'Unavailable')); }
      finally { if (p) try { await p.close(); } catch (e) { error = (error || '') + ' Cleanup: ' + e.message; } }
      results.push({ case: item.name, family: item.family, pass: !error, error, facts, requests: p?.requests, errors: p?.errors });
      fs.writeFileSync(path.join(browser.run, 'progress.json'), JSON.stringify(results, null, 2)); console.log(item.name + ': ' + (error ? 'FAIL ' + error : 'PASS'));
    }
    a.equal(results.length, planned.length); a.equal(results.filter(x => !x.pass).length, 0, 'All selected cases must pass');
  } catch (e) { failure = e.message; }
  finally {
    cleanup = await browser.close();
    const report = { status: failure || cleanup.cleanupError ? 'FAIL' : 'PASS', failure, ...cleanup, cases: results.length,
      passed: results.filter(x => x.pass).length, failed: results.filter(x => !x.pass).length, filter: process.env.QA_C52_CASE_FILTER || null,
      results, scope: 'Actual exported React/Chromium, synthetic intercepted APIs/effects only. No live Identity/SQL/device/model qualification.' };
    fs.writeFileSync(path.join(browser.run, 'result.json'), JSON.stringify(report, null, 2));
    console.log(JSON.stringify({ status: report.status, cases: report.cases, passed: report.passed, failed: report.failed, evidence: browser.run }));
    if (failure || cleanup.cleanupError) throw Error(failure || cleanup.cleanupError);
  }
}
main().catch(e => { console.error(e.message); process.exitCode = 1; });
