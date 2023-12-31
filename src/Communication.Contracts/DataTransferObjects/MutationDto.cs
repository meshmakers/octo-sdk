using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a typed mutation.
/// </summary>
/// <typeparam name="TItemType"></typeparam>
public class MutationDto<TItemType> : MutationDto where TItemType : class
{
    /// <summary>
    /// Item to mutate.
    /// </summary>
    public TItemType Item { get; set; } = null!;
}

/// <summary>
/// Represents a mutation.
/// </summary>
public class MutationDto
{
    /// <summary>
    /// Runtime id of the item to mutate.
    /// </summary>
    public OctoObjectId RtId { get; set; }
}