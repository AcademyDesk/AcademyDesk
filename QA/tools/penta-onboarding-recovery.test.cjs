'use strict';
// Additive same-request recovery checks; actual vendored validator, synthetic APIs.
const assert = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const { fixture, harness, conversation, draft, academy, actor, epoch } = require('./penta-onboarding-review.test.cjs');
const checks = [];
const json = (v, status=200) => new Response(JSON.stringify(v), { status, headers:{ 'Content-Type':'application/json' } });
function setup(options={}) {
  const f = fixture(); f.current.version = 2;
  const pending = { ...structuredClone(f.current), status:'pending', pendingRequestId:epoch, snapshot:null };
  const result = { conversationId:conversation, requestId:epoch, version:2, canExecute:false, draftId:draft,
    draftRevision:1, kind:'unsupported', status:'unsupported', operations:[], questions:[], snapshot:structuredClone(f.current.snapshot) };
  if (options.change) options.change(f, pending, result);
  const h = harness(f, { onRequest:options.onRequest, async respond(n, url, init) {
    if (options.waitAt === n) await options.wait;
    let v = n === 1 ? f.account : n === 2 ? pending : n === 3 ? (options.pending ? pending : f.current) :
      n === 4 ? (options.pending ? { conversationId:conversation, requestId:epoch, version:2, status:'pending', canExecute:false } : result) :
      n === 5 ? f.account : n === 7 ? f.view : f.current;
    v = structuredClone(v);
    if (options.at) options.at(n, v);
    return json(v, options.statusAt === n ? options.status : options.pending && n === 4 ? 202 : 200);
  } });
  return { ...h, async handle(signal) {
    let failure; try { await h.run(signal); } catch (error) { failure = error; }
    assert.equal(failure?.reason, 'pending'); assert.ok(failure.recovery); assert.equal(h.calls.length, 2);
    return failure.recovery;
  } };
}
async function check(name, run) { try { await run(); checks.push({ name, pass:true }); }
  catch (error) { checks.push({ name, pass:false, error:error.name }); console.error(`FAIL ${name}: ${error.message}`); } }
