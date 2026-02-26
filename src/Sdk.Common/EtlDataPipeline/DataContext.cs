using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// The data context is used to pass data between nodes in the data pipeline.
/// </summary>
internal class DataContext : IDataContext
{
    private Dictionary<string, JToken>? _sharedData;
    private HashSet<string>? _materializedKeys;

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
    /// Creates a new instance of <see cref="DataContext"/> with shared read-only data.
    /// Shared data is accessible via path lookups but not cloned per instance.
    /// </summary>
    /// <param name="parent">Parent data context</param>
    /// <param name="mutableInput">Mutable input token for this context</param>
    /// <param name="sharedData">Read-only shared data keyed by property name</param>
    internal DataContext(IDataContext parent, JToken mutableInput, Dictionary<string, JToken> sharedData)
        : this(parent, mutableInput)
    {
        _sharedData = sharedData;
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
        if (TryResolveShared(path, out var sharedToken, out var subPath))
        {
            return sharedToken!.GetSimpleValueByPath<T>(subPath);
        }

        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        return Current.GetSimpleValueByPath<T>(path);
    }

    /// <inheritdoc />
    public bool IsPathSimpleArrayValue(string? path)
    {
        if (TryResolveShared(path, out var sharedToken, out var subPath))
        {
            var jToken = sharedToken!.SelectToken(subPath ?? "$");
            return jToken switch
            {
                null => false,
                JObject => false,
                JValue => true,
                _ => true
            };
        }

        if (Current == null)
        {
            return false;
        }

        var jToken2 = Current.SelectToken(path ?? "$");
        return jToken2 switch
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
        if (TryResolveShared(path, out var sharedToken, out var subPath))
        {
            var jToken = sharedToken!.SelectToken(subPath ?? "$");
            return jToken switch
            {
                null => null,
                JObject => throw DataPipelineException.ValueIsObjectButMustBeArray(path),
                JValue jValue => new List<T?> { jValue.Value<T>() },
                _ => jToken.Values<T>()
            };
        }

        if (Current == null)
        {
            throw DataPipelineException.CurrentIsNull();
        }

        var jToken2 = Current.SelectToken(path ?? "$");
        return jToken2 switch
        {
            null => null,
            JObject => throw DataPipelineException.ValueIsObjectButMustBeArray(path),
            JValue jValue => new List<T?> { jValue.Value<T>() },
            _ => jToken2.Values<T>()
        };
    }

    /// <inheritdoc />
    public IEnumerable<JToken> SelectByPath(string path)
    {
        if (TryResolveShared(path, out var sharedToken, out var subPath))
        {
            return sharedToken!.SelectTokens(subPath ?? "$");
        }

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

        MaterializeSharedKeyIfNeeded(path);

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
        // Shared path resolution (e.g. $.full.body.name)
        if (TryResolveShared(path, out var sharedToken, out var subPath))
        {
            var resolved = subPath != null ? sharedToken!.SelectToken(subPath) : sharedToken;
            if (resolved == null)
            {
                return default;
            }

            return resolved.ToObject<T>(jsonSerializer);
        }

        if (Current == null)
        {
            return default;
        }

        // Root-path merge: include shared data in the result
        if (_sharedData != null && (string.IsNullOrWhiteSpace(path) || path == "$"))
        {
            var merged = Current.DeepClone();
            if (merged is JObject mergedObj)
            {
                foreach (var kvp in _sharedData)
                {
                    if (_materializedKeys == null || !_materializedKeys.Contains(kvp.Key))
                    {
                        mergedObj[kvp.Key] = kvp.Value;
                    }
                }
            }

            return merged.ToObject<T>(jsonSerializer);
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

    private bool TryResolveShared(string? path, out JToken? sharedToken, out string? subPath)
    {
        sharedToken = null;
        subPath = null;

        if (_sharedData == null || string.IsNullOrWhiteSpace(path) || path == "$")
            return false;

        var normalizedPath = path!.StartsWith("$.") ? path.Substring(2) : path;

        if (string.IsNullOrEmpty(normalizedPath))
            return false;

        var dotIndex = normalizedPath.IndexOf('.');
        var firstSegment = dotIndex >= 0 ? normalizedPath.Substring(0, dotIndex) : normalizedPath;

        if (_materializedKeys != null && _materializedKeys.Contains(firstSegment))
            return false;

        if (!_sharedData.TryGetValue(firstSegment, out var token))
            return false;

        sharedToken = token;
        subPath = dotIndex >= 0 ? normalizedPath.Substring(dotIndex + 1) : null;
        return true;
    }

    private void MaterializeSharedKeyIfNeeded(string? path)
    {
        if (_sharedData == null || string.IsNullOrWhiteSpace(path) || path == "$")
            return;

        var normalizedPath = path!.StartsWith("$.") ? path.Substring(2) : path;

        if (string.IsNullOrEmpty(normalizedPath))
            return;

        var dotIndex = normalizedPath.IndexOf('.');
        var firstSegment = dotIndex >= 0 ? normalizedPath.Substring(0, dotIndex) : normalizedPath;

        if (_materializedKeys != null && _materializedKeys.Contains(firstSegment))
            return;

        if (!_sharedData.TryGetValue(firstSegment, out var sharedToken))
            return;

        _materializedKeys ??= new HashSet<string>();
        _materializedKeys.Add(firstSegment);

        CreateCurrentIfNull();
        if (Current is JObject jObj)
        {
            jObj[firstSegment] = sharedToken.DeepClone();
        }
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
        if (_sharedData != null)
        {
            return new DataContext(this, input.DeepClone(), _sharedData);
        }

        return new DataContext(this, input.DeepClone());
    }
}