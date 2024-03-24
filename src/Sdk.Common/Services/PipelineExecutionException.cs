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

    internal static Exception PipelineNotFound(string tenantId, OctoObjectId pipelineRtId)
    {
        return new PipelineExecutionException($"Pipeline '{pipelineRtId}' not found for tenant '{tenantId}'");
    }
}
