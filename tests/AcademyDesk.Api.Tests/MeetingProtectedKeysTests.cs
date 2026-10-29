using System.Security.Cryptography;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingProtectedKeysTests
{
    [Fact] public void Actual_encrypted_snapshot_restores_after_writer_disposal()
    {
        using var fixture = new QaMeetingKeyRing(); string payload;
        using (var writer = fixture.Provider()) payload = writer.GetDataProtector("synthetic-meeting-test").Protect("synthetic-before");
        var snapshot = fixture.Snapshot("backup"); var restored = fixture.Snapshot("restored", snapshot);
        using var reader = fixture.Provider(directory: restored); Assert.Equal("synthetic-before", reader.GetDataProtector("synthetic-meeting-test").Unprotect(payload));
    }
    [Fact] public void Rotated_writer_uses_new_key_and_restores_both_generations()
    {
        using var fixture = new QaMeetingKeyRing(); string before, after;
        using (var writer = fixture.Provider()) before = writer.GetDataProtector("synthetic-meeting-test").Protect("synthetic-before");
        var key = fixture.Rotate();
        using (var writer = fixture.Provider(rotated: true)) after = writer.GetDataProtector("synthetic-meeting-test").Protect("synthetic-after");
        Assert.Equal(key, new Guid(WebEncoders.Base64UrlDecode(after).AsSpan(4, 16))); Assert.True(fixture.EncryptedFilesOnly());
        var restored = fixture.Snapshot("restored"); using var reader = fixture.Provider(rotated: true, directory: restored);
        Assert.Equal("synthetic-before", reader.GetDataProtector("synthetic-meeting-test").Unprotect(before));
        Assert.Equal("synthetic-after", reader.GetDataProtector("synthetic-meeting-test").Unprotect(after));
    }
    [Theory] [InlineData("environment")] [InlineData("empty")] [InlineData("stale")] [InlineData("retired-certificate")] [InlineData("revoked")]
    public void Missing_or_revoked_material_cannot_silently_recover_protected_payloads(string mode)
    {
        using var fixture = new QaMeetingKeyRing(); string before, after;
        using (var writer = fixture.Provider()) before = writer.GetDataProtector("synthetic-meeting-test").Protect("synthetic-before");
        var stale = fixture.Snapshot("old-backup"); fixture.Rotate();
        using (var writer = fixture.Provider(rotated: true)) after = writer.GetDataProtector("synthetic-meeting-test").Protect("synthetic-after");
        if (mode == "revoked") { using var revoker = fixture.Provider(rotated: true); revoker.GetRequiredService<IKeyManager>().RevokeAllKeys(DateTimeOffset.UtcNow.AddSeconds(1), "Synthetic QA only"); }
        using var reader = fixture.Provider(rotated: true, keepOriginal: mode != "retired-certificate", directory: mode == "empty" ? fixture.Empty("empty") : mode == "stale" ? stale : fixture.Keys,
            environment: mode == "environment" ? "SyntheticOtherEnvironment" : "Testing");
        var protector = reader.GetDataProtector("synthetic-meeting-test");
        if (mode == "retired-certificate") { Assert.Equal("synthetic-after", protector.Unprotect(after)); Assert.Throws<CryptographicException>(() => protector.Unprotect(before)); }
        else Assert.Throws<CryptographicException>(() => protector.Unprotect(after));
    }
    [Fact] public void Only_the_owned_synthetic_fixture_is_removed()
    {
        var fixture = new QaMeetingKeyRing(); Assert.True(fixture.Exists); fixture.Dispose(); Assert.False(fixture.Exists); fixture.Dispose();
    }
}
