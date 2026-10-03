# Browser-only handoff — Payments page

Use Cursor Composer 2.5 only if Codex computer-use remains unavailable. Sol High owns the engineering program; this is a bounded evidence request, not a transfer of implementation ownership.

## Ready-to-paste task

Repository: `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. Read `AGENTS.md`, `AI_HANDOFF.md`, `AI_MODEL_ROUTING.md`, `ENTERPRISE_TESTING_ROADMAP.md`, `QA/00_QA_README.md`, `QA/ISSUES/BUG-DATA-0001.md` and `QA/REPORTS/PHASE_2B_P0_CONTINUATION.md`. Confirm branch/status first; do not edit main or overwrite uncommitted work. Do not push, merge, deploy, touch production data or credentials, or change application code in this browser-only task.

Using only synthetic local QA data and a disposable database, exercise the actual signed-in `/payments` page (and relevant invoice/portal reads) in a real browser. Verify initial/refresh balance and collected display for an invoice 1000 with Reconciled 600; an accepted 400; rejected excess with visible error and no duplicate payment; and an invoice 1000 with approved adjustment 200, Reconciled 600 and remaining 200. Inspect console/network failures, direct URL/refresh behavior, success/error/loading states and at least desktop plus a mobile viewport. Do not claim physical Android/iOS validation from an emulated viewport. The existing SQL race modules already pass 5/5 each; do not rerun or rewrite them unless needed to set up synthetic browser data. Do not enter or disclose real credentials.

Record exact setup, route, viewport, observed UI values, browser/console/network result, SQL/API correlation where available, failures and evidence paths in a new QA report. Keep `BUG-DATA-0001` and `BUG-DATA-0002` OPEN. If safe browser setup is unavailable, report the exact blocker rather than marking PASS. Leave any disposable resources you create cleaned by exact ownership checks. Stop this bounded browser task and return the evidence to Sol High; do not begin another QA plan.
