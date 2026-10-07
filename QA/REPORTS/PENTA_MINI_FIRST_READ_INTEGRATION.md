# PENTA Mini — first real read integration

Date: 2026-10-07. Worktree `D:\AcademyDesk-codex-p0`; branch `codex/penta-search`; starting HEAD `f8389a77ff731ebc5a095f42a1d72d2671f22e18`. Validated implementation commit `856058dd613c757abf0812baffe5c078b5e9d88a`; subsequent documentation checkpoint records this SHA before normal feature-branch publication. Verify the current remote tip; no main merge or Azure deployment.

Scope: **Outstanding Fees Conversational Search**, one continuous prompt-first PENTA AI experience with a preserved Manual Workspace. Local/private, default-off, Development/Testing-only, current own-academy Owner/Admin + Finance. This is the first read-only local product slice, **not production acceptance or completion of enterprise testing**.

## Authoritative contracts and implementation

- Existing PENTA Mini repo `D:\PENTA AI Models`, unchanged main `2bf76b54054ba610071e3435c94cf36bedff3a99`; authoritative `HANDOFF_TO_ACADEMY_DESK.md`, `protocol.py` and `learning.py` were inspected. No new model engine, external inference, training, weights or credentials were introduced.
- Service 0.1.0; Tool Protocol 0.1; prompt planner-0.1; pinned Qwen3.5-2B Q4_0, conversion revision `f6d5376be1edb4d416d56da11e5397a961aca8ae`, weight SHA256 `cd70221bebaee0503e0f6717e174250cd7825aa88438b3aabec9ad55731d9bb1`. Existing engine readiness and selected model filename were checked. Academy Desk has no runtime revision-attestation endpoint: audit records expected revision explicitly, not an attested identity.
- Architecture and conflict reconciliation: [PENTA-002 addendum](../../PENTA_CONVERSATIONAL_ARCHITECTURE.md#penta-mini-local-integration--2026-10-07). Host sends the actual generic tool-name array and structured bounded state, not invented schemas or unlimited history. Only `SearchLearners` and `GetLearner` are exposed. Registered READ classification, current auth/tenant policy and strict arguments remain host-owned.
- Shared deterministic `OutstandingFeesService` supplies the connector, manual invoice ledger and linked Student 360 invoice projection. Completed/Reconciled payments only; adjustments honored, no model arithmetic. Student 360 uses `useSearchParams` for its static-export deep link, avoiding the previous hydration mismatch. Active enrolled subject selection does not attribute all invoices to that course; the UI explains this limitation. Stable SQL order, separate duplicate-name IDs, no enrollment multiplication, consistent transaction and ten-row maximum. Invalid/cross-currency and overcollected source data fail closed.
- Private state uses purpose-bound Data Protection, academy/actor composite FKs, protected prompt-input digest and protected receipt, SQL version/claim and required atomic audit. Raw prompts/model prose are not persisted to SQL or audit. No model call runs inside a SQL transaction. Pending/unknown requests are not automatically repeated. Expired sessions deny after 24 hours; this is access expiry, **not a production purge policy**.
- Typed learning metadata aligns with Mini's contract and remains tenant scoped, with private argument maps empty and `training_eligible=false`. No event is exported or used for automatic training.
- Central local adapter: loopback-only, no redirects/proxy/retries, 16 KiB response limit, duplicate/unknown/schema keys rejected, 125-second planning deadline, three-second readiness. Provider/domain/store failures produce safe fallback; an audit/store failure cannot produce a success receipt.

## Real validation

| Check | Result |
| --- | --- |
| Complete API test suite | **1,104/1,104 PASS**, 0 failures/skips |
| API / SqlHarness build | PASS, 0 warnings/errors |
| TypeScript | PASS |
| Targeted frontend lint | 0 errors; one inherited profile `<img>` warning |
| Static-export frontend build | PASS, webpack, 83 pages including `/penta` |
| EF pending-model changes | None; existing AdjustedAmount/PassingPercent precision warnings retained |
| Earlier full SQL/browser run | `f1d4dd4260a641faa4719989789bcf3a`, exit 0; 88 application + 7 Identity migrations; unchanged domain/AI execution counts; owned database/login/container removed |
| Prior hardened SQL/API run | `d221703f59204effa7aefbdf1ae6db9e`: all foundation/C1a and Mini/security checks PASS; exit 0 after stopping its browser host; owned database/login/container removed |
| Final real SQL/API run | `e38114f1f2ca478cabee6a55472a8543`: all foundation/C1a and real Mini/security checks PASS, including Student 360 adjusted-balance parity; browser host retained temporarily for owner viewing. Final teardown/invariant check occurs when this host stops, not claimed already complete |
| Four real Mini turns | Pending **4**; Piano **3**; SQL sort **400, 300, 200**; second source **Ananya Piano / INR 300** |
| Finance/source integrity | Adjusted/Paid-label invoice 300, Completed+Reconciled invoice 400; Pending/Voided excluded; cancelled/inactive/foreign excluded; duplicate names/enrollments; empty/mixed-currency/corrupt/mismatched-currency guards; manual invoices 400 and linked Student 360 300 match PENTA |
| Security / state | Teacher/student/foreign tenant 403; other same-tenant admin private session 404; frontend forged state 400; unavailable/unknown/disallowed tool or forged argument no source facts; prompt injection cannot cross tenant |
| Revocation / failure / replay | Finance revocation during planning and receipt replay denied; concurrent pending/stale request 409 and one dispatch; current identical receipt replay 200 with no new model call; create/claim/completion audit failures roll back, safe 503, unknown claim held and not redispatched |
| Final browser | PASS against the production static export: actual Identity login → real Mini → SQL, four turns, correct ordinal/source href, actual Ananya Student 360 with INR 300, Manual round-trip, shared capability context, help/focus return/Escape, 320/390/1440px no overflow, light/dark screenshots; zero console/runtime/hydration errors, exactly four turn POSTs |
| Physical Android / iOS | **NOT RUN**; viewport emulation is not device acceptance |

Final browser evidence is local/untracked: `QA/EVIDENCE/penta-mini-e38114f1f2ca478cabee6a55472a8543/browser-result.json` and light/dark/mobile PNGs. Earlier evidence remains intact. Native computer tools failed to initialize; installed Playwright/Edge performed the real browser checks with no response mocks or token injection. The final frontend serves the webpack production export, without development hot refresh. `QA/tools/penta-static-preview.cjs` binds only loopback and serves actual exported files, including the Windows Next 16 nested-segment/dot-URL mapping. Azure/Linux export routing remains a separate release gate; this local mapping is not an Azure validation claim. No package changes were introduced.

Preparatory runs exposed a fixture bearer-header reset, a non-translatable EF positional record projection, and existing Student 360 hydration/fee-summary defects. They were corrected without weakening assertions. Final SQL uses a member-init ledger projection. Opening a source page under Next development hot refresh reset a peer tab's memory-only conversation; final browser acceptance therefore uses the actual production export. Initial preview failures identified the Windows export path mapping, corrected in the local preview server. The zero-console-error assertion was retained. Only exact labelled run-owned containers were removed; no global prune, main edits, production database or credentials were touched.

## Human-view checkpoint and boundaries

Frontend `http://127.0.0.1:49542/penta`; login first at `/login` as the disposable QA AcademyAdmin `penta-admin-a@example.invalid` (synthetic test password supplied separately in the checkpoint, not a production credential). Academy is **PENTA synthetic A**. API bridge is loopback 49541 and private Mini loopback 8000. SQL is this run's disposable, guarded local fixture—not Azure/customer data. The fixture denies all domain-write HTTP methods; conversation bookkeeping and Identity login are the only allowed POSTs. The bridge has a four-hour deadline and an exact run-owned stop marker; runner then checks unchanged domain/AI execution counts and cleans only its owned resources.

Send: “Show students with pending fees.” → “Only piano.” → “Highest first.” → “Show the second one.” Expect 4 then 3, balances 400/300/200, then Ananya 300 with source link. Capability labels do not unlock five full AI products; they share this one read-only pilot.

Known limitations: CPU inference latency; narrow phrase/tool pilot, not guaranteed general language accuracy; ten active records per read; reload/navigation starts a new in-memory transcript; no production transcript retention/key-ring/purge approval; only Owner/Admin Finance; subject selects enrolled students, not course-specific invoice balances; no currency conversion; no writes/sends/scheduling/Autopilot/training. Student 360's existing ten-recent-invoice cap and broader mixed-currency display are not an all-history/all-finance acceptance claim. Global accounting/refund/credit-note policy and existing enterprise issues remain OPEN. Owner review is next; do not automatically expand features. Suggested next accepted slice is richer source-grounded learner search, then Pulse briefing; Sol High for authority-sensitive integration, Sol Medium for bounded visual refinements. No main merge or Azure deployment.

## N1 paired continuation — 2026-10-08

Starting branch tip `bd1422e31687e50dfa63c6f17233c8041ac9ae0b`. The owner authorized continuation and reaffirmed the 90% AI-assisted / approved-automated eligible-workflow target, 10% manual control, and frontend/backend delivery together. [The existing product roadmap](../../ACADEMY_DESK_PRODUCT_ROADMAP.md#paired-delivery-plan--2026-10-08) now retains completed checkpoints and separates nine remaining delivery packages, not nine small jobs. No claim of general-assistant parity or current 90% automation.

### Changes and boundaries

- Shared source read accepts the existing generic `name` argument against student name or StudentNumber; returns nullable source record code. Exact codes remain academy-scoped, and partial-code searches can match more than one record. No new protocol, permission, model runtime or financial arithmetic.
- A name/code-filtered search with multiple source matches returns trusted `CLARIFICATION_REQUIRED`, separate IDs/codes and bounded ordered results. It never auto-selects an identity. Broad “Meera” fixture search includes the earlier twelve learners: total fourteen, ten displayed, `hasMore=true`. Refining to “Meera Piano” isolates two duplicate full names without deleting or changing the prior fixture.
- Chat cards show codes/reference/source links. Only the latest clarification offers choice buttons; a choice populates the ordinal prompt and focuses the composer, with no hidden POST. Explicit Send goes through the existing trusted-ID guard; old cards lose choice buttons after a new receipt. Manual access, themes, mobile targets and original conversation remain intact.
- Invalid opaque GetLearner identifiers produce a helpful guarded clarification, no source read. They are not silently converted into a search or accepted as authority.

### Real-model gap — remains OPEN

Actual private Mini probes of `Find student AD-M001.` and `Search students named AD-M001.` returned `GetLearner(learner_id="AD-M001")` instead of `SearchLearners(name="AD-M001")`. The host blocks that non-displayed identifier. This is **not a passing conversational code lookup**. UI says direct code prompts are not supported reliably and suggests name search/verified choice. Typed synthetic-provider tests prove the connector's own/foreign/unknown code filtering separately; fake planning does not count as real-model acceptance. Engine repo remains unchanged; the failed phrases belong in N2 held-out intelligence evaluation before any tuning or engine change.

### Validation checkpoint

- Full API suite **1,107/1,107 PASS**, no failures/skips; three additional generic name/code contract cases.
- Harness build **0 warnings/errors** after stopping the previous owned preview. Initial build hit its running harness DLL lock; no process-wide kill or user environment reset.
- TypeScript, targeted PENTA frontend lint and JavaScript syntax checks PASS; webpack production static export **83 pages** PASS.
- Previous viewing run `e38114f1f2ca478cabee6a55472a8543` stopped normally: domain/AI execution counts unchanged, application/Identity migrations 88/7, owned database/login/container removed, exit 0.
- Preparatory N1 runs `306a060cca9546d0990983f0a90c08b6` and `4995ad0e83a64b65beaaa6723f2921c9` failed an overly narrow fixture expectation (Meera total was fourteen, not two). They are failures, not acceptance passes. Exact owned labelled loopback SQL containers were removed after inspection; no other container pruned.
- Final N1 SQL/API run `90d24d2d9cfc4ab7a983d208030089a3`: existing foundation/security/audit/replay tests and original four-turn real Mini flow PASS; real name total fourteen/display ten, exact duplicate pair and explicit ordinal PASS; current code-prompt gap safely clarifies, not counted as a successful code lookup. Synthetic typed-code adapter own/foreign/unknown filtering PASS; strict opaque GetLearner guard retained. No existing regression runner weakened.
- Final production-export Edge browser PASS with synthetic same-academy admin-c: seven explicit real HTTP turns, duplicate codes/cards, choice only prepares a prompt, explicit Send resolves the trusted ordinal, stale buttons removed, 44px target at 320/390/1440px, no horizontal overflow, light/dark screenshots visually inspected, manual/capability/help preservation, source-linked Ananya Student 360 INR 300. Zero console/runtime/hydration errors; no HTTP 429 or duplicate requests. Local evidence `QA/EVIDENCE/penta-mini-90d24d2d9cfc4ab7a983d208030089a3/browser-result.json`.
- Two preparatory browser checks failed the new 44px assertion (shared workspace computed 38px/40px actual). A narrow PENTA-only minimum-size override fixed it; the assertion was retained and expanded to all three widths. A fresh existing fixture admin was used for deliberate retests rather than raising quotas. Earlier fixture assertions and failed browser runs are not converted into passes.
- Current loopback bridge/owned SQL fixture remains for a four-hour view from browser-ready (~18:59 UTC Oct 7 / 00:29 IST Oct 8). Final teardown and post-browser unchanged-row snapshot occur at stop/deadline and are **not yet claimed PASS/exit 0**. The prior viewing run's teardown did pass as recorded above. No production/customer database or credentials touched.

Enterprise issues, physical Android/iOS, production history/privacy/key lifecycle, all governed writes and Azure gates remain OPEN. Next route: **Sol High**, N2 varied-language and code-planning evaluation, not a repeat of the accepted Astra audit.
