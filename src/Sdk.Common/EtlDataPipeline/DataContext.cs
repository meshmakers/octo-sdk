using System.Diagnostics.CodeAnalysis;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    public DataContext(IServiceProvider globalServiceProvider)
    {
        GlobalServiceProvider = globalServiceProvider;
        var loggerFactory = globalServiceProvider.GetRequiredService<ILoggerFactory>();
        Logger = loggerFactory.CreateLogger("DataPipeline");
    }

    /// <inheritdoc />
    public IServiceProvider GlobalServiceProvider { get; }

    /// <inheritdoc />

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
    /// Sets the current node configuration.
    /// </summary>
    /// <param name="node"></param>
    public void SetNodeConfiguration(INodeConfiguration node)
    {
        _configurationNode = node;
    }

    /// <inheritdoc />
    public T? GetCurrentValueByPath<T>(string? path)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var jToken = Current.SelectToken(path ?? "$");
        if (jToken == null)
        {
            return default;
        }

        if (typeof(T).IsPrimitive && jToken is JObject)
        {
            throw DataPipelineException.ValueIsObjectButMustBePrimitive(path);
        }
        
        if (jToken is JArray)
        {
            throw DataPipelineException.ValueIsArrayMustBeScalar(path);
        }

        return jToken.Value<T?>();
    }

    /// <inheritdoc />
    public IEnumerable<T?>? GetCurrentValuesByPath<T>(string? path)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var jToken = Current.SelectToken(path ?? "$");
        if (jToken == null)
        {
            return default;
        }

        if (jToken is JObject)
        {
            throw DataPipelineException.ValueIsObjectButMustBeArray(path);
        }

        return jToken.Values<T>();
    }

    /// <inheritdoc />
    public void SetCurrentValueByPath<T>(string? path, T? value)
    {
        SetCurrentValueByPath(path, value, JsonSerializer.CreateDefault());
    }

    /// <inheritdoc />
    public void SetCurrentValueByPath<T>(string? path, T? value, JsonSerializer jsonSerializer)
    {
        JToken targetValue;
        if (value is JToken jToken)
        {
            targetValue = jToken;
        }
        else
        {
            targetValue = JToken.FromObject(value!, jsonSerializer);
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            CreateCurrentIfNull();

            var token = Current.SelectToken(path);
            if (token == null)
            {
                Current.ReplaceNested(path, targetValue);
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
    public void SetCurrentValue<T>(T value, JsonSerializer jsonSerializer)
    {
        SetCurrentValueByPath(null, value, jsonSerializer);
    }
    
    /// <inheritdoc />
    public void SetCurrentValue<T>(T value)
    {
        SetCurrentValueByPath(null, value);
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
    public T? DeserializeCurrentValue<T>(string? path, JsonSerializer jsonSerializer)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var token = Current.SelectToken(path ?? "$");
        if (token == null)
        {
            return default;
        }

        return token.ToObject<T>(jsonSerializer);
    }

    /// <inheritdoc />
    public T? DeserializeCurrentValue<T>(string? path)
    {
        return DeserializeCurrentValue<T>(path, JsonSerializer.CreateDefault());
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
        return new DataContext(GlobalServiceProvider)
        {
            Current = Current
        };
    }
}