namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a dynamic update message.
/// </summary>
/// <typeparam name="TItem"></typeparam>
public class DynamicUpdateMessageDto<TItem> where TItem : GraphQlDto
{
    /// <summary>
    /// Collection of items
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public ICollection<TItem>? Items { get; set; }
}