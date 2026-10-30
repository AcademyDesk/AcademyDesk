# Native session verification adapter — 29 October 2026 IST

Run [40838396103](https://github.com/AcademyDesk/AcademyDesk/actions/runs/40838396103) for eaa95e5 completed FAILURE in10m16s at SessionRefreshRegression.cs:87, the native Node client result. Later concurrency/recovery modules did not execute in that run. This failure is retained; no newer acceptance is inferred.

The native session-refresh VM loaded the current production api.ts without exposing AbortController. The helper now requires that browser global for controlled refresh cancellation. A local controlled probe reproduced `AbortController is not defined` with the old VM globals, then verified401→refresh→200 replay with the supplied global. This is QA-adapter evidence, not real SQL evidence; the application helper was not changed. The concurrency adapter already supplies AbortController.

Both native clients now emit a shared safe diagnostic:whitelisted exception type, bounded numeric expected/actual values, a known missing browser global, and a known source-file/line. Raw messages, stack paths, bodies, strings and tokens are never emitted. Four leakage/bounds tests PASS. Existing refresh/concurrency client regressions plus these tests:61 PASS.

Review also found that RoleRevocation's old diagnostic mode could report a policy gap without failing. That original mode remains available for historical reproduction. CI now uses an explicit fixed/regression mode with the same cases and requires zero gaps. RoleReplacement and SessionRevocationRepair already enforce every failed control. No product authorization rules or expected denials were relaxed.

New harness compilation0warnings/errors, PowerShell parsing/actionlint/diff checks PASS. Actual native SQL replay, strict role revocation, concurrent refresh and Recovery require the next CI result. The five P0 and full staging/production gates remain OPEN. No configured DB, customer records, Azure settings, main merge or deployment changed.
