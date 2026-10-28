// Controlled actual TSX; no browser, SQL, billing or customer actions.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');
global.window = { location: { search: '' } };
const field = (h, placeholder) => nodes(h.render()).find(n => n.type === 'input' && n.props.placeholder === placeholder);
const button = h => nodes(h.render()).find(n => n.type === 'button' && /Save profile|Saving|^Add$/.test(text(n)));
const editor = h => nodes(h.render()).find(n => n.type === 'student-profile-editor');
const numberField = h => field(h, 'Student number, e.g. HMA-2026-001');

async function studentPage(read) {
  const h = mount('apps/web/src/app/student-profile/page.tsx', async url => {
    if (url === '/api/academies') return response([{ id: 'a' }]);
    if (url.endsWith('/students')) return response(['one', 'two'].map(id => ({ id, firstName: id, lastName: 'Synthetic', isActive: true })));
    const id = url.match(/\/students\/([^/]+)\/profile/)?.[1]; assert.ok(id, url);
    return read ? read(id) : response({ studentNumber: id.toUpperCase(), adminNotes: `Notes for ${id}` });
  });
  await h.flush();
  return { h, async select(id) { nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === 'student-record').props.onChange(id); await h.flush(); } };
}

test('student profile hides the old administrative editor during pending and failed loads', async () => {
  const wait = deferred(); const f = await studentPage(id => id === 'two' ? wait.promise : response({ studentNumber: 'ONE' }));
  assert.equal(editor(f.h).props.profile.studentNumber, 'ONE');
  await f.select('two'); assert.equal(editor(f.h), undefined);
  wait.resolve(response({}, false)); await f.h.flush(); assert.equal(editor(f.h), undefined);
  assert.match(text(f.h.render()), /could not be loaded/); f.h.unmount();
});
test('student profile ignores a reversed read and an old child save callback', async () => {
  const old = deferred(); const f = await studentPage(id => id === 'two' ? old.promise : response({ studentNumber: 'ONE' }));
  const previous = editor(f.h).props.onSaved;
  await f.select('two'); await f.select('one');
  const newest = editor(f.h).props.onSaved;
  newest({ studentNumber: 'CURRENT' }); f.h.render();
  old.resolve(response({ studentNumber: 'TWO' })); await f.h.flush();
  previous({ studentNumber: 'OLD SAVE' }); await f.h.flush();
  assert.equal(editor(f.h).props.profile.studentNumber, 'CURRENT'); f.h.unmount();
});
test('student profile clearing selection removes the old editor', async () => {
  const f = await studentPage(); await f.select(''); assert.equal(editor(f.h), undefined); f.h.unmount();
});
test('student profile offers recovery after a rejected detail read', async () => {
  let fail = true; const f = await studentPage(id => response({ studentNumber: id.toUpperCase() }, !fail));
  assert.equal(editor(f.h), undefined); fail = false;
  const retry = nodes(f.h.render()).find(n => n.type === 'button' && /retry|reload/i.test(text(n)));
  assert.ok(retry); retry.props.onClick(); await f.h.flush();
  assert.equal(editor(f.h).props.profile.studentNumber, 'ONE'); f.h.unmount();
});

async function adminProfile(write, props = {}) {
  const writes = [], saved = [];
  const h = mount('apps/web/src/components/student-admin-profile.tsx', async (url, init) => {
    writes.push({ url, ...init }); return write ? write(url, init) : response(JSON.parse(init.body));
  }, { academyId: 'a', studentId: 'one', profile: { studentNumber: 'ONE', adminNotes: 'First notes' }, onSaved: value => saved.push(value), ...props }, 'StudentAdminProfile');
  await h.flush();
  const save = async () => { button(h).props.onClick(); await h.flush(); };
  return { h, writes, saved, save };
}

