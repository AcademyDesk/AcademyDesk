const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_FINANCE_CHAT_PLAYWRIGHT);

function loopback(value) {
  const url = new URL(value);
  assert.equal(url.protocol, 'http:');
  assert.equal(url.hostname, '127.0.0.1');
  assert.ok(Number(url.port) >= 1024 && url.href === url.origin + '/');
  return url.origin;
}

const web = loopback(process.env.QA_FINANCE_CHAT_WEB);
const api = loopback(process.env.QA_FINANCE_CHAT_API);
assert.notEqual(web, api);
const route = '/penta/scheduling', hostSegment = '/penta/class-scheduling/browser/';
const pointer = 'academydesk.penta.class-scheduling-recovery.v1';
const meeting = 'https://class.example.invalid/rehearsal';
const clock = (days, hour) => {
  const now = new Date();
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + days, hour)).toISOString().slice(0, 16);
};

async function verify(browser, move) {
  const width = move ? 1440 : 390;
  const context = await browser.newContext({ viewport: { width, height: 844 }, timezoneId: 'Asia/Kolkata',
    colorScheme: move ? 'dark' : 'light' });
  try {
    const page = await context.newPage(), requests = [];
    const isHost = url => url.origin === api && url.pathname.includes(hostSegment);
    page.on('request', request => {
      const url = new URL(request.url());
      if (isHost(url) && request.method() !== 'OPTIONS')
        requests.push({ method: request.method(), path: url.pathname, body: request.postData() });
    });
    await page.goto(web + '/login?returnTo=%2Fpenta');
    await page.getByLabel('User name', { exact: true }).fill('qa-class-' + (move ? 'move' : 'create') + '@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const session = page.waitForResponse(response => response.url() === api + '/api/auth/session' && response.status() === 200);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    assert.ok((await (await session).json()).roles.includes(move ? 'Owner' : 'AcademyAdmin'));
    await page.getByRole('link', { name: 'Class scheduling', exact: true }).click();
    await page.waitForURL(web + route);
    const conversation = page.getByRole('region', { name: 'Class scheduling conversation' });
    const button = name => conversation.getByRole('button', { name, exact: true });
    const writes = () => requests.filter(request => request.method !== 'GET');
    const prompt = conversation.getByLabel('Message Executor about a class');
    await prompt.fill((move ? 'Reschedule' : 'Schedule') + ' a class for Browser Rehearsal');
    await button('Send message').click();
    const selected = conversation.getByRole('button', { name: move ? /^Select Browser Rehearsal at .*09:00 UTC$/ : /^Select Browser Rehearsal$/ });
    await selected.click();
    const day = move ? 62 : 61, hour = move ? 13 : 11;
    await conversation.getByLabel('Start (UTC)', { exact: true }).fill(clock(day, hour));
    await conversation.getByLabel('End (UTC)', { exact: true }).fill(clock(day, hour + 1));
    if (move) await conversation.getByLabel('Delivery mode').selectOption('Hybrid');
    await conversation.getByLabel(move ? 'HTTPS meeting link' : 'Room (optional)', { exact: true }).fill(move ? meeting : 'Browser studio');
    await button('Review class schedule').click();
    const approval = button('Approve exact schedule'), execute = button(move ? 'Save reschedule' : 'Schedule class');
    await approval.waitFor();
    const review = conversation.getByRole('region', { name: 'Verified scheduling review' });
    await review.getByText(move ? meeting : 'Browser studio', { exact: true }).waitFor();
    if (move) await review.getByText('Original slot', { exact: true }).waitFor();
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    assert.equal(overflow, false);
    assert.equal(await execute.count(), 0);
    assert.deepEqual(writes().map(r => r.method), ['PUT', 'PUT']);
    const prepared = JSON.parse(writes().at(-1).body).payload;
    assert.equal(prepared.tool_key, 'AcademyDesk.ClassSession.' + (move ? 'Reschedule' : 'Create'));
    assert.equal(prepared.fields.StartUtc.value, clock(day, hour) + ':00Z');
    const storage = await page.evaluate(key => sessionStorage.getItem(key), pointer);
    assert.deepEqual(Object.keys(JSON.parse(storage)).sort(), ['academyId', 'conversationId', 'epoch', 'requestId', 'startRequestId']);
    assert.ok(!storage.includes(meeting) && !storage.includes('Browser studio'));
    await approval.click();
    await execute.waitFor();
    assert.equal(JSON.parse(writes().at(-1).body).intent, 'ApproveClassSession' + (move ? 'Reschedule' : 'Schedule'));
    assert.equal(writes().filter(r => r.path.endsWith('/execute')).length, 0);
    console.log('PENTA CLASS LIVE CASE ' + (move ? 'Owner reschedule' : 'Academy Admin schedule') + ' exact review and separate approval PASS');

    let captured, interceptedError;
    if (move) await page.route(url => isHost(url) && url.pathname.endsWith('/execute'), async request => {
      if (request.request().method() !== 'POST') return request.continue();
      try {
        const result = await request.fetch({ maxRetries: 0 });
        assert.equal(result.status(), 201);
        captured = (await result.json()).receipt;
      } catch (error) { interceptedError = error; }
      await request.abort('failed');
    });
    await execute.click();
    const success = review.getByRole('heading', { name: move ? 'Class rescheduled' : 'Class scheduled', exact: true });
    if (move) {
      await conversation.getByText(/outcome is unconfirmed/).waitFor();
      if (interceptedError) throw interceptedError;
      assert.ok(captured?.classSessionId);
    } else await success.waitFor();

    const committedRequests = writes().length;
    const readFrom = requests.length;
    await page.reload();
    await success.waitFor();
    assert.equal(writes().length, committedRequests);
    assert.ok(requests.slice(readFrom).some(r => r.method === 'GET' && /\/drafts\/[^/]+$/.test(r.path)));
    if (captured) await review.getByText(captured.receiptId, { exact: true }).waitFor();
    assert.equal(writes().filter(r => r.path.endsWith('/execute')).length, 1);
    console.log('PENTA CLASS LIVE CASE ' + (move ? 'Owner lost response' : 'Academy Admin completion') + ' GET-only receipt recovery PASS');
  } finally {
    await context.close();
  }
}

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  try {
    await verify(browser, false);
    await verify(browser, true);
    console.log('PENTA CLASS SCHEDULING LIVE SUMMARY 4 browser groups PASS');
  } finally { await browser.close(); }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