(async () => {
  await check('pending bootstrap captures only current server request and makes no status request automatically', async () => {
    const h = setup(); await h.handle(); assert.equal(h.calls.length, 2); assert.ok(h.calls.every(x => x.init.method === 'GET'));
  });
  await check('explicit pending check uses exact same request GET only and never resends', async () => {
    const h = setup({ pending:true }), handle = await h.handle(); assert.equal((await handle.check()).kind, 'pending');
    assert.equal(h.calls.length, 4); assert.ok(h.calls[3].url.endsWith(`/claims/${epoch}/result`));
    assert.ok(h.calls.every(x => x.init.method === 'GET' && !x.init.body));
  });
  await check('completed draft-only result requires fresh full projection and final same-turn cursor', async () => {
    const h = setup(), result = await (await h.handle()).check(); assert.equal(result.kind, 'review');
    assert.equal(h.calls.length, 9); const p = result.projection;
    assert.equal(p.host.scope.actor_id, actor); assert.equal(p.host.scope.tenant_id, academy);
    assert.equal(p.receipt, null); assert.equal(p.operation, null); assert.ok(Object.values(p.controls).every(x => x === false));
    assert.equal(p.host.draft.fields.StudentFirstName.value, 'SYNTHETIC-REVIEW-ONLY');
  });
  await check('partial clarification revision advances only to the fresh verified draft', async () => {
    const h = setup({ change(f, pending, result) {
      f.view.revision = 2; f.current.draftRevision = 2; f.current.snapshot.draft.revision = 2;
      result.kind = 'clarify'; result.status = 'draft_updated'; result.draftRevision = 2;
      result.questions = ['DateOfBirth']; result.snapshot = structuredClone(f.current.snapshot);
    } });
    const r = await (await h.handle()).check(); assert.equal(r.projection.host.draft.revision, 2);
  });
  for (const [name, mutate] of Object.entries({
    'wrong request': v => { v.requestId = actor; }, 'wrong version': v => { v.version++; },
    'execute grant': v => { v.canExecute = true; }, 'unknown status': v => { v.status = 'succeeded'; },
    'private extra field': v => { v.fields = { secret:'never expose' }; }
  })) await check(`withhold malformed pending ${name}`, async () => {
    const h = setup({ pending:true, at(n,v) { if (n === 4) mutate(v); } });
    await assert.rejects((await h.handle()).check()); assert.equal(h.calls.length, 4);
  });
  for (const [name, mutate] of Object.entries({
    'wrong request': v => { v.requestId = actor; }, 'wrong draft': v => { v.draftId = actor; },
    'wrong conversation': v => { v.conversationId = actor; }, 'wrong version': v => { v.version++; },
    'foreign actor': v => { v.snapshot.scope.actor_id = epoch; }, 'foreign tenant': v => { v.snapshot.scope.tenant_id = epoch; },
    'unknown kind': v => { v.kind = 'executed'; }, 'false success': v => { v.status = 'succeeded'; },
    'execute grant': v => { v.canExecute = true; }, 'unbound operation': v => { v.operations = [{}]; },
    'changed catalogue': v => { v.snapshot.tools[0].description = 'Different'; },
    'changed private fields': v => { v.snapshot.draft.fields.StudentFirstName.value = 'Different'; }
  })) await check(`withhold completed ${name}`, async () => {
    const h = setup({ at(n,v) { if (n === 4) mutate(v); } }); await assert.rejects((await h.handle()).check());
  });
  for (const [name, mutate] of Object.entries({
    'changed pending ID': v => { v.pendingRequestId = actor; }, 'changed conversation version': v => { v.version++; },
    'changed selected draft': v => { v.currentDraftId = actor; }, 'private pending snapshot': v => { v.snapshot = {}; },
    'pending revision changed': v => { v.draftRevision++; }
  })) await check(`no result dispatch after ${name}`, async () => {
    const h = setup({ pending:true, at(n,v) { if (n === 3) mutate(v); } });
    await assert.rejects((await h.handle()).check()); assert.equal(h.calls.length, 3);
  });
  for (const status of [401,403,404,409,410,429,503]) await check(`status HTTP ${status} has no retry or POST`, async () => {
    const h = setup({ statusAt:4, status }); await assert.rejects((await h.handle()).check());
    assert.equal(h.calls.length, 4); assert.ok(h.calls.every(x => x.init.method === 'GET'));
  });
  for (const n of [3,4,5,8,9]) await check(`changed token at request${n} discards result`, async () => {
    const h = setup({ onRequest(number, api) { if (number === n) api.replaceToken(); } });
    await assert.rejects((await h.handle()).check()); assert.equal(h.calls.length, n);
  });
  await check('aborted review cannot dispatch a pending status check', async () => {
    const abort = new AbortController(), h = setup(), handle = await h.handle(abort.signal); abort.abort();
    await assert.rejects(handle.check()); assert.equal(h.calls.length, 2);
  });
  await check('parallel double click admits only one status read sequence', async () => {
    let release; const wait = new Promise(resolve => { release = resolve; });
    const h = setup({ pending:true, waitAt:3, wait }), handle = await h.handle();
    const first = handle.check(); await assert.rejects(handle.check()); assert.equal(h.calls.length, 3);
    release(); assert.equal((await first).kind, 'pending'); assert.equal(h.calls.length, 4);
  });
  await check('later cursor change withholds an otherwise valid completed result', async () => {
    const h = setup({ at(n,v) { if (n === 9) v.version++; } }); await assert.rejects((await h.handle()).check()); assert.equal(h.calls.length, 9);
  });
  await check('completed cursor cannot be relabelled pending by a mismatched202 result', async () => {
    const h = setup({ statusAt:4, status:202 }); await assert.rejects((await h.handle()).check()); assert.equal(h.calls.length, 4);
  });
  for(const at of[3,4,5,6,7,8,9])await check('identical-token replacement in pending recovery read '+at+' withheld',async()=>{
    const h=setup({onRequest(n,tools){if(n===at)tools.replaceEpoch();}}),handle=await h.handle();
    await assert.rejects(handle.check());assert.equal(h.calls.length,at);const count=h.calls.length;
    await assert.rejects(handle.check());assert.equal(h.calls.length,count);
  });
  const report = { passed:checks.filter(x => x.pass).length, failed:checks.filter(x => !x.pass).length, checks,
    sqlAcceptance:false, browserAcceptance:false, inference:false, writes:0 };
  if (process.env.QA_PENTA_RECOVERY_OUTPUT) {
    const output = path.resolve(process.env.QA_PENTA_RECOVERY_OUTPUT); assert.ok(!fs.existsSync(output));
    fs.mkdirSync(path.dirname(output), { recursive:true }); fs.writeFileSync(output, JSON.stringify(report, null, 2));
  }
  console.log(JSON.stringify({ passed:report.passed, failed:report.failed }));
  if (report.failed) process.exitCode = 1;
})().catch(error => { console.error(error.message); process.exitCode = 1; });
