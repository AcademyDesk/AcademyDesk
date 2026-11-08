// Actual MediaRecorder and private media APIs using Chromium's fake microphone.
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');

const web = 'http://127.0.0.1:54998';
const api = 'http://127.0.0.1:54999';
const run = process.env.QA_MEDIA_RUN;
assert.match(run ?? '', /^[a-f0-9]{32}$/);
const root = path.resolve(process.env.QA_MEDIA_FIXTURE_ROOT);
assert.equal(fs.readFileSync(path.join(root, '.qa-owner'), 'utf8').split(/\r?\n/)[0], run);
const proof = JSON.parse(fs.readFileSync(process.env.QA_MEDIA_PROOF, 'utf8'));
assert.deepEqual([proof.run, proof.origin, proof.api, proof.responsesMocked], [run, web, api, false]);
assert.match(proof.sourceSha, /^[a-f0-9]{40}$/);
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for (const [file, digest] of Object.entries(proof.sourceHashes)) {
  assert.equal(hash(path.join(proof.webSource, file)), digest, file);
}
assert.ok(proof.artifacts.length > 100);
const artifactRoot = fs.realpathSync(proof.artifactRoot);
for (const file of proof.artifacts) {
  const target = fs.realpathSync(path.join(artifactRoot, file.path));
  assert.ok(target.startsWith(artifactRoot + path.sep));
  assert.equal(hash(target), file.sha256, file.path);
}

const output = path.resolve(process.env.QA_MEDIA_OUTPUT);
const { chromium } = require(process.env.QA_MEDIA_PLAYWRIGHT);
const responses = [];
const errors = [];
let step = 'startup';
const title = 'QA synthetic microphone practice';
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

async function open(browser, width, microphone = false) {
  const context = await browser.newContext({ viewport: { width, height: 844 }, timezoneId: 'Asia/Kolkata', acceptDownloads: true });
  if (microphone) await context.grantPermissions(['microphone'], { origin: web });
  await context.route('**/*', route => {
    const url = new URL(route.request().url());
    return url.origin === web || url.origin === api || url.protocol === 'data:' || url.protocol === 'blob:'
      ? route.continue() : route.abort('blockedbyclient');
  });
  const page = await context.newPage();
  page.setDefaultTimeout(18000);
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => {
    const url = new URL(response.url());
    if (url.origin === api) responses.push({ path: url.pathname, method: response.request().method(), status: response.status() });
  });
  return { page, context };
}

