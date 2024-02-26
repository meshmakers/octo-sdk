using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Interface for an extract-transform-load data orchestrator
/// </summary>
public interface IEtlDataOrchestrator
{
    /// <summary>
    /// Executes the pipeline
    /// </summary>
    /// <param name="pipelineConfigurationRoot">Configuration of the data pipeline to run</param>
    /// <param name="etlContext">Context the data pipeline is running in to pass information about tenants, adapters etc.</param>
    Task<object?> ExecutePipelineAsync<TContext>(PipelineConfigurationRoot pipelineConfigurationRoot, TContext etlContext)
        where TContext : class, IEtlContext;
}