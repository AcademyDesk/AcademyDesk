'use strict';
// Separate actual api.ts realms with synthetic shared storage and a deterministic
// Web Locks adapter. Native browser/Web Locks acceptance is a separate runner.
const fs = require('node:fs'), vm = require('node:vm'), a = require('node:assert/strict'), { test } = require('node:test');
const ts = require('../../apps/web/node_modules/typescript');
const compile = file => ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
const code = compile(process.env.QA_CROSS_TAB_SOURCE || 'apps/web/src/lib/api.ts');
const deferred = () => { let resolve; const promise = new Promise(r => resolve = r); return { promise, resolve }; };
const tick = () => new Promise(r => setImmediate(r));
const workspaces = ['AcademyAdmin', 'Teacher', 'Portal', 'Platform'];
const paths = { AcademyAdmin: '/reports', Teacher: '/teacher/ai', Portal: '/portal/ai', Platform: '/platform/ai' };

function fixture(options = {}) {
  const storage = new Map(), calls = [], refreshes = [], queues = new Map(), lockRequests = [];
  const locks = { request(name, config, callback) {
    lockRequests.push({ name, signal: config.signal });
    if (options.lockError) return Promise.reject(Error('synthetic lock unavailable'));
    const predecessor = queues.get(name) || Promise.resolve();
    const release = deferred(); queues.set(name, predecessor.then(() => release.promise));
    return new Promise((resolve, reject) => {
      let granted = false;
      const abort = () => { if (!granted) reject(Error('synthetic lock aborted')); };
      config.signal.addEventListener('abort', abort, { once: true });
      predecessor.then(async () => {
        try {
          if (config.signal.aborted) throw Error('synthetic lock aborted');
          granted = true; config.signal.removeEventListener('abort', abort);
          resolve(await callback({ name }));
        } catch (error) { reject(error); }
        finally { config.signal.removeEventListener('abort', abort); release.resolve(); }
      });
    });
  } };
  function tab(workspace) {
    const events = new EventTarget(), mod = { exports: {} };
    const lateInitial = deferred(); let deferInitial = false;
    const window = { location: { pathname: paths[workspace] }, localStorage: {
      getItem: key => storage.get(key) ?? null,
      setItem: (key, value) => storage.set(key, String(value)), removeItem: key => storage.delete(key),
    }, addEventListener: events.addEventListener.bind(events), dispatchEvent: events.dispatchEvent.bind(events) };
    const fetch = async (url, init = {}) => {
      const call = { workspace, url, headers: new Headers(init.headers), body: init.body }; calls.push(call);
      if (url.endsWith('/api/auth/refresh')) {
        const gate = deferred(); refreshes.push({ gate, call, signal: init.signal });
        await Promise.race([gate.promise, new Promise((_, reject) => {
          const abort = () => reject(Error('synthetic fetch aborted'));
          if (init.signal.aborted) abort(); else init.signal.addEventListener('abort', abort, { once: true });
          gate.promise.then(() => init.signal.removeEventListener('abort', abort));
        })]);
        return { ok: !options.denied, json: async () => ({ accessToken: 'renewed-' + workspace, refreshToken: 'renewed-refresh-' + workspace }) };
      }
      const response = { status: options.replayDenied ? 401 : call.headers.get('Authorization') === 'Bearer renewed-' + workspace ? 200 : options.status || 401 };
      if (deferInitial && response.status === 401) { deferInitial = false; await lateInitial.promise; }
      return response;
    };
    vm.runInNewContext(code, { module: mod, exports: mod.exports, process: { env: {} }, window,
      ...(options.unsupported ? {} : { navigator: { locks } }),
      crypto: require('node:crypto').webcrypto, Headers, AbortController, CustomEvent, fetch,
      setTimeout: options.immediateTimeout ? callback => setTimeout(callback, 0) : setTimeout, clearTimeout,
    });
    return { api: mod.exports, request: (signal, path = '/api/synthetic') => mod.exports.academyApi(path, { method: 'POST', body: 'preserved', signal }),
      lateRequest: () => { deferInitial = true; return mod.exports.academyApi('/api/synthetic', { method: 'POST', body: 'preserved' }); }, releaseLate: lateInitial.resolve };
  }
  return { tab, storage, calls, refreshes, lockRequests, locks, seed(workspace, legacy = false) {
    const source = tab(workspace);
    if (legacy) source.api.savePortalTokens(workspace, 'old-' + workspace, 'refresh-' + workspace);
    else source.api.beginPortalSession(workspace, 'old-' + workspace, 'refresh-' + workspace);
    return source;
  } };
}

