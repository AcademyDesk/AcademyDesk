# PENTA explicit-login ownership — continuation across 21–22 October 2026

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. No Mini/contract/model qualification.
This follows [session refresh coordination](SESSION_REFRESH_LOCK_2026-10-21.md).

## Implemented

Onboarding start/review/pending recovery/dialogue/turn/approval/creation helpers
previously compared bearer text. An explicit replacement with the same token pair
could leave detached handles/projections usable if the event had not arrived.
They now capture the login epoch and compare it before dispatch and after reads.
Review provenance retains its original session-current predicate, including when
composed into a dialogue. Reconstructing an operation from an old verified object
does not capture a new login's authority. Same-document replacement events abort
mounted start/review and operation/turn channels; native storage/focus checks remain.

The read-only PENTA chat now pins bootstrap/conversation/turn adoption to its login
epoch, checks direct submit/New conversation, clears private context/replies/prompts
on replacement, and handles focus/page-show/visibility resume plus local/native
storage events. Normal same-login automatic token renewal remains allowed. An
explicit availability recheck clears the prior conversation and checks the current
server account; it does not resend a prompt. Unavailable status is not shown as a
connected model after session invalidation.

These are defensive client ownership/privacy controls, not server authorization,
revocation or atomically coordinated login/logout writes. Existing server actor/
tenant/current-authority/approval/transaction/audit safeguards remain unchanged.
Already-admitted server work cannot be undone. PENTA write latches/uncertain
outcomes remain one-shot and GET-only recovery; no new model/tool/default enabled.
Legacy null epoch remains usable only while it stays unchanged. Same-origin script
compromise or writes bypassing the explicit login marker are not prevented.

## Focused evidence

- New actual onboarding code baseline:17checks,1pass/16fail; same bearer/missed
  replacement event reproduces late adoption, detached provenance and pending gaps.
  Red logs retain observed results; the pre-edit onboarding/read-chat source was
  not copied into a separately immutable baseline packet, so none is claimed.
- Final onboarding checks **397 distinct PASS**:358 assertions from seven retained
  script suites plus39direct Node checks.62new assertions, old checks retained.
  Scripts emit their own counts; Node's wrapper count46 is NOT the assertion count.
-55UI/entry/callback checks PASS;12idle-expiry checks overlap the397 above, so only
 43additional distinct checks.219auth/helper regressions also PASS.
- Read-chat baseline13checks:3pass/10fail; final **13new +57retained contract =70**
  checks PASS. One intermediate implementation shadowed a predicate with the old
  session variable; the normal-renewal control failed, naming corrected, final pass.
- **729 distinct selected assertions PASS** across these runs, not the sum of
  wrapper counts or duplicate idle tests. No inference/model-quality claim.
- Onboarding source build/TypeScript/webpack87routes and full lint0/0 PASS. Read-chat
  successor target lint0/0 PASS. Final combined **TypeScript/webpack87-route export
  and full lint0errors/0warnings PASS**. One overlapping standalone TypeScript
  process was stopped (not passed)
  when available RAM fell below1.5GiB during SQL/browser activity. Only exact owned
  node process14116 was stopped; no services/containers/data/other compiler stopped.
- **25final exported React read-chat browser cases PASS**:22original malformed/
  recovery/outage/stop/logout/role-change cases retained plus3explicit-login
  replacements during bootstrap/ready/pending turn. Synthetic API/credentials,
  actual exported component/actual session helper; local replacement event and
  shell clearing, NOT native Identity/model/device acceptance. Existing22cases
  still start with epoch-less synthetic credentials. No unexpected console/page
  errors; original3intentional409/410/network diagnostics retained. Receipt:
  `QA/EVIDENCE/penta-chat-contract-1792607713901/result.json`, SHA256
  `BFC48DB991EE042F3E2DDC7B6AF873EBC5E848280F247352ABB1AC17BD9CEC28`.

## Native SQL/browser evidence — PASS

Run `afce2af7fd7c4adabe164e61b5929095`, **exit0/395.6s**, guarded availableRAM3.92GiB before start.
Cached harness build0warnings/0errors,117.87s; unchanged102+7migration/runtime gates.
No model/provider/schema/business change. New QA-only add-on uses the existing
read-only relay, native bearer/profile/SQL responses, exported React pages and the
actual login-boundary helper on a private QA peer page. This is NOT a native login
form test, real identity replacement/role change, actual model or physical device.

