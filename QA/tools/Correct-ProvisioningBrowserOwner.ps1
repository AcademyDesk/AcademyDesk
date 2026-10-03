# Correct the missing owner flag on ONE disposable browser fixture, never a development/customer identity.
param([Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{32}$')][string]$RunId,[Parameter(Mandatory)][ValidateRange(1024,65535)][int]$SqlPort)
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
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON;
SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() <> 'AcademyDesk_QA_RUNID' OR
  (SELECT COUNT(*) FROM dbo.QaRunOwnership) <> 1 OR
  NOT EXISTS (SELECT 1 FROM dbo.QaRunOwnership WHERE RunId=CONVERT(uniqueidentifier,'RUNUUID') AND TargetAddress='127.0.0.1,SQLPORT' AND TokenDigest='DIGEST')
  THROW 51000, 'Fixture ownership mismatch', 1;
BEGIN TRANSACTION;
IF (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE Email='qa-ui-owner@example.invalid' AND UserName='qa-ui-owner@example.invalid' AND DisplayName='Synthetic UI Owner' AND AcademyId IS NULL AND IsActive=1 AND IsPlatformOwner=0) <> 1
  THROW 51000, 'Exact synthetic owner fixture mismatch', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers u JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.Id JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE u.Email='qa-ui-owner@example.invalid' AND r.Name='PlatformOwner')
  THROW 51000, 'Synthetic owner role missing', 1;
UPDATE dbo.AspNetUsers SET IsPlatformOwner=1 WHERE Email='qa-ui-owner@example.invalid' AND UserName='qa-ui-owner@example.invalid' AND IsPlatformOwner=0;
IF @@ROWCOUNT <> 1 THROW 51000, 'Fixture update count mismatch', 1;
COMMIT;
PRINT 'PROVISIONUI FIXTURE PASS owner flag0->1;exactly one owned synthetic identity,existing role preserved;new login required,no production change.';
'@
$qaSql=$qaSql.Replace('RUNID',$RunId).Replace('RUNUUID',([Guid]::ParseExact($RunId,'N').ToString('D'))).Replace('SQLPORT',"$SqlPort").Replace('DIGEST',$qaDigest)
docker exec -e "SQLCMDPASSWORD=$qaPassword" $qaContainer /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d "AcademyDesk_QA_$RunId" -Q $qaSql
if ($LASTEXITCODE -ne 0) { throw 'Owned fixture correction failed; transaction requires inspection.' }
$qaPassword=$null; $qaPasswordEntry=$null; $qaInspect=$null
