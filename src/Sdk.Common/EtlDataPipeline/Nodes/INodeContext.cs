using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

/// <summary>
/// Interface for the node context
/// </summary>
public interface INodeContext
{
    /// <summary>
    /// Gets the node path
    /// </summary>
    NodePath NodePath { get; }
    
    /// <summary>
    /// Gets the sequence number of the node within a transformation list.
    /// </summary>
    uint SequenceNumber { get; }

    /// <summary>
    /// Get configuration for the current node
    /// </summary>
    /// <typeparam name="T">Generic type of configuration</typeparam>
    /// <returns></returns>
    T GetNodeConfiguration<T>() where T : INodeConfiguration;
    
    /// <summary>
    /// Returns the node calling stack
    /// </summary>
    Stack<NodePath> NodeStack { get; }
    
    /// <summary>
    /// Logs a message with debug severity
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Debug(string message, params object[] args);
    
    /// <summary>
    /// Logs a message with information severity
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Info(string message, params object[] args);
    
    /// <summary>
    /// Logs a message with warning severity
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Warning(string message, params object[] args);
    
    /// <summary>
    /// Logs a message with error severity
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Error(string message, params object[] args);

    /// <summary>
    /// Logs an exception with error severity
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Error(Exception exception, string message, params object[] args);
    
    /// <summary>
    /// Completes the node context
    /// </summary>
    /// <param name="dataContext"></param>
    void Complete(IDataContext dataContext);
}