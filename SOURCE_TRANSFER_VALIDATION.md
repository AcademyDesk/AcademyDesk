# Source-transfer validation — 29 October 2026

This report records preservation/portability checks, not a new combined release
qualification. The original application and Mini folders were not modified.

- Copied changed/untracked source files: 1143.
- Byte-for-byte comparison against original manifest: PASS, zero mismatches.
- Portable text comparison uses LF normalization; binary bytes stay exact.
- JSON documents parsed: 13. Python files parsed: 7.
- All copied files below 10 MiB; source delta only, not model/evidence archives.
- Local QA/EVIDENCE and operations/evidence are excluded. Historical frontend
  validation runs and generated SDK distributions are excluded.
- Source secret heuristic review: no probable live credential discovered. Two
  private-key-header matches in Academy negative scanner tests are literal test
  markers, not private keys. Credential-like literals were confined to synthetic
  QA/test fixtures or generated per-run SQL credentials. This is not a formal
  security certification or a replacement for the existing release security gate.
- Original Academy HEAD b1f8cb6 and Mini HEAD2f88069 are preserved. Accepted main
  and shared payroll integration branch are not replaced by this snapshot.

Fresh bounded deterministic checks against the transferred sources:
- Mini/Core contract/domain/platform suite:208passed, one existing FastAPI/httpx
  deprecation warning. No model loading, inference or real domain action.
- Academy selected chat/onboarding/teacher transport scripts: Node runner109pass,
  zero failed/skipped; controlled synthetic responses and existing read-only
  TypeScript dependency cache. Script-specific embedded counters are retained
  separately in the local log and are not added to the API acceptance count.

- Transferred Mini operational UI: TypeScript, lint, production boundary and
  schema-drift validation PASS using the existing read-only dependency cache.

The earlier full API3,183, SQL/browser and Mini quality records remain historical.
No full API/build/SQL suite was repeated simply to copy unchanged source.
Actual combined-source validation remains required after integration. Production
NOT READY, Mini NOT ACCEPTED. Owner stop before candidate loading stands.

See LOCAL_SOURCE_TRANSFER.md for exact integration order, policy differences,
exclusions and work-laptop/cloud continuation. Do not merge a snapshot wholesale.

## Preserved diff-hygiene findings

`git diff --cached --check` reports 13 inherited EOF-blank-line findings in copied pre-existing source.
These are recorded, not described as a passing check. This preservation checkpoint
retains the exact original bytes, including pinned vendor hashes; no application
or vendor reformat was performed. Resolve any required formatting through a
separate reviewed integration change and update its source pins. This is not a
combined-source CI or production acceptance claim.
