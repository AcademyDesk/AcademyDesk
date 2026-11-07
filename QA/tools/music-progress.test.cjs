// Current TSX with synthetic delayed APIs. Not live Identity/SQL acceptance.
const { test } = require('node:test');
const a = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');

async function fixture(options = {}) {
  const writes = [];
  let stored = [
    { id: 'zero', studentId: 'student', musicPieceId: 'piece', status: 'Assigned', score: 0, targetDate: '2026-12-01', notes: 'Preserve this note' },
    { id: 'other', studentId: 'student', musicPieceId: 'piece', status: 'Learning', score: 37.5, targetDate: null, notes: null },
  ];
  let progressReads = 0;
  const h = mount('apps/web/src/app/music/page.tsx', async (url, init = {}) => {
    if (init.method) {
      const write = { url, body: JSON.parse(init.body), method: init.method };
      writes.push(write);
      if (options.write) {
        const result = await options.write(write, writes.length);
        if (result && !result.ok) return result;
      }
      stored = stored.map(row => url.endsWith('/' + row.id) ? { ...row, ...write.body } : row);
      return response(stored.find(row => url.endsWith('/' + row.id)));
    }
    if (url === '/api/academies') return response([{ id: 'academy' }]);
    if (url.endsWith('/students')) return response([{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }]);
    if (url.endsWith('/music-pieces')) return response([{ id: 'piece', title: 'Synthetic piece', difficulty: 'Beginner' }]);
    a.ok(url.endsWith('/music-progress'), url);
    progressReads++;
    if (options.read) return options.read(progressReads, structuredClone(stored));
    return response(structuredClone(stored));
  });
  const control = (id = 'zero') => nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === `progress-${id}`);
  const change = async (status, id = 'zero') => { control(id).props.onChange(status); await h.flush(); };
  const retry = async () => { const b = nodes(h.render()).find(n => n.type === 'button' && /Retry loading progress/.test(text(n))); a.ok(b, 'Explicit read retry'); b.props.onClick(); await h.flush(); };
  await h.flush();
  return { h, writes, control, change, retry, rows: () => structuredClone(stored) };
}

test('music rejected status leaves score/status intact and explicit retry succeeds', async () => {
  const f = await fixture({ write: (_, n) => n === 1 ? response({}, false) : null });
  try {
    await f.change('Mastered');
    a.equal(f.control().props.value, 'Assigned'); a.equal(f.rows()[0].score, 0);
    a.match(text(f.h.render()), /could not be updated/); a.equal(f.writes.length, 1);
    a.equal(f.control().props.disabled, true);
    await f.retry(); a.equal(f.writes.length, 1, 'Read retry must not replay a mutation');
    await f.change('Learning'); a.equal(f.control().props.value, 'Learning');
    a.deepEqual(f.writes[1].body, { status: 'Learning', targetDate: '2026-12-01', score: 0, notes: 'Preserve this note' });
    a.equal(f.rows()[1].score, 37.5);
  } finally { f.h.unmount(); }
});

test('music pending status blocks a duplicate callback and all row controls until refresh', async () => {
  const gate = deferred(); const f = await fixture({ write: () => gate.promise });
  try {
    const old = f.control(); old.props.onChange('Learning'); old.props.onChange('Mastered');
    await f.h.flush(); a.equal(f.writes.length, 1, 'No overlapping full replacement PATCH');
    a.equal(f.control().props.disabled, true); a.equal(f.control('other').props.disabled, true);
  } finally { gate.resolve(response({})); await f.h.flush(); f.h.unmount(); }
});

test('music save refresh keeps controls locked until fresh progress is available', async () => {
  const gate = deferred(); const f = await fixture({ read: (n, rows) => n === 2 ? gate.promise : response(rows) });
  try {
    await f.change('Learning'); a.equal(f.control().props.disabled, true);
    await f.change('Mastered'); a.equal(f.writes.length, 1);
  } finally { gate.resolve(response(f.rows())); await f.h.flush(); f.h.unmount(); }
});

