// Error diagnostics must never publish tokens, response bodies or raw messages.
const names = new Set(['Error', 'AssertionError', 'ReferenceError', 'TypeError', 'SyntaxError', 'AbortError']);
const globals = ['AbortController', 'Headers', 'fetch', 'URL', 'DOMException', 'setTimeout', 'clearTimeout'];
function safeClientFailure(error) {
  let line = 'QA client failure: ' + (names.has(error?.name) ? error.name : 'Error');
  if (Number.isInteger(error?.expected) && Number.isInteger(error?.actual) &&
      error.expected >= 0 && error.expected <= 9999 && error.actual >= 0 && error.actual <= 9999)
    line += ` expected=${error.expected} actual=${error.actual}`;
  const missing = globals.find(name => error?.name === 'ReferenceError' && error.message === name + ' is not defined');
  if (missing) line += ' missing=' + missing;
  const frame = typeof error?.stack === 'string' && error.stack.match(/\b(session-refresh-http\.cjs|refresh-concurrency-http\.cjs):([0-9]+):[0-9]+/);
  return frame ? [line, `QA failure frame: ${frame[1]}:${frame[2]}`] : [line];
}
module.exports = { safeClientFailure };
