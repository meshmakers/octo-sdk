using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common;

/// <summary>
/// Extension methods for <see cref="JObject"/>
/// </summary>
public static class JTokenExtensions
{
    /// <summary>
    /// Get the value as a specific type. The value is expected to be a simple value.
    /// </summary>
    /// <param name="self">The instance to get the value from</param>
    /// <param name="path">Path to the value</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetSimpleValueByPath<T>(this JToken self, string? path)
    {
        var jToken = self.SelectToken(path ?? "$");
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

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="self">The instance to get the value from</param>
    /// <param name="path">Property name</param>
    /// <param name="valueKinds">Defines if a value should be a simple value or array</param>
    /// <param name="targetValueWriteModes">Defines if a value should be replaced or appended</param>
    /// <param name="value">Value to set</param>
    /// <typeparam name="T">Type of the value</typeparam>
    public static void SetValueByPath<T>(this JToken self, string? path, ValueKinds valueKinds, TargetValueWriteModes targetValueWriteModes,
        T? value)
    {
        SetValueByPath(self, path, value, valueKinds, targetValueWriteModes, JsonSerializer.CreateDefault());
    }

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="self">The instance to get the value from</param>
    /// <param name="path">Property name</param>
    /// <param name="value">Value to set</param>
    /// <param name="valueKinds">Defines if a value should be a simple value or array</param>
    /// <param name="targetValueWriteModes">Defines if a value should be replaced or appended</param>
    /// <param name="jsonSerializer">JSON serializer to use</param>
    /// <typeparam name="T">Type of the value</typeparam>
    public static void SetValueByPath<T>(this JToken self, string? path, T? value, ValueKinds valueKinds,
        TargetValueWriteModes targetValueWriteModes,
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

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (!string.IsNullOrWhiteSpace(path) && path != null && path != "$")
        {
            switch (valueKinds)
            {
                case ValueKinds.Simple:
                    SetSimpleValue(path, self, targetValue, targetValueWriteModes);
                    break;
                case ValueKinds.Array:
                    SetArrayValue(path, self, targetValue, targetValueWriteModes);
                    break;
            }
        }
        else
        {
            self.Replace(targetValue);
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
            case TargetValueWriteModes.Prepend:
                throw DataPipelineException.ValueIsArrayMustBeScalarForWriteMode(path, targetValueWriteModes);
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


    /// <summary>
    /// Finds the parent <see cref="JProperty"/> of the given <see cref="JToken"/>
    /// </summary>
    /// <param name="self">Instance to find the parent property for</param>
    /// <returns>The parent <see cref="JProperty"/> or null if not found</returns>
    public static JProperty? FindParentProperty(this JToken self)
    {
        JToken? temp = self;
        while (temp != null)
        {
            if (temp is JProperty property)
            {
                return property;
            }

            temp = temp.Parent;
        }

        return null;
    }

    /// <summary>
    /// Replaces value based on path. New object tokens are created for missing parts of the given path.
    /// </summary>
    /// <param name="self">Instance to update</param>
    /// <param name="path">Dot delimited path of the new value. E.g. 'foo.bar'</param>
    /// <param name="value">Value to set.</param>
    public static void ReplaceNested(this JToken self, string path, JToken value)
    {
        if (self is null)
        {
            throw new ArgumentNullException(nameof(self));
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        lock (self)
        {
            var pathParts = path.Split('.');
            JToken currentNode = self;

            for (int i = 0; i < pathParts.Length; i++)
            {
                var pathPart = pathParts[i];
                var isLast = i == pathParts.Length - 1;
                var partNode = currentNode!.SelectToken(pathPart);

                if (partNode is null)
                {
                    var nodeToAdd = isLast ? value : new JObject();
                    if (!(currentNode is JObject jObject))
                    {
                        throw DataPipelineException.SourceMustBeAnObject(currentNode);
                    }

                    jObject.Add(pathPart, nodeToAdd);
                    currentNode = jObject.SelectToken(pathPart)!;
                }
                else
                {
                    currentNode = partNode;

                    if (isLast)
                    {
                        if (currentNode.Parent != null)
                        {
                            currentNode.Replace(value);
                        }
                        else
                        {
                            foreach (var jToken in value.Children())
                            {
                                if (jToken is JProperty jProperty)
                                {
                                    currentNode[jProperty.Name] = jProperty.Value.DeepClone();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}