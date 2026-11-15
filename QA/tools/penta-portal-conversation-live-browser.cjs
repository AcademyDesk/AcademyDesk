const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_FINANCE_CHAT_PLAYWRIGHT);

function loopback(value) {
  const url = new URL(value);
  assert.equal(url.protocol, 'http:');
  assert.equal(url.hostname, '127.0.0.1');
  assert.ok(Number(url.port) >= 1024 && url.href === url.origin + '/');
  return url.origin;
}
const web = loopback(process.env.QA_FINANCE_CHAT_WEB), api = loopback(process.env.QA_FINANCE_CHAT_API);
assert.notEqual(web, api);
const root = '/api/portal/penta';

async function login(context, email, destination, role) {
  const page = await context.newPage();
  await page.goto(web + '/login?returnTo=' + encodeURIComponent(destination));
  await page.getByLabel('User name', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
  const verified = page.waitForResponse(r => r.url() === api + '/api/auth/session' && r.status() === 200);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.waitForURL(web + destination);
  const account = await (await verified).json();
  assert.ok(account.roles.includes(role));
  return { page, account };
}
async function ask(page, text) {
  const chat = page.getByRole('region', { name: 'PENTA learner conversation' });
  await chat.getByLabel('Message PENTA').fill(text);
  await chat.getByRole('button', { name: 'Send', exact: true }).click();
  return chat;
}
function token(page, workspace) {
  return page.evaluate(key => localStorage.getItem(key), 'academydesk.accessToken.' + workspace);
}
async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  try {
    const studentContext = await browser.newContext({ viewport: { width: 390, height: 844 }, timezoneId: 'Asia/Kolkata' });
    const { page: student } = await login(studentContext, 'qa-penta-portal-student@example.invalid', '/portal/ai', 'Student');
    const requests = [];
    student.on('request', r => { if (new URL(r.url()).pathname.startsWith(root)) requests.push(r.method()); });
    const next = student.waitForResponse(r => new URL(r.url()).pathname.endsWith('/next-class'));
    const studentChat = await ask(student, 'When is my next class?');
    const nextClass = await next;
    assert.equal(nextClass.status(), 200);
    const answer = await nextClass.json();
    assert.equal(answer.rows.length, 1);
    assert.deepEqual(Object.keys(answer.rows[0]).sort(), ['batchName', 'deliveryMode', 'endUtc', 'startUtc']);
    const learner = answer.studentId;
    await studentChat.getByText(/PENTA Browser Piano/).waitFor();
    await ask(student, 'What homework is due?');
    await studentChat.getByText(/PENTA visible practice/).waitFor();
    assert.equal(await studentChat.getByText(/submitted practice|unpublished practice|private sibling work/).count(), 0);
    assert.ok(requests.every(method => ['GET', 'OPTIONS'].includes(method)));
    assert.equal(await student.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    console.log('PENTA PORTAL CASE Student next class and published unsubmitted due work PASS');

    const parentContext = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const { page: parent, account } = await login(parentContext, 'qa-penta-portal-guardian@example.invalid', '/portal/ai', 'Guardian');
    const chat = await ask(parent, 'Show outstanding fees');
    await chat.getByText('Which linked learner should I check?').waitFor();
    const choose = name => chat.getByRole('button', { name: 'Choose ' + name, exact: true });
    assert.equal(await chat.getByRole('button', { name: /^Choose / }).count(), 2);
    await choose('Leo Penta Sibling').click();
    await chat.getByText(/does not have access to that information/).waitFor();
    await ask(parent, 'Show outstanding fees for Mira');
    await chat.getByText(/PENTA-PORTAL-EXACT · USD 450.00/).waitFor();
    console.log('PENTA PORTAL CASE Guardian disambiguation, finance consent and exact ledger PASS');

    const parentToken = await token(parent, 'Portal');
    const profile = await parent.request.get(api + '/api/portal/me', { headers: { Authorization: 'Bearer ' + parentToken } });
    assert.equal(profile.status(), 200);
    const { parentId } = await profile.json();
    const adminContext = await browser.newContext();
    const { page: admin } = await login(adminContext, 'qa-admin-a@example.invalid', '/dashboard', 'AcademyAdmin');
    const adminToken = await token(admin, 'AcademyAdmin');
    const revoke = await admin.request.patch(api + '/api/academies/' + account.academyId + '/students/' + learner +
      '/guardians/' + parentId + '/portal-access', {
      headers: { Authorization: 'Bearer ' + adminToken }, data: { allowPortalAccess: false }
    });
    assert.equal(revoke.status(), 200);
    const stale = await parent.request.get(api + root + '/students/' + learner + '/outstanding-fees', {
      headers: { Authorization: 'Bearer ' + parentToken }
    });
    assert.equal(stale.status(), 403);
    await ask(parent, 'Show outstanding fees');
    await chat.getByText('Which linked learner should I check?').waitFor();
    assert.equal(await chat.getByText(/USD 450.00/).count(), 0);
    assert.equal(await choose('Mira Penta Learner').count(), 0);
    await choose('Leo Penta Sibling').waitFor();
    await ask(parent, 'Next class for Leo');
    await chat.getByText(/PENTA Browser Violin/).waitFor();
    const own = await student.request.get(api + root + '/students/' + learner + '/next-class', {
      headers: { Authorization: 'Bearer ' + await token(student, 'Portal') }
    });
    assert.equal(own.status(), 200);
    console.log('PENTA PORTAL CASE current link revocation preserves sibling and Student access PASS');
    console.log('PENTA PORTAL BROWSER SUMMARY 3 groups PASS');
  } finally { await browser.close(); }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
