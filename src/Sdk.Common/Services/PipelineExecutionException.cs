using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Exception thrown when a pipeline execution fails
/// </summary>
public class PipelineExecutionException : Exception
{
    private PipelineExecutionException()
    {
    }

    private PipelineExecutionException(string message) : base(message)
    {
    }

    private PipelineExecutionException(string message, Exception inner) : base(message, inner)
    {
    }

    internal static Exception PipelineNotFound(string tenantId, OctoObjectId pipelineConfigurationDataPipelineRtId)
    {
        return new PipelineExecutionException("Pipeline '{pipelineConfigurationDataPipelineRtId}' not found for tenant '{tenantId}'");
    }
}
