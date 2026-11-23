'use strict';
const { test } = require('node:test');
const a = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');
const os = require('node:os');
const { spawnSync } = require('node:child_process');
const { knownCritical } = require('./check-production-gates.cjs');
const { required, validateConfiguration, buildReport } = require('./azure-readiness-preflight.cjs');

// These strings test presence/redaction, not real credential or Azure validity.
const canary = 'PRIVATE-CANARY-DO-NOT-DISCLOSE';
function configuration() {
  return {
    AZURE_RESOURCE_GROUP: 'synthetic-group', AZURE_REGISTRY_NAME: 'syntheticregistry',
    AZURE_REGISTRY_LOGIN_SERVER: 'syntheticregistry.azurecr.io', AZURE_API_CONTAINER_APP: 'synthetic-api',
    WEB_APP_ORIGIN: 'https://app.example.invalid', PRODUCTION_API_URL: 'https://api.example.invalid',
    AZURE_CREDENTIALS: canary, AZURE_STATIC_WEB_APPS_API_TOKEN: canary,
    AZURE_SQL_CONNECTION_STRING: canary, PENTA_SOURCE_READ_TOKEN: canary,
    ASPNETCORE_ENVIRONMENT: 'Production', MediaStorage__Enabled: 'true',
    MediaStorage__ServiceUri: 'https://syntheticstorage.blob.core.windows.net', MediaStorage__ContainerName: 'private-media',
    Security__DataProtection__Enabled: 'true', Security__DataProtection__KeyRingDirectory: '/mnt/private-keys',
    Security__DataProtection__CertificatePath: '/mnt/private-certs/protection.pfx',
    Security__DataProtection__CertificatePassword: canary,
  };
}
function acceptedRecords(t) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'academydesk-azure-preflight-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const issues = path.join(root, 'QA/ISSUES'); fs.mkdirSync(issues, { recursive: true });
  for (const id of [...knownCritical, 'BUG-AUTH-REVOCATION-001']) {
    fs.writeFileSync(path.join(issues, id + '.md'), '| Status | CLOSED |\n| Final verification | PASS |\n');
  }
  fs.writeFileSync(path.join(issues, 'INDEX.md'), knownCritical.map(id => `| [${id}](${id}.md) | Synthetic | P0 | Critical |`).join('\n'));
  return root;
}
test('even accepted local declarations and synthetic records cannot imply Azure or production acceptance', t => {
  a.deepEqual(validateConfiguration(configuration()), { ok: true, failures: [] });
  const report = buildReport(configuration(), acceptedRecords(t));
  a.equal(report.localChecksPassed, true);
  a.equal(report.criticalAcceptance.checked.length, 6);
  for (const key of ['productionQualified', 'deploymentAuthorized', 'azureResourcesVerified']) a.equal(report[key], false);
  a.equal(report.aiCoveragePercent, null);
});
test('an unaccepted critical record blocks otherwise valid local inputs', t => {
  const root = acceptedRecords(t);
  fs.writeFileSync(path.join(root, 'QA/ISSUES', knownCritical[0] + '.md'), '| Status | OPEN |\n| Final verification | NOT RUN |\n');
  const report = buildReport(configuration(), root);
  a.equal(report.localChecksPassed, false); a.equal(report.criticalAcceptance.ok, false);
});
for (const key of required) test('missing required input fails: ' + key, () => {
  const env = configuration(); delete env[key];
  a.ok(validateConfiguration(env).failures.some(f => f.setting === key && f.code === 'MISSING_REQUIRED_INPUT'));
});
for (const url of ['http://api.example.invalid', 'https://user:password@api.example.invalid',
  'https://api.example.invalid/path', 'https://api.example.invalid?token=' + canary,
  'https://api.example.invalid#fragment', 'https://127.0.0.1', 'https://test.localhost',
  'https://api..example.invalid', 'https://api.-example.invalid', 'https://' + 'a'.repeat(64) + '.example.invalid',
  'https://api.example.invalid:444', ' https://api.example.invalid']) test('unsafe API origin is refused without disclosure: ' + url.split('?')[0], () => {
  const env = configuration(); env.PRODUCTION_API_URL = url;
  const result = validateConfiguration(env); a.equal(result.ok, false);
  a.equal(JSON.stringify(result).includes(canary), false); a.equal(JSON.stringify(result).includes(url), false);
});
for (const [key, value] of [
  ['AZURE_RESOURCE_GROUP', 'invalid group'], ['AZURE_RESOURCE_GROUP', 'invalid;command'],
  ['AZURE_RESOURCE_GROUP', 'invalid.'],
  ['AZURE_REGISTRY_LOGIN_SERVER', 'otherregistry.azurecr.io'], ['AZURE_REGISTRY_LOGIN_SERVER', 'registry.example.invalid'],
  ['AZURE_REGISTRY_NAME', 'invalid.registry'], ['AZURE_API_CONTAINER_APP', 'invalid--app'],
  ['WEB_APP_ORIGIN', 'https://api.example.invalid'], ['NEXT_PUBLIC_API_URL', 'https://other.example.invalid'],
  ['Cors__AllowedOrigins__0', 'https://other.example.invalid'], ['ASPNETCORE_ENVIRONMENT', 'Testing'],
  ['MediaStorage__Enabled', 'false'], ['MediaStorage__ServiceUri', 'https://syntheticstorage.blob.core.windows.net?sig=' + canary],
  ['MediaStorage__ContainerName', 'public--media'], ['MediaStorage__ManagedIdentityClientId', canary],
  ['Security__DataProtection__Enabled', 'false'], ['Security__DataProtection__KeyRingDirectory', '/app/private-keys'],
  ['Security__DataProtection__KeyRingDirectory', '/mnt/../private-keys'], ['Security__DataProtection__KeyRingDirectory', '/tmp/keys'],
  ['Security__DataProtection__CertificatePath', '/mnt/private-keys/protection.pfx'],
  ['Security__DataProtection__CertificatePath', 'D:\\private\\protection.pfx'],
  ['Security__DataProtection__CertificatePath', '/mnt/private-certs/protection.pem'],
  ['PLATFORM_OWNER_EMAIL', 'synthetic@example.invalid'], ['PLATFORM_OWNER_PASSWORD', canary],
  ['NEXT_PUBLIC_PENTA_MINI_EVALUATION', 'true'], ['QA_INTERACTIVE_DEMO', 'true'],
  ['Penta__Enabled', 'true'], ['Penta__Mini__Enabled', 'true'],
  ['DevelopmentSeed__DefaultPassword', canary], ['DevelopmentSeed__PlatformOwnerPassword', canary],
  ['DevelopmentSeed__AcademyAdminPassword', canary],
]) test('invalid or evaluation configuration is blocked: ' + key + '/' + value.split('?')[0].replace(canary, '[canary]'), () => {
  const env = configuration(); env[key] = value;
  const result = validateConfiguration(env); a.equal(result.ok, false); a.equal(JSON.stringify(result).includes(canary), false);
});
test('optional bootstrap, matching origins and disabled evaluation flags are valid declarations', () => {
  const env = { ...configuration(), PLATFORM_OWNER_EMAIL: 'synthetic@example.invalid', PLATFORM_OWNER_PASSWORD: canary,
    NEXT_PUBLIC_API_URL: 'https://api.example.invalid/', Cors__AllowedOrigins__0: 'https://app.example.invalid',
    NEXT_PUBLIC_PENTA_MINI_EVALUATION: 'false', Penta__Enabled: 'false', Penta__Mini__Enabled: 'false', QA_INTERACTIVE_DEMO: 'false',
    MediaStorage__ManagedIdentityClientId: '11111111-2222-4333-8444-555555555555' };
  a.equal(validateConfiguration(env).ok, true);
});
test('whitespace, non-string and inherited values cannot stand in for a required secret', () => {
  for (const value of ['   ', 1, null, {}]) {
    const env = configuration(); env.AZURE_CREDENTIALS = value; a.equal(validateConfiguration(env).ok, false);
  }
  const env = Object.create(configuration()); a.equal(validateConfiguration(env).ok, false);
});
test('unreadable critical evidence stays blocked without exposing its private path', () => {
  const report = buildReport(configuration(), path.join(__dirname, canary, 'absent'));
  a.equal(report.localChecksPassed, false); a.equal(JSON.stringify(report).includes(canary), false);
});
test('CLI output excludes every credential value and exits blocked on incomplete bootstrap', () => {
  const result = spawnSync(process.execPath, [path.join(__dirname, 'azure-readiness-preflight.cjs')], {
    env: { ...process.env, ...configuration(), PLATFORM_OWNER_PASSWORD: canary, PLATFORM_OWNER_EMAIL: '' }, encoding: 'utf8',
  });
  a.ifError(result.error);
  a.equal(result.status, 1); a.equal((result.stdout + result.stderr).includes(canary), false);
  const report = JSON.parse(result.stdout); a.equal(report.configuration.ok, false); a.equal(report.deploymentAuthorized, false);
});
test('unsupported CLI arguments are neither evaluated nor echoed', () => {
  const result = spawnSync(process.execPath, [path.join(__dirname, 'azure-readiness-preflight.cjs'), canary], { encoding: 'utf8' });
  a.ifError(result.error);
  a.equal(result.status, 2); a.equal((result.stdout + result.stderr).includes(canary), false);
});
test('help lists input names without requiring or inspecting credential values', () => {
  const result = spawnSync(process.execPath, [path.join(__dirname, 'azure-readiness-preflight.cjs'), '--help'], {
    env: { ...process.env, AZURE_CREDENTIALS: canary }, encoding: 'utf8',
  });
  a.ifError(result.error);
  a.equal(result.status, 0); a.equal((result.stdout + result.stderr).includes(canary), false);
  a.deepEqual(JSON.parse(result.stdout).required, required);
});
