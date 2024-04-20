using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
/// 
/// </summary>
public class QlMutationResponseConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(QlMutationResponse<>);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var typeParams = typeToConvert.GetGenericArguments();
        var converterType = typeof(QlMutationResponseConverter<>).MakeGenericType(typeParams);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Converter for Octo query response
/// </summary>
public class QlMutationResponseConverter<TDto> : JsonConverter<QlMutationResponse<TDto>> where TDto : class
{
    /// <inheritdoc />
    public override QlMutationResponse<TDto>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                if (propertyName == "runtime" || propertyName == "constructionKit")
                {
                    continue;
                }

                reader.Read(); // property connection
                reader.Read(); // start object
                reader.Read(); // update property

                var r = JsonSerializer.Deserialize<IEnumerable<TDto>>(ref reader, options);
                reader.Read(); // end array
                reader.Read(); // end object
                reader.Read(); // end object
                if (r == null)
                {
                    return null;
                }

                return new QlMutationResponse<TDto>(r);
            }
        }

        throw new QlQueryErrorException("End of JSON reached without finding object.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, QlMutationResponse<TDto> value, JsonSerializerOptions options)
    {
        throw new QlQueryErrorException("This converter is only for reading.");
    }
}