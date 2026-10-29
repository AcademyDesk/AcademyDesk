// Actual assessment page with controlled transport. Browser/SQL acceptance separate.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');
async function fixture(options = {}) {
  const reads = [], writes = [];
  const results = id => [{ studentId: 's', score: id === 'one' ? 80 : 20, grade: id === 'one' ? 'Merit' : 'Fail', isGradeManual: true, remarks: id + ' remarks', isPublished: true }];
  const h = mount('apps/web/src/app/assessments/page.tsx', async (url, init = {}) => {
    if (url === '/api/academies') return response([{ id: 'a' }]);
    if (url.endsWith('/options')) return response({ batches: [{ id: 'b', name: 'Synthetic batch' }], students: [{ id: 's', firstName: 'Shared', lastName: 'Learner' }], enrollments: [{ studentId: 's', batchId: 'b', status: 'Active' }], gradingSchemes: [] });
    if (url.endsWith('/assessments')) return response(['one', 'two'].map(id => ({ id, batchId: 'b', title: id, maxScore: 100 })));
    const id = url.match(/assessments\/(one|two)\/results/)?.[1]; assert.ok(id, url);
    if (init.method) { writes.push({ url, ...init }); return options.write ? options.write(id, init) : response({}); }
    reads.push(id); return options.read ? options.read(id, results(id), reads.filter(x => x === id).length) : response(results(id));
  });
  await h.flush();
  const row = () => nodes(h.render()).find(n => typeof n.type === 'function' && n.type.name === 'ResultRow');
  const select = async id => { nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === 'assessment').props.onChange(id); await h.flush(); };
  const save = () => row()?.props.onSave('s', row().props.result?.score ?? 0, row().props.result?.grade ?? '', 'Kept notes', true);
  return { h, reads, writes, results, row, select, save };
}
test('Pending assessment selection never exposes or saves the previous results', async () => {
  const wait = deferred(), f = await fixture({ read: (id, data) => id === 'two' ? wait.promise : response(data) });
  assert.equal(f.row().props.result.score, 80); await f.select('two');
  assert.equal(f.row(), undefined); await f.save(); assert.equal(f.writes.length, 0);
  wait.resolve(response(f.results('two'))); await f.h.flush(); assert.equal(f.row().props.result.score, 20);
  await f.save(); assert.ok(f.writes[0].url.includes('/two/')); assert.equal(JSON.parse(f.writes[0].body).score, 20); f.h.unmount();
});
test('Failed selection is locked and explicit retry recovers that assessment', async () => {
  let fail = true; const f = await fixture({ read: (id, data) => response(data, id !== 'two' || !fail) });
  await f.select('two'); assert.equal(f.row(), undefined); await f.save(); assert.equal(f.writes.length, 0);
  fail = false; const retry = nodes(f.h.render()).find(n => n.type === 'button' && /retry|reload|try again/i.test(text(n))); assert.ok(retry);
  retry.props.onClick(); await f.h.flush(); assert.equal(f.row().props.result.score, 20); f.h.unmount();
});
test('Malformed result lists cannot unlock blank result entry', async () => {
  const f = await fixture({ read: () => response({}) }); assert.equal(f.row(), undefined);
  assert.match(text(f.h.render()), /could not|unavailable/i); f.h.unmount();
});
test('A retired row callback cannot write after selecting another assessment', async () => {
  const f = await fixture(), save = f.row().props.onSave; await f.select('two');
  await save('s', 80, 'Merit', 'Old draft', true); assert.equal(f.writes.length, 0); f.h.unmount();
});
test('Late save completion cannot load results or success notices into the new selection', async () => {
  const wait = deferred(), f = await fixture({ write: () => wait.promise }); const saving = f.save();
  await f.select('two'); assert.equal(f.row().props.result.score, 20); wait.resolve(response({})); await saving; await f.h.flush();
  assert.equal(f.row().props.result.score, 20); assert.equal(f.reads.filter(x => x === 'one').length, 1);
  assert.ok(!text(f.h.render()).includes('Assessment result saved.')); f.h.unmount();
});
test('Late save readback cannot replace the selected assessment results', async () => {
  const wait = deferred(), f = await fixture({ read: (id, data, count) => id === 'one' && count > 1 ? wait.promise : response(data) });
  const saving = f.save(); await f.h.flush(); await f.select('two'); wait.resolve(response(f.results('one'))); await saving; await f.h.flush();
  assert.equal(f.row().props.result.score, 20); f.h.unmount();
});
test('Same-tick repeated result saves issue one request', async () => {
  const wait = deferred(), f = await fixture({ write: () => wait.promise }), save = f.row().props.onSave;
  const one = save('s', 80, 'Merit', 'Draft', true), two = save('s', 80, 'Merit', 'Draft', true);
  assert.equal(f.writes.length, 1); wait.resolve(response({})); await Promise.all([one, two]); f.h.unmount();
});
test('Unmount during a save never updates the retired page', async () => {
  const wait = deferred(), f = await fixture({ write: () => wait.promise }), saving = f.save();
  f.h.unmount(); wait.resolve(response({})); await saving; await f.h.flush(); assert.equal(f.h.lateWrites, 0);
});
test('Reversed result reads keep the current selection', async () => {
  const old = deferred(), fresh = deferred(); let repeat = false;
  const f = await fixture({ read: (id, data) => id === 'two' ? old.promise : repeat ? fresh.promise : response(data) });
  await f.select('two'); repeat = true; await f.select('one'); fresh.resolve(response(f.results('one'))); await f.h.flush();
  old.resolve(response(f.results('two'))); await f.h.flush(); assert.equal(f.row().props.result.score, 80); f.h.unmount();
});

for (const [label, data] of [['null row',[null]],['missing score',[{studentId:'s',isPublished:true}]],['duplicate learner',[{studentId:'s',score:80,isPublished:true},{studentId:'s',score:20,isPublished:true}]]]) test(label+' keeps malformed records locked',async()=>{
 const f=await fixture({read:()=>response(data)});assert.equal(f.row(),undefined);assert.match(text(f.h.render()),/could not|unavailable/i);f.h.unmount();
});
test('A same-tick selection retires the previous save callback immediately',async()=>{
 const f=await fixture(),save=f.row().props.onSave;
 nodes(f.h.render()).find(n=>n.type==='select-control'&&n.props.name==='assessment').props.onChange('two');
 await save('s',80,'Merit','Retired draft',true);assert.equal(f.writes.length,0);await f.h.flush();f.h.unmount();
});
test('A-to-B-to-A does not accept completion from the earlier A save',async()=>{
 const wait=deferred(),f=await fixture({write:()=>wait.promise}),saving=f.save();
 await f.select('two');await f.select('one');const reads=f.reads.length;wait.resolve(response({}));await saving;await f.h.flush();
 assert.equal(f.reads.length,reads);assert.equal(f.row().props.result.score,80);assert.ok(!text(f.h.render()).includes('Assessment result saved.'));f.h.unmount();
});
