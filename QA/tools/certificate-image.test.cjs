const { test } = require('node:test'), assert = require('node:assert/strict');
const { waitForCertificateImage } = require('./certificate-image-test-runtime.cjs');
function image(overrides = {}) {
  const target = new EventTarget(), listeners = new Set();
  return Object.assign(target, { complete: false, naturalWidth: 0, naturalHeight: 0,
    addEventListener(type, fn) { listeners.add(type); EventTarget.prototype.addEventListener.call(this, type, fn); },
    removeEventListener(type, fn) { listeners.delete(type); EventTarget.prototype.removeEventListener.call(this, type, fn); }, listeners }, overrides);
}
test('already loaded logo waits for decoding', async () => {
  let release; const logo = image({ complete: true, naturalWidth: 64, naturalHeight: 64, decode: () => new Promise(resolve => release = resolve) });
  let done = false; const pending = waitForCertificateImage(logo, new AbortController().signal).then(() => done = true);
  await Promise.resolve(); assert.equal(done, false); release(); await pending; assert.equal(done, true); assert.equal(logo.listeners.size, 0);
});
test('delayed logo cannot complete before load', async () => {
  const logo = image(); let done = false; const pending = waitForCertificateImage(logo, new AbortController().signal).then(() => done = true);
  await Promise.resolve(); assert.equal(done, false); Object.assign(logo, { complete: true, naturalWidth: 64, naturalHeight: 64 }); logo.dispatchEvent(new Event('load')); await pending; assert.equal(done, true); assert.equal(logo.listeners.size, 0);
});
test('load error rejects and removes listeners', async () => { const logo = image(); const pending = waitForCertificateImage(logo, new AbortController().signal); logo.dispatchEvent(new Event('error')); await assert.rejects(pending, /could not be loaded/); assert.equal(logo.listeners.size, 0); });
test('complete broken image cannot pass', async () => { const logo = image({ complete: true }); await assert.rejects(waitForCertificateImage(logo, new AbortController().signal), /could not be loaded/); });
test('decode failure rejects', async () => { const logo = image({ complete: true, naturalWidth: 64, naturalHeight: 64, decode: () => Promise.reject(Error('decode')) }); await assert.rejects(waitForCertificateImage(logo, new AbortController().signal), /could not be loaded/); });
test('timeout is bounded and cleans up', async () => { const logo = image(); await assert.rejects(waitForCertificateImage(logo, new AbortController().signal, 5), /timed out/); assert.equal(logo.listeners.size, 0); });
test('close/unmount abort rejects without waiting for network', async () => { const logo = image(), controller = new AbortController(); const pending = waitForCertificateImage(logo, controller.signal); controller.abort(); await assert.rejects(pending, /cancelled/); assert.equal(logo.listeners.size, 0); });
test('already aborted preparation never proceeds', async () => { const controller = new AbortController(); controller.abort(); await assert.rejects(waitForCertificateImage(image({ complete: true, naturalWidth: 64, naturalHeight: 64 }), controller.signal), /cancelled/); });
