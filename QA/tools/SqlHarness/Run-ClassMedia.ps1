# Fresh, labelled loopback SQL; contains only synthetic QA data.
# Failure retains the exact container for investigation, never touches dev/Azure SQL.
$qaRun = [Guid]::NewGuid().ToString('N')
$qaContainer = "academydesk-qa-$qaRun"
$env:QA_SQL_SA_PASSWORD = 'Qa!a9' + [Guid]::NewGuid().ToString('N')
$env:MSSQL_SA_PASSWORD = $env:QA_SQL_SA_PASSWORD
$qaStarted = Get-Date
$qaDockerId = docker run -d --name $qaContainer --hostname $qaContainer --label "academydesk.qa.run=$qaRun" -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD -p 127.0.0.1::1433 mcr.microsoft.com/mssql/server:2022-latest
if ($LASTEXITCODE -ne 0) { throw 'Could not create isolated SQL container.' }
Write-Output "QA run=$qaRun container=$qaContainer started=$($qaStarted.ToUniversalTime().ToString('o'))"
$qaReady = $false
for ($qaAttempt=0; $qaAttempt -lt 30; $qaAttempt++) {
  $qaLogs = docker logs $qaContainer 2>&1
  if ($qaLogs -match 'SQL Server is now ready for client connections') { $qaReady = $true; break }
  Start-Sleep -Seconds 2
}
if (!$qaReady) { throw "SQL readiness failed; retained $qaContainer" }
$qaPort = [int]((docker port $qaContainer 1433/tcp).Trim().Split(':')[-1])
Write-Output "QA SQL port=$qaPort"
dotnet run --project QA/tools/SqlHarness/SqlHarness.csproj --no-build -- $qaRun $qaPort $qaContainer --class-media 2>&1 | ForEach-Object {
  $qaLine = $_.ToString()
  if ($qaLine -match '^(Runtime inventory|MEDIA REGRESSION|Created run-owned|HTTP controls|Tenant controls|SECURITY-FILE|FINANCE-RULE|PAYROLL CASE|PAYROLL-RULE|TRANSITION |Negative cleanup|PASS:|Unhandled exception)' -or $_ -is [System.Management.Automation.ErrorRecord]) { Write-Output $qaLine }
}
$qaExit = $LASTEXITCODE
Write-Output "QA harness exit=$qaExit elapsedSeconds=$([Math]::Round(((Get-Date)-$qaStarted).TotalSeconds,1))"
if ($qaExit -eq 0) {
  $qaInspect = docker inspect $qaContainer | ConvertFrom-Json
  if ($qaInspect[0].Name -ne "/$qaContainer" -or $qaInspect[0].Config.Labels.'academydesk.qa.run' -ne $qaRun) { throw 'Refused container cleanup: ownership mismatch.' }
  docker stop $qaContainer
  docker rm $qaContainer
} else { Write-Output "Retained failed isolated container $qaContainer" }
Remove-Item Env:\QA_SQL_SA_PASSWORD,Env:\MSSQL_SA_PASSWORD
if ($qaExit -ne 0) { throw 'Class-media regression infrastructure failed.' }
