using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;

namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     System.Text.Json property converter for untyped attribute values (<c>object?</c>, e.g.
///     <c>RtEntityAttributeDto.Value</c>) that guarantees an <see cref="RtSecretValue" /> is never
///     written with its content, also when the serializer options do not carry
///     <see cref="RtSecretValueJsonConverter" /> and also when the secret is nested in a list or
///     record (AB#5528). Everything else is written exactly like the default <c>object</c>
///     handling (by runtime type) and read like the default (a <c>JsonElement</c>, or a
///     <c>JsonNode</c> when the options say so).
/// </summary>
/// <remarks>
///     Apply it with <c>[JsonConverter(typeof(SecretSafeAttributeValueJsonConverter))]</c> on
///     <c>object</c> properties only; do not add it to <see cref="JsonSerializerOptions.Converters" />.
/// </remarks>
public sealed class SecretSafeAttributeValueJsonConverter : JsonConverter<object>
{
    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> SafeOptions = new();

    /// <inheritdoc />
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // The property converter is not part of `options`, so this is the default object handling.
        return JsonSerializer.Deserialize<object>(ref reader, options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is RtSecretValue)
        {
            RtSecretValueJsonConverter.WriteMarker(writer);
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), GetSafeOptions(options));
    }

    private static JsonSerializerOptions GetSafeOptions(JsonSerializerOptions options)
    {
        if (options.Converters.Any(c => c is RtSecretValueJsonConverter))
        {
            return options;
        }

        return SafeOptions.GetValue(options, static o =>
        {
            var copy = new JsonSerializerOptions(o);
            copy.Converters.Add(new RtSecretValueJsonConverter());
            return copy;
        });
    }
}
