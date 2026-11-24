'use strict';
const test=require('node:test'),a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const{bindings,validateApproval,packetFileFor}=require('./penta-connected-query-evaluation.cjs');
const directory=path.resolve(__dirname,'../../.build-check/d73-evaluator-approval-tests');
fs.mkdirSync(directory,{recursive:true});
test('new approval must bind every current packet/evaluator/probe/source component',()=>{
  // Synthetic scope/hashes can never match the executable packet's bindings.
  const expected=Object.fromEntries(Object.keys(bindings()).map(key=>[key,'synthetic-test-'+key])),file=path.join(directory,'binding.json');
  fs.writeFileSync(file,JSON.stringify({approved:true,...expected}));
  a.doesNotThrow(()=>validateApproval(file,expected));
  for(const key of Object.keys(expected)){
    fs.writeFileSync(file,JSON.stringify({approved:true,...expected,[key]:'changed'}));
    a.throws(()=>validateApproval(file,expected),/Approval binding changed/);
  }
});
test('completed D65 comparison approval cannot authorize this query packet',()=>{
  const file=path.join(directory,'old-scope.json');
  fs.writeFileSync(file,JSON.stringify({approved:true,scope:'one-bounded-local-qwen35-4b-comparison',proposalSha256:'a'.repeat(64)}));
  a.throws(()=>validateApproval(file,bindings()),/Approval binding changed/);
});
test('proposed or missing approval never grants execution',()=>{
  const file=path.join(directory,'not-approved.json');
  fs.writeFileSync(file,JSON.stringify({approved:false,...bindings()}));
  a.throws(()=>validateApproval(file,bindings()),/Explicit approval missing/);
  a.throws(()=>validateApproval(path.join(directory,'missing.json'),bindings()),/ENOENT/);
});
test('packet selection accepts reviewed paths and rejects arbitrary or escaping files',()=>{
  a.equal(path.basename(packetFileFor('QA/HANDOFF/D74_CONTEXT_ISOLATION_4B_PACKET_2026-11-24.json')),'D74_CONTEXT_ISOLATION_4B_PACKET_2026-11-24.json');
  a.throws(()=>packetFileFor('.build-check/arbitrary.json'),/reviewed repository/);
  a.throws(()=>packetFileFor('QA/HANDOFF/arbitrary.json'),/Unreviewed model packet/);
  a.throws(()=>packetFileFor('QA/HANDOFF/../D73_CONNECTED_4B_PACKET_2026-11-24.json'),/reviewed repository/);
});
