// Actual TSX request construction with synthetic responses; no financial writes.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response, deferred } = require('./frontend-page-runtime.cjs');

const rateLabels = {
  monthlySalary: 'Monthly salary', standardHourlyRate: 'Standard hourly rate',
  beginnerHourlyRate: 'Beginner hourly rate (optional)', intermediateHourlyRate: 'Intermediate hourly rate (optional)',
  advancedHourlyRate: 'Advanced hourly rate (optional)',
};
async function compensation(profile, options = {}) {
  const writes = []; let stored = { ...profile };
  const h = mount('apps/web/src/app/teacher-payments/page.tsx', async (url, init = {}) => {
    if (init.method) {
      writes.push({ url, ...init });
      if (options.write) return options.write(url, init);
      stored = JSON.parse(init.body); return response(stored);
    }
    if (url === '/api/academies') return response([{ id: 'a' }]);
    if (url.endsWith('/teachers')) return response(['one', 'two'].map(id => ({ id, firstName: id, lastName: 'Synthetic', isActive: true })));
    return options.read ? options.read(url, stored) : response(stored);
  });
  await h.flush();
  return { h, writes, async save() { void nodes(h.render()).find(n => n.type === 'form').props.onSubmit({ preventDefault() {} }); await h.flush(); },
    async select(id) { nodes(h.render()).find(n => n.type === 'select-control' && n.props.name === 'payment-teacher').props.onChange(id); await h.flush(); } };
}

for (const [rate, label] of Object.entries(rateLabels)) for (const value of [0, '0', 12.5, null, undefined])
  test(`teacher compensation ${rate}: ${String(value)} (${typeof value}) survives unchanged resaves`, async () => {
    const profile = { model: rate === 'monthlySalary' ? 'Monthly' : 'Hourly', monthlySalary: 100,
      standardHourlyRate: 200, beginnerHourlyRate: 100, intermediateHourlyRate: 120, advancedHourlyRate: 140,
      effectiveFrom: '2026-10-01', [rate]: value };
    const f = await compensation(profile); await f.save(); await f.save();
    assert.equal(f.writes.length, 2);
    for (const write of f.writes) {
      assert.equal(write.url, '/api/academies/a/teachers/one/compensation');
      assert.equal(JSON.parse(write.body)[rate], value == null ? null : Number(value), label);
    }
    f.h.unmount();
  });

test('teacher compensation cannot save a previous teacher while the next load is pending', async () => {
  const pending = deferred(); const f = await compensation({ model: 'Monthly', monthlySalary: 101 }, {
    read: (url, stored) => url.includes('/two/') ? pending.promise : response(stored),
  });
  await f.select('two');
  const save = nodes(f.h.render()).find(n => n.type === 'button' && /Save payment details/.test(text(n)));
  assert.ok(!save || save.props.disabled); await f.save(); assert.equal(f.writes.length, 0);
  pending.resolve(response({ model: 'Monthly', monthlySalary: 202 })); await f.h.flush(); await f.save();
  assert.ok(f.writes[0].url.includes('/two/')); assert.equal(JSON.parse(f.writes[0].body).monthlySalary, 202); f.h.unmount();
});

test('teacher compensation old save cannot replace the next teacher rates', async () => {
  const pending = deferred(); const f = await compensation({ model: 'Monthly', monthlySalary: 101 }, {
    write: () => pending.promise, read: (url, stored) => response(url.includes('/two/') ? { model: 'Monthly', monthlySalary: 202 } : stored),
  });
  await f.save(); await f.select('two'); pending.resolve(response({ model: 'Monthly', monthlySalary: 101 })); await f.h.flush();
  const amount = nodes(f.h.render()).find(n => n.type === 'input' && n.props.type === 'number');
  assert.equal(Number(amount.props.value), 202); assert.doesNotMatch(text(f.h.render()), /details saved/); f.h.unmount();
});

for (const score of [0, 37.5, null, undefined]) for (const status of ['Learning', 'Mastered'])
  test(`music status ${status} preserves score ${String(score)}`, async () => {
    const writes = []; let item = { id: 'progress', studentId: 'student', musicPieceId: 'piece', status: 'Assigned', score,
      targetDate: '2026-12-01', notes: 'Synthetic learning note' };
    const h = mount('apps/web/src/app/music/page.tsx', async (url, init = {}) => {
      if (init.method) { writes.push({ url, ...init }); item = { ...item, ...JSON.parse(init.body) }; return response(item); }
      if (url === '/api/academies') return response([{ id: 'a' }]);
      if (url.endsWith('/students')) return response([{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }]);
      if (url.endsWith('/music-pieces')) return response([{ id: 'piece', title: 'Synthetic piece', difficulty: 'Beginner' }]);
      return response([item]);
    });
    await h.flush();
    const control = nodes(h.render()).find(n => n.type === 'select-control' && n.props.value === 'Assigned');
    assert.ok(control); control.props.onChange(status); await h.flush();
    assert.equal(writes.length, 1); assert.equal(writes[0].method, 'PATCH');
    assert.equal(writes[0].url, '/api/academies/a/music-progress/progress');
    assert.deepEqual(JSON.parse(writes[0].body), { status, targetDate: '2026-12-01', score: score ?? null, notes: 'Synthetic learning note' }); h.unmount();
  });

