using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// The data context is used to pass data between nodes in the data pipeline.
/// </summary>
internal class DataContext : IDataContext
{
    /// <summary>
    /// Creates a new instance of <see cref="DataContext"/>
    /// </summary>
    /// <param name="parent">Parent data context</param>
    /// <param name="value">Optional value to pass to the pipeline</param>
    internal DataContext(IDataContext parent, object? value = null)
        : this(value)
    {
        Parent = parent;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DataContext"/>
    /// </summary>
    /// <param name="value">Optional value to pass to the pipeline</param>
    public DataContext(object? value = null)
    {
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
        else
        {
            Current = new JObject();
        }
    }

    /// <inheritdoc />
    public IDataContext? Parent { get; }

    /// <inheritdoc />
    public JToken? Current { get; set; }

    /// <inheritdoc />
    public T? GetSimpleValueByPath<T>(string? path)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        return Current.GetSimpleValueByPath<T>(path);
    }

    /// <inheritdoc />
    public bool IsPathSimpleArrayValue(string? path)
    {
        if (Current == null)
        {
            return false;
        }

        var jToken = Current.SelectToken(path ?? "$");
        return jToken switch
        {
            null => false,
            JObject => false,
            JValue => true,
            _ => true
        };
    }

    /// <inheritdoc />
    public IEnumerable<T?>? GetSimpleArrayValueByPath<T>(string? path)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var jToken = Current.SelectToken(path ?? "$");
        return jToken switch
        {
            null => null,
            JObject => throw DataPipelineException.ValueIsObjectButMustBeArray(path),
            JValue jValue => new List<T?> { jValue.Value<T>() },
            _ => jToken.Values<T>()
        };
    }

    /// <inheritdoc />
    public IEnumerable<JToken> SelectByPath(string path)
    {
        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var jTokens = Current.SelectTokens(path);
        return jTokens;
    }

    /// <inheritdoc />
    public void SetValueByPath<T>(string? path, DocumentModes documentModes, ValueKinds valueKinds, TargetValueWriteModes targetValueWriteModes, T? value)
    {
        SetValueByPath(path, value, documentModes, valueKinds, targetValueWriteModes, JsonSerializer.CreateDefault());
    }

    /// <inheritdoc />
    public void SetValueByPath<T>(string? path, T? value, DocumentModes documentModes, ValueKinds valueKinds, TargetValueWriteModes targetValueWriteModes,
        JsonSerializer jsonSerializer)
    {
        JToken targetValue = JValue.CreateNull();
        if (value is JToken jToken)
        {
            targetValue = jToken;
        }
        else if (value != null)
        {
            targetValue = JToken.FromObject(value, jsonSerializer);
        }

        if (documentModes == DocumentModes.Replace)
        {
            Current = new JObject();
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (!string.IsNullOrWhiteSpace(path) && path != null && path != "$")
        {
            CreateCurrentIfNull();

            switch (valueKinds)
            {
                case ValueKinds.Simple:
                    SetSimpleValue(path, Current!, targetValue, targetValueWriteModes);
                    break;
                case ValueKinds.Array:
                    SetArrayValue(path, Current!, targetValue, targetValueWriteModes);
                    break;
            }
        }
        else
        {
            Current = targetValue;
        }
    }

    private static void SetSimpleValue(string path, JToken current, JToken targetValue, TargetValueWriteModes targetValueWriteModes)
    {
        var token = current.SelectToken(path);

        switch (targetValueWriteModes)
        {
            case TargetValueWriteModes.Overwrite:
                if (token == null)
                {
                    current.ReplaceNested(path, targetValue);
                }
                else
                {
                    token.Replace(targetValue);
                }

                break;
            case TargetValueWriteModes.Append:
            {
                var items = current.SelectToken(path);
                if (items is JArray jArray)
                {
                    if (targetValue is JArray targetArray)
                    {
                        foreach (var item in targetArray)
                        {
                            jArray.Add(item);
                        }
                    }
                    else
                    {
                        jArray.Add(targetValue);
                    }
                }
                else
                {
                    throw DataPipelineException.ValueIsArrayMustBeScalarForWriteMode(path, targetValueWriteModes);
                }

                break;
            }
            case TargetValueWriteModes.Merge:
            {
                var items = current.SelectToken(path);
                if (items is JObject jObject)
                {
                    if (targetValue is JObject targetObject)
                    {
                        foreach (var item in targetObject)
                        {
                            jObject[item.Key] = item.Value;
                        }
                    }
                    else
                    {
                        throw DataPipelineException.TargetValueIsObjectMustBeObjectForWriteMode(path, targetValueWriteModes);
                    }
                }
                else
                {
                    throw DataPipelineException.SourceValueIsObjectMustBeObjectForWriteMode(path, targetValueWriteModes);
                }

                break;
            }
            case TargetValueWriteModes.Prepend:
            {
                var items = current.SelectToken(path);
                if (items is JArray jArray)
                {
                    if (targetValue is JArray targetArray)
                    {
                        foreach (var item in targetArray)
                        {
                            jArray.Insert(0, item);
                        }
                    }
                    else
                    {
                        jArray.Insert(0, targetValue);
                    }
                }
                else
                {
                    throw DataPipelineException.ValueIsArrayMustBeScalarForWriteMode(path, targetValueWriteModes);
                }

                break;
            }
            default:
                throw DataPipelineException.UnknownWriteMode(targetValueWriteModes);
        }
    }

    private static void SetArrayValue(string path, JToken current, JToken targetValue, TargetValueWriteModes targetValueWriteModes)
    {
        var token = current.SelectToken(path);

        switch (targetValueWriteModes)
        {
            case TargetValueWriteModes.Overwrite:

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
            case TargetValueWriteModes.Append:
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
            case TargetValueWriteModes.Prepend:
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
                throw DataPipelineException.UnknownWriteMode(targetValueWriteModes);
        }
    }

    /// <inheritdoc />
    public T? GetComplexObjectByPath<T>(string? path, JsonSerializer jsonSerializer)
    {
        if (Current == null)
        {
            return default;
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

    /// <inheritdoc />
    public IDataContext CreateChildDataContext(JToken input)
    {
        return new DataContext(this, input.DeepClone());
    }
}