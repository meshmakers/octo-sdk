using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// The data context is used to pass data between nodes in the data pipeline.
/// </summary>
public class DataContext : IDataContext
{
    private ConfigurationNode? _configurationNode;
    
    /// <summary>
    /// Creates a new instance of <see cref="DataContext"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    public DataContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        Logger = loggerFactory.CreateLogger("DataPipeline");
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public T GetNodeConfiguration<T>() where T : ConfigurationNode
    {
        if (_configurationNode == null)
        {
            throw DataPipelineException.NoConfigurationNodeSet();
        }
        return (T)_configurationNode;
    }
    
    /// <summary>
    /// Sets the current configuration node.
    /// </summary>
    /// <param name="node"></param>
    public void SetConfigurationNode(ConfigurationNode node)
    {
        _configurationNode = node;
    }
}

/// <summary>
/// Object data context
/// </summary>
public class ObjectDataContext : DataContext, IObjectDataContext
{
    /// <summary>
    /// Creates a new instance of <see cref="ObjectDataContext"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="source"></param>
    public ObjectDataContext(IServiceProvider serviceProvider, object source) 
        : base(serviceProvider)
    {
        Source = source;
    }

    /// <inheritdoc />
    public object Source { get; }
}

/// <summary>
/// Signal data context
/// </summary>
public class SignalDataContext : DataContext, ISignalDataContext
{
    /// <summary>
    /// Creates a new instance of <see cref="SignalDataContext"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="value"></param>
    public SignalDataContext(IServiceProvider serviceProvider, object? value) 
        : base(serviceProvider)
    {
        Value = value;
    }

    /// <inheritdoc />
    public object? Value { get; }

    /// <inheritdoc />
    public T? GetValue<T>()
    {
        return (T?)Convert.ChangeType(Value, typeof(T));
    }
}