for (const payslipTemplateKey of ['Standard', 'Compact', 'Professional']) for (const accepted of [true, false])
  test(`finance policy preserves ${payslipTemplateKey} payslip theme on ${accepted ? 'accepted' : 'rejected'} save`, async () => {
    const writes = [];
    const settings = { taxRegistrationNumber: 'Synthetic registration', taxLabel: 'Original tax', taxRatePercent: 5,
      defaultPaymentTermsDays: 14, taxInclusivePricing: true, invoiceTemplateKey: 'Modern', payslipTemplateKey,
      invoiceLogoUrl: '', invoiceAuthorityName: 'Synthetic officer', invoiceAuthorityTitle: 'Finance', invoiceSignatureUrl: '' };
    const h = mount('apps/web/src/app/finance-policy/page.tsx', async (url, init = {}) => {
      if (init.method) { writes.push({ url, ...init }); return response({ ...settings, ...JSON.parse(init.body) }, accepted); }
      return response(url === '/api/academies' ? [{ id: 'a' }] : settings);
    });
    await h.flush(); const form = nodes(h.render()).find(n => n.type === 'form'); assert.ok(form);
    const values = { ...settings, taxLabel: 'Edited tax', taxRatePercent: '7.5', defaultPaymentTermsDays: '30', taxInclusivePricing: 'on' };
    delete values.payslipTemplateKey; // That theme is deliberately absent from this editor.
    await form.props.onSubmit({ preventDefault() {}, currentTarget: { values } }); await h.flush();
    const payload = JSON.parse(writes[0].body);
    assert.equal(payload.payslipTemplateKey, payslipTemplateKey);
    assert.equal(payload.taxLabel, 'Edited tax'); assert.equal(payload.taxRatePercent, 7.5);
    assert.equal(payload.invoiceTemplateKey, 'Modern'); assert.equal(payload.defaultPaymentTermsDays, 30);
    assert.equal(payload.taxInclusivePricing, true);
    assert.match(text(h.render()), accepted ? /Finance policy saved/ : /could not be saved/); h.unmount();
  });

for (const [rate, label] of Object.entries(rateLabels)) test(`teacher compensation typed zero stays zero after ${rate} hydration`, async () => {
  const f = await compensation({ model: rate === 'monthlySalary' ? 'Monthly' : 'Hourly', monthlySalary: 100,
    standardHourlyRate: 200, beginnerHourlyRate: 100, intermediateHourlyRate: 120, advancedHourlyRate: 140 });
  const row = nodes(f.h.render()).find(n => n.type === 'label' && text(n) === label);
  assert.ok(row); nodes(row).find(n => n.type === 'input').props.onChange({ target: { value: '0' } });
  f.h.render(); await f.save(); await f.save();
  for (const write of f.writes) assert.equal(JSON.parse(write.body)[rate], 0); f.h.unmount();
});

for (const model of ['Monthly', 'Hourly']) test(`teacher compensation ${model} transition preserves only applicable rates`, async () => {
  const f = await compensation({ model: model === 'Monthly' ? 'Hourly' : 'Monthly', monthlySalary: 0,
    standardHourlyRate: 0, beginnerHourlyRate: null, intermediateHourlyRate: 0, advancedHourlyRate: 15 });
  nodes(f.h.render()).find(n => n.type === 'select-control' && n.props.name === 'payment-model').props.onChange(model);
  f.h.render(); await f.save(); const payload = JSON.parse(f.writes[0].body);
  assert.deepEqual(payload, { model, monthlySalary: model === 'Monthly' ? 0 : null,
    standardHourlyRate: model === 'Hourly' ? 0 : null, beginnerHourlyRate: null,
    intermediateHourlyRate: model === 'Hourly' ? 0 : null, advancedHourlyRate: model === 'Hourly' ? 15 : null, effectiveFrom: null }); f.h.unmount();
});

test('teacher compensation failed selection is locked until explicit retry succeeds', async () => {
  let failing = true; const f = await compensation({ model: 'Monthly', monthlySalary: 0 }, { read: (url, value) => response(value, !failing) });
  await f.save(); assert.equal(f.writes.length, 0); failing = false;
  const retry = nodes(f.h.render()).find(n => n.type === 'button' && /retry/i.test(text(n)));
  assert.ok(retry); retry.props.onClick(); await f.h.flush(); await f.save();
  assert.equal(JSON.parse(f.writes[0].body).monthlySalary, 0); f.h.unmount();
});

test('teacher compensation duplicate clicks and rejected-save recovery preserve the draft', async () => {
  const pending = deferred(); const f = await compensation({ model: 'Monthly', monthlySalary: 0 }, { write: () => pending.promise });
  await f.save(); await f.save(); assert.equal(f.writes.length, 1);
  pending.resolve(response({ message: 'Synthetic validation rejection' }, false)); await f.h.flush();
  assert.match(text(f.h.render()), /Synthetic validation rejection/);
  const save = nodes(f.h.render()).find(n => n.type === 'button' && /Save payment details/.test(text(n)));
  assert.equal(save.props.disabled, false); assert.equal(Number(nodes(f.h.render()).find(n => n.type === 'input').props.value), 0); f.h.unmount();
});

test('teacher compensation transport failure unlocks the same zero draft', async () => {
  const f = await compensation({ model: 'Monthly', monthlySalary: 0 }, { write: async () => { throw Error('Private transport detail'); } });
  await f.save(); assert.match(text(f.h.render()), /could not be saved/); assert.doesNotMatch(text(f.h.render()), /Private transport detail/);
  assert.equal(nodes(f.h.render()).find(n => n.type === 'button' && /Save payment details/.test(text(n))).props.disabled, false);
  await f.save(); assert.equal(f.writes.length, 2); assert.equal(JSON.parse(f.writes[1].body).monthlySalary, 0); f.h.unmount();
});
