using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Meshmakers.Octo.Runtime.Contracts.Serialization;
using Newtonsoft.Json;

namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     Newtonsoft.Json converter for <see cref="RtSecretValue" /> with exactly the behaviour of
///     <see cref="RtSecretValueJsonConverter" />: writes the marker <c>{"isSet":true|false}</c>, never an
///     envelope or plaintext; reads a string as <see cref="RtSecretValue.Pending" /> and the marker
///     (<c>isSet</c> plus the optional read-state fields <c>keyMissing</c> / <c>setAt</c>) as
///     <c>Pending("")</c> ("unchanged"); <c>null</c> stays <c>null</c>; anything else throws a
///     <see cref="JsonSerializationException" /> without the value.
/// </summary>
/// <remarks>
///     Thin public wrapper over the engine's strict wire contract (<see cref="RtSecretValueWireFormat" />).
///     Note that Newtonsoft prefers the type-level converter of <see cref="RtSecretValue" /> over the
///     converters of the serializer settings, so in practice the engine converter reads and writes; this
///     converter only takes effect where it is applied to a member.
/// </remarks>
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

        WriteMarker(writer, value);
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
                throw new JsonSerializationException($"{SecretMarkerRules.ExpectedShape}; got a token of type {reader.TokenType}.");
        }
    }

    /// <summary>
    ///     Writes the marker <c>{"isSet":true}</c>.
    /// </summary>
    /// <param name="writer">The writer</param>
    public static void WriteMarker(JsonWriter writer)
    {
        WriteMarker(writer, true);
    }

    /// <summary>
    ///     Writes the marker <c>{"isSet":true|false}</c> of <paramref name="value" />
    ///     (<see cref="RtSecretValueWireFormat.IsSet" />).
    /// </summary>
    /// <param name="writer">The writer</param>
    /// <param name="value">The secret value</param>
    public static void WriteMarker(JsonWriter writer, RtSecretValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        WriteMarker(writer, RtSecretValueWireFormat.IsSet(value));
    }

    private static void WriteMarker(JsonWriter writer, bool isSet)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WritePropertyName(RtSecretValueWireFormat.IsSetPropertyName);
        writer.WriteValue(isSet);
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
                case JsonToken.PropertyName when SecretMarkerRules.Classify((string?)reader.Value) is var kind &&
                                                 kind != SecretMarkerRules.Kind.None:
                    if (!reader.Read())
                    {
                        break;
                    }

                    if (!IsValidMarkerValue(kind, reader))
                    {
                        throw new JsonSerializationException(
                            SecretMarkerRules.InvalidPropertyMessage(kind, reader.TokenType.ToString()));
                    }

                    continue;
                default:
                    throw new JsonSerializationException(SecretMarkerRules.OtherPropertiesMessage);
            }
        }

        throw new JsonSerializationException("Unexpected end of JSON while reading a secret attribute marker.");
    }

    private static bool IsValidMarkerValue(SecretMarkerRules.Kind kind, JsonReader reader)
    {
        if (kind == SecretMarkerRules.Kind.Boolean)
        {
            return reader.TokenType == JsonToken.Boolean;
        }

        // Depending on DateParseHandling a date string arrives as a Date token.
        return reader.TokenType is JsonToken.Null or JsonToken.Undefined or JsonToken.Date ||
               (reader.TokenType == JsonToken.String && SecretMarkerRules.IsDateText((string?)reader.Value));
    }
}
