using System.Text.Json;
using System.Text.Json.Serialization;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Meshmakers.Octo.Runtime.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     System.Text.Json converter for <see cref="RtSecretValue" /> (AB#5528, concept §3.3). It never
///     writes an envelope or a plaintext.
/// </summary>
/// <remarks>
///     <para>
///         <b>Write:</b> every <see cref="RtSecretValue" /> (protected, legacy or pending) is written as
///         the marker <c>{"isSet":true|false}</c> (<see cref="RtSecretValueWireFormat.IsSet" />: false only
///         for an empty value or a legacy placeholder / corrupt string found in storage) - the same shape as <c>OctoSecretStateDto</c> and the
///         GraphQL type <c>OctoSecretState</c>. A <c>null</c> value is written as <c>null</c> by the
///         serializer.
///     </para>
///     <para>
///         <b>Read:</b> a JSON string is input and becomes <see cref="RtSecretValue.Pending" /> (not
///         trimmed; <c>""</c> becomes <c>Pending("")</c>, which the engine write step treats as
///         "unchanged"). The marker object (<c>{"isSet":true|false}</c>, also <c>{}</c>) means
///         "unchanged" and becomes <c>Pending("")</c> as well; the marker may also carry the other fields of
///         the read state, <c>keyMissing</c> (boolean) and <c>setAt</c> (ISO-8601 date string or null), so a
///         client echoing what it read is "unchanged" (AB#5534 round 2) - deliberately not <c>null</c>, because
///         <c>null</c> clears a secret in the engine write rules (concept §3.6), so a document that
///         was read and is written back keeps its secrets. JSON <c>null</c> stays <c>null</c>. Any
///         other token, or an object with other properties, throws a <see cref="JsonException" />
///         that never contains the value.
///     </para>
///     <para>
///         This is the strict wire contract of the engine (<see cref="RtSecretValueWireFormat" />, the
///         converters on <see cref="RtSecretValue" /> itself); this type stays as a thin public wrapper for
///         API compatibility. It enforces the contract explicitly so it does not depend on the engine
///         version it runs against (AB#5534).
///     </para>
/// </remarks>
public sealed class RtSecretValueJsonConverter : JsonConverter<RtSecretValue>
{
    /// <summary>
    ///     Name of the marker property written by the converters (reading also accepts <c>keyMissing</c> and
    ///     <c>setAt</c>).
    /// </summary>
    public const string IsSetPropertyName = SecretMarkerRules.IsSetPropertyName;

    /// <inheritdoc />
    public override RtSecretValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return RtSecretValue.Pending(reader.GetString() ?? string.Empty);
            case JsonTokenType.StartObject:
                ReadMarker(ref reader);
                return RtSecretValue.Pending(string.Empty);
            default:
                throw new JsonException($"{SecretMarkerRules.ExpectedShape}; got a token of type {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RtSecretValue value, JsonSerializerOptions options)
    {
        WriteMarker(writer, value);
    }

    /// <summary>
    ///     Writes the marker <c>{"isSet":true}</c>.
    /// </summary>
    /// <param name="writer">The writer</param>
    public static void WriteMarker(Utf8JsonWriter writer)
    {
        WriteMarker(writer, true);
    }

    /// <summary>
    ///     Writes the marker <c>{"isSet":true|false}</c> of <paramref name="value" />
    ///     (<see cref="RtSecretValueWireFormat.IsSet" />).
    /// </summary>
    /// <param name="writer">The writer</param>
    /// <param name="value">The secret value</param>
    public static void WriteMarker(Utf8JsonWriter writer, RtSecretValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        WriteMarker(writer, RtSecretValueWireFormat.IsSet(value));
    }

    private static void WriteMarker(Utf8JsonWriter writer, bool isSet)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteBoolean(IsSetPropertyName, isSet);
        writer.WriteEndObject();
    }

    private static void ReadMarker(ref Utf8JsonReader reader)
    {
        // Positioned on StartObject; only isSet / keyMissing (booleans) and setAt (date string or null).
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return;
            }

            if (reader.TokenType == JsonTokenType.Comment)
            {
                continue;
            }

            var kind = reader.TokenType == JsonTokenType.PropertyName
                ? SecretMarkerRules.Classify(reader.GetString())
                : SecretMarkerRules.Kind.None;
            if (kind == SecretMarkerRules.Kind.None)
            {
                throw new JsonException(SecretMarkerRules.OtherPropertiesMessage);
            }

            if (!reader.Read())
            {
                break;
            }

            var valid = kind == SecretMarkerRules.Kind.Boolean
                ? reader.TokenType is JsonTokenType.True or JsonTokenType.False
                : reader.TokenType == JsonTokenType.Null ||
                  (reader.TokenType == JsonTokenType.String && SecretMarkerRules.IsDateText(reader.GetString()));
            if (!valid)
            {
                throw new JsonException(SecretMarkerRules.InvalidPropertyMessage(kind, reader.TokenType.ToString()));
            }
        }

        throw new JsonException("Unexpected end of JSON while reading a secret attribute marker.");
    }
}
