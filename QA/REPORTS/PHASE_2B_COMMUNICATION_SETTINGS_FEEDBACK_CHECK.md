# Communication Settings feedback gap check — 2026-10-10

Starting `fe7c693d865efeb33e7247c1dbb16e25acd7a986`, `codex/penta-search`, `D:\AcademyDesk-codex-p0`. BUG-FUNC-0003 and related BUG-DATA-0041 remain OPEN.

## Reuse rather than reimplement

The accepted independent channel repair already provides durable accessible channel-specific success, full summary validation, no whole-page readback, duplicate per-channel guards, concurrent reply isolation, retained drafts on failure and field-level reconciliation of newer edits. All retained controlled assertions pass; none of that logic was rewritten. Only application change: move the existing mount-only loader inside its effect to remove the captured `load` dependency warning without suppression or save-driven reloads. Initial complete-shape/duplicate-channel guards, provider wire fields, hidden optional values, explicit clears, status normalization, credentials/authority and child controls remain unchanged. No visual redesign or backend modification.

## Executed evidence

- Initial retained run:81/84 PASS; three historical replay cases failed **ENOENT**, not a reproduced product failure. Log `QA/EVIDENCE/communication-settings-feedback-baseline-20261010.log` retained.
- Located the original historical SQL receipt read-only at `D:\AcademyDesk\QA\EVIDENCE\logs\phase-2b-communication-roundtrip-sql.log`, SHA256 `3457496c605a7e684a2ac8cac40dbb63b81087a4012c07f0c66e3a0d78442e6f`. No copy or edit of evidence/main. Added optional `QA_CHANNEL_HISTORY_LOG` input with unchanged default path and unchanged replay assertions; `QA_CHANNEL_SQL=1` enables the original additional three replay controls.
- **90/90 PASS against both HEAD and final source**:87 original retained draft/roundtrip controls plus3 new per-channel mount request-count and accessible durable notice checks. Frozen source selected via QA_SETTINGS_BASELINE=1. Logs `communication-settings-resolved-baseline-20261010.log` and `communication-settings-final-20261010.log` local. Historical receipt replay is not a new HTTP/SQL test.
- **24/24 exported browser channel cases PASS**:320/1440 × light/dark × Email/WhatsApp/Meeting × in-flight edit/rejected save. Real DOM/React over synthetic API proves newer-edit notice, other-panel draft retention, hidden-field preservation, one initial settings GET, exactly6 explicit PUTs/context, restored controls, accessible statuses, no horizontal overflow or unexpected console/hydration error. Deliberate400 resource errors identified separately. Local receipt `QA/EVIDENCE/communication-settings-browser-1791572458938/result.json`, screenshots alongside.
- First browser attempt `1791572418437` failed because test locator still expected the pre-save button name after it became `Saving…`; corrected to assert the real pending button, no app change or weakened check. Failure retained locally.
- ESLint baseline0 errors/1 inherited dependency warning; final **0/0**. TypeScript PASS; production webpack/static export **83/83 PASS**; diff checks PASS.

No new live Identity/SQL/container/backend suite, physical Android/iOS, real-server concurrent writers, navigation/reload draft persistence or secure-provider connection acceptance. These remain existing enterprise gates. No issue/release closure, main merge or Azure deployment. QA/EVIDENCE remains untracked; all unrelated Mini work preserved; saved86/94 CONTRACT READY / ENGINE BLOCKED unchanged.

Next **Sol Medium**: Communications existing-feedback gap check, reuse accepted delivery-channel repair and test guards. Escalate to **Sol High** for external-send, credentials, authorization or domain/persistence changes. Continue existing program, not a new testing plan.
