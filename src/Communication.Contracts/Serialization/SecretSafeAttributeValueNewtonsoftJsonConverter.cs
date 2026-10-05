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
                // Objects (e.g. a raw RtRecord) can hold a secret below a member the checks above do
                // not see; serialize them with the secret converter so Newtonsoft never falls back
                // to RtSecretValue's public members (Envelope, KeyId).
                GetSafeSerializer(serializer).Serialize(writer, value);
                return;
        }
    }

    private static JsonSerializer GetSafeSerializer(JsonSerializer serializer)
    {
        if (serializer.Converters.Any(c => c is RtSecretValueNewtonsoftJsonConverter))
        {
            return serializer;
        }

        // A new instance per call: the given serializer may be shared between threads and must
        // not be mutated.
        var safe = new JsonSerializer
        {
            CheckAdditionalContent = serializer.CheckAdditionalContent,
            ConstructorHandling = serializer.ConstructorHandling,
            Context = serializer.Context,
            ContractResolver = serializer.ContractResolver,
            Culture = serializer.Culture,
            DateFormatHandling = serializer.DateFormatHandling,
            DateFormatString = serializer.DateFormatString,
            DateParseHandling = serializer.DateParseHandling,
            DateTimeZoneHandling = serializer.DateTimeZoneHandling,
            DefaultValueHandling = serializer.DefaultValueHandling,
            EqualityComparer = serializer.EqualityComparer,
            FloatFormatHandling = serializer.FloatFormatHandling,
            FloatParseHandling = serializer.FloatParseHandling,
            Formatting = serializer.Formatting,
            MaxDepth = serializer.MaxDepth,
            MetadataPropertyHandling = serializer.MetadataPropertyHandling,
            MissingMemberHandling = serializer.MissingMemberHandling,
            NullValueHandling = serializer.NullValueHandling,
            ObjectCreationHandling = serializer.ObjectCreationHandling,
            PreserveReferencesHandling = serializer.PreserveReferencesHandling,
            ReferenceLoopHandling = serializer.ReferenceLoopHandling,
            ReferenceResolver = serializer.ReferenceResolver,
            SerializationBinder = serializer.SerializationBinder,
            StringEscapeHandling = serializer.StringEscapeHandling,
            TraceWriter = serializer.TraceWriter,
            TypeNameAssemblyFormatHandling = serializer.TypeNameAssemblyFormatHandling,
            TypeNameHandling = serializer.TypeNameHandling
        };
        foreach (var converter in serializer.Converters)
        {
            safe.Converters.Add(converter);
        }

        safe.Converters.Add(new RtSecretValueNewtonsoftJsonConverter());
        return safe;
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
