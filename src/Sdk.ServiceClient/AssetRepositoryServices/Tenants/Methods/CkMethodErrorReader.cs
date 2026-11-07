using System.Collections;
using System.Globalization;
using System.Text.Json;
using GraphQL;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     Reads the CK method error contract of the asset repository (<c>CkMethodGraphQlError</c>: <c>extensions.code</c>,
///     <c>extensions.parameters</c>, <c>extensions.details</c>) from the GraphQL errors of a response (AB#8423).
///     The extension values arrive as the GraphQL client deserialized them (maps, lists, primitives or
///     <see cref="JsonElement" />); all forms are read.
/// </summary>
public static class CkMethodErrorReader
{
    private const string CodeKey = "code";
    private const string ParametersKey = "parameters";
    private const string DetailsKey = "details";

    /// <summary>
    ///     Returns the method error of the first GraphQL error that carries <c>extensions.code</c>, or <c>null</c>.
    /// </summary>
    /// <param name="errors">The GraphQL errors of a response</param>
    public static CkMethodError? Read(IReadOnlyList<GraphQLError>? errors)
    {
        if (errors == null)
        {
            return null;
        }

        foreach (var error in errors)
        {
            if (error.Extensions == null || !error.Extensions.TryGetValue(CodeKey, out var codeValue))
            {
                continue;
            }

            var code = AsString(codeValue);
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            error.Extensions.TryGetValue(ParametersKey, out var parameters);
            error.Extensions.TryGetValue(DetailsKey, out var details);
            return new CkMethodError(code!, error.Message ?? string.Empty, ReadParameters(parameters),
                ReadDetails(details));
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> ReadParameters(object? value)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        switch (value)
        {
            case JsonElement { ValueKind: JsonValueKind.Object } element:
                foreach (var property in element.EnumerateObject())
                {
                    var text = AsString(property.Value);
                    if (text != null)
                    {
                        result[property.Name] = text;
                    }
                }

                break;
            case IDictionary<string, object?> map:
                foreach (var pair in map)
                {
                    var text = AsString(pair.Value);
                    if (text != null)
                    {
                        result[pair.Key] = text;
                    }
                }

                break;
        }

        return result;
    }

    private static IReadOnlyList<CkMethodErrorDetail> ReadDetails(object? value)
    {
        var items = new List<object?>();
        switch (value)
        {
            case JsonElement { ValueKind: JsonValueKind.Array } element:
                items.AddRange(element.EnumerateArray().Select(e => (object?)e));
                break;
            case string:
                break;
            case IEnumerable enumerable:
                items.AddRange(enumerable.Cast<object?>());
                break;
        }

        var result = new List<CkMethodErrorDetail>();
        foreach (var item in items)
        {
            object? code = null;
            object? parameters = null;
            switch (item)
            {
                case JsonElement { ValueKind: JsonValueKind.Object } element:
                    if (element.TryGetProperty(CodeKey, out var c))
                    {
                        code = c;
                    }

                    if (element.TryGetProperty(ParametersKey, out var p))
                    {
                        parameters = p;
                    }

                    break;
                case IDictionary<string, object?> map:
                    map.TryGetValue(CodeKey, out code);
                    map.TryGetValue(ParametersKey, out parameters);
                    break;
            }

            var codeText = AsString(code);
            if (!string.IsNullOrWhiteSpace(codeText))
            {
                result.Add(new CkMethodErrorDetail(codeText!, ReadParameters(parameters)));
            }
        }

        return result;
    }

    private static string? AsString(object? value) => value switch
    {
        null => null,
        string s => s,
        JsonElement { ValueKind: JsonValueKind.String } e => e.GetString(),
        JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        JsonElement e => e.GetRawText(),
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}
