using System.Collections;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Newtonsoft.Json;

namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     Newtonsoft.Json property converter for untyped attribute values (<c>object?</c>), the
///     counterpart of <see cref="SecretSafeAttributeValueJsonConverter" />: an
///     <see cref="RtSecretValue" /> - directly, or as an element of a list or dictionary - is
///     written as the marker <c>{"isSet":true}</c>; everything else is serialized by the given
///     serializer. Reading uses the default handling.
/// </summary>
public sealed class SecretSafeAttributeValueNewtonsoftJsonConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        throw new NotSupportedException("Reading is done by the default handling (CanRead is false).");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        WriteValue(writer, value, serializer);
    }

    private static void WriteValue(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        switch (value)
        {
            case null:
                writer.WriteNull();
                return;
            case RtSecretValue:
                RtSecretValueNewtonsoftJsonConverter.WriteMarker(writer);
                return;
            case string:
                serializer.Serialize(writer, value);
                return;
            case IDictionary dictionary when ContainsSecret(dictionary.Values):
                writer.WriteStartObject();
                foreach (DictionaryEntry entry in dictionary)
                {
                    writer.WritePropertyName(Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
                    WriteValue(writer, entry.Value, serializer);
                }

                writer.WriteEndObject();
                return;
            case IEnumerable enumerable and not IDictionary when ContainsSecret(enumerable):
                writer.WriteStartArray();
                foreach (var item in enumerable)
                {
                    WriteValue(writer, item, serializer);
                }

                writer.WriteEndArray();
                return;
            default:
                serializer.Serialize(writer, value);
                return;
        }
    }

    private static bool ContainsSecret(IEnumerable values)
    {
        foreach (var item in values)
        {
            switch (item)
            {
                case RtSecretValue:
                    return true;
                case string:
                    continue;
                case IDictionary nested when ContainsSecret(nested.Values):
                    return true;
                case IEnumerable nested and not IDictionary when ContainsSecret(nested):
                    return true;
            }
        }

        return false;
    }
}
