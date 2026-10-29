# Activity read/filter lifecycle —21 October2026

Continues QUALITY-LINT-001 on `codex/penta-search`,unchangedHEADb1f8cb6.
No backend,tenant/role,domain,retention or audit-write policy change.

- Replaced render-time Date.now with read/event/minute snapshots and cleanup.
  No background API polling; live rolling-window snapshots pause while hidden.
- Date filter now uses the existing displayed Asia/Kolkata timezone, explicitly
  labelled IST. SQL-backed suffixless OccurredAtUtc is interpreted as UTC,not as
  the device zone; explicit Z/offset supported. Invalid wire shapes/timestamps
  produce a failed-read state,not a render exception or false empty register.
- Preserved existing rolling24-hour/7-day/30-day durations. The old “Today” label
  now says “Last24hours” rather than implying a civil-day boundary. Future rows
  remain in All time but do not qualify as past-window activity.
- Added guarded abort/unmount reads,filter labels/disabled unavailable controls
  and a clear distinction between unavailable and verified empty activity.
  Metadata remains React text,not injected markup. Server authorization unchanged.

## Validation

-30actual-helper/component checks PASS;combined related session/login/workspace/
  practice/date/theme/activity suite **259PASS**,0fail/skip/cancel.
  `.build-check/session-resume/continuation-activity-final.log`.
- TypeScript and targeted lint PASS;87-route webpack export PASS.
- **40/40 actual exported-page browser cases PASS**,320/1440px,light/dark,
  Asia/Kolkata/America/Los_Angeles. Cases:populated,empty,denied,malformed,
  malformed timestamp. Search/type/time/date/year selection,metadata text safety,
  clear,scroll reachability,zero writes/pageerrors/horizontal overflow checked.
  Receipt:`QA/EVIDENCE/activity-view-1792580916443/result.json`.
- Initial browser receipt `activity-view-1792580862215` retained:20cases passed,
  first desktop case failed the fixture's document-scroll assumption. Desktop
  intentionally scrolls a workspace region. Oracle now requires actual last-row
  reachability and a scrolled ancestor;no product scroll rule or assertion removed.
- Full configured lint after Activity:**18errors/40warnings**,still FAIL.
  `.build-check/session-resume/lint-after-activity.json`;no rules/config suppressed.
  Courses/Batches continuation has its own subsequent counts and acceptance.

Actual APIs/credentials were synthetic;not native SQL/role/tenant/physical-device
acceptance or an all-screen audit. No issue or enterprise/release gate closed.
No main/stage/commit/push/Azure/model/training change.

Fingerprints(SHA256):page43645187bf6a1b7eb6814baa53f32ea07bf15409d33c190c82e0a6d3a07f7998;
helperaf389593e8095e1ba2dfa157ac11b98b7c4177898d51849f4ba336c7ea330f1a;
browserreceipt51b27fbbb100bdca41360f0d55c4cccea0c27f22e8bf6646d00f0ac60b921f95.

Next Medium:settled catalogue read-lifecycle/quality work. High:remaining
qualified-model/host/domain/security gates;all established release gates retained.
