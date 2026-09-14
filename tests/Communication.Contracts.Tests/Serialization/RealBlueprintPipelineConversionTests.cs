using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Communication.Contracts.Serialization;

namespace Communication.Contracts.Tests.Serialization;

/// <summary>
///     AB#5240 acceptance: the converter is exercised against a real multi-node blueprint pipeline,
///     not only a minimal snippet. The fixture is the pipeline shipped in the Monitoring.CkHealth
///     blueprint — ~130 lines, nested <c>ForEach</c> / <c>If</c> transformations, and the numeric
///     and boolean node properties that the old parse path turned into strings.
/// </summary>
public class RealBlueprintPipelineConversionTests
{
    private static string LoadFixture()
    {
        const string name = "Communication.Contracts.Tests.Serialization.Resources.ck-health-check.pipeline.yaml";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        Assert.True(stream is not null, $"the embedded fixture '{name}' must ship with the test assembly");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static JsonNode Convert()
    {
        var node = YamlToJsonConverter.ToJsonNode(LoadFixture());
        Assert.NotNull(node);
        return node!;
    }

    [Fact]
    public void RealBlueprintPipeline_ConvertsToValidJson()
    {
        // The converted document has to be JSON the schema evaluator can read back.
        Assert.NotNull(JsonNode.Parse(Convert().ToJsonString()));
    }

    [Fact]
    public void RealBlueprintPipeline_KeepsItsStructure()
    {
        var node = Convert();

        Assert.Equal(2, node["triggers"]!.AsArray().Count);
        Assert.True(node["transformations"]!.AsArray().Count > 5);
        Assert.Equal("FromPipelineTriggerEvent@1", node["triggers"]![0]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void RealBlueprintPipeline_NumericAndBooleanNodeProperties_AreNotStrings()
    {
        var kinds = new Dictionary<JsonValueKind, int>();
        Walk(Convert(), kinds);

        // The regression: every one of these came through as JsonValueKind.String.
        Assert.True(kinds.GetValueOrDefault(JsonValueKind.Number) > 0,
            "the pipeline carries retry.maxAttempts and maxDegreeOfParallelism");
        Assert.True(kinds.GetValueOrDefault(JsonValueKind.True) + kinds.GetValueOrDefault(JsonValueKind.False) > 0,
            "the pipeline carries continueOnError flags");
    }

    [Fact]
    public void RealBlueprintPipeline_TypesTheScalarsTheSchemaCaresAbout()
    {
        var node = Convert();
        var transformations = node["transformations"]!.AsArray();

        var httpRequest = transformations.Single(t => t!["description"]?.GetValue<string>() == "Acquire access token")!;
        Assert.Equal(3, httpRequest["retry"]!["maxAttempts"]!.GetValue<long>());

        var perTenant = transformations.Single(t => t!["description"]?.GetValue<string>() == "Check every monitored tenant")!;
        Assert.True(perTenant["continueOnError"]!.GetValue<bool>());
        Assert.Equal(1, perTenant["maxDegreeOfParallelism"]!.GetValue<long>());

        // …and a quoted scalar that merely looks numeric is still a string.
        var teamsAlert = FindFirstWithProperty(node, "themeColor")!;
        Assert.Equal(JsonValueKind.String, teamsAlert["themeColor"]!.GetValueKind());
    }

    private static void Walk(JsonNode? node, Dictionary<JsonValueKind, int> kinds)
    {
        switch (node)
        {
            case null:
                break;
            case JsonObject obj:
                foreach (var property in obj)
                {
                    Walk(property.Value, kinds);
                }
                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    Walk(item, kinds);
                }
                break;
            default:
                var kind = node.GetValueKind();
                kinds[kind] = kinds.GetValueOrDefault(kind) + 1;
                break;
        }
    }

    private static JsonObject? FindFirstWithProperty(JsonNode? node, string propertyName)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey(propertyName))
                {
                    return obj;
                }
                return obj.Select(p => FindFirstWithProperty(p.Value, propertyName)).FirstOrDefault(m => m != null);
            case JsonArray array:
                return array.Select(i => FindFirstWithProperty(i, propertyName)).FirstOrDefault(m => m != null);
            default:
                return null;
        }
    }
}
