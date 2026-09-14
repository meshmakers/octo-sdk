using System.Text.Json;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Communication.Contracts.Serialization;
using YamlDotNet.Core;

namespace Communication.Contracts.Tests.Serialization;

/// <summary>
///     AB#5240. The contract that matters here is parity: converting a YAML document must yield the
///     same JSON document an author would have written by hand, so that a pipeline definition gets
///     the same verdict from a JSON Schema in either format. The regression these tests pin is a
///     converter that stringified every scalar, so numbers and booleans failed the schema's
///     <c>number</c> / <c>integer</c> / <c>boolean</c> types.
/// </summary>
public class YamlToJsonConverterTests
{
    private static JsonNode Convert(string yaml)
    {
        var node = YamlToJsonConverter.ToJsonNode(yaml);
        Assert.NotNull(node);
        return node!;
    }

    // ===== scalar typing — the AB#5240 regression ================================

    [Fact]
    public void PlainIntegerScalar_BecomesJsonNumber()
    {
        var node = Convert("comparisonValue: 1\n");

        Assert.Equal(JsonValueKind.Number, node["comparisonValue"]!.GetValueKind());
        Assert.Equal(1, node["comparisonValue"]!.GetValue<long>());
        Assert.Equal("""{"comparisonValue":1}""", node.ToJsonString());
    }

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("-2.25", -2.25)]
    [InlineData("1e3", 1000d)]
    [InlineData("-1.5E-2", -0.015)]
    [InlineData(".5", 0.5)]
    public void PlainFloatScalar_BecomesJsonNumber(string literal, double expected)
    {
        var node = Convert($"value: {literal}\n");

        Assert.Equal(JsonValueKind.Number, node["value"]!.GetValueKind());
        Assert.Equal(expected, node["value"]!.GetValue<double>());
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    public void PlainBooleanScalar_BecomesJsonBoolean(string literal, bool expected)
    {
        Assert.Equal(expected, Convert($"enabled: {literal}\n")["enabled"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData("0x1F", 31)]
    [InlineData("0o17", 15)]
    [InlineData("-0x10", -16)]
    public void RadixPrefixedInteger_BecomesItsJsonValue(string literal, long expected)
    {
        Assert.Equal(expected, Convert($"value: {literal}\n")["value"]!.GetValue<long>());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("Null")]
    [InlineData("NULL")]
    [InlineData("~")]
    [InlineData("")]
    public void PlainNullScalar_BecomesJsonNull(string literal)
    {
        var node = Convert($"value: {literal}\n");

        Assert.True(node.AsObject().ContainsKey("value"));
        Assert.Null(node["value"]);
    }

    // ===== quoting is respected ==================================================

    [Theory]
    [InlineData("\"1\"")]
    [InlineData("'1'")]
    public void QuotedNumericScalar_StaysString(string literal)
    {
        Assert.Equal("1", Convert($"value: {literal}\n")["value"]!.GetValue<string>());
    }

    [Fact]
    public void QuotedBooleanScalar_StaysString()
    {
        Assert.Equal("true", Convert("value: \"true\"\n")["value"]!.GetValue<string>());
    }

    [Fact]
    public void QuotedNullScalar_StaysString()
    {
        Assert.Equal("null", Convert("value: \"null\"\n")["value"]!.GetValue<string>());
    }

    [Fact]
    public void BlockScalar_StaysString()
    {
        Assert.Equal("123\n", Convert("value: |\n  123\n")["value"]!.GetValue<string>());
    }

    [Fact]
    public void ExplicitStringTag_OverridesImplicitTyping()
    {
        Assert.Equal("7", Convert("value: !!str 7\n")["value"]!.GetValue<string>());
    }

    [Fact]
    public void ExplicitIntTag_ProducesNumberEvenWhenQuoted()
    {
        Assert.Equal(7, Convert("value: !!int \"7\"\n")["value"]!.GetValue<long>());
    }

    [Fact]
    public void ExplicitTagContradictingTheValue_IsReportedNotSilentlyCoerced()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => YamlToJsonConverter.ToJsonNode("value: !!int abc\n"));

        Assert.Contains("!!int", ex.Message);
        Assert.Contains("abc", ex.Message);
    }

    // ===== values JSON cannot represent stay strings =============================

    [Fact]
    public void LongDigitRunThatOverflowsInt64_StaysStringRatherThanLosingDigits()
    {
        // Octo runtime ids are 24 digits and routinely written unquoted. Widening one to a double
        // would lose digits AND turn a value the schema types as a string into a number.
        const string id = "670000000000000000000002";

        var value = Convert($"adapterId: {id}\n")["adapterId"]!;

        Assert.Equal(JsonValueKind.String, value.GetValueKind());
        Assert.Equal(id, value.GetValue<string>());
    }

    [Theory]
    [InlineData(".inf")]
    [InlineData("-.inf")]
    [InlineData(".nan")]
    [InlineData("1e999")]
    public void NonFiniteNumber_StaysStringSoTheOutputRemainsValidJson(string literal)
    {
        var node = Convert($"value: {literal}\n");

        Assert.Equal(JsonValueKind.String, node["value"]!.GetValueKind());
        // Round-trips as JSON — the point of keeping it a string.
        Assert.NotNull(JsonNode.Parse(node.ToJsonString()));
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("no")]
    [InlineData("on")]
    [InlineData("off")]
    public void Yaml11Booleans_StayStrings(string literal)
    {
        // The runtime pipeline deserializer does not accept these for a bool property either;
        // recognising them here would pass definitions that then fail to load.
        Assert.Equal(literal, Convert($"value: {literal}\n")["value"]!.GetValue<string>());
    }

    [Fact]
    public void DateLikeScalar_StaysString()
    {
        Assert.Equal("2026-09-14", Convert("value: 2026-09-14\n")["value"]!.GetValue<string>());
    }

    // ===== structure =============================================================

    [Fact]
    public void NestedMappingsAndSequences_KeepShapeAndTypes()
    {
        const string yaml = """
            name: billing
            nodes:
              - nodeType: SumAggregation@1
                id: sum
                aggregation:
                  comparisonValue: 1
                  value: 1
                  strict: false
              - nodeType: ApplyChanges@1
                id: load
                retries: 3
            """;

        var node = Convert(yaml);

        Assert.Equal("billing", node["name"]!.GetValue<string>());
        var nodes = node["nodes"]!.AsArray();
        Assert.Equal(2, nodes.Count);
        Assert.Equal(1, nodes[0]!["aggregation"]!["comparisonValue"]!.GetValue<long>());
        Assert.Equal(1, nodes[0]!["aggregation"]!["value"]!.GetValue<long>());
        Assert.False(nodes[0]!["aggregation"]!["strict"]!.GetValue<bool>());
        Assert.Equal(3, nodes[1]!["retries"]!.GetValue<long>());
    }

    [Fact]
    public void RootLevelSequence_BecomesJsonArray()
    {
        var array = Convert("- 1\n- two\n- true\n").AsArray();

        Assert.Equal(1, array[0]!.GetValue<long>());
        Assert.Equal("two", array[1]!.GetValue<string>());
        Assert.True(array[2]!.GetValue<bool>());
    }

    [Fact]
    public void KeysAreTakenVerbatim_NotRecased()
    {
        // The schema's property names are the authority; re-casing keys here would invent errors.
        var node = Convert("nodeType: FromHttpRequest@1\nPascalKey: x\n");

        Assert.Equal(new[] { "nodeType", "PascalKey" }, node.AsObject().Select(p => p.Key).ToArray());
    }

    [Fact]
    public void Anchors_AreExpandedIntoEachUseSite()
    {
        const string yaml = """
            defaults: &defaults
              retries: 3
            first: *defaults
            """;

        Assert.Equal(3, Convert(yaml)["first"]!["retries"]!.GetValue<long>());
    }

    [Fact]
    public void DuplicateKey_IsRejectedRatherThanSilentlyResolved()
    {
        // YamlDotNet refuses the document, which is what the spec says and what the previous
        // implementations did too. Stricter than JSON's last-wins, but a duplicate key in a
        // pipeline definition is a mistake worth naming rather than quietly resolving.
        Assert.ThrowsAny<YamlException>(() => YamlToJsonConverter.ToJsonNode("value: 1\nvalue: 2\n"));
    }

    // ===== edge inputs ===========================================================

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    [InlineData("# just a comment\n")]
    public void InputWithoutADocument_ReturnsNull(string yaml)
    {
        Assert.Null(YamlToJsonConverter.ToJsonNode(yaml));
    }

    [Fact]
    public void MultipleDocuments_AreRejectedRatherThanSilentlyTruncated()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => YamlToJsonConverter.ToJsonNode("name: a\n---\nname: b\n"));

        Assert.Contains("single YAML document", ex.Message);
    }

    [Fact]
    public void NonScalarMappingKey_IsReported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => YamlToJsonConverter.ToJsonNode("? [a, b]\n: value\n"));

        Assert.Contains("not a scalar", ex.Message);
    }

    [Fact]
    public void MalformedYaml_ThrowsYamlExceptionForTheCallerToReport()
    {
        Assert.ThrowsAny<YamlException>(
            () => YamlToJsonConverter.ToJsonNode("name: foo\n  nodes: [   ]   bad indentation here\n"));
    }

    // ===== auto-detecting overload ===============================================

    [Fact]
    public void AutoDetect_JsonObject_UsesTheJsonPathAndKeepsTypes()
    {
        var node = YamlToJsonConverter.ToJsonNodeAutoDetect("""{"retries":3,"strict":false,"id":"x"}""")!;

        Assert.Equal(3, node["retries"]!.GetValue<long>());
        Assert.False(node["strict"]!.GetValue<bool>());
        Assert.Equal("x", node["id"]!.GetValue<string>());
    }

    [Fact]
    public void AutoDetect_JsonArray_UsesTheJsonPath()
    {
        Assert.Equal(2, YamlToJsonConverter.ToJsonNodeAutoDetect("[1, 2]")!.AsArray().Count);
    }

    [Fact]
    public void AutoDetect_Yaml_UsesTheYamlPath()
    {
        Assert.Equal(3, YamlToJsonConverter.ToJsonNodeAutoDetect("retries: 3\n")!["retries"]!.GetValue<long>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AutoDetect_BlankInput_ReturnsNull(string definition)
    {
        Assert.Null(YamlToJsonConverter.ToJsonNodeAutoDetect(definition));
    }

    [Fact]
    public void AutoDetect_TheSameDocumentInBothFormats_ConvertsIdentically()
    {
        const string yaml = """
            name: billing
            enabled: true
            nodes:
              - id: sum
                retries: 3
                timeout: 1.5
                adapterId: 670000000000000000000002
            """;
        const string json = """
            {"name":"billing","enabled":true,
             "nodes":[{"id":"sum","retries":3,"timeout":1.5,"adapterId":"670000000000000000000002"}]}
            """;

        var fromYaml = YamlToJsonConverter.ToJsonNodeAutoDetect(yaml)!;
        var fromJson = YamlToJsonConverter.ToJsonNodeAutoDetect(json)!;

        Assert.Equal(fromJson.ToJsonString(), fromYaml.ToJsonString());
    }
}
