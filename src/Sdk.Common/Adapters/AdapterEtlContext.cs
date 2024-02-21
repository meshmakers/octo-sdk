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
    /// <param name="properties">properties that are shared between the different stages of the ETL process and different runs of the pipeline</param>
    public AdapterEtlContext(string tenantId, OctoObjectId dataPipelineRtId, IDictionary<string, object?> properties)
        : base(tenantId, properties)
    {
        DataPipelineRtId = dataPipelineRtId;
    }

    /// <summary>
    /// Returns the pipeline id.
    /// </summary>
    public OctoObjectId DataPipelineRtId { get; }
}