# Docker image and runtime validation

Date: 2026-10-03. Branch `cursor/docker-runtime-validation` in worktree `D:\AcademyDesk-cursor-docker`, started from `f7af512f884ca5fe8ac3ddb287c852492592e709`. Application baseline remains `535e6784e38b20fa1c9486b889544706f3c454a7`. This continues `ENTERPRISE_TESTING_ROADMAP.md` section 1. It does not replace Phase 2B issues, the test matrix, or the 1,042 API-test baseline.

No application source, test, migration, Dockerfile, or Azure configuration was changed. No images were published. No Azure deployment. The original `D:\AcademyDesk` checkout and its local `QA/EVIDENCE` were not used.

## Environment

| Item | Observed |
| --- | --- |
| Docker client / engine | 29.8.0 |
| Docker Desktop | 4.91.0, context `desktop-linux`, engine linux/amd64 |
| buildx | v0.37.0 |
| Resources | 12 CPUs, about 8 GB memory reported by the engine |
| Pre-existing containers | Left untouched: two exited SQL Server 2022 QA containers and one exited Azurite container |

A private bridge network and loopback-only published ports were used. Credentials were generated for this run, passed through a temporary env file, and were not written into the repository. Log excerpts below are redacted.

## API image

Command, from the worktree root:

```text
docker build -f apps/api/Dockerfile -t academydesk-qa-api:docker-runtime-f7af512 .
```

| Result | Detail |
| --- | --- |
| Exit | 0 |
| Duration | 83 seconds |
| Image | `academydesk-qa-api:docker-runtime-f7af512` |
| Image ID | `sha256:dd092f9786df2e248f625080fbd000ab7f7f9a1fd773f6f83ab3b44ad6759d9c` |
| Size | 460,362,966 bytes |
| Runtime | `mcr.microsoft.com/dotnet/aspnet:10.0`, port 8080, `ASPNETCORE_ENVIRONMENT=Production` |

The image was not pushed. It remains only in the local Docker image store.

## Web image

Command:

```text
docker build -f apps/web/Dockerfile --build-arg NEXT_PUBLIC_API_URL=http://127.0.0.1:18080 -t academydesk-qa-web:docker-runtime-f7af512 .
```

| Result | Detail |
| --- | --- |
| Exit | 1 |
| Duration | 100 seconds |
| Image | not created |

`npm ci` and `npm run build` inside the image succeeded. Next.js 16.3.5 compiled and generated 82 static routes. The image then failed:

```text
COPY --from=build /app/.next/standalone ./
"/app/.next/standalone": not found
```

Cause: `apps/web/next.config.ts` sets `output: "export"`. That produces a static `out` tree, not `.next/standalone`. The accepted Azure workflow builds that static export and deploys Azure Static Web Apps. It does not build `apps/web/Dockerfile`.

This was not changed. Switching Next output to `standalone`, or rewriting the web Dockerfile to serve `out/`, would change the deployment architecture. That is an escalation, not a routine Docker correction.

Because no web image exists, the web container was not started and WEB → API was not observed on a container path.

`npm` reported 6 dependency vulnerabilities (5 high, 1 critical) during `npm ci`. That audit was not remediated in this phase.

## Disposable SQL Server

Pattern reused from `QA/tools/SqlHarness`: `mcr.microsoft.com/mssql/server:2022-latest`, `ACCEPT_EULA=Y`, `MSSQL_PID=Developer`, run label `academydesk.qa.run=docker-runtime-validation`, database `AcademyDesk_QA_DockerRuntime`. The container joined the QA network as `academydesk-qa-sql-dockerruntime`. Port 1433 was published only on `127.0.0.1`. No Azure SQL and no development database was used.

SQL accepted a local login after recovery completed. An earlier login attempt during recovery failed and was not treated as a product defect.

## API runtime

Container `academydesk-qa-api-dockerruntime` used the built image, the QA network, and `127.0.0.1:18080` → `8080`.

Configuration, by key only:

- `ConnectionStrings__DefaultConnection` pointed at the disposable SQL container and `AcademyDesk_QA_DockerRuntime`
- `Database__ApplyMigrationsOnStartup=true` (same switch the Azure workflow sets)
- one loopback CORS origin
- `ASPNETCORE_ENVIRONMENT=Production`
- platform-owner bootstrap was left unset, so `ProductionIdentityBootstrapper` returned without creating an account

