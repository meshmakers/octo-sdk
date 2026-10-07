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

    /// <summary>
    ///     Warning codes about the environment (JSON <c>warnings</c>, AB#5534); empty when there is nothing to
    ///     warn about. Known codes: <see cref="SecretEnvironmentWarningCodes" />. Clients show unknown codes as
    ///     a generic warning.
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    ///     Key ids of all encrypted (<c>.octoenc</c>) dumps currently held by bot services for this instance
    ///     (pre-sweep dumps, tenant dumps, staged restore uploads), read from their clear-text headers (JSON
    ///     <c>requiredKeyIds</c>, AB#5559). Every one of them must stay in the key ring until the newest dump
    ///     encrypted with it has expired; a missing one is reported as
    ///     <see cref="SecretEnvironmentWarningCodes.DumpKeyMissing" />. Empty when there are no encrypted dumps or
    ///     the service is older than AB#5559. Never key material.
    /// </summary>
    public List<string> RequiredKeyIds { get; set; } = [];
}

/// <summary>
///     Codes of <see cref="SecretEnvironmentStatusDto.Warnings" /> (AB#5534).
/// </summary>
public static class SecretEnvironmentWarningCodes
{
    /// <summary>
    ///     No key ring is configured on the bot (<see cref="SecretEnvironmentStatusDto.KeyRingConfigured" /> is
    ///     <c>false</c>): secret writes fail, and a restore only classifies the restored secrets (key-free Verify)
    ///     and lists the ones to re-enter - nothing is encrypted until the key ring is set and Encrypt runs.
    ///     UIs show a prominent banner.
    /// </summary>
    public const string NoKeyRing = "NoKeyRing";

    /// <summary>
    ///     The legacy <c>enc:v1</c> key is not configured although the tenant's last secret sweep found
    ///     <c>enc:v1</c> values (counted as key missing, key id <c>enc:v1</c>): they cannot be read or converted
    ///     until <c>SecretEncryption:LegacyV1Key</c> is configured.
    /// </summary>
    public const string NoLegacyV1Key = "NoLegacyV1Key";

    /// <summary>
    ///     An encrypted dump needs a key id (<see cref="SecretEnvironmentStatusDto.RequiredKeyIds" />) that is not
    ///     in the key ring (AB#5559): it can no longer be decrypted, i.e. neither restored nor downloaded. Put the
    ///     key back into <c>SecretEncryption:Keys</c>, or accept the loss until the dump expires.
    /// </summary>
    public const string DumpKeyMissing = "DumpKeyMissing";
}
