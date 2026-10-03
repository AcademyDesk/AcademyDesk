# Fresh source-only copy on D:; never copies .env files or replaces the dev server.
param([int]$WebPort = 49001, [int]$ApiPort = 49002)
$ErrorActionPreference = 'Stop'
if ($WebPort -lt 1024 -or $WebPort -gt 65535 -or $ApiPort -lt 1024 -or $ApiPort -gt 65535 -or $WebPort -eq $ApiPort) { throw 'Distinct nonprivileged loopback ports required.' }
if (Get-NetTCPConnection -State Listen -LocalPort $WebPort -ErrorAction SilentlyContinue) { throw 'Refused to replace an existing listener.' }
$qaRepo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$qaWebSource = Join-Path $qaRepo 'apps/web'
$qaWebRoot = Join-Path $qaRepo '.build-check'
$qaWebCopy = Join-Path $qaWebRoot ('browser-web-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $qaWebCopy | Out-Null
Copy-Item -LiteralPath (Join-Path $qaWebSource 'src'),(Join-Path $qaWebSource 'public') -Destination $qaWebCopy -Recurse
foreach ($qaFileName in @('package.json','package-lock.json','next.config.ts','next-env.d.ts','postcss.config.mjs','tsconfig.json','AGENTS.md')) {
    Copy-Item -LiteralPath (Join-Path $qaWebSource $qaFileName) -Destination $qaWebCopy
}
New-Item -ItemType Junction -Path (Join-Path $qaWebCopy 'node_modules') -Target (Join-Path $qaWebSource 'node_modules') | Out-Null
$qaPreviousApi = $env:NEXT_PUBLIC_API_URL
$qaPreviousTelemetry = $env:NEXT_TELEMETRY_DISABLED
try {
    $env:NEXT_PUBLIC_API_URL = "http://127.0.0.1:$ApiPort"
    $env:NEXT_TELEMETRY_DISABLED = '1'
    Push-Location -LiteralPath $qaWebCopy
    try {
        Write-Output "QA browser frontend copy=$qaWebCopy url=http://127.0.0.1:$WebPort api=$($env:NEXT_PUBLIC_API_URL)"
        node node_modules/next/dist/bin/next dev --webpack --hostname 127.0.0.1 --port $WebPort
        if ($LASTEXITCODE -ne 0) { throw 'Owned Next process exited nonzero; copy retained for investigation.' }
    } finally { Pop-Location }
} finally {
    $env:NEXT_PUBLIC_API_URL = $qaPreviousApi
    $env:NEXT_TELEMETRY_DISABLED = $qaPreviousTelemetry
}
# Keep the generated copy/cache recoverable; never recursively delete a junction.
