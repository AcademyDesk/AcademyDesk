// Actual attendance handlers with synthetic delayed responses; no SQL/browser proof.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');
async function fixture(options = {}) {
  const reads = [], writes = [];
  const rows = id => [{ studentId: 's', status: id === 'one' ? 'Present' : 'Late', notes: id + ' saved note' }];
  const h = mount('apps/web/src/app/attendance/page.tsx', async (url, init = {}) => {
    if (url === '/api/academies') return response([{ id: 'a' }]);
    if (url.endsWith('/students')) return response([{ id: 's', firstName: 'Shared', lastName: 'Learner' }]);
    if (url.endsWith('/batches')) return response([{ id: 'b', name: 'Synthetic batch' }]);
    if (url.endsWith('/enrollments')) return response([{ studentId: 's', batchId: 'b', status: 'Active' }]);
    if (url.endsWith('/sessions')) return response(['one','two'].map(id => ({ id, batchId: 'b', startUtc: '2026-10-29T10:00:00Z' })));
    const id = url.match(/sessions\/(one|two)\/attendance/)?.[1]; assert.ok(id, url);
    if (init.method) { writes.push({ url, ...init }); return options.write ? options.write(id, init) : response({}); }
    reads.push(id); return options.read ? options.read(id, rows(id), reads.filter(x => x === id).length) : response(rows(id));
  }); await h.flush();
  const input = () => nodes(h.render()).find(n => n.type === 'input' && n.props.placeholder === 'Notes (optional)');
  const save = () => nodes(h.render()).find(n => n.type === 'button' && n.props.className === 'attendance-save');
  const select = async id => { nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === 'session').props.onChange(id); await h.flush(); };
  return { h, reads, writes, rows, input, save, select };
}
test('Revisiting a cached session stays locked until its fresh read completes',async()=>{
  const wait=deferred(),f=await fixture({read:(id,data,count)=>id==='one'&&count>1?wait.promise:response(data)});
  await f.select('two');await f.select('one');assert.equal(f.input(),undefined);assert.equal(f.save(),undefined);
  wait.resolve(response(f.rows('one')));await f.h.flush();assert.equal(f.input().props.value,'one saved note');f.h.unmount();
});
test('Failed cached reload is locked and explicit retry recovers',async()=>{
  let fail=true;const f=await fixture({read:(id,data,count)=>response(data,id!=='one'||count===1||!fail)});
  await f.select('two');await f.select('one');assert.equal(f.input(),undefined);assert.equal(f.save(),undefined);
  fail=false;const retry=nodes(f.h.render()).find(n=>n.type==='button'&&/retry|reload|try again/i.test(text(n)));assert.ok(retry);
  retry.props.onClick();await f.h.flush();assert.equal(f.input().props.value,'one saved note');f.h.unmount();
});
test('Retired session save callbacks cannot write after selection',async()=>{
  const f=await fixture(),click=f.save().props.onClick;await f.select('two');click();await f.h.flush();assert.equal(f.writes.length,0);f.h.unmount();
});
test('Late save completion does not read back into a new session or change its notice',async()=>{
  const wait=deferred(),f=await fixture({write:()=>wait.promise});f.save().props.onClick();await f.select('two');
  wait.resolve(response({}));await f.h.flush();assert.equal(f.input().props.value,'two saved note');assert.equal(f.reads.filter(x=>x==='one').length,1);
  assert.ok(!text(f.h.render()).includes('Attendance saved.'));f.h.unmount();
});
test('A-to-B-to-A keeps a new draft when an older A save completes',async()=>{
  const wait=deferred(),f=await fixture({write:()=>wait.promise});f.save().props.onClick();await f.select('two');await f.select('one');
  f.input().props.onChange({target:{value:'Newer A draft'}});wait.resolve(response({}));await f.h.flush();assert.equal(f.input().props.value,'Newer A draft');f.h.unmount();
});
test('Unmount during attendance save does not update the retired page',async()=>{
  const wait=deferred(),f=await fixture({write:()=>wait.promise});f.save().props.onClick();f.h.unmount();wait.resolve(response({}));await f.h.flush();assert.equal(f.h.lateWrites,0);
});
for (const [name,data] of [['null row',[null]],['missing status',[{studentId:'s',notes:'Incomplete'}]],['invalid notes',[{studentId:'s',status:'Present',notes:{}}]],['duplicate learner',[{studentId:'s',status:'Present'},{studentId:'s',status:'Late'}]]]) test(name+' cannot unlock attendance entry',async()=>{
 const f=await fixture({read:()=>response(data)});assert.equal(f.input(),undefined);assert.equal(f.save(),undefined);assert.match(text(f.h.render()),/could not|unavailable/i);f.h.unmount();
});
