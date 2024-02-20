using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// The data context is used to pass data between nodes in the data pipeline.
/// </summary>
public abstract class DataContext : IDataContext
{
    private NodeConfiguration? _configurationNode;

    /// <summary>
    /// Creates a new instance of <see cref="DataContext"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected DataContext(IServiceProvider serviceProvider)
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
    public T GetNodeConfiguration<T>() where T : NodeConfiguration
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
    public void SetConfigurationNode(NodeConfiguration node)
    {
        _configurationNode = node;
    }
}

/// <summary>
/// Data context of extracted stage
/// </summary>
public class ExtractDataContext : DataContext, IExtractDataContext
{
    /// <summary>
    /// Creates a new instance of <see cref="ExtractDataContext"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    public ExtractDataContext(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <inheritdoc />
    public object? Source { get; set; }
}

/// <summary>
/// Data context of transform stage
/// </summary>
public class TransformDataContext : DataContext, ITransformDataContext
{
    /// <summary>
    /// Creates a new instance of <see cref="ExtractDataContext"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="source"></param>
    public TransformDataContext(IServiceProvider serviceProvider, JToken? source)
        : base(serviceProvider)
    {
        Source = source;
        Target = new JObject();
    }

    /// <inheritdoc />
    public JToken? Source { get; }

    /// <inheritdoc />
    public JToken Target { get; private set; }

    /// <inheritdoc />
    public T? GetSourceValueByPath<T>(string path)
    {
        if (Source == null)
        {
            throw DataPipelineException.SourceIsNull();
        }
        var v = Source.SelectToken(path);
        if (v == null)
        {
            return default;
        }
        
        if (typeof(T).IsPrimitive && v is JObject)
        {
            throw DataPipelineException.ValueIsObjectButMustBePrimitive(path);
        }
        
        return v.Value<T?>();
    }

    /// <inheritdoc />
    public void SetTargetValueByName<T>(string? propertyName, T? value)
    {
        JToken targetValue;
        if (value is JToken jToken)
        {
            targetValue = jToken;
        }
        else
        {
            targetValue = JToken.FromObject(value!);
        }

        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            var token = Target.SelectToken(propertyName);
            if (token == null)
            {
                Target[propertyName] = targetValue;
            }
            else
            {
                token.Replace(targetValue);
            }
        }
        else
        {
            Target = targetValue;
        }
    }
    
    /// <inheritdoc />
    public void SetTargetValue<T>(T value)
    {
        SetTargetValueByName(null, value);
    }
}

/// <summary>
/// Data context of load stage
/// </summary>
public class LoadDataContext : DataContext, ILoadDataContext
{
    /// <summary>
    /// Creates a new instance of <see cref="LoadDataContext"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="target"></param>
    public LoadDataContext(IServiceProvider serviceProvider, JToken target)
        : base(serviceProvider)
    {
        Target = target;
    }

    /// <inheritdoc />
    public JToken Target { get; }
}
