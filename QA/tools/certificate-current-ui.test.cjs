// Run the preserved 48 issuance/enrollment cases with the current real helper imports.
// Assertions and fixture behavior remain in the original certificate-ui.test.cjs.
const fs = require('node:fs'), path = require('node:path'), Module = require('node:module');
const file = path.resolve(__dirname, 'certificate-ui.test.cjs');
const source = fs.readFileSync(file, 'utf8');
const anchor = "name === '@/lib/api' ?";
if (source.split(anchor).length !== 2) throw Error('Certificate harness import boundary changed');
const adapted = source.replace(anchor,
  "name === '@/lib/certificate-font' ? require('./certificate-font-test-runtime.cjs') : " +
  "name === '@/lib/certificate-image' ? require('./certificate-image-test-runtime.cjs') : " + anchor);
const harness = new Module(file, module);
harness.filename = file;
harness.paths = module.paths;
harness._compile(adapted, file);
