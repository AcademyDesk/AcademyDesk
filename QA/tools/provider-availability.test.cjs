// Actual TSX handlers with synthetic transport; this is not live browser/provider evidence.
const test = require('node:test');
const assert = require('node:assert/strict');
const { mount, nodes, text, response } = require('./frontend-page-runtime.cjs');
const { page, base, channels } = require('./communication-page-harness.cjs');

for (const provider of ['GoogleWorkspace', 'Zoom', 'Microsoft365']) {
  for (const hasSecureConnection of [false, true]) {
    test(`Meeting ${provider}, recorded connection=${hasSecureConnection}: no operational creation or fake history`, async () => {
      const calls = [];
      const p = mount('apps/web/src/app/meeting-links/page.tsx', async (url, options = {}) => {
        calls.push({ url, ...options });
        return response(url === '/api/academies' ? [{ id: 'owned' }]
          : url.endsWith('/communication-settings') ? [{ channel: 'Meeting', provider, hasSecureConnection }]
          : []);
      });
      await p.flush();
      let tree = p.render();
      const selection = nodes(tree).find(n => n.props?.name === 'provider');
      if (selection) selection.props.onChange(provider);
      tree = p.render();
      assert.match(text(tree), /meeting creation.*unavailable/i);
      const form = nodes(tree).find(n => n.type === 'form');
      assert.ok(form, 'The unavailable form retains an explicit disabled state');
      const fieldset = nodes(form).find(n => n.type === 'fieldset' && n.props.disabled);
      assert.ok(fieldset, 'All creation fields must be disabled together');
      const controls = nodes(form).filter(n => ['input', 'button', 'select-control', 'date-control', 'time-control'].includes(n.type));
      assert.ok(controls.length > 0);
      for (const control of controls) assert.ok(control.props.disabled || nodes(fieldset).includes(control), 'No enabled creation input');
      form.props.onSubmit({ preventDefault() {} }); // Defense against a stale/forced submit.
      await p.flush();
      assert.equal(calls.filter(c => c.method && c.method !== 'GET').length, 0);
      tree = p.render();
      assert.doesNotMatch(text(tree), /will send|ready to create|when created, use edit|no meeting links have been created/i);
      assert.ok(!nodes(tree).some(n => n.type === 'button' && /edit details/i.test(text(n))));
      assert.match(text(tree), /meeting history.*unavailable/i);
      p.unmount();
    });
  }
}

for (const hasSecureConnection of [false, true]) {
  for (const channel of channels) {
    test(`${channel} metadata save, recorded connection=${hasSecureConnection}: preserves persistence without claiming provider readiness`, async () => {
      const p = await page({ rows: base.map(row => ({ ...row, hasSecureConnection })) });
      assert.match(text(p.render()), /provider connection.*unavailable/i);
      assert.doesNotMatch(text(p.render()), /connected securely in the next step/i);
      assert.equal(p.disabled(channel), false, 'Real metadata editing remains available');
      p.edit(channel, 'senderName', 'Saved metadata');
      await p.save(channel);
      assert.equal(p.writes().length, 1);
      assert.equal(p.rows().find(row => row.channel === channel).senderName, 'Saved metadata');
      const payload = JSON.parse(p.writes()[0].body);
      assert.ok(!Object.hasOwn(payload, 'hasSecureConnection'), 'Metadata save must not manufacture provider verification');
      assert.match(p.notice(channel), /settings saved/i);
      assert.match(p.notice(channel), /metadata only|provider connection.*unavailable/i);
      assert.doesNotMatch(p.notice(channel), /successfully connected|ready to send|ready to create/i);
      assert.ok(!nodes(p.render()).some(n => n.type === 'button' && /^(connect|test connection|send)/i.test(text(n))));
    });
  }
}
