# Protected key-ring foundation — 2026-10-10

Scope: Academy Desk, `D:\AcademyDesk-codex-p0`, `codex/penta-search`, parent
`8303cefeb30923e3b43996b9f07d54de4c1b5d23`. Sol High / Codex.
Status: **IMPLEMENTED / TARGETED TESTED; DISABLED / NOT PRODUCTION APPROVED**.
This is the existing enterprise/security program, not a new testing strategy.

## Change and boundary

- `apps/api/Infrastructure/ProtectedKeyRingRegistration.cs` adds opt-in filesystem
  persistence with RSA certificate encryption of newly generated DataProtection keys.
- `apps/api/Program.cs` validates explicit configuration before web-root creation,
  migrations or bootstrap. Missing/false `Enabled` registers nothing and preserves
  existing local, QA and deployed behavior. No configuration is enabled in this change.
- Explicit enablement fails closed on malformed flags, missing/unprovisioned paths,
  application/web/repository/volume-root storage, detected reparse-point ancestry,
  missing password configuration/public-only/non-RSA/expired/future encryption material, or
  invalid retired certificates. RSA keys must be at least 2048 bits; at most 64
  additional decryption certificates. Loader errors do not echo paths/passwords.
- Certificates remain separate from the key directory and source tree. Private-key
  handles are loaded ephemerally and owned by the DI key-management lifecycle.
  Retired certificates decrypt only; their expiry does not prohibit recovery of old
  payloads. Active certificates must be valid when registration runs.
- The enabled namespace is `AcademyDesk:<EnvironmentName>`. Purpose isolation remains
  the existing actor/academy/conversation scope, not model-supplied authority.
  Identity opaque bearer format/lifetimes, current security-stamp checks, RBAC,
  tenant/finance/domain rules, PENTA availability, tools and approvals are unchanged.
- This does not inspect filesystem ACLs, retroactively encrypt existing plaintext
  keys, continuously monitor certificate expiry, provision storage/certificates or
  implement a Key Vault/Blob provider. It is not a substitute for those release gates.

[Microsoft configuration guidance](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)
requires explicit at-rest protection after choosing a persistence repository and
describes certificate rotation through additional decryption certificates. Encrypted
keys still require restricted write access: encryption alone cannot prevent an attacker
from inserting new keys. Verified 2026-10-10; no new package or paid resource added.

## Fresh evidence

| Evidence | Result and exact scope |
| --- | --- |
| `dotnet test ... --artifacts-path .build-check/protected-key-ring --filter FullyQualifiedName~ProtectedKeyRingTests` | 24/24 PASS: disabled/no-op, safe configuration failures, encrypted XML, independent-provider recovery of real framework access/refresh tickets and scoped PENTA state, actor/tenant/session/environment separation, certificate rotation and old-certificate removal |
| Filter `FullyQualifiedName~ProtectedKeyRingTests\|FullyQualifiedName~Penta\|FullyQualifiedName~ClassMaterialDownloadTicketTests` | 116/116 PASS, zero skipped; includes existing PENTA and private media-ticket tests. This is not the full API suite. |
| `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/penta-mini-sql --verbosity minimal` | PASS, 0 warnings / 0 errors |
| Existing `Run-ReconciledPayment.ps1 -Module PentaFoundation`, `QA_PENTA_HOST_SECURITY=1` | Original 40/40 hostile-planner host checks and foundation controls PASS, including 5/5 conversation races (201/409, no429), real Identity/HTTP/disposable SQL, mandatory audit faults and manual fallback |
| SQL receipt | Run `c156b84c05bc407ab1c81be85ac4a9a6`, loopback SQL port55884, 44.7s, 88 application +7 Identity migrations; exact owned database/login/container removed; mismatched-marker cleanup refused |
| Scoped publication checks | Eight intended files only; 13 local document targets, staged diff checks and heuristic credential/private-artifact checks PASS. Not a full secret-scanner certification. Unrelated continuity changes remain 32/28 added lines, unstaged. |

