# Finance Policy hidden payslip theme — bounded repair/retest, 2026-10-11

## Verification successor — ordinary save repaired; stale save still unsafe

Frontend repair below unchanged. Final actual-handler checks **6/6 PASS**; production-export DOM checks **16/16 PASS**, at320/1440px × light/dark × three successful themes plus Compact rejection. Each case checks exact eleven-field PUT, one submission, retained theme/intended tax-label result, rejected draft retention, absent payslip control, no unexpected console errors and no horizontal page overflow. Two mobile screenshots inspected. Browser responses are synthetic, **not linked browser-to-SQL or physical-device proof**.

New `FinancePolicyTheme` module built with **zero warnings/errors** and ran real Identity/HTTP/disposable SQL: **15 ordinary-save/reset/rejection/authority requests checked**, plus **three requests deliberately reproducing a stale overwrite** (18 PUTs total). Each theme seed/edit/readback persists its theme; rejected tax101/unsupported theme/anonymous401/Teacher403/foreign-academy403 requests leave the settings snapshot unchanged. Explicit NULL and omitted payslip keys still reset to Standard. Fresh SQL records equal successful response records and fresh authenticated GETs. No429 occurred.

**Known failure reproduced, NOT concurrency acceptance:** seed Compact → newer editor saves Professional → stale Compact policy request returns200 and stores Compact. Echoing loaded settings prevents absent-field resets but not lost updates. No backend version/CAS/transaction contract changed. BUG-DATA-0053 remains **OPEN / RETEST**.

SQL run `59358d6cf5574380a4510f976c42bd83`, loopback port65060, runtime54.5s, application/Identity migrations88/7; runner verified exact owned DB/login cleanup and removed only `academydesk-qa-59358d6cf5574380a4510f976c42bd83`. Negative mismatched ownership check refused cleanup. Subsequent read-only Docker inventory could not connect to Docker Desktop; cleanup proof is the completed runner receipt, not a claimed later inventory. No daemon restart/prune/other-container mutation performed.

Local evidence: `QA/EVIDENCE/finance-policy-preservation-20261011/sql-final.txt` and `QA/EVIDENCE/finance-policy-theme-browser-1791702215438/result.json`/screenshots. TypeScript, target lint0/0 and83-page export below retained from same frontend source, not fresh reruns. No full critical suite/linked browser/device acceptance. Unrelated Mini/Platform/private work and older32/28 continuity additions excluded from scoped feature publication. No main merge, Mini/Core, finance policy or Azure deployment.

**Next: Sol High in Academy Desk**, stale-settings protection across Finance Policy and Finance Governance document editors with strict SQL concurrency/no-write regression. Retain direct reset semantics or explicitly review any compatibility change. Broader loading/pending/uncertain-result feedback, linked/device/critical gates and Manual Orbit + Aura dark/Prism light rollout remain queued. Sol Medium for settled UI after integrity contract is settled. No project switch required.

Publication checks: staged13-file scope, changed local document links10/10, runner/browser syntax, final6 handler and diff checks PASS. Initial whole-staged-file credential heuristic flagged existing symbolic environment/interpolated password references; reviewed without exposing values, no literal credential found, final heuristic scan PASS (not full-history/security attestation). Remote feature tip matched startingbfad3f8 before commit; older32/28 continuity additions remain unstaged. GitHub publication is not main acceptance or Azure deployment.

## Earlier local checkpoint (historical, superseded above)

BUG-DATA-0053 / FINANCE-POLICY-PRESERVE-001. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `bfad3f8592be8433189d2e4f5467daa19e5bce34`.

## Reproduction and narrow repair

The actual FinancePolicy TSX save handler reads a nonexistent form field `payslipTemplateKey`; native FormData returns NULL. Existing FinanceGovernance.Save treats NULL as Standard. The form now uses its loaded `settings.payslipTemplateKey` and requires loaded settings before saving. Nothing changes in the API, replacement/reset semantics, accepted document themes, finance calculations, permissions or tenant scope. No new theme control was added.

## Evidence

- Frozen starting HEAD under the same six actual-handler assertions: **0/6 PASS**, all requests sent NULL instead of Standard/Compact/Professional.
- Repaired handler: **6/6 PASS**. All three supported themes preserved in exact captured requests and successful synthetic response hydration; rejected requests retain loaded settings and rejection wording. No hidden/visible payslip field is rendered. The fixture uses native FormData with controlled fields/hooks/transport; it is not a browser DOM or real HTTP/SQL proof. Fixture omissions of nullable branding inputs are not evidence of native empty-field serialization.
- Installed TypeScript validation: PASS.
- Targeted Finance Policy ESLint: PASS, no errors/warnings.
- Production webpack build/static export: **83/83 pages PASS**.
- Evidence stays local: `QA/EVIDENCE/finance-policy-preservation-20261011/handler-baseline.txt` and `handler-final.txt`.

## Remaining gates / continuation

Issue stays **OPEN**, local repair not yet committed/pushed. Next: real disposable HTTP/SQL and controlled exported-browser verification with all three saved themes, intended-field change and rejected no-write controls. Explicit NULL/omitted direct API reset semantics must stay unchanged and be tested, not silently converted to PATCH semantics. Existing second-editor/concurrent update case is **not fixed** by echoing the loaded theme: a stale editor can still overwrite a newer theme. Reproduce separately; consequential concurrency/versioning/API decisions use Sol High, not a speculative backend change in this serialization slice. Broader loading/pending/network/uncertain-response feedback and physical devices/full critical gates remain open.

No API/database/Mini/Core/permission/financial policy/main/Azure change, no enterprise/production readiness claim. Unrelated dirty work and QA evidence preserved. Sol Medium for settled UI/browser verification; High for live SQL/authority or stale-edit integrity decisions. Existing Manual Orbit + Aura dark/Prism light and PENTA-owned AI frontend queues retained; do not restart accepted audits.
