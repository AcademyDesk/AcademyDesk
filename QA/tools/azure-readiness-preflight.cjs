'use strict';

// Local input validation only. Never invokes Azure, GitHub, a model or a shell.
// Reports setting names and fixed codes; supplied values must never reach output.
const path = require('node:path');
const { checkProductionGates } = require('./check-production-gates.cjs');

const required = Object.freeze([
  'AZURE_RESOURCE_GROUP', 'AZURE_REGISTRY_NAME', 'AZURE_REGISTRY_LOGIN_SERVER',
  'AZURE_API_CONTAINER_APP', 'WEB_APP_ORIGIN', 'PRODUCTION_API_URL',
  'AZURE_CREDENTIALS', 'AZURE_STATIC_WEB_APPS_API_TOKEN',
  'AZURE_SQL_CONNECTION_STRING', 'PENTA_SOURCE_READ_TOKEN',
  'ASPNETCORE_ENVIRONMENT', 'MediaStorage__Enabled', 'MediaStorage__ServiceUri',
  'MediaStorage__ContainerName', 'Security__DataProtection__Enabled',
  'Security__DataProtection__KeyRingDirectory',
  'Security__DataProtection__CertificatePath',
  'Security__DataProtection__CertificatePassword',
]);
const get = (env, key) => Object.hasOwn(env, key) && typeof env[key] === 'string' ? env[key] : '';
const present = value => value.trim().length > 0;
function origin(value) {
  try {
    if (!value || value !== value.trim()) return null;
    const u = new URL(value);
    return u.protocol === 'https:' && !u.username && !u.password && !u.port &&
      u.pathname === '/' && !u.search && !u.hash &&
      u.hostname.length <= 253 && u.hostname.includes('.') &&
      u.hostname.split('.').every(label => /^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/i.test(label)) &&
      !/^\d+\.\d+\.\d+\.\d+$/.test(u.hostname) &&
      !u.hostname.endsWith('.localhost') ? u.origin : null;
  } catch { return null; }
}
function privateLinuxPath(value) {
  return value.length > 1 && !value.includes('\0') && !value.includes('\\') &&
    path.posix.isAbsolute(value) && path.posix.normalize(value) === value &&
    !['/app', '/src', '/workspace', '/wwwroot', '/tmp', '/var/tmp'].some(p => value === p || value.startsWith(p + '/'));
}
function validateConfiguration(env) {
  const failures = [];
  const fail = (code, setting) => failures.push({ code, setting });
  for (const key of required) if (!present(get(env, key))) fail('MISSING_REQUIRED_INPUT', key);
  const check = (key, valid, code = 'INVALID_INPUT') => {
    const value = get(env, key);
    if (present(value) && !valid(value)) fail(code, key);
  };
  check('AZURE_RESOURCE_GROUP', v => /^[a-zA-Z0-9_.-]{1,90}$/.test(v) && !v.endsWith('.'));
  check('AZURE_REGISTRY_NAME', v => /^[a-zA-Z0-9]{5,50}$/.test(v));
  check('AZURE_REGISTRY_LOGIN_SERVER', v => /^[a-zA-Z0-9]{5,50}\.azurecr\.io$/.test(v) &&
    v.toLowerCase() === get(env, 'AZURE_REGISTRY_NAME').toLowerCase() + '.azurecr.io');
  check('AZURE_API_CONTAINER_APP', v => /^[a-z][a-z0-9-]{0,30}[a-z0-9]$/.test(v) && !v.includes('--'));
  for (const key of ['WEB_APP_ORIGIN', 'PRODUCTION_API_URL']) check(key, v => origin(v) !== null, 'EXACT_HTTPS_ORIGIN_REQUIRED');
  const web = origin(get(env, 'WEB_APP_ORIGIN')), api = origin(get(env, 'PRODUCTION_API_URL'));
  if (web && api && web === api) fail('SEPARATE_STATIC_WEB_AND_API_ORIGINS_REQUIRED', 'PRODUCTION_API_URL');
  for (const [key, expected] of [['NEXT_PUBLIC_API_URL', api], ['Cors__AllowedOrigins__0', web]]) {
    check(key, v => expected !== null && origin(v) === expected, 'BUILD_RUNTIME_ORIGIN_MISMATCH');
  }
  check('ASPNETCORE_ENVIRONMENT', v => v === 'Production', 'PRODUCTION_ENVIRONMENT_REQUIRED');
  for (const key of ['MediaStorage__Enabled', 'Security__DataProtection__Enabled']) check(key, v => v === 'true', 'EXPLICIT_TRUE_REQUIRED');
  check('MediaStorage__ServiceUri', v => /^https:\/\/[a-z0-9]{3,24}\.blob\.core\.windows\.net\/?$/.test(v), 'PRIVATE_BLOB_SERVICE_ORIGIN_REQUIRED');
  check('MediaStorage__ContainerName', v => /^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$/.test(v) && !v.includes('--'));
  check('MediaStorage__ManagedIdentityClientId', v => /^[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}$/i.test(v));
  const directory = get(env, 'Security__DataProtection__KeyRingDirectory');
  check('Security__DataProtection__KeyRingDirectory', privateLinuxPath, 'PRIVATE_PERSISTENT_PATH_REQUIRED');
  check('Security__DataProtection__CertificatePath', v => privateLinuxPath(v) && /\.(?:pfx|p12)$/i.test(v) &&
    v !== directory && !v.startsWith(directory + '/'), 'SEPARATE_PRIVATE_CERTIFICATE_REQUIRED');
  const email = present(get(env, 'PLATFORM_OWNER_EMAIL')), password = present(get(env, 'PLATFORM_OWNER_PASSWORD'));
  if (email !== password) fail('BOOTSTRAP_REQUIRES_EMAIL_AND_PASSWORD_TOGETHER', 'PLATFORM_OWNER_EMAIL/PLATFORM_OWNER_PASSWORD');
  for (const key of ['NEXT_PUBLIC_PENTA_MINI_EVALUATION', 'QA_INTERACTIVE_DEMO', 'Penta__Enabled', 'Penta__Mini__Enabled']) {
    check(key, v => v === 'false', 'EVALUATION_OR_UNQUALIFIED_AI_FLAG_MUST_BE_FALSE');
  }
  for (const key of ['DevelopmentSeed__DefaultPassword', 'DevelopmentSeed__PlatformOwnerPassword', 'DevelopmentSeed__AcademyAdminPassword']) {
    if (present(get(env, key))) fail('DEVELOPMENT_CREDENTIAL_MUST_NOT_BE_PROVISIONED', key);
  }
  return { ok: failures.length === 0, failures };
}

