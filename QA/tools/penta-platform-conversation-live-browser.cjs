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
const root = '/api/platform/penta/';

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  try {
    const context = await browser.newContext({ viewport: { width: 390, height: 844 }, timezoneId: 'Asia/Kolkata' });
    const page = await context.newPage(), methods = [];
    page.on('request', r => { if (new URL(r.url()).pathname.startsWith(root)) methods.push(r.method()); });
    await page.goto(web + '/login?returnTo=%2Fplatform%2Fai');
    await page.getByLabel('User name', { exact: true }).fill('qa-platform-conversation@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const identity = page.waitForResponse(r => r.url() === api + '/api/auth/session' && r.status() === 200);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await page.waitForURL(web + '/platform/ai');
    assert.equal((await (await identity).json()).isPlatformOwner, true);
    const chat = page.getByRole('region', { name: 'Platform conversation' });
    const submit = async text => {
      await chat.getByLabel('Message PENTA').fill(text);
      await chat.getByRole('button', { name: 'Send', exact: true }).click();
    };
    async function ask(text, kind, title) {
      const reply = page.waitForResponse(r => r.url() === api + root + kind && r.request().method() === 'GET');
      await submit(text);
      const response = await reply;
      assert.equal(response.status(), 200);
      assert.match(response.headers()['cache-control'], /no-store/);
      const result = await response.json();
      assert.equal(result.scope, 'PlatformOwner');
      assert.equal(result.sourcePath, '/platform/control');
      await chat.getByRole('region', { name: title }).waitFor();
      return result.metrics;
    }

    const health = await ask('Show system status', 'health', 'Platform health');
    assert.deepEqual(health, { api: 'Operational', database: 'Operational', maintenanceMode: false });
    console.log('PENTA PLATFORM CASE verified health and maintenance summary PASS');

    const onboarding = await ask('Show onboarding status', 'onboarding', 'Academy onboarding');
    assert.deepEqual(onboarding, { academyCount: 2, withProfile: 1, inProgress: 1, completed: 0 });
    assert.equal(await chat.getByText(/private-portal-probe/).count(), 0);
    console.log('PENTA PLATFORM CASE bounded onboarding aggregate without contact data PASS');

    const support = await ask('Show support queue', 'support', 'Support queue');
    assert.deepEqual(support, { open: 2, highOrUrgent: 1 });
    assert.equal(await chat.getByText(/Private support probe/).count(), 0);
    await submit('Show a learner’s private fees');
    await chat.getByText(/Try system status/).waitFor();
    assert.equal(await chat.getByRole('region', { name: 'Support queue' }).count(), 0);
    assert.ok(methods.every(method => ['GET', 'OPTIONS'].includes(method)));
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    assert.equal(await chat.getByRole('link', { name: 'Open Manual Platform Control' }).getAttribute('href'), '/platform/control');
    console.log('PENTA PLATFORM CASE scoped support counts, prompt fallback and Manual entry PASS');
    console.log('PENTA PLATFORM BROWSER SUMMARY 3 groups PASS');
  } finally { await browser.close(); }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
