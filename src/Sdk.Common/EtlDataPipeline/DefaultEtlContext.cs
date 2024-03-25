using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Represents the default implementation of the <see cref="IEtlContext"/> interface.
/// </summary>
public class DefaultEtlContext : IEtlContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="DefaultEtlContext"/> class.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="dataPipelineRtId">Data pipeline runtime identifier</param>
    /// <param name="pipelineRtEntityEntityId">Pipeline identifier</param>
    /// <param name="transactionStartedDateTime">Date and time when the transaction started</param>
    /// <param name="externalReceivedDateTime">Date and time when the value was received by an optional external system</param>
    /// <param name="properties">properties that are shared between the different stages of the ETL process and different runs of the pipeline</param>
    public DefaultEtlContext(string tenantId, OctoObjectId dataPipelineRtId, RtEntityId pipelineRtEntityEntityId, DateTime transactionStartedDateTime, DateTime? externalReceivedDateTime, IDictionary<string, object?> properties)
    {
        TenantId = tenantId;
        DataPipelineRtId = dataPipelineRtId;
        PipelineRtEntityId = pipelineRtEntityEntityId;
        ExternalReceivedDateTime = externalReceivedDateTime;
        TransactionStartedDateTime = transactionStartedDateTime;
        Properties = properties;
    }

    /// <inheritdoc />
    public string TenantId { get; }

    /// <inheritdoc />
    public OctoObjectId DataPipelineRtId { get; }

    /// <inheritdoc />
    public DateTime TransactionStartedDateTime { get; }
    
    /// <inheritdoc />
    public RtEntityId PipelineRtEntityId { get; }

    /// <inheritdoc />
    public DateTime? ExternalReceivedDateTime { get; }
    
    /// <inheritdoc />
    public IDictionary<string, object?> Properties { get; }
}