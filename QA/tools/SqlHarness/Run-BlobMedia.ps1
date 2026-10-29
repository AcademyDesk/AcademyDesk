# Disposable, labelled, loopback-only SQL + Azurite. Never reads Azure keys/config.
param([switch]$Browser, [int]$BrowserApiPort = 49002, [int]$BrowserWebPort = 49001, [int]$BrowserPauseChunkMs = 0, [switch]$BrowserStorageFailOnce, [switch]$BrowserLoseCompletionOnce, [switch]$BrowserRevokeAfterChunk, [switch]$Lifecycle, [switch]$LargeTransfer, [switch]$RecorderBrowser, [switch]$RecorderRecovery, [switch]$RecorderUpload, [switch]$AdmissionReview, [switch]$PublicationGuard)
$ErrorActionPreference = 'Stop'
if ($env:QA_MEDIA_PUBLICATION_GUARD -or ($PublicationGuard -and $AdmissionReview)) { throw 'Use PublicationGuard alone without inherited or duplicate modes.' }
if ($PublicationGuard) { $AdmissionReview=$true }
if ($env:QA_MEDIA_ADMISSION_REVIEW -or ($AdmissionReview -and ($Browser -or $Lifecycle -or $LargeTransfer -or $RecorderBrowser -or $RecorderRecovery -or $RecorderUpload -or $BrowserPauseChunkMs -ne 0 -or $BrowserStorageFailOnce -or $BrowserLoseCompletionOnce -or $BrowserRevokeAfterChunk -or $env:QA_MEDIA_LIFECYCLE -or $env:QA_MEDIA_LARGE_TRANSFER -or $env:QA_MEDIA_RECORDER_BROWSER -or $env:QA_MEDIA_RECORDER_RECOVERY -or $env:QA_MEDIA_RECORDER_UPLOAD))) { throw 'Use AdmissionReview alone without inherited or mixed modes.' }
if ($env:QA_MEDIA_RECORDER_UPLOAD -or ($RecorderUpload -and ($RecorderRecovery -or $RecorderBrowser))) { throw 'Use RecorderUpload alone; inherited or duplicate recorder modes are refused.' }
if ($RecorderUpload) { $RecorderBrowser = $true }
if ($env:QA_MEDIA_RECORDER_RECOVERY -or ($RecorderRecovery -and $RecorderBrowser)) { throw 'Use RecorderRecovery alone; inherited or duplicate recorder modes are refused.' }
if ($RecorderRecovery) { $RecorderBrowser = $true }
$qaRepo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$qaAdmissionDll = Join-Path $qaRepo '.build-check/media-admission-sql/SqlHarness.dll'
if ($PublicationGuard) { $qaAdmissionDll = Join-Path $qaRepo '.build-check/media-publication-sql/SqlHarness.dll' }
if ($AdmissionReview -and !(Test-Path -LiteralPath $qaAdmissionDll)) { throw 'Build the media-admission-sql harness before the guarded review.' }
$qaRecorderDll = Join-Path $qaRepo '.build-check/media-recorder-sql/SqlHarness.dll'
if ($env:QA_MEDIA_RECORDER_BROWSER -or ($RecorderBrowser -and ($LargeTransfer -or $Lifecycle -or $Browser -or $BrowserPauseChunkMs -ne 0 -or $BrowserStorageFailOnce -or $BrowserLoseCompletionOnce -or $BrowserRevokeAfterChunk -or $env:QA_MEDIA_LIFECYCLE -or $env:QA_MEDIA_LARGE_TRANSFER))) { throw 'Use RecorderBrowser only for the isolated recorder gate without inherited or mixed modes.' }
if ($RecorderBrowser -and !(Test-Path -LiteralPath $qaRecorderDll)) { throw 'Build the media-recorder-sql harness before the guarded run.' }
$qaLifecycleDll = Join-Path $qaRepo '.build-check/media-lifecycle-sql/SqlHarness.dll'
$qaLargeDll = Join-Path $qaRepo '.build-check/media-large-sql/SqlHarness.dll'
if ($env:QA_MEDIA_LARGE_TRANSFER -or ($LargeTransfer -and ($Lifecycle -or $Browser -or $BrowserPauseChunkMs -ne 0 -or $BrowserStorageFailOnce -or $BrowserLoseCompletionOnce -or $BrowserRevokeAfterChunk))) { throw 'Use LargeTransfer only for the isolated native Blob gate, without lifecycle/browser faults or inherited large mode.' }
if ($LargeTransfer -and !(Test-Path -LiteralPath $qaLargeDll)) { throw 'Build the media-large-sql harness before the guarded run.' }
if ($env:QA_MEDIA_LIFECYCLE -or ($Lifecycle -and ($Browser -or $BrowserPauseChunkMs -ne 0 -or $BrowserStorageFailOnce -or $BrowserLoseCompletionOnce -or $BrowserRevokeAfterChunk))) { throw 'Use Lifecycle only for the isolated native Blob gate, without browser faults or inherited lifecycle mode.' }
if ($Lifecycle -and !(Test-Path -LiteralPath $qaLifecycleDll)) { throw 'Build the media-lifecycle-sql harness before the guarded run.' }
if ($BrowserStorageFailOnce -and !$Browser) { throw 'Provider failure injection requires the guarded Browser mode.' }
if ($BrowserLoseCompletionOnce -and (!$Browser -or $BrowserStorageFailOnce)) { throw 'Completion loss requires guarded Browser mode without another fault.' }
if ($BrowserRevokeAfterChunk -and (!$Browser -or $BrowserPauseChunkMs -lt 1000 -or $BrowserStorageFailOnce -or $BrowserLoseCompletionOnce)) { throw 'Revocation requires isolated paced Browser mode.' }
if ($Browser -and ($BrowserApiPort -lt 1024 -or $BrowserApiPort -gt 65535 -or $BrowserWebPort -lt 1024 -or $BrowserWebPort -gt 65535 -or $BrowserApiPort -eq $BrowserWebPort)) { throw 'Distinct nonprivileged loopback browser ports required.' }
if ($BrowserPauseChunkMs -ne 0 -and (!$Browser -or $BrowserPauseChunkMs -lt 1000 -or $BrowserPauseChunkMs -gt 30000)) { throw 'Chunk pacing requires Browser mode and 1000–30000 ms.' }
if ((Get-PSDrive C).Free -lt 2GB) { throw 'At least 2 GiB free on C: required before owned SQL provisioning.' }
if ($LargeTransfer -and (Get-PSDrive C).Free -lt 8GB) { throw 'At least 8 GiB free on C: required for maximum-size local media testing.' }
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
    if ($qaSqlLogs -match 'SQL Server is now ready for client connections' -and $qaBlobLogs -match 'successfully listens') {
      $qaProbeErrorAction=$ErrorActionPreference
      try {
        $ErrorActionPreference='Continue'
        docker exec $qaSql /bin/bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -Q "SELECT 1"' 2>&1 | Out-Null
        $qaProbeExit=$LASTEXITCODE
      } finally { $ErrorActionPreference=$qaProbeErrorAction }
      if ($qaProbeExit -eq 0) { $qaReady=$true; break }
    }
    Start-Sleep -Seconds 2
  }
  if (!$qaReady) { throw 'Owned SQL/Blob readiness failed.' }
  if ($LargeTransfer) {
    $qaDf = @(docker exec $qaBlob df -Pk /opt/azurite)
    if ($LASTEXITCODE -ne 0 -or $qaDf.Count -lt 2) { throw 'Owned Blob filesystem capacity probe failed.' }
    $qaFreeKiB = [long](($qaDf[-1].Trim() -split '\s+')[3])
    if ($qaFreeKiB -lt 8*1024*1024) { throw 'At least 8 GiB free inside the owned Blob filesystem required.' }
    Write-Output 'BLOB LARGE capacity PASS: host and owned Blob filesystem each have at least8GiB free.'
  }
  $qaPort = [int]((docker port $qaSql 1433/tcp).Trim().Split(':')[-1])
  $env:QA_BLOB_PORT = (docker port $qaBlob 10000/tcp).Trim().Split(':')[-1]
  $qaMode = '--blob-media'
  if ($Browser) { $qaMode = '--browser-media'; $env:QA_BROWSER_API_PORT = "$BrowserApiPort"; $env:QA_BROWSER_ORIGIN = "http://127.0.0.1:$BrowserWebPort" }
  if ($BrowserPauseChunkMs -gt 0) { $env:QA_BROWSER_PAUSE_CHUNK_MS = "$BrowserPauseChunkMs" } else { Remove-Item Env:\QA_BROWSER_PAUSE_CHUNK_MS -ErrorAction SilentlyContinue }
  if ($BrowserStorageFailOnce) { $env:QA_BROWSER_FAIL_STAGE_ONCE = '1' } else { Remove-Item Env:\QA_BROWSER_FAIL_STAGE_ONCE -ErrorAction SilentlyContinue }
  if ($BrowserLoseCompletionOnce) { $env:QA_BROWSER_LOSE_COMPLETION_ONCE = '1' } else { Remove-Item Env:\QA_BROWSER_LOSE_COMPLETION_ONCE -ErrorAction SilentlyContinue }
  if ($BrowserRevokeAfterChunk) { $env:QA_BROWSER_REVOKE_AFTER_CHUNK = '1' } else { Remove-Item Env:\QA_BROWSER_REVOKE_AFTER_CHUNK -ErrorAction SilentlyContinue }
  $qaDotnetArgs = @('run','--project','QA/tools/SqlHarness/SqlHarness.csproj','--no-build','--',$qaRun,$qaPort,$qaSql,$qaMode)
  if ($Lifecycle) { $env:QA_MEDIA_LIFECYCLE='1'; $qaDotnetArgs=@($qaLifecycleDll,$qaRun,$qaPort,$qaSql,$qaMode) }
  if ($LargeTransfer) { $env:QA_MEDIA_LARGE_TRANSFER='1'; $qaDotnetArgs=@($qaLargeDll,$qaRun,$qaPort,$qaSql,$qaMode) }
  if ($RecorderBrowser) { $env:QA_MEDIA_RECORDER_BROWSER='1'; $qaDotnetArgs=@($qaRecorderDll,$qaRun,$qaPort,$qaSql,$qaMode) }
  if ($RecorderRecovery) { $env:QA_MEDIA_RECORDER_RECOVERY='1' }
  if ($RecorderUpload) { $env:QA_MEDIA_RECORDER_UPLOAD='1' }
  if ($AdmissionReview) { $env:QA_MEDIA_ADMISSION_REVIEW='1'; $qaDotnetArgs=@($qaAdmissionDll,$qaRun,$qaPort,$qaSql,$qaMode) }
  if ($PublicationGuard) { $env:QA_MEDIA_PUBLICATION_GUARD='1' }
  dotnet @qaDotnetArgs 2>&1 | ForEach-Object {
    $qaLine = $_.ToString()
    if ($qaLine -match '^(Runtime inventory|BROWSER |BLOB |Created run-owned|HTTP controls|Tenant controls|Negative cleanup|PASS:|Unhandled exception)' -or $qaLine -match 'Private media commit failed:' -or $_ -is [System.Management.Automation.ErrorRecord]) { Write-Output $qaLine }
  }
  $qaExit = $LASTEXITCODE
  Write-Output "QA harness exit=$qaExit elapsedSeconds=$([Math]::Round(((Get-Date)-$qaStarted).TotalSeconds,1))"
  if ($qaExit -ne 0) { throw 'Blob-media harness failed; exact owned containers retained.' }
  foreach ($qaName in @($qaSql,$qaBlob)) {
    $qaInspect = docker inspect $qaName | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $qaInspect[0].Name -ne "/$qaName" -or $qaInspect[0].Config.Labels.'academydesk.qa.run' -ne $qaRun) { throw 'Refused container cleanup: ownership mismatch.' }
    $qaPortKey = '1433/tcp'; $qaExpectedPort = "$qaPort"
    if ($qaName -eq $qaBlob) { $qaPortKey='10000/tcp'; $qaExpectedPort=$env:QA_BLOB_PORT }
    $qaBindings = @($qaInspect[0].NetworkSettings.Ports.$qaPortKey)
    if ($qaBindings.Count -ne 1 -or $qaBindings[0].HostIp -ne '127.0.0.1' -or $qaBindings[0].HostPort -ne $qaExpectedPort) { throw 'Refused container cleanup: loopback port mismatch.' }
    docker stop $qaName
    if ($LASTEXITCODE -ne 0) { throw 'Owned container stop failed.' }
    docker rm $qaName
    if ($LASTEXITCODE -ne 0) { throw 'Owned container removal failed.' }
  }
} finally {
  Remove-Item Env:\QA_MEDIA_PUBLICATION_GUARD -ErrorAction SilentlyContinue
  Remove-Item Env:\QA_MEDIA_ADMISSION_REVIEW -ErrorAction SilentlyContinue
  Remove-Item Env:\QA_MEDIA_RECORDER_UPLOAD -ErrorAction SilentlyContinue
  Remove-Item Env:\QA_MEDIA_RECORDER_RECOVERY -ErrorAction SilentlyContinue
  Remove-Item Env:\QA_MEDIA_RECORDER_BROWSER -ErrorAction SilentlyContinue
  Remove-Item Env:\QA_SQL_SA_PASSWORD,Env:\MSSQL_SA_PASSWORD,Env:\QA_BLOB_KEY,Env:\QA_BLOB_PORT,Env:\AZURITE_ACCOUNTS,Env:\QA_BROWSER_API_PORT,Env:\QA_BROWSER_ORIGIN,Env:\QA_BROWSER_PAUSE_CHUNK_MS,Env:\QA_BROWSER_FAIL_STAGE_ONCE,Env:\QA_BROWSER_LOSE_COMPLETION_ONCE,Env:\QA_BROWSER_REVOKE_AFTER_CHUNK,Env:\QA_MEDIA_LIFECYCLE,Env:\QA_MEDIA_LARGE_TRANSFER -ErrorAction SilentlyContinue
}
