namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
/// <summary>
///     Represents the configuration of a pool for data transfer.
/// </summary>
public record PoolConfigurationDto
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PoolConfigurationDto" /> class.
    /// </summary>
    /// <param name="communicationAdapterList">Communication adapters associated with the pool.</param>
    public PoolConfigurationDto(IEnumerable<PoolCommunicationAdapterDto> communicationAdapterList)
    {
        CommunicationAdapterList = communicationAdapterList;
    }

    /// <summary>
    ///     Gets or sets communication adapters associated with the pool.
    /// </summary>
    public IEnumerable<PoolCommunicationAdapterDto> CommunicationAdapterList { get; } = null!;
}