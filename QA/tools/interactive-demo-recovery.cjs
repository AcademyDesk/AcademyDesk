'use strict';

// Explicit opt-in to the existing, owned synthetic demo only. No model request,
// model/container recreation, package download, production DB or Azure access.
const a = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const cp = require('node:child_process');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const digest = value => crypto.createHash('sha256').update(JSON.stringify(value)).digest('hex');
const docker = args => cp.execFileSync('docker', args, {
  encoding: 'utf8', windowsHide: true, timeout: 30000, stdio: ['ignore', 'pipe', 'pipe']
}).trim();
const inspect = id => JSON.parse(docker(['inspect', id]))[0];
const inventory = () => Object.fromEntries([
  ['containers', ['ps', '-aq', '--no-trunc']], ['images', ['image', 'ls', '-aq', '--no-trunc']],
  ['volumes', ['volume', 'ls', '-q']], ['networks', ['network', 'ls', '-q', '--no-trunc']]
].map(([key, args]) => [key, [...new Set(docker(args).split(/\r?\n/).filter(Boolean))].sort()]));

async function main() {
  a.equal(process.env.QA_INTERACTIVE_DEMO_RECOVERY, '1', 'Explicit synthetic-demo opt-in required.');
  const active = JSON.parse(fs.readFileSync(path.join(root, '.build-check/ACTIVE_DEMO.json')));
  const relative = path.relative(path.join(root, '.build-check'), active.folder);
  a.ok(relative && !relative.startsWith('..') && !path.isAbsolute(relative), 'Private demo folder escaped repository.');
  a.match(active.run, /^[a-f0-9]{32}$/);
  a.equal(path.resolve(active.loginFile), path.join(active.folder, 'LOGIN.txt'));
  const meta = JSON.parse(fs.readFileSync(path.join(active.folder, 'result.json')));
  a.equal(meta.run, active.run); a.equal(meta.ready, true); a.equal(meta.synthetic, true);
  a.equal(meta.azure, false); a.equal(meta.fixture.synthetic, true);
  a.ok(Date.parse(meta.expiresAtUtc) > Date.now(), 'Existing demo expired.');
  a.equal(active.url, 'http://127.0.0.1:3000/login');
  a.equal(meta.fixture.api, 'http://127.0.0.1:54999');
  const sqlRun = meta.fixture.run.replaceAll('-', ''); a.match(sqlRun, /^[a-f0-9]{32}$/);
  const coreName = 'academydesk-mini-api-' + active.run;
  const core = inspect(coreName), sql = inspect('academydesk-qa-' + sqlRun);
  a.equal(core.Config.Labels['academydesk.qa.run'], active.run);
  a.equal(sql.Config.Labels['academydesk.qa.run'], sqlRun);
    a.equal(core.State.Running, true); a.equal(core.State.Paused, false); a.equal(sql.State.Running, true);
  const inference = inspect('academydesk-mini-llama-' + active.run);
  a.equal(inference.Config.Labels['academydesk.qa.run'], active.run);
  a.equal(inference.State.Running, true); a.equal(inference.State.Health?.Status, 'healthy');
  const output = path.join(active.folder, 'recovery-' + crypto.randomUUID().replaceAll('-', '') + '.json');
  const result = { run: active.run, head: cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(),
    startedAt: new Date().toISOString(), sourceSha256: crypto.createHash('sha256').update(fs.readFileSync(__filename)).digest('hex'),
    before: inventory(), checks: [], synthetic: true, azure: false, modelRequests: 0, modelLoaded: false, modelPromoted: false };
  const save = () => fs.writeFileSync(output, JSON.stringify(result, null, 2));
  const pass = check => { result.checks.push({ check, pass: true }); save(); };
  const password = fs.readFileSync(active.loginFile, 'utf8').match(/Password for all six demo accounts: (.+)/)?.[1];
  a.ok(password, 'Private synthetic credentials unavailable.');
  let token, paused = false;
  const api = async (route, method = 'GET', body, bearer = token) => {
    const response = await fetch(meta.fixture.api + route, { method, headers: {
      ...(bearer ? { Authorization: 'Bearer ' + bearer } : {}), ...(body ? { 'Content-Type': 'application/json' } : {})
    }, body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(15000) });
    const raw = await response.text(); let data; try { data = JSON.parse(raw); } catch { data = null; }
    return { status: response.status, data, noStore: /no-store/i.test(response.headers.get('cache-control') || '') };
  };
  // Only SELECTs on the already verified exact demo SQL container. The password
  // is resolved inside that container and is never copied into arguments/output.
  const select = statement => {
    a.match(statement, /^SET NOCOUNT ON; SELECT /);
    const command = '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -h -1 -w 65535 -d AcademyDesk_QA_' + sqlRun;
    const raw = docker(['exec', sql.Id, 'bash', '-lc', command + ' -Q "$1"', 'qa-read-only', statement]);
    return JSON.parse(raw.split(/\r?\n/).map(x => x.trim()).join(''));
  };
  const ledger = () => select('SET NOCOUNT ON; SELECT (SELECT COUNT(*) FROM Invoices) AS invoices,' +
    '(SELECT CONVERT(varchar(100),SUM(TotalAmount)) FROM Invoices) AS billed,' +
    '(SELECT COUNT(*) FROM Payments) AS payments,(SELECT CONVERT(varchar(100),SUM(Amount)) FROM Payments) AS paid,' +
    '(SELECT COUNT(*) FROM PentaMiniTurns) AS turns FOR JSON PATH;');
  save();
  try {
    const login = await api('/api/auth/login', 'POST', { email: 'owner@demo.example.invalid', password }, null);
    a.equal(login.status, 200); a.ok(login.data?.accessToken); token = login.data.accessToken;
    pass('Owner real Identity login');
    const profile = await api('/api/auth/session'); a.equal(profile.status, 200);
    a.match(meta.fixture.academyId, /^[a-f0-9-]{36}$/i);
    const prefix = '/api/academies/' + meta.fixture.academyId;
    const sessions = select("SET NOCOUNT ON; SELECT TOP (1) t.SessionId,t.RequestId FROM PentaMiniTurns t " +
      'JOIN PentaMiniSessions s ON s.Id=t.SessionId JOIN AspNetUsers u ON u.Id=t.ActorUserId ' +
      "WHERE u.Email='owner@demo.example.invalid' AND t.ProtectedReceipt IS NOT NULL " +
      'AND s.Version=t.ExpectedVersion+1 AND s.PendingRequestId IS NULL AND s.ExpiresAtUtc>SYSUTCDATETIME() ' +
      'ORDER BY t.CreatedAtUtc DESC FOR JSON PATH;');
    a.equal(sessions.length, 1, 'An existing current owner receipt is required; do not dispatch a substitute.');
    for (const value of Object.values(sessions[0])) a.match(value, /^[a-f0-9-]{36}$/i);
    const receiptRoute = prefix + '/penta/chat/conversations/' + sessions[0].SessionId + '/turns/' + sessions[0].RequestId;
    const original = await api(receiptRoute); a.equal(original.status, 200); a.equal(original.noStore, true);
    a.equal(original.data.exactMoney?.version, 'mini-exact-money.v1');
    pass('Existing exact receipt recovered with no model POST');
    const beforeLedger = ledger(); result.ledgerBeforeSha256 = digest(beforeLedger);
    // Pause retains Docker's dynamic loopback binding. Stop/start can reassign
    // that port and strand the unchanged host configuration after the test.
    docker(['pause', core.Id]); paused = true;
    a.equal(inspect(core.Id).State.Paused, true);
    const unavailable = await api(prefix + '/penta/chat/health');
    a.equal(unavailable.status, 200); a.equal(unavailable.data.status, 'Unavailable');
    pass('Authoritative host reports Mini unavailable while only owned Core API is paused');
    const recovered = await api(receiptRoute); a.equal(recovered.status, 200); a.equal(recovered.noStore, true);
    a.deepEqual(recovered.data, original.data);
    pass('GET-only recovery returns unchanged exact receipt during real Core outage');
    const anon = await api(receiptRoute, 'GET', undefined, null); a.equal(anon.status, 401);
    pass('Anonymous receipt recovery denied');
    for (const role of ['teacher', 'student']) {
      const login = await api('/api/auth/login', 'POST', { email: role + '@demo.example.invalid', password }, null);
      a.equal(login.status, 200);
      const denied = await api(receiptRoute, 'GET', undefined, login.data.accessToken);
      a.ok([403, 404].includes(denied.status)); a.equal(denied.data?.exactMoney, undefined);
      pass(role + ' cannot read the Owner financial conversation');
    }
    const wrongTenant = await api(receiptRoute.replace(meta.fixture.academyId, crypto.randomUUID()));
    a.ok([403, 404].includes(wrongTenant.status)); a.equal(wrongTenant.data?.exactMoney, undefined);
    pass('Manipulated tenant route cannot recover the receipt');
    const beforeStudents = await api(prefix + '/students'); a.equal(beforeStudents.status, 200);
    const suffix = path.basename(output, '.json').slice(-8);
    const saved = await api(prefix + '/students', 'POST', { firstName: 'Demo', lastName: 'Fallback ' + suffix,
      email: null, phone: null, branchId: null, dateOfBirth: null });
    a.equal(saved.status, 201); a.ok(saved.data?.id);
    const afterStudents = await api(prefix + '/students'); a.equal(afterStudents.status, 200);
    a.equal(afterStudents.data.length, beforeStudents.data.length + 1);
    a.ok(afterStudents.data.some(x => x.id === saved.data.id));
    result.retainedSyntheticLearnerId = saved.data.id;
    pass('Manual optional-fields-empty save persists while AI is unavailable');
    const manual = await fetch('http://127.0.0.1:3000/student-management', { signal: AbortSignal.timeout(10000) });
    a.equal(manual.status, 200); pass('Manual static route remains available during AI outage');
    result.ledgerAfterSha256 = digest(ledger()); a.equal(result.ledgerAfterSha256, result.ledgerBeforeSha256);
    pass('SQL invoice/payment counts and totals and Mini turn count unchanged');
    result.validationPassed = true;
  } catch (error) {
    // Assertions never embed credentials, tokens or raw API/SQL bodies.
    result.failure = { type: error.name, code: error.code || null, check: result.checks.length + 1 };
  } finally {
    if (paused || inspect(core.Id).State.Paused) docker(['unpause', core.Id]);
    a.equal(inspect(core.Id).Image, core.Image);
    a.equal(inspect(inference.Id).Image, inference.Image);
    a.equal(inspect(inference.Id).State.StartedAt, inference.State.StartedAt, 'Inference was restarted.');
    let available = false;
    for (let i = 0; i < 20; i++) {
      try {
        const health = await api('/api/academies/' + meta.fixture.academyId + '/penta/chat/health');
        available = health.status === 200 && health.data?.status === 'Available';
      } catch { /* Keep restoration bounded, without a model dispatch. */ }
      if (available) break; await sleep(1000);
    }
    result.coreRestored = inspect(core.Id).State.Running && !inspect(core.Id).State.Paused && available;
    a.deepEqual(inspect(core.Id).NetworkSettings.Ports, core.NetworkSettings.Ports, 'Core loopback binding changed.');
    result.after = inventory(); result.inventoryPreserved = digest(result.before) === digest(result.after);
    result.passed = !!result.validationPassed && result.coreRestored && result.inventoryPreserved;
    result.finishedAt = new Date().toISOString(); save();
  }
  console.log(JSON.stringify({ output, passed: result.passed, checks: result.checks.length,
    coreRestored: result.coreRestored, inventoryPreserved: result.inventoryPreserved, failure: result.failure || null }));
  if (!result.passed) process.exitCode = 1;
}
main().catch(error => {
  const folder = path.join(root, '.build-check/demo-recovery-guards');
  fs.mkdirSync(folder, { recursive: true });
  const output = path.join(folder, crypto.randomUUID().replaceAll('-', '') + '.json');
  fs.writeFileSync(output, JSON.stringify({ passed: false, type: error.name, code: error.code || null,
    at: new Date().toISOString(), scope: 'Guard or restoration failure; no application success claimed.' }, null, 2));
  console.error(JSON.stringify({ output, passed: false, type: error.name }));
  process.exitCode = 1;
});