for (const workspace of workspaces) {
  test('two realms share one successful renewal: ' + workspace, async () => {
    const h = fixture(); h.seed(workspace); const first = h.tab(workspace), peer = h.tab(workspace);
    const one = first.request(); await tick(); const two = peer.request(); await tick();
    const concurrent = h.refreshes.length; h.refreshes.forEach(x => x.gate.resolve());
    const results = await Promise.all([one, two]);
    a.equal(concurrent, 1); a.deepEqual(results.map(x => x.status), [200, 200]);
    a.equal(h.refreshes.length, 1); a.equal(h.calls.length, 5);
    a.ok(h.calls.every(x => x.body === 'preserved' || x.url.endsWith('/refresh')));
    a.equal(h.lockRequests.length, 2); a.equal(new Set(h.lockRequests.map(x => x.name)).size, 1);
    a.ok(h.lockRequests.every(x => !/old-|refresh-|renewed-/.test(x.name)));
  });
  test('late old401 reuses same-epoch peer renewal: ' + workspace, async () => {
    const h = fixture(); h.seed(workspace); const first = h.tab(workspace), peer = h.tab(workspace);
    // Start peer's initial response before the first refresh finishes, deliver it later.
    const two = peer.lateRequest(); await tick(); const one = first.request(); await tick();
    h.refreshes.forEach(x => x.gate.resolve()); a.equal((await one).status, 200);
    peer.releaseLate(); a.equal((await two).status, 200); a.equal(h.refreshes.length, 1);
  });
  test('queued old realm rejects identical-pair replacement: ' + workspace, async () => {
    const h = fixture(), source = h.seed(workspace), first = h.tab(workspace), peer = h.tab(workspace);
    const one = first.request(); await tick(); const two = peer.request(); await tick();
    source.api.beginPortalSession(workspace, 'old-' + workspace, 'refresh-' + workspace);
    h.refreshes.forEach(x => x.gate.resolve());
    a.deepEqual((await Promise.all([one, two])).map(x => x.status), [401, 401]);
    a.equal(h.refreshes.length, 1); a.equal(h.calls.length, 3);
    a.equal(h.storage.get('academydesk.accessToken.' + workspace), 'old-' + workspace);
  });
  test('queued old realm rejects logout: ' + workspace, async () => {
    const h = fixture(), source = h.seed(workspace), first = h.tab(workspace), peer = h.tab(workspace);
    const one = first.request(); await tick(); const two = peer.request(); await tick(); source.api.clearPortalTokens(workspace);
    h.refreshes.forEach(x => x.gate.resolve());
    a.deepEqual((await Promise.all([one, two])).map(x => x.status), [401, 401]);
    a.equal(h.refreshes.length, 1); a.equal(h.storage.has('academydesk.accessToken.' + workspace), false);
  });
}
test('different workspaces do not block each other', async () => {
  const h = fixture(); h.seed('AcademyAdmin'); h.seed('Teacher');
  const one = h.tab('AcademyAdmin').request(), two = h.tab('Teacher').request(); await tick();
  const count = h.refreshes.length; h.refreshes.forEach(x => x.gate.resolve());
  a.deepEqual((await Promise.all([one, two])).map(x => x.status), [200, 200]); a.equal(count, 2);
  a.equal(new Set(h.lockRequests.map(x => x.name)).size, 2);
});
test('aborting queued realm never refreshes or cancels the active peer', async () => {
  const h = fixture(); h.seed('AcademyAdmin'); const first = h.tab('AcademyAdmin'), peer = h.tab('AcademyAdmin'), abort = new AbortController();
  const one = first.request(); await tick(); const two = peer.request(abort.signal); await tick(); abort.abort();
  a.equal((await two).status, 401); a.equal(h.refreshes[0].signal.aborted, false);
  h.refreshes[0].gate.resolve(); a.equal((await one).status, 200); await tick(); a.equal(h.refreshes.length, 1);
});
test('lock rejection fails closed without an unlocked duplicate', async () => {
  const h = fixture({ lockError: true }); h.seed('AcademyAdmin');
  a.equal((await h.tab('AcademyAdmin').request()).status, 401); a.equal(h.refreshes.length, 0);
});
test('bounded lock wait cancels without refresh', async () => {
  const h = fixture({ immediateTimeout: true }); h.seed('AcademyAdmin');
  const hold = deferred(), blocker = h.locks.request('academydesk:session-refresh:AcademyAdmin', { signal: new AbortController().signal }, () => hold.promise);
  await tick(); try { a.equal((await h.tab('AcademyAdmin').request()).status, 401); a.equal(h.refreshes.length, 0); }
  finally { hold.resolve(); await blocker; }
});
test('no Web Locks retains the existing same-realm path', async () => {
  const h = fixture({ unsupported: true }); h.seed('AcademyAdmin'); const page = h.tab('AcademyAdmin');
  const one = page.request(), two = page.request(); await tick(); h.refreshes.forEach(x => x.gate.resolve());
  a.deepEqual((await Promise.all([one, two])).map(x => x.status), [200, 200]); a.equal(h.refreshes.length, 1); a.equal(h.lockRequests.length, 0);
});
test('epoch-less legacy session retains its original independent refresh behavior', async () => {
  const h = fixture(); h.seed('AcademyAdmin', true); const one = h.tab('AcademyAdmin').request(); await tick(); const two = h.tab('AcademyAdmin').request(); await tick();
  h.refreshes.forEach(x => x.gate.resolve()); a.deepEqual((await Promise.all([one, two])).map(x => x.status), [200, 401]); a.equal(h.refreshes.length, 2); a.equal(h.lockRequests.length, 0);
});
test('aborted active owner releases the lock for a surviving peer', async () => {
  const h = fixture(); h.seed('AcademyAdmin'); const first = h.tab('AcademyAdmin'), peer = h.tab('AcademyAdmin'), abort = new AbortController();
  const one = first.request(abort.signal); await tick(); const two = peer.request(); await tick(); abort.abort();
  a.equal((await one).status, 401); await tick(); a.equal(h.refreshes.length, 2);
  h.refreshes.forEach(x => x.gate.resolve()); a.equal((await two).status, 200); a.equal(h.refreshes[0].signal.aborted, true);
});
test('denied owner releases the lock and never publishes a successful receipt', async () => {
  const h = fixture({ denied: true }); h.seed('AcademyAdmin');
  const one = h.tab('AcademyAdmin').request(); await tick(); const two = h.tab('AcademyAdmin').request(); await tick();
  h.refreshes[0].gate.resolve(); a.equal((await one).status, 401); await tick(); a.equal(h.refreshes.length, 2);
  h.refreshes[1].gate.resolve(); a.equal((await two).status, 401); a.equal(h.calls.length, 4);
  a.equal(h.storage.get('academydesk.accessToken.AcademyAdmin'), 'old-AcademyAdmin');
});
test('same-realm manual retry after a locked refresh failure remains possible', async () => {
  const options = { denied: true }, h = fixture(options); h.seed('AcademyAdmin'); const page = h.tab('AcademyAdmin');
  const one = page.request(); await tick(); h.refreshes[0].gate.resolve(); a.equal((await one).status, 401);
  options.denied = false; const two = page.request(); await tick(); h.refreshes[1].gate.resolve(); a.equal((await two).status, 200);
});
test('peer renewal replay401 is terminal, not a refresh loop', async () => {
  const h = fixture({ replayDenied: true }); h.seed('AcademyAdmin'); const one = h.tab('AcademyAdmin').request(); await tick(); const two = h.tab('AcademyAdmin').request(); await tick();
  h.refreshes.forEach(x => x.gate.resolve()); a.deepEqual((await Promise.all([one, two])).map(x => x.status), [401, 401]); a.equal(h.refreshes.length, 1); a.equal(h.calls.length, 5);
});
test('a changed pair without a refresh credential is not adopted as peer renewal', async () => {
  const h = fixture(); h.seed('AcademyAdmin'); const one = h.tab('AcademyAdmin').request(); await tick(); const two = h.tab('AcademyAdmin').request(); await tick();
  h.storage.set('academydesk.accessToken.AcademyAdmin', 'synthetic-unverified'); h.storage.delete('academydesk.refreshToken.AcademyAdmin');
  h.refreshes.forEach(x => x.gate.resolve()); a.deepEqual((await Promise.all([one, two])).map(x => x.status), [401, 401]); a.equal(h.refreshes.length, 1); a.equal(h.calls.length, 3);
});
for (const status of [403, 409, 429, 500]) test('status ' + status + ' never requests a refresh lock', async () => {
  const h = fixture({ status }); h.seed('AcademyAdmin'); a.equal((await h.tab('AcademyAdmin').request()).status, status); a.equal(h.lockRequests.length, 0);
});
