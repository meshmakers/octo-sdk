using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Common.Shared.DataTransferObjects;

/// <summary>
/// Represents a typed mutation.
/// </summary>
/// <typeparam name="TItemType"></typeparam>
public class MutationDto<TItemType> : MutationDto
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rtId">Id of the item to mutate.</param>
    /// <param name="item">Item to mutate.</param>
    public MutationDto(OctoObjectId rtId, TItemType item)
        : base(rtId)
    {
        Item = item;
    }

    /// <summary>
    /// Item to mutate.
    /// </summary>
    public TItemType Item { get; }
}

/// <summary>
/// Represents a mutation.
/// </summary>
public class MutationDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MutationDto"/> class.
    /// </summary>
    /// <param name="rtId">Id of the item to mutate.</param>
    public MutationDto(OctoObjectId rtId)
    {
        RtId = rtId;
    }

    /// <summary>
    /// Runtime id of the item to mutate.
    /// </summary>
    public OctoObjectId RtId { get; }
}