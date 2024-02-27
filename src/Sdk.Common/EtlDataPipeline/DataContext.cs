using System.Diagnostics.CodeAnalysis;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// The data context is used to pass data between nodes in the data pipeline.
/// </summary>
public class DataContext : IDataContext
{
    private INodeConfiguration? _configurationNode;

    /// <summary>
    /// Creates a new instance of <see cref="DataContext"/>
    /// </summary>
    /// <param name="globalServiceProvider">Service provider for the global services</param>
    /// <param name="pipelineServiceProvider">Service provider for the pipeline services</param>
    public DataContext(IServiceProvider globalServiceProvider, IServiceProvider pipelineServiceProvider)
    {
        GlobalServiceProvider = globalServiceProvider;
        PipelineServiceProvider = pipelineServiceProvider;
        var loggerFactory = globalServiceProvider.GetRequiredService<ILoggerFactory>();
        Logger = loggerFactory.CreateLogger("DataPipeline");
    }

    /// <inheritdoc />
    public IServiceProvider GlobalServiceProvider { get; }

    /// <inheritdoc />
    public IServiceProvider PipelineServiceProvider { get; }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public T GetNodeConfiguration<T>() where T : INodeConfiguration
    {
        if (_configurationNode == null)
        {
            throw DataPipelineException.NoConfigurationNodeSet();
        }

        return (T)_configurationNode;
    }

    /// <inheritdoc />
    public JToken? Current { get; set; }

    /// <summary>
    /// Sets the current configuration node.
    /// </summary>
    /// <param name="node"></param>
    public void SetConfigurationNode(INodeConfiguration node)
    {
        _configurationNode = node;
    }

    /// <inheritdoc />
    public T? GetCurrentValueByPath<T>(string path)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var v = Current.SelectToken(path);
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
    public T? GetCurrentValueByName<T>(string? propertyName)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var v = propertyName == null ? Current : Current[propertyName];
        if (v == null)
        {
            return default;
        }

        if (typeof(T).IsPrimitive && v is JObject)
        {
            throw DataPipelineException.ValueIsObjectButMustBePrimitive(propertyName);
        }

        if (v is JArray)
        {
            throw DataPipelineException.ValueIsArrayMustBeScalar(propertyName);
        }

        return v.Value<T?>();
    }

    /// <inheritdoc />
    public IEnumerable<T?>? GetCurrentValuesByName<T>(string? propertyName)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var v = propertyName == null ? Current : Current[propertyName];
        if (v == null)
        {
            return default;
        }

        if (v is JObject)
        {
            throw DataPipelineException.ValueIsObjectButMustBeArray(propertyName);
        }

        return v.Values<T>();
    }

    /// <inheritdoc />
    public void SetCurrentValueByName<T>(string? propertyName, T? value)
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
            Current ??= new JObject();

            var token = Current.SelectToken(propertyName);
            if (token == null)
            {
                Current[propertyName] = targetValue;
            }
            else
            {
                token.Replace(targetValue);
            }
        }
        else
        {
            Current = targetValue;
        }
    }

    /// <inheritdoc />
    public void SetCurrentValue<T>(T value)
    {
        SetCurrentValueByName(null, value);
    }

    /// <inheritdoc />
    public void AppendToCurrentValue<T>(string path, T value)
    {
        CreateCurrentIfNull();


        var jToken = JToken.FromObject(value!);

        var token = Current.SelectToken(path);
        if (token == null)
        {
            var newArray = new JArray { jToken };
            Current.ReplaceNested(path, newArray);
        }
        else if (token is JArray jArray)
        {
            jArray.Add(jToken);
        }
        else
        {
            throw DataPipelineException.ValueIsNotArray(path);
        }
    }

    /// <inheritdoc />
    [MemberNotNull(nameof(Current))]
    public void CreateCurrentIfNull()
    {
        Current ??= new JObject();
    }

    /// <inheritdoc />
    public IDataContext Clone()
    {
        return new DataContext(GlobalServiceProvider, PipelineServiceProvider)
        {
            Current = Current
        };
    }
}