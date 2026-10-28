# Cloud frontend quality, form safety and dependency remediation

Date: 2026-10-28. Branch: `codex/release-export-security`.
Base checkpoint: `b374b35482fa07c3dba41e27f4a746d9efc3a62e`.
This increment remains local and uncommitted; the user requested that later pushes wait.

MODEL: Current Codex session
TOOL: Codex
ENVIRONMENT: Cloud development, synthetic frontend transport and controlled hooks
TASK: Repair configured lint failures and related state/form defects; audit dependencies
MAY MODIFY: Affected frontend components, dependency declarations/lock, tests and QA records
MUST NOT MODIFY: Backend business rules, migrations, deployment or customer data
NEXT ROUTE: Codex for guarded SQL/HTTP and critical security gates; browser/device review separately

## Behavior changes

- Cleared all configured frontend lint errors without disabling rules. React state
  that follows controlled selection now resets before committing the new view;
  async read effects use completion callbacks and reject superseded/unmounted responses.
- Fixed contact-preference drafts carrying unsaved consent/notes across contacts
  when neither has a stored preference. Identity includes academy, contact type,
  contact ID and the loaded preference. Existing preference defaults are preserved.
- The calendar preserves manual browsing until its controlled value changes;
  reopening refreshes the current-day marker. Theme output stays consistent with
  server hydration. Shell menus close on route changes without a second effect render.
- Activity age filtering and platform announcement expiry use a ticking clock,
  so time windows update without unrelated user interactions. The existing period
  definitions/time-zone policies were not redefined.
- Teacher attendance drafts reset on session/roster changes, and changing from a
  one-to-one class to another group clears the old learner focus. Teacher and
  Platform Owner access-denied recovery remains intact.
- Initial loads across academic periods, courses, batches, access review, students,
  platform services/controls, teacher workspace and work queue reject late updates.
  Selection-driven attendance, result, guardian-link and fee reads also reject
  superseded effect completions. This is not a claim that every save/read race in
  BUG-DATA-0018 is repaired; that broad issue remains open.
- Captured form elements before awaiting API replies in seven handlers: academic
  periods, access grants, access-review sign-off, work queue, support requests,
  teacher onboarding and student onboarding. Released React event targets no
  longer prevent their confirmed-save reset/navigation. Payload/business rules
  remain unchanged. Other reset sites and success-notice/readback issues remain open.

## Reproduction and checks

Contact-preference reproduction: 13 controlled TSX cases ran before repair:
10 passed and three failed. After repair all 13 passed.

Released-event-target reproduction: 35 controlled cases ran before the reset fix:
29 passed and six successful-submit cases failed. The completed suite now contains
37 passing cases, including 14 accepted/rejected submit checks across seven handlers,
request cancellation, reversed replies, tenant selection and teacher draft behavior.
Shared controls have eight additional passing checks, including real React server
rendering for hydration output. Controlled hooks are not browser acceptance evidence.

Final checks on the security-updated dependency tree:

| Command | Observed result |
| --- | --- |
| `npm run lint` | PASS: zero errors, 65 warnings; earlier baseline was 39 errors/77 warnings |
| `npm run build -- --webpack` | PASS: production static export on Next.js 16.4.3 |
| `npm audit --json` | Zero known vulnerabilities across frontend production and development dependencies |
| `npm test` | FAIL: 943 tests, 940 passed, three failed, zero skipped/cancelled; 43 current test files |
| TypeScript | PASS: final post-upgrade `npx tsc --noEmit` |
| Package/lock declarations | Dependency and devDependency declarations agree |
| `git diff --check` | PASS |

The earlier backend result remains 1,073 passed/zero failed/skipped after the Pkcs
9.0.22 update. It was not rerun for subsequent frontend-only changes. API and test
NuGet audits reported zero known vulnerable packages. SQL-backed dependency/runtime
validation remains blocked; none of these audits proves complete application security.

## Frontend advisories repaired

The first full frontend audit reported one critical and three high vulnerable
packages: Next.js 16.3.5, sharp 0.35.4, source-map-js 1.2.1 and zod 4.6.5.
Verified the supported package metadata and moved Next.js and eslint-config-next
together to 16.4.3. Targeted updates within declared dependency ranges resolved
sharp 0.35.5, source-map-js 1.2.2 and zod 4.6.7. The lockfile was refreshed;
no blanket forced audit fix or TLS/signature bypass was used.

The findings include Next.js image/cache RCE advisories
[GHSA-2464-fhr4-w9xw](https://github.com/advisories/GHSA-2464-fhr4-w9xw) and
[GHSA-673q-mjm6-r5xr](https://github.com/advisories/GHSA-673q-mjm6-r5xr), plus
[sharp](https://github.com/advisories/GHSA-wq5f-xc86-pv6w),
[source-map-js](https://github.com/advisories/GHSA-68fv-2mgg-jv7q) and
[zod](https://github.com/advisories/GHSA-mrhx-c7xc-m3w5) advisories.
Applicability/exploitation in production was not tested or inferred from package presence.

## Test entry point and remaining failures

`apps/web/package.json` now supplies `npm test`, running
`QA/tools/run-frontend-tests.cjs` from the repository root with four local workers.
Logs and machine-readable counts are saved under ignored
`.build-check/frontend-tests/`. Zero-test/incomplete/cancelled/failed runs return
failure. The cloud command required subprocess access; sandbox-denied workers
produced no test output and were correctly rejected, not counted as passes.

A broad exploratory run of the historical `*.test.cjs` files initially returned
877 passed/152 failed out of 1,029. The certificate application and historical test
inputs were byte-identical to b374b35. Three older certificate entry points had
stale import boundaries: the original issuance input now runs through the added
`certificate-current-ui.test.cjs` adapter (all 48 original assertions pass), and
both historical print/appearance inputs are covered by the existing current
`certificate-font-ui.test.cjs` adapter, preserving the original 50 print cases.
The runner prints these successor mappings rather than silently dropping them.
Original test inputs and assertions remain unchanged.

One media transport test incorrectly accessed `Authorization` as a plain object
property. It now reads via the standard `Headers` interface, retaining both old/new
bearer checks, retry body identity and no-token-in-URL assertions. All 28 media
client cases pass; product transport code was unchanged.

The three remaining failures are the Email/WhatsApp/Meeting historical real-HTTP
summary cases in `communication-drafts.test.cjs`. They fail reading absent
`QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`. The other 45 cases
in that file pass. Do not fabricate that evidence, skip those checks, or describe
the full frontend suite as green. Restore the original synthetic evidence or run
the guarded real SQL fixture and validate newly captured evidence. Optional
capture-only cases gated by QA_* variables were not enabled; no overrides were set.

## Release and continuation limits

The developer/staging/production gates remain open: three failed frontend tests,
65 lint warnings, real SQL migration/transaction/concurrency and tenant/RBAC
checks, critical finance/file-access cases, browser/physical-device review and
backup/restore evidence still need their existing protocols.

SQL/model downloads remain blocked on the three exact CDN domains recorded in
the earlier dependency report. Their environment draft additions were saved but
not applied or published. No database, customer record, environment snapshot,
GitHub push/merge or Azure deployment was changed during this increment.
PentaMini source is unchanged; real inference remains unverified.
