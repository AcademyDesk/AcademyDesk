// Presentation preserves financial calculations. Invoice/payment save/load handlers
// now intentionally hardened and covered by invoice-payment-safety.test.cjs;
// do not compare those changed declarations to the older unsafe HEAD.
// Finance CSV was also deliberately hardened after HEAD. Run its actual-handler
// recovery/duplicate/MIME/cleanup tests here instead of freezing the unsafe body.
require('./csv-download-recovery.test.cjs');
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),cp=require('node:child_process'),ts=require('../../apps/web/node_modules/typescript');
const printer=ts.createPrinter({removeComments:true});
function declarations(source){const file=ts.createSourceFile('page.tsx',source,ts.ScriptTarget.Latest,true,ts.ScriptKind.TSX),nodes=new Map();function visit(n){if((ts.isFunctionDeclaration(n)||ts.isVariableDeclaration(n))&&n.name&&ts.isIdentifier(n.name))nodes.set(n.name.text,printer.printNode(ts.EmitHint.Unspecified,n,file));ts.forEachChild(n,visit);}visit(file);return nodes;}
for(const[route,names]of[['fee-plans',['money','frequencies','planStats']],['finance',['money','cards','modules']],['finance-summary',['money','netBillable','collectionRate','awaitingReconciliation','cards']],['invoices',['money','choosePlan','student','studentName','invoiceStats']],['payments',['money','balance','selectedInvoice','selectedBalance','paymentTotals','selectInvoice','studentName']],['expenses',['money','totals','load','refreshRegister','createExpense','change','branchName','filtered','blocked']]]){
const path=`apps/web/src/app/${route}/page.tsx`,before=declarations(cp.execFileSync('git',['show','HEAD:'+path],{encoding:'utf8'})),after=declarations(fs.readFileSync(path,'utf8'));
for(const name of names)test(route+' preserves accepted '+name,()=>{assert.ok(before.has(name),name);assert.equal(after.get(name),before.get(name));});}
