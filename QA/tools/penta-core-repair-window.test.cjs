const test=require('node:test'),a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const{bindings,validateApproval,validateAttempt,validationResult}=require('./penta-core-repair-window.cjs');
test('repair window rejects missing approval and any altered binding',()=>{
  a.throws(()=>validateApproval({approved:false,...bindings()}),/approval missing/);
  for(const key of Object.keys(bindings()))a.throws(()=>validateApproval({approved:true,...bindings(),[key]:'synthetic-invalid'}),/binding changed/);
});
test('attempt limit, deadline and source/code retries are independently enforced',()=>{
  a.doesNotThrow(()=>validateAttempt({deadline:20,attempts:[]},'new',10,'code-new'));
  a.throws(()=>validateAttempt({deadline:10,attempts:[]},'new',10),/expired/);
  a.throws(()=>validateAttempt({deadline:20,attempts:[{},{},{}]},'new',10),/Three-attempt/);
  a.throws(()=>validateAttempt({deadline:20,attempts:[{accepted:true}]},'new',10),/Successful window/);
  a.throws(()=>validateAttempt({deadline:20,attempts:[{mini:'new'}]},'new',10),/Same-source/);
  a.throws(()=>validateAttempt({deadline:20,attempts:[{mini:'old',sourceHash:'same-code'}]},'new',10,'same-code'),/Unchanged model-code/);
});
test('validation evidence must match exact source, hash, coverage and all passing counters',()=>{
  const dir=path.resolve(__dirname,'../../.build-check/d76-validation-tests');fs.mkdirSync(dir,{recursive:true});const file=path.join(dir,'synthetic.xml');
  const make=(xml,mini='synthetic-source')=>{fs.writeFileSync(file,xml);return{mini,xml:file,xmlSha256:require('crypto').createHash('sha256').update(xml).digest('hex')};};
  a.equal(validationResult(make('<testsuite tests="361" failures="0" errors="0" skipped="0"></testsuite>'),'synthetic-source'),361);
  a.throws(()=>validationResult(make('<testsuite tests="361" failures="1" errors="0" skipped="0"></testsuite>'),'synthetic-source'),/fully pass/);
  a.throws(()=>validationResult(make('<testsuite tests="360" failures="0" errors="0" skipped="0"></testsuite>'),'synthetic-source'),/coverage missing/);
  a.throws(()=>validationResult(make('<testsuite tests="361" failures="0" errors="0" skipped="0"></testsuite>'),'another-source'),/another Mini/);
});
