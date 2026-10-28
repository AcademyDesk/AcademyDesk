# Verification before production deployment — 2026-10-28

MODEL: Session Codex model; no override or delegation.
TOOL: Codex managed-cloud terminal.
REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/release-export-security; base0bceb439b95485297e2c05e8bc8cf9a4a19f572e.
ENVIRONMENT: Cloud local checks; prepared GitHub-hosted CI, remote result pending.
TASK: Enforce existing verification/P0 requirements before Azure mutations and
connect the existing guarded SQL/private-media fixtures to ordinary CI.
MAY MODIFY: Workflows, QA runners/guards/tests, release records and handoff.
MUST NOT MODIFY: Customer records, production configuration, accepted issue
status/evidence without execution, application financial/security rules.
VALIDATE: Workflow lint, PowerShell/shell syntax/preflight, fail-closed gate tests,
synthetic container-script simulations, API/SQL-harness build and frontend tests.
STOP WHEN: Prepared workflow pushed, local results/remote limits accurately recorded.
NEXT ROUTE: Codex for actual CI result interpretation and remaining critical gates.

## Implemented verification path

`verify.yml` uses a fresh Ubuntu runner, read-only repository permissions and
checkout credentials that are not persisted. It installs the tested .NET10.0.403
and Node24.19.0 versions, rejects dependency audit failures, runs the full API suite
and frontend type/lint checks, then builds the existing isolated SQL harness.

Actual TSX tests generate the24 payload fixtures used by the communication SQL
module. Eight existing guarded modules run on separate owned loopback SQL
containers: reconciliation, adjustments, payroll, transitions/restoration,
consumers, finance access, class-file access and communication roundtrip. Blob
checks use an owned private Azurite emulator. Real communication output supplies
the previously missing SQL fixture for the full frontend suite. No missing test
is skipped, no synthetic replacement SQL log is written, and no customer DB or
Azure credentials are needed by verification.

The wrappers accept an explicit compiled SqlHarness.dll, validate it before
provisioning, preserve existing command modes and ownership/cleanup guards, and
support Linux disk-space preflight for Blob runs. The added restoration batch
waits one120-request limiter window after the preceding HTTP suite; no limiter
configuration or forwarded-header bypass was introduced.

The workflow then builds the web in tested webpack mode and builds the API image.
A new startup/failure smoke uses only that locally built image (`--pull=never`),
the explicit local Docker socket and a UUID-labelled loopback container. Its
unavailable SQL target is container-local127.0.0.1:1, with no credentials/bootstrap,
migrations or live media enabled. Expected health503 and anonymous academy401
check failure/auth behavior. This is separate from actual SQL-backed HTTP checks;
it is not a healthy production DB or full Docker deployment claim.

Artifacts include source SHA, API TRX, filtered SQL/frontend logs and image
digests, retained seven days. Upload paths are explicit and include only these
synthetic execution outputs, not environment/credential files.

## Deployment enforcement

The existing manual Azure workflow now needs verification, runs deployment only
from main, and checks critical acceptance before Azure login/mutation. The P0
guard checks the five known critical issues even if removed from the open index,
includes newly registered P0s, rejects malformed links/missing/duplicate fields,
and requires exact CLOSED/PASS status values. It does not silently close issues.
Current records correctly return BLOCKED for all five P0s. This guard supplements
the remaining P1/staging/browser/restore requirements; it does not replace them.
Deployment also uses the validated webpack build mode. Existing startup migration
and Azure configuration logic are unchanged and still need operational acceptance.

## Local validation and limits

- Pinned actionlint1.7.7 validates both workflows. Official release checksum
  verified before local use. The command named go here is a different program;
  its install attempt failed, so the published binary was used.
- PowerShell7.6.0 installed under workspace tooling; both scripts parse. Missing
  harness paths reject before any Docker call. Windows execution not rerun.
- Fourteen production-gate fixture tests PASS. Actual repository invocation
  correctly exits1 for all five open P0s; no acceptance status was changed.
- Shell syntax and five command-stub container scenarios PASS: expected statuses,
  failed health/auth, non-loopback refusal and ownership-safe cleanup. Remote
  Docker selectors are cleared and image pulls prohibited. **No actual API
  container was started by those simulations.**
- CI restore/audit command PASS. SQL harness build PASS/zero warnings/errors.
  API1,108 PASS retained from the preceding policy repair; no API source changes
  in this workflow increment. Route type generation and TypeScript PASS.
- Fresh actual TSX communication fixture generation PASS:24 distinct payloads.
  No real SQL log generated here. Full current frontend:48 files,1,112 tests,
  **1,109 PASS/three FAIL**, zero skipped/cancelled, actual exit1 retained for
  the missing original SQL fixture. Previous lint/build retained; app source
  unchanged in this increment.
- Runtime still lacks saved CDN/GitHub API domains. `gh api` was Forbidden;
  no workflow run/result, real SQL/migration/private Blob pass or image startup
  pass is claimed. After this push, inspect the actual Actions run once available.
- No workflow dispatch, Azure login/deployment, main merge, customer record or
  environment publication. Feature-branch pushes remain user-authorized.

The user must review/save the confirmed network draft and publish in environment
settings to activate its exact domains. Until applied, the current cloud cannot
perform the blocked downloads or inspect GitHub API results. Remaining P0/P1,
concurrency, full role/tenant/browser/device, restore and release acceptance
continue under the original QA program.
