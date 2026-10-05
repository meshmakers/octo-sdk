using System.Text.Json.Serialization;
using Meshmakers.Octo.Communication.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a query cell in a query result
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class RtQueryCellDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the attribute path.
    /// </summary>
    public required string AttributePath { get; set; }

    /// <summary>
    ///     Gets or sets the attribute value. Secret attributes are not queryable (AB#5528); should
    ///     an <c>RtSecretValue</c> end up here anyway, serialization writes the marker
    ///     <c>{"isSet":true}</c> and never its content.
    /// </summary>
    [JsonConverter(typeof(SecretSafeAttributeValueJsonConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(SecretSafeAttributeValueNewtonsoftJsonConverter))]
    public object? Value { get; set; }
}
