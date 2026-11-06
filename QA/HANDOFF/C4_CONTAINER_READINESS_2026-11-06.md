# Cloud C4 container/dependency packet — 6 November 2026

Status: CLAIMED. Base `a4ce05bee7040a836899c5077433db41bcc74e39` includes C3.2;
shared host still verified at `9dc2ccda5c6aca18d1cc33ec16976df827574533`.

Exact owned paths:

- `apps/web/Dockerfile`
- `apps/web/nginx.static.conf`
- `QA/tools/static-web-container.test.cjs`
- `QA/tools/check-web-container.sh`
- `QA/HANDOFF/C4_CONTAINER_READINESS_2026-11-06.md`
- `AI_HANDOFF.md` (additive task pointer only; integrator reconciles)

Restore/review existing static-export container source from preservation6887e6e;
verify actual output/routing prerequisites, inspect dependencies and CI read-only.
No parallel backend/API/SQL/migration changes, package/lock upgrades, new package
or image downloads, environment settings, model actions, main merge or Azure.
Only the managed local Docker socket may be inspected; preserve all existing
containers, volumes and images. Missing build images/runtime are prerequisites,
not permission to pull or to substitute a different artifact as production proof.

Validation: installed exact-lock output, meaningful route/config/build regressions,
read-only npm/NuGet audit, real isolated runtime checks only if original cached
capabilities support them. Keep all genuine evidence gaps and test failures.
