param([switch]$SelfTest)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$versioning = 'C:/Program Files/dotnet/sdk/10.0.401/NuGet.Versioning.dll'
[void][Reflection.Assembly]::LoadFrom($versioning)
$script:lastAssertion=$null
function Assert-QA($condition, [string]$message) { if (-not $condition) { $script:lastAssertion=$message; throw $message } }
function Assert-FeedUri([string]$value, [switch]$Service) {
    $uri = [Uri]::new($value, [UriKind]::Absolute)
    Assert-QA ($uri.Scheme -eq 'https' -and $uri.IsDefaultPort -and !$uri.UserInfo -and !$uri.Query -and !$uri.Fragment) 'Unsafe feed URI; values withheld.'
    Assert-QA ($uri.Host -in @('api.nuget.org','data.nuget.org')) 'Unknown feed host; values withheld.'
    if ($Service) { Assert-QA ($uri.AbsolutePath -eq '/v3/index.json') 'Unexpected service index path.' }
    else { Assert-QA ($uri.AbsolutePath.StartsWith('/v3/vulnerabilities/', [StringComparison]::Ordinal) -or $uri.AbsolutePath.StartsWith('/v3-vulnerabilities/', [StringComparison]::Ordinal)) 'Unexpected vulnerability resource path.' }
}
function Assert-Index($index) {
    Assert-QA ($index -is [Array] -and $index.Count -ge 1 -and $index.Count -le 16) 'Missing or oversized vulnerability index.'
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $urls = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($page in $index) {
        Assert-QA ($page -is [Collections.IDictionary]) 'Invalid vulnerability index entry.'
        Assert-QA ($page['@name'] -is [string] -and $page['@name'] -cmatch '^[A-Za-z0-9_-]{1,32}$') 'Invalid vulnerability page name.'
        Assert-QA ($names.Add($page['@name']) -and $urls.Add($page['@id'])) 'Duplicate vulnerability page.'
        Assert-FeedUri $page['@id']
        $updated = [DateTimeOffset]::MinValue
        Assert-QA ([DateTimeOffset]::TryParse($page['@updated'], [ref]$updated)) 'Invalid vulnerability page timestamp.'
        Assert-QA ($updated -le [DateTimeOffset]::UtcNow.AddMinutes(5)) 'Vulnerability page timestamp is in the future.'
    }
}
function Find-Advisories($packages, $pages) {
    $findings = [Collections.Generic.List[object]]::new()
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($page in $pages) {
        if ($page -is [Array]) { Assert-QA ($page.Count -eq 0) 'Nonempty array is not a vulnerability page.'; continue }
        Assert-QA ($page -is [Collections.IDictionary]) 'Invalid vulnerability page.'
        foreach ($packageId in $page.Keys) {
            Assert-QA ($packageId -cmatch '^[a-z0-9_.-]+$') 'Invalid vulnerability package identity.'
            Assert-QA ($page[$packageId] -is [Array]) 'Invalid advisory list.'
            foreach ($advisory in $page[$packageId]) {
                Assert-QA ($advisory -is [Collections.IDictionary]) 'Invalid advisory record.'
                Assert-QA ($advisory['severity'] -is [long] -or $advisory['severity'] -is [int]) 'Invalid advisory severity type.'
                Assert-QA ($advisory['severity'] -in @(0,1,2,3)) 'Unknown advisory severity.'
                Assert-QA ($advisory['url'] -is [string] -and $advisory['url'] -cmatch '^https://github\.com/advisories/GHSA-[A-Za-z0-9-]+$') 'Unsupported advisory URL; review required.'
                Assert-QA ($advisory['versions'] -is [string]) 'Missing advisory range.'
                $range = [NuGet.Versioning.VersionRange]::Parse($advisory['versions'])
                foreach ($package in $packages) {
                    if ($package.name.ToLowerInvariant() -cne $packageId) { continue }
                    $version = [NuGet.Versioning.NuGetVersion]::Parse($package.version)
                    if (!$range.Satisfies($version)) { continue }
                    $key = $packageId + '|' + $package.version + '|' + $advisory['url'] + '|' + $advisory['versions']
                    if ($seen.Add($key)) { $findings.Add(@{ package=$package.name; version=$package.version; severity=$advisory['severity']; advisory=$advisory['url']; affectedRange=$advisory['versions'] }) }
                }
            }
        }
    }
    return ,$findings.ToArray()
}
function Assert-Rejected([scriptblock]$action) { $rejected=$false; try { & $action } catch { $rejected=$true }; Assert-QA $rejected 'Negative control unexpectedly accepted.' }
if ($SelfTest) {
    $package = @{ name='Example'; version='1.2.3' }
    $advisory = @{ severity=2; url='https://github.com/advisories/GHSA-test-test-test'; versions='(,1.2.3]' }
    $page = @{ example=@($advisory) }
    Assert-QA ((Find-Advisories @($package) @($page,$page)).Count -eq 1) 'Matching/deduplication failed.'
    Assert-QA ((Find-Advisories @(@{name='Example';version='1.2.4'}) @($page)).Count -eq 0) 'Exclusive patched version matched.'
    Assert-QA ((Find-Advisories @($package) @(@())).Count -eq 0) 'Empty page rejected.'
    foreach ($case in @(@('(1.2.3,2.0.0)','1.2.3',$false),@('[1.2.3,2.0.0)','1.2.3',$true),@('[1.2.3.4]','1.2.3.4',$true),@('[1.0.0-alpha,1.0.0)','1.0.0-beta',$true))) {
        Assert-QA ([NuGet.Versioning.VersionRange]::Parse($case[0]).Satisfies([NuGet.Versioning.NuGetVersion]::Parse($case[1])) -eq $case[2]) 'NuGet range boundary failed.'
    }
    Assert-Rejected { Assert-Index @() }
    Assert-Rejected { Assert-Index @(@{'@name'='base';'@id'='https://external.invalid/v3/vulnerabilities/base.json';'@updated'='2020-01-01T00:00:00Z'}) }
    Assert-Rejected { Assert-FeedUri 'https://api.nuget.org/v3-flatcontainer/example/index.json' }
    Assert-FeedUri 'https://api.nuget.org/v3-vulnerabilities/2026.10.19/vulnerability.base.json'
    Assert-Rejected { Assert-FeedUri 'https://api.nuget.org/v3/vulnerabilities/base.json?credential=synthetic' }
    Assert-Rejected { Assert-Index @(@{'@name'='base';'@id'='https://api.nuget.org/v3/vulnerabilities/base.json';'@updated'='2020-01-01T00:00:00Z'},@{'@name'='base';'@id'='https://api.nuget.org/v3/vulnerabilities/base.json';'@updated'='2020-01-01T00:00:00Z'}) }
    foreach ($mutation in @(@{severity=9;url=$advisory.url;versions=$advisory.versions},@{severity=2;url='javascript:invalid';versions=$advisory.versions},@{severity=2;url=$advisory.url;versions='invalid range'})) { Assert-Rejected { Find-Advisories @($package) @(@{example=@($mutation)}) | Out-Null } }
    Assert-Rejected { Find-Advisories @($package) @(@{Example=@($advisory)}) | Out-Null }
    @{ selfTest='PASS'; networkUsed=$false; rangeEngine=[NuGet.Versioning.NuGetVersion].Assembly.GetName().Version.ToString(); productionApproved=$false } | ConvertTo-Json -Compress
    exit 0
}
$run = [Guid]::NewGuid().ToString('N')
$evidence = Join-Path $root ('QA/EVIDENCE/api-package-advisories-' + $run)
[void][IO.Directory]::CreateDirectory($evidence)
$result = @{ run=$run; completed=$false; productionApproved=$false; wholeImageScanComplete=$false; retrievedAtUtc=[DateTimeOffset]::UtcNow.ToString('o'); sources=@(); credentialsUsed=$false; packageDownloads=$false; dockerMutation=$false }
$client=$null; $stage='pinned candidate input'
try {
    Assert-QA ($root.ToLowerInvariant() -eq 'd:\academydesk-codex-p0') 'Unexpected worktree.'
    $inventoryFile = Join-Path $root 'QA/EVIDENCE/api-component-inventory-bcce82c8108942b2a0ddb7664d3063e1/result.json'
    $inventoryBytes = [IO.File]::ReadAllBytes($inventoryFile)
    $inventory = [Text.Encoding]::UTF8.GetString($inventoryBytes) | ConvertFrom-Json -AsHashtable
    Assert-QA ($inventory.sourceUnchanged -eq $true -and $inventory.packageCount -eq 31 -and $inventory.verifiedPublishedFiles -eq 63) 'Component inventory incomplete.'
    Assert-QA ($inventory.build -eq '0a5f8d2c708a40a994dd97e9f6d12425' -and $inventory.imageId -eq 'sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70') 'Wrong candidate inventory.'
    $qualificationText = & node (Join-Path $PSScriptRoot 'api-image-qualification.cjs')
    Assert-QA ($LASTEXITCODE -eq 0) 'Current candidate qualification failed.'
    $qualification = ($qualificationText -join "`n") | ConvertFrom-Json -AsHashtable
    Assert-QA ($qualification.currentApplicationSourcesMatch -eq $true -and $qualification.qualified.Count -eq 14) 'Current source/evidence qualification incomplete.'
    $depsFile = Join-Path $root '.build-check/api-artifacts/d98dcbc285f94ec39dbc72f44b9e8e43/app/AcademyDesk.Api.deps.json'
    Assert-QA ((Get-FileHash -LiteralPath $depsFile -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $inventory.dependencyManifestSha256) 'Dependency manifest changed.'
    $deps = [IO.File]::ReadAllText($depsFile) | ConvertFrom-Json -AsHashtable
    $expectedPackages = @($deps.libraries.Keys | Where-Object { $deps.libraries[$_].type -eq 'package' } | Sort-Object)
    $packages = @($inventory.components | Where-Object type -eq 'package')
    Assert-QA ($packages.Count -eq 31) 'Published package count mismatch.'
    $actualPackages = @($packages | ForEach-Object { $_.name+'/'+$_.version } | Sort-Object)
    Assert-QA (($actualPackages -join "`n") -ceq ($expectedPackages -join "`n")) 'Inventory package/version mismatch.'
    $result.inventorySha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($inventoryBytes)).ToLowerInvariant()
    $result.build=$inventory.build; $result.imageId=$inventory.imageId; $result.packageCount=$packages.Count
    $result.rangeEngine=@{ assembly=[NuGet.Versioning.NuGetVersion].Assembly.GetName().Version.ToString(); sha256=(Get-FileHash -LiteralPath $versioning -Algorithm SHA256).Hash.ToLowerInvariant() }
    $handler=[Net.Http.HttpClientHandler]::new(); $handler.AllowAutoRedirect=$false; $handler.UseCookies=$false; $handler.UseDefaultCredentials=$false; $handler.AutomaticDecompression=[Net.DecompressionMethods]::All
    $client=[Net.Http.HttpClient]::new($handler); $client.Timeout=[TimeSpan]::FromSeconds(20); $client.DefaultRequestHeaders.CacheControl=[Net.Http.Headers.CacheControlHeaderValue]::Parse('no-cache')
    function Get-Feed([string]$uri,[string]$name,[switch]$Service) {
        Assert-FeedUri $uri -Service:$Service
        $response=$client.GetAsync($uri,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        try {
            Assert-QA ([int]$response.StatusCode -eq 200) 'Feed unavailable/redirected; no clean result permitted.'
            Assert-QA ($response.Content.Headers.ContentType.MediaType -in @('application/json','text/json')) 'Unexpected feed content type.'
            $stream=$response.Content.ReadAsStream(); $memory=[IO.MemoryStream]::new(); $buffer=[byte[]]::new(65536); $deadline=[Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(20))
            try { while (($read=$stream.ReadAsync($buffer,0,$buffer.Length,$deadline.Token).GetAwaiter().GetResult()) -gt 0) { Assert-QA ($memory.Length+$read -le 32MB) 'Feed body exceeds bound.'; $memory.Write($buffer,0,$read) }; $bytes=$memory.ToArray() }
            finally { $deadline.Dispose(); $memory.Dispose(); $stream.Dispose() }
            $parsed=[Text.UTF8Encoding]::new($false,$true).GetString($bytes) | ConvertFrom-Json -AsHashtable -NoEnumerate
            [IO.File]::WriteAllBytes((Join-Path $evidence $name),$bytes)
            $result.sources+=@{ uri=$uri; file=$name; bytes=$bytes.Length; sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant(); httpDate=[string]$response.Headers.Date }
            return ,$parsed
        } finally { $response.Dispose() }
    }
    $stage='official service index'; $service=Get-Feed 'https://api.nuget.org/v3/index.json' 'service-index.json' -Service
    $resources=@($service.resources | Where-Object { $_['@type'] -eq 'VulnerabilityInfo/6.7.0' }); Assert-QA ($resources.Count -eq 1) 'Missing/ambiguous vulnerability resource.'
    $stage='official vulnerability index'; $index=Get-Feed $resources[0]['@id'] 'vulnerability-index.json'; Assert-Index $index
    $pages=[Collections.Generic.List[object]]::new(); $pageNumber=0
    foreach ($page in $index) { $pageNumber++; $stage='official vulnerability page '+$pageNumber; $pages.Add((Get-Feed $page['@id'] ('page-'+$pageNumber+'.json'))) }
    $stage='exact NuGet range matching'
    $result.findings=Find-Advisories $packages $pages.ToArray()
    $result.pageCount=$pageNumber; $result.pageUpdates=@($index | ForEach-Object { @{ name=$_['@name']; updated=$_['@updated'] } })
    $result.findingCount=$result.findings.Count; $result.distinctAdvisoryCount=@($result.findings | ForEach-Object { $_.advisory } | Sort-Object -Unique).Count
    $result.noKnownAffectedNuGetPackageVersions=($result.findingCount -eq 0); $result.completed=$true
    $result.limitations=@('Exact31published NuGet packages only; shared framework/base OS/frontend/build tooling excluded.','Feed snapshot and range matching are not exploitability, licence, full security or production approval.','No automatic package upgrade or advisory suppression.')
} catch { $result.failure='Advisory verification incomplete; values withheld. No clean result permitted.'; $result.diagnostic=@{stage=$stage;type=$_.Exception.GetType().Name;baseType=$_.Exception.GetBaseException().GetType().Name;line=$_.InvocationInfo.ScriptLineNumber;rule=$script:lastAssertion} }
finally {
    if ($client) { $client.Dispose() }
    [IO.File]::WriteAllText((Join-Path $evidence 'result.json'),($result | ConvertTo-Json -Depth 15))
    @{run=$run;evidence=$evidence;completed=$result.completed;findingCount=$result['findingCount'];productionApproved=$false} | ConvertTo-Json -Compress
    if (!$result.completed -or $result['findingCount'] -gt 0) { exit 1 }
}
