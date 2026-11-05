'use strict';
// Local configuration checks only. No Azure request, credential output, approval
// assertion or resource mutation. This supplements every existing release gate.
const { isIP } = require('node:net');
const path = require('node:path').posix;

// These are implementation gaps in deploy-azure.yml, not switches an operator
// can approve. Replace a blocker only alongside its reviewed implementation and
// actual acceptance. Configuration strings and evidence hashes do not close it.
const implementationBlockers = Object.freeze([
  Object.freeze({ code: 'PRIVATE_MEDIA_PROVISIONING_MISSING', setting: 'deployment', message: 'The deployment does not provision or verify private Blob storage, API managed identity and scoped storage access, or apply the private media configuration.' }),
  Object.freeze({ code: 'SHARED_KEY_STORAGE_PROVISIONING_MISSING', setting: 'deployment', message: 'The deployment does not provision or verify shared encrypted key storage and certificate access across replicas and revisions.' }),
  Object.freeze({ code: 'CONTROLLED_MIGRATION_RECOVERY_MISSING', setting: 'deployment', message: 'The deployment still enables startup migrations and has no controlled migration, compatible rollback and production restore acceptance step.' }),
]);
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const sha256 = /^[0-9a-f]{64}$/;
const placeholder = /^(?:true|false|null|undefined|todo|changeme|replace[-_ ]?me|pending|not[-_ ]?set)$/i;
function supplied(value) {
  return typeof value === 'string' && value.trim().length > 0 && !placeholder.test(value.trim()) && !/\$\{\{|[\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/.test(value);
}
function publicHost(host) {
  return host.length <= 253 && !isIP(host) && host.includes('.') &&
    host.split('.').every(label => /^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/.test(label)) &&
    !/(?:^|\.)(?:localhost|local|internal|invalid|example|test)$/.test(host) &&
    !/(?:^|\.)example\.(?:com|net|org)$/.test(host);
}
function origin(value) {
  try { const url = new URL(value); return url.protocol === 'https:' && !url.port && url.origin === value && publicHost(url.hostname); }
  catch { return false; }
}
function privatePath(value, certificate = false) {
  return typeof value === 'string' && value === path.normalize(value) && !value.endsWith('/') &&
    !/[\\\s\u0000-\u001f\u007f]/.test(value) &&
    (certificate ? /^\/run\/secrets\/[A-Za-z0-9_./-]+\.(?:pfx|p12)$/i.test(value) : /^\/mnt\/[A-Za-z0-9_./-]+$/.test(value)) &&
    !value.split('/').some(part => part === '.git' || part === 'wwwroot' || part === '.' || part === '..');
}

// Parse quoted SqlClient values rather than searching the raw secret: passwords
// may contain semicolons and security keywords. Duplicate aliases fail closed.
function sqlValues(input) {
  const values = new Map();
  let offset = 0;
  while (offset < input.length) {
    while (/[;\s]/.test(input[offset] ?? '') && offset < input.length) offset++;
    if (offset === input.length) break;
    const keyStart = offset;
    while (offset < input.length && input[offset] !== '=' && input[offset] !== ';') offset++;
    if (input[offset] !== '=') throw Error('Invalid connection setting');
    const raw = input.slice(keyStart, offset++).trim().toLowerCase().replaceAll(' ', '');
    const key = ({ 'datasource': 'server', 'address': 'server', 'addr': 'server', 'networkaddress': 'server', 'initialcatalog': 'database', 'uid': 'userid', 'pwd': 'password', 'trusted_connection': 'integratedsecurity' })[raw] ?? raw;
    if (!/^[a-z_]+$/.test(key) || values.has(key)) throw Error('Invalid connection setting');
    while (/\s/.test(input[offset] ?? '') && offset < input.length) offset++;
    let value = '';
    if (input[offset] === '"' || input[offset] === "'") {
      const quote = input[offset++]; let closed = false;
      while (offset < input.length) {
        if (input[offset] === quote) {
          if (input[offset + 1] === quote) { value += quote; offset += 2; }
          else { offset++; closed = true; break; }
        } else value += input[offset++];
      }
      if (!closed) throw Error('Invalid connection setting');
      while (/\s/.test(input[offset] ?? '') && offset < input.length) offset++;
      if (offset < input.length && input[offset] !== ';') throw Error('Invalid connection setting');
    } else {
      const start = offset;
      while (offset < input.length && input[offset] !== ';') offset++;
      value = input.slice(start, offset).trim();
    }
    values.set(key, value);
  }
  return values;
}
function secureAzureSql(value) {
  try {
    const values = sqlValues(value);
    const authentication = (values.get('authentication') ?? '').toLowerCase().replaceAll(' ', '');
    const authenticated = ['activedirectorymanagedidentity', 'activedirectorymsi'].includes(authentication) ||
      ['', 'sqlpassword'].includes(authentication) && supplied(values.get('userid')) && supplied(values.get('password'));
    return /^(?:tcp:)?[a-z0-9][a-z0-9-]{0,61}[a-z0-9]\.database\.windows\.net(?:,1433)?$/i.test(values.get('server') ?? '') &&
      supplied(values.get('database')) && /^(?:true|strict|mandatory)$/i.test(values.get('encrypt') ?? '') &&
      /^false$/i.test(values.get('trustservercertificate') ?? '') &&
      (!values.has('integratedsecurity') || /^(?:false|no)$/i.test(values.get('integratedsecurity'))) && authenticated &&
      !['attachdbfilename', 'userinstance', 'failoverpartner', 'hostnameincertificate', 'servercertificate'].some(key => values.has(key));
  } catch { return false; }
}

function validateAzureConfiguration(env = {}) {
  const errors = [];
  const reject = (code, setting, message) => errors.push({ code, setting, message });
  const requireSetting = key => {
    if (!supplied(env[key])) { reject('MISSING_CONFIGURATION', key, 'A nonblank configured value is required.'); return false; }
    return true;
  };
  const check = (key, valid, message) => { if (requireSetting(key) && !valid(env[key])) reject('INVALID_CONFIGURATION', key, message); };
  check('GITHUB_REF', value => value === 'refs/heads/main', 'Production deployment requires the main branch.');
  check('GITHUB_EVENT_NAME', value => value === 'workflow_dispatch', 'Production deployment requires an explicit workflow dispatch.');
  check('GITHUB_SHA', value => /^[0-9a-f]{40}$/.test(value) && !/^0+$/.test(value), 'An exact source commit is required.');
  check('AZURE_RESOURCE_GROUP', value => /^[A-Za-z0-9][A-Za-z0-9_.()-]{0,89}$/.test(value) && !value.endsWith('.'), 'Supply a resource group name without whitespace or shell syntax.');
  check('AZURE_REGISTRY_NAME', value => /^[a-z0-9]{5,50}$/.test(value), 'Supply an Azure Container Registry name.');
  check('AZURE_REGISTRY_LOGIN_SERVER', value => /^[a-z0-9]{5,50}\.azurecr\.io$/.test(value) && value === `${env.AZURE_REGISTRY_NAME}.azurecr.io`, 'The public Azure registry host must match the registry name.');
  check('AZURE_API_CONTAINER_APP', value => /^[a-z][a-z0-9-]{0,30}[a-z0-9]$/.test(value) && !value.includes('--'), 'Supply an Azure Container App name.');
  check('WEB_APP_ORIGIN', origin, 'Supply the exact public HTTPS web origin, without a path, query, credentials or trailing slash.');
  check('PRODUCTION_API_URL', origin, 'Supply the exact public HTTPS API origin, without a path, query, credentials or trailing slash.');
  if (origin(env.WEB_APP_ORIGIN) && env.WEB_APP_ORIGIN === env.PRODUCTION_API_URL)
    reject('INVALID_CONFIGURATION', 'PRODUCTION_API_URL', 'The current deployment requires distinct web and API origins.');
  check('AZURE_CREDENTIALS', value => {
    try { const credentials = JSON.parse(value); return credentials && !Array.isArray(credentials) &&
      ['clientId', 'tenantId', 'subscriptionId'].every(key => guid.test(credentials[key] ?? '') && !/^0{8}-/.test(credentials[key])) && supplied(credentials.clientSecret); }
    catch { return false; }
  }, 'Supply the Azure login credential object with configured client, tenant, subscription and secret fields.');
  requireSetting('AZURE_STATIC_WEB_APPS_API_TOKEN');
  check('AZURE_SQL_CONNECTION_STRING', secureAzureSql, 'Supply an Azure SQL connection with an explicit database, encryption and certificate validation; ambiguous security settings are rejected.');
  check('AZURE_MEDIA_SERVICE_URI', value => /^https:\/\/[a-z0-9]{3,24}\.blob\.core\.windows\.net$/.test(value), 'Supply a public Azure Blob HTTPS service origin without credentials, query or path.');
  check('AZURE_MEDIA_CONTAINER', value => /^[a-z0-9](?:[a-z0-9-]{1,61})[a-z0-9]$/.test(value) && !value.includes('--'), 'Supply the private media container name.');
  check('AZURE_KEY_RING_DIRECTORY', value => privatePath(value), 'Supply the intended private shared mount directory under /mnt; a path does not prove that it is provisioned.');
  check('AZURE_KEY_RING_CERTIFICATE_PATH', value => privatePath(value, true), 'Supply the intended separately provisioned certificate path under /run/secrets.');
  requireSetting('AZURE_KEY_RING_CERTIFICATE_PASSWORD');
  for (const key of ['AZURE_MIGRATION_ARTIFACT_SHA256', 'AZURE_SQL_RESTORE_EVIDENCE_SHA256', 'AZURE_ROLLBACK_IMAGE_SHA256'])
    check(key, value => sha256.test(value) && !/^0+$/.test(value), 'Supply the immutable artifact or evidence SHA256; this is a reference, not operational acceptance.');
  // Bootstrap remains optional. A partially configured account is a deployment
  // error rather than an apparently successful skipped bootstrap.
  const bootstrap = ['PLATFORM_OWNER_EMAIL', 'PLATFORM_OWNER_NAME', 'PLATFORM_OWNER_PASSWORD'];
  if (bootstrap.some(key => env[key] !== undefined && env[key] !== '')) {
    check('PLATFORM_OWNER_EMAIL', value => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value), 'Supply a valid initial owner email.');
    requireSetting('PLATFORM_OWNER_NAME'); requireSetting('PLATFORM_OWNER_PASSWORD');
  }
  return { ok: errors.length === 0, errors };
}
function checkAzurePreflight(env = {}) {
  const configuration = validateAzureConfiguration(env);
  return { ok: false, configurationValid: configuration.ok, errors: [...configuration.errors, ...implementationBlockers] };
}
if (require.main === module) {
  // Output only code-owned descriptions and setting names, never input values,
  // parsed credentials, SQL details, paths or thrown loader messages.
  const result = checkAzurePreflight(process.env);
  console.error('Azure deployment BLOCKED by preflight:');
  for (const error of result.errors) console.error(`- ${error.code} [${error.setting}]: ${error.message}`);
  process.exitCode = 1;
}
module.exports = { validateAzureConfiguration, checkAzurePreflight, implementationBlockers };