test('student administrative editor cannot save an unloaded profile', async () => {
  const f = await adminProfile(undefined, { profile: undefined });
  assert.equal(button(f.h).props.disabled, true); await f.save(); assert.equal(f.writes.length, 0); f.h.unmount();
});
test('student administrative editor blocks duplicate pending saves', async () => {
  const wait = deferred(); const f = await adminProfile(() => wait.promise);
  await f.save(); await f.save(); assert.equal(f.writes.length, 1);
  wait.resolve(response({ studentNumber: 'ONE' })); await f.h.flush(); f.h.unmount();
});
for (const changed of ['student', 'academy', 'profile']) for (const accepted of [true, false])
  test(`student administrative ${accepted ? 'saved' : 'rejected'} response ignores changed ${changed} source`, async () => {
    const wait = deferred(); const f = await adminProfile(() => wait.promise);
    await f.save();
    if (changed === 'student') f.h.props.studentId = 'two';
    if (changed === 'academy') f.h.props.academyId = 'b';
    f.h.props.profile = { studentNumber: 'TWO', adminNotes: 'Current notes' };
    f.h.render(); await f.h.flush();
    wait.resolve(response({ studentNumber: 'OLD', message: 'Old rejection' }, accepted)); await f.h.flush();
    assert.equal(numberField(f.h).props.value, 'TWO'); assert.equal(f.saved.length, 0);
    assert.doesNotMatch(text(f.h.render()), /Old rejection|profile saved/); assert.equal(button(f.h).props.disabled, false); f.h.unmount();
  });
test('student administrative confirmed save with malformed readback retains draft and success', async () => {
  const f = await adminProfile(() => ({ ok: true, json: async () => { throw Error('Private JSON detail'); } }));
  numberField(f.h).props.onChange({ target: { value: 'DRAFT' } }); await f.save();
  assert.equal(numberField(f.h).props.value, 'DRAFT'); assert.equal(f.saved.length, 0);
  assert.match(text(f.h.render()), /saved.*Reload/); assert.doesNotMatch(text(f.h.render()), /Private JSON detail/); f.h.unmount();
});
test('student administrative save preserves nullable dates and releases busy after transport failure', async () => {
  const f = await adminProfile(async () => { throw Error('Private network detail'); });
  await f.save(); assert.equal(button(f.h).props.disabled, false);
  assert.equal(JSON.parse(f.writes[0].body).dateOfBirth, null); assert.equal(JSON.parse(f.writes[0].body).admissionDate, null);
  assert.doesNotMatch(text(f.h.render()), /Private network detail/); f.h.unmount();
});

