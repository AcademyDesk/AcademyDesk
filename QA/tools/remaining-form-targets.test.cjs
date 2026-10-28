// Actual handlers with synthetic API replies and a released React event target.
// No real payroll, invoice, notification, SQL or customer action is performed.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response } = require('./frontend-page-runtime.cjs');

const cases = [
  { name: 'teacher assignment', file: 'teacher', exportName: 'TeacherTasks', props: { batches: [{ id: 'batch', name: 'Synthetic class' }] }, path: '/api/teacher/assignments', form: 0 },
  { name: 'teacher leave', file: 'teacher', exportName: 'TeacherSelfService', path: '/api/teacher/leave-requests', form: 1 },
  { name: 'learner practice', file: 'portal', exportName: 'Action', props: { title: 'Practice', url: '/api/portal/practice-logs', fields: null }, path: '/api/portal/practice-logs', form: 0 },
  { name: 'grading scheme', file: 'academic-governance', path: '/api/academies/a/academic-governance/grading-schemes', form: 0 },
  { name: 'platform support', file: 'platform/control', tab: 'Support', marker: 'platform-support-editor', path: '/api/platform/support-cases' },
  { name: 'platform invoice', file: 'platform/control', tab: 'Billing', marker: 'platform-billing-editor', path: '/api/platform/billing-invoices' },
  { name: 'platform announcement', file: 'platform/control', tab: 'Announcements', marker: 'platform-announcement-editor', path: '/api/platform/announcements' },
  { name: 'payroll profile', file: 'payroll', path: '/api/academies/a/payroll/profiles', form: 0 },
  { name: 'payroll payout', file: 'payroll', path: '/api/academies/a/payroll/payouts', form: 1 },
];

async function exercise(scenario, outcome) {
  const accepted = outcome !== 'rejected', writes = [];
  const academies = [{ id: 'a', name: 'Synthetic Academy', subscriptionPlan: 'Trial', subscriptionStatus: 'Trial', enabledModulesJson: '["Core"]', studentLimit: 10, staffLimit: 5 }];
  const profiles = [{ id: 'profile', workerType: 'Teacher', workerName: 'Synthetic Teacher', paymentModel: 'Monthly', monthlyAmount: 1000, effectiveFrom: '2026-01-01', isActive: true }];
  const h = mount(`apps/web/src/app/${scenario.file}/page.tsx`, async (url, options = {}) => {
    if (options.method) {
      writes.push({ url, ...options });
      return response({ id: 'created', message: 'Synthetic rejection', startDate: '2026-10-28', endDate: '2026-10-29', reason: 'Synthetic leave', status: 'Pending' }, accepted);
    }
    if (writes.length && outcome === 'refresh-failed') throw Error('Synthetic readback failure');
    if (url === '/api/academies' || url === '/api/platform/academies') return response(academies);
    if (url.endsWith('/onboarding')) return response({});
    if (url === '/api/teacher/profile') return response({});
    if (url.endsWith('/teachers')) return response([{ id: 'teacher', firstName: 'Synthetic', lastName: 'Teacher' }]);
    if (url.endsWith('/payroll/profiles')) return response(profiles);
    if (url.startsWith('/api/platform/') || url === '/api/auth/session') return response([], false);
    return response([]);
  }, scenario.props ?? {}, scenario.exportName ?? 'default');
  await h.flush();
  if (scenario.tab) { h.query.set('tab', scenario.tab); h.render(); await h.flush(); }
  function select(name, value, type = 'select-control') {
    const field = nodes(h.render()).find(n => n.type === type && n.props.name === name);
    assert.ok(field, name); field.props.onChange(value);
  }
  if (scenario.name === 'payroll profile') { select('workerId', 'teacher'); select('effectiveFrom', '2026-01-01', 'date-control'); }
  if (scenario.name === 'payroll payout') select('payrollProfileId', 'profile');
  const values = { batchId: 'batch', title: 'Synthetic assignment', reason: 'Synthetic leave', startDate: '2026-10-28', endDate: '2026-10-29',
    minutesPracticed: '30', name: 'Synthetic scheme', passingPercent: '50', bandsJson: '[]', academyId: 'a',
    subject: 'Synthetic support', priority: 'Normal', description: 'Synthetic description', invoiceNumber: 'SYN-1', amount: '42', currency: 'INR',
    periodStart: '2026-10-01', periodEnd: '2026-10-31', dueDate: '2026-11-01', body: 'Synthetic announcement', hours: '24',
    monthlyAmount: '1000', periodLabel: 'Synthetic month', deductions: '10', reference: 'Synthetic reference' };
  let resets = 0;
  const form = { values, reset() { resets++; } };
  const forms = nodes(h.render()).filter(n => n.type === 'form');
  const node = scenario.marker ? forms.find(n => n.props.className?.includes(scenario.marker)) : forms[scenario.form];
  assert.ok(node, scenario.name);
  const event = { currentTarget: form, preventDefault() {} };
  const pending = node.props.onSubmit(event);
  event.currentTarget = null;
  await pending; await h.flush();
  assert.equal(writes.length, 1); assert.equal(writes[0].url, scenario.path);
  assert.equal(resets, accepted ? 1 : 0);
  if (scenario.name === 'payroll payout') assert.deepEqual(JSON.parse(writes[0].body), {
    payrollProfileId: 'profile', periodLabel: 'Synthetic month', sessionsCovered: null, grossAmount: null,
    deductions: 10, paymentMethod: 'BankTransfer', reference: 'Synthetic reference', paidAtUtc: null,
  });
  if (scenario.name === 'payroll profile') {
    const body = JSON.parse(writes[0].body);
    assert.equal(body.teacherId, 'teacher'); assert.equal(body.staffUserId, null);
    assert.equal(body.monthlyAmount, 1000); assert.equal(body.amountPerCycle, null); assert.equal(body.sessionsPerCycle, null);
  }
  if (outcome === 'refresh-failed') {
    assert.match(text(h.render()), scenario.file === 'payroll' ? /profile saved|payout recorded/i : /created|live|published/i);
    assert.match(text(h.render()), /refresh/i);
    assert.doesNotMatch(text(h.render()), /Synthetic readback failure/);
  }
  h.unmount();
}

for (const scenario of cases) for (const outcome of ['accepted', 'rejected'])
  test(`${scenario.name}: ${outcome} submission retains the correct form target`, () => exercise(scenario, outcome));
for (const scenario of cases.filter(s => s.file === 'platform/control' || s.file === 'payroll'))
  test(`${scenario.name}: confirmed write stays successful when readback fails`, () => exercise(scenario, 'refresh-failed'));
