namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Describes a runtime type with its attributes.
/// </summary>
public abstract class RtTypeWithAttributesDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the attributes of the record
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IList<RtEntityAttributeDto>? Attributes { get; set; }
}