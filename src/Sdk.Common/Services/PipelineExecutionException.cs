using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

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
    /// Exception thrown when a pipeline execution is not found
    /// </summary>
    public static Exception PipelineExecutionNotFound(string tenantId, RtEntityId pipelineRtEntityId, Guid pipelineExecutionId)
    {
        return new PipelineExecutionException($"[{tenantId}] Pipeline '{pipelineRtEntityId}' execution '{pipelineExecutionId}' not found");        
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger is missing
    /// </summary>
    public static Exception PipelineTriggerMissing(string tenantId, RtEntityId pipelineRtEntityId)
    {
       return new PipelineExecutionException($"[{tenantId}] Pipeline '{pipelineRtEntityId}' trigger missing");
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger registration fails
    /// </summary>
    /// <returns></returns>
    public static Exception PipelineRegisterTriggerFailed(string tenantId, RtEntityId pipelineRtEntityId, string nodeQualifiedName, Exception exception)
    {
        return new PipelineExecutionException($"[{tenantId}] Pipeline '{pipelineRtEntityId}' trigger registration failed for node '{nodeQualifiedName}'", exception);
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger is already registered
    /// </summary>
    /// <returns></returns>
    public static Exception PipelineTriggerAlreadyRegistered(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return new PipelineExecutionException($"[{tenantId}] Pipeline '{pipelineRtEntityId}' trigger already registered");
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger unregistration fails
    /// </summary>
    /// <returns></returns>
    public static Exception PipelineUnregisterTriggerFailed(string tenantId, RtEntityId pipelineRtEntityId, NodePath nodeContextNodePath, Exception exception)
    {
        return new PipelineExecutionException($"[{tenantId}] Pipeline '{pipelineRtEntityId}' trigger unregistration failed for node '{nodeContextNodePath}'", exception);
    }

    /// <summary>
    /// Exception thrown when a pipeline registration fails
    /// </summary>
    /// <returns></returns>
    public static Exception PipelineRegistrationFailed(string tenantId, List<string> errorMessages)
    {
        return new PipelineExecutionException($"[{tenantId}] Pipeline registration failed: {string.Join(Environment.NewLine, errorMessages)}");
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger start fails
    /// </summary>
    /// <returns></returns>
    public static Exception StartTriggerPipelineNodesFailed(string tenantId, List<string> errorMessages)
    {
        return new PipelineExecutionException($"[{tenantId}] Pipeline registration failed: {string.Join(Environment.NewLine, errorMessages)}");
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger end fails
    /// </summary>
    /// <returns></returns>
    public static Exception EtlContextTypeMismatch<TContext>(IEtlContext context) where TContext : class, IEtlContext
    {
        return new PipelineExecutionException($"Etl context type mismatch. Expected {typeof(TContext).Name} but got {context.GetType().Name}");
    }

    /// <summary>
    /// Exception thrown when a global configuration parameter is not found
    /// </summary>
    /// <param name="configurationName">Configuration name</param>
    /// <returns></returns>
    public static Exception GlobalConfigurationParameterNotFound(string configurationName)
    {
        return new PipelineExecutionException($"Global configuration parameter '{configurationName}' not found");
    }
}
