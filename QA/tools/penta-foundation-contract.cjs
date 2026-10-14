const assert = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
function verifyVendor() {
 const folder = path.join(root, 'apps/web/src/vendor/penta-shared-ui'), manifest = JSON.parse(fs.readFileSync(path.join(folder,'UPSTREAM.json'),'utf8'));
 assert.equal(manifest.version,'0.1.0-fw1.r1'); assert.equal(manifest.tokenVersion,'1.0.0-fw1.r1');
 for(const file of manifest.files) { const content=fs.readFileSync(path.join(folder,file.name),'utf8').replace(/\r\n/g,'\n').trimEnd()+'\n';assert.equal(crypto.createHash('sha256').update(content).digest('hex'),file.normalizedSha256,'immutable vendor '+file.name); }
 return manifest;
}
async function assertManual(page, theme) {
 const root = page.locator('[data-penta-manual="fw1-r1"]');
 await root.waitFor(); try { await page.waitForFunction(theme=>document.querySelector('[data-penta-manual]')?.getAttribute('data-penta-theme')===(theme==='light'?'prism':'orbit'),theme); } catch(error) { error.message+='\n'+JSON.stringify(await page.evaluate(()=>({html:document.documentElement.dataset.theme,roots:[...document.querySelectorAll('[data-penta-manual]')].map(e=>({tag:e.tagName,theme:e.getAttribute('data-penta-theme'),classes:e.className})),theme:localStorage.getItem('academydesk.theme')})));throw error; }
 assert.equal(await root.getAttribute('data-penta-foundation'),'0.1.0-fw1.r1');
 const tokens=await root.evaluate(e=>{const s=getComputedStyle(e);return Object.fromEntries(['--ad-canvas','--pui-canvas','--ad-control-surface','--pui-control','--ad-text','--pui-text','--manual-aura'].map(k=>[k,s.getPropertyValue(k).trim()]));});
 assert.equal(tokens['--ad-canvas'],tokens['--pui-canvas']);assert.equal(tokens['--ad-control-surface'],tokens['--pui-control']);assert.equal(tokens['--ad-text'],tokens['--pui-text']);
 assert.notEqual(tokens['--ad-control-surface'],tokens['--ad-canvas'],'fields distinct from canvas');assert.equal(tokens['--ad-canvas'],theme==='light'?'#f4f7fb':'#07111f');assert.ok(tokens['--manual-aura']);
}
module.exports={verifyVendor,assertManual};
if(require.main===module){const manifest=verifyVendor();console.log('PASS pinned vendor '+manifest.files.length+' files / '+manifest.version);}
