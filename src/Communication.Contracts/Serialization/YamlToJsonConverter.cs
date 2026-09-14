using System.Globalization;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     Converts a YAML document into the equivalent <see cref="JsonNode" />, preserving scalar
///     types so that a document is interpreted identically whether it arrives as YAML or as the
///     equivalent JSON.
/// </summary>
/// <remarks>
///     <para>
///         This lives in the shared contracts assembly because pipeline definitions are authored in
///         YAML but validated against a JSON Schema, and every party that does so — the
///         Communication Controller's deploy-time validator and the MCP server's
///         <c>validate_pipeline_definition</c> tool — has to reach the same verdict on the same
///         document. Both previously hand-rolled the conversion and both got it wrong the same way
///         (AB#5240).
///     </para>
///     <para>
///         The obvious shortcut — <c>Deserialize&lt;object?&gt;</c> followed by a JSON-compatible
///         re-serialization — is what was wrong: untyped YamlDotNet deserialization yields every
///         scalar as a <see cref="string" />, so numbers and booleans reach the schema as strings
///         and fail its <c>number</c>, <c>integer</c> and <c>boolean</c> types. This converter walks
///         the representation model instead, where the scalar style and tag needed to resolve the
///         type are still intact.
///     </para>
///     <para>
///         Scalars are resolved per the YAML 1.2 core schema, restricted to the types JSON itself
///         has: null, boolean, integer, float, string. A quoted, literal or folded scalar is always
///         a string, as is any scalar carrying an explicit <c>!!str</c> tag. Anything a plain scalar
///         cannot be resolved to losslessly stays a string, so the result is always a valid JSON
///         document that says what the author wrote.
///     </para>
///     <para>
///         The YAML 1.1 booleans (<c>yes</c>, <c>no</c>, <c>on</c>, <c>off</c>) are deliberately NOT
///         recognised: the runtime pipeline deserializer
///         (<c>YamlPipelineConfigurationSerializer</c>, which binds to typed properties) does not
///         accept them for a <c>bool</c> property either, so treating them as strings keeps
///         validation aligned with what actually loads.
///     </para>
/// </remarks>
public static class YamlToJsonConverter
{
    /// <summary>
    ///     Guards against the unbounded recursion a self-referencing YAML anchor would otherwise
    ///     cause. Real pipeline definitions nest a handful of levels deep.
    /// </summary>
    private const int MaxDepth = 256;

    private const string TagStr = "tag:yaml.org,2002:str";
    private const string TagInt = "tag:yaml.org,2002:int";
    private const string TagFloat = "tag:yaml.org,2002:float";
    private const string TagBool = "tag:yaml.org,2002:bool";
    private const string TagNull = "tag:yaml.org,2002:null";

