using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
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
///     <para>
///         The input is usually caller-supplied, so the conversion is bounded by
///         <see cref="YamlToJsonConverterOptions" />: the token stream is scanned before the document is loaded, and a
///         document that nests too deep, expands to too many nodes once its aliases are resolved, or uses a recursive or
///         forward alias is refused with <see cref="YamlDocumentLimitExceededException" /> without being loaded or
///         expanded.
///     </para>
/// </remarks>
public static class YamlToJsonConverter
{
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
    ///     The input holds more than one document or uses a mapping key that is not a scalar and
    ///     therefore has no JSON equivalent.
    /// </exception>
    /// <exception cref="YamlDocumentLimitExceededException">
    ///     The input exceeds one of the default limits of <see cref="YamlToJsonConverterOptions" />.
    /// </exception>
    public static JsonNode? ToJsonNode(string yaml) => ToJsonNode(yaml, YamlToJsonConverterOptions.Default);

    /// <summary>
    ///     Parses <paramref name="yaml" /> and converts it to a <see cref="JsonNode" /> within the limits of
    ///     <paramref name="options" />.
    /// </summary>
    /// <param name="yaml">The YAML document.</param>
    /// <param name="options">The limits.</param>
    /// <returns>
    ///     The converted node, or <c>null</c> for input that holds no document at all (empty or
    ///     comment-only) or whose root resolves to YAML null.
    /// </returns>
    /// <exception cref="YamlException">The input is not well-formed YAML.</exception>
    /// <exception cref="NotSupportedException">
    ///     The input holds more than one document or uses a mapping key that is not a scalar and
    ///     therefore has no JSON equivalent.
    /// </exception>
    /// <exception cref="YamlDocumentLimitExceededException">
    ///     The input exceeds one of the limits of <paramref name="options" />.
    /// </exception>
    public static JsonNode? ToJsonNode(string yaml, YamlToJsonConverterOptions options)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        // Before the load: YamlStream builds its node tree recursively (a deep document would
        // overflow the stack) and keeps aliases as shared nodes, which only the conversion below
        // copies. The scan refuses both on the token stream, iteratively.
        Scan(yaml, options);

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

