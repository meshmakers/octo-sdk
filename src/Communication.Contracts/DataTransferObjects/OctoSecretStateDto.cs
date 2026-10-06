using System.Text.Json.Serialization;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Read-side state of a <c>Secret</c> attribute (AB#5528, concept §4.1): an API only ever tells
///     whether a secret is set, whether a stored value cannot be read because its key id is missing in
///     this environment's key ring, and when it was set - never the value. GraphQL type
///     <c>OctoSecretState { isSet keyMissing setAt }</c>; the JSON form is e.g. <c>{"isSet":true}</c>.
/// </summary>
/// <remarks>
///     Generated query DTOs (<c>Sdk.SourceGeneration</c>) expose Secret attributes as this type, so
///     code that read <c>.Password</c> as a string stops compiling instead of silently receiving
///     ciphertext. Writes use a plain <c>string?</c> on the mutation DTO and
///     <see cref="MutationDto.ClearSecretAttributes" /> to clear.
/// </remarks>
public sealed class OctoSecretStateDto
{
    /// <summary>
    ///     Creates a state that is not set (needed for deserialization).
    /// </summary>
    public OctoSecretStateDto()
    {
    }

    /// <summary>
    ///     Creates a state.
    /// </summary>
    /// <param name="isSet">True when the secret holds a value</param>
    public OctoSecretStateDto(bool isSet)
    {
        IsSet = isSet;
    }

    /// <summary>
    ///     True when the secret holds a value. Never carries the value itself.
    /// </summary>
    [JsonPropertyName("isSet")]
    [Newtonsoft.Json.JsonProperty("isSet")]
    public bool IsSet { get; set; }

    /// <summary>
    ///     True when a value is stored but cannot be read because its key id is not in this
    ///     environment's key ring (e.g. after a restore from another environment); <see cref="IsSet" />
    ///     is <c>false</c> then and the secret has to be entered again (decision 2026-10-06).
    ///     GraphQL field <c>keyMissing</c>. Not written when <c>false</c>, so the serialized form of a
    ///     readable or unset secret stays the marker <c>{"isSet":…}</c> that write paths accept as
    ///     "unchanged".
    /// </summary>
    [JsonPropertyName("keyMissing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Newtonsoft.Json.JsonProperty("keyMissing", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)]
    public bool KeyMissing { get; set; }

    /// <summary>
    ///     When the current value was set (UTC); <c>null</c> when not set or for values set before this
    ///     was recorded (legacy). GraphQL field <c>setAt</c>. Not written when <c>null</c>.
    /// </summary>
    [JsonPropertyName("setAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Newtonsoft.Json.JsonProperty("setAt", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public DateTime? SetAt { get; set; }

    /// <summary>
    ///     Tells whether a runtime value found in a Secret attribute slot counts as set: any
    ///     <see cref="RtSecretValue" /> (protected, legacy or pending) and any non-empty string
    ///     (legacy clear text stored before the attribute became Secret). <c>null</c> and <c>""</c>
    ///     are not set.
    /// </summary>
    /// <param name="value">The raw attribute value</param>
    /// <returns>True when set</returns>
    public static bool IsValueSet(object? value)
    {
        return value switch
        {
            null => false,
            RtSecretValue => true,
            string text => text.Length > 0,
            _ => true
        };
    }

    /// <summary>
    ///     Builds the state of a runtime value found in a Secret attribute slot
    ///     (see <see cref="IsValueSet" />).
    /// </summary>
    /// <param name="value">The raw attribute value</param>
    /// <returns>The state; never the value</returns>
    public static OctoSecretStateDto FromValue(object? value)
    {
        return new OctoSecretStateDto(IsValueSet(value));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return KeyMissing ? "{ isSet: false, keyMissing: true }" : IsSet ? "{ isSet: true }" : "{ isSet: false }";
    }
}
