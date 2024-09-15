using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Converts a <see cref="NodePath"/> to and from JSON.
/// </summary>
public class NodePathConverter : JsonConverter<NodePath>
{
    /// <inheritdoc />
    public override NodePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Hier wird der String aus JSON gelesen und in NodePath konvertiert
        return new NodePath(reader.GetString());
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, NodePath value, JsonSerializerOptions options)
    {
        // Hier wird der NodePath in einen String konvertiert und geschrieben
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Converts a <see cref="NodePath"/> to and from a string.
/// </summary>
public class NodePathTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            string decodedValue = Uri.UnescapeDataString(stringValue);
            return new NodePath(decodedValue);
        }

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is NodePath nodePath)
        {
            return Uri.EscapeDataString(nodePath.ToString(CultureInfo.InvariantCulture));
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}