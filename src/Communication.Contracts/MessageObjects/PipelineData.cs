using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
/// The base class for pipeline dat transfer between core services and adapter.
/// </summary>
public abstract record PipelineData
{
    /// <summary>
    ///     The tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the id of the data pipeline.
    /// </summary>
    public OctoObjectId DataPipelineRtId { get; set; }

    /// <summary>
    ///     The mapping object identifier.
    /// </summary>
    public RtEntityId PipelineRtEntityId { get; set; }
    
    /// <summary>
    ///     The value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    ///     The date time the transaction is started. This is the date and time a value was received from the source.
    /// </summary>
    public DateTime TransactionStartedDateTime { get; set; }
}