The first key-ring run was 23/24: the already-running provider retained its cached
default key after creating a replacement. The rotation test now explicitly restarts
the writer and asserts the actual replacement key ID in its protected output, then
proves old/new recovery and failure after removing old private material. No application
key-cache rule was bypassed. Active writers must not be assumed to rotate immediately.

SQL regression uses the existing isolated QA key configuration, **not** newly enabled
certificate configuration. Framework-ticket recovery is not a real HTTP login across
restarted containers. No new real inference, browser/device, full suite, Linux,
multi-replica mount or Azure validation was run. Previous receipts remain historical.
Tests ran on the current working tree, which includes preserved unrelated Mini work;
that work is not part of this commit or a claim of exact remote-release acceptance.

Local evidence: `QA/EVIDENCE/penta-protected-key-ring-host-security.log` and ignored
`.build-check/protected-key-ring` / `.build-check/penta-mini-sql` outputs. Synthetic
certificate fixtures use fresh random secrets in task-owned temp directories; validated
ownership precedes fixture cleanup. No real credentials or keys are in source/evidence.
Existing two Mini containers, main checkout and Mini project were not changed.

## Activation runbook — human/security review required

1. Select durable private storage shared only by intended replicas of the same app
   and environment. Separately provision the directory; review service-identity
   read/write ACLs, restricted certificate read permissions, backups and restore.
   Paths must be absolute, outside served/app/Git trees and free of detected links.
   Source checks do not prove protection against filesystem races or mount misconfiguration.
2. Supply server-controlled configuration through reviewed secure configuration:
   `Security:DataProtection:Enabled`, `KeyRingDirectory`, `CertificatePath`,
   `CertificatePassword`; optional `DecryptionCertificates:<index>:Path` and `Password`.
   Passwords/private certificates must never enter Git, chat, ordinary logs or model
   context. Do not place certificates inside the key ring. No ready-to-deploy secrets
   or production configuration are provided here.
3. **Before enabling**, inventory and back up existing keys, application discriminator,
   token/receipt purposes and consumers. Explicit namespace/persistence changes can
   invalidate existing access/refresh tickets and stored protected state. Do not
   auto-import, delete or replace keys. Review a compatible migration or an explicitly
   approved invalidation/re-login/state-expiry plan, including rollback. This code
   does not implement such migration or retroactive key encryption.
4. Prove encrypted new keys plus real login/refresh, private media tickets and stored
   PENTA recovery across container restarts and intended replicas. Prove current
   role/stamp/tenant revocation still wins after recovery. Verify wrong-environment
   and unauthorized access denial. Production PENTA remains separately gated.
5. Rotate by first distributing approved old+new decryption material to all replicas,
   switching the active certificate and activating new keys through a reviewed
   procedure. Restart/verify writers; do not rely on immediate cache refresh. Record
   key IDs and test new/old receipts and backups before retiring any old material.
   Keep old keys/certificates for the reviewed recoverability window; never automatically
   erase them because a certificate expired or one example ticket expired.
6. Add expiry/rotation/restore alerts, Linux/image/mount/ACL proof, independently
   reviewed operational ownership and release evidence. Test missing/unreadable
   material and rollback. Only then request explicit activation/deployment approval.

## Preserved gates / next route

No issue, legal obligation, enterprise program or Azure gate is closed. Privileged
MFA, ingress/limits, customer-data purpose/guardian/consent, retention, incident/logging,
restore and independent review remain open. Legacy Phase1 QA consistency validation
remains blocked here by missing local `observed-checks.json`; no older evidence was
copied or represented as current acceptance. Mini's saved 86/94 qualification remains
blocked; this source change does not upgrade the model.

**Next: Sol High in Academy Desk** for a bounded read-purpose/minor/prompt boundary
slice, continuing the first real read and manual/frontend QA. No project switch is
needed for these host controls. A genuine shared model/contract dependency uses the
existing `PENTA_HANDOFF_TO_MINI.md` and an explicit **SWITCH PROJECT CHECKPOINT**;
do not edit Mini or duplicate the engine here. Publish only this scope on the feature
branch; no main merge, Azure deployment or unrelated staging.
