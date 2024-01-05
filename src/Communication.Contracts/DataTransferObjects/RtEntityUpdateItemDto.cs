namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents an update item of subscription.
/// </summary>
public class RtEntityUpdateItemDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the update type.
    /// </summary>
    public UpdateTypesDto UpdateState { get; set; }
}