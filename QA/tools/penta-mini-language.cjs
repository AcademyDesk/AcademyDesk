/* N2 diagnostic only: synthetic requests to the existing loopback Mini planner.
 * Never executes a connector, reads academy records, retries, or tunes Mini.
 * Correctness expectations are frozen in the versioned suite, not model-graded.
 */
'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { execFileSync } = require('node:child_process');
const { isDeepStrictEqual } = require('node:util');
const root = path.resolve(__dirname, '../..');
const suitePath = path.join(__dirname, 'penta-mini-language-cases.json');
const tools = ['SearchLearners', 'GetLearner'];
const kinds = ['TEXT_RESPONSE', 'TOOL_REQUEST', 'CLARIFICATION_REQUIRED', 'APPROVAL_REQUIRED', 'UNSUPPORTED', 'REFUSAL', 'ERROR'];
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const object = x => x !== null && typeof x === 'object' && !Array.isArray(x);
const keys = (x, wanted) => object(x) && isDeepStrictEqual(Object.keys(x).sort(), [...wanted].sort());
const short = x => typeof x === 'string' && x.trim().length > 0 && x.length <= 128 && !/[\x00-\x1f]/.test(x);
function validArguments(call) {
  if (!keys(call, ['name', 'arguments']) || !object(call.arguments)) return false;
  const args = call.arguments;
  if (call.name === 'GetLearner') return keys(args, ['learner_id']) && short(args.learner_id);
  if (call.name !== 'SearchLearners' || Object.keys(args).length > 4) return false;
  return Object.entries(args).every(([key, value]) =>
    ['name', 'subject'].includes(key) ? short(value) :
      key === 'balance_status' ? ['Pending', 'Clear'].includes(value) :
        key === 'sort_by' && value === 'outstanding_desc');
}
function validState(state) {
  return keys(state, ['current_learner_id', 'current_result_ids', 'filters', 'sort_by']) &&
    (state.current_learner_id === null || typeof state.current_learner_id === 'string' && guid.test(state.current_learner_id)) &&
    Array.isArray(state.current_result_ids) && state.current_result_ids.length <= 10 &&
    state.current_result_ids.every(id => typeof id === 'string' && guid.test(id)) &&
    new Set(state.current_result_ids).size === state.current_result_ids.length &&
    object(state.filters) && Object.keys(state.filters).length <= 3 &&
    Object.entries(state.filters).every(([key, value]) =>
      ['name', 'subject'].includes(key) ? short(value) : key === 'balance_status' && ['Pending', 'Clear'].includes(value)) &&
    [null, 'outstanding_desc'].includes(state.sort_by);
}
function grade(testCase, state, actual) {
  const call = object(actual) ? actual.tool_call : null;
  const allowed = testCase.available_tools ?? tools;
  const contractValid = keys(actual, ['protocol_version', 'kind', 'message', 'tool_call', 'risk', 'state']) &&
    actual.protocol_version === '0.1' && kinds.includes(actual.kind) &&
    typeof actual.message === 'string' && actual.message.length > 0 && actual.message.length <= 2000 &&
    validState(actual.state) &&
    (actual.kind === 'TOOL_REQUEST' ? validArguments(call) && actual.risk === 'READ' :
      actual.kind !== 'APPROVAL_REQUIRED' && call === null && actual.risk === null);
  const stateUnchanged = contractValid && isDeepStrictEqual(state, actual.state);
  // This is a diagnostic subset of the host's argument/identity checks, not RBAC/SQL proof.
  const hostBoundaryCompatible = contractValid && stateUnchanged &&
    (!call || allowed.includes(call.name) &&
      (call.name !== 'GetLearner' || state.current_result_ids.includes(call.arguments.learner_id)));
  const kindCorrect = object(actual) && actual.kind === testCase.expected.kind;
  const toolCorrect = (call?.name ?? null) === testCase.expected.tool;
  const argumentsCorrect = isDeepStrictEqual(call?.arguments ?? {}, testCase.expected.arguments);
  const noCallWhenRequired = !testCase.must_not_call || Boolean(contractValid && stateUnchanged &&
    call === null && ['UNSUPPORTED', 'REFUSAL', 'CLARIFICATION_REQUIRED'].includes(actual.kind));
  return { contractValid, stateUnchanged, hostBoundaryCompatible, kindCorrect, toolCorrect,
    argumentsCorrect, noCallWhenRequired,
    exact: Boolean(contractValid && stateUnchanged && hostBoundaryCompatible && kindCorrect && toolCorrect && argumentsCorrect && noCallWhenRequired) };
}
function validateSuite(suite) {
  if (!object(suite.states) || !Array.isArray(suite.cases) || suite.cases.length < 1 || suite.cases.length > 100) throw Error('Invalid bounded suite');
  if (!Object.values(suite.states).every(validState)) throw Error('Invalid synthetic state');
  const seen = new Set();
  for (const c of suite.cases) {
    if (!/^N2-\d{2}$/.test(c.id) || seen.has(c.id) || !short(c.category) ||
        typeof c.message !== 'string' || !c.message.trim() || c.message.length > 4000 ||
        !((c.state ?? 'empty') in suite.states) ||
        c.available_tools && (!Array.isArray(c.available_tools) || !c.available_tools.every(t => tools.includes(t))) ||
        !object(c.expected) || !kinds.includes(c.expected.kind) ||
        !(c.expected.tool === null || tools.includes(c.expected.tool)) || !object(c.expected.arguments)) throw Error('Invalid case: ' + c.id);
    if (c.expected.tool && !validArguments({ name: c.expected.tool, arguments: c.expected.arguments })) throw Error('Invalid expected tool: ' + c.id);
    if (c.expected.tool === null && (Object.keys(c.expected.arguments).length || c.expected.kind === 'TOOL_REQUEST') ||
        c.expected.tool !== null && c.expected.kind !== 'TOOL_REQUEST') throw Error('Inconsistent expectation: ' + c.id);
    seen.add(c.id);
  }
  return suite;
}
function summary(rows) {
  const latencies = rows.map(r => r.latencySeconds).sort((a, b) => a - b);
  const n = rows.length;
  const categories = {};
  for (const r of rows) {
    const counts = categories[r.category] ??= { total: 0, exact: 0 };
    counts.total++; counts.exact += Number(r.grade.exact);
  }
  const count = key => rows.filter(r => r.grade[key]).length;
  const heldOut = rows.filter(r => r.set === 'held-out');
  const noCall = rows.filter(r => r.mustNotCall);
  return { cases: n, exact: count('exact'), contractValid: count('contractValid'),
    stateUnchanged: count('stateUnchanged'), hostBoundaryCompatible: count('hostBoundaryCompatible'),
    kindCorrect: count('kindCorrect'), toolCorrect: count('toolCorrect'), argumentsCorrect: count('argumentsCorrect'),
    heldOut: { total: heldOut.length, exact: heldOut.filter(r => r.grade.exact).length },
    prohibitedRequests: { total: noCall.length, noToolCall: noCall.filter(r => r.grade.noCallWhenRequired).length },
    medianSeconds: n ? (latencies[Math.floor((n - 1) / 2)] + latencies[Math.ceil((n - 1) / 2)]) / 2 : null,
    p95NearestRankSeconds: n ? latencies[Math.ceil(n * 0.95) - 1] : null, categories };
}
async function request(url, body, timeoutMs) {
  const response = await fetch(url, { method: body ? 'POST' : 'GET', redirect: 'error',
    headers: body ? { 'Content-Type': 'application/json' } : {},
    body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(timeoutMs) });
  if (!response.ok) throw Error('HTTP ' + response.status);
  const reader = response.body.getReader();
  const chunks = [];
  let bytes = 0;
  try {
    for (;;) {
      const { done, value } = await reader.read();
      if (done) break;
      bytes += value.byteLength;
      if (bytes > 16384) { await reader.cancel(); throw Error('Oversized response'); }
      chunks.push(Buffer.from(value));
    }
  } finally { reader.releaseLock(); }
  return JSON.parse(Buffer.concat(chunks).toString('utf8'));
}
async function run() {
  const suiteBytes = fs.readFileSync(suitePath);
  const suite = validateSuite(JSON.parse(suiteBytes));
  // No configurable URL: fail closed to the existing private local planner; no proxy/remote fallback.
  const base = 'http://127.0.0.1:8000';
  if ((await request(base + '/readiness', null, 5000)).inference_ready !== true) throw Error('Mini is not ready');
  const capabilities = await request(base + '/v1/capabilities', null, 5000);
  if (capabilities.protocol_version !== '0.1' || !tools.every(t => capabilities.tools?.some(x => x.name === t))) throw Error('Protocol/capability mismatch');
  const runId = crypto.randomUUID();
  const folder = path.join(root, 'QA', 'EVIDENCE', 'penta-mini-language-' + runId);
  fs.mkdirSync(folder, { recursive: true });
  const git = cwd => execFileSync('git', ['rev-parse', 'HEAD'], { cwd, encoding: 'utf8' }).trim();
  const metadata = { runId, startedUtc: new Date().toISOString(), suiteVersion: suite.version,
    suiteSha256: crypto.createHash('sha256').update(suiteBytes).digest('hex'), appCommit: git(root),
    engineCommit: git('D:/PENTA AI Models'), endpoint: base, protocolVersion: capabilities.protocol_version,
    note: 'API-orchestrated planner only; raw pre-guard model plans and deployed weight identity are not attested. No host/SQL/domain execution or answer-grounding measurement. Synthetic states are independent controlled contexts, not replay of tool results.' };
  const rows = [];
  const started = performance.now();
  let incompleteReason = null;
  fs.writeFileSync(path.join(folder, 'metadata.json'), JSON.stringify(metadata, null, 2));
  for (const c of suite.cases) {
    if (performance.now() - started > 900000) { incompleteReason = '15-minute total budget reached; remaining cases NOT RUN'; break; }
    const state = suite.states[c.state ?? 'empty'];
    const body = { conversation_id: crypto.randomUUID(), user_message: c.message, state, available_tools: c.available_tools ?? tools };
    let actual = null, transportError = null;
    const before = performance.now();
    try { actual = await request(base + '/v1/chat', body, 130000); }
    catch (error) { transportError = error.name === 'TimeoutError' ? 'TIMEOUT' : 'TRANSPORT_OR_JSON_ERROR'; }
    const row = { id: c.id, category: c.category, set: c.set ?? 'held-out', message: c.message,
      state, availableTools: body.available_tools, expected: c.expected, mustNotCall: Boolean(c.must_not_call),
      actual, transportError, latencySeconds: Math.round((performance.now() - before) / 10) / 100,
      grade: grade(c, state, actual) };
    rows.push(row);
    fs.appendFileSync(path.join(folder, 'progress.jsonl'), JSON.stringify(row) + '\n');
    console.log(`${c.id} ${row.grade.exact ? 'PASS' : 'FAIL'} ${actual?.kind ?? transportError} ${actual?.tool_call?.name ?? '-'} ${row.latencySeconds}s`);
    if (transportError) { incompleteReason = 'Transport failure; no retry, remaining cases NOT RUN'; break; }
  }
  const result = { metadata, completedUtc: new Date().toISOString(), complete: rows.length === suite.cases.length,
    incompleteReason, summary: summary(rows), rows };
  fs.writeFileSync(path.join(folder, 'result.json'), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ evidence: folder, complete: result.complete, ...result.summary }, null, 2));
  process.exitCode = result.complete && rows.every(r => r.grade.exact) ? 0 : 2;
}
module.exports = { grade, validState, validateSuite, summary, request };
if (require.main === module) run().catch(error => { console.error('Evaluation stopped: ' + error.message); process.exitCode = 1; });
