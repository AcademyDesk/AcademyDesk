# BUG-DATA-0006 — full-batch activation reproduced natively

23 October2026. **FAIL / RUNTIME-CONFIRMED / REPAIR REQUIRED.** This continues
the existing admission-integrity issue, not a replacement audit/testing plan.
No application repair or production approval is claimed.

SQL run `0a81a6a9409b442bbd52510922058076`; resource window
`21350108d4a342c79d0818421c46d6fa`. Current source-pinned API image
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`.
Same `codex/penta-search` HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.

## Actual reproduction

36setup/security/domain-seed checks completed in disposable SQL through real
Kestrel/Identity. A same-academy Owner seeds a course, open batch of capacity1,
two students, one Waitlisted target and one Active occupant. No customer data,
mock API response, inserted invalid enrollment or disabled constraint is used.

| Authoritative state | Before activation | After actual PUT |
| --- | --- | --- |
| Batch capacity |1|1|
| Target status |Waitlisted|Active|
| Target enrollment rows |1|1|
| Active enrollment rows |1|2|

`PUT /api/academies/{academyId}/enrollments/{targetId}` with statusActive,
endDatenull/lifecycleReasonnull returned **HTTP200**, not the required409.
Fresh SQL confirms committed over-capacity state. The oracle records the real
result and deliberately fails: `passed=false`, `runtimeReproduced=true`,
`invariantPreserved=false`. A successful defect reproduction is not a passing
admission gate. No429 was observed for this actual activation (it returned200).

Scope: this proves the single activation-capacity bypass. It does not claim
concurrent creation/transfer/promotion/prerequisite or all-role reproduction;
those existing parts of BUG-DATA-0006 remain separately scoped.

## Cleanup and provenance

Admission4.055GiB with unchanged3GiB guard. Only run-owned SQL/API/network removed;
exact original two PENTA services/images restored healthy, full inventories
preserved, exclusive lease released. Native child/window exit1 and overallFAIL
are retained even though cleanup/restoration succeeded. No restart/prune/main/
Mini/model/application change, credentials, commit/push or Azure access.

SQL receipt SHA256 `ac3af18a09024b49b09c4fa8441e51d8a69ce2c35b4c8c4af9f8c48edd476068`.
Window SHA256 `2e5c3756d41f8ba47a4d32ea2e22caa8b51f91e50628414cae93b28cae875e0a`.

Initial SQL `da8dda1621ff4a00947a1a670788899c`, window
`61ca5fd63b704718ab49f89b7032c456` failed in the QA transport before the actual
PUT, so it is not domain reproduction. The helper's new PUT support is restricted
to GUID academy/enrollment paths; arbitrary new write routes remain refused.
Unit negative controls cover that boundary. Both initial and corrected windows
completed exact cleanup/restoration/inventories; neither is relabelledPASS.

145focused QA-tool tests PASS, including “reproduced bypass cannot pass” and
“409 must preserve exact state” negative controls. This unit success is not the
native admission gate result. The other19scoped accepted receipts still qualify
against unchanged application source; they do not override this negative failure.

## Next engineering

Use High for the shared authoritative admission/transaction repair and affected
SQL concurrency/audit/role tests. Existing details and boundaries:
[repair packet](../HANDOFFS/ENROLLMENT_ADMISSION_REPAIR_2026-10-23.md).
Do not add only an optimistic precheck to Update while Create/Transfer/Promotion
can race it. Preserve accepted terminal-promotion and unrelated finance rules.
Fresh source-pinned candidate/retests are required for a repair; keep the issue
OPEN and retain all original evidence. All11broader delivery/release packages,
HIGH dependency advisory, model/fullAI/devices/operations/production gates OPEN.
