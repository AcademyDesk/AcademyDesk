// Actual TSX with controlled responses; no browser/SQL or customer records.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');
global.window = { location: { search: '' } };

const scenarios = [
  { page: 'guardian-profile', resource: 'guardians', field: 'preferredName', placeholder: 'Preferred name' },
  { page: 'teacher-profile', resource: 'teachers', field: 'employeeCode', placeholder: 'Employee code', selector: 'teacher-record' },
  { page: 'student-fees', resource: 'students', field: 'amount', selector: 'fee-student', admission: true },
];

async function fixture(s, options = {}) {
  const writes = [], reads = [];
  const data = id => s.admission ? { amount: id === 'one' ? 11 : 22, dueDate: id === 'one' ? '2026-11-01' : '2026-12-02' }
    : { [s.field]: id === 'one' ? 'FIRST' : 'SECOND', availability: [{ day: id === 'one' ? 'Mon' : 'Tue', from: '10:00', to: '12:00' }] };
  const h = mount(`apps/web/src/app/${s.page}/page.tsx`, async (url, init = {}) => {
    if (init.method) { writes.push({ url, ...init }); return options.write ? options.write(url, init) : response(JSON.parse(init.body)); }
    if (url === '/api/academies') return response([{ id: 'academy' }]);
    if (url.endsWith(`/${s.resource}`)) return response(['one', 'two'].map(id => ({ id, firstName: id, lastName: 'Synthetic', isActive: true })));
    const id = url.match(new RegExp(`/${s.resource}/([^/]+)/`))?.[1];
    assert.ok(id, url); reads.push(id);
    return options.read ? options.read(id, data(id)) : response(data(id));
  });
  const field = () => nodes(h.render()).find(n => n.type === 'input' && (s.admission ? n.props.type === 'number' : n.props.placeholder === s.placeholder));
  const save = () => nodes(h.render()).find(n => n.type === 'button' && /Save (profile|admission fee)|Saving/.test(text(n)));
  const select = async id => {
    const control = nodes(h.render()).find(n => s.selector ? n.type === 'select-control' && n.props.name === s.selector : n.type === 'select');
    assert.ok(control); control.props.onChange(s.selector ? id : { target: { value: id } }); await h.flush();
  };
  const submit = async () => {
    if (s.admission) {
      const form = nodes(h.render()).find(n => n.type === 'form');
      if (form) void form.props.onSubmit({ preventDefault() {} });
    } else save()?.props.onClick();
    await h.flush();
  };
  await h.flush();
  return { h, writes, reads, field, save, select, submit, data };
}

