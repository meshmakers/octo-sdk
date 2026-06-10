#if !NETSTANDARD2_0
using System;
using System.Security.Cryptography;
using System.Text;

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
/// Implementation is stateless and thread-safe; a single instance can be registered as a singleton
/// across the host. The per-service options binder (e.g. <c>AiEncryptionOptions</c>,
/// <c>CommunicationControllerOptions</c>) is responsible for Base64-decoding the configured key
/// to a 32-byte <see cref="T:System.Byte" />[] before invoking <see cref="Encrypt" />.
/// </para>
/// </remarks>
public sealed class InstanceSecretCrypto : IInstanceSecretCrypto
{
    internal const string SentinelV1 = "enc:v1:";
    internal const int KeyLength = 32;    // AES-256
    internal const int NonceLength = 12;  // GCM standard
    internal const int TagLength = 16;    // GCM standard

    /// <inheritdoc />
    public string Encrypt(byte[] key, string plaintext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(plaintext);
        if (key.Length != KeyLength)
        {
            throw new ArgumentException(
                $"Key must be {KeyLength} bytes (AES-256); got {key.Length}.", nameof(key));
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagLength];

        using var aes = new AesGcm(key, TagLength);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var combined = new byte[NonceLength + TagLength + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceLength);
        Buffer.BlockCopy(tag, 0, combined, NonceLength, TagLength);
        Buffer.BlockCopy(ciphertext, 0, combined, NonceLength + TagLength, ciphertext.Length);

        return SentinelV1 + Convert.ToBase64String(combined);
    }

    /// <inheritdoc />
    public string Decrypt(byte[] key, string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(ciphertext);

        if (!IsEncrypted(ciphertext))
        {
            return ciphertext;
        }

        if (!ciphertext.StartsWith(SentinelV1, StringComparison.Ordinal))
        {
            throw new CryptographicException(
                $"Unsupported encryption sentinel. Expected '{SentinelV1}'.");
        }

        if (key.Length != KeyLength)
        {
            throw new ArgumentException(
                $"Key must be {KeyLength} bytes (AES-256); got {key.Length}.", nameof(key));
        }

        var payload = ciphertext[SentinelV1.Length..];
        byte[] combined;
        try
        {
            combined = Convert.FromBase64String(payload);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Encrypted payload is not valid Base64.", ex);
        }

        if (combined.Length < NonceLength + TagLength)
        {
            throw new CryptographicException("Encrypted payload is truncated.");
        }

        var nonce = new byte[NonceLength];
        var tag = new byte[TagLength];
        var actualCipher = new byte[combined.Length - NonceLength - TagLength];
        Buffer.BlockCopy(combined, 0, nonce, 0, NonceLength);
        Buffer.BlockCopy(combined, NonceLength, tag, 0, TagLength);
        Buffer.BlockCopy(combined, NonceLength + TagLength, actualCipher, 0, actualCipher.Length);

        var plaintextBytes = new byte[actualCipher.Length];
        using var aes = new AesGcm(key, TagLength);
        aes.Decrypt(nonce, actualCipher, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    /// <inheritdoc />
    public bool IsEncrypted(string value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith("enc:", StringComparison.Ordinal);
}
#endif
