'use strict';
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const roles = ['owner', 'academyadmin', 'teacher', 'student', 'guardian', 'platformowner'];

// Credentials are private per-run files, never source-controlled configuration.
function demoPassword(active, role) {
  a.ok(roles.includes(role), 'Unknown synthetic role.');
  if (active.credentialFile) {
    a.equal(path.resolve(active.credentialFile), path.join(path.resolve(active.folder), 'credentials.json'));
    const saved = JSON.parse(fs.readFileSync(active.credentialFile, 'utf8'));
    a.equal(saved.schema, 'academydesk.synthetic-credentials.v1'); a.equal(saved.run, active.run);
    const rows = saved.accounts.filter(x => x.role === role && x.email === role + '@demo.example.invalid');
    a.equal(rows.length, 1); a.equal(rows[0].status, 'verified');
    a.ok(typeof rows[0].password === 'string' && rows[0].password.length >= 16);
    return rows[0].password;
  }
  // A new disposable seed may still use its original generated shared password.
  a.equal(path.resolve(active.loginFile), path.join(path.resolve(active.folder), 'LOGIN.txt'));
  const value = fs.readFileSync(active.loginFile, 'utf8').match(/Password for all six demo accounts: (.+)/)?.[1];
  a.ok(value, 'Private synthetic credentials unavailable.'); return value;
}
module.exports = { demoPassword };
