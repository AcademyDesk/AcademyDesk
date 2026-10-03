// Rerun the accepted 50 print regressions because the product effect changed.
// Preserve the original test file; only teach its import boundary about the new real helper.
const fs = require('node:fs'), path = require('node:path'), Module = require('node:module');
const file = path.resolve(__dirname, 'certificate-print-ui.test.cjs');
const source = fs.readFileSync(file, 'utf8');
const anchor = "name === '@/lib/api' ?";
if (source.split(anchor).length !== 2) throw Error('Accepted print harness import anchor changed');
const adapted = source.replace(anchor, "name === '@/lib/certificate-image' ? require('./certificate-image-test-runtime.cjs') : " + anchor);
const harness = new Module(file, module); harness.filename = file; harness.paths = module.paths; harness._compile(adapted, file);
