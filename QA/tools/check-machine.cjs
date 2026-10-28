#!/usr/bin/env node
'use strict';

// Read-only inventory. Never reads credential/configuration files or starts services.
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawnSync } = require('node:child_process');

function command(file, args, options = {}) {
  const result = spawnSync(file, args, {
    encoding: 'utf8', timeout: 5000, maxBuffer: 1024 * 1024,
    windowsHide: true, shell: false, ...options,
  });
  // Errors can contain paths or configuration; return a status, never raw errors.
  if (result.error || result.status !== 0) return null;
  return result.stdout.trim();
}

function version(file, args) {
  const value = command(file, args);
  return value?.match(/\b\d+\.\d+(?:\.\d+)?(?:[-+][\w.-]+)?\b/)?.[0] ?? null;
}

function fileMetadata(filename) {
  try {
    const stat = fs.statSync(filename);
    return stat.isFile() ? { present: true, bytes: stat.size } : { present: false };
  } catch { return { present: false }; }
}

function repository(root) {
  const git = (...args) => command('git', ['--no-optional-locks', '-C', root, ...args]);
  const top = git('rev-parse', '--show-toplevel');
  let exactRoot = false;
  try {
    const normalize = value => process.platform === 'win32' ? value.toLowerCase() : value;
    exactRoot = top !== null && normalize(fs.realpathSync(top)) === normalize(fs.realpathSync(root));
  } catch { /* Missing directory, or Git could not inspect it. */ }
  if (!exactRoot) return { available: false };
  const status = git('status', '--porcelain', '--untracked-files=normal');
  const head = git('rev-parse', 'HEAD');
  return {
    available: true,
    branch: git('symbolic-ref', '--short', '-q', 'HEAD'),
    head: /^[a-f0-9]{40,64}$/.test(head ?? '') ? head : null,
    dirty: status === null ? null : status.length > 0,
    statusEntries: status === null ? null : status.split('\n').filter(Boolean).length,
  };
}

function dockerReadiness() {
  // Select a local socket explicitly; never follow saved or inherited remote contexts.
  const env = { ...process.env };
  for (const key of Object.keys(env)) {
    if (['DOCKER_HOST', 'DOCKER_CONTEXT', 'DOCKER_TLS', 'DOCKER_TLS_VERIFY', 'DOCKER_CERT_PATH']
      .includes(key.toUpperCase())) delete env[key];
  }
  const macSocket = path.join(os.homedir(), '.docker', 'run', 'docker.sock');
  const host = process.platform === 'win32' ? 'npipe:////./pipe/docker_engine'
    : `unix://${process.platform === 'darwin' && fs.existsSync(macSocket) ? macSocket : '/var/run/docker.sock'}`;
  const docker = (...args) => command('docker', ['--host', host, ...args], { env });
  const raw = docker('version', '--format', '{{json .Server}}');
  let server;
  try { server = JSON.parse(raw); } catch { /* Not installed, stopped or inaccessible. */ }
  if (!server?.Version) return { localDaemonReachable: false };
  const images = docker('image', 'ls', '--format', '{{.Repository}}:{{.Tag}}', 'mcr.microsoft.com/mssql/server');
  return {
    localDaemonReachable: true,
    serverVersion: server.Version,
    serverOS: server.Os ?? null,
    serverArch: server.Arch ?? null,
    composeVersion: version('docker', ['--host', host, 'compose', 'version', '--short']),
    sqlServer2022Cached: images === null ? null : images.split('\n').includes('mcr.microsoft.com/mssql/server:2022-latest'),
  };
}

function main(args) {
  const roots = { academy: path.resolve(__dirname, '../..') };
  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--help') {
      process.stdout.write('node check-machine.cjs [--academy PATH] [--penta PATH]\nRead-only; prints JSON to stdout; performs no network requests.\n');
      return;
    }
    if (!['--academy', '--penta'].includes(args[i]) || !args[i + 1] || args[i + 1].startsWith('--')) {
      process.stderr.write('Expected --academy PATH or --penta PATH. Use --help.\n');
      process.exitCode = 2;
      return;
    }
    roots[args[i].slice(2)] = path.resolve(args[++i]);
  }
  roots.penta ??= path.resolve(roots.academy, '../PentaMini');
  const report = {
    schemaVersion: 1,
    observedAt: new Date().toISOString(),
    execution: {
      platform: process.platform, arch: process.arch,
      managedCloudPolicyFilePresent: fs.existsSync('/etc/codex/network-policy.json'),
      logicalCPUs: os.cpus().length,
      totalMemoryGiB: Math.round(os.totalmem() / 1024 ** 3),
    },
    tools: {
      node: process.versions.node,
      git: version('git', ['--version']),
      dotnet: version('dotnet', ['--version']),
      python: version('python', ['--version']) ?? version('python3', ['--version']),
    },
    repositories: { academy: repository(roots.academy), penta: repository(roots.penta) },
    docker: dockerReadiness(),
    artifacts: {
      communicationRoundtripSqlLog: fileMetadata(path.join(roots.academy, 'QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log')),
      penta08bModel: fileMetadata(path.join(roots.penta, 'models/Qwen3.5-0.8B-Q4_0.gguf')),
      penta2bModel: fileMetadata(path.join(roots.penta, 'models/Qwen3.5-2B-Q4_0.gguf')),
    },
    limitations: [
      'Presence and size do not verify evidence provenance or model checksums.',
      'No database connections, Docker pulls, installs, fetches, services or file writes were performed.',
      'Unavailable tools/daemon may reflect PATH, permission, timeout or a stopped service.',
    ],
  };
  process.stdout.write(`${JSON.stringify(report, null, 2)}\n`);
}

if (require.main === module) main(process.argv.slice(2));
