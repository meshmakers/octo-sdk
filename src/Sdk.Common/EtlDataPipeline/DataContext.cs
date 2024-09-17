using System.Diagnostics.CodeAnalysis;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// The data context is used to pass data between nodes in the data pipeline.
/// </summary>
public class DataContext : IDataContext
{
    private readonly IDataContext? _parent;
    private readonly IPipelineLogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="DataContext"/>
    /// </summary>
    /// <param name="parent">Parent data context</param>
    /// <param name="globalServiceProvider">Service provider for the global services</param>
    /// <param name="pipelineLogger">The logger for the pipeline</param>
    /// <param name="value">Optional value to pass to the pipeline</param>
    /// <param name="pipelineDebugger">Optional debugger for the pipeline</param>
    private DataContext(IDataContext parent, IServiceProvider globalServiceProvider, IPipelineLogger pipelineLogger,
        object? value = null,
        IPipelineDebugger? pipelineDebugger = null)
        : this(globalServiceProvider, pipelineLogger, value, pipelineDebugger)
    {
        _parent = parent;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DataContext"/>
    /// </summary>
    /// <param name="globalServiceProvider">Service provider for the global services</param>
    /// <param name="pipelineLogger">The logger for the pipeline</param>
    /// <param name="value">Optional value to pass to the pipeline</param>
    /// <param name="pipelineDebugger">Optional debugger for the pipeline</param>
    public DataContext(IServiceProvider globalServiceProvider, IPipelineLogger pipelineLogger, object? value = null,
        IPipelineDebugger? pipelineDebugger = null)
    {
        GlobalServiceProvider = globalServiceProvider;
        _logger = pipelineLogger;
        Debugger = pipelineDebugger;
        if (value != null)
        {
            if (value is JToken jToken)
            {
                Current = jToken;
            }
            else
            {
                Current = JObject.FromObject(value);
            }
        }

        NodeContext = new NodeContext(null, "PipelineExecution", 0, _logger, null);
        Debugger?.LogInput(NodeContext.NodePath, 0, Current);
    }

    /// <inheritdoc />
    public IDataContext CreateChildContext(JToken? input)
    {
        var dataContext = new DataContext(this, GlobalServiceProvider, _logger, input, Debugger)
        {
            NodeContext = NodeContext
        };
        return dataContext;
    }

    /// <summary>
    /// Register a node as a child of the current node
    /// </summary>
    /// <param name="nodeQualifiedName"></param>
    /// <param name="sequenceNumber"></param>
    /// <param name="nodeConfiguration"></param>
    /// <returns></returns>
    public INodeContext RegisterNode(string nodeQualifiedName, uint sequenceNumber,
        INodeConfiguration nodeConfiguration)
    {
        NodeContext = new NodeContext(null, nodeQualifiedName, sequenceNumber, _logger, nodeConfiguration);
        Debugger?.LogInput(NodeContext.NodePath, sequenceNumber, Current);
        return NodeContext;
    }

    /// <inheritdoc />
    public INodeContext RegisterChildNode(INodeContext parent, string nodeQualifiedName, uint sequenceNumber,
        INodeConfiguration nodeConfiguration)
    {
        NodeContext = new NodeContext(parent, nodeQualifiedName, sequenceNumber, _logger, nodeConfiguration);
        Debugger?.LogInput(NodeContext.NodePath, sequenceNumber, Current);
        return NodeContext;
    }


    /// <inheritdoc />
    public INodeContext NodeContext { get; private set; }

    /// <inheritdoc />
    public IServiceProvider GlobalServiceProvider { get; }


    /// <inheritdoc />
    public IPipelineDebugger? Debugger { get; }

    /// <inheritdoc />
    public JToken? Current { get; set; }

    /// <inheritdoc />
    public T? GetSimpleValueByPath<T>(string? path)
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
    public IEnumerable<T?>? GetSimpleArrayValueByPath<T>(string? path)
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
    public void SetValueByPath<T>(string? path, ValueKind valueKind, WriteMode writeMode, T? value)
    {
        SetValueByPath(path, value, valueKind, writeMode, JsonSerializer.CreateDefault());
    }

    /// <inheritdoc />
    public void SetValueByPath<T>(string? path, T? value, ValueKind valueKind, WriteMode writeMode,
        JsonSerializer jsonSerializer)
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

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (!string.IsNullOrWhiteSpace(path) && path != null && path != "$")
        {
            CreateCurrentIfNull();

            switch (valueKind)
            {
                case ValueKind.Simple:
                    SetSimpleValue(path, Current!, targetValue, writeMode);
                    break;
                case ValueKind.Array:
                    SetArrayValue(path, Current!, targetValue, writeMode);
                    break;
            }
        }
        else
        {
            Current = targetValue;
        }
    }

    private static void SetSimpleValue(string path, JToken current, JToken targetValue, WriteMode writeMode)
    {
        var token = current.SelectToken(path);

        switch (writeMode)
        {
            case WriteMode.Overwrite:
                if (token == null)
                {
                    current.ReplaceNested(path, targetValue);
                }
                else
                {
                    token.Replace(targetValue);
                }

                break;
            case WriteMode.Append:
            case WriteMode.Prepend:
                throw DataPipelineException.ValueIsArrayMustBeScalarForWriteMode(path, writeMode);
            default:
                throw DataPipelineException.UnknownWriteMode(writeMode);
        }
    }

    private static void SetArrayValue(string path, JToken current, JToken targetValue, WriteMode writeMode)
    {
        var token = current.SelectToken(path);

        switch (writeMode)
        {
            case WriteMode.Overwrite:

                if (token == null)
                {
                    var newArray = new JArray { targetValue };
                    current.ReplaceNested(path, newArray);
                }
                else
                {
                    var newArray = new JArray { targetValue };
                    token.Replace(newArray);
                }

                break;
            case WriteMode.Append:
                if (token == null)
                {
                    var newArray = new JArray { targetValue };
                    current.ReplaceNested(path, newArray);
                }
                else if (token is JArray jArray)
                {
                    jArray.Add(targetValue);
                }
                else
                {
                    throw DataPipelineException.ValueIsNotArray(path);
                }

                break;
            case WriteMode.Prepend:
                if (token == null)
                {
                    var newArray = new JArray { targetValue };
                    current.ReplaceNested(path, newArray);
                }
                else if (token is JArray jArray)
                {
                    jArray.Insert(0, targetValue);
                }
                else
                {
                    throw DataPipelineException.ValueIsNotArray(path);
                }

                break;
            default:
                throw DataPipelineException.UnknownWriteMode(writeMode);
        }
    }

    /// <inheritdoc />
    public T? GetComplexObjectByPath<T>(string? path, JsonSerializer jsonSerializer)
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
    public T? GetComplexObjectByPath<T>(string? path)
    {
        return GetComplexObjectByPath<T>(path, JsonSerializer.CreateDefault());
    }

    /// <inheritdoc />
#if !NETSTANDARD2_0
    [MemberNotNull(nameof(Current))]
#endif
    public void CreateCurrentIfNull()
    {
        Current ??= new JObject();
    }
}