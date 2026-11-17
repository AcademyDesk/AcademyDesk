# C50 — broad cloud regression batch

Cloud / Sol High, 17 November 2026, Asia/Calcutta.
Branch: `codex/cloud-c50-regression-batch-20261117`.
Clean base: current Desktop
`2b286995d47895403542961e9463739ebb8c1298`, freshly fetched.

## Ownership before integration and testing

Only these paths may change:

- `apps/web/src/components/private-material-actions.tsx`: reviewed C49 bytes.
- `QA/tools/private-material-actions-browser.cjs`: reviewed C49 browser runner.
- `QA/HANDOFF/C49_PRIVATE_PREVIEW_SCOPE_2026-11-17.md`: original C49 handoff.
- this C50 task-specific result/handoff.

Integrate C49 final commit `ed252d3d631b85fc1374fa2e6e19f4983716e3a0`
into the newer Desktop source. C48 is already integrated there. No new
application behavior or test weakening is planned. Preserve prior branches,
dirty work, raw evidence and all current source/lock/model/vendor boundaries.

The user explicitly requested a large test batch. Run fresh full backend and
frontend suites, types/lint, exact-lock production build, dependency advisory
checks and existing release guards. Run all20 available self-contained CDP
browser suites in two concurrent lanes against the exact built source, with
owned loopback fixtures/profiles. Preserve each previous evidence directory
before execution and collect every suite result, including failures.
Mini deterministic checks remain no-model; existing original-evidence failures
must stay visible and separate from application failures.

Historical Windows/session runners that require absent protected SQL, original
fixture ownership or source-bound receipts must remain not-run prerequisites.
No substitute fixture may be labelled as their real acceptance. Desktop owns
the protected native SQL, physical devices, staging and private-model work.

No main merge, Azure deployment, production database access, environment
publication, public-access change or model download/loading/training/inference/
activation. Mini/Plus/Pro remain unqualified; Ultra deferred;90% is unmeasured.
All six critical release acceptance records remain open.

Results will be recorded after the complete batch.
