# Disposable, labelled, loopback-only SQL + Azurite. Never reads Azure keys/config.
param([switch]$Browser, [int]$BrowserApiPort = 49002, [int]$BrowserWebPort = 49001, [int]$BrowserPauseChunkMs = 0, [switch]$BrowserStorageFailOnce, [switch]$BrowserLoseCompletionOnce, [switch]$BrowserRevokeAfterChunk)
$ErrorActionPreference = 'Stop'
if ($BrowserStorageFailOnce -and !$Browser) { throw 'Provider failure injection requires the guarded Browser mode.' }
if ($BrowserLoseCompletionOnce -and (!$Browser -or $BrowserStorageFailOnce)) { throw 'Completion loss requires guarded Browser mode without another fault.' }
if ($BrowserRevokeAfterChunk -and (!$Browser -or $BrowserPauseChunkMs -lt 1000 -or $BrowserStorageFailOnce -or $BrowserLoseCompletionOnce)) { throw 'Revocation requires isolated paced Browser mode.' }
if ($Browser -and ($BrowserApiPort -lt 1024 -or $BrowserApiPort -gt 65535 -or $BrowserWebPort -lt 1024 -or $BrowserWebPort -gt 65535 -or $BrowserApiPort -eq $BrowserWebPort)) { throw 'Distinct nonprivileged loopback browser ports required.' }
if ($BrowserPauseChunkMs -ne 0 -and (!$Browser -or $BrowserPauseChunkMs -lt 1000 -or $BrowserPauseChunkMs -gt 30000)) { throw 'Chunk pacing requires Browser mode and 1000–30000 ms.' }
if ((Get-PSDrive C).Free -lt 2GB) { throw 'At least 2 GiB free on C: required before owned SQL provisioning.' }
$qaRun = [Guid]::NewGuid().ToString('N')
$qaSql = "academydesk-qa-$qaRun"
$qaBlob = "academydesk-blob-qa-$qaRun"
$qaAccount = 'qa' + $qaRun.Substring(0,20)
$qaStarted = Get-Date
$env:QA_SQL_SA_PASSWORD = 'Qa!a9' + [Guid]::NewGuid().ToString('N')
$env:MSSQL_SA_PASSWORD = $env:QA_SQL_SA_PASSWORD
$env:QA_BLOB_KEY = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:AZURITE_ACCOUNTS = "${qaAccount}:$($env:QA_BLOB_KEY)"
try {
  $qaId = docker run -d --name $qaSql --hostname $qaSql --label "academydesk.qa.run=$qaRun" -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD -p 127.0.0.1::1433 mcr.microsoft.com/mssql/server:2022-latest
  if ($LASTEXITCODE -ne 0) { throw 'Failed to create owned SQL container.' }
  $qaId = docker run -d --name $qaBlob --label "academydesk.qa.run=$qaRun" -e AZURITE_ACCOUNTS -p 127.0.0.1::10000 mcr.microsoft.com/azure-storage/azurite:latest azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck
  if ($LASTEXITCODE -ne 0) { throw 'Failed to create owned Blob emulator.' }
  Write-Output "QA run=$qaRun SQL=$qaSql Blob=$qaBlob started=$($qaStarted.ToUniversalTime().ToString('o'))"
  $qaReady = $false
  for ($qaAttempt=0; $qaAttempt -lt 30; $qaAttempt++) {
    $qaSqlLogs = docker logs $qaSql 2>&1
    $qaBlobLogs = docker logs $qaBlob 2>&1
    if ($qaSqlLogs -match 'SQL Server is now ready for client connections' -and $qaBlobLogs -match 'successfully listens') { $qaReady = $true; break }
    Start-Sleep -Seconds 2
  }
  if (!$qaReady) { throw 'Owned SQL/Blob readiness failed.' }
  $qaPort = [int]((docker port $qaSql 1433/tcp).Trim().Split(':')[-1])
  $env:QA_BLOB_PORT = (docker port $qaBlob 10000/tcp).Trim().Split(':')[-1]
  $qaMode = '--blob-media'
  if ($Browser) { $qaMode = '--browser-media'; $env:QA_BROWSER_API_PORT = "$BrowserApiPort"; $env:QA_BROWSER_ORIGIN = "http://127.0.0.1:$BrowserWebPort" }
  if ($BrowserPauseChunkMs -gt 0) { $env:QA_BROWSER_PAUSE_CHUNK_MS = "$BrowserPauseChunkMs" } else { Remove-Item Env:\QA_BROWSER_PAUSE_CHUNK_MS -ErrorAction SilentlyContinue }
  if ($BrowserStorageFailOnce) { $env:QA_BROWSER_FAIL_STAGE_ONCE = '1' } else { Remove-Item Env:\QA_BROWSER_FAIL_STAGE_ONCE -ErrorAction SilentlyContinue }
  if ($BrowserLoseCompletionOnce) { $env:QA_BROWSER_LOSE_COMPLETION_ONCE = '1' } else { Remove-Item Env:\QA_BROWSER_LOSE_COMPLETION_ONCE -ErrorAction SilentlyContinue }
  if ($BrowserRevokeAfterChunk) { $env:QA_BROWSER_REVOKE_AFTER_CHUNK = '1' } else { Remove-Item Env:\QA_BROWSER_REVOKE_AFTER_CHUNK -ErrorAction SilentlyContinue }
  dotnet run --project QA/tools/SqlHarness/SqlHarness.csproj --no-build -- $qaRun $qaPort $qaSql $qaMode 2>&1 | ForEach-Object {
    $qaLine = $_.ToString()
    if ($qaLine -match '^(Runtime inventory|BROWSER |BLOB |Created run-owned|HTTP controls|Tenant controls|Negative cleanup|PASS:|Unhandled exception)' -or $qaLine -match 'Private media commit failed:' -or $_ -is [System.Management.Automation.ErrorRecord]) { Write-Output $qaLine }
  }
  $qaExit = $LASTEXITCODE
  Write-Output "QA harness exit=$qaExit elapsedSeconds=$([Math]::Round(((Get-Date)-$qaStarted).TotalSeconds,1))"
  if ($qaExit -ne 0) { throw 'Blob-media harness failed; exact owned containers retained.' }
  foreach ($qaName in @($qaSql,$qaBlob)) {
    $qaInspect = docker inspect $qaName | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $qaInspect[0].Name -ne "/$qaName" -or $qaInspect[0].Config.Labels.'academydesk.qa.run' -ne $qaRun) { throw 'Refused container cleanup: ownership mismatch.' }
    docker stop $qaName
    if ($LASTEXITCODE -ne 0) { throw 'Owned container stop failed.' }
    docker rm $qaName
    if ($LASTEXITCODE -ne 0) { throw 'Owned container removal failed.' }
  }
} finally {
  Remove-Item Env:\QA_SQL_SA_PASSWORD,Env:\MSSQL_SA_PASSWORD,Env:\QA_BLOB_KEY,Env:\QA_BLOB_PORT,Env:\AZURITE_ACCOUNTS,Env:\QA_BROWSER_API_PORT,Env:\QA_BROWSER_ORIGIN,Env:\QA_BROWSER_PAUSE_CHUNK_MS,Env:\QA_BROWSER_FAIL_STAGE_ONCE,Env:\QA_BROWSER_LOSE_COMPLETION_ONCE,Env:\QA_BROWSER_REVOKE_AFTER_CHUNK -ErrorAction SilentlyContinue
}
