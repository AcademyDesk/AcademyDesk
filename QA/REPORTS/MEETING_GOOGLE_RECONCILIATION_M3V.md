# M3v — acknowledgement is not completed Google grant reconciliation

2026-10-16. Recommended route **Sol High / Codex**, existing `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work. Continue existing M3/enterprise integration; no new testing plan or provider activation.

Scope: close an early-reconnect gap in the existing private OAuth candidate. May modify the host reconnect guard, targeted tests, retained SQL fixture isolation/new assertions and documentation. Do not change primary main, Mini, public DI/routes/tools/UI, provider accounts/registrations/credentials/scopes, release gates or unrelated work. No stage/commit/push/Azure. Stop after the local checkpoint and evidence verification; next stays Sol High in this project.

## Reproduced gap and correction

M3u correctly lets both code-owned Google profiles send/complete their existing separately consented revocation. But `PrepareAsync` blocked Started/Unknown/manual records and omitted **ProviderAcknowledged**. A new intent could therefore return201 immediately after acknowledgement, despite the existing architecture explicitly not treating it as completed independent project-grant reconciliation. A changed registration/profile or waiting did not establish independent proof.

[Google's official OAuth documentation](https://developers.google.com/identity/protocols/oauth2/web-server#tokenrevoke), checked2026-10-16, describes the project-wide impact across registered clients and warns that a successful response can precede full effect. The host's safe interpretation here is to retain the reconnect interlock until a separately qualified reconciliation workflow exists. This is a conservative application policy, not a claim that Google's200 is unsuccessful or that indefinite blocking is the final product UX.

The existing academy/provider-scoped guard now also includes ProviderAcknowledged. No new status, unlock API, fixed waiting period, provider polling, negative-token test, arbitrary evidence field, role/scope escalation, resend or human/model force-clear was added. The acknowledgement/audit remains truthful and immutable, binding locally Disconnected; source credentials stay inaccessible. It blocks both Google profiles/new fingerprints and any current admin for that academy. Other providers and unrelated academies are not globally blocked. Cross-academy account/project sharing still needs actual host policy and provider-impact qualification; this is not global grant-isolation proof.

Eight new regression cases first failed **expected409 / actual201** before the application change. They cover old/new source and target profile pairs, a changed registration revision, immediate requests and365days later. Red receipt retained/excluded from passing totals. The unchanged assertions pass after the guard correction, including exact unchanged attempts/binding/version/generation/revocation/audit and no credential/manual-clear fallback.

The retained35-case revocation SQL suite previously created independent successful-revocation scenarios by reconnecting the same academy after an acknowledgement. Its fixture now creates a distinct run-owned academy/current identities when another Google acknowledgement is present, preserving all previous records rather than clearing evidence or bypassing the real guard. All35 named assertions remain; the manual-recovery check retains its original academy so it still checks a real manual record, not a nonexistent cross-academy ID. No runner, assertion or release criterion was weakened.

## Verification

- **38/38 targeted and1,112/1,112 retained focused tests PASS**, zero failed/skipped (8new /1,104retained). Receipts: `QA/EVIDENCE/meeting-google-reconciliation-20261016/google-reconciliation-targeted.trx`, `google-reconciliation-retained-final.trx`; red receipt `google-reconciliation-red.trx`. Initial narrower1,030 meeting-only pass retained as `google-reconciliation-final.trx`, not substituted for the final expanded retained filter.
- Clean isolated no-incremental API/harness build PASS, zero warnings/errors,38.82s. Current34-case SQL marker verified. API/harness-copy DLL SHA256 both `A3436355B42D140D69DBEDA385BD386F4B146A507B281C7597B57A77BBA8EA2D`.
- Explicit current-assembly `AcademyDeskDbContext` EF no-pending-model-changes PASS, no migration added. Testing environment/startup migrations disabled and unused loopback connection only; no production database access. Existing decimal warnings for PassingPercent and AdjustedAmount remain unchanged.
- **726/726 named SQL-run cases PASS** (10new /716retained;90HTTP-SQL+636direct/internal). Run `a14c0f14793e490aa27b106fdb5daa3e`, port58862, exit0/242.1s; final100app/7Identity migrations, all ten migration guards PASS. Twenty-five complete receipts contain726 checks and149source-hash entries independently rehashed against current source. Current34-case Google SQL marker and all34cases PASS: immediate/old-profile/changed-registration/other-admin/concurrent reconnect denials, elapsed time/expiry not reconciliation, no manual-clear fallback, exact unchanged domain/audit snapshots, and independently allowed other-provider/unrelated-academy setup. All35 retained revocation cases and32 admission cases PASS, no429/provider calls in the admission suite. Runtime inventory333routes/322controller-method routes/10framework Identity, SHA256 `3CA8D1FABA64A043749774D75B9EE01ABC90DA1428443AE2C2C7517BE1477728`.
- Runner proved/removes its owned database/login and exact container `academydesk-qa-a14c0f14793e490aa27b106fdb5daa3e`; container absence independently verified. API/test-copy/harness-copy DLLs all still match the SHA256 above. Other containers, including both PENTA containers, untouched. Initial `powershell -NoProfile -File` was refused by local script execution policy before SQL resources were created; actual unchanged runner used process-only `-ExecutionPolicy Bypass`, not a machine/user policy change.
- Final diff check PASS, zero staged files. Worktree HEAD remains `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`; primary remains `f7af512f884ca5fe8ac3ddb287c852492592e709`. QA/EVIDENCE remains local/untracked, .build-check ignored. No production/provider/Mini changes, public activation, commit/push/Azure or whole-gate closure. Synthetic provider responses and owned SQL are not live grant-removal or enterprise acceptance.

## Existing qualification inputs / next handoff

No genuine Mini/Core contract change is needed here. This remains Academy host integration. Before a live-provider qualification step, the established M3 path needs an authorized operator's approved **disposable nonproduction** setup, supplied through secure configuration, never pasted credentials in chat/evidence:

| Input / proof | Required boundary |
| --- | --- |
| Environment and origins | Approved API/workspace HTTPS origins, exact per-provider `/api/meetings/oauth/{provider}/callback` registrations, valid TLS/cookie/key-ring/egress configuration. The internal registry is not currently a public handler or deployment. |
| Three independently approved apps/accounts | Google project/client/account; Microsoft work directory/app/organizer; Zoom app/client type/account/organizer. Exact host-owned academy assignment and current authorized operator, no silent adoption of legacy configuration markers. |
| Secrets and consent | Backend-only secret references/approved custody; exact existing code-owned scopes and provider/project-impact consent. New scopes/admin roles/shared-account assignments require separate approval, not an engineering shortcut. |
| Reconciliation proof | A reviewed provider-specific evidence source, its identity/project/grant coverage, ordering/freshness, asynchronous completion semantics, outage/unknown handling and allowed residual grants. No complete evidence source is currently qualified; don't invent a force-clear or call an access-token denial family-wide proof. |
| Execution qualification | Real approved organizer/write/account limits, durable operation dispatch/reconciliation, exact user/tenant/approval/audit rules, safe invite/side-effect targets and Manual/PENTA parity. Current private observations/candidates do not enable meeting creation or Connected. |

These are dependencies of the existing integration program, not authority to provision/change accounts, broaden scopes, access real grants or run live operations. Next local work may close host lifecycle/transport/key/retention gaps, but must not label synthetic proof as external qualification. Any actual-provider step requiring new authority stops for that direction. A safe production reconnect/unlock workflow remains **OPEN**, explicitly not completed by this protective interlock.

## Progress / release truth

- [x] Prior checkpoints preserved; early reconnect reproduced and host guard corrected, targeted/retained tests passed.
- [x] Current SQL, EF, source/compiled provenance, exact owned teardown and six-ledger update verified; M3v locally complete only.
- [ ] Independently evidenced reconciliation/unlock, approved actual configuration/consent, remaining organizer/write/lifecycle/public-browser/approved operations.
- [ ] Existing three unfinished delivery groups/eight workstreams and full Manual/UI2/Mini/enterprise/physical-device/security/release queues stay OPEN.90%eligible AI/10%Manual remains a target, not a measured completion claim.

Owner order stays complete agreed app → accepted/explicitly authorized go-live → Hayansh onboarding → product website after first live customer. No publication/deployment or whole issue/workstream/release closure.
