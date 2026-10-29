# E5-R2 — paired PENTA payload binding, not operational activation

17 October 2026. Owner authorizes continuing both local projects from this chat. Academy remains `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`; PENTA remains `D:\PENTA AI Models`, `codex/academy-language-contract`, base `2f8806967709a1a7ec29e5176e7c31968adbf6aa` plus preserved work. No main edits, stage/commit/push/merge/Azure, production data or permission changes.

## Return reviewed and limitation preserved

Reviewed PENTA's current `operations/OPERATIONAL_RETURN.md`, candidate.5 manifest, SDK/Core/runtime/strict contracts, integration/conformance instructions, controlled frontend README and production-readiness record. The preceding read-only review matched147 source/archive/dependency/coverage hash entries and57 frontend/evidence entries; all77 referenced Academy controller hashes matched. Actual saved JUnit contains606 tests, zero failures/errors/skips; those606 were NOT rerun here. The317 source actions/14 families are an inventory, with zero newly engine-qualified or host-accepted bindings. Existing15 exclusions remain.

CORE-01 remains blocked: no accepted operational interpreter; saved failed/partial language results and resource deferrals remain. Do not repeat unchanged local starts/benchmarks, alter memory guards or promote a model. The current0.1 serving/read tools remain unchanged. Other PENTA components are contract-tested candidates, not a full operational app. Owner permits local cross-project work now, not deployment/model acquisition/resource purchases.

## Implemented host code

Added `PentaOperationalPayload` to Academy's existing backend. The inactive `operational-draft-0.3.0` adapter validates a closed `{tool_key,tool_revision,fields}` envelope, strict typed values, duplicate/unknown keys and bounded field/row/column/text/input sizes. Numbers for decimal values remain exact strings; integer values and host catalog revisions stay within the interoperable safe-integer bound. It rejects invalid dates/references, nested rows, null/coerced values and malformed Unicode.

Canonicalization matches PENTA Python `json.dumps(sort_keys=True,separators=(',',':'),ensure_ascii=True,allow_nan=False)` for admitted payloads, rather than relying on .NET's different default escaping. Unicode is not normalized, decimal string scale and row order remain significant, and object property order/JSON whitespace do not change the digest. Constant-time digest comparison rejects malformed/non-lowercase hashes and invalid payloads without echoing data.

This is deliberately a **payload binding primitive only**. It does not validate a permitted domain catalog, resolve identity/references, detect all secrets, grant permission, approve anything or execute a tool. Catalog/revision bounds, current authority, complete domain rules, duplicate resolution, host-owned draft/approval/transaction/audit and receipt checks must surround it. No DI registration, HTTP endpoint, new write tool or engine replacement was added. Existing synthetic approvals remain synthetic; current read allowlist remains two tools. No database migration or storage write was performed.

## Actual validation

- API/test project builds passed as part of `dotnet test --no-restore`, no compile errors or warnings emitted.
- Final **125/125 PENTA-focused .NET tests PASS**, zero failed/skipped:47 new payload cases and78 retained tests. Receipt `QA/EVIDENCE/penta-operational-payload-20261017/penta-contract-regression.trx`. Earlier41-case receipt is retained, not the final count.
- **5/5 cross-language vectors PASS against the actual existing Python SDK** (`TypeAdapter(Value)` plus its `digest`), using `python -B` and process-only PYTHONPATH. No engine source/file/cache changes or model inference. Vectors cover all scalar kinds, Indian-language Unicode/emoji/control escaping, exact decimals, row arrays and empty values. Python-produced canonical strings and expected SHA256 values are pinned in the Academy fixture.
- Negative .NET cases cover duplicate/envelope/typed keys, unknown values, coercions, unsafe/floating integers, decimal spelling/precision, bad dates/references, malformed surrogates, nested/oversized collections and text, missing/wrong hashes and active READ-only boundary retention. Changing tool, revision, canonical values, decimal scale or row order changes the binding.
- No SQL/browser/provider/device/model tests were run: this helper has no persistence, transport or frontend path yet. Prior enterprise evidence is retained, not claimed to validate a newly activated operation.

Source SHA256: host helper `BA2213352C8EEB14325AF3D5AD026C914FCBC71B92AB3815B1751DFF0E93B60C`; tests `512FFA4302EC479030CD16D8D203C3A256DBC5E3ECC09D11C9332CA54D7DFE6F`; vector fixture `3376B53E4F8D1B34C9A980516465097094FC0EAF2F338213242B14FF5B4B19F6`. The test project copies only that fixture to its output. No unrelated source edits.

## Dev environment / next actual work

Read-only startup checks confirm local SQL service running, all three required development-seed passwords configured without printing values, and Development DefaultConnection resolves to a local SQL alias (not an Azure suffix). Initial generic PowerShell dictionary access misclassified the target as unverified; using the actual connection-string property methods resolved the check. No connection-string/password/configuration change occurred. API5092/frontend3000 were not listening; Mini8000 was listening. This does not establish working login, applied migrations, runtime keys or linked qualified AI. No login URL/password is claimed ready by this checkpoint.

- [x] New return verified; SDK support vs actual engine/host acceptance separated.
- [x] First shared .NET operational payload binding and paired Python compatibility proof implemented.
- [ ] Next: strict remaining host operational DTO/catalog/reference bindings, current authorization and protected draft/approval/status persistence using existing domain services, followed by real SQL/HTTP/browser verticals.
- [ ] Stable local startup with checked local database/migrations and a verified secure test login; then authenticated controlled AI frontend adapters.
- [ ] Qualified private Mini remains an independent blocker before actual operational interpretation/activation. Continue safe host work without pretending synthetic plans qualify the engine.

Sol High remains the repository-guided route for consequential contract/security/domain integration; this statement does not switch models. No project-to-project messaging or subagents started. Full agreed AI-first scope and existing enterprise/release gates remain open; Manual stays functional and its cosmetic migration remains deferred.
