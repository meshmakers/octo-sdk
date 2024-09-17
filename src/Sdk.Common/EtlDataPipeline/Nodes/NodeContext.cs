using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

/// <summary>
/// Implementation of the node context
/// </summary>
public class NodeContext : INodeContext
{
    private readonly IPipelineLogger _logger;
    private readonly INodeConfiguration? _configurationNode;

    /// <summary>
    /// Creates a new instance of <see cref="NodeContext"/>
    /// </summary>
    /// <param name="parent">Parent node context</param>
    /// <param name="nodeQualifiedName">The qualified name of the node</param>
    /// <param name="sequenceNumber">Sequence number of the node within a transformation list</param>
    /// <param name="pipelineLogger">The logger for the pipeline</param>
    /// <param name="nodeConfiguration">The node configuration</param>
    public NodeContext(INodeContext? parent, string nodeQualifiedName, uint sequenceNumber, IPipelineLogger pipelineLogger, INodeConfiguration? nodeConfiguration)
    {
        _logger = pipelineLogger;
        _configurationNode = nodeConfiguration;

        string name = nodeQualifiedName + "[" + sequenceNumber + "]";
        if (sequenceNumber == 0 && !string.IsNullOrEmpty(nodeQualifiedName))
        {
            name = nodeQualifiedName;
        }

        if (parent != null)
        {
            NodePath = new NodePath(parent.NodePath + "/" + name);

        }
        else
        {
            NodePath = new NodePath(name);
        }
        SequenceNumber = sequenceNumber;
        NodeStack = new Stack<NodePath>();

        NodeStack = new Stack<NodePath>(parent?.NodeStack.Reverse() ?? []);
        NodeStack.Push(NodePath);
    }
    
    /// <inheritdoc />
    public NodePath NodePath { get; }

    /// <inheritdoc />
    public uint SequenceNumber { get; }
    
    /// <inheritdoc />
    public Stack<NodePath> NodeStack { get; }

    /// <inheritdoc />
    public void Debug(string message, params object[] args)
    {
        _logger.Debug(NodePath, message, args);
    }

    /// <inheritdoc />
    public void Info(string message, params object[] args)
    {
        _logger.Info(NodePath, message, args);
    }

    /// <inheritdoc />
    public void Warning(string message, params object[] args)
    {
        _logger.Warning(NodePath, message, args);
    }

    /// <inheritdoc />
    public void Error(string message, params object[] args)
    {
        _logger.Error(NodePath, message, args);
    }

    /// <inheritdoc />
    public void Error(Exception exception, string message, params object[] args)
    {
        _logger.Error(NodePath, exception, message, args);
    }

    /// <inheritdoc />
    public void Complete(IDataContext dataContext)
    {
        dataContext.Debugger?.LogOutput(NodePath, dataContext.Current); 
    }

    /// <inheritdoc />
    public T GetNodeConfiguration<T>() where T : INodeConfiguration
    {
        if (_configurationNode == null)
        {
            throw DataPipelineException.NoConfigurationNodeSet();
        }

        return (T)_configurationNode;
    }
    

}