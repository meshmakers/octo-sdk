using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Newtonsoft.Json;

namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     Newtonsoft.Json converter for <see cref="RtSecretValue" /> with exactly the behaviour of
///     <see cref="RtSecretValueJsonConverter" />: writes the marker <c>{"isSet":true}</c>, never an
///     envelope or plaintext; reads a string as <see cref="RtSecretValue.Pending" /> and the marker
///     as <c>Pending("")</c> ("unchanged"); <c>null</c> stays <c>null</c>.
/// </summary>
public sealed class RtSecretValueNewtonsoftJsonConverter : JsonConverter<RtSecretValue>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, RtSecretValue? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        WriteMarker(writer);
    }

    /// <inheritdoc />
    public override RtSecretValue? ReadJson(JsonReader reader, Type objectType, RtSecretValue? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null:
            case JsonToken.Undefined:
                return null;
            case JsonToken.String:
                return RtSecretValue.Pending((string?)reader.Value ?? string.Empty);
            case JsonToken.StartObject:
                ReadMarker(reader);
                return RtSecretValue.Pending(string.Empty);
            default:
                throw new JsonSerializationException(
                    $"A secret attribute value must be a string or the marker {{\"{RtSecretValueJsonConverter.IsSetPropertyName}\":...}}; got a token of type {reader.TokenType}.");
        }
    }

    /// <summary>
    ///     Writes the marker <c>{"isSet":true}</c>.
    /// </summary>
    /// <param name="writer">The writer</param>
    public static void WriteMarker(JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WritePropertyName(RtSecretValueJsonConverter.IsSetPropertyName);
        writer.WriteValue(true);
        writer.WriteEndObject();
    }

    private static void ReadMarker(JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonToken.EndObject:
                    return;
                case JsonToken.Comment:
                    continue;
                case JsonToken.PropertyName when string.Equals((string?)reader.Value,
                    RtSecretValueJsonConverter.IsSetPropertyName, StringComparison.OrdinalIgnoreCase):
                    if (!reader.Read() || reader.TokenType != JsonToken.Boolean)
                    {
                        throw new JsonSerializationException(
                            $"The property '{RtSecretValueJsonConverter.IsSetPropertyName}' of a secret attribute must be a boolean.");
                    }

                    continue;
                default:
                    throw new JsonSerializationException(
                        $"A secret attribute object may only contain the property '{RtSecretValueJsonConverter.IsSetPropertyName}'.");
            }
        }

        throw new JsonSerializationException("Unexpected end of JSON while reading a secret attribute marker.");
    }
}
