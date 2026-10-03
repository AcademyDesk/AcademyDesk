# Local development credentials

The API seeds sample identities only when `ASPNETCORE_ENVIRONMENT=Development`.
It requires three password settings before startup can migrate, bootstrap, or
seed any data:

| Configuration key | Environment variable | Purpose |
| --- | --- | --- |
| `DevelopmentSeed:DefaultPassword` | `DevelopmentSeed__DefaultPassword` | Sample finance, student, and teacher accounts |
| `DevelopmentSeed:PlatformOwnerPassword` | `DevelopmentSeed__PlatformOwnerPassword` | Local Platform Owner sample account |
| `DevelopmentSeed:AcademyAdminPassword` | `DevelopmentSeed__AcademyAdminPassword` | Local Academy Admin sample account |

On this development laptop, the previous development values have been moved
to .NET user-secrets for `apps/api/AcademyDesk.Api.csproj`; no values are in
tracked configuration. On a new machine, set your own strong, disposable
values from a private PowerShell session. This passes JSON on standard input,
so the values do not appear in command history or command arguments:

```powershell
$developmentSecrets = @{}
foreach ($key in @('DevelopmentSeed:DefaultPassword', 'DevelopmentSeed:PlatformOwnerPassword', 'DevelopmentSeed:AcademyAdminPassword')) {
    $secure = Read-Host -Prompt $key -AsSecureString
    $developmentSecrets[$key] = [System.Net.NetworkCredential]::new('', $secure).Password
}
$developmentSecrets | ConvertTo-Json -Compress | dotnet user-secrets set --project apps/api/AcademyDesk.Api.csproj
$developmentSecrets.Clear()
Remove-Variable developmentSecrets, secure
```

PowerShell prompts for each value. Keep the password out of the command line,
source files, logs, and screenshots. User-secrets are local development
storage outside the repository, not a production secret store. The seeder
resets the two named privileged sample accounts on each Development startup;
choose values appropriate for disposable local accounts. If any previously
committed sample value was reused elsewhere, rotate that account separately.

The normal API Docker image sets `ASPNETCORE_ENVIRONMENT=Production` and does
not invoke development seeding. An intentionally Development-mode container
does not automatically receive host user-secrets: provide the three settings
from a private local environment or secret mount. For example, `docker run`
can forward already-set host variables with `--env DevelopmentSeed__DefaultPassword`
and the two other names above. It also needs the usual database settings.
Do not bake these values into the image or a tracked Compose file.

The existing isolated QA host runs in `Testing`, creates its own synthetic
accounts, and does not depend on these values. A CI job that explicitly boots
the API in `Development` must inject its own disposable values as CI secrets;
the production deployment job needs none of the `DevelopmentSeed__*` values.
If any required value is absent, Development startup fails with the missing
configuration key name and does not print a password.
