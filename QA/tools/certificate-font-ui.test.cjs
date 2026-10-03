// Successor adapter: rerun the original 50 print cases on actual changed TSX, not a re-audit.
const fs = require('node:fs'), path = require('node:path'), Module = require('node:module');
const file = path.resolve(__dirname, 'certificate-print-ui.test.cjs');
let source = fs.readFileSync(file, 'utf8');
function replaceOnce(before, after) { if(source.split(before).length!==2)throw Error('Accepted print harness anchor changed'); source=source.replace(before,after); }
replaceOnce("name === '@/lib/api' ?", "name === '@/lib/certificate-font' ? require('./certificate-font-test-runtime.cjs') : name === '@/lib/certificate-image' ? require('./certificate-image-test-runtime.cjs') : name === '@/lib/api' ?");
replaceOnce("'exports', 'window', code)", "'exports', 'window', 'document', code)");
replaceOnce('mod.exports, win);', 'mod.exports, win, { fonts: { ready: Promise.resolve() } });');
const harness = new Module(file, module); harness.filename = file; harness.paths = module.paths; harness._compile(source, file);