function buildReport(env, root = path.resolve(__dirname, '../..')) {
  const configuration = validateConfiguration(env);
  let criticalAcceptance;
  try { criticalAcceptance = checkProductionGates(root); }
  catch { criticalAcceptance = { ok: false, checked: [], failures: ['Critical acceptance records are unreadable.'] }; }
  return {
    scope: 'Local deployment-input validation and existing critical-record guard only.',
    localChecksPassed: configuration.ok && criticalAcceptance.ok,
    configuration, criticalAcceptance,
    productionQualified: false, deploymentAuthorized: false, azureResourcesVerified: false,
    aiCoveragePercent: null,
    stillRequired: [
      'Exact candidate ordinary and paired hosted CI, dependency/security acceptance and original Mini archives.',
      'Qualified Production provider/tool path and independently measured eligible conversational outcomes.',
      'Actual Azure identity/RBAC, private SQL/Blob connectivity, encrypted shared key-ring mount and certificate lifecycle.',
      'Physical-device, exact-candidate staging, migration/restore/rollback, monitoring and authenticated smoke acceptance.',
      'Reviewed release candidate and separate deployment authorization.',
    ],
  };
}
if (require.main === module) {
  if (process.argv.length === 3 && process.argv[2] === '--help') {
    console.log(JSON.stringify({ scope: 'Read locally provisioned environment inputs; never supply secrets as command arguments or commit an env file.', required }, null, 2));
  } else if (process.argv.length !== 2) {
    console.error('Unsupported arguments. Use --help. No supplied argument is printed.');
    process.exitCode = 2;
  } else {
    const report = buildReport(process.env);
    console.log(JSON.stringify(report, null, 2));
    process.exitCode = report.localChecksPassed ? 0 : 1;
  }
}
module.exports = { required, validateConfiguration, buildReport };
