'use strict';
// Real Identity/HTTP/SQL acceptance on the existing owned synthetic fixture.
// Only login POSTs and application GETs; no mutation, model request or resources.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const cp = require('node:child_process'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const hash = value => crypto.createHash('sha256').update(JSON.stringify(value)).digest('hex');
async function main() {
  a.equal(process.env.QA_INTERACTIVE_DEMO_PERMISSIONS, '1', 'Explicit synthetic opt-in required.');
  const active = JSON.parse(fs.readFileSync(path.join(root, '.build-check/ACTIVE_DEMO.json')));
  a.match(active.run, /^[a-f0-9]{32}$/);
  const folder = path.join(root, '.build-check', 'interactive-demo-' + active.run);
  a.equal(path.resolve(active.folder), folder); a.equal(path.resolve(active.loginFile), path.join(folder, 'LOGIN.txt'));
  const meta = JSON.parse(fs.readFileSync(path.join(folder, 'result.json')));
  a.equal(meta.ready, true); a.equal(meta.synthetic, true); a.equal(meta.azure, false);
  a.equal(meta.fixture.synthetic, true); a.equal(meta.fixture.api, 'http://127.0.0.1:54999');
  a.ok(Date.parse(meta.expiresAtUtc) > Date.now()); a.match(meta.fixture.academyId, /^[a-f0-9-]{36}$/i);
  const password = fs.readFileSync(active.loginFile, 'utf8').match(/Password for all six demo accounts: (.+)/)?.[1];
  a.ok(password);
  // Preserve the real120/minute limiter. Separate complete packets by one
  // window; never retry a429 or alter an assertion to conceal rate limiting.
  const previous = fs.readdirSync(folder).filter(name => /^permissions-[a-f0-9]{32}\.json$/.test(name))
    .map(name => JSON.parse(fs.readFileSync(path.join(folder, name))))
    .filter(value => value.finishedAt).map(value => Date.parse(value.finishedAt));
  if (previous.length) {
    const remaining = 61000 - (Date.now() - Math.max(...previous));
    if (remaining > 0) await new Promise(resolve => setTimeout(resolve, remaining));
  }
  const sqlRun = meta.fixture.run.replaceAll('-', ''); a.match(sqlRun, /^[a-f0-9]{32}$/);
  const docker = args => cp.execFileSync('docker', args, { encoding: 'utf8', windowsHide: true,
    timeout: 30000, stdio: ['ignore', 'pipe', 'pipe'] }).trim();
  const sql = JSON.parse(docker(['inspect', 'academydesk-qa-' + sqlRun]))[0];
  a.equal(sql.Config.Labels['academydesk.qa.run'], sqlRun); a.equal(sql.State.Running, true);
  const snapshot = () => {
    const query = 'SET NOCOUNT ON; SELECT (SELECT COUNT(*) FROM Invoices) AS invoices,' +
      '(SELECT CONVERT(varchar(100),SUM(TotalAmount)) FROM Invoices) AS billed,' +
      '(SELECT CONVERT(varchar(100),SUM(AdjustedAmount)) FROM Invoices) AS adjusted,' +
      '(SELECT COUNT(*) FROM Payments) AS payments,(SELECT CONVERT(varchar(100),SUM(Amount)) FROM Payments) AS paid,' +
      '(SELECT COUNT(*) FROM PentaMiniTurns) AS turns,(SELECT COUNT(*) FROM PentaMiniSessions) AS sessions FOR JSON PATH;';
    const command = '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -h -1 -w 65535 -d AcademyDesk_QA_' + sqlRun + ' -Q "$1"';
    return hash(JSON.parse(docker(['exec', sql.Id, 'bash', '-lc', command, 'qa-read-only', query]).split(/\r?\n/).map(x => x.trim()).join('')));
  };
  const output = path.join(folder, 'permissions-' + crypto.randomUUID().replaceAll('-', '') + '.json');
  const result = { run: active.run, source: cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(),
    toolSha256: crypto.createHash('sha256').update(fs.readFileSync(__filename)).digest('hex'),
    startedAt: new Date().toISOString(), checks: [], synthetic: true, modelRequests: 0, applicationWrites: 0, azure: false };
  const save = () => fs.writeFileSync(output, JSON.stringify(result, null, 2));
  const pass = (name, status) => { result.checks.push({ name, status, pass: true }); save(); };
  const tokens = {};
  let requests = 0, currentCheck = 'initial snapshot';
  const request = async (route, role, body) => {
    requests++;
    const r = await fetch(meta.fixture.api + route, { method: body ? 'POST' : 'GET', headers: {
      ...(tokens[role] ? { Authorization: 'Bearer ' + tokens[role] } : {}), ...(body ? { 'Content-Type': 'application/json' } : {})
    }, body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(12000) });
    const raw = await r.text(); let data; try { data = JSON.parse(raw); } catch { data = null; }
    a.notEqual(r.status, 429, 'Rate-limited acceptance is not a pass.');
    return { status: r.status, data, raw };
  };
  const expect = async (name, route, role, status) => {
    currentCheck = name;
    const r = await request(route, role); a.equal(r.status, status, name);
    if (status >= 400) a.equal(r.data?.exactMoney, undefined);
    pass(name, r.status); return r;
  };
  result.before = snapshot(); save();
  try {
    const roles = ['owner', 'academyadmin', 'teacher', 'student', 'guardian', 'platformowner'];
    const workspaces = ['AcademyAdmin', 'AcademyAdmin', 'Teacher', 'Portal', 'Portal', 'Platform'];
    for (let i = 0; i < roles.length; i++) {
      const role = roles[i]; currentCheck = role + ' real Identity login';
      const r = await request('/api/auth/login', null, { email: role + '@demo.example.invalid', password });
      a.equal(r.status, 200); a.ok(r.data?.accessToken); tokens[role] = r.data.accessToken; pass(currentCheck, r.status);
      const me = await expect(role + ' authoritative workspace', '/api/auth/session', role, 200);
      a.equal(me.data.workspace, workspaces[i]);
      if (role !== 'platformowner') a.equal(me.data.academyId, meta.fixture.academyId);
      else a.equal(me.data.isPlatformOwner, true);
    }
    const prefix = '/api/academies/' + meta.fixture.academyId;
    const foreign = '/api/academies/' + crypto.randomUUID();
    for (const role of ['owner', 'academyadmin']) {
      for (const endpoint of ['invoices', 'payments']) {
        await expect(role + ' permitted ' + endpoint, prefix + '/' + endpoint, role, 200);
        await expect(role + ' foreign tenant ' + endpoint + ' refused', foreign + '/' + endpoint, role, 403);
      }
      const health = await expect(role + ' current Mini availability', prefix + '/penta/chat/health', role, 200);
      a.equal(health.data.status, 'Available');
    }
    for (const route of [prefix + '/invoices', prefix + '/payments', prefix + '/penta/chat/health', '/api/platform/overview'])
      await expect('anonymous protected route ' + route.split('/').at(-1), route, null, 401);
    for (const role of ['teacher', 'student', 'guardian']) {
      for (const endpoint of ['invoices', 'payments', 'students'])
        await expect(role + ' academy-wide ' + endpoint + ' refused', prefix + '/' + endpoint, role, 403);
      await expect(role + ' Owner financial Mini refused', prefix + '/penta/chat/health', role, 403);
    }
    const platform = await expect('Platform Owner platform overview', '/api/platform/overview', 'platformowner', 200);
    a.ok(platform.data && !Array.isArray(platform.data));
    await expect('Platform Owner cannot become academy Mini actor', prefix + '/penta/chat/health', 'platformowner', 403);
    for (const role of roles.filter(x => x !== 'platformowner'))
      await expect(role + ' platform overview refused', '/api/platform/overview', role, 403);
    const learners = await expect('Owner learner list for protected scope controls', prefix + '/students', 'owner', 200);
    a.ok(Array.isArray(learners.data) && learners.data.length >= 12);
    const invoices = await expect('Owner invoice list for download scope controls', prefix + '/invoices', 'owner', 200);
    const student = await expect('Student own portal identity', '/api/portal/me', 'student', 200);
    const guardian = await expect('Guardian linked portal identity', '/api/portal/me', 'guardian', 200);
    const child = student.data.studentId; a.match(child, /^[a-f0-9-]{36}$/i);
    a.ok(guardian.data.children.some(x => x.id === child));
    const other = learners.data.find(x => x.id !== child && invoices.data.some(y => y.studentId === x.id)); a.ok(other);
    const ownInvoice = invoices.data.find(x => x.studentId === child); a.ok(ownInvoice);
    const otherInvoice = invoices.data.find(x => x.studentId === other.id); a.ok(otherInvoice);
    for (const role of ['student', 'guardian']) {
      const details = await expect(role + ' permitted linked learner', '/api/portal/students/' + child, role, 200);
      a.ok(details.data.invoices.every(x => x.id === ownInvoice.id));
      await expect(role + ' other learner refused', '/api/portal/students/' + other.id, role, 403);
      const document = await expect(role + ' own invoice download', '/api/portal/students/' + child + '/invoices/' + ownInvoice.id + '/download', role, 200);
      a.ok(document.raw.includes(ownInvoice.invoiceNumber) && !document.raw.includes(otherInvoice.invoiceNumber));
      await expect(role + ' another learner invoice refused', '/api/portal/students/' + other.id + '/invoices/' + otherInvoice.id + '/download', role, 403);
      await expect(role + ' same learner mismatched invoice refused', '/api/portal/students/' + child + '/invoices/' + otherInvoice.id + '/download', role, 404);
      await expect(role + ' Teacher portal refused', '/api/teacher/me', role, 403);
    }
    const children = await expect('Guardian own children list', '/api/portal/guardians/' + guardian.data.parentId + '/children', 'guardian', 200);
    a.ok(children.data.every(x => x.id === child));
    await expect('Guardian foreign guardian identity refused', '/api/portal/guardians/' + crypto.randomUUID() + '/children', 'guardian', 403);
    const teacher = await expect('Teacher assigned workspace', '/api/teacher/me', 'teacher', 200);
    a.ok(teacher.data.batches.length > 0 && teacher.data.batches.length < 4);
    await expect('Teacher calendar requires explicit period', '/api/teacher/calendar', 'teacher', 400);
    const today = new Date();
    await expect('Teacher permitted calendar', '/api/teacher/calendar?year=' + today.getUTCFullYear() + '&month=' + (today.getUTCMonth() + 1), 'teacher', 200);
    await expect('Teacher cannot assume family identity', '/api/portal/me', 'teacher', 403);
    await expect('Teacher cannot read family learner details', '/api/portal/students/' + child, 'teacher', 403);
    result.after = snapshot(); a.equal(result.after, result.before, 'Read checks changed ledger or Mini state.');
    pass('Unchanged invoice/payment totals, adjustments, Mini turns and sessions', null);
    result.passed = true;
  } catch (error) {
    result.passed = false; result.failure = { check: currentCheck, type: error.name, code: error.code || null,
      expectedStatus: typeof error.expected === 'number' ? error.expected : null,
      actualStatus: typeof error.actual === 'number' ? error.actual : null };
  }
  result.requests = requests; result.finishedAt = new Date().toISOString(); save();
  console.log(JSON.stringify({ output, passed: result.passed, checks: result.checks.length, requests, failure: result.failure || null }));
  if (!result.passed) process.exitCode = 1;
}
main().catch(error => { console.error('Synthetic permission preflight failed: ' + error.name); process.exitCode = 1; });
