using Microsoft.Extensions.DependencyInjection;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.DependencyInjection;

/// <summary>
/// Builder for data pipeline
/// </summary>
public interface IDataPipelineBuilder
{
    /// <summary>
    ///     Gets the services.
    /// </summary>
    /// <value>
    ///     The services.
    /// </value>
    IServiceCollection Services { get; }

    /// <summary>
    /// Register a node
    /// </summary>
    /// <param name="nodeType">Type of node to register</param>
    IDataPipelineBuilder RegisterNode(Type nodeType);
    
    /// <summary>
    /// Register a node
    /// </summary>
    /// <typeparam name="TNodeType">Type of node to register</typeparam>
    /// <returns></returns>
    IDataPipelineBuilder RegisterNode<TNodeType>() where TNodeType : IPipelineNode;
}