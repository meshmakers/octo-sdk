using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     Writes and reads a <see cref="TimeSpan" /> CK method parameter or result as whole seconds, the wire form of the
///     asset repository's <c>TimeSpan</c> GraphQL scalar (AB#8423). Used by the generated method clients.
/// </summary>
public sealed class CkMethodTimeSpanSecondsConverter : JsonConverter<TimeSpan>
{
    /// <inheritdoc />
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => TimeSpan.FromSeconds(reader.GetDouble()),
            JsonTokenType.String when double.TryParse(reader.GetString(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var seconds) => TimeSpan.FromSeconds(seconds),
            _ => throw new JsonException("A TimeSpan in seconds was expected.")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((long)value.TotalSeconds);
    }
}
