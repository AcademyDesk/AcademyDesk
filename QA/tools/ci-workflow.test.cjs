'use strict';
const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), path = require('node:path');
const root = path.resolve(__dirname, '../..');
const yaml = require(require.resolve('js-yaml', { paths: [path.join(root, 'apps/web')] }));
const read = name => yaml.load(fs.readFileSync(path.join(root, '.github/workflows', name), 'utf8'));
function enforce(deploy, checks) {
  assert.deepEqual(Object.keys(deploy.on), ['workflow_dispatch'], 'Deployment remains manually dispatched.');
  assert.equal(deploy.permissions.contents, 'read');
  assert.equal(deploy.defaults.run.shell, 'bash', 'Pipeline failures require explicit Bash pipefail semantics.');
  assert.equal(deploy.jobs.verify.uses, './.github/workflows/verify.yml');
  assert.equal(deploy.jobs.verify.secrets, undefined, 'Developer checks receive no production secrets.');
  assert.equal(deploy.jobs.verify.with.web_api_url, '${{ vars.PRODUCTION_API_URL }}');
  assert.equal(deploy.jobs.deploy.needs, 'verify', 'All verification jobs must succeed before deployment.');
  assert.equal(deploy.jobs.deploy.if, undefined, 'No always/skip override may bypass verification.');
  assert.equal(deploy.jobs.deploy['continue-on-error'], undefined);
  assert.equal(deploy.jobs.deploy.environment, 'production');
  const steps = deploy.jobs.deploy.steps;
  for (const step of steps) if (step.uses) assert.match(step.uses, /^(actions|azure|Azure)\/[a-z-]+@[a-f0-9]{40}$/);
  const guard = steps.findIndex(s => s.name === 'Verify exact candidate source, content and web API origin before Azure access');
  const login = steps.findIndex(s => s.uses?.startsWith('azure/login@'));
  assert(guard >= 0 && login > guard);
  assert.match(steps[guard].run, /ci-release-artifacts\.cjs verify .* api /);
  assert.match(steps[guard].run, /ci-release-artifacts\.cjs verify .* web /);
  assert.match(steps[guard].run, /docker load/);
  assert.equal(steps[guard]['continue-on-error'], undefined);
  assert(!steps.some(s => /docker build|npm ci|npm run build/.test(s.run ?? '')), 'Deploy promotes checked artifacts; it must not rebuild.');
  assert(checks.on.workflow_call); assert(checks.on.pull_request);
  assert.equal(checks.permissions.contents, 'read');
  assert.equal(checks.defaults.run.shell, 'bash');
  for (const job of Object.values(checks.jobs)) {
    assert.equal(job.environment, undefined);
    assert.equal(job['continue-on-error'], undefined);
    assert(job['timeout-minutes'] > 0 && job['timeout-minutes'] <= 30);
    for (const step of job.steps) {
      assert.equal(step['continue-on-error'], undefined);
      assert(!/azure\//i.test(step.uses ?? ''));
      assert(!/secrets\.|az containerapp|docker push|\|\|\s*true/.test(JSON.stringify(step)));
      if (step.uses) assert.match(step.uses, /^actions\/[a-z-]+@[a-f0-9]{40}$/);
    }
  }
  const api = checks.jobs.api.steps.map(s => s.run ?? '').join('\n');
  assert.match(api, /dotnet build AcademyDesk\.slnx/); assert.match(api, /dotnet test tests\/AcademyDesk.Api.Tests\/AcademyDesk.Api.Tests.csproj/);
  assert(!/--filter|--list-tests/.test(api));
  assert.match(api, /ci-release-artifacts\.cjs create .* api /);
  const web = checks.jobs.web.steps.map(s => s.run ?? '').join('\n');
  assert.match(web, /npm ci/); assert.match(web, /npm run lint -- --max-warnings=0/);
  assert.match(web, /npx --no-install tsc --noEmit/); assert.match(web, /npm run build/);
  assert.match(web, /ci-release-artifacts\.cjs create .* web /);
}
test('actual workflows fail closed before Azure access and promote source-bound artifacts', () => enforce(read('deploy-azure.yml'), read('verify.yml')));
for (const [name, mutate] of Object.entries({
  missing_dependency: d => { delete d.jobs.deploy.needs; },
  skipped_check_override: d => { d.jobs.deploy.if = '${{ always() }}'; },
  forwarded_secrets: d => { d.jobs.verify.secrets = 'inherit'; },
  remote_verification: d => { d.jobs.verify.uses = 'somewhere/else/.github/workflows/verify.yml@main'; },
  rebuild_after_checks: d => { d.jobs.deploy.steps.push({ run: 'npm run build' }); },
  deploy_on_push: d => { d.on.push = {}; },
  optional_artifact_guard: d => { d.jobs.deploy.steps.find(s => s.name?.startsWith('Verify exact candidate'))['continue-on-error'] = true; },
  lost_pipefail: d => { delete d.defaults; }
})) test(`workflow regression rejects ${name}`, () => {
  const deploy = read('deploy-azure.yml'); mutate(deploy);
  assert.throws(() => enforce(deploy, read('verify.yml')));
});
