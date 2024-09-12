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

    /// <summary>
    /// Exception thrown when a pipeline is not found 
    /// </summary>
    public static Exception PipelineNotFound(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return new PipelineExecutionException($"[{tenantId}]Pipeline '{pipelineRtEntityId}' not found");
    }

    /// <summary>
    /// Exception thrown when a pipeline execution fails
    /// </summary>
    public static Exception PipelineExecutionFailed(string tenantId, OctoObjectId dataPipelineRtId, RtEntityId pipelineRtEntityId, Exception exception)
    {
        string messages = "";
        Exception? tmpException = exception;
        while (tmpException != null)
        {
            messages += tmpException.Message + Environment.NewLine;
            tmpException = exception.InnerException;
        }
        
        return new PipelineExecutionException($"[{tenantId}] Pipeline '{pipelineRtEntityId}' (data pipeline '{dataPipelineRtId}') execution failed: {Environment.NewLine}{messages}", exception);
    }
}
