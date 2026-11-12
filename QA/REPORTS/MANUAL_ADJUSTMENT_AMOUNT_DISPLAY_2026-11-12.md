# Exact Manual invoice adjustment amounts — 12 November 2026

Desktop base `af4bc7637806de8c5b2834f818beb7fdd10c7245`.

The adjustment register rounded cents and the approval queue used INR
for every adjustment. Both now show the amount with two decimal places
and the stored currency. The adjustment invoice picker shows the exact
invoice total with its stored currency. The API adds exact decimal
strings while preserving the numeric properties used by existing clients.
If the API cannot provide a valid amount and currency, the approval
button is disabled and the user sees “Amount unavailable.”

## Verification

- 31 targeted frontend checks passed, including USD 125.01, EUR 0.01,
  the maximum supported 16-digit whole amount with cents and unavailable
  amount handling. Existing approval cancellation and rejection cases passed.
- Exact-lock Next.js build, TypeScript, static export and targeted lint
  passed. Lint reported four existing warnings.
- The API and SQL harness built with zero warnings or errors.
- All 24 live browser checks and 150 protected HTTP/SQL checks passed.
  At 390 px, the live Admin flow submitted one USD 125.01 proposal and
  recovered its receipt. The Manual history and Manual approval queue
  both displayed USD 125.01. SQL verified the single pending proposal,
  its audit, and unchanged applied adjustment and payment count.
- Disposable SQL run `77b34d7b717f491882243e1a6bdd75c4`.
  The validation window `06b407153d434be1b7fbdb47d06d4499`
  removed its owned SQL resources and verified original PENTA service
  restoration and HTTP 200.

The release checker still lists six open gates. This result covers the
finance display and retained workflow checks. Critical, physical-device,
release CI and staging acceptance are still required.
