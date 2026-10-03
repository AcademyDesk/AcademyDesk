// Load the real small TypeScript image guard without a browser emulator.
const fs = require('node:fs'), ts = require('../../apps/web/node_modules/typescript');
const code = ts.transpileModule(fs.readFileSync('apps/web/src/lib/certificate-image.ts', 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
new Function('module', 'exports', code)(module, module.exports);
