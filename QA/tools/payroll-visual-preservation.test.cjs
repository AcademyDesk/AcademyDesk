// Compare against pre-edit working source, preserving earlier uncommitted payroll fixes.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),crypto=require('node:crypto'),ts=require('../../apps/web/node_modules/typescript'),snapshot=require('./fixtures/payroll-visual-preservation.json');
const file=ts.createSourceFile(snapshot.path,fs.readFileSync(snapshot.path,'utf8'),ts.ScriptTarget.Latest,true,ts.ScriptKind.TSX),printer=ts.createPrinter({removeComments:true}),map=new Map();
function visit(n){if(n.name&&ts.isIdentifier(n.name)&&(ts.isVariableDeclaration(n)||ts.isFunctionDeclaration(n)))map.set(n.name.text,printer.printNode(ts.EmitHint.Unspecified,n,file));ts.forEachChild(n,visit);}visit(file);
// Successor payroll recovery deliberately hardens these declarations; behavior tests replace unchanged claims.
const hardened=new Set(['load','refreshRegister','save','createProfile','pay']);
for(const entry of snapshot.entries.filter(x=>!hardened.has(x.name)))test('Payroll preserves '+entry.name,()=>assert.equal(crypto.createHash('sha256').update(map.get(entry.name)).digest('hex'),entry.sha256));
for(const [path,hash]of Object.entries(snapshot.hashes))test('Payroll preserves whole file '+path,()=>assert.equal(crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex'),hash));
