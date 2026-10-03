/* Validates QA documentation consistency only. Never reports app behavior as PASS. */
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');
const { resolveControl, reconcileCoverage } = require('./coverage.cjs');
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const inv=JSON.parse(read('QA/INVENTORY/source-inventory.json'));
const cases=JSON.parse(read('QA/test-cases.json'));
const registry=JSON.parse(read('QA/registry.json'));
const counts=JSON.parse(read('QA/REPORTS/counts.json'));
const files=d=>fs.readdirSync(path.join(root,d),{withFileTypes:true}).flatMap(e=>e.isDirectory()?files(`${d}/${e.name}`):[`${d}/${e.name}`]);
const checks=[];
const cp=require('node:child_process');
const observed=JSON.parse(read('QA/EVIDENCE/logs/observed-checks.json'));
function check(name,fn){try{fn();checks.push({name,status:'PASS'});}catch(e){checks.push({name,status:'FAIL',message:e.message});}}
check('current commit matches recorded evidence',()=>assert.equal(cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),observed.commit));
check('source file set matches inventory (including new files)',()=>{
 const walk=d=>fs.readdirSync(path.join(root,d),{withFileTypes:true}).flatMap(e=>['node_modules','bin','obj','.next','out','wwwroot'].includes(e.name)?[]:e.isDirectory()?walk(`${d}/${e.name}`):[`${d}/${e.name}`]);
 const actual=['apps/web/src','apps/api','tests'].flatMap(walk).filter(f=>/\.(tsx?|cs|csproj|css)$/.test(f)).sort();
 assert.deepEqual(actual,inv.files.map(f=>f.file).sort());
});
check('recorded source fingerprint matches inventory',()=>assert.equal(crypto.createHash('sha256').update(JSON.stringify(inv.files.map(f=>[f.file,f.sha256]))).digest('hex'),observed.sourceFingerprint));
check('tracked repository hashes match including configuration and lockfiles',()=>{
 const entries=[...read('QA/INVENTORY/REPOSITORY_MANIFEST.md').matchAll(/^\| (.+?) \| ([a-f0-9]{64}) \|$/gm)];
 assert(entries.length>0);
 for(const [,file,hash] of entries)assert.equal(crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex'),hash,file);
});
check('native form occurrence keys are unique and fields resolve',()=>{
 const ids=new Set(inv.forms.map(f=>f.id));
 assert.equal(ids.size,inv.forms.length);
 for(const f of inv.fields)if(f.form)assert(ids.has(f.form),`${f.file}:${f.line}`);
});
check('retained evidence matches recorded digests',()=>{
 const manifest=JSON.parse(read('QA/EVIDENCE/logs/phase1-evidence-hashes.json'));
 for(const f of manifest.hashes)assert.equal(crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'QA/EVIDENCE/logs',f.file))).digest('hex'),f.sha256,f.file);
});
check('acceptance and conditional next-phase document links resolve',()=>{
 for(const f of ['QA/REPORTS/PHASE_1_ACCEPTANCE.md','QA/REPORTS/PHASE_2_START_PLAN.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){
  const target=m[1].split('#')[0];if(!target||/^https?:/.test(target))continue;
  assert(fs.existsSync(path.resolve(root,path.dirname(f),target)),`${f}: ${target}`);
 }
});
check('existing test results reconcile with retained TRX',()=>{
 const trx=read('QA/EVIDENCE/logs/phase1-existing.trx');
 const counter=trx.match(/<Counters\s+[^>]+/)[0];
 for(const key of ['total','passed','failed'])assert.equal(Number(counter.match(new RegExp(`${key}="(\\d+)"`))[1]),observed.existingTests[key]);
 assert.equal((trx.match(/<UnitTestResult\s/g)||[]).length,observed.existingTests.total);
});
check('lint totals reconcile with retained diagnostics',()=>{
 const lint=JSON.parse(read('QA/EVIDENCE/logs/eslint.json'));
 assert.equal(lint.reduce((n,f)=>n+f.errorCount,0),observed.lint.errors);
 assert.equal(lint.reduce((n,f)=>n+f.warningCount,0),observed.lint.warnings);
});
check('required control-centre documents',()=>{for(const p of ['00_QA_README','01_APPLICATION_INVENTORY','02_TEST_MASTER_PLAN','03_TEST_MATRIX','04_ROLE_PERMISSION_MATRIX','05_BUSINESS_WORKFLOW_MATRIX','06_DEVICE_VIEWPORT_MATRIX','07_VISUAL_TEST_MATRIX','08_SECURITY_TEST_MATRIX','09_RELEASE_GATES','10_TEST_DATA_STRATEGY'])assert(fs.existsSync(path.join(root,`QA/${p}.md`)));assert(fs.existsSync(path.join(root,'QA/REPORTS/PHASE_1_AUDIT_REPORT.md')));});
check('unique valid scenario IDs',()=>{assert.equal(new Set(cases.map(c=>c.id)).size,cases.length);for(const c of cases)assert.match(c.id,/^[A-Z][A-Z0-9-]+-\d{3}$/);});
check('every controller action has its scenario',()=>{for(const e of inv.endpoints){const id=registry.entries[`${e.method} ${e.route}::${e.controller}.${e.name}`];assert(cases.some(c=>c.id===id&&c.source===`${e.file}:${e.line}`),e.route);}});
check('every route has visual/state coverage',()=>{for(const p of inv.pages)assert(cases.some(c=>c.id===registry.entries[`visual:${p.route}`]),p.route);});
check('every native form has an explicit scenario',()=>{for(const f of inv.forms){const key=`form:${f.file}:${f.handler}:${inv.forms.filter(x=>x.file===f.file&&x.handler===f.handler).indexOf(f)}`;assert(cases.some(c=>c.id===registry.entries[key]&&c.source===`${f.file}:${f.line}`),key);}});
check('counts reconcile',()=>{assert.equal(cases.length,counts.registeredScenarios);assert.equal(Object.values(counts.testPriorities).reduce((a,b)=>a+b,0),cases.length);assert.equal(Object.values(counts.proposedPriorities).reduce((a,b)=>a+b,0),counts.proposedTests);assert.equal(counts.issues,files('QA/ISSUES').filter(p=>/BUG-.*\.md$/.test(p)).length);});
check('issue references resolve',()=>{for(const c of cases)for(const id of c.issue.match(/BUG-[A-Z]+-\d{4}/g)||[])assert(fs.existsSync(path.join(root,`QA/ISSUES/${id}.md`)),id);});
check('application source hashes unchanged since inventory',()=>{for(const f of inv.files)assert.equal(crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f.file))).digest('hex'),f.sha256,f.file);});
check('no unexecuted proposal reported as PASS',()=>{for(const c of cases.filter(c=>c.status==='PASS'))assert(c.id.startsWith('EXISTING-')||c.id==='QUALITY-TYPE-001',c.id);});
check('all issues remain open',()=>{for(const f of files('QA/ISSUES').filter(p=>/BUG-.*\.md$/.test(p)))assert(read(f).includes('| Status | OPEN |'),f);});
check('issues have explicit confirmation and final-verification states',()=>{
 for(const f of files('QA/ISSUES').filter(p=>/BUG-.*\.md$/.test(p))){
  const content=read(f);
  assert(content.includes(`| Confirmation status | ${f.endsWith('BUG-FUNC-0004.md')?'RUNTIME-REPRODUCED':'STATIC-FINDING'} |`),f);
  assert(content.includes('| Final verification | NOT RUN |'),f);
 }
});
check('reviewed coverage batch matches pinned inventory and permanent Test IDs',()=>{
 const progress=JSON.parse(read('QA/COVERAGE/progress.json'));
 assert.equal(progress.sourceFingerprint,observed.sourceFingerprint);
 assert.equal(new Set(progress.reviewedForms.map(f=>f.form)).size,progress.reviewedForms.length);
 for(const f of progress.reviewedForms){
  assert(inv.forms.some(x=>x.id===f.form),f.form);
  assert.equal(inv.fields.filter(x=>x.form===f.form).length,f.fields,f.form);
  assert(cases.some(c=>c.id===f.testId),f.testId);
  assert(read(f.document).includes(f.testId),f.document);
  assert.equal(f.runtime,'NOT RUN');
 }
 for(const controller of progress.reviewedControllers){
  assert(inv.endpoints.some(e=>e.controller===controller),controller);
  assert(read(progress.controllerDocuments?.[controller]||progress.controllerDocument).includes(controller.replace('Controller','')),controller);
 }
 const standalone=progress.reviewedStandaloneControls||[];
 assert.equal(new Set(standalone.map(f=>`${f.file}:${f.name}:${f.line??''}`)).size,standalone.length);
 for(const f of standalone){
  resolveControl(inv.fields, f);
  assert(read(f.document).includes(f.name),f.name);
  if(f.document==='QA/COVERAGE/31_SHARED_CONTROL_MAPPINGS.md') {
   assert(f.testIds?.length, `Missing scenario link: ${f.name}`);
   for(const id of f.testIds) { assert(cases.some(c=>c.id===id),id); assert(read(f.document).includes(id),id); }
  }
  assert.equal(f.runtime,'NOT RUN');
 }
 for(const interaction of progress.partialInteractions||[]){
  assert(read(interaction.file).includes(interaction.handler),interaction.file);
  assert(cases.some(c=>c.id===interaction.testId),interaction.testId);
  assert(read(interaction.document).includes(interaction.testId),interaction.document);
  assert.equal(interaction.runtime,'NOT RUN');
  assert(interaction.excludes);
 }
 for(const partial of progress.partialControllerReviews||[]){
  assert(!progress.reviewedControllers.includes(partial.controller),partial.controller);
  for(const action of partial.actions)assert(inv.endpoints.some(e=>e.controller===partial.controller&&e.name===action),`${partial.controller}.${action}`);
  assert(read(partial.document).includes(partial.controller),partial.document);
  assert.equal(partial.runtime,'NOT RUN');
  assert(partial.excludes);
 }
});
check('all declarations have distinct coverage mappings',()=>{
 const progress=JSON.parse(read('QA/COVERAGE/progress.json'));
 assert.deepEqual(reconcileCoverage(inv,progress),progress.declarationCounts);
});
check('framework endpoint specification is distinct from runtime evidence',()=>{
 const progress=JSON.parse(read('QA/COVERAGE/progress.json'));
 const manifest=JSON.parse(read(progress.frameworkManifest));
 assert.equal(manifest.runtime,'NOT RUN');
 assert(cases.some(c=>c.id===manifest.testId&&c.status==='NOT RUN'),manifest.testId);
 assert.equal(manifest.endpoints.length,10);
 assert.equal(new Set(manifest.endpoints.map(e=>`${e.method} ${e.route}`)).size,10);
 for(const e of manifest.endpoints){assert(e.route.startsWith('/api/auth/'));assert(e.authorization);assert(e.input);}
 assert(read(manifest.applicationMapping).includes('MapIdentityApi<ApplicationUser>()'));
 assert(manifest.source.includes('/v10.0.12/'));
});
check('semantic evidence locator matches inventory without claiming acceptance',()=>{
 const {currentIndex,validateIndex}=require('./semantic-index.cjs');
 validateIndex(JSON.parse(read('QA/COVERAGE/semantic-index.json')),currentIndex());
});
check('no unapproved golden images',()=>{assert.equal(files('QA/BASELINES/golden-images').filter(p=>/\.(png|jpg|webp)$/i.test(p)).length,0);});
check('human-authored control-document local links resolve',()=>{for(const f of [...files('QA').filter(p=>/^QA\/\d\d_.*\.md$/.test(p)),'QA/REPORTS/PHASE_1_AUDIT_REPORT.md'])for(const m of read(f).matchAll(/\]\(([^)]+)\)/g)){const target=m[1].split('#')[0];if(!target||/^https?:/.test(target))continue;assert(fs.existsSync(path.resolve(root,path.dirname(f),target)),`${f}: ${target}`);}});
const result={checkType:'QA artifact consistency, NOT application testing',timestamp:new Date().toISOString(),checks};
fs.writeFileSync(path.join(root,'QA/REPORTS/ARTIFACT_VALIDATION.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result,null,2));
if(checks.some(c=>c.status==='FAIL'))process.exitCode=1;
