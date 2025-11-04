using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Describes a runtime type with its attributes.
/// </summary>
public abstract class RtTypeWithAttributesDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the attributes of the type
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    // ReSharper disable once CollectionNeverQueried.Global
    public IList<RtEntityAttributeDto>? Attributes { get; set; }
}