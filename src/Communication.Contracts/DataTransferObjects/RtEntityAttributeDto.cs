namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents attribute of an entity.
/// </summary>
public class RtEntityAttributeDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the attribute name.
    /// </summary>
    public required string AttributeName { get; set; }

    /// <summary>
    ///     Gets or sets the attribute type.
    /// </summary>
    public object? Value { get; set; }
}