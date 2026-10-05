using System.Security.Cryptography;
using System.Text;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Meshmakers.Octo.Runtime.Contracts.Secrets;
using Meshmakers.Octo.Runtime.Engine.Configuration.DependencyInjection;
using Meshmakers.Octo.Sdk.Common.Encryption;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sdk.Common.Tests.Encryption;

/// <summary>
///     AB#5534 (WP4 of AB#5528): <see cref="InstanceSecretCrypto" /> keeps writing byte-compatible
///     <c>enc:v1</c>, shares the envelope parsing with Runtime.Contracts and reads <c>enc:v2</c>
///     through the engine's <see cref="ISecretAttributeProtector" />. All keys are generated per test.
/// </summary>
public class InstanceSecretCryptoTests
{
    private const string FakePlaintext = "fake-test-password-not-a-secret";

    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    private readonly InstanceSecretCrypto _crypto = new();

    [Theory]
    [InlineData(FakePlaintext)]
    [InlineData("")]
    [InlineData("  whitespace is part of a credential  ")]
    [InlineData("ümlaut-€-日本-🔑")]
    public void Encrypt_Decrypt_RoundTrip(string plaintext)
    {
        var encrypted = _crypto.Encrypt(_key, plaintext);

        Assert.StartsWith("enc:v1:", encrypted, StringComparison.Ordinal);
        Assert.DoesNotContain(plaintext.Length > 0 ? plaintext : "\0", encrypted, StringComparison.Ordinal);
        Assert.Equal(plaintext, _crypto.Decrypt(_key, encrypted));
    }

    [Fact]
    public void Encrypt_WireLayout_IsUnchanged_NonceTagCiphertextWithoutAad()
    {
        var encrypted = _crypto.Encrypt(_key, FakePlaintext);

        // enc:v1: + standard base64 (padded) of nonce(12) ‖ tag(16) ‖ ciphertext(N), no AAD.
        var combined = Convert.FromBase64String(encrypted["enc:v1:".Length..]);
        var plaintextBytes = Encoding.UTF8.GetBytes(FakePlaintext);
        Assert.Equal(12 + 16 + plaintextBytes.Length, combined.Length);

        var decrypted = new byte[plaintextBytes.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(combined.AsSpan(0, 12), combined.AsSpan(28), combined.AsSpan(12, 16), decrypted);
        Assert.Equal(plaintextBytes, decrypted);
        Assert.True(SecretEnvelope.TryParse(encrypted, out var info));
        Assert.Equal(new SecretEnvelopeInfo(1, null), info);
    }

    [Fact]
    public void Decrypt_AcceptsAnEncV1ValueBuiltIndependently()
    {
        // Built with a fixed (fake) nonce outside the SDK: the layout every existing enc:v1 value has.
        var nonce = new byte[12];
        var plaintextBytes = Encoding.UTF8.GetBytes(FakePlaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(_key, 16))
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        var value = "enc:v1:" + Convert.ToBase64String([.. nonce, .. tag, .. ciphertext]);

        Assert.Equal(FakePlaintext, _crypto.Decrypt(_key, value));
    }

    [Fact]
    public void Encrypt_UsesAFreshNoncePerCall()
    {
        Assert.NotEqual(_crypto.Encrypt(_key, FakePlaintext), _crypto.Encrypt(_key, FakePlaintext));
    }

    [Fact]
    public void Decrypt_PlaintextWithoutPrefix_IsReturnedUnchanged()
    {
        Assert.Equal(FakePlaintext, _crypto.Decrypt(_key, FakePlaintext));
        Assert.Equal(string.Empty, _crypto.Decrypt(_key, string.Empty));
    }

    [Fact]
    public void Decrypt_WrongKey_Throws()
    {
        var encrypted = _crypto.Encrypt(_key, FakePlaintext);

        Assert.ThrowsAny<CryptographicException>(() => _crypto.Decrypt(RandomNumberGenerator.GetBytes(32), encrypted));
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_Throws()
    {
        var encrypted = _crypto.Encrypt(_key, FakePlaintext);
        var combined = Convert.FromBase64String(encrypted["enc:v1:".Length..]);
        combined[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() =>
            _crypto.Decrypt(_key, "enc:v1:" + Convert.ToBase64String(combined)));
    }

    [Theory]
    [InlineData("enc:v1:not-base64!")]
    [InlineData("enc:v1:")]
    [InlineData("enc:v1:AAAA")] // truncated: fewer than nonce + tag bytes
    [InlineData("enc:v9:AAAA")] // unsupported sentinel
    [InlineData("enc:v2:k1:AAAA")] // malformed v2 envelope
    public void Decrypt_MalformedEnvelopes_ThrowCryptographicException(string value)
    {
        Assert.ThrowsAny<CryptographicException>(() => _crypto.Decrypt(_key, value));
    }

    [Fact]
    public void WrongKeyLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _crypto.Encrypt(new byte[16], FakePlaintext));
        Assert.Throws<ArgumentException>(() =>
            _crypto.Decrypt(new byte[16], _crypto.Encrypt(_key, FakePlaintext)));
    }

