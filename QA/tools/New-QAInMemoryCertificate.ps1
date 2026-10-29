$ErrorActionPreference = 'Stop'
# Fresh synthetic PFX bytes return only to the parent process memory, never a file/log.
if ($env:ACADEMY_QA_CERT_RUN -notmatch '^[0-9a-f]{32}$' -or $env:ACADEMY_QA_CERT_PASSWORD -notmatch '^Qa!a9[0-9a-f]{40}$') { throw 'Owned synthetic certificate inputs required.' }
$qaRsa = [System.Security.Cryptography.RSA]::Create(2048)
$qaCertificate = $null
try {
    $qaRequest = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
        ('CN=AcademyDesk-QA-' + $env:ACADEMY_QA_CERT_RUN), $qaRsa,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $qaCertificate = $qaRequest.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-5), [DateTimeOffset]::UtcNow.AddDays(2))
    [Console]::Out.Write([Convert]::ToBase64String($qaCertificate.Export(
        [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $env:ACADEMY_QA_CERT_PASSWORD)))
}
finally {
    if ($null -ne $qaCertificate) { $qaCertificate.Dispose() }
    $qaRsa.Dispose()
}
