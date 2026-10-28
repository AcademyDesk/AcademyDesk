'use strict';
const fs = require('node:fs');
const path = require('node:path');

// Existing release policy: no open or unverified P0 may reach production.
// Keep known critical issues checked even if removed from the open register.
const knownCritical = ['BUG-DATA-0001', 'BUG-DATA-0002', 'BUG-DATA-0003', 'BUG-DATA-0010', 'BUG-SEC-0001'];
function checkProductionGates(root = path.resolve(__dirname, '../..')) {
  const failures = [], ids = new Set(knownCritical);
  const index = fs.readFileSync(path.join(root, 'QA/ISSUES/INDEX.md'), 'utf8');
  const listed = new Set();
  for (const line of index.split('\n')) {
    const cells = line.split('|').slice(1, -1).map(x => x.trim());
    if (cells[2] !== 'P0') continue;
    const match = cells[0]?.match(/^\[(BUG-[A-Z]+-\d{4})\]\(\1\.md\)$/);
    if (!match) { failures.push('Malformed critical issue link in register.'); continue; }
    if (listed.has(match[1])) failures.push(`Duplicate critical issue: ${match[1]}`);
    listed.add(match[1]); ids.add(match[1]);
  }
  for (const id of ids) {
    let source;
    try { source = fs.readFileSync(path.join(root, 'QA/ISSUES', `${id}.md`), 'utf8'); }
    catch { failures.push(`${id}: issue record is missing.`); continue; }
    const field = name => [...source.matchAll(new RegExp(`^\\|\\s*${name}\\s*\\|\\s*([^|]+)\\|\\s*$`, 'gm'))];
    const status = field('Status'), verification = field('Final verification');
    if (status.length !== 1 || status[0][1].trim().toUpperCase() !== 'CLOSED')
      failures.push(`${id}: status must be CLOSED after acceptance.`);
    const result = verification.length === 1 ? verification[0][1].trim() : '';
    if (result.toUpperCase() !== 'PASS')
      failures.push(`${id}: final verification must PASS with no pending critical evidence.`);
  }
  return { ok: failures.length === 0, checked: [...ids], failures };
}

if (require.main === module) {
  try {
    const result = checkProductionGates();
    if (!result.ok) {
      console.error('Production BLOCKED by existing critical release policy:\n' + result.failures.map(x => `- ${x}`).join('\n'));
      process.exitCode = 1;
    } else console.log(`Critical issue acceptance PASS: ${result.checked.length} P0 records closed/verified. CI, staging and operational gates also remain required.`);
  } catch {
    console.error('Production BLOCKED: critical issue records could not be validated.');
    process.exitCode = 1;
  }
}
module.exports = { checkProductionGates, knownCritical };
