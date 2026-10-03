const { test } = require('node:test'), assert = require('node:assert/strict');
const { waitForCertificateFonts } = require('./certificate-font-test-runtime.cjs');
function deferred() { let resolve, reject; const ready=new Promise((a,b)=>{resolve=a;reject=b;}); return {ready,resolve,reject}; }
test('settled fonts proceed', async()=>{await waitForCertificateFonts({ready:Promise.resolve()},new AbortController().signal);});
test('loading fonts hold preparation until ready',async()=>{const fonts=deferred();let done=false;const pending=waitForCertificateFonts(fonts,new AbortController().signal).then(()=>done=true);await Promise.resolve();assert.equal(done,false);fonts.resolve();await pending;assert.equal(done,true);});
test('font readiness rejection does not authorize output',async()=>{await assert.rejects(waitForCertificateFonts({ready:Promise.reject(Error('fonts'))},new AbortController().signal),/failed/);});
test('readiness getter failure is rejected',async()=>{await assert.rejects(waitForCertificateFonts({get ready(){throw Error('fonts');}},new AbortController().signal),/failed/);});
test('slow fonts time out without an indefinite lock',async()=>{await assert.rejects(waitForCertificateFonts(deferred(),new AbortController().signal,5),/timed out/);});
test('close cancels waiting and later readiness does not revive it',async()=>{const fonts=deferred(),controller=new AbortController();let success=false;const pending=waitForCertificateFonts(fonts,controller.signal).then(()=>success=true);controller.abort();await assert.rejects(pending,/cancelled/);fonts.resolve();await Promise.resolve();assert.equal(success,false);});
test('already cancelled preparation cannot proceed',async()=>{const controller=new AbortController();controller.abort();await assert.rejects(waitForCertificateFonts({ready:Promise.resolve()},controller.signal),/cancelled/);});
test('browser without Font Loading API retains stable system-font fallback',async()=>{await waitForCertificateFonts(undefined,new AbortController().signal);});
