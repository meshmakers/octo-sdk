using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

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
    public static Exception PipelineExecutionNotFound(string tenantId, RtEntityId pipelineRtEntityId,
        Guid pipelineExecutionId)
    {
        return new PipelineExecutionException(
            $"[{tenantId}] Pipeline '{pipelineRtEntityId}' execution '{pipelineExecutionId}' not found");
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
    public static Exception PipelineRegisterTriggerFailed(string tenantId, RtEntityId pipelineRtEntityId,
        string nodeQualifiedName, Exception exception)
    {
        return new PipelineExecutionException(
            $"[{tenantId}] Pipeline '{pipelineRtEntityId}' trigger registration failed for node '{nodeQualifiedName}'",
            exception);
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger is already registered
    /// </summary>
    /// <returns></returns>
    public static Exception PipelineTriggerAlreadyRegistered(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return new PipelineExecutionException(
            $"[{tenantId}] Pipeline '{pipelineRtEntityId}' trigger already registered");
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger unregistration fails
    /// </summary>
    /// <returns></returns>
    public static Exception PipelineUnregisterTriggerFailed(string tenantId, RtEntityId pipelineRtEntityId,
        NodePath nodeContextNodePath, Exception exception)
    {
        return new PipelineExecutionException(
            $"[{tenantId}] Pipeline '{pipelineRtEntityId}' trigger unregistration failed for node '{nodeContextNodePath}'",
            exception);
    }

    /// <summary>
    /// Exception thrown when a pipeline registration fails
    /// </summary>
    /// <returns></returns>
    public static Exception PipelineRegistrationFailed(string tenantId, List<string> errorMessages)
    {
        return new PipelineExecutionException(
            $"[{tenantId}] Pipeline registration failed: {string.Join(Environment.NewLine, errorMessages)}");
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger start fails
    /// </summary>
    /// <returns></returns>
    public static Exception StartTriggerPipelineNodesFailed(string tenantId, List<string> errorMessages)
    {
        return new PipelineExecutionException(
            $"[{tenantId}] Pipeline registration failed: {string.Join(Environment.NewLine, errorMessages)}");
    }

    /// <summary>
    /// Exception thrown when a pipeline trigger end fails
    /// </summary>
    /// <returns></returns>
    public static Exception EtlContextTypeMismatch<TContext>(IEtlContext context) where TContext : class, IEtlContext
    {
        return new PipelineExecutionException(
            $"Etl context type mismatch. Expected {typeof(TContext).Name} but got {context.GetType().Name}");
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

    /// <summary>
    /// Exception thrown when a parent property is not found
    /// </summary>
    /// <param name="nodePath">Path to the node</param>
    /// <param name="fcPath">Path to the field</param>
    /// <returns></returns>
    public static Exception ParentPropertyNotFound(NodePath nodePath, string fcPath)
    {
        return new PipelineExecutionException($"[{nodePath}]: Parent property not found for field {fcPath}");
    }

    /// <summary>
    /// Exception thrown when a value is not an array
    /// </summary>
    /// <param name="nodePath">Path to the node</param>
    /// <param name="configurationPropertyName">Name of the configuration property</param>
    /// <param name="path">Path to the value</param>
    /// <returns></returns>
    public static Exception PathMustBeArray(string nodePath, string configurationPropertyName, string path)
    {
        return new PipelineExecutionException(
            $"[{nodePath}]: Configuration property '{configurationPropertyName}' defines '{path}', but the value in the pipeline is not an array");
    }

    /// <summary>
    /// Exception thrown when a value is not an array
    /// </summary>
    /// <param name="nodePath">Path to the node</param>
    /// <param name="path">Path to the value</param>
    /// <returns></returns>
    public static Exception PathNotFound(NodePath nodePath, string path)
    {
        return new PipelineExecutionException($"[{nodePath}]: Path '{path}' not found");
    }

    /// <summary>
    /// Exception thrown when a value type is not supported
    /// </summary>
    /// <param name="nodePath">Path to the node</param>
    /// <param name="valueType">Value type that is not supported</param>
    /// <param name="path">Path the value type has been loaded from</param>
    /// <returns></returns>
    public static Exception ValueTypeNotSupported(NodePath nodePath, AttributeValueTypesDto valueType, string path)
    {
        return new PipelineExecutionException(
            $"[{nodePath}]: Value type '{valueType}' is not supported for path '{path}'.");
    }

    /// <summary>
    /// Exception thrown when a value type is not supported
    /// </summary>
    /// <param name="nodePath">Path to the node</param>
    /// <param name="valueType">Value type that is not supported</param>
    /// <param name="value">Value that is not supported</param>
    /// <returns></returns>
    public static Exception DefinedValueTypeNotSupported(NodePath nodePath, AttributeValueTypesDto valueType, object? value)
    {
        return new PipelineExecutionException(
            $"[{nodePath}]: Value type '{valueType}' is not supported to convert. Defined value '{value}'.");
    }
}