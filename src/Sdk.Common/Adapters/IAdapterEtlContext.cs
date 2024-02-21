using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
/// Interface for ETL context for adapters
/// </summary>
public interface IAdapterEtlContext : IEtlContext
{
    /// <summary>
    /// Returns the pipeline id.
    /// </summary>
    OctoObjectId DataPipelineRtId { get; }
}