    [Theory]
    [InlineData("enc:v1:abc", true)]
    [InlineData("enc:v2:k1:abc", true)]
    [InlineData("enc:anything", true)]
    [InlineData("plain", false)]
    [InlineData("", false)]
    public void IsEncrypted_KeepsTheGenericPrefixRule(string value, bool expected)
    {
        Assert.Equal(expected, _crypto.IsEncrypted(value));
    }

    [Fact]
    public void EngineProtector_DecryptsSdkEncV1()
    {
        var protector = CreateEngineProtector(_key);

        var encrypted = _crypto.Encrypt(_key, FakePlaintext);

        Assert.True(protector.IsProtectedEnvelope(encrypted));
        Assert.Equal(FakePlaintext, protector.Unprotect(encrypted));
        Assert.Equal(FakePlaintext, protector.Unprotect(RtSecretValue.LegacyPlaintext(encrypted)));
    }

    [Fact]
    public void EngineProtector_ReprotectsSdkEncV1ToEncV2_WhichTheSdkReadsWithTheProtector()
    {
        var protector = CreateEngineProtector(_key);
        var legacy = RtSecretValue.LegacyPlaintext(_crypto.Encrypt(_key, FakePlaintext));

        var reprotected = protector.Reprotect(legacy);

        Assert.True(reprotected.IsProtected);
        Assert.StartsWith("enc:v2:k1:", reprotected.Envelope, StringComparison.Ordinal);
        var crypto = new InstanceSecretCrypto(protector);
        Assert.Equal(FakePlaintext, crypto.Decrypt(_key, reprotected.Envelope!));
    }

    [Fact]
    public void Decrypt_EncV2_WithProtector_DelegatesToTheKeyRing()
    {
        var protector = CreateEngineProtector(_key);
        var crypto = new InstanceSecretCrypto(protector);
        var envelope = protector.Protect(FakePlaintext).Envelope!;

        Assert.Equal(FakePlaintext, crypto.Decrypt(_key, envelope));
        // enc:v1 still works on the protector-backed instance and is still what Encrypt writes.
        var v1 = crypto.Encrypt(_key, FakePlaintext);
        Assert.StartsWith("enc:v1:", v1, StringComparison.Ordinal);
        Assert.Equal(FakePlaintext, crypto.Decrypt(_key, v1));
    }

    [Fact]
    public void Decrypt_EncV2_WithoutProtector_ThrowsNotConfigured()
    {
        var envelope = CreateEngineProtector(_key).Protect(FakePlaintext).Envelope!;

        var exception = Assert.Throws<SecretEncryptionNotConfiguredException>(() => _crypto.Decrypt(_key, envelope));
        Assert.DoesNotContain(envelope, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Decrypt_EncV2_UnknownKeyId_Throws()
    {
        var otherEnvironment = CreateEngineProtector(RandomNumberGenerator.GetBytes(32), "k9");
        var envelope = otherEnvironment.Protect(FakePlaintext).Envelope!;
        var crypto = new InstanceSecretCrypto(CreateEngineProtector(_key));

        Assert.Throws<UnknownSecretKeyIdException>(() => crypto.Decrypt(_key, envelope));
    }

    [Fact]
    public void DependencyInjection_PicksTheProtectorConstructorOnlyWhenAProtectorIsRegistered()
    {
        var plain = new ServiceCollection()
            .AddSingleton<IInstanceSecretCrypto, InstanceSecretCrypto>()
            .BuildServiceProvider()
            .GetRequiredService<IInstanceSecretCrypto>();
        var protector = CreateEngineProtector(_key);
        var withProtector = new ServiceCollection()
            .AddSingleton(protector)
            .AddSingleton<IInstanceSecretCrypto, InstanceSecretCrypto>()
            .BuildServiceProvider()
            .GetRequiredService<IInstanceSecretCrypto>();
        var envelope = protector.Protect(FakePlaintext).Envelope!;

        Assert.Throws<SecretEncryptionNotConfiguredException>(() => plain.Decrypt(_key, envelope));
        Assert.Equal(FakePlaintext, withProtector.Decrypt(_key, envelope));
    }

    private static ISecretAttributeProtector CreateEngineProtector(byte[] key, string keyId = "k1")
    {
        var encodedKey = Convert.ToBase64String(key);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"SecretEncryption:Keys:{keyId}"] = encodedKey,
                ["SecretEncryption:ActiveKeyId"] = keyId,
                ["SecretEncryption:LegacyV1Key"] = encodedKey
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRuntimeEngine();
        return services.BuildServiceProvider().GetRequiredService<ISecretAttributeProtector>();
    }
}
