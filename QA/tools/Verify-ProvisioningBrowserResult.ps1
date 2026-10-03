# Read-only durable acceptance on the interrupted owned fixture. No application writes.
param([Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{32}$')][string]$RunId,[Parameter(Mandatory)][ValidateRange(1024,65535)][int]$SqlPort,[Parameter(Mandatory)][ValidateRange(1024,65535)][int]$MarkerSqlPort)
$ErrorActionPreference='Stop'
$qaContainer="academydesk-qa-$RunId"
$qaRoot=Join-Path ([IO.Path]::GetTempPath()) "AcademyDesk-QA/$RunId"
$qaMarker=Join-Path $qaRoot '.qa-owner'
foreach ($qaPath in @($qaRoot,$qaMarker)) {
  if (!(Test-Path -LiteralPath $qaPath) -or ((Get-Item -LiteralPath $qaPath).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Refused fixture correction: unsafe ownership path.' }
}
$qaOwnerText=[IO.File]::ReadAllText($qaMarker)
if ($qaOwnerText -notmatch ('^'+$RunId+'\n([A-F0-9]{64})$')) { throw 'Refused fixture correction: invalid owner marker.' }
$qaDigest=$Matches[1]
$qaInspect=docker inspect $qaContainer | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $qaInspect.Count -ne 1) { throw 'Refused fixture correction: inspection failed.' }
$qaInfo=$qaInspect[0]; $qaPorts=@($qaInfo.NetworkSettings.Ports.'1433/tcp')
if ($qaInfo.Name -ne "/$qaContainer" -or $qaInfo.Config.Hostname -ne $qaContainer -or $qaInfo.Config.Image -ne 'mcr.microsoft.com/mssql/server:2022-latest' -or $qaInfo.Config.Labels.'academydesk.qa.run' -ne $RunId -or @($qaInfo.Mounts).Count -ne 0 -or $qaPorts.Count -ne 1 -or $qaPorts[0].HostIp -ne '127.0.0.1' -or [int]$qaPorts[0].HostPort -ne $SqlPort) { throw 'Refused fixture correction: container ownership mismatch.' }
$qaPasswordEntry=@($qaInfo.Config.Env | Where-Object { $_.StartsWith('MSSQL_SA_PASSWORD=') })
if ($qaPasswordEntry.Count -ne 1) { throw 'Refused fixture correction: missing owned credential.' }
$qaPassword=$qaPasswordEntry[0].Substring('MSSQL_SA_PASSWORD='.Length)
$qaSql=@'
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() <> 'AcademyDesk_QA_RUNID' OR
 (SELECT COUNT(*) FROM dbo.QaRunOwnership) <> 1 OR
 NOT EXISTS (SELECT 1 FROM dbo.QaRunOwnership WHERE RunId=CONVERT(uniqueidentifier,'RUNUUID') AND TargetAddress='127.0.0.1,MARKERPORT' AND TokenDigest='DIGEST')
 THROW 51000, 'Fixture ownership mismatch', 1;
IF (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE Email IN ('qa-ui-student@example.invalid','qa-ui-teacher@example.invalid','qa-ui-admin@example.invalid')) <> 3
 THROW 51000, 'Expected exactly three browser identities', 1;
IF (SELECT COUNT(*) FROM dbo.Academies WHERE Name='Synthetic UI Academy') <> 1
 THROW 51000, 'Expected exactly one onboarded academy', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers u JOIN dbo.Students s ON s.Id=u.StudentId AND s.AcademyId=u.AcademyId JOIN dbo.Academies a ON a.Id=u.AcademyId JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.Id JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE u.Email='qa-ui-student@example.invalid' AND r.Name='Student' AND a.Name='Synthetic Academy A' AND s.FirstName='Isolated-A' AND u.TeacherId IS NULL AND u.GuardianId IS NULL)
 THROW 51000, 'Browser student typed link or role missing', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers u JOIN dbo.Teachers t ON t.Id=u.TeacherId AND t.AcademyId=u.AcademyId JOIN dbo.Academies a ON a.Id=u.AcademyId JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.Id JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE u.Email='qa-ui-teacher@example.invalid' AND r.Name='Teacher' AND a.Name='Synthetic Academy A' AND t.FirstName='Synthetic' AND u.StudentId IS NULL AND u.GuardianId IS NULL)
 THROW 51000, 'Browser teacher typed link or role missing', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers u JOIN dbo.Academies a ON a.Id=u.AcademyId JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.Id JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE u.Email='qa-ui-admin@example.invalid' AND r.Name='AcademyAdmin' AND a.Name='Synthetic UI Academy' AND u.StudentId IS NULL AND u.TeacherId IS NULL AND u.GuardianId IS NULL)
 THROW 51000, 'Browser admin academy or role missing', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Email='qa-ui-owner@example.invalid' AND IsPlatformOwner=1 AND IsActive=1 AND AcademyId IS NULL)
 THROW 51000, 'Owner fixture correction not durable', 1;
PRINT 'PROVISIONUI INDEPENDENT SQL PASS exactly three browser-created users;expected roles/academy associations and student/teacher typed links;one unique onboarded academy;corrected synthetic owner flag persists;read-only acceptance,no audit-total claim.';
'@
$qaSql=$qaSql.Replace('RUNID',$RunId).Replace('RUNUUID',([Guid]::ParseExact($RunId,'N').ToString('D'))).Replace('MARKERPORT',"$MarkerSqlPort").Replace('DIGEST',$qaDigest)
docker exec -e "SQLCMDPASSWORD=$qaPassword" $qaContainer /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d "AcademyDesk_QA_$RunId" -Q $qaSql
if ($LASTEXITCODE -ne 0) { throw 'Independent owned SQL acceptance failed.' }
$qaPassword=$null; $qaPasswordEntry=$null; $qaInspect=$null
