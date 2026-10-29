# E5-R30 — local app restored; inactive compact instruction candidate

19 October2026. Continues the actual E5-R29 failures, not another identical trial.
**Development/token-cost checkpoint only; CORE-01 still BLOCKED.**
Academy `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`; Mini `D:\PENTA AI Models`,
`codex/academy-language-contract`, HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa`.
Dirty unrelated work retained, no staging/commit/push/main/deployment.

## Existing local application

No Academy dotnet/Next dev process was present and neither3000 nor5092 was listening.
Prior process exit cause remains unknown; this is a restart, not a diagnosed crash
fix. Parsed existing Development SQL settings in memory using the real connection
string setter, confirmed loopback/non-QA/non-production Academy target, no values
logged. Initial parser diagnostics failed closed before launch; PowerShell's
dictionary adaptation requires `set_ConnectionString`, not property assignment.

Restored unchanged developer API DLL (no build) on127.0.0.1:5092, process22228,
with Testing process environment, migrations false, bootstrap credentials blank,
development seeding skipped, all relevant PENTA/Mini/operational/provider flags
false. No local secret changes, credentials rotation, seeding, user provisioning,
data reset or migrations. Temporary parent environment restored after launch.
Original API SHA256 remains
`049BBA230504354EE8245F32A1FFAA01DC0218429B1F30D7F53BC2F21DEAA504`.

Existing Next frontend started hidden using installed Node/Next `dev --webpack`
on127.0.0.1:3000, parent20644/observed listener4736. No install, alternate app,
new backend or Turbopack repair claim. Logs under ignored
`.build-check/local-review-e5r30-20261019090019/`.

Observed smoke: login200, onboarding200, API health200, anonymous protected
session401. Actual developer Admin sign-in remains **unverified**. The normal
API DLL is intentionally the preserved developer baseline, not the isolated
onboarding candidate DLL; this smoke is not candidate runtime acceptance.

## Candidate, inactive by construction

Added trusted opt-in `transitions-compact-v3` to the existing prompt composer.
It is an independent, shorter general instruction, not replacements of previous
profiles or a new model/decoder. No default/configuration/factory selects it.
It has no examples, benchmark IDs, expected answers, customer data or host repairs.
Emphasizes the general distinction: existing draft uses refine unless explicit
restart; unchanged fields omitted; clear uses remove rather than instruction text.
Typed values, ambiguous references, safe partials, explicit lifecycle intent,
catalog authority and separate host validation/approval remain stated.

Existing baseline, contract-v1 and transitions-v2 instructions/hashes, payload
projection, Proposal schema, reducer, native gate, model, weights, wire shape,
host business rules/30s bound, approvals and production defaults unchanged.
Old/new qualification hashes cannot cross-admit; current profile is immutable.
This does **not** prove the model follows the shorter instruction or that its
semantic failures are repaired. No inference or training occurred in E5-R30.

## Actual tokenizer measurement

Read-only Docker exec in the exact restored PENTA baseline API, using only fixed
internal `/slots`, `/apply-template` and `/tokenize`. No generation route, model
load, container stop/start or external traffic.72 synthetic inputs from the
existing E5-R29 set; identical user payloads, different trusted system instruction.
No expected answers forwarded. Existing model/templates not rehashed by this
probe; it measures current baseline tokenizer cost, not a new model identity.

Receipt `D:\PENTA AI Models\operations\evidence\compact-tokenizer-e5r30-24fb2b758485\result.json`:

- [x] Before/after4idle native slots; baseline IDs/images/config/running states unchanged.
- [x] Every input uses **71 fewer tokens**. Total input51306→46194 over72cases,
  **9.96% input-token reduction**; largest input908→837.
- [x] All fit unchanged2048context+512output budget. No prompt truncation.
- [x] Model inference calls0/model loads0/qualification=false.
- [ ] Accuracy improvement **not measured**; response latency improvement **not measured**.

Token cost is only one contributor to E5-R29 latency. This10% input saving does not
establish that larger CPU models can meet30s, nor that2B can handle edits correctly.
Do not call it a10% faster model or a repaired engine.

## Code verification / hardware observation

- [x]378selected Python tests PASS (354retained+24new compact-profile checks),
  actual final JUnit17.65s/0failed/errors/skips, one inherited Starlette/httpx warning.
- [x]14development payloads byte-identical across profiles/no oracle packing;
  unknown/untrusted text cannot select the profile, exact input limits retained,
  six old/new qualification mismatch combinations deny before provider calls.
- [x] Existing default/contract-v1/v2 hashes and frozen acceptance files verified by
  retained tests. R29 immutable staged source/receipts remain historical, unchanged.
  New planner/prompt source gets a separate `compact-source-pins-e5r30.json`;
  do not pretend old E5-R18 or R29 pins describe these changed files.
- [x] Diff/whitespace PASS, no application code changes or SQL suite rerun.
  Prior461SQL/API+29browser evidence retained, NOT new model acceptance.
- [x] Read-only machine inventory:6CPU cores/12logical processors; NVIDIA Quadro
  T1000 reports4096MiB with installed driver610.60. This is not a validated
  inference backend, available-VRAM guarantee or proof the pinned CPU runtime
  supports CUDA. No driver/runtime/image download/build/GPU trial occurred.

Source/evidence SHA256:

| Path | SHA256 |
| --- | --- |
| Mini `src/penta_mini/operational/planner.py` | `E5ED9A952D7932D125BD86A0D8C1D64ED0BCBAA3C81CC0D3A5872E00D34D3CE8` |
| Mini `src/penta_mini/operational/prompt_profiles.py` | `334E260084CE0533D2DAA1D1282D723B8ECAFF3BF36A51B7BE3D2AEF39BF3FD3` |
| Mini `tests/test_operational_compact_profile_e5r30.py` | `DBB7991A154AA038793EC3315982FC5CA1F85ECED2D17988C47073A2FD6F3486` |
| Mini `operations/tools/measure_compact_prompt_e5r30.py` | `7150FAC10955B03C02BE29A5B7B93FC3ABE3FFE269F7B27E532D9264579D46AF` |
| Mini tokenizer `result.json` | `E97BBFD689A13C6E4355C64F5038BA27B725854EC8F69F1C67021681A06A2DBC` |
| Academy `QA/EVIDENCE/penta-compact-e5r30/python-final.xml` | `81E200AB9FD885851254344F7A199C83DEAC9D7E03A1677F8B44056C37E31C9E` |

Commands in Mini root, `PYTHONPATH=src`, `PYTHONDONTWRITEBYTECODE=1`:
E5-R29's10selected pytest suites plus `tests/test_operational_compact_profile_e5r30.py`,
`-q -p no:cacheprovider --junitxml=D:/AcademyDesk-codex-p0/QA/EVIDENCE/penta-compact-e5r30/python-final.xml`;
`python operations/tools/measure_compact_prompt_e5r30.py` (tokenizer-only).
No model trial script run or new Qualification record created.

## Next bounded route

Stay Sol High. Before another actual trial, review exact compact-profile/native
source pins and bounded compute choice. Keep30s/one-attempt/strict scoring and
memory/quarantine/restoration unchanged. More CPU threads or a GPU-capable
runtime needs an explicitly bounded changed-resource proposal; hardware presence
is not consent to new downloads, driver installation, paid Azure or model promotion.
R29's now-exposed fresh cases are regression, never relabeled unseen holdout.

Real Mini read/operational qualification, reviewed real admission and exact-model
host/browser proof remain OPEN; old read86/94 is not repaired here. Broader
families/roles/durable history/Manual/provider/privacy/devices/security/enterprise/
release and BUG-DATA-0001 remain OPEN. Production NOT READY. No automatic
project switch/handoff message, deployment or model activation.
