using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for node logger
/// </summary>
[NodeName("Logger", 1)]
public class LoggerNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// Message to log
    /// </summary>
    public string Message { get; init; } = null!;
}

/// <summary>
/// Logs a message
/// </summary>
/// <param name="next"></param>
[NodeConfiguration(typeof(LoggerNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
internal class LoggerNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<LoggerNodeConfiguration>();
        dataContext.Logger.Info(dataContext.NodeStack.Peek(), c.Message);

        await next(dataContext);
    }
}