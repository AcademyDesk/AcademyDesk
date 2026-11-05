'use strict';
const { test } = require('node:test');
const a = require('node:assert/strict');
const fs = require('node:fs');
const { spawnSync } = require('node:child_process');
const { validateAzureConfiguration, checkAzurePreflight, implementationBlockers } = require('./check-azure-preflight.cjs');
function fixture() {
  return {
    GITHUB_REF: 'refs/heads/main', GITHUB_EVENT_NAME: 'workflow_dispatch', GITHUB_SHA: '1'.repeat(40),
    AZURE_CREDENTIALS: JSON.stringify({ clientId: '11111111-1111-4111-8111-111111111111', tenantId: '22222222-2222-4222-8222-222222222222', subscriptionId: '33333333-3333-4333-8333-333333333333', clientSecret: 'synthetic-private-client' }),
    AZURE_STATIC_WEB_APPS_API_TOKEN: 'synthetic-private-swa', AZURE_RESOURCE_GROUP: 'academy-production',
    AZURE_REGISTRY_NAME: 'academysynthetic', AZURE_REGISTRY_LOGIN_SERVER: 'academysynthetic.azurecr.io', AZURE_API_CONTAINER_APP: 'academy-api',
    WEB_APP_ORIGIN: 'https://academy-synthetic.azurestaticapps.net', PRODUCTION_API_URL: 'https://academy-synthetic.azurecontainerapps.io',
    AZURE_SQL_CONNECTION_STRING: 'Server=tcp:academy-synthetic.database.windows.net,1433;Initial Catalog=Synthetic;User ID=synthetic;Password="synthetic;Encrypt=false";Encrypt=True;TrustServerCertificate=False;',
    AZURE_MEDIA_SERVICE_URI: 'https://academysynthetic.blob.core.windows.net', AZURE_MEDIA_CONTAINER: 'private-media',
    AZURE_KEY_RING_DIRECTORY: '/mnt/private-keys', AZURE_KEY_RING_CERTIFICATE_PATH: '/run/secrets/key-encryption.pfx',
    AZURE_KEY_RING_CERTIFICATE_PASSWORD: 'synthetic-private-certificate', AZURE_MIGRATION_ARTIFACT_SHA256: 'a'.repeat(64),
    AZURE_SQL_RESTORE_EVIDENCE_SHA256: 'b'.repeat(64), AZURE_ROLLBACK_IMAGE_SHA256: 'c'.repeat(64),
  };
}
const invalid = (key, value) => { const config = fixture(); config[key] = value; const result = validateAzureConfiguration(config); a.equal(result.ok, false); a.ok(result.errors.some(error => error.setting === key)); };
test('well-formed synthetic configuration passes input validation but cannot approve operational readiness', () => {
  a.equal(validateAzureConfiguration(fixture()).ok, true);
  const result = checkAzurePreflight(fixture());
  a.equal(result.ok, false); a.equal(result.configurationValid, true);
  a.deepEqual(result.errors.map(error => error.code), implementationBlockers.map(error => error.code));
  a.equal(result.errors.length, 3);
});
for (const key of Object.keys(fixture())) for (const value of [undefined, '', ' ', '\n\t', 'true', '${{ secrets.UNSET }}'])
  test(`missing or placeholder ${key} cannot pass (${JSON.stringify(value)})`, () => invalid(key, value));
for (const value of ['http://academy.example.net', 'http://localhost:5092', 'https://127.0.0.1', 'https://[::1]', 'https://localhost', 'https://app.localhost', 'https://app.internal', 'https://app.example.com', 'https://app.invalid', 'https://app.test', 'https://app.azurestaticapps.net/', 'https://app.azurestaticapps.net/path', 'https://app.azurestaticapps.net?token=private', 'https://app.azurestaticapps.net#private', 'https://private@app.azurestaticapps.net', 'https://app.azurestaticapps.net:8443', 'https://app.azurestaticapps.net\n'])
  for (const key of ['WEB_APP_ORIGIN', 'PRODUCTION_API_URL']) test(`invalid public origin ${key} ${value}`, () => invalid(key, value));
