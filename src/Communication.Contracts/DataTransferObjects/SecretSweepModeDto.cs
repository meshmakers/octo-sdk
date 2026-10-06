using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Mode of a secret sweep (AB#5528 §5.2, bot-services AB#5539). Mirrors the engine's
///     <c>SecretSweepMode</c> (same names and numbers); serialized by name.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SecretSweepModeDto>))]
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum SecretSweepModeDto
{
    /// <summary>
    ///     Count the stored forms only; writes nothing.
    /// </summary>
    Verify = 0,

    /// <summary>
    ///     Encrypt legacy clear text and <c>enc:v1</c> values with the active key.
    /// </summary>
    Encrypt = 1,

    /// <summary>
    ///     Re-encrypt every value that is not under the active key id (key rotation).
    /// </summary>
    Reprotect = 2,

    /// <summary>
    ///     Set values whose key id is unknown to "not set" (typically after a cross-environment restore).
    /// </summary>
    ClearUnknownKid = 3,

    /// <summary>
    ///     Emergency decryption back to clear text. Never offered through the bot API (the service answers
    ///     <c>400</c>, the client refuses it up front); listed only so reports stay readable.
    /// </summary>
    Decrypt = 4
}

/// <summary>
///     What started a secret sweep.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SecretSweepTriggerDto>))]
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum SecretSweepTriggerDto
{
    /// <summary>
    ///     Started on demand (API, CLI, dashboard) for one tenant or all tenants.
    /// </summary>
    Manual = 0,

    /// <summary>
    ///     The recurring verify job.
    /// </summary>
    Recurring = 1,

    /// <summary>
    ///     A repository restore.
    /// </summary>
    Restore = 2
}

/// <summary>
///     Outcome of the secret sweep of one tenant.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SecretSweepOutcomeDto>))]
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum SecretSweepOutcomeDto
{
    /// <summary>
    ///     Every step completed without failures.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    ///     Every step completed, but some values could not be processed (see the steps' failures).
    /// </summary>
    CompletedWithFailures = 1,

    /// <summary>
    ///     Nothing was done (see <see cref="SecretSweepReportDto.Reason" />), e.g. no keys configured or the
    ///     pre-sweep dump could not be taken.
    /// </summary>
    Skipped = 2,

    /// <summary>
    ///     The sweep aborted with an error (see <see cref="SecretSweepReportDto.Reason" />).
    /// </summary>
    Failed = 3
}

/// <summary>
///     Stored form of a Secret value. Mirrors the engine's <c>SecretValueForm</c>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SecretValueFormDto>))]
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum SecretValueFormDto
{
    /// <summary>
    ///     No value.
    /// </summary>
    NotSet = 0,

    /// <summary>
    ///     Legacy placeholder or empty string.
    /// </summary>
    Placeholder = 1,

    /// <summary>
    ///     Legacy clear text.
    /// </summary>
    Plaintext = 2,

    /// <summary>
    ///     Legacy <c>enc:v1</c>.
    /// </summary>
    EncV1 = 3,

    /// <summary>
    ///     <c>enc:v2</c> with a known key id.
    /// </summary>
    EncV2 = 4,

    /// <summary>
    ///     <c>enc:v2</c> with an unknown key id.
    /// </summary>
    UnknownKeyId = 5
}