async function login(page, email, route) {
  await page.goto(web + '/login?returnTo=' + encodeURIComponent(route));
  await page.getByLabel('User name', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
  const [response] = await Promise.all([
    page.waitForResponse(value => {
      const url = new URL(value.url());
      return url.origin === api && url.pathname === '/api/auth/login' && value.request().method() === 'POST';
    }),
    page.getByRole('button', { name: 'Sign in', exact: true }).click(),
  ]);
  assert.equal(response.status(), 200);
  await page.waitForURL(url => url.pathname === route);
}
async function openTab(page, label) {
  await page.locator('.learner-bottom-nav').getByRole('button').filter({ has: page.getByText(label, { exact: true }) }).click();
}

async function openRecorder(page) {
  await openTab(page, 'Classroom');
  const form = page.locator('form.teacher-upload-form');
  const classSelector = page.locator('.teacher-classroom-controls').getByRole('button', { name: 'Class', exact: true });
  await classSelector.waitFor();
  if (!(await form.isVisible())) {
    await classSelector.click();
    await page.getByRole('option', { name: 'Synthetic Batch', exact: true }).click();
  }
  return form;
}

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true,
    chromiumSandbox: true, args: ['--use-fake-device-for-media-stream'] });
  try {
    step = 'denied microphone keeps the selected file';
    const blocked = await open(browser, 390);
    const cdp = await blocked.context.newCDPSession(blocked.page);
    const { targetInfo } = await cdp.send('Target.getTargetInfo');
    assert.ok(targetInfo.browserContextId);
    await cdp.send('Browser.setPermission', { origin: web, browserContextId: targetInfo.browserContextId,
      permission: { name: 'microphone' }, setting: 'denied' });
    await login(blocked.page, 'qa-teacher-a@example.invalid', '/teacher');
    const deniedForm = await openRecorder(blocked.page);
    const selected = deniedForm.getByLabel('Choose class attachment');
    await selected.setInputFiles({ name: 'synthetic-selected.txt', mimeType: 'text/plain', buffer: Buffer.from('Synthetic retained file') });
    await deniedForm.locator('[name="title"]').fill('Synthetic retained title');
    assert.equal(await blocked.page.evaluate(async () => (await navigator.permissions.query({ name: 'microphone' })).state), 'denied');
    await deniedForm.getByRole('button', { name: 'Record audio', exact: true }).click();
    await deniedForm.getByText('Microphone access could not be started. Allow access or choose a recording from your device.', { exact: true }).waitFor();
    assert.equal(await selected.evaluate(element => element.files[0]?.name), 'synthetic-selected.txt');
    assert.equal(await deniedForm.locator('[name="title"]').inputValue(), 'Synthetic retained title');
    assert.equal(await deniedForm.getByRole('button', { name: 'Confirm upload', exact: true }).isEnabled(), true);
    assert.equal(responses.filter(row => row.path === '/api/teacher/media-uploads' && row.method === 'POST').length, 0);
    await blocked.context.close();

    step = 'teacher synthetic recording';
    const teacher = await open(browser, 390, true);
    await login(teacher.page, 'qa-teacher-a@example.invalid', '/teacher');
    const page = teacher.page;
    const form = await openRecorder(page);
    await form.getByRole('button', { name: 'Record audio', exact: true }).click();
    await form.getByRole('button', { name: 'Pause recording', exact: true }).waitFor();
    await delay(1100);
    await form.getByRole('button', { name: 'Pause recording', exact: true }).click();
    await form.getByText(/Recording paused/).waitFor();
    await form.getByRole('button', { name: 'Resume recording', exact: true }).click();
    await form.getByRole('button', { name: 'Pause recording', exact: true }).waitFor();
    await delay(350);
    await form.getByRole('button', { name: 'Stop recording', exact: true }).click();
    await form.getByText('Recording ready. Review it, then confirm upload.', { exact: true }).waitFor();
    const preview = form.locator('.teacher-file-preview audio, .teacher-file-preview video');
    await preview.waitFor();
    const source = await preview.evaluate(async element => {
      const url = element.getAttribute('src');
      if (!url?.startsWith('blob:')) throw Error('The recording preview is not local.');
      const data = await (await fetch(url)).arrayBuffer();
      const digest = Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', data)), value => value.toString(16).padStart(2, '0')).join('');
      return { length: data.byteLength, sha256: digest };
    });
    assert.ok(source.length > 200 && source.length < 1024 * 1024);

    step = 'confirmed private upload';
    await form.locator('[name="title"]').fill(title);
    const [done] = await Promise.all([
      page.waitForResponse(response => {
        const url = new URL(response.url());
        return url.origin === api && /^\/api\/teacher\/media-uploads\/[a-f0-9-]{36}\/complete$/.test(url.pathname) &&
          response.request().method() === 'POST';
      }),
      form.getByRole('button', { name: 'Confirm upload', exact: true }).click(),
    ]);
    assert.equal(done.status(), 200);
    const saved = await done.json();
    assert.equal(saved.url, '/api/class-media/' + saved.id + '/content');
    await form.getByText('Class material uploaded successfully.', { exact: true }).waitFor();
    assert.equal(await form.locator('[name="title"]').inputValue(), '');
    const creates = responses.filter(row => row.path === '/api/teacher/media-uploads' && row.method === 'POST');
    const chunks = responses.filter(row => row.path.endsWith('/chunks/0') && row.method === 'PUT');
    assert.deepEqual(creates.map(row => row.status), [201]);
    assert.deepEqual(chunks.map(row => row.status), [200]);
    await teacher.context.close();

    step = 'student decoding and exact native download';
    const student = await open(browser, 390);
    await login(student.page, 'qa-browser-student@example.invalid', '/portal');
    await openTab(student.page, 'Library');
    const resource = student.page.locator('.learner-resource-row').filter({ hasText: title });
    await resource.getByText(title, { exact: true }).waitFor();
    assert.equal(await resource.count(), 1);
    await resource.getByRole('button', { name: 'Preview', exact: true }).click();
    const media = resource.locator('.private-material-preview audio, .private-material-preview video');
    await media.waitFor();
    await student.page.waitForFunction(element => element.readyState >= 2 && !element.error, await media.elementHandle());
    await resource.getByRole('button', { name: 'Close preview', exact: true }).click();
    const [download] = await Promise.all([
      student.page.waitForEvent('download'),
      resource.getByRole('button', { name: 'Download', exact: true }).click(),
    ]);
    assert.match(download.suggestedFilename(), /^class-recording-\d+\.(webm|m4a|ogg)$/);
    const downloaded = path.join(output, 'student-recording.bin');
    assert.equal(fs.existsSync(downloaded), false);
    await download.saveAs(downloaded);
    assert.deepEqual({ length: fs.statSync(downloaded).size, sha256: hash(downloaded) }, source);
    await student.context.close();

    step = 'linked guardian exact native download';
    const guardian = await open(browser, 390);
    await login(guardian.page, 'qa-browser-guardian@example.invalid', '/portal');
    await openTab(guardian.page, 'Library');
    const guardianResource = guardian.page.locator('.learner-resource-row').filter({ hasText: title });
    const [guardianDownload] = await Promise.all([
      guardian.page.waitForEvent('download'), guardianResource.getByRole('button', { name: 'Download', exact: true }).click(),
    ]);
    assert.equal(guardianDownload.suggestedFilename(), download.suggestedFilename());
    const guardianFile = path.join(output, 'guardian-recording.bin');
    assert.equal(fs.existsSync(guardianFile), false);
    await guardianDownload.saveAs(guardianFile);
    assert.deepEqual({ length: fs.statSync(guardianFile).size, sha256: hash(guardianFile) }, source);
    await guardian.context.close();

    step = 'anonymous and unassigned teacher denial';
    const anonymous = await fetch(api + saved.url, { redirect: 'manual', signal: AbortSignal.timeout(12000) });
    assert.equal(anonymous.status, 401);
    const other = await open(browser, 1440);
    await login(other.page, 'qa-browser-teacher-b@example.invalid', '/teacher');
    const denied = await other.page.evaluate(async url => {
      const token = localStorage.getItem('academydesk.accessToken.Teacher');
      if (!token) throw Error('Real Teacher browser session missing.');
      const response = await fetch(url, { headers: { Authorization: 'Bearer ' + token }, redirect: 'error' });
      return { status: response.status, contentType: response.headers.get('Content-Type') ?? '' };
    }, api + saved.url);
    assert.ok([403, 404].includes(denied.status));
    assert.doesNotMatch(denied.contentType, /^(audio|video)\//);
    await other.context.close();

    assert.equal(responses.filter(row => row.path === saved.url && row.method === 'POST' && row.status === 200).length, 2);
    assert.ok(responses.every(row => row.status !== 429 && row.status < 500));
    assert.deepEqual(errors, []);
    fs.writeFileSync(path.join(output, 'result.json'), JSON.stringify({ run, passed: true, mediaId: saved.id, source, microphoneDenied: true, downloads: 2,
      anonymousStatus: anonymous.status, unassignedStatus: denied.status, responses, errors }, null, 2), { flag: 'wx' });
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  fs.writeFileSync(path.join(output, 'failure.json'), JSON.stringify({ run, step, responses, errors }, null, 2), { flag: 'wx' });
  console.error(step, error.stack);
  process.exitCode = 1;
});