test('web and API cannot accidentally point to the same static origin', () => { const config = fixture(); config.PRODUCTION_API_URL = config.WEB_APP_ORIGIN; a.equal(validateAzureConfiguration(config).ok, false); });
for (const [key, values] of Object.entries({
  GITHUB_REF: ['refs/heads/codex/work', 'main'], GITHUB_EVENT_NAME: ['push', 'pull_request'], GITHUB_SHA: ['main', '0'.repeat(40)],
  AZURE_RESOURCE_GROUP: ['group;echo secret', 'group$(command)', 'group with spaces', '-group', 'group.'],
  AZURE_REGISTRY_NAME: ['a', 'AcademyRegistry', 'registry;command', 'a'.repeat(51)],
  AZURE_REGISTRY_LOGIN_SERVER: ['https://academysynthetic.azurecr.io', 'different.azurecr.io', 'academysynthetic.azurecr.io/private'],
  AZURE_API_CONTAINER_APP: ['-api', 'api-', 'api--production', 'API', 'a'.repeat(33)],
  AZURE_MEDIA_SERVICE_URI: ['http://academysynthetic.blob.core.windows.net', 'https://academysynthetic.blob.core.windows.net/container', 'https://academysynthetic.blob.core.windows.net?sas=secret', 'https://private@academysynthetic.blob.core.windows.net', 'https://arbitrary.net'],
  AZURE_MEDIA_CONTAINER: ['a', 'PrivateMedia', 'private--media', '$root', '../container'],
  AZURE_KEY_RING_DIRECTORY: ['/tmp/keys', '/app/keys', '/app/wwwroot/keys', 'relative', '/', '/mnt/../app/keys', '/mnt/private-keys/', '/mnt/wwwroot/keys', '/mnt/.git/keys', '/mnt/keys\\other'],
  AZURE_KEY_RING_CERTIFICATE_PATH: ['/mnt/private-keys/certificate.pfx', '/tmp/certificate.pfx', '/run/secrets/certificate.pem', '/run/secrets/../certificate.pfx'],
  AZURE_MIGRATION_ARTIFACT_SHA256: ['approved', 'a'.repeat(63), '0'.repeat(64)],
  AZURE_SQL_RESTORE_EVIDENCE_SHA256: ['PASS', 'https://evidence.test/accepted', '0'.repeat(64)],
  AZURE_ROLLBACK_IMAGE_SHA256: ['latest', 'sha256:'+'a'.repeat(64), '0'.repeat(64)],
})) for (const value of values) test(`unsafe or malformed ${key}: ${value}`, () => invalid(key, value));
for (const value of ['{', '[]', 'null', '{}', JSON.stringify({ clientId: 'not-a-guid', tenantId: 'not-a-guid', subscriptionId: 'not-a-guid', clientSecret: 'synthetic' })])
  test('malformed credential objects are rejected without loader details', () => invalid('AZURE_CREDENTIALS', value));
