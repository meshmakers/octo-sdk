namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Encryption status of the environment as seen from one tenant (bot services
///     <c>GET {tenantId}/v1/secrets/status</c>, AB#5528/AB#5544). Environment-level and identical in every
///     tenant except <see cref="LastVerifyAt" />. Never contains key material.
/// </summary>
public class SecretEnvironmentStatusDto
{
    /// <summary>
    ///     <c>false</c>: no key ring is configured - secret inputs are disabled, writes would fail with
    ///     <c>SecretEncryptionNotConfigured</c>.
    /// </summary>
    public bool KeyRingConfigured { get; set; }

    /// <summary>
    ///     Key id new values are encrypted with; <c>null</c> when not configured.
    /// </summary>
    public string? ActiveKeyId { get; set; }

    /// <summary>
    ///     All key ids of the key ring.
    /// </summary>
    public List<string> KnownKeyIds { get; set; } = [];

    /// <summary>
    ///     True when the legacy <c>enc:v1</c> key is configured (to read legacy values).
    /// </summary>
    public bool LegacyV1KeyConfigured { get; set; }

    /// <summary>
    ///     True when strict mode is in force (legacy clear text / <c>enc:v1</c> no longer accepted).
    /// </summary>
    public bool StrictMode { get; set; }

    /// <summary>
    ///     When strict mode is scheduled / became active (UTC), or <c>null</c>.
    /// </summary>
    public DateTime? StrictModeSince { get; set; }

    /// <summary>
    ///     Cron expression of the recurring Verify sweep; <c>null</c> when disabled.
    /// </summary>
    public string? RecurringVerifyCron { get; set; }

    /// <summary>
    ///     This tenant's last Verify run (UTC), <c>null</c> if none.
    /// </summary>
    public DateTime? LastVerifyAt { get; set; }
}
