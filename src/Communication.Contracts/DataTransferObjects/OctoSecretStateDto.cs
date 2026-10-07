using System.Text.Json.Serialization;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Meshmakers.Octo.Runtime.Contracts.Secrets;

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
    ///     Tells whether a runtime value found in a Secret attribute slot counts as set, classified without
    ///     a key ring (<see cref="Describe(object?, Func{string?, bool}?)" /> with <c>null</c>): a protected value, a non-empty pending
    ///     value and a legacy string (stored before the attribute became Secret) that is neither empty, a
    ///     legacy placeholder nor corrupt. <c>null</c> and <c>""</c> are not set.
    /// </summary>
    /// <param name="value">The raw attribute value</param>
    /// <returns>True when set</returns>
    public static bool IsValueSet(object? value)
    {
        return Describe(value, null).IsSet;
    }

    /// <summary>
    ///     Builds the state of a runtime value found in a Secret attribute slot without key-ring knowledge:
    ///     <see cref="IsSet" /> (see <see cref="IsValueSet" />) and <see cref="SetAt" />;
    ///     <see cref="KeyMissing" /> stays <c>false</c> because it cannot be determined.
    /// </summary>
    /// <param name="value">The raw attribute value</param>
    /// <returns>The state; never the value</returns>
    public static OctoSecretStateDto FromValue(object? value)
    {
        return FromValue(value, null);
    }

    /// <summary>
    ///     Builds the state of a runtime value found in a Secret attribute slot (AB#5534 round 2).
    /// </summary>
    /// <param name="value">The raw attribute value</param>
    /// <param name="isKnownKeyId">
    ///     True when a key id is in the host's key ring (e.g. <see cref="ISecretAttributeProtector.IsKnownKeyId" />);
    ///     <c>null</c> = no key ring: protected values count as set and <see cref="KeyMissing" /> stays <c>false</c>.
    ///     With a key ring, a protected value whose key id is unknown is <c>isSet: false, keyMissing: true</c>.
    /// </param>
    /// <returns>The state; never the value</returns>
    public static OctoSecretStateDto FromValue(object? value, Func<string?, bool>? isKnownKeyId)
    {
        var info = Describe(value, isKnownKeyId);
        return new OctoSecretStateDto(info.IsSet)
        {
            KeyMissing = info.KeyMissing,
            SetAt = info.SetAt
        };
    }

    /// <summary>
    ///     Describes a raw runtime value found in a Secret attribute slot with the engine's classification
    ///     (<see cref="SecretValueStates.Describe(RtSecretValue?, Func{string?, bool}?)" />), never decrypting it. A plain string (a legacy value read
    ///     without CK knowledge) is classified as legacy clear text; any other non-secret object counts as set.
    /// </summary>
    /// <param name="value">The raw attribute value</param>
    /// <param name="isKnownKeyId">True when a key id is in the key ring; <c>null</c> = no key ring</param>
    /// <returns>The description (state, form, key id, set-at)</returns>
    public static SecretReadInfo Describe(object? value, Func<string?, bool>? isKnownKeyId)
    {
        return Describe(value, isKnownKeyId, true);
    }

    /// <summary>
    ///     Like <see cref="Describe(object?, Func{string?, bool}?)" />, plus whether the host's legacy <c>enc:v1</c>
    ///     key is configured (<see cref="ISecretAttributeProtector.IsLegacyV1KeyConfigured" />): without it a legacy
    ///     <c>enc:v1</c> string is <c>isSet: false, keyMissing: true</c> with key id
    ///     <see cref="SecretValueStates.LegacyV1KeyId" />, as in GraphQL and the secrets overview (AB#5532).
    /// </summary>
    /// <param name="value">The raw attribute value</param>
    /// <param name="isKnownKeyId">True when a key id is in the key ring; <c>null</c> = no key ring</param>
    /// <param name="legacyV1KeyConfigured">True when the legacy <c>enc:v1</c> key is configured</param>
    /// <returns>The description (state, form, key id, set-at)</returns>
    public static SecretReadInfo Describe(object? value, Func<string?, bool>? isKnownKeyId,
        bool legacyV1KeyConfigured)
    {
        return value switch
        {
            null => SecretValueStates.Describe(null, isKnownKeyId, legacyV1KeyConfigured),
            RtSecretValue secret => SecretValueStates.Describe(secret, isKnownKeyId, legacyV1KeyConfigured),
            string text => SecretValueStates.Describe(RtSecretValue.LegacyPlaintext(text), isKnownKeyId,
                legacyV1KeyConfigured),
            _ => new SecretReadInfo(SecretValueState.Set, SecretStorageForm.Plaintext, null, null)
        };
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return KeyMissing ? "{ isSet: false, keyMissing: true }" : IsSet ? "{ isSet: true }" : "{ isSet: false }";
    }
}
