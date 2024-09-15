using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
/// Implementation of the <see cref="IEtlContext"/> interface for execution of an adapter.
/// </summary>
public class AdapterEtlContext : DefaultEtlContext, IAdapterEtlContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="AdapterEtlContext"/> class.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="dataPipelineRtId">Data pipeline runtime identifier</param>
    /// <param name="pipelineExecutionId">Guid that identifies the pipeline execution instance</param>
    /// <param name="pipelineRtEntityId">Data pipeline runtime identifier</param>
    /// <param name="transactionStartedDateTime">Date and time when the transaction started</param>
    /// <param name="externalReceivedDateTime">Date and time when the value was received by an optional external system</param>
    /// <param name="properties">properties that are shared between the different stages of the ETL process and different runs of the pipeline</param>
    public AdapterEtlContext(string tenantId, OctoObjectId dataPipelineRtId, Guid pipelineExecutionId, RtEntityId pipelineRtEntityId, DateTime transactionStartedDateTime, DateTime? externalReceivedDateTime, IDictionary<string, object?> properties)
        : base(tenantId, dataPipelineRtId, pipelineExecutionId, pipelineRtEntityId, transactionStartedDateTime, externalReceivedDateTime, properties)
    {
    }
}