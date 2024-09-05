using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

/// <summary>
/// Configuration for the PrintDebugNode
/// </summary>
[NodeName("PrintDebug", 1)]
public class PrintDebugNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// The severity of the log message.
    /// </summary>
    public LoggerSeverity Severity { get; set; } = LoggerSeverity.Information;
}


/// <summary>
/// Prints the current object to the log
/// </summary>
[NodeConfiguration(typeof(PrintDebugNodeConfiguration))]
public class PrintDebugNode(NodeDelegate next) : IPipelineNode
{
    /// <summary>
    /// Processes the current object
    /// </summary>
    /// <param name="dataContext"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var config = dataContext.GetNodeConfiguration<PrintDebugNodeConfiguration>();
        var logger = dataContext.Logger;

        switch (config.Severity)
        {
            case LoggerSeverity.Debug:
                logger.Debug(dataContext.NodeStack.Peek(), dataContext.Current?.ToString() ?? "null");
                break;
            case LoggerSeverity.Information:
                logger.Info(dataContext.NodeStack.Peek(), dataContext.Current?.ToString() ?? "null");
                break;
            case LoggerSeverity.Warning:
                logger.Warning(dataContext.NodeStack.Peek(), dataContext.Current?.ToString() ?? "null");
                break;
            case LoggerSeverity.Error:
                logger.Error(dataContext.NodeStack.Peek(), dataContext.Current?.ToString() ?? "null");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(config.Severity), config.Severity, null);
        }
        
        await next(dataContext);
    }
}