The original15review cases/32exact audit assertions passed unchanged BEFORE the
add-on. Six additive selected/pending ×peer/local/late cases PASS,17GETs/11exact
actor/tenant read audits(8current/3draft). Identical bearer retained while login
epoch replaced; private forms/results disappear, late responses cannot rehydrate,
no extra read/retry/domain write. Independent SQL snapshot verifies all domain,
Identity,drafts,sessions,turns and unrelated state unchanged. No429. Original
operation/start/dialogue/pending/expiry browser work also passed unchanged:33retained
contexts +six new =**39actual SQL-backed browser contexts**. Three terminal reload/
reentries occur inside those contexts, not three more. Native creation/lost-response/
approval/pending/expiry controls retained; scripted test interpretations do not
qualify a real model. No suite assertion, audit count, quota or timeout relaxed.
Negative cleanup-marker check,102application+7Identity migrations and exact owned
database/login/container teardown PASS. Container independently absent,29containers/
41volumes remain, only original two PENTA services running. Memory recovered to
3.47GiB; final read-chat export completed separately, not during the SQL/browser run.

New receipt `QA/EVIDENCE/penta-onboarding-session-live-afce2af7fd7c4adabe164e61b5929095/`
contains `result.json`(SHA256`C7C6F5858350E504BA6D54F17F3BC5D0A037447D5DCF1CFBD2978E67389AAF89`),
`sql-postconditions.json`(SHA256`F1B652B02D5222D8034BC7E1F3E05B0F792AD77520E8F3D41003212AC6AC660A`)
and six screenshots; no credential
values persisted. All credentials enter test processes via stdin only. Original
assertions, quota, timeouts and limiter not weakened. Add-on flag is admitted only
for the retained OnboardingAtomic/review browser route before SQL creation.
Selected/peer320px light recovery screenshot inspected: no private fields or
horizontal overflow, visible sign-in route. Native authenticated replacement here
changes only the UI marker with the same original bearer; no server logout or
revocation is asserted. Read-chat source changed during this run but was not part
of its consumed onboarding export; onboarding/API source pins match the receipt.

## Source pins

| Source | SHA256 |
| --- | --- |
| `lib/penta-onboarding-start.ts` | `BA5686D53B61E80171EF6A0D3BCC5B172FCF2CDD7E533AC90353077E9582D351` |
| `lib/penta-onboarding-review.ts` | `898B37013A823F9BCC942B406246A8ADC841C669A461EDBC2F6D2007DCC1F4FE` |
| `lib/penta-onboarding-dialogue.ts` | `54E24EC856D30C8998FEF09BB72D119998F4456926CD91E300E9CFD5CA3BEFB2` |
| `lib/penta-onboarding-operation.ts` | `8995686A76C51ECE3948E5BBBFFD182BFE64AB21E02EFA00B57E16EBA81D322D` |
| `lib/penta-onboarding-channel.ts` | `418B44E364E68A91210D4FF060BAF631791454CEB7EFE2BBD5EE3CECE74E689F` |
| `components/penta/onboarding-review-workspace.tsx` | `6A94A114B0B0455251496B3BE90FE0C3A1AD8672ACFC4EF5C79D5AC2CB48317E` |
| `components/penta/onboarding-start-workspace.tsx` | `17BBB9F08CAA716D0D400A7C5ACD5C86668341B908FD16D2BC14DE51CC44BA70` |
| `components/penta/penta-chat-workspace.tsx` | `58DB4E76E4FE8C13E08261879542A40AA2DC963938A5377360D287D770AEE99D` |

Source paths above are under `apps/web/src/`. Logs `.build-check/session-resume/`:
`penta-session-*` and `penta-chat-session-*`. New controlled cases are not a claim
that all native races, conversations or business workflows are delivered.

## Remaining gates

Independent model review/qualification, scoped training authority, durable history,
broader domain/approval/role/privacy/device/enterprise/release work remain OPEN.
Model full-plan6/12fresh7/12exposed NOT ACCEPTED;76pending candidates/zero eligible.
BUG-DATA-0001 and broad auth issues remain OPEN; all11delivery packages retain their
remaining acceptance. No production percentage or readiness/date inferred.
No commit/stage/push/main/Mini/Azure/acquisition/training/activation/restart/prune.
Stay High for consequential session/privacy/model/domain gates; Medium for settled UI.
