# C55 combined native acceptance — 23 November 2026

Integrated the exact eight-path C55 implementation from ea7da00 on Desktop c52613f. Reviewed the additive invariant-string projection, legacy protected snapshots, strict client parsing and unchanged protocol0.1/tool/ledger/security boundaries. No domain calculation, migration, model activation, production flag or package/lock change. Added a QA-only exact-money SQL case alongside the existing protected receipt suite.

| Verification | Observed result |
| --- | --- |
| PENTA API selection |837passed, zero failed/skipped; includes29new money and23retained recovery cases |
| Targeted money/recovery selection |52passed; overlaps837and is not additional coverage |
| PENTA client regressions |24files,1020passed, zero failed/skipped; includes74Mini cases |
| TypeScript and changed-file lint |Both passed, zero changed-file warnings |
| Default-off export/browser |Build passed;4/4casespassed |
| Enabled composer export/browser |Build passed;118/118casespassed |
| New exact-money browser matrix |92/92casespassed |
| Native SQL/Identity |Passed,86application and7Identity migrations; existing C54 guard checks retained |

## Real SQL financial result

The synthetic source holds two currencies for one scoped learner: INR1000.01billed with1000reconciled and USD90071992547409.91unpaid. The actual protected POST returns exact strings0.01and90071992547409.91. GET preserves them without a second dispatch. A legacy snapshot is produced by removing only the optional computed projection from the actual protected JSON; its original decimal ledger data remain unchanged and its GET regenerates the same exact presentation. The provider reports Unavailable during recovery.

Replay-audit failure returns503without identity/amount disclosure, writes no audit/state and leaves one original planner call. Invoice/payment counts and totals stay equal. Native SQL run4a70d9f3fb06491eade64372ed8afb1fremoved its own database/login/container, restored both exact original services/images and API200. It also retained the C54unknown/expired/stale/onboarding/tampered/inactive/role/module/account and audit-rollback checks. These security cases use a controlled planner, not a live-model intelligence score.

## Resource result remains explicit

All214browser cases and their assertions passed. The enclosing browser wrapper's validationPassed=true but its overall passed=false/restorationPassed=false because one built-in empty bridge network ID changed during the window. Containers, images, volumes and all custom network IDs remained equal; both original services were restored healthy and API200. The new built-in bridge has no attached containers or labels. Cause is not established. Keep the original failure receipt; do not claim complete resource-inventory acceptance from application browser passes or silently whitelist this difference.

The initial no-service-change attempt correctly refused the native run when the demo lease had ended. The final owned SQL run used the existing exclusive window after the demo stopped. The full837API regression had triggered the demo's unchanged low-RAM safety stop. No demo database is reused as a test fixture. Restore the interactive demo after validation; no simultaneous heavy regression runs while it is active.

## Mini cross-profile reconciliation

The active0.15branch lacked accepted C1/C5 work from14c11fc. Branch codex/mini-evidence-bytes-20261123 now publishes2686d90, reusing the existing reviewed portability/original-verification paths and preserving planner0.15. All33byte pins remain exact to the accepted source; no recorded hash, original archive or training permission is rewritten. Final289selected tests plus5subtests passed. All seven directly missing C47 originals exist on D: and are hashed. The genuine version1CLI verifies62original records, excludes14exposed records, and retains zero training/inference/download counters and model_qualified=false.

The intermediate920db34hosted run45117337045 improved1661to1664passes by removing all three frozen-byte failures, with23failures/60errors/1skipstill open. The newer2686d90CI is a separate result. Original private archive prerequisites and historical platform-bound cases must remain visible; no raw original evidence is pushed. The Windows full suite's15Linuxfcntlcollection errors are retained. No new model qualification follows metadata/provenance checks.

## Remaining launch gates

- [x] C55combined host/native SQL/client/browser assertions.
- [x] Reused C1/C5portability integration and genuine original-input verification.
- [ ] Restore and verify the real-model interactive demo on this combined application.
- [ ] Diagnose the built-in bridge identity drift and retain honest resource qualification.
- [ ] Exact candidate hosted verification, read-only private Core Actions credential and current full dependency/security gates.
- [ ] Supply genuine private archive prerequisites for full Mini CI, without modifying expected identities or skips.
- [ ] Wider role/workflow conversational outcomes and measured90%, six critical acceptance records, physical devices and staging/private Blob/restore/rollback.

No main merge or Azure deployment. Sol High remains the route for native/security/model integration. Preserve current accepted source and focus on these gaps rather than restarting enterprise testing.

## Private receipt seals

- .build-check/penta-host/mini-exact-money-native-6758a9ce7a004db4b76e952c7a539293/result.json: 94b588a2dd7b0ee8fe4df9ffc17bb336487124063b23bfb0271f4e7835fd9d8c
- .build-check/penta-host/c55-browser-checkpoint-e953397b2f7447b382e978981dc89ae1/result.json: c5f3fe185c3bffeef71960bb0ad16ba4d341c1fac7edac92c20a1bb932ab23f1
