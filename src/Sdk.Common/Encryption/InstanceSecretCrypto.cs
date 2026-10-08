using System;
using System.Security.Cryptography;
using System.Text;
using Meshmakers.Octo.Runtime.Contracts.Secrets;

namespace Meshmakers.Octo.Sdk.Common.Encryption;

/// <summary>
/// AES-256-GCM implementation of <see cref="IInstanceSecretCrypto" />. Wire format after the
/// <c>enc:v1:</c> sentinel and Base64 decode: <c>nonce(12) ‖ tag(16) ‖ ciphertext(N)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Wire layout deliberately matches the layouts in the AI Adapter's legacy
/// <c>InstanceSecretEncryptionService</c> and the Communication Controller's
/// <c>WorkloadEncryptionService</c> so the M1 cross-service unification round-trips byte-for-byte.
/// See <c>octo-ai-services/docs/concepts/implementation-m1.md</c> §4.1 for the migration
/// context.
/// </para>
/// <para>
/// AB#5528: envelope parsing uses <c>SecretEnvelope</c> (Runtime.Contracts), the format
/// definition shared with the engine's <c>ISecretAttributeProtector</c>.
/// <see cref="Encrypt" /> still writes <c>enc:v1</c> (running clusters and their current callers
/// depend on it; the engine's protector reads it with <c>SecretEncryption:LegacyV1Key</c>, which is
/// the same <c>instance_secret_key</c>).
/// </para>
/// <para>
/// AB#5534 (decryption-oracle hardening): <see cref="Decrypt" /> decrypts <c>enc:v1</c> only and
/// refuses <c>enc:v2:&lt;kid&gt;:</c> envelopes. Those live only inside Secret attributes and are
/// decrypted by allowlisted callers through
/// <c>ISecretAttributeProtector.Unprotect(RtSecretValue)</c>; decrypting
/// an arbitrary <c>enc:v2</c> string here would let anyone holding a copied envelope have a service
/// decrypt it.
/// </para>
/// <para>
/// Implementation is stateless and thread-safe; a single instance can be registered as a singleton
/// across the host. The per-service options binder (e.g. <c>AiEncryptionOptions</c>,
/// <c>CommunicationControllerOptions</c>) is responsible for Base64-decoding the configured key
/// to a 32-byte <see cref="T:System.Byte" />[] before invoking <see cref="Encrypt" />.
/// </para>
/// </remarks>
public sealed class InstanceSecretCrypto : IInstanceSecretCrypto
{
    internal const string SentinelV1 = SecretEnvelope.PrefixV1;
    internal const int KeyLength = 32;    // AES-256
    internal const int NonceLength = SecretEnvelope.NonceLength;  // GCM standard
    internal const int TagLength = SecretEnvelope.TagLength;    // GCM standard

    /// <summary>
    /// Creates an instance that reads and writes <c>enc:v1</c>.
    /// </summary>
    public InstanceSecretCrypto()
    {
    }

    /// <summary>
    /// Kept for compatibility: hosts that register the runtime engine (<c>AddRuntimeEngine()</c>)
    /// resolve this constructor through dependency injection. Behaves exactly like
    /// the parameterless constructor; the protector is not used, in particular not to decrypt
    /// <c>enc:v2</c> envelopes (AB#5534).
    /// </summary>
    /// <param name="protector">The engine's secret protector; kept for compatibility, not used.</param>
    public InstanceSecretCrypto(ISecretAttributeProtector protector)
    {
        ArgumentNullException.ThrowIfNull(protector);
    }

    /// <inheritdoc />
    public string Encrypt(byte[] key, string plaintext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateKey(key);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var combined = new byte[NonceLength + TagLength + plaintextBytes.Length];
        var nonce = combined.AsSpan(0, NonceLength);
        var tag = combined.AsSpan(NonceLength, TagLength);
        var ciphertext = combined.AsSpan(NonceLength + TagLength);
        RandomNumberGenerator.Fill(nonce);

        using (var aes = new AesGcm(key, TagLength))
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        CryptographicOperations.ZeroMemory(plaintextBytes);
        return SentinelV1 + Convert.ToBase64String(combined);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <list type="bullet">
    /// <item>No <c>enc:</c> prefix: returned unchanged (mixed plaintext/ciphertext during rollouts).</item>
    /// <item><c>enc:v1:</c>: decrypted with <paramref name="key" />; a malformed or truncated payload
    /// throws <see cref="CryptographicException" />.</item>
    /// <item><c>enc:v2:&lt;kid&gt;:</c>: refused with <see cref="InvalidOperationException" />, never
    /// decrypted (AB#5534); the message does not contain the value. Secret attribute values are read
    /// through <c>ISecretAttributeProtector</c>.</item>
    /// <item>Any other <c>enc:</c> prefix: <see cref="CryptographicException" /> (unsupported sentinel).</item>
    /// </list>
    /// </remarks>
    public string Decrypt(byte[] key, string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(ciphertext);

        if (!IsEncrypted(ciphertext))
        {
            return ciphertext;
        }

        if (ciphertext.StartsWith(SecretEnvelope.PrefixV2, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "InstanceSecretCrypto decrypts only 'enc:v1' values; an 'enc:v2' secret envelope is refused and " +
                "not decrypted. 'enc:v2' values belong to Secret attributes and are read through the runtime " +
                "engine's ISecretAttributeProtector.");
        }

        if (!ciphertext.StartsWith(SentinelV1, StringComparison.Ordinal))
        {
            throw new CryptographicException(
                $"Unsupported encryption sentinel. Expected '{SentinelV1}'.");
        }

        ValidateKey(key);

        if (!SecretEnvelope.TryParse(ciphertext, out var info, out _, out var combined) || info.Version != 1)
        {
            throw new CryptographicException(
                "Encrypted payload is not valid: expected Base64 of nonce(12) ‖ tag(16) ‖ ciphertext.");
        }

        var nonce = combined.AsSpan(0, NonceLength);
        var tag = combined.AsSpan(NonceLength, TagLength);
        var actualCipher = combined.AsSpan(NonceLength + TagLength);
        var plaintextBytes = new byte[actualCipher.Length];

        using (var aes = new AesGcm(key, TagLength))
        {
            aes.Decrypt(nonce, actualCipher, tag, plaintextBytes);
        }

        var plaintext = Encoding.UTF8.GetString(plaintextBytes);
        CryptographicOperations.ZeroMemory(plaintextBytes);
        return plaintext;
    }

    /// <inheritdoc />
    public bool IsEncrypted(string value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith("enc:", StringComparison.Ordinal);

    private static void ValidateKey(byte[] key)
    {
        if (key.Length != KeyLength)
        {
            throw new ArgumentException(
                $"Key must be {KeyLength} bytes (AES-256); got {key.Length}.", nameof(key));
        }
    }
}
