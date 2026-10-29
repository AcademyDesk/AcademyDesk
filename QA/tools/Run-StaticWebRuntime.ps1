# Cached local runtime/read-only export. Never pulls/builds/pushes an image or
# touches existing containers/volumes. This is not the full Docker release gate.
param([switch]$PortableExport,[string]$BuildRun)
$ErrorActionPreference='Stop'
$qaRepo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if($qaRepo -ne 'D:\AcademyDesk-codex-p0'){throw 'This bounded runtime packet is restricted to its approved worktree.'}
if(!$BuildRun){$qaOut=(Resolve-Path (Join-Path $qaRepo 'apps/web/out')).Path}
if($BuildRun){
 if($BuildRun -notmatch '^[0-9a-f]{32}$'){throw 'Only an exact isolated default-build run may be served.'}
 $qaBuildReceipt=Get-Content -LiteralPath (Join-Path $qaRepo "QA/EVIDENCE/default-web-build-$BuildRun/result.json") -Raw | ConvertFrom-Json
 if(!$qaBuildReceipt.passed -or $qaBuildReceipt.exitCode -ne 0 -or !$qaBuildReceipt.turbopackObserved -or $qaBuildReceipt.run -ne $BuildRun){throw 'Successful default-build receipt required.'}
 foreach($qaPin in $qaBuildReceipt.sourcePins){
  if($qaPin.path -match '(^|/)\.\.(/|$)' -or [IO.Path]::IsPathRooted($qaPin.path)){throw 'Invalid build source path.'}
  $qaSource=(Resolve-Path (Join-Path (Join-Path $qaRepo 'apps/web') $qaPin.path)).Path
  if(!$qaSource.StartsWith((Join-Path $qaRepo 'apps/web')+'\') -or (Get-FileHash -LiteralPath $qaSource -Algorithm SHA256).Hash -ne $qaPin.sha256){throw 'Worktree source no longer matches this build.'}
 }
 $qaOut=(Resolve-Path (Join-Path $qaRepo ".build-check/default-web-build/$BuildRun/app/out")).Path
}
$qaConfig=(Resolve-Path (Join-Path $qaRepo 'apps/web/nginx.static.conf')).Path
if($PortableExport){$qaConfig=(Resolve-Path (Join-Path $qaRepo 'QA/tools/fixtures/nginx-direct-static.conf')).Path}
if(!$qaOut.StartsWith($qaRepo+'\') -or !$qaConfig.StartsWith($qaRepo+'\')){throw 'Static mounts escaped the worktree.'}
if((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory/1MB -lt 3){throw '3GiB memory guard: no runtime started.'}
$qaImage='sha256:add1449de3e17847da8baba6eddbb71c4d1289b7ec7ae7bec25fb37ce830fc2a'
$qaAvailable=docker image inspect $qaImage --format '{{.Id}}'
if($LASTEXITCODE -ne 0 -or $qaAvailable -ne $qaImage){throw 'Required exact cached runtime image unavailable; no pull allowed.'}
$qaRun=[guid]::NewGuid().ToString('N');$qaName='academydesk-static-qa-'+$qaRun
$qaPriorOrigin=$env:QA_PENTA_STATIC_ORIGIN
$qaPriorExport=$env:QA_PENTA_EXPORT_ROOT
$qaEvidence=Join-Path $qaRepo ('QA/EVIDENCE/static-web-runtime-'+$qaRun)
$qaStarted=$false
try{
 $qaContainerId=docker run --detach --pull=never --name $qaName --label "academydesk.qa.run=$qaRun" --label 'academydesk.qa.kind=static-runtime' --read-only --user '101:101' --cap-drop ALL --security-opt no-new-privileges --pids-limit 64 --memory 128m --cpus 1 --publish '127.0.0.1::3000' --tmpfs '/var/cache/nginx:rw,noexec,nosuid,size=16777216,mode=1777' --tmpfs '/var/run:rw,noexec,nosuid,size=1048576,mode=1777' --mount "type=bind,source=$qaOut,target=/usr/share/nginx/html,readonly" --mount "type=bind,source=$qaConfig,target=/etc/nginx/conf.d/default.conf,readonly" --entrypoint nginx $qaImage '-g' 'daemon off;'
 if($LASTEXITCODE -ne 0){throw 'Owned static runtime creation failed.'};$qaStarted=$true
 $qaInspect=docker inspect $qaName | ConvertFrom-Json
 if($qaInspect[0].Config.Labels.'academydesk.qa.run' -ne $qaRun -or $qaInspect[0].Image -ne $qaImage){throw 'Owned runtime identity mismatch.'}
 $qaPort=$qaInspect[0].NetworkSettings.Ports.'3000/tcp'[0]
 if($qaPort.HostIp -ne '127.0.0.1'){throw 'Static runtime exposed beyond loopback.'}
 $qaOrigin='http://127.0.0.1:'+$qaPort.HostPort
 Write-Output "STATICRUNTIME run=$qaRun container=$qaName origin=$qaOrigin image=$qaImage readOnly=true user101 memory128MiB portableExport=$PortableExport"
 Push-Location $qaRepo
 try{
  node QA/tools/static-web-runtime.cjs $qaOrigin $qaRun $qaOut
  if($LASTEXITCODE -ne 0){throw 'Exact static runtime byte checks failed.'}
  $env:QA_PENTA_STATIC_ORIGIN=$qaOrigin
  $env:QA_PENTA_EXPORT_ROOT=$qaOut
  node QA/tools/penta-chat-contract-browser.cjs
  if($LASTEXITCODE -ne 0){throw 'Container-served read-chat browser checks failed.'}
  $qaInspect=docker inspect $qaName | ConvertFrom-Json
  if(!$qaInspect[0].State.Running -or !$qaInspect[0].HostConfig.ReadonlyRootfs -or $qaInspect[0].Config.User -ne '101:101' -or @($qaInspect[0].Mounts | Where-Object {$_.Type -eq 'bind' -and $_.RW}).Count -ne 0){throw 'Runtime lost read-only/non-root/mount restrictions.'}
  $qaPost=@{run=$qaRun;passed=$true;image=$qaImage;readOnlyRoot=$true;nonRootUser='101:101';writableBindMounts=0;loopbackOnly=$true;imageBuild=$false;imagePull=$false;portableExport=[bool]$PortableExport;buildRun=$BuildRun;configSha256=(Get-FileHash -LiteralPath $qaConfig -Algorithm SHA256).Hash;nativeApiAcceptance=$false;productionApproved=$false}
  $qaPost | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $qaEvidence 'runtime-postconditions.json') -Encoding utf8
 }finally{Pop-Location}
}finally{
 $env:QA_PENTA_STATIC_ORIGIN=$qaPriorOrigin
 $env:QA_PENTA_EXPORT_ROOT=$qaPriorExport
 if($qaStarted){
  $qaInspect=docker inspect $qaName | ConvertFrom-Json
  if($LASTEXITCODE -ne 0 -or $qaInspect[0].Name -ne ('/'+$qaName) -or $qaInspect[0].Config.Labels.'academydesk.qa.run' -ne $qaRun -or $qaInspect[0].Image -ne $qaImage){throw 'Refused cleanup: owned runtime identity mismatch.'}
  docker stop --timeout 5 $qaName
  if($LASTEXITCODE -ne 0){throw 'Owned runtime stop failed.'}
  docker rm $qaName
  if($LASTEXITCODE -ne 0){throw 'Owned runtime removal failed.'}
  Write-Output "STATICRUNTIME exact owned container removed=$qaName;no volumes/images/other containers pruned"
 }
}
