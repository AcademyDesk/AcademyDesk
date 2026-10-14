# PENTA shared UI — pinned Academy vendor snapshot

Source `frontend/shared-foundation/src` from the FW1-R1 candidate, package0.1.0-fw1.r1/token1.0.0-fw1.r1. `UPSTREAM.json` records hashes, archive identity and exclusions. This is a controlled versioned vendor import, not a live cross-project link. Treat these six source files as immutable; host adaptations belong in `components/penta-manual-theme.*`. Future updates need a new reviewed manifest and affected regressions. No registry/dependency install or main node_modules mutation is needed.

Only Manual roots opt in to `penta-ui pui-academy-mapping`. Host ThemeProvider selects Prism light or Orbit dark with the separately scoped Aura accent treatment. Login and current AI pilot are not wrapped. CSS imported once in root layout; all vendor selectors remain under penta-ui. No prototype bootstrap, demo CSS, synthetic records, model choices, credentials or host transport is imported. Demo-oriented Confirmation/DateField/stateCopy are not used by production forms; existing handlers/date/time/financial/context/approval logic stays authoritative.

Rollback: remove the host theme props/classes and the two vendor CSS imports, retaining the previous Manual module/handlers. Do not reset the dirty worktree, mutate SQL or change model/security settings. No package is published or production acceptance claimed.
