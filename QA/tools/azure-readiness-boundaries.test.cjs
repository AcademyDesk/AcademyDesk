'use strict';
const { test } = require('node:test');
const a = require('node:assert/strict');
const { validateConfiguration } = require('./azure-readiness-preflight.cjs');

// Invented presence markers only; never load a deployment credential to test.
function configuration() {
  return {
    AZURE_RESOURCE_GROUP: 'synthetic-group', AZURE_REGISTRY_NAME: 'syntheticregistry',
    AZURE_REGISTRY_LOGIN_SERVER: 'syntheticregistry.azurecr.io', AZURE_API_CONTAINER_APP: 'synthetic-api',
    WEB_APP_ORIGIN: 'https://app.example.invalid', PRODUCTION_API_URL: 'https://api.example.invalid',
    AZURE_CREDENTIALS: 'synthetic-only', AZURE_STATIC_WEB_APPS_API_TOKEN: 'synthetic-only',
    AZURE_SQL_CONNECTION_STRING: 'synthetic-only', PENTA_SOURCE_READ_TOKEN: 'synthetic-only',
    ASPNETCORE_ENVIRONMENT: 'Production', MediaStorage__Enabled: 'true',
    MediaStorage__ServiceUri: 'https://syntheticstorage.blob.core.windows.net', MediaStorage__ContainerName: 'private-media',
    Security__DataProtection__Enabled: 'true', Security__DataProtection__KeyRingDirectory: '/mnt/private-keys',
    Security__DataProtection__CertificatePath: '/mnt/private-certs/protection.pfx',
    Security__DataProtection__CertificatePassword: 'synthetic-only'
  };
}
for (const suffix of ['', '/']) test('certificate within key directory is refused with directory suffix ' + JSON.stringify(suffix), () => {
  const env = configuration(); env.Security__DataProtection__KeyRingDirectory += suffix;
  env.Security__DataProtection__CertificatePath = '/mnt/private-keys/protection.pfx';
  a.ok(validateConfiguration(env).failures.some(x => x.code === 'SEPARATE_PRIVATE_CERTIFICATE_REQUIRED'));
});
for (const value of ['Testing', 'Development', 'Staging']) test('DOTNET_ENVIRONMENT cannot override declared Production with ' + value, () => {
  const env = { ...configuration(), DOTNET_ENVIRONMENT: value };
  a.ok(validateConfiguration(env).failures.some(x => x.setting === 'DOTNET_ENVIRONMENT' && x.code === 'PRODUCTION_ENVIRONMENT_REQUIRED'));
});
test('matching Production bootstrap environment and separate trailing key directory remain valid declarations', () => {
  const env = { ...configuration(), DOTNET_ENVIRONMENT: 'Production', Security__DataProtection__KeyRingDirectory: '/mnt/private-keys/' };
  a.equal(validateConfiguration(env).ok, true);
});
