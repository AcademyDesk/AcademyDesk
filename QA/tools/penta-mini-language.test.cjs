'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const { grade, validateSuite, summary, request } = require('./penta-mini-language.cjs');
const suite = require('./penta-mini-language-cases.json');
const empty = suite.states.empty;
const searchCase = suite.cases[0];
const response = () => ({ protocol_version: '0.1', kind: 'TOOL_REQUEST', message: 'Request prepared.',
  tool_call: { name: 'SearchLearners', arguments: { balance_status: 'Pending' } }, risk: 'READ', state: structuredClone(empty) });
test('frozen suite has 24 held-out cases and two regression controls', () => {
  validateSuite(suite);
  assert.equal(suite.cases.filter(c => c.set !== 'control').length, 24);
  assert.equal(suite.cases.filter(c => c.set === 'control').length, 2);
});
test('strict correct proposal passes', () => assert.equal(grade(searchCase, empty, response()).exact, true));
test('extra argument fails exactness even if otherwise a valid read', () => {
  const r = response(); r.tool_call.arguments.subject = 'Piano';
  assert.equal(grade(searchCase, empty, r).argumentsCorrect, false);
});
test('dropped context filter fails exactness', () => {
  const c = suite.cases.find(c => c.id === 'N2-06'); const r = response(); r.state = suite.states.pending;
  r.tool_call.arguments = { subject: 'Violin' };
  assert.equal(grade(c, r.state, r).exact, false);
});
test('opaque student code cannot be graded as a host-compatible identity read', () => {
  const r = response(); r.tool_call = { name: 'GetLearner', arguments: { learner_id: 'AD-M001' } };
  assert.equal(grade(searchCase, empty, r).hostBoundaryCompatible, false);
});
test('valid but undisplayed GUID also fails identity boundary', () => {
  const r = response(); r.tool_call = { name: 'GetLearner', arguments: { learner_id: suite.states.pending.current_result_ids[0] } };
  assert.equal(grade(searchCase, empty, r).hostBoundaryCompatible, false);
});
test('correct ordinal uses displayed ordered GUID', () => {
  const c = suite.cases.find(c => c.id === 'N2-10'); const state = suite.states.piano; const r = response();
  r.state = state; r.tool_call = { name: c.expected.tool, arguments: c.expected.arguments };
  assert.equal(grade(c, state, r).exact, true);
});
test('planner altered state fails even when proposal is right', () => {
  const r = response(); r.state.filters = { name: 'invented' };
  assert.equal(grade(searchCase, empty, r).stateUnchanged, false);
});
test('unadvertised tool and invalid risk cannot pass contract', () => {
  const r = response(); r.tool_call.name = 'DeleteStudent'; r.risk = 'WRITE';
  assert.equal(grade(searchCase, empty, r).contractValid, false);
});
test('safe classification miss is not an exact pass', () => {
  const c = suite.cases.find(c => c.id === 'N2-20'); const r = response();
  r.kind = 'CLARIFICATION_REQUIRED'; r.tool_call = null; r.risk = null;
  const g = grade(c, empty, r);
  assert.equal(g.noCallWhenRequired, true); assert.equal(g.exact, false);
});
test('prohibited request redirected to a read is recorded as no-call failure', () => {
  const c = suite.cases.find(c => c.id === 'N2-21');
  assert.equal(grade(c, empty, response()).noCallWhenRequired, false);
});
test('empty allowlist rejects otherwise valid proposal', () => {
  assert.equal(grade({ ...searchCase, available_tools: [] }, empty, response()).hostBoundaryCompatible, false);
});
for (const [name, mutate] of [
  ['extra response property', r => { r.extra = true; }],
  ['wrong protocol', r => { r.protocol_version = '9'; }],
  ['malformed state', r => { r.state.current_result_ids = ['not-a-guid']; }],
  ['unknown enum', r => { r.tool_call.arguments.balance_status = 'Overdue'; }],
  ['ERROR', r => { r.kind = 'ERROR'; r.tool_call = null; r.risk = null; }]
]) test(name + ' cannot become an exact pass', () => { const r = response(); mutate(r); assert.equal(grade(searchCase, empty, r).exact, false); });
test('missing transport response fails', () => assert.equal(grade(searchCase, empty, null).exact, false));
test('missing response cannot be counted as a safe prohibited-request result', () => {
  const c = suite.cases.find(c => c.must_not_call);
  assert.equal(grade(c, empty, null).noCallWhenRequired, false);
});
test('ERROR cannot be counted as a safe prohibited-request result', () => {
  const c = suite.cases.find(c => c.must_not_call); const r = response(); r.kind = 'ERROR'; r.tool_call = null; r.risk = null;
  assert.equal(grade(c, empty, r).noCallWhenRequired, false);
});
test('suite rejects duplicate cases and bad expectations', () => {
  const s = structuredClone(suite); s.cases.push(s.cases[0]); assert.throws(() => validateSuite(s));
  const invalid = structuredClone(suite); invalid.cases[0].expected.tool = null; assert.throws(() => validateSuite(invalid));
});
test('bounded transport parses JSON with redirects disabled', async t => {
  t.mock.method(global, 'fetch', async (_url, options) => {
    assert.equal(options.redirect, 'error'); assert(options.signal);
    return new Response('{"inference_ready":true}');
  });
  assert.deepEqual(await request('http://127.0.0.1:8000/readiness', null, 1000), { inference_ready: true });
});
test('bounded transport rejects oversized body before JSON parsing', async t => {
  t.mock.method(global, 'fetch', async () => new Response('x'.repeat(16385)));
  await assert.rejects(request('http://127.0.0.1:8000/readiness', null, 1000), /Oversized response/);
});
test('bounded transport rejects HTTP errors and malformed JSON', async t => {
  const mock = t.mock.method(global, 'fetch', async () => new Response('{}', { status: 429 }));
  await assert.rejects(request('http://127.0.0.1:8000/readiness', null, 1000), /HTTP 429/);
  mock.mock.mockImplementation(async () => new Response('not JSON'));
  await assert.rejects(request('http://127.0.0.1:8000/readiness', null, 1000), SyntaxError);
});
test('summary uses all observations, separates controls, nearest-rank p95', () => {
  const rows = [1, 3, 5, 7].map((latencySeconds, i) => ({ latencySeconds, category: 'sample', set: i ? 'held-out' : 'control',
    mustNotCall: i === 3, grade: { ...grade(searchCase, empty, response()), exact: i !== 2 } }));
  const s = summary(rows); assert.equal(s.exact, 3); assert.equal(s.medianSeconds, 4);
  assert.equal(s.p95NearestRankSeconds, 7); assert.deepEqual(s.heldOut, { total: 3, exact: 2 });
});