for (const s of scenarios) {
  test(`${s.page}: pending selection cannot display or save the previous record`, async () => {
    const wait = deferred();
    const f = await fixture(s, { read: (id, value) => id === 'two' ? wait.promise : response(value) });
    assert.equal(f.field().props.value, s.admission ? '11' : 'FIRST');
    await f.select('two');
    assert.ok(!f.field() || f.field().props.value !== (s.admission ? '11' : 'FIRST'));
    assert.ok(!f.save() || f.save().props.disabled);
    await f.submit(); assert.equal(f.writes.length, 0);
    wait.resolve(response(f.data('two'))); await f.h.flush();
    assert.equal(f.field().props.value, s.admission ? '22' : 'SECOND');
    await f.submit(); assert.equal(f.writes.length, 1);
    assert.ok(f.writes[0].url.includes(`/${s.resource}/two/`));
    assert.equal(JSON.parse(f.writes[0].body)[s.field], s.admission ? 22 : 'SECOND');
    f.h.unmount();
  });

  test(`${s.page}: failed selection is locked and retry recovers that record`, async () => {
    let failing = true;
    const f = await fixture(s, { read: (id, value) => response(value, id !== 'two' || !failing) });
    await f.select('two'); assert.ok(!f.save() || f.save().props.disabled);
    await f.submit(); assert.equal(f.writes.length, 0);
    assert.match(text(f.h.render()), /could not be loaded/);
    failing = false;
    const retry = nodes(f.h.render()).find(n => n.type === 'button' && /retry|reload|try again/i.test(text(n)));
    assert.ok(retry, 'A failed selection has an explicit recovery action');
    retry.props.onClick(); await f.h.flush();
    assert.equal(f.field().props.value, s.admission ? '22' : 'SECOND'); f.h.unmount();
  });

  test(`${s.page}: reversed loads cannot replace the newest selection`, async () => {
    const old = deferred(); let first = true;
    const f = await fixture(s, { read: (id, value) => id === 'one' && first ? (first = false, old.promise) : response(value) });
    await f.select('two');
    old.resolve(response(f.data('one'))); await f.h.flush();
    assert.equal(f.field().props.value, s.admission ? '22' : 'SECOND');
    await f.submit(); assert.ok(f.writes[0].url.includes(`/${s.resource}/two/`)); f.h.unmount();
  });

  for (const accepted of [true, false]) test(`${s.page}: old ${accepted ? 'saved' : 'rejected'} response cannot overwrite the newly loaded editor`, async () => {
    const pending = deferred();
    const f = await fixture(s, { write: () => pending.promise });
    await f.submit(); assert.equal(f.writes.length, 1);
    await f.select('two');
    pending.resolve(response({ ...f.data('one'), message: 'Old rejection' }, accepted)); await f.h.flush();
    assert.equal(f.field().props.value, s.admission ? '22' : 'SECOND');
    assert.doesNotMatch(text(f.h.render()), /Old rejection|profile saved|Admission fee saved/); f.h.unmount();
  });

  test(`${s.page}: clearing selection discards the old editor and does not fetch an empty ID`, async () => {
    const f = await fixture(s); await f.select('');
    assert.ok(!f.field()); await f.submit(); assert.equal(f.writes.length, 0); assert.deepEqual(f.reads, ['one']); f.h.unmount();
  });

  test(`${s.page}: repeated save clicks produce only one pending write`, async () => {
    const pending = deferred(); const f = await fixture(s, { write: () => pending.promise });
    await f.submit(); await f.submit(); assert.equal(f.writes.length, 1);
    pending.resolve(response(f.data('one'))); await f.h.flush(); f.h.unmount();
  });

  test(`${s.page}: transport failure preserves the matching draft and permits retry`, async () => {
    let fail = true;
    const f = await fixture(s, { write: async () => { if (fail) throw Error('Private network detail'); return response(f.data('one')); } });
    f.field().props.onChange({ target: { value: s.admission ? '33' : 'UNSAVED' } });
    await f.submit(); assert.equal(f.field().props.value, s.admission ? '33' : 'UNSAVED');
    assert.ok(!f.save().props.disabled); assert.doesNotMatch(text(f.h.render()), /Private network detail/);
    fail = false; await f.submit(); assert.equal(f.writes.length, 2);
    assert.equal(JSON.parse(f.writes[1].body)[s.field], s.admission ? 33 : 'UNSAVED'); f.h.unmount();
  });

  for (const pendingOperation of ['read', 'write']) test(`${s.page}: ignores ${pendingOperation} completion after unmount`, async () => {
    const pending = deferred();
    const f = await fixture(s, pendingOperation === 'read' ? { read: () => pending.promise } : { write: () => pending.promise });
    if (pendingOperation === 'write') await f.submit();
    f.h.unmount(); pending.resolve(response(f.data('one'))); await f.h.flush(); assert.equal(f.h.lateWrites, 0);
  });

  test(`${s.page}: returning to the same ID rejects its earlier load generation`, async () => {
    const old = deferred(); let first = true;
    const f = await fixture(s, { read: (id, value) => id === 'one' && first ? (first = false, old.promise) : response(value) });
    await f.select('two'); await f.select('one');
    f.field().props.onChange({ target: { value: s.admission ? '44' : 'CURRENT DRAFT' } });
    old.resolve(response(f.data('two'))); await f.h.flush();
    assert.equal(f.field().props.value, s.admission ? '44' : 'CURRENT DRAFT'); f.h.unmount();
  });

  test(`${s.page}: an old save completion cannot release the new record's pending save`, async () => {
    const old = deferred(), current = deferred(); let count = 0;
    const f = await fixture(s, { write: () => ++count === 1 ? old.promise : current.promise });
    await f.submit(); await f.select('two'); await f.submit();
    old.resolve(response(f.data('one'))); await f.h.flush();
    assert.equal(f.save().props.disabled, true); await f.submit(); assert.equal(f.writes.length, 2);
    current.resolve(response(f.data('two'))); await f.h.flush(); assert.equal(f.save().props.disabled, false); f.h.unmount();
  });

  test(`${s.page}: confirmed write with unreadable readback keeps success and the submitted draft`, async () => {
    const f = await fixture(s, { write: () => ({ ok: true, json: async () => { throw Error('Invalid JSON'); } }) });
    f.field().props.onChange({ target: { value: s.admission ? '55' : 'CONFIRMED' } });
    await f.submit();
    assert.equal(f.field().props.value, s.admission ? '55' : 'CONFIRMED');
    assert.match(text(f.h.render()), /saved.*Reload/); assert.doesNotMatch(text(f.h.render()), /could not be saved|Invalid JSON/); f.h.unmount();
  });

  for (const invalid of [null, []]) test(`${s.page}: invalid ${invalid === null ? 'null' : 'array'} load cannot enable saving`, async () => {
    const f = await fixture(s, { read: () => response(invalid) });
    assert.match(text(f.h.render()), /could not be loaded/);
    await f.submit(); assert.equal(f.writes.length, 0); f.h.unmount();
  });
}

test('admission fees preserve zero amounts and nullable dates in the write payload', async () => {
  const f = await fixture(scenarios[2]);
  f.field().props.onChange({ target: { value: '0' } });
  nodes(f.h.render()).find(n => n.type === 'date-control').props.onChange('');
  await f.submit(); assert.deepEqual(JSON.parse(f.writes[0].body), { amount: 0, dueDate: null }); f.h.unmount();
});

test('teacher profile keeps typed availability and nullable dates in its own request', async () => {
  const f = await fixture(scenarios[1]);
  await f.submit(); const value = JSON.parse(f.writes[0].body);
  assert.equal(value.dateOfBirth, null); assert.equal(value.joiningDate, null);
  assert.deepEqual(JSON.parse(value.availabilityJson), [{ day: 'Mon', from: '10:00', to: '12:00' }]); f.h.unmount();
});
