using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Exception thrown when a pipeline execution fails
/// </summary>
public class PipelineExecutionException : Exception
{
    /// <inheritdoc />
    public PipelineExecutionException()
    {
    }

    /// <inheritdoc />
    // ReSharper disable once MemberCanBePrivate.Global
    public PipelineExecutionException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    // ReSharper disable once MemberCanBePrivate.Global
    public PipelineExecutionException(string message, Exception inner) : base(message, inner)
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
    public static Exception PipelineExecutionFailed(string tenantId, RtEntityId pipelineRtEntityId, Exception exception)
    {
        string messages = "";
        Exception? tmpException = exception;
        while (tmpException != null)
        {
            messages += tmpException.Message + Environment.NewLine;
            tmpException = exception.InnerException;
        }
        
        return new PipelineExecutionException($"[{tenantId}] Pipeline '{pipelineRtEntityId}' execution failed: {Environment.NewLine}{messages}", exception);
    }

    /// <summary>
    /// Exception thrown when a pipeline execution is not found
    /// </summary>
    public static Exception PipelineExecutionNotFound(string tenantId, RtEntityId pipelineRtEntityId, Guid pipelineExecutionId)
    {
        return new PipelineExecutionException($"[{tenantId}] Pipeline '{pipelineRtEntityId}' execution '{pipelineExecutionId}' not found");        
    }
}