    /// <summary>
    ///     Parses <paramref name="yaml" /> and converts it to a <see cref="JsonNode" />.
    /// </summary>
    /// <param name="yaml">The YAML document.</param>
    /// <returns>
    ///     The converted node, or <c>null</c> for input that holds no document at all (empty or
    ///     comment-only) or whose root resolves to YAML null.
    /// </returns>
    /// <exception cref="YamlException">The input is not well-formed YAML.</exception>
    /// <exception cref="NotSupportedException">
    ///     The input holds more than one document, nests deeper than 256 levels, or uses a mapping
    ///     key that is not a scalar and therefore has no JSON equivalent.
    /// </exception>
    public static JsonNode? ToJsonNode(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));

        if (stream.Documents.Count == 0)
        {
            return null;
        }
        if (stream.Documents.Count > 1)
        {
            throw new NotSupportedException(
                $"Expected a single YAML document but found {stream.Documents.Count}; " +
                "a pipeline definition is one document — remove the `---` separators.");
        }

        return ConvertNode(stream.Documents[0].RootNode, 0);
    }

    /// <summary>
    ///     Converts a definition that may be either YAML or JSON, auto-detecting the format from
    ///     the first non-whitespace character (<c>{</c> or <c>[</c> means JSON).
    /// </summary>
    /// <param name="definition">The definition document, in either format.</param>
    /// <returns>
    ///     The converted node, or <c>null</c> when the input is empty or resolves to null.
    /// </returns>
    /// <exception cref="System.Text.Json.JsonException">
    ///     The input was detected as JSON but is not well-formed.
    /// </exception>
    /// <exception cref="YamlException">
    ///     The input was detected as YAML but is not well-formed.
    /// </exception>
    /// <exception cref="NotSupportedException">
    ///     The YAML input has a shape with no JSON equivalent — see <see cref="ToJsonNode" />.
    /// </exception>
    public static JsonNode? ToJsonNodeAutoDetect(string definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var firstNonWhitespace = definition.AsSpan().TrimStart();
        if (firstNonWhitespace.IsEmpty)
        {
            return null;
        }
        return firstNonWhitespace[0] is '{' or '['
            ? JsonNode.Parse(definition)
            : ToJsonNode(definition);
    }

    private static JsonNode? ConvertNode(YamlNode node, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new NotSupportedException(
                $"YAML nesting exceeds the supported depth of {MaxDepth} — a recursive anchor?");
        }

        return node switch
        {
            YamlScalarNode scalar => ConvertScalar(scalar),
            YamlSequenceNode sequence => ConvertSequence(sequence, depth),
            YamlMappingNode mapping => ConvertMapping(mapping, depth),
            _ => throw new NotSupportedException(
                $"Unsupported YAML node type '{node.GetType().Name}' at {node.Start}."),
        };
    }

    private static JsonArray ConvertSequence(YamlSequenceNode sequence, int depth)
    {
        var array = new JsonArray();
        foreach (var child in sequence.Children)
        {
            array.Add(ConvertNode(child, depth + 1));
        }
        return array;
    }

    private static JsonObject ConvertMapping(YamlMappingNode mapping, int depth)
    {
        var obj = new JsonObject();
        foreach (var (keyNode, valueNode) in mapping.Children)
        {
            if (keyNode is not YamlScalarNode { Value: { } key })
            {
                throw new NotSupportedException(
                    $"YAML mapping key at {keyNode.Start} is not a scalar; JSON object keys must be strings.");
            }
            // Indexer, not Add, purely to stay total: YamlStream itself refuses a document with a
            // duplicate key before the walk gets here, so this never overwrites in practice.
            obj[key] = ConvertNode(valueNode, depth + 1);
        }
        return obj;
    }

    private static JsonNode? ConvertScalar(YamlScalarNode scalar)
    {
        var value = scalar.Value;
        if (value is null)
        {
            return null;
        }

        if (scalar.Tag.IsEmpty)
        {
            return ResolveUntagged(scalar, value);
        }

        return scalar.Tag.Value switch
        {
            TagStr => JsonValue.Create(value),
            TagNull => null,
            TagBool => ParseBool(value) is { } tagged
                ? JsonValue.Create(tagged)
                : throw TaggedScalarMismatch(scalar, value, "!!bool", "a boolean"),
            TagInt => ParseInteger(value) is { } tagged
                ? JsonValue.Create(tagged)
                : throw TaggedScalarMismatch(scalar, value, "!!int", "an integer JSON can represent"),
            TagFloat => ParseFloat(value) is { } tagged
                ? JsonValue.Create(tagged)
                : throw TaggedScalarMismatch(scalar, value, "!!float", "a float JSON can represent"),
            // An unrecognised or application-specific tag carries no JSON meaning of its own, so
            // fall back to resolving the scalar on its own terms.
            _ => ResolveUntagged(scalar, value),
        };
    }

    private static NotSupportedException TaggedScalarMismatch(
        YamlScalarNode scalar, string value, string tag, string expected) =>
        new($"Scalar at {scalar.Start} is tagged {tag} but '{value}' is not {expected}.");

    private static JsonNode? ResolveUntagged(YamlScalarNode scalar, string value)
    {
        // Only a plain (unquoted) scalar carries an implicit type; everything else is text the
        // author explicitly quoted, so `"1"` stays the string "1".
        if (scalar.Style != ScalarStyle.Plain)
        {
            return JsonValue.Create(value);
        }

        if (value.Length == 0 || value == "~" || value is "null" or "Null" or "NULL")
        {
            return null;
        }
        if (ParseBool(value) is { } boolean)
        {
            return JsonValue.Create(boolean);
        }
        // Integer before float, and neither falls back to the other: an integer too large for
        // `long` must NOT become a float. Octo runtime ids are 24 digits and frequently written
        // unquoted (`adapterId: 670000000000000000000002`); widening one to a double would both
        // lose digits and turn a value the schema types as a string into a number — the very class
        // of false positive this converter exists to remove. Unrepresentable numbers stay strings.
        if (IsIntegerShaped(value))
        {
            return ParseInteger(value) is { } integer ? JsonValue.Create(integer) : JsonValue.Create(value);
        }
        if (IsFloatShaped(value) && ParseFloat(value) is { } number)
        {
            return JsonValue.Create(number);
        }
        return JsonValue.Create(value);
    }

    private static bool? ParseBool(string value) => value switch
    {
        "true" or "True" or "TRUE" => true,
        "false" or "False" or "FALSE" => false,
        _ => null,
    };

    /// <summary>Whether the scalar reads as an integer literal — decimal, hex or octal.</summary>
    private static bool IsIntegerShaped(string value)
    {
        var digits = StripSign(value);
        if (digits.Length == 0)
        {
            return false;
        }
        if (HasRadixPrefix(digits, out var body, out _))
        {
            return body.Length > 0;
        }
        return digits.All(char.IsAsciiDigit);
    }

    /// <summary>
    ///     Whether the scalar reads as a float literal. Requires a decimal point or an exponent, so
    ///     that integer-shaped text is never reinterpreted as a float.
    /// </summary>
    private static bool IsFloatShaped(string value) =>
        value.AsSpan().IndexOfAny('.', 'e', 'E') >= 0;

    private static long? ParseInteger(string value)
    {
        var sign = value.Length > 0 && value[0] == '-' ? -1 : 1;
        var digits = StripSign(value);
        if (digits.Length == 0)
        {
            return null;
        }

        // Radix prefixes are YAML spelling that JSON has no literal for; the value is what counts.
        if (HasRadixPrefix(digits, out var body, out var fromBase))
        {
            try
            {
                return sign * Convert.ToInt64(body, fromBase);
            }
            catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
            {
                return null;
            }
        }

        return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? sign * parsed
            : null;
    }

    private static double? ParseFloat(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return null;
        }
        // `.inf`, `.nan` and overflowing literals such as 1e999 have no JSON representation —
        // leaving them as strings keeps the emitted document parseable.
        return double.IsFinite(parsed) ? parsed : null;
    }

    private static string StripSign(string value) =>
        value.Length > 0 && value[0] is '+' or '-' ? value[1..] : value;

    private static bool HasRadixPrefix(string digits, out string body, out int fromBase)
    {
        if (digits.StartsWith("0x", StringComparison.Ordinal))
        {
            (body, fromBase) = (digits[2..], 16);
            return true;
        }
        if (digits.StartsWith("0o", StringComparison.Ordinal))
        {
            (body, fromBase) = (digits[2..], 8);
            return true;
        }
        (body, fromBase) = (string.Empty, 10);
        return false;
    }
}
