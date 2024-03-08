namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// The pipeline logger that is used to log node and execution messages
/// </summary>
public interface IPipelineLogger
{
    /// <summary>
    /// Logs a message with debug severity
    /// </summary>
    /// <param name="nodePath">The node path</param>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Debug(string nodePath, string message, params object[] args);
    
    /// <summary>
    /// Logs a message with information severity
    /// </summary>
    /// <param name="nodePath">The node path</param>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Info(string nodePath, string message, params object[] args);
    
    /// <summary>
    /// Logs a message with warning severity
    /// </summary>
    /// <param name="nodePath">The node path</param>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Warning(string nodePath, string message, params object[] args);
    
    /// <summary>
    /// Logs a message with error severity
    /// </summary>
    /// <param name="nodePath">The node path</param>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Error(string nodePath, string message, params object[] args);

    /// <summary>
    /// Logs an exception with error severity
    /// </summary>
    /// <param name="nodePath">The node path</param>
    /// <param name="exception">The exception</param>
    /// <param name="message">The message to log</param>
    /// <param name="args">Arguments for logging message</param>
    void Error(string nodePath, Exception exception, string message, params object[] args);
}