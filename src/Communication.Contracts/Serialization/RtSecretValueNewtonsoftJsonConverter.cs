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
///     Thin public wrapper: reading and writing delegate to the engine's strict wire contract
///     (<see cref="RtSecretValueWireFormat" />).
///     Note that Newtonsoft prefers the type-level converter of <see cref="RtSecretValue" /> over the
///     converters of the serializer settings, so in practice the engine converter reads and writes; this
///     converter only takes effect where it is applied to a member.
/// </remarks>
public sealed class RtSecretValueNewtonsoftJsonConverter : JsonConverter<RtSecretValue>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, RtSecretValue? value, JsonSerializer serializer)
    {
        RtSecretValueWireFormat.Write(writer, value);
    }

    /// <inheritdoc />
    public override RtSecretValue? ReadJson(JsonReader reader, Type objectType, RtSecretValue? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        return RtSecretValueWireFormat.Read(reader);
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
        RtSecretValueWireFormat.Write(writer, value);
    }

    private static void WriteMarker(JsonWriter writer, bool isSet)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WritePropertyName(RtSecretValueWireFormat.IsSetPropertyName);
        writer.WriteValue(isSet);
        writer.WriteEndObject();
    }
}
