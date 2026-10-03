param([int]$WebPort = 49131, [int]$ApiPort = 49132)
$ErrorActionPreference = 'Stop'
if ($WebPort -lt 1024 -or $WebPort -gt 65535 -or $ApiPort -lt 1024 -or $ApiPort -gt 65535 -or $WebPort -eq $ApiPort) { throw 'Distinct nonprivileged loopback ports required.' }
if (Get-NetTCPConnection -State Listen -LocalPort $WebPort -ErrorAction SilentlyContinue) { throw 'Refused existing listener.' }
$qaRepo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$qaSource = Join-Path $qaRepo 'apps/web'
$qaCopy = Join-Path $qaRepo ('.build-check/certificate-browser-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $qaCopy 'src/app/certificates') -Force | Out-Null
# Narrow source-only fixture. No .env, normal build/cache or normal app services.
Copy-Item -LiteralPath (Join-Path $qaSource 'src/components'),(Join-Path $qaSource 'src/lib') -Destination (Join-Path $qaCopy 'src') -Recurse
Copy-Item -LiteralPath (Join-Path $qaSource 'src/app/certificates/page.tsx') -Destination (Join-Path $qaCopy 'src/app/certificates/page.tsx')
Copy-Item -LiteralPath (Join-Path $qaSource 'src/app/globals.css') -Destination (Join-Path $qaCopy 'src/app/globals.css')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CertificateBrowserFixture/layout.tsx'),(Join-Path $PSScriptRoot 'CertificateBrowserFixture/qa-probe.tsx') -Destination (Join-Path $qaCopy 'src/app')
foreach ($qaFile in @('package.json','package-lock.json','next.config.ts','next-env.d.ts','postcss.config.mjs','tsconfig.json','AGENTS.md')) { Copy-Item -LiteralPath (Join-Path $qaSource $qaFile) -Destination $qaCopy }
New-Item -ItemType Junction -Path (Join-Path $qaCopy 'node_modules') -Target (Join-Path $qaSource 'node_modules') | Out-Null
foreach ($qaFile in @('src/app/certificates/page.tsx','src/app/globals.css','src/components/standard-select-field.tsx','src/components/standard-date-field.tsx','src/lib/api.ts')) {
  if ((Get-FileHash -LiteralPath (Join-Path $qaSource $qaFile)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $qaCopy $qaFile)).Hash) { throw 'Source-copy hash mismatch.' }
}
$qaPreviousApi = $env:NEXT_PUBLIC_API_URL; $qaPreviousTelemetry = $env:NEXT_TELEMETRY_DISABLED
try {
  $env:NEXT_PUBLIC_API_URL = "http://127.0.0.1:$ApiPort"; $env:NEXT_TELEMETRY_DISABLED = '1'
  Push-Location -LiteralPath $qaCopy
  try {
    Write-Output "QA certificate frontend copy=$qaCopy url=http://127.0.0.1:$WebPort/certificates; exact certificate/CSS/controls/API hashes verified; synthetic API, QA-only shell/print probe."
    node node_modules/next/dist/bin/next dev --webpack --hostname 127.0.0.1 --port $WebPort
    if ($LASTEXITCODE -ne 0) { throw 'Owned QA Next process exited nonzero; generated copy retained.' }
  } finally { Pop-Location }
} finally { $env:NEXT_PUBLIC_API_URL = $qaPreviousApi; $env:NEXT_TELEMETRY_DISABLED = $qaPreviousTelemetry }
# Keep generated source/cache recoverable; never recursively delete a junction.
