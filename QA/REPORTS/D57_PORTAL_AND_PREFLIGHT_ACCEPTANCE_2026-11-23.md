# D57 portal and preflight acceptance — 23 November 2026

## Sources and ownership

Started on2e8e12c36c0252d339c78e8a83f794f954bd9c80 after fetching current tips.
Imported only the three completed C56 paths from
ba4b261e3e5a7f7cd7e5026a8cc979c5156afe32. No wholesale branch replay, C55/C54
re-import, main modification or replacement of newer D56 work. Published this
reviewed preflight and the bounded follow-up ownership at
fef8bdff6120ef018bd40ea09815b2d6519b119c.

Cloud's active C57 claim49bec84a6ec54e4d6c256cb40d0c26fff658b709 owns four
inactive private-transport paths. None is edited here. Desktop's Guardian query,
native regression, CI module and current demo work are separately claimed in
D57_DESKTOP_CHECKS_2026-11-23.md. C56's source-bound handoff remains unchanged.
At final fetch, C57 published3a0c37942b0445e52c4695d048c0b146ca61676e.
That completed inactive foundation is queued for separate review/integration;
it is not imported or represented as validated/activated by this packet.

## C56 review and real boundary checks

- Imported69C56 tests plus15existing production-guard tests passed84/84.
- Six additional boundary cases initially passed2/6, failed4/6. The original
  output is retained; these were actual preflight gaps rather than Azure access.
- A trailing slash on the key-ring directory could conceal a certificate located
  inside it. Normalize only its trailing separator before containment comparison.
- A supplied DOTNET_ENVIRONMENT could conflict with declared ASPNETCORE_ENVIRONMENT
  Production. The preflight now refuses non-Production bootstrap overrides.
  [Microsoft runtime precedence](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/7/environment-variable-precedence?view=aspnetcore-10.0)
  supports this source review; no host environment or production gate was changed.
- Final combined90/90passed, zero failures/skips. Existing C56 assertions, secret
  redaction, fail-closed critical guard and productionQualified=false remain.
- Current actual Desktop preflight exits1:18input failures and six unaccepted
  critical records. Missing local input declarations do not prove missing Azure
  resources. No Azure request or credential provisioning was attempted.

## Newly reproduced Guardian availability bug

Read-only synthetic six-role Identity/HTTP/SQL packet reached61passing assertions
then failed own Guardian children list. A direct independent request confirmed
login200, portal/me200 with the valid linked child, and children500. Fresh owned
SQL run748eb9d715544e0f9149ef18c5598f81 reproduced expected200/actual500 with
identical before/after data fingerprints; wrappera4293190 retained passed=false
and restored original resources/inventories/API200. No disclosure is asserted.

PortalController.GuardianChildren ordered by a constructed PortalChildSummary's
Name after projection. Move the same first-name/space/last-name ordering before
DTO construction so SQL can translate it. No permission, guardian/tenant/link/
revocation filter, response DTO, module gate, model tool or database schema change.
Do not turn a query failure into an empty authorized response.

New issueBUG-FUNC-0034 remains OPEN with bounded repair verification. The six
existing critical acceptance records are unchanged and OPEN.

## Fixed native verification

Fresh owned SQL runc91a50238e1c4343acaceba6bb7fd85d,
wrapper38d15c03eda14cbabe19916790cd188f: **58/58real Identity/HTTP/SQL checks pass**.
The retained55cases are unchanged; three added cases cover authorized children,
foreign guardian refusal and revoked child removal. All eight finance/document/
academic combinations, own learner, other actor/tenant, scoped invoices,
certificate path, current revocation and inactive-record controls remain enforced.
Every captured GET before/after fingerprint stays equal.86application and7Identity
migrations, runtime login, negative cleanup guard and owned cleanup pass.
Both exact original services/images and complete inventories restored; API200.

Both harness builds passed with zero warnings/errors. Focused Guardian access/
provisioning API tests passed36/36, zero failures/skips. Two early API admission
attempts refused the unchanged3GiB guard; no tests ran in those refusals. Stopping
only the original two services and releasing WSL filesystem cache admitted the
bounded test. Both original services/images and API200 were restored afterward.
No unrelated application was terminated, Docker restarted or resources pruned.

The existing GuardianFlags module is added once to ordinary CI, retaining all26
original modules and the mandatory paired Core job. YAML parses and list-diff
assertions confirm27modules with the old ordered list intact. This closes the
specific regression coverage gap that let the relational500 escape prior CI;
it does not claim the new hosted run has already passed.

## Demo preservation and current-source checks

