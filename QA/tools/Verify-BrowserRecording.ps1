# Read-only assertions against the exact labelled disposable recording run, never Azure.
param([Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$RunId)
$ErrorActionPreference = 'Stop'
$qaContainer = "academydesk-qa-$RunId"
$qaDatabase = "AcademyDesk_QA_$RunId"
$qaInspect = docker inspect $qaContainer | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $qaInspect[0].Name -ne "/$qaContainer" -or
    $qaInspect[0].Config.Labels.'academydesk.qa.run' -ne $RunId -or
    $qaInspect[0].Config.Image -ne 'mcr.microsoft.com/mssql/server:2022-latest' -or
    !$qaInspect[0].State.Running -or $qaInspect[0].NetworkSettings.Ports.'1433/tcp'[0].HostIp -ne '127.0.0.1') {
    throw 'Recording verification refused: container ownership mismatch.'
}
$qaGuid = [Guid]::ParseExact($RunId, 'N').ToString()
$qaQuery = @"
SET NOCOUNT ON;
IF DB_NAME() <> '$qaDatabase' OR (SELECT COUNT(*) FROM dbo.QaRunOwnership) <> 1
 OR NOT EXISTS (SELECT 1 FROM dbo.QaRunOwnership WHERE RunId='$qaGuid' AND TargetAddress LIKE '127.0.0.1,%')
 THROW 51000, 'Owned database marker mismatch', 1;
IF (SELECT COUNT(*) FROM dbo.ClassMediaUploadSessions) <> 1
 OR NOT EXISTS (SELECT 1 FROM dbo.ClassMediaUploadSessions s JOIN dbo.AspNetUsers u ON u.Id=s.OwnerUserId
 JOIN dbo.Batches b ON b.Id=s.BatchId
 WHERE u.Email='qa-teacher-a@example.invalid' AND b.Name='Synthetic Batch' AND b.TeacherId=s.TeacherId
 AND s.CompletedAtUtc IS NOT NULL AND s.Length>0 AND s.Length<33554432
 AND s.FileName LIKE 'class-recording-%.webm' AND s.Title='Synthetic microphone check'
 AND (s.Description IS NULL OR s.Description=''))
 THROW 51000, 'Recorded audio completion mismatch', 1;
IF (SELECT COUNT(*) FROM dbo.LearningResources) <> 1
 OR NOT EXISTS (SELECT 1 FROM dbo.LearningResources r JOIN dbo.ClassMediaUploadSessions s ON s.Id=r.Id
 WHERE r.Title=s.Title AND r.BatchId=s.BatchId AND r.AcademyId=s.AcademyId)
 OR (SELECT COUNT(*) FROM dbo.Notifications WHERE Title='New class material' AND RecipientType='Student') <> 1
 THROW 51000, 'One resource/notification assertion failed', 1;
PRINT 'BROWSER RECORDING SQL PASS completedSessions=1 resources=1 studentNotifications=1 optionalCommentBlank=True';
"@
docker exec $qaContainer sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d "$1" -Q "$2" -b -W' qa-read $qaDatabase $qaQuery
if ($LASTEXITCODE -ne 0) { throw 'Read-only recording SQL verification failed.' }