const feeItem = id => ({ id, subjectName: `${id} subject`, amount: 12, frequency: 'Monthly', effectiveFrom: '2026-01-01', isActive: true });
async function subjectFees(options = {}) {
  const writes = [];
  const h = mount('apps/web/src/components/student-fee-arrangements.tsx', async (url, init = {}) => {
    const id = url.match(/\/students\/([^/]+)\//)?.[1]; assert.ok(id);
    if (init.method) { writes.push({ url, ...init }); return options.write ? options.write(id, init) : response(feeItem(id)); }
    return options.read ? options.read(id, writes.length) : response([feeItem(id)]);
  }, { academyId: 'a', studentId: 'one' }, 'StudentFeeArrangements');
  await h.flush();
  const add = async () => { void nodes(h.render()).find(n => n.type === 'form').props.onSubmit({ preventDefault() {} }); await h.flush(); };
  const select = async id => { h.props.studentId = id; h.render(); await h.flush(); };
  return { h, writes, add, select };
}
const feeDraft = (h, subject = 'Unsaved subject') => {
  field(h, 'Subject, e.g. Piano').props.onChange({ target: { value: subject } });
  field(h, 'Amount').props.onChange({ target: { value: '24' } }); h.render();
};

test('subject fees discard the prior student draft and list before the next load', async () => {
  const wait = deferred(); const f = await subjectFees({ read: id => id === 'two' ? wait.promise : response([feeItem(id)]) });
  feeDraft(f.h); await f.select('two');
  assert.equal(field(f.h, 'Subject, e.g. Piano').props.value, '');
  assert.doesNotMatch(text(f.h.render()), /one subject/); assert.equal(button(f.h).props.disabled, true);
  await f.add(); assert.equal(f.writes.length, 0);
  wait.resolve(response([feeItem('two')])); await f.h.flush(); f.h.unmount();
});
test('subject fees failed initial read cannot add and has retry', async () => {
  let fail = true; const f = await subjectFees({ read: () => response([], !fail) });
  assert.equal(button(f.h).props.disabled, true); await f.add(); assert.equal(f.writes.length, 0);
  const retry = nodes(f.h.render()).find(n => n.type === 'button' && /retry|reload/i.test(text(n)));
  assert.ok(retry); fail = false; retry.props.onClick(); await f.h.flush(); assert.equal(button(f.h).props.disabled, false); f.h.unmount();
});
test('subject fee old completion cannot clear another student draft or list', async () => {
  const wait = deferred(); const f = await subjectFees({ write: () => wait.promise });
  feeDraft(f.h, 'First draft'); await f.add(); await f.select('two'); feeDraft(f.h, 'Second draft');
  wait.resolve(response(feeItem('one'))); await f.h.flush();
  assert.equal(field(f.h, 'Subject, e.g. Piano').props.value, 'Second draft');
  assert.match(text(f.h.render()), /two subject/); assert.doesNotMatch(text(f.h.render()), /one subject|arrangement added/); f.h.unmount();
});
test('subject fees block duplicate POST and keep success when readback fails', async () => {
  const wait = deferred(); const f = await subjectFees({ write: () => wait.promise, read: (id, written) => written ? response([], false) : response([feeItem(id)]) });
  feeDraft(f.h); await f.add(); await f.add(); assert.equal(f.writes.length, 1);
  wait.resolve(response(feeItem('one'))); await f.h.flush();
  assert.match(text(f.h.render()), /arrangement added.*refresh/i); assert.equal(button(f.h).props.disabled, false); f.h.unmount();
});
test('subject fee transport failure keeps input and recovers', async () => {
  const f = await subjectFees({ write: async () => { throw Error('Private transport detail'); } });
  feeDraft(f.h); await f.add(); assert.equal(field(f.h, 'Subject, e.g. Piano').props.value, 'Unsaved subject');
  assert.equal(button(f.h).props.disabled, false); assert.match(text(f.h.render()), /could not be saved/);
  assert.doesNotMatch(text(f.h.render()), /Private transport detail/); f.h.unmount();
});

test('student parent accepts its current child save and the child keeps success after new props', async () => {
  const f = await studentPage();
  const props = editor(f.h).props;
  const child = await adminProfile(undefined, { ...props });
  numberField(child.h).props.onChange({ target: { value: 'UPDATED' } }); await child.save(); await f.h.flush();
  assert.equal(editor(f.h).props.profile.studentNumber, 'UPDATED');
  child.h.props.profile = editor(f.h).props.profile; child.h.render();
  assert.match(text(child.h.render()), /Administrative profile saved/);
  assert.equal(numberField(child.h).props.value, 'UPDATED'); child.h.unmount(); f.h.unmount();
});

for (const stage of ['academy', 'profile']) test(`student page ignores a late ${stage} read after unmount`, async () => {
  const pending = deferred();
  const h = mount('apps/web/src/app/student-profile/page.tsx', async url => {
    if (stage === 'academy') return pending.promise;
    if (url === '/api/academies') return response([{ id: 'a' }]);
    if (url.endsWith('/students')) return response([{ id: 'one' }]);
    return pending.promise;
  });
  await h.flush(); h.unmount(); pending.resolve(response(stage === 'academy' ? [{ id: 'a' }] : {}));
  await h.flush(); assert.equal(h.lateWrites, 0);
});

for (const accepted of [true, false]) test(`student administrative editor ignores ${accepted ? 'accepted' : 'rejected'} save after unmount`, async () => {
  const pending = deferred(); const f = await adminProfile(() => pending.promise);
  await f.save(); f.h.unmount(); pending.resolve(response({ studentNumber: 'ONE' }, accepted));
  await f.h.flush(); assert.equal(f.h.lateWrites, 0); assert.equal(f.saved.length, 0);
});

test('student administrative rejected response preserves the draft and validation message', async () => {
  const f = await adminProfile(() => response({ message: 'Synthetic duplicate learner number' }, false));
  numberField(f.h).props.onChange({ target: { value: 'DRAFT' } }); await f.save();
  assert.equal(numberField(f.h).props.value, 'DRAFT'); assert.match(text(f.h.render()), /Synthetic duplicate learner number/);
  assert.equal(f.saved.length, 0); assert.equal(button(f.h).props.disabled, false); f.h.unmount();
});

for (const stage of ['read', 'write']) test(`subject fees ignore late ${stage} after unmount`, async () => {
  const pending = deferred(); const f = await subjectFees(stage === 'read' ? { read: () => pending.promise } : { write: () => pending.promise });
  if (stage === 'write') { feeDraft(f.h); await f.add(); }
  f.h.unmount(); pending.resolve(response(stage === 'read' ? [feeItem('one')] : feeItem('one')));
  await f.h.flush(); assert.equal(f.h.lateWrites, 0);
});

test('subject fee late post-save refresh cannot replace the next student list', async () => {
  const old = deferred(); const f = await subjectFees({ read: (id, writes) => id === 'one' && writes ? old.promise : response([feeItem(id)]) });
  feeDraft(f.h); await f.add(); await f.select('two');
  old.resolve(response([feeItem('one')])); await f.h.flush();
  assert.match(text(f.h.render()), /two subject/); assert.doesNotMatch(text(f.h.render()), /one subject/); f.h.unmount();
});

test('subject fee rejected old POST leaves the next student draft untouched', async () => {
  const old = deferred(); const f = await subjectFees({ write: () => old.promise });
  feeDraft(f.h); await f.add(); await f.select('two'); feeDraft(f.h, 'Current subject');
  old.resolve(response({}, false)); await f.h.flush();
  assert.equal(field(f.h, 'Subject, e.g. Piano').props.value, 'Current subject');
  assert.doesNotMatch(text(f.h.render()), /positive fee/); f.h.unmount();
});

test('subject fee previous completion does not release another student pending POST', async () => {
  const old = deferred(), current = deferred(); let count = 0;
  const f = await subjectFees({ write: () => ++count === 1 ? old.promise : current.promise });
  feeDraft(f.h); await f.add(); await f.select('two'); feeDraft(f.h); await f.add();
  old.resolve(response(feeItem('one'))); await f.h.flush(); assert.equal(button(f.h).props.disabled, true);
  await f.add(); assert.equal(f.writes.length, 2);
  current.resolve(response(feeItem('two'))); await f.h.flush(); assert.equal(button(f.h).props.disabled, false); f.h.unmount();
});

test('subject fees reset draft and frequency when academy changes', async () => {
  const f = await subjectFees(); feeDraft(f.h);
  nodes(f.h.render()).find(n => n.type === 'select-control').props.onChange('Annual'); f.h.render();
  f.h.props.academyId = 'b'; f.h.render(); await f.h.flush();
  assert.equal(field(f.h, 'Subject, e.g. Piano').props.value, '');
  assert.equal(nodes(f.h.render()).find(n => n.type === 'select-control').props.value, 'Monthly'); f.h.unmount();
});

test('subject fees retain exact selected student, amount and billing payload', async () => {
  const f = await subjectFees(); await f.select('two'); feeDraft(f.h, 'Synthetic Piano');
  nodes(f.h.render()).find(n => n.type === 'select-control').props.onChange('Quarterly'); f.h.render();
  await f.add(); assert.equal(f.writes[0].url, '/api/academies/a/students/two/fee-arrangements');
  assert.deepEqual(JSON.parse(f.writes[0].body), { subjectName: 'Synthetic Piano', amount: 24, frequency: 'Quarterly', effectiveFrom: new Date().toISOString().slice(0, 10) }); f.h.unmount();
});

test('subject fees reject malformed list responses without enabling writes', async () => {
  const f = await subjectFees({ read: () => response({ message: 'not a list' }) });
  assert.equal(button(f.h).props.disabled, true); assert.match(text(f.h.render()), /could not be loaded/);
  await f.add(); assert.equal(f.writes.length, 0); f.h.unmount();
});

test('subject fees A/B/A load generation cannot erase the newest draft or list', async () => {
  const old = deferred(); let first = true;
  const f = await subjectFees({ read: id => id === 'one' && first ? (first = false, old.promise) : response([feeItem(id)]) });
  await f.select('two'); await f.select('one'); feeDraft(f.h, 'Newest draft');
  old.resolve(response([feeItem('stale')])); await f.h.flush();
  assert.equal(field(f.h, 'Subject, e.g. Piano').props.value, 'Newest draft');
  assert.match(text(f.h.render()), /one subject/); assert.doesNotMatch(text(f.h.render()), /stale subject/); f.h.unmount();
});