| Check | Result |
| --- | --- |
| Process | stayed running |
| Listener | `8080/tcp -> 127.0.0.1:18080` |
| `GET /health` | 200 `{"status":"Healthy"}` after startup |
| `POST /api/auth/login` with a non-existent synthetic account | 401, body title Unauthorized, detail Failed |
| `GET /api/academies` without a token | 401 |
| `docker restart` | process running, health 200 |
| `docker stop` | exit code 0 |

Startup logs contained EF decimal warnings for `GradingScheme.PassingPercent` and `Invoice.AdjustedAmount`, a data-protection warning that keys live in `/root/.aspnet/DataProtection-Keys` and may be unencrypted, and `Failed to determine the https port for redirect`. No connection-string value was observed in the retained log lines. HTTPS redirection did not block the HTTP health or anonymous requests on port 8080.

## Migrations

`Database:ApplyMigrationsOnStartup` applied both contexts to the disposable database.

| Check | Result |
| --- | --- |
| `__EFMigrationsHistory` rows | 89 |
| Seven known identity migration IDs present | 7 |
| Latest observed ID | `20261001152214_AddNotificationReadReceipts` |
| Identity tables present | `AspNetUsers`, `AspNetRoles`, and related Identity tables |

This shows the startup migration path ran against SQL Server 2022. It is not a full migration-upgrade, rollback, constraint, or transaction suite. Existing SqlHarness evidence remains the place those cases continue.

## Failure behaviour

| Case | Result |
| --- | --- |
| Image started with no connection string and migrations off | process stayed up; `GET /health` returned 503 |
| Migrations on and SQL host unreachable | process exited 139; logs showed `SqlException` for the unreachable server |
| SQL container stopped while API was up | health request timed out from the client instead of returning quickly |
| SQL container started again | next successful health check was 503, then after the API container was started again health returned 200 |

The first image hung `/health` while SQL was stopped. The remediated image returns 503 in about 5 seconds. See the remediation section.

## Remediation — 2026-10-03

The first web build failed because the Dockerfile copied `.next/standalone` while `next.config.ts` sets `output: "export"`. Production web hosting is that static export. The Dockerfile now copies `out/` into nginx on port 3000 and serves `route.html`. The Azure Static Web Apps workflow was not changed.

`GET /health` waits at most 5 seconds. SqlClient can block the calling thread during connect, so the check runs on a worker and the request wait is bounded separately. An unreachable server configured with a 30-second SQL connect timeout returned **503 in 5206 ms**. The same image returned **200** `{"status":"Healthy"}` against disposable SQL Server 2022.

## Web runtime

Image tag: `academydesk-qa-web:docker-runtime-f7af512`.

| Check | Result |
| --- | --- |
| `GET /` | 200 |
| `GET /login` and `/login.html` | 200 |
| `GET /certificates` and `/certificates.html` | 200 |
| Baked `http://127.0.0.1:18080` | present in three static chunks |
| `GET /api/academies` with `Origin: http://127.0.0.1:13000` | 401 and `Access-Control-Allow-Origin: http://127.0.0.1:13000` |

The browser calls the API directly. nginx does not proxy it. That matches the static-export deployment.

## Data Protection classification

Startup logs warn that keys are stored in `/root/.aspnet/DataProtection-Keys` and may be unencrypted.

- One QA container: keys last for that container only.
- A replacement container cannot read payloads protected by the previous container.
- More than one API replica is not safe until a shared persisted key ring exists.

No key ring was added. That remains a pre-Azure multi-replica requirement.

## Regression

After the health change, `dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj` reported **1042 passed, 0 failed**. The web image build ran `next build`, including TypeScript.

## Release-gate impact

`QA/09_RELEASE_GATES.md` still requires the wider staging and production gates. This slice closes the Docker image and single-container runtime check:

- API image starts, listens, and reaches SQL.
- Static-export web image starts and serves application routes.
- The static bundle targets the test API, and that origin is allowed by CORS.
- Startup migrations run.
- Health is 200 when SQL is up and 503 within about 5 seconds when it is not.

Authentication, RBAC, tenant isolation, concurrency, browser journeys, and responsive gates stay open.

Owned containers and the disposable database were removed. Images were not pushed to a registry. Pre-existing exited containers were not removed.

## Residual risks

- Data-protection keys are classified and still required before multi-replica production.
- `npm ci` reported 6 vulnerabilities (5 high, 1 critical). Not triaged.
- With migrations enabled, an unreachable database still terminates process startup.
- Decimal EF warnings remain.
- BUG-FUNC-0032 and the P0 register remain open.

DOCKER RUNTIME GATE: PASS