        var budget = new NodeBudget(options);
        return ConvertNode(stream.Documents[0].RootNode, 0, budget);
    }

    /// <summary>
    ///     Converts a definition that may be either YAML or JSON. A document opening with <c>{</c>
    ///     or <c>[</c> is tried as JSON first and re-read as YAML if that fails, because those
    ///     characters also open a YAML flow collection.
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
    ///     The YAML input has a shape with no JSON equivalent — see <see cref="ToJsonNode(string)" />.
    /// </exception>
    /// <exception cref="YamlDocumentLimitExceededException">
    ///     The YAML input exceeds one of the default limits of <see cref="YamlToJsonConverterOptions" />.
    /// </exception>
    public static JsonNode? ToJsonNodeAutoDetect(string definition) =>
        ToJsonNodeAutoDetect(definition, YamlToJsonConverterOptions.Default);

    /// <summary>
    ///     Converts a definition that may be either YAML or JSON, within the limits of
    ///     <paramref name="options" /> for YAML input — see <see cref="ToJsonNodeAutoDetect(string)" />.
    ///     JSON input is bounded by the depth limit of <see cref="JsonNode.Parse(string, JsonNodeOptions?, JsonDocumentOptions)" />
    ///     (64 levels by default) and has no aliases.
    /// </summary>
    /// <param name="definition">The definition document, in either format.</param>
    /// <param name="options">The limits for YAML input.</param>
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
    ///     The YAML input has a shape with no JSON equivalent — see <see cref="ToJsonNode(string)" />.
    /// </exception>
    /// <exception cref="YamlDocumentLimitExceededException">
    ///     The YAML input exceeds one of the limits of <paramref name="options" />.
    /// </exception>
    public static JsonNode? ToJsonNodeAutoDetect(string definition, YamlToJsonConverterOptions options)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var firstNonWhitespace = definition.AsSpan().TrimStart();
        if (firstNonWhitespace.IsEmpty)
        {
            return null;
        }

        if (firstNonWhitespace[0] is '{' or '[')
        {
            try
            {
                return JsonNode.Parse(definition);
            }
            catch (JsonException)
            {
                // `{` and `[` also open a YAML *flow* collection, whose keys need no quotes
                // (`{enabled: true}`). Falling through keeps the auto-detection honest instead
                // of refusing YAML this method advertises it accepts.
            }
        }
        return ToJsonNode(definition, options);
    }

    /// <summary>
    ///     Scans <paramref name="yaml" /> as a token stream and counts its nodes (scalars, sequences,
    ///     mappings) as the conversion would produce them, with every alias expanded to the size of
    ///     its anchored node. Refuses the document once the count exceeds the node limit, the nesting
    ///     (every alias expanded where it stands) exceeds the depth limit, or an alias names an anchor
    ///     that is not complete yet — still open (recursive) or not defined before the alias: the
    ///     loader resolves both into a cycle or a forward reference that the conversion would expand
    ///     without bound. The scan is iterative and holds only the size and height of every anchor
    ///     and the open collections, so it cannot itself run out of
    ///     stack or memory. Text that stops tokenizing ends the scan; the loader then reports the
    ///     error at the same position and reads no further.
    /// </summary>
    private static void Scan(string yaml, YamlToJsonConverterOptions options)
    {
        var parser = new Parser(new StringReader(yaml));
        // Per completed anchor: its expanded size (nodes) and its expanded height (levels of
        // sequences and mappings, aliases expanded; a scalar has height 0).
        var anchors = new Dictionary<string, (long Size, int Height)>(StringComparer.Ordinal);
        var open = new Stack<OpenCollection>();
        long total = 0;

        void AddToParent(long size, int height)
        {
            if (open.TryPeek(out var parent))
            {
                parent.Size += size;
                parent.ChildHeight = Math.Max(parent.ChildHeight, height);
            }
        }

        try
        {
            while (parser.MoveNext())
            {
                switch (parser.Current)
                {
                    case DocumentStart:
                        // Anchors are scoped to their document.
                        anchors.Clear();
                        break;
                    case Scalar scalar:
                        total++;
                        AddToParent(1, 0);
                        if (!scalar.Anchor.IsEmpty)
                        {
                            anchors[scalar.Anchor.Value] = (1, 0);
                        }

                        break;
                    case NodeEvent { Anchor: var anchor } and (SequenceStart or MappingStart):
                        total++;
                        if (!anchor.IsEmpty)
                        {
                            // A redefined anchor names the new node from here on; until that node
                            // is complete, an alias to it is recursive.
                            anchors.Remove(anchor.Value);
                        }

                        open.Push(new OpenCollection(anchor.IsEmpty ? null : anchor.Value));
                        if (open.Count > options.MaxDepth)
                        {
                            throw DepthLimitExceeded(options.MaxDepth);
                        }

                        break;
                    case SequenceEnd or MappingEnd when open.Count > 0:
                    {
                        var completed = open.Pop();
                        var height = completed.ChildHeight + 1;
                        if (completed.Anchor != null)
                        {
                            anchors[completed.Anchor] = (completed.Size, height);
                        }

                        AddToParent(completed.Size, height);
                        break;
                    }
                    case AnchorAlias alias:
                        if (!anchors.TryGetValue(alias.Value.Value, out var anchored))
                        {
                            throw new YamlDocumentLimitExceededException(YamlDocumentLimit.Alias, 0,
                                $"The YAML alias at {alias.Start} refers to an anchor that is not defined before it " +
                                "or that contains the alias itself; recursive and forward aliases are not accepted.");
                        }

                        // An alias nests its anchored node where it stands: an alias chain can reach a
                        // depth no part of the text has. The loader compares mapping keys by content,
                        // recursively, so the depth must be refused here, before the load.
                        if (open.Count + anchored.Height > options.MaxDepth)
                        {
                            throw DepthLimitExceeded(options.MaxDepth);
                        }

                        total += anchored.Size;
                        AddToParent(anchored.Size, anchored.Height);
                        break;
                }

                if (total > options.MaxNodes)
                {
                    throw NodeLimitExceeded(options.MaxNodes);
                }
            }
        }
        catch (YamlException)
        {
            // Not well-formed from here on: the loader refuses it at the same position.
        }
    }

    /// <summary>A sequence or mapping the scan has seen start but not end.</summary>
    private sealed class OpenCollection(string? anchor)
    {
        public string? Anchor { get; } = anchor;

        /// <summary>Nodes so far, the collection itself included, aliases expanded.</summary>
        public long Size { get; set; } = 1;

        /// <summary>The largest height of a child so far (aliases expanded).</summary>
        public int ChildHeight { get; set; }
    }

    private static YamlDocumentLimitExceededException DepthLimitExceeded(int maxDepth) =>
        new(YamlDocumentLimit.Depth, maxDepth,
            $"The YAML document nests more than {maxDepth} levels deep (aliases expanded); the limit is {maxDepth}.");

    private static YamlDocumentLimitExceededException NodeLimitExceeded(long maxNodes) =>
        new(YamlDocumentLimit.Nodes, maxNodes,
            $"The YAML document expands to more than {maxNodes} nodes once its aliases are resolved; the limit " +
            $"is {maxNodes}. Write repeated parts out instead of nesting aliases, or split the document.");

    /// <summary>
    ///     Counts the nodes the conversion produces. The scan already refused a document above the
    ///     limits; this keeps the conversion bounded on its own, whatever the loader made of the text.
    /// </summary>
    private sealed class NodeBudget(YamlToJsonConverterOptions options)
    {
        private long _used;

        public int MaxDepth => options.MaxDepth;

        public void Take()
        {
            if (++_used > options.MaxNodes)
            {
                throw NodeLimitExceeded(options.MaxNodes);
            }
        }
    }

    private static JsonNode? ConvertNode(YamlNode node, int depth, NodeBudget budget)
    {
        if (depth > budget.MaxDepth)
        {
            throw DepthLimitExceeded(budget.MaxDepth);
        }

        budget.Take();
        return node switch
        {
            YamlScalarNode scalar => ConvertScalar(scalar),
            YamlSequenceNode sequence => ConvertSequence(sequence, depth, budget),
            YamlMappingNode mapping => ConvertMapping(mapping, depth, budget),
            _ => throw new NotSupportedException(
                $"Unsupported YAML node type '{node.GetType().Name}' at {node.Start}."),
        };
    }

    private static JsonArray ConvertSequence(YamlSequenceNode sequence, int depth, NodeBudget budget)
    {
        var array = new JsonArray();
        foreach (var child in sequence.Children)
        {
            array.Add(ConvertNode(child, depth + 1, budget));
        }
        return array;
    }

    private static JsonObject ConvertMapping(YamlMappingNode mapping, int depth, NodeBudget budget)
    {
        var obj = new JsonObject();
        foreach (var (keyNode, valueNode) in mapping.Children)
        {
            if (keyNode is not YamlScalarNode { Value: { } key })
            {
                throw new NotSupportedException(
                    $"YAML mapping key at {keyNode.Start} is not a scalar; JSON object keys must be strings.");
            }
            // YamlStream refuses a document with two *identical* keys, but it compares scalars by
            // tag AND value — `1` and `!!str 1` are distinct YAML keys that both render as the JSON
            // property "1". Assigning through the indexer would drop the first value without a word.
            if (obj.ContainsKey(key))
            {
                throw new NotSupportedException(
                    $"YAML mapping key at {keyNode.Start} collapses to the JSON property '{key}', " +
                    "which the mapping already carries; the document has no lossless JSON equivalent.");
            }
            budget.Take(); // the key
            obj.Add(key, ConvertNode(valueNode, depth + 1, budget));
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
            TagFloat => ParseFloat(value)
                ?? throw TaggedScalarMismatch(scalar, value, "!!float", "a float JSON can represent"),
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
            return number;
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

    /// <summary>
    ///     Parses an integer-shaped scalar, or returns <c>null</c> when <see cref="long" /> cannot
    ///     hold it (the caller then keeps the scalar as a string rather than misrepresenting it).
    /// </summary>
    /// <remarks>
    ///     The magnitude is parsed <em>unsigned</em> and range-checked by hand. Two reasons:
    ///     <c>Convert.ToInt64(s, 16)</c> reads the high bit as a sign, so
    ///     <c>0xFFFFFFFFFFFFFFFF</c> would silently become <c>-1</c> instead of overflowing; and
    ///     parsing the magnitude as a signed <c>long</c> rejects <c>-9223372036854775808</c>,
    ///     whose magnitude is one past <see cref="long.MaxValue" /> even though the value itself
    ///     is perfectly representable.
    /// </remarks>
    private static long? ParseInteger(string value)
    {
        var negative = value.Length > 0 && value[0] == '-';
        var digits = StripSign(value);
        if (digits.Length == 0)
        {
            return null;
        }

        ulong magnitude;
        // Radix prefixes are YAML spelling that JSON has no literal for; the value is what counts.
        if (HasRadixPrefix(digits, out var body, out var fromBase))
        {
            try
            {
                magnitude = Convert.ToUInt64(body, fromBase);
            }
            catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
            {
                return null;
            }
        }
        else if (!ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out magnitude))
        {
            return null;
        }

        const ulong negativeLimit = (ulong)long.MaxValue + 1; // |long.MinValue|
        if (negative)
        {
            if (magnitude > negativeLimit)
            {
                return null;
            }
            return magnitude == negativeLimit ? long.MinValue : -(long)magnitude;
        }
        return magnitude <= long.MaxValue ? (long)magnitude : null;
    }

    /// <summary>
    ///     Parses a float-shaped scalar into a JSON number, or returns <c>null</c> when JSON has no
    ///     faithful representation for it (the caller then keeps the scalar as a string).
    /// </summary>
    /// <remarks>
    ///     A scalar that is already a valid JSON number literal is handed to
    ///     <see cref="JsonNode.Parse(string, JsonNodeOptions?, JsonDocumentOptions)" /> verbatim so
    ///     its exact lexical form survives. Widening everything through <see cref="double" /> would
    ///     quietly rewrite the authored value — <c>9007199254740993.0</c> loses its last digit and
    ///     <c>1e-400</c> underflows to <c>0</c>, neither of which <see cref="double.IsFinite" />
    ///     catches. Only the YAML-only spellings JSON has no literal for (<c>.5</c>, <c>1.</c>,
    ///     <c>+1.5</c>) are normalised through <c>double</c>; <c>.inf</c> and <c>.nan</c> fail both
    ///     paths and stay strings, which is what keeps the emitted document valid JSON.
    /// </remarks>
    private static JsonNode? ParseFloat(string value)
    {
        try
        {
            var literal = JsonNode.Parse(value);
            if (literal is JsonValue && literal.GetValueKind() == JsonValueKind.Number)
            {
                return literal;
            }
        }
        catch (JsonException)
        {
            // Not a JSON number literal — try the YAML-only spellings below.
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || !double.IsFinite(parsed))
        {
            return null;
        }
        return JsonValue.Create(parsed);
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
