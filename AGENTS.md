# Academy Desk agent instructions

Academy Desk is an existing enterprise SaaS codebase. It is not a greenfield prototype.

GitHub is the accepted source of truth for application changes. Local working trees, design tools, and experimental prototypes do not override `origin/main` until their changes are reviewed and accepted there.

These root instructions govern coding agents working in this repository. `apps/web/AGENTS.md` is generated Next.js tooling guidance for that app. It does not replace this file, `AI_HANDOFF.md`, `AI_MODEL_ROUTING.md`, `AI_PRODUCT_VISION.md`, or `ENTERPRISE_TESTING_ROADMAP.md`.

## Required reading before modifying code

Every coding agent must, before modifying code:

1. Read `AGENTS.md`.
2. Read `AI_HANDOFF.md`.
3. Read `AI_MODEL_ROUTING.md`.
4. Read `ENTERPRISE_TESTING_ROADMAP.md`.
5. Read `AI_PRODUCT_VISION.md` when the task could affect product behaviour, architecture, UX, or AI functionality.
6. Run `git status`.
7. Confirm the branch and worktree.
8. Inspect recent relevant commits.
9. Never overwrite another agent's uncommitted work.
10. Never reset, clean, or stash unrelated work without explicit approval.
11. Preserve business behaviour unless explicitly instructed otherwise.
12. Run relevant validation after changes.
13. Update `AI_HANDOFF.md` after substantial work.
14. Use logical commits.
15. Never expose secrets.
16. Never deploy to Azure unless explicitly instructed.
17. Never conclude that enterprise testing is complete merely because unit or API tests pass.

Passing builds and API tests are necessary evidence. They are not the enterprise testing program. Continue that program from the existing baseline, QA issues, reports, tools, and evidence described in `ENTERPRISE_TESTING_ROADMAP.md`.

## Operating sequence

```text
READ → CHECK → PLAN → MODIFY → TEST → DOCUMENT → COMMIT → HANDOFF
```

- **READ.** Load the governance files required for the task and the existing code, tests, and QA records the change depends on.
- **CHECK.** Confirm branch, worktree, `git status`, and recent relevant commits. Stop if another agent already owns the same files.
- **PLAN.** State scope, files that may change, files that must stay unchanged, and the validation that will prove the change.
- **MODIFY.** Change only the approved scope. Preserve existing business behaviour unless the task explicitly changes it.
- **TEST.** Run the relevant validation. Record what passed, what was not run, and why.
- **DOCUMENT.** Update `AI_HANDOFF.md` after substantial work so the next agent inherits the real state.
- **COMMIT.** Use logical commits when a commit is requested. Do not commit secrets, local evidence that is intentionally uncommitted, or unrelated dirty work.
- **HANDOFF.** Name the next route from `AI_MODEL_ROUTING.md`.

## Multi-agent branch rules

Use a feature branch or worktree for substantial work. `main` contains reviewed, accepted changes.

Recommended branch names:

- `codex/<task>`
- `cursor/<task>`
- `v0/<task>`
- `experiment/lovable-<task>`

Cursor, Codex, and v0 must not independently change the same production files at the same time. Do not allow multiple agents to edit the same files on `main` simultaneously.

Lovable experiments stay off production `main`. A successful prototype returns through review and is implemented in this repository by Cursor or Codex.

Framer work for the public marketing site stays logically separate from the secured SaaS application unless an explicit task joins them.

## Boundaries

- Do not reintroduce hard-coded credentials. Development seeder credentials were removed from current source, and local development values were rotated using secure local secrets.
- Do not deploy to Azure unless the task explicitly instructs deployment and the pre-Azure quality gate in `ENTERPRISE_TESTING_ROADMAP.md` is satisfied.
- Do not delete or replace useful QA work because GitHub governance now exists. `QA/EVIDENCE` remains primarily local and is intentionally not broadly committed.
- Do not sacrifice security or testing to accelerate AI functionality. Product direction lives in `AI_PRODUCT_VISION.md`. Model and tool choice lives in `AI_MODEL_ROUTING.md`.

## Which context each tool needs

| Tool | Required context |
| --- | --- |
| Codex | All five governance files for substantial engineering work. |
| Cursor | All five governance files for substantial engineering or testing work. |
| v0 | `AGENTS.md` constraints where relevant, `AI_PRODUCT_VISION.md`, and the applicable UI requirements. v0 does not execute the enterprise testing plan. |
| Lovable | `AI_PRODUCT_VISION.md` and the experimental boundaries in `AI_MODEL_ROUTING.md`. Lovable has no authority over production `main`. |
| Framer | Product positioning, public-facing capabilities, and brand or design requirements. Internal backend, security, and testing details are out of scope unless a specific task requires them. |
