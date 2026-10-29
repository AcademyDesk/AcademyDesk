# Pinned PENTA operational UI candidate

Unchanged production source from operational-ui0.3.0-candidate.1 and sibling
shared-foundation FW1-R1. UPSTREAM.json pins the existing release manifest,
original source hashes and LF-normalized copies. Keep this vendor source immutable;
host behavior belongs in components/penta/onboarding-review-workspace.tsx and
lib/penta-onboarding-review.ts. This is a snapshot, not a cross-project symlink.

No gallery, dist, fixture, demo bootstrap, model registry or package installation.
The existing root import of penta-shared-ui/foundation.css is the same pinned
foundation version; no duplicate global stylesheet is imported. Operational CSS
is consumed by the dedicated review component. The sibling source tree preserves
the delivered relative imports; future deduplication requires a reviewed package.

Only a validated, current, authenticated read-only host projection mounts this
component. No sending, edits, confirmation, execution, history, feedback, secure
media, model selection or automatic retry is enabled. Manual and old read chat
remain independent. Unmount this route to roll back; never reset the dirty tree,
delete evidence or resend an unknown operation. Model/SQL/browser/device/release
acceptance remains separate from this source import.
