const { test } = require('node:test');
const assert = require('node:assert/strict');
const { safeClientFailure } = require('./safe-client-failure.cjs');
test('Missing browser globals are identified without publishing a raw stack',()=>{
  const error=new ReferenceError('AbortController is not defined');error.stack='ReferenceError: private message\n at /private/folder/session-refresh-http.cjs:12:7';
  assert.deepEqual(safeClientFailure(error),['QA client failure: ReferenceError missing=AbortController','QA failure frame: session-refresh-http.cjs:12']);
});
test('Numeric HTTP/count mismatches can be diagnosed safely',()=>{
  assert.deepEqual(safeClientFailure({name:'AssertionError',expected:200,actual:401}),['QA client failure: AssertionError expected=200 actual=401']);
});
test('Credential-shaped messages, bodies and assertion strings never enter output',()=>{
  const marker='DO_NOT_EXPORT_PRIVATE_TEST_VALUE';
  const error={name:'AssertionError',message:marker,expected:marker,actual:{accessToken:marker},stack:'Error: '+marker+'\n at /private/'+marker+'.cjs:1:1'};
  assert.deepEqual(safeClientFailure(error),['QA client failure: AssertionError']);assert.ok(!safeClientFailure(error).join('').includes(marker));
});
test('Unknown names, missing globals and large numeric identifiers are excluded',()=>{
  assert.deepEqual(safeClientFailure({name:'PRIVATE_NAME',message:'privateIdentifier is not defined',actual:123456789,expected:0}),['QA client failure: Error']);
});
