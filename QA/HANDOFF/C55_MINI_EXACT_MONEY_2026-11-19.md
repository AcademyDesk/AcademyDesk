# C55 — exact money in private Mini read receipts

MODEL: Sol High
TOOL: Cloud Codex
BRANCH: codex/cloud-c55-mini-exact-money-20261119
DESKTOP: 219bba2971b30321f57a5f509e19e9704d6ee1b7
BASE: f9975dd2 (reviewed C54 return applied on current Desktop source)
STATUS: CLAIMED — 19 November 2026 UTC

Desktop integrated C53 and C50, verified native protected Core/SQL and original
artifacts, and clarified the school-market scope. It has not loaded the Mini
candidate or qualified C54's new native receipt route. The model-loading stop
and all six critical acceptance gates remain. Current Desktop CI44274804584
failed its frontend install/audit step and private Core source access. Local npm
audit has zero findings; hosted log storage could not be read, so no root cause
or passing hosted result is inferred.

C54 was preserved during the prior GitHub503 outage and is now published at
2fa56f86ddc947957d87646c22d06237030c8939. Its two task commits apply cleanly on
219bba29. C53 provider/test bytes match Desktop exactly; no duplicate C53 import.
Cloud claims only:

- apps/api/Intelligence/Penta/PentaMiniMoney.cs (new invariant decimal-string projection).
- apps/api/Intelligence/Penta/PentaMiniOrchestrator.cs (computed read-only receipt property).
- tests/AcademyDesk.Api.Tests/PentaMiniMoneyTests.cs (new precision/compatibility regressions).
- apps/web/src/lib/penta-mini-conversation.ts (strict optional versioned money validation).
- apps/web/src/components/penta/mini-conversation.tsx (exact amount text or explicit unavailable state).
- QA/tools/penta-mini-conversation.test.cjs (new version/precision/binding tests).
- QA/tools/penta-mini-exact-money-browser.cjs (new actual Chromium precision/refusal/recovery cases).
- QA/HANDOFF/C55_MINI_EXACT_MONEY_2026-11-19.md (this packet/results).

The host projection is derived only from existing authorized .NET decimal
receipt results. Keep the numeric0.1 payload for existing consumers and expose a
separate versioned exact-money presentation. Do not alter shared ledger DTOs,
accounting/calculation/currency rules, stored originals, SQL/migrations, model
protocol/tools, tier routing, approvals or server/feature activation gates.
Reject unsupported precision/invalid values instead of rounding. Client must
never derive exact strings from JS Number; validate strict version/shape and
learner/currency alignment. Older receipts retain the safe Manual fallback.

Native SQL recovery/qualification remains Desktop-owned. Run actual projection/
serialization/client tests, enabled and default-off build/browser checks and
scoped API regressions. Preserve source/frozen/vendor data, initial failures,
previous branches and source-bound raw evidence. No main merge, Azure/environment
publication, public-access change, dependency/model acquisition, model loading,
inference, training or activation. Publish bounded feature source and handoff;
verify remote SHA and exact CI. Mini/Plus/Pro unqualified; Ultra deferred;90%
unmeasured. Next route Sol High for host/contract and native acceptance.
