namespace Meshmakers.Octo.Sdk.Common.Encryption;

/// <summary>
/// At-rest encryption primitive shared across Octo services and adapters.
/// Implementations are stateless with respect to the key — every call takes the key as a
/// <see cref="T:System.Byte" />[] argument so the caller (typically a per-service
/// <c>IOptions&lt;T&gt;</c> binder) retains control over key lifecycle and rotation.
/// </summary>
/// <remarks>
/// <para>
/// Wire format is byte-format-stable across implementations: the <c>enc:v1:</c> sentinel marks
/// ciphertext; the payload after the sentinel is
/// <c>Base64(nonce(12) ‖ tag(16) ‖ ciphertext)</c> using AES-256-GCM. Cross-service decrypt
/// round-trips work whenever both replicas hold byte-identical key values (the operational
/// unification path used by the OctoMesh Helm chart's <c>global.instanceSecretKey</c>).
/// </para>
/// </remarks>
public interface IInstanceSecretCrypto
{
    /// <summary>
    /// Encrypt a UTF-8 plaintext string using the given 32-byte AES-256 key. Returns ciphertext
    /// prefixed with the <c>enc:v1:</c> sentinel and Base64-wrapped.
    /// </summary>
    /// <param name="key">A 32-byte AES-256 key. The caller owns the key lifecycle.</param>
    /// <param name="plaintext">The UTF-8 string to encrypt.</param>
    /// <returns>The encrypted value with the <c>enc:v1:</c> sentinel prefix.</returns>
    string Encrypt(byte[] key, string plaintext);

    /// <summary>
    /// Decrypt a value produced by <see cref="Encrypt" />. If the value does not carry the
    /// <c>enc:v1:</c> sentinel, the value is returned unchanged — this allows mixed
    /// plaintext/ciphertext during a gradual rollout where some rows are encrypted and others
    /// are not yet.
    /// </summary>
    /// <param name="key">The 32-byte AES-256 key that was used to encrypt the value.</param>
    /// <param name="ciphertext">A value with the <c>enc:v1:</c> sentinel, or any other string.</param>
    /// <returns>The plaintext, or the original string when no sentinel is present.</returns>
    string Decrypt(byte[] key, string ciphertext);

    /// <summary>
    /// Returns <c>true</c> when the value carries the encryption sentinel prefix
    /// (matches <c>enc:</c> generically so future sentinel versions are also detected). Callers
    /// use this to gate decrypt calls without exception-driven control flow.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <returns><c>true</c> if the string is in the encrypted wire format.</returns>
    bool IsEncrypted(string value);
}
