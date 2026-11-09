// Read-only issue reconciliation. Writes derived reports only with --write.
// Never changes a status/priority or infers acceptance from a PASS mentioned in prose.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..'), dir = path.join(root, 'QA/ISSUES');
const prefix = path.join(root, 'QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08');
const hash = data => crypto.createHash('sha256').update(data).digest('hex');
const field = (s, name) => s.match(new RegExp('^\\|\\s*' + name + '\\s*\\|\\s*([^|]*)\\|', 'mi'))?.[1].trim() || '';
const plain = s => s.replace(/[`*]/g, '').replace(/\s+/g, ' ').trim();
const cell = s => plain(s).replaceAll('|', '\\|');
const files = fs.readdirSync(dir).filter(name => /^BUG-.+\.md$/.test(name)).sort();
const index = fs.readFileSync(path.join(dir, 'INDEX.md'), 'utf8');
const listed = [...index.matchAll(/\[(BUG-[A-Z0-9-]+)\]\(\1\.md\)/g)].map(m => m[1]);
const inputErrors = [];
for (const id of new Set(listed)) if (!files.includes(id + '.md')) inputErrors.push('Missing issue file: ' + id);
for (const file of files) if (!listed.includes(file.slice(0, -3))) inputErrors.push('Issue not in register: ' + file);
if (new Set(listed).size !== listed.length) inputErrors.push('Duplicate issue IDs in register');
const records = files.map(file => {
  const source = fs.readFileSync(path.join(dir, file), 'utf8'), id = file.slice(0, -3);
  const rawPriority = field(source, 'Priority'), priority = rawPriority.match(/^P[0-3]\b/)?.[0] || 'UNKNOWN';
  if (priority === 'UNKNOWN') inputErrors.push('Unrecognized priority: ' + id);
  const status = field(source, 'Status'), verification = field(source, 'Final verification');
  if (!status) inputErrors.push('Missing status: ' + id);
  const headings = [...source.matchAll(/^## (.+)$/gm)];
  const links = [...new Set([...source.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)].map(m => m[1]))]
    .filter(href => /REPORTS\/|HANDOFF\/|actions\/runs\//.test(href)).map(href => {
      if (/^https?:\/\//.test(href)) return { href, localExists: null, observation: 'Remote link recorded, not probed' };
      const target = path.resolve(dir, href.split('#')[0]);
      return { href: path.relative(path.dirname(prefix), target).split(path.sep).join('/'), localExists: fs.existsSync(target), observation: 'Reference presence only, not evidence acceptance' };
    });
  const pending = source.split('\n').filter(line => /pending|remain.{0,20}open|not run|still.{0,20}open|required.{0,20}(?:critical|device|acceptance)/i.test(line)).slice(-5).map(line => plain(line).slice(0, 360));
  return { id, title: plain(source.split('\n')[0].replace(/^#\s*/, '')), priority, rawPriority,
    additionalReleaseGate: rawPriority !== priority ? rawPriority : null, status, finalVerification: verification,
    recordedClosedAndPass: status.toUpperCase() === 'CLOSED' && verification.toUpperCase() === 'PASS',
    confirmation: field(source, 'Confirmation status'), module: field(source, 'Module'), route: field(source, 'Screen / route'),
    recordedSource: field(source, 'Source'), recordedFix: field(source, 'Fix commit'), recordedRegression: field(source, 'Regression result'),
    recordedClosureNotes: field(source, 'Closure notes'), latestSection: headings.at(-1)?.[1] || null,
    recordedPendingExcerpts: pending, evidenceReferences: links,
    historicalMetadataNeedsReview: /not implemented|phase 1|not run/i.test(field(source, 'Fix commit')) && /\bPASS\b|implemented|repair/i.test(source.slice(headings[0]?.index || source.length)),
    issueSha256: hash(source) };
}).sort((a, b) => a.priority.localeCompare(b.priority) || a.id.localeCompare(b.id));
const counts = Object.fromEntries(['P0', 'P1', 'P2', 'P3', 'UNKNOWN'].map(p => [p, records.filter(r => r.priority === p).length]));
const output = { sourceBase: 'b6793d45788b74d8cd5ab82e246c483da8df4aec',
  inheritedDesktopCommit: '9c9658132446124b70c53467e940d9769cf97d44',
  scope: 'All issue files reconciled with INDEX. Metadata and reference inventory only; no inferred repair, execution, closure or production acceptance.',
  closureRule: 'Original reproduction → cause → fix commit → original/regression retest → module suite → critical regression → documented acceptance; device/release gates where required.',
  totalRecords: records.length, counts, recordedClosedAndPass: records.filter(r => r.recordedClosedAndPass).length,
  historicalMetadataNeedingReview: records.filter(r => r.historicalMetadataNeedsReview).length,
  inputErrors, indexSha256: hash(index), generatorSha256: hash(fs.readFileSync(__filename)), records };
let md = '# All-priority issue acceptance inventory — 8 November 2026\n\n';
md += `${output.totalRecords} issue files: ${counts.P0} P0, ${counts.P1} P1, ${counts.P2} P2, ${counts.P3} P3. ${output.recordedClosedAndPass} have both literal CLOSED and PASS markers. ${output.historicalMetadataNeedingReview} have old header metadata that needs reconciliation with appended scoped evidence.\n\n`;
md += 'This is not a list of untouched bugs or an acceptance verdict. Existing fixes and source-bound SQL/browser results are retained. Quoted pending statements may be historical; use the exact source and linked reports to determine the final retest. No issue/priority/exception is changed by this generator. A P1 record carrying an additional P0 session gate keeps that note.\n\n';
md += '**Closure:** ' + output.closureRule + '\n\n';
md += '**Remaining P0 acceptance:** finance/reminder/family read agreement and linked critical/device scenarios (DATA-0001/0002); positive-net/payroll identity/history/print and critical/device scenarios (DATA-0003); audited void/evidence restore, linked critical/device scenarios (DATA-0010); complete private-media authorization/recovery and actual-device capture/upload/view/download (SEC-0001). Current-source CI/images/security and staging restore/rollback remain release gates. Source fixes or synthetic browser runs do not replace those results.\n\n';
md += '**Additional mandatory session gate:** BUG-AUTH-REVOCATION-001 remains P1 in the issue register and an explicit P0 release scenario. Its own broader SECURITY-SESSION-001 final acceptance is required even if all five P0 records pass; the inherited production guard enforces all six records.\n\n';
md += '**Acceptance order / owners:** Desktop first integrates the scoped family/reminder reader return and verifies actual SQL plus role/tenant/guardian revocation and no-write readback; complete the remaining five P0 and broad-session critical cases next. Desktop owns actual microphone/mobile/tablet acceptance, SQL migration/backup/restore and staging recovery. Cloud owns bounded source repairs, current combined automated/browser checks and this full-register reconciliation. The release integrator must obtain exact-release CI/image/security evidence and source-bound documented acceptance before marking any issue closed. Model qualification and 90% eligible conversational outcomes remain unaccepted; the stop before model loading stays in force.\n\n';
md += '| Issue | Priority | Recorded status | Latest evidence section | Recorded pending / closure pointer |\n| --- | --- | --- | --- | --- |\n';
for (const r of records) md += `| [${r.id}](../ISSUES/${r.id}.md) | ${cell(r.rawPriority)} | ${cell(r.status)} | ${cell(r.latestSection || r.confirmation)} | ${cell(r.recordedPendingExcerpts.at(-1) || r.recordedClosureNotes || 'Acceptance not established by this inventory')} |\n`;
md += '\nThe JSON companion includes every source hash, original metadata, all report references, presence checks and historical pending excerpts. A reference existing is not proof its tests passed; missing Cloud raw evidence is not proof Desktop never ran it. The production guard and source-bound acceptance requirements remain authoritative.\n';
if (inputErrors.length) md += '\n**Inventory input errors:**\n\n' + inputErrors.map(e => '- ' + e).join('\n') + '\n';
if (process.argv.includes('--write')) { fs.writeFileSync(prefix + '.json', JSON.stringify(output, null, 2) + '\n'); fs.writeFileSync(prefix + '.md', md); }
console.log(JSON.stringify({ totalRecords: output.totalRecords, counts, recordedClosedAndPass: output.recordedClosedAndPass, historicalMetadataNeedingReview: output.historicalMetadataNeedingReview, inputErrors }, null, 2));
if (inputErrors.length) process.exitCode = 1;