test('music stale callback uses freshly loaded score after a completed status save', async () => {
  const f = await fixture({ read: (n, rows) => response(rows.map(row => n > 1 && row.id === 'zero' ? { ...row, score: 42 } : row)) });
  try {
    const stale = f.control(); await f.change('Learning');
    stale.props.onChange('Mastered'); await f.h.flush();
    a.equal(f.writes[1].body.score, 42, 'A callback must not reapply the previous score');
  } finally { f.h.unmount(); }
});

test('music placeholder status never sends a mutation', async () => {
  const f = await fixture();
  try { await f.change(''); a.equal(f.writes.length, 0); a.equal(f.rows()[0].status, 'Assigned'); }
  finally { f.h.unmount(); }
});

test('music pending save cannot update state or fetch after unmount', async () => {
  const gate = deferred(); let reads = 0;
  const f = await fixture({ write: () => gate.promise, read: (_, rows) => { reads++; return response(rows); } });
  await f.change('Learning'); f.h.unmount(); gate.resolve(response({})); await f.h.flush();
  a.equal(reads, 1, 'No late refresh'); a.equal(f.h.lateWrites, 0);
});

test('music transport rejection locks stale state and read retry never replays PATCH', async () => {
  const f = await fixture({ write: (_, n) => { if (n === 1) throw Error('Synthetic transport loss'); } });
  try {
    await f.change('Learning'); a.equal(f.control().props.disabled, true);
    a.match(text(f.h.render()), /could not be confirmed/);
    await f.h.flush(); a.equal(f.writes.length, 1);
    await f.change('Mastered'); a.equal(f.writes.length, 1);
    await f.retry(); a.equal(f.writes.length, 1); a.equal(f.control().props.value, 'Assigned');
    await f.change('Mastered'); a.equal(f.rows()[0].status, 'Mastered'); a.equal(f.rows()[0].score, 0);
  } finally { f.h.unmount(); }
});

test('music committed save with failed read requires fresh GET before another mutation', async () => {
  let failing = true;
  const f = await fixture({ read: (n, rows) => response(rows, n === 1 || !failing) });
  try {
    await f.change('Learning'); a.equal(f.rows()[0].status, 'Learning');
    a.equal(f.control().props.disabled, true); a.match(text(f.h.render()), /saved.*reload/);
    await f.change('Mastered'); a.equal(f.writes.length, 1);
    failing = false; await f.retry(); a.equal(f.writes.length, 1);
    a.equal(f.control().props.value, 'Learning'); a.equal(f.control().props.disabled, false);
  } finally { f.h.unmount(); }
});

test('music failed read retry stays locked and can recover without automatic writes', async () => {
  let failing = true;
  const f = await fixture({ write: (_, n) => n === 1 ? response({}, false) : null,
    read: (n, rows) => response(rows, n === 1 || !failing) });
  try {
    await f.change('Learning'); await f.retry();
    a.match(text(f.h.render()), /could not be loaded/); a.equal(f.control().props.disabled, true);
    a.equal(f.writes.length, 1); failing = false; await f.retry();
    a.equal(f.control().props.disabled, false); a.equal(f.writes.length, 1);
  } finally { f.h.unmount(); }
});

test('music initial progress failure can recover through explicit read retry', async () => {
  let failing = true;
  const f = await fixture({ read: (_, rows) => response(rows, !failing) });
  try {
    a.equal(f.control(), undefined); failing = false; await f.retry();
    a.equal(f.control().props.value, 'Assigned'); a.equal(f.writes.length, 0);
  } finally { f.h.unmount(); }
});

test('music initial held read cannot commit state after unmount', async () => {
  const gate = deferred(); const f = await fixture({ read: () => gate.promise });
  f.h.unmount(); gate.resolve(response(f.rows())); await f.h.flush(); a.equal(f.h.lateWrites, 0);
});

test('music callback for a removed progress row cannot reintroduce it with a stale PATCH', async () => {
  const f = await fixture({ read: (n, rows) => response(n > 1 ? rows.filter(row => row.id !== 'zero') : rows) });
  try {
    const old = f.control(); await f.change('Learning'); a.equal(f.control(), undefined);
    old.props.onChange('Mastered'); await f.h.flush(); a.equal(f.writes.length, 1);
  } finally { f.h.unmount(); }
});
