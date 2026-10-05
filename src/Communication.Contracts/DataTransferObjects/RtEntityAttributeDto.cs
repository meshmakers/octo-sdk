using System.Text.Json.Serialization;
using Meshmakers.Octo.Communication.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents attribute of an entity.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class RtEntityAttributeDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the attribute name.
    /// </summary>
    public required string AttributeName { get; set; }

    /// <summary>
    ///     Gets or sets the attribute value. Always <c>null</c> for a <c>Secret</c> attribute
    ///     (AB#5528); see <see cref="SecretIsSet" />.
    /// </summary>
    /// <remarks>
    ///     Serialization never writes the content of an <c>RtSecretValue</c> placed here by mistake:
    ///     the converters write the marker <c>{"isSet":true}</c> instead (see
    ///     <see cref="RtSecretValueJsonConverter" />).
    /// </remarks>
    [JsonConverter(typeof(SecretSafeAttributeValueJsonConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(SecretSafeAttributeValueNewtonsoftJsonConverter))]
    public object? Value { get; set; }

    /// <summary>
    ///     For a <c>Secret</c> attribute: whether the secret is set (the value itself is never
    ///     projected, <see cref="Value" /> is <c>null</c>). <c>null</c> for every other attribute.
    ///     GraphQL field <c>secretIsSet</c> (concept §4.2). Not written when <c>null</c>, so inputs
    ///     built from this DTO stay valid against servers that do not know the field.
    /// </summary>
    [JsonPropertyName("secretIsSet")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Newtonsoft.Json.JsonProperty("secretIsSet", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public bool? SecretIsSet { get; set; }
}
