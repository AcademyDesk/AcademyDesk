// Read-only status reconciliation. Never regenerates issues, closes gates or writes files.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

function summarize(issueFiles, indexText) {
  const ids = new Set(), priorities = { P0: 0, P1: 0, P2: 0, P3: 0 };
  const issues = [];
  for (const { name, text } of issueFiles) {
    const id = /^#\s+(BUG-(?:[A-Z0-9]+-)+\d+)\b/m.exec(text)?.[1];
    const priority = /^\|\s*Priority\s*\|\s*(P[0-3])\b/m.exec(text)?.[1];
    const declaredStatus = /^\|\s*Status\s*\|([^\r\n]*?)\|/m.exec(text)?.[1]?.trim();
    if (!id || id + '.md' !== name || !priority || !declaredStatus)
      throw new Error('Unmapped issue identity/priority/status: ' + name);
    if (ids.has(id)) throw new Error('Duplicate issue ID: ' + id);
    ids.add(id); priorities[priority]++;
    issues.push({ id, priority, declared_status: declaredStatus,
      sha256: crypto.createHash('sha256').update(text).digest('hex') });
  }
  const indexed = new Set([...indexText.matchAll(/\[(BUG-(?:[A-Z0-9]+-)+\d+)\]\(/g)].map(match => match[1]));
  return {
    scope: 'issue records and index metadata only; not uncorrected defect count or acceptance',
    records: issues.length, unique_ids: ids.size, priorities,
    missing_from_index: issues.filter(row => !indexed.has(row.id)).map(row => row.id),
    index_without_issue: [...indexed].filter(id => !ids.has(id)).sort(),
    p0_ids: issues.filter(row => row.priority === 'P0').map(row => row.id).sort(),
    source_manifest_sha256: crypto.createHash('sha256').update(JSON.stringify(issues.sort((a,b) => a.id.localeCompare(b.id)))).digest('hex'),
    auto_closed: 0, production_approved: false,
  };
}

if (require.main === module) {
  if (process.argv.length !== 2) throw new Error('Read-only inventory takes no action flags');
  const root = path.resolve(__dirname, '../..'), directory = path.join(root, 'QA/ISSUES');
  const files = fs.readdirSync(directory).filter(name => /^BUG-.*\.md$/.test(name)).sort();
  const report = summarize(files.map(name => ({ name, text: fs.readFileSync(path.join(directory, name), 'utf8') })),
    fs.readFileSync(path.join(directory, 'INDEX.md'), 'utf8'));
  console.log(JSON.stringify(report, null, 2));
}
module.exports = { summarize };
