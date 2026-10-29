# Access-review sign-off provenance and audit boundary — 25 October 2026

## Qualified backend return — 26 October 2026

- [x]Fresh offline API `3368e36621f942bfac353b8ac92824da`, build window
  `c79968927c2a4ab085830a58e7075220` PASS2995/2995 API tests/zero skips.
  Only AccessReviewsController differs from the preceding accepted API source.
- [x]Actual unstarted-image payload `7022ffcac189401fa37c783c834c0c97`
  verifies all65published files. Local offline-publish image, not release build.
- [x]SQL `23c4ffbcb9054108a85c6505a62f3b2f`, runtime window
  `ac30bf34d42c42ada728194d54877620` PASS49ordinary HTTP checks.
  Five normal/recovery sign-offs use authoritative server identity, reject
  spoofed identity fields, and correlate each review/optional task/two audits.
  Final disposable rows:five reviews,three linked tasks,ten success audits.
- [x]Four deliberate500controls: ordinary generic audit, platform generic
  audit, domain audit and follow-up insertion. All13table snapshots unchanged;
  no review/task/success audit survives failure. Overlong follow-up400and
  role/tenant/module/anonymous refusals remain no-write. All faults removed.
- [x]Exact original service IDs/images/health and full Docker inventory restored;
  owned SQL resources removed, lease released. Independent source/build/payload/
  baseline/native/window qualification PASS via
  `node QA/tools/review-provenance-qualification.cjs 23c4ffbcb9054108a85c6505a62f3b2f ac30bf34d42c42ada728194d54877620`.

SQL receipt SHA256 `def750da5826059ef27a34d9431b2cde07400febe2813ff1d52dac59c2e20a59`;
window SHA256 `b132f6099ad7cc3a3cadf7b7771bdfea206b88b7510c8269d5e2d436900b8c20`.
Earlier running/pending descriptions below are retained checkpoint history.
Current sign-off browser, concurrent separate sign-offs, physical devices,
standard release-image/full AI/enterprise gates remain OPEN. Frontend receipt/
recovery continuation is tracked separately; no issue closure or deployment.

Existing BUG-DATA-0013 and BUG-API-0002 continuation. Source branch
`codex/penta-search`, worktree `D:\AcademyDesk-codex-p0`. No main/Mini/GitHub/
Azure/customer data changes. Preserve all earlier qualified application work.

## Baseline and scope

- [x]Thirteen actual-controller/EF and action-boundary cases reproduce missing
  reviewer/task/audit/receipt protection:0PASS/13FAIL in
  `QA/EVIDENCE/access-review-provenance-baseline-20261025/access-review-provenance-baseline.trx`.
  Direct-controller actor tests do not execute MVC's global authorization filter
  and are not evidence that anonymous/foreign HTTP requests can bypass it.
- [x]Corrected native baseline window `39a3d8718f764732af258c87af9be85e` /
  SQL `90c6a162794c44b0b100f1569ab612fc` PASS46ordinary HTTP checks and
  diagnostic reproduction on accepted old API `52c9676e11d24cf9863a123d178021a1`:
  three normal sign-offs lack reviewer/task correlation; generic audit fault
  returns500for ordinary Owner but leaves review/task saved. Existing platform
  bypass returns200without required audit. Five reviews/four tasks/two generic
  audits remain in the disposable baseline; no protected acceptance is claimed.
  Thirteen table snapshots preserve unrelated state. Owned cleanup/exact original
  services/full inventory PASS; no source mutation during baseline validation.
- [x]First native baseline `5e2bba9321f04bd8b9fa17be63135dc9`/window
  `a3e3ca5daaa14446900dbb52f97565d7` FAIL after37ordinary HTTP checks:
  three normal200sign-offs lack provenance/task links; platform path has no
  audit. Actual anonymous401/foreign403/Teacher403/Student403 remain enforced.
  Fault setup stopped at SQL111: CREATE TRIGGER must start its batch. Fixed QA
  batching with sqlcmd GO, no application/assertion change. Exact originals/
  full inventory/owned cleanup PASS. This is not accepted audit-fault proof.

The disposable fault trigger targets the existing generic POST AccessReviews
audit separately from the new correlated domain event, so a later generic audit
failure must roll back both the review and linked task. Trigger creation follows
[Microsoft SQL Server CREATE TRIGGER documentation](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-trigger-transact-sql?view=sql-server-ver17).
Triggers/temporary DENY exist only inside this run's ownership-verified SQL DB.

## Intended bounded repair and acceptance

- [x]Resolve reviewer from the existing server UserManager/current account,
  never request/model actor or academy fields; retain global role/tenant policy.
- [x]Persist exact review→optional follow-up→correlated domain audit IDs and
  return an authoritative receipt. Keep the existing follow-up unassigned; no
  automatic staffing policy or role grant is introduced.
- [x]Use existing action-level domain transaction, including the already-allowed
  platform route, to encompass both domain and generic success audits. No
  Identity writes/enlistment, new model tool or security configuration endpoint.
- [x]Preserve optional notes; reject overlong follow-up descriptions before
  saving rather than failing after a partial result. No schema/migration needed.
- [ ]Full API suite/fresh offline image/actual payload; native ordinary/spoof/
  tenant/role/module refusals and domain/generic/task-insert fault rollbacks,
  then native recovery/exact correlations and source-bound qualification.
  Build window `c79968927c2a4ab085830a58e7075220` running the full suite and
  existing cached offline-publish image path. This is not a standard release
  source-Dockerfile acceptance. No frontend source change in this packet.

Full suite in that build window now PASS2995/2995/zero skipped, including all13
new cases. One earlier xUnit assertion-style warning in the baseline was corrected
using the equivalent Assert.Single predicate overload; no assertion removed.
The new unit fixture uses the real EF-backed Identity UserManager, not model or
client-provided actor data. Image/native acceptance is still pending.

Separate-signoff concurrency, current live sign-off browser/physical devices,
all-portal, full AI and standard source-Dockerfile/release gates remain OPEN.
No issue closure from unit tests or these slices. Recommended route:Sol High
for this identity/audit/SQL integration; Medium for later settled receipt UI.
