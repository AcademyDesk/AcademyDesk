// Current frontend regression entry point. Historical inputs remain unchanged and
// execute through their latest adapters; missing SQL evidence must still fail.
const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');

const root = path.resolve(__dirname, '../..');
const output = path.join(root, '.build-check/frontend-tests');
const successors = new Map([
  ['certificate-ui.test.cjs', 'certificate-current-ui.test.cjs'],
  ['certificate-print-ui.test.cjs', 'certificate-font-ui.test.cjs'],
  ['certificate-appearance-ui.test.cjs', 'certificate-font-ui.test.cjs'],
]);
const files = fs.readdirSync(__dirname).filter(file => file.endsWith('.test.cjs') && !successors.has(file)).sort();
for (const [input, successor] of successors) {
  if (!fs.existsSync(path.join(__dirname, input)) || !files.includes(successor))
    throw Error(`Missing preserved certificate input or successor: ${input} -> ${successor}`);
  console.log(`Preserved test input: ${input} -> ${successor}`);
}
if (!files.length) throw Error('No frontend tests discovered');
fs.mkdirSync(output, { recursive: true });

function run(file) {
  return new Promise(resolve => {
    const child = spawn(process.execPath, [path.join(__dirname, file)], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
    let log = '', timedOut = false, settled = false;
    const timer = setTimeout(() => { timedOut = true; child.kill('SIGTERM'); }, 90_000);
    const forceTimer = setTimeout(() => { if (timedOut) child.kill('SIGKILL'); }, 95_000);
    const append = data => { log += data.toString(); };
    child.stdout.on('data', append); child.stderr.on('data', append);
    function finish(exit, error) {
      if (settled) return;
      settled = true; clearTimeout(timer); clearTimeout(forceTimer);
      if (error) log += '\n' + error.message;
      fs.writeFileSync(path.join(output, file + '.log'), log);
      const counts = {};
      for (const key of ['tests', 'pass', 'fail', 'skipped', 'cancelled']) {
        const matches = [...log.matchAll(new RegExp(`(?:^|\\n)(?:ℹ |# )${key} (\\d+)`, 'g'))];
        counts[key] = matches.length ? Number(matches.at(-1)[1]) : null;
      }
      resolve({ file, exit, timedOut, ...counts, complete: exit === 0 && !timedOut && counts.tests > 0 &&
        counts.fail === 0 && counts.cancelled === 0 && counts.pass + counts.skipped === counts.tests });
    }
    child.on('error', error => finish(1, error));
    child.on('close', code => finish(code ?? 1));
  });
}

async function main() {
  const results = [];
  let next = 0;
  async function worker() {
    while (next < files.length) {
      const file = files[next++], result = await run(file);
      results.push(result);
      if (!result.complete) console.error(`FAIL ${file}: see .build-check/frontend-tests/${file}.log`);
    }
  }
  await Promise.all(Array.from({ length: Math.min(4, files.length) }, worker));
  results.sort((a, b) => a.file.localeCompare(b.file));
  const totals = Object.fromEntries(['tests', 'pass', 'fail', 'skipped', 'cancelled'].map(key =>
    [key, results.reduce((sum, result) => sum + (result[key] ?? 0), 0)]));
  const summary = { historicalInputs: Object.fromEntries(successors), results, totals };
  fs.writeFileSync(path.join(output, 'summary.json'), JSON.stringify(summary, null, 2) + '\n');
  console.log(`${results.length} current test files: ${totals.tests} tests; ${totals.pass} passed; ${totals.fail} failed; ` +
    `${totals.skipped} skipped; ${totals.cancelled} cancelled.`);
  process.exitCode = results.some(result => !result.complete) ? 1 : 0;
}

main().catch(error => { console.error(error); process.exitCode = 1; });
