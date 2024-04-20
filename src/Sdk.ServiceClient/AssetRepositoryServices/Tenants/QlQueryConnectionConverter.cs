using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
/// 
/// </summary>
public class QlQueryConnectionConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(QlQueryConnection<>);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var typeParams = typeToConvert.GetGenericArguments();
        var converterType = typeof(QlQueryConnectionConverter<>).MakeGenericType(typeParams);
        return (JsonConverter) Activator.CreateInstance(converterType)!;
    }
}


/// <summary>
/// Converter for Octo query response
/// </summary>
public class QlQueryConnectionConverter<TDto> : JsonConverter<QlQueryConnection<TDto>> where TDto: class
{
    /// <inheritdoc />
    public override QlQueryConnection<TDto>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token.");
        }

        bool hasIgnoredProperty = false;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                if (propertyName == "runtime" || propertyName == "constructionKit")
                {
                    hasIgnoredProperty = true;
                    continue;
                }

                reader.Read();

                var r = JsonSerializer.Deserialize<QlItemsContainer<TDto>>(ref reader, options);
                reader.Read();
                if (hasIgnoredProperty)
                {
                    reader.Read();
                }
                if (r == null)
                {
                    return null;
                }
                return new QlQueryConnection<TDto>(r);
            }
        }

        throw new QlQueryErrorException("End of JSON reached without finding object.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, QlQueryConnection<TDto> value, JsonSerializerOptions options)
    {
        throw new QlQueryErrorException("This converter is only for reading.");
    }
}