using System.Text.Json.Serialization;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Read-side state of a <c>Secret</c> attribute (AB#5528, concept §4.1): the only thing an API
///     ever tells about a secret is whether it is set. GraphQL type <c>OctoSecretState</c> with the
///     field <c>isSet</c>; the JSON form is <c>{"isSet":true}</c>.
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
        return IsSet ? "{ isSet: true }" : "{ isSet: false }";
    }
}
