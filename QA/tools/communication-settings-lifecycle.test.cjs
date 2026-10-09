// Reuse accepted actual-TSX harness, not a replacement for browser/live SQL proof.
const test=require('node:test'),assert=require('node:assert/strict');
const {channels,nodes,page}=require('./communication-page-harness.cjs');
for(const channel of channels)test(channel+' edits/notices/pending never reload initial settings',async()=>{
 const p=await page();assert.equal(p.calls.length,2);p.edit(channel,'senderName','Lifecycle fixture');await p.flush();assert.equal(p.calls.length,2);
 await p.save(channel);await p.flush();assert.equal(p.calls.length,3);assert.equal(p.writes().length,1);assert.match(p.notice(channel),/settings saved/);
 await p.flush();assert.equal(p.calls.length,3);
 const n=nodes(p.render()).find(n=>n.props?.['data-channel-notice']===channel);assert.equal(n.props.role,'status');assert.equal(n.props['aria-live'],'polite');
});