for (const [label, update] of [
  ['localhost', sql => sql.replace('tcp:academy-synthetic.database.windows.net,1433', 'localhost')],
  ['unencrypted', sql => sql.replace(';Encrypt=True;', ';Encrypt=False;')],
  ['missing encryption', sql => sql.replace(';Encrypt=True;', ';')],
  ['trusted server certificate', sql => sql.replace('TrustServerCertificate=False', 'TrustServerCertificate=True')],
  ['missing certificate validation', sql => sql.replace('TrustServerCertificate=False;', '')],
  ['conflicting encryption', sql => sql+'Encrypt=False;'],
  ['duplicate server alias', sql => sql+'Data Source=localhost;'],
  ['certificate hostname override', sql => sql+'Host Name In Certificate=arbitrary.net;'],
  ['blank database', sql => sql.replace('Initial Catalog=Synthetic', 'Initial Catalog=')],
  ['unclosed secret quote', sql => sql.replace('Password="synthetic;Encrypt=false"', 'Password="synthetic')],
  ['unexpected quoted suffix', sql => sql.replace('Password="synthetic;Encrypt=false"', 'Password="synthetic"extra')],
  ['interactive identity', sql => sql+'Authentication=Active Directory Interactive;'],
  ['trusted Windows identity', sql => sql+'Integrated Security=SSPI;'],
]) test('insecure Azure SQL configuration rejects '+label, () => invalid('AZURE_SQL_CONNECTION_STRING', update(fixture().AZURE_SQL_CONNECTION_STRING)));
test('explicit Azure managed identity SQL authentication and escaped secret delimiters are parsed safely', () => {
  const config=fixture();config.AZURE_SQL_CONNECTION_STRING='Data Source=academy-synthetic.database.windows.net;Database=Synthetic;Authentication=Active Directory Managed Identity;Encrypt=Strict;Trust Server Certificate=False';
  a.equal(validateAzureConfiguration(config).ok,true);
  config.AZURE_SQL_CONNECTION_STRING="Server=academy-synthetic.database.windows.net;Database=Synthetic;UID=synthetic;PWD='synthetic;it''s private';Encrypt=True;TrustServerCertificate=False";
  a.equal(validateAzureConfiguration(config).ok,true);
});
test('bootstrap is optional but partial or malformed settings cannot be silently skipped', () => {
  for (const partial of [{PLATFORM_OWNER_EMAIL:'owner@academy.test'}, {PLATFORM_OWNER_PASSWORD:'synthetic-private-owner'}, {PLATFORM_OWNER_NAME:'Owner'}, {PLATFORM_OWNER_EMAIL:' ',PLATFORM_OWNER_PASSWORD:' '}])
    a.equal(validateAzureConfiguration({...fixture(),...partial}).ok,false);
  a.equal(validateAzureConfiguration({...fixture(),PLATFORM_OWNER_EMAIL:'owner@academy.test',PLATFORM_OWNER_NAME:'Synthetic Owner',PLATFORM_OWNER_PASSWORD:'synthetic-private-owner'}).ok,true);
});
test('ready flags and fabricated acceptance labels cannot bypass implementation blockers', () => {
  const result=checkAzurePreflight({...fixture(),AZURE_PREFLIGHT_READY:'true',BLOB_READY:'true',SHARED_KEY_RING_READY:'true',MIGRATION_APPROVED:'true',STAGING_ACCEPTED:'PASS'});
  a.equal(result.ok,false);a.equal(result.errors.length,3);
});
test('CLI fails closed and never prints supplied secrets, paths, identifiers or host values', () => {
  const config=fixture(), result=spawnSync(process.execPath,[require.resolve('./check-azure-preflight.cjs')],{env:config,encoding:'utf8'});
  a.equal(result.status,1);const output=result.stdout+result.stderr;
  for(const value of [...Object.values(config),...Object.values(JSON.parse(config.AZURE_CREDENTIALS))]) a.equal(output.includes(value),false);
  a.ok(output.includes('PRIVATE_MEDIA_PROVISIONING_MISSING'));a.ok(output.includes('CONTROLLED_MIGRATION_RECOVERY_MISSING'));
});
test('deployment retains dispatch/main/production/verification/P0 gates and checks preflight before login or writes', () => {
  const workflow=fs.readFileSync(require('node:path').join(__dirname,'../../.github/workflows/deploy-azure.yml'),'utf8');
  a.match(workflow,/workflow_dispatch:/);a.doesNotMatch(workflow,/^  (?:push|pull_request):/m);
  a.match(workflow,/if: github.ref == 'refs\/heads\/main'/);a.match(workflow,/environment: production/);
  a.match(workflow,/uses: \.\/\.github\/workflows\/verify.yml/);a.match(workflow,/needs: verify/);
  const p0=workflow.indexOf('run: node QA/tools/check-production-gates.cjs'),preflight=workflow.indexOf('run: node QA/tools/check-azure-preflight.cjs'),login=workflow.indexOf('uses: azure/login@v2');
  a.ok(p0>0&&preflight>p0&&login>preflight);a.ok(workflow.indexOf('run: az acr login')>login);
  a.match(workflow,/Database__ApplyMigrationsOnStartup=true/); // Unchanged implementation is intentionally blocked.
  for(const key of Object.keys(fixture()).filter(key=>!key.startsWith('GITHUB_'))) a.match(workflow,new RegExp('          '+key+':'));
});