Before its native window, preserved the prior invented fixture12994a6a with a
COPY_ONLY/CHECKSUM SQL backup, successful RESTORE VERIFYONLY and original private
key ring. Backup778,240bytes, SHA256
ea02a7a692d2dda58566617aa83c7ef0f5aafe59f463799c21c487425656c942.
Raw DB/keys remain local under.build-check/d57-20261123/preserved-demo-12994a6ab57b40f2b333f6efc554f2ba.
This is preservation, not an isolated restore/PITR acceptance claim. Its graceful
shutdown completed without the old stop-file race and restored original services.

Restored demoe13e72728f55494ab957e7e04d22182b uses the same cached authorized2B
packet and Mini4732116; no new candidate, package/weight download, training or
production activation. Web export/source remains unchanged. A first corrected-
app permission run passed64assertions before a test request omitted Teacher
calendar year/month and correctly got400. Retain that failed harness result;
fix the request and explicitly test the missing-period400 as its own control.
No application relaxation or429retry is introduced. Packets respect the real
120/minute limiter using a full-window cooldown between complete attempts.

The corrected read-only current-source packet passed69assertions/68HTTP requests,
zero429. All six roles use real Identity/session profiles. Owner/Admin finance
reads succeed; foreign tenant routes fail. Teacher/Student/Guardian cannot read
academy-wide invoices/payments/student lists or use Owner financial Mini.
Platform Owner aggregate access succeeds without becoming an academy Mini actor.
Student/Guardian own details and invoice downloads succeed; other learners,
mismatched invoices and foreign guardian IDs are refused. The repaired Guardian
list succeeds. Teacher workspace/calendar succeed with period validation and
family identity remains refused. Invoice/payment totals, adjustments, Mini turn
and session counts are unchanged. No model request or application write was made
by this packet. Final permission receipt SHA256:
f658487e72695c8e01437a71af51e48f6bb0ebaa0635a8cba0453425b9cec887.

All role checks are API/native SQL and static route checks, not fresh browser/
physical phone evidence. No further heavy local suite runs beside the demo.

The same fresh demo then passed13real-model/Identity/SQL smoke checks (first
pending-fee query29.422seconds) and11Core pause/outage/GET-only recovery/Manual
checks. Core availability, bindings, inference StartedAt and complete resource
inventories restored. Fourteen invented learners remain after two Manual saves.
Current credentials stay in the private LOGIN.txt under this run; the URL remains
http://127.0.0.1:3000/login. Expiry24November2026at06:04IST, or earlier on low RAM/
shutdown. This is the same authorized cached packet, not new tier qualification.
Final demo shutdown is intentionally unrun while it remains available.

## Evidence seals and remaining acceptance

| Private receipt/source | SHA256 |
| --- | --- |
| Before native resulta4293190/result.json | fe5e8f0ff3e2d0c6253b98b9880fbaad48f9d8763434dadfc38e894347e49efc |
| Fixed native result38d15c03/result.json | a426b0897f56a9468defe54991bb238b5635e63025056d02f4c8ad91133fe9bc |
| Combined preflight test log | e4d893ff98983b51c970937c696da1356bf238a69aeb7a6087f0d5f21fa9c777 |
| Guardian API result JSON | 9141f21b22db2f520ad88e39250936ba65f1e5a87408b333086cabca29caba2a |
| Fixed PortalController.cs working source | 24c96553c2185da3b290080079cbb52eda99bd0729531b6ea3e27d1449e48e1f |
| Expanded GuardianFlagsRegression.cs working source | 5bbe15f70623f756904a68985d4b76803202a613789ac4b97ce20bddf622d835 |
| Current demo verification.json | 7631b325d839aa02b8413d3b09d8cffc6933d94938c7f8a82160a089330741bf |
| Current demo recovery-69d1f7658286405ea2d1f04cd03dc962.json | e5fe47c1628591c0ad337943d9d697d5c54d46464ed4496b29187e41c17e9e8a |

Native results live under.build-check/penta-host/guardian-list-native-<fullrun>;
other logs/results are under.build-check/d57-20261123. No raw evidence is pushed.
Baseline full CI45155173334 on2e8e12c ordinary job completedSUCCESS, paired job
failed private source access. Later publication needs its own exact-SHA result.

Remaining production blockers retain D56's owners/conditions: dedicated read-only
Core Actions credential; genuine private Mini archive prerequisites/full suite;
qualified production private transport and eligible role/workflow outcomes;
six critical closure chains and actual physical devices; actual staging network,
TLS/key/storage/migration/restore/rollback/alerts. The90%target is still unmeasured;
Mini/Plus/Pro remain unqualified, Ultra deferred. No assertion weakened, new test
plan substituted, main merge, production DB change or Azure deployment.
