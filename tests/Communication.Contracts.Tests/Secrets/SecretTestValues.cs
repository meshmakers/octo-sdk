using System.Buffers.Text;
using System.Security.Cryptography;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;

namespace Communication.Contracts.Tests.Secrets;

/// <summary>
///     Obviously fake secret material for the AB#5528 tests. The envelopes are structurally valid
///     (random bytes, not encrypted with any key) so the tests can look for them in the output.
/// </summary>
internal static class SecretTestValues
{
    public const string FakePlaintext = "fake-test-password-not-a-secret";

    public static string NewEnvelope(string keyId = "k1")
    {
        return $"enc:v2:{keyId}:" + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(48));
    }

    public static string NewLegacyV1Value()
    {
        return "enc:v1:" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }

    public static RtSecretValue NewProtected(out string envelope)
    {
        envelope = NewEnvelope();
        return RtSecretValue.Protected(envelope);
    }

    /// <summary>
    ///     Everything that would identify leaked secret content in serialized output.
    /// </summary>
    public static void AssertNoSecretContent(string output, params string[] values)
    {
        Assert.DoesNotContain("enc:", output, StringComparison.Ordinal);
        Assert.DoesNotContain(FakePlaintext, output, StringComparison.Ordinal);
        Assert.DoesNotContain("envelope", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("keyId", output, StringComparison.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            Assert.DoesNotContain(value, output, StringComparison.Ordinal);
        }
    }
}
