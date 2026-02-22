using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Sdk.Common.Tests.EtlDataPipeline.Configuration;

public class PipelineSchemaGeneratorTests
{
    private readonly INodeSchemaRegistry _registry = A.Fake<INodeSchemaRegistry>();

    private PipelineSchemaGenerator CreateGenerator() => new(_registry);

    [Fact]
    public void GenerateSchema_EmptyRegistry_ReturnsValidSchema()
    {
        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>());

        var generator = CreateGenerator();
        var json = generator.GenerateSchema();
        var schema = JObject.Parse(json);

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", schema["$schema"]?.Value<string>());
        Assert.Equal("object", schema["type"]?.Value<string>());
        Assert.NotNull(schema["properties"]?["triggers"]);
        Assert.NotNull(schema["properties"]?["transformations"]);
        Assert.NotNull(schema["$defs"]?["TriggerNode"]);
        Assert.NotNull(schema["$defs"]?["TransformationNode"]);
    }

    [Fact]
    public void GenerateSchema_WithTriggerAndTransformation_CreatesOneOfArrays()
    {
        var triggerSchema = """{"type":"object","properties":{"interval":{"type":"integer"}}}""";
        var transformSchema = """{"type":"object","properties":{"path":{"type":"string"}}}""";

        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>
        {
            new("Polling", 1, "Trigger", true, false, triggerSchema),
            new("Select", 1, "Transform", false, false, transformSchema),
        });

        var generator = CreateGenerator();
        var schema = JObject.Parse(generator.GenerateSchema());

        var triggerNode = schema["$defs"]?["TriggerNode"];
        Assert.NotNull(triggerNode?["oneOf"]);
        Assert.Single(triggerNode!["oneOf"]!);

        var transformationNode = schema["$defs"]?["TransformationNode"];
        Assert.NotNull(transformationNode?["oneOf"]);
        Assert.Single(transformationNode!["oneOf"]!);
    }

    [Fact]
    public void GenerateSchema_InjectsTypeDiscriminator()
    {
        var nodeSchema = """{"type":"object","properties":{"path":{"type":"string"}}}""";

        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>
        {
            new("Select", 2, "Transform", false, false, nodeSchema),
        });

        var generator = CreateGenerator();
        var schema = JObject.Parse(generator.GenerateSchema());

        var nodeOneOf = schema["$defs"]?["TransformationNode"]?["oneOf"]?[0];
        Assert.NotNull(nodeOneOf);

        // Check type property is injected
        var typeProp = nodeOneOf["properties"]?["type"];
        Assert.Equal("string", typeProp?["type"]?.Value<string>());
        Assert.Equal("Select@2", typeProp?["const"]?.Value<string>());

        // Check "type" is in required array
        var required = nodeOneOf["required"] as JArray;
        Assert.NotNull(required);
        Assert.Contains("type", required.Select(r => r.Value<string>()));
    }

    [Fact]
    public void GenerateSchema_ChildCapableNode_AddsTransformationsProperty()
    {
        var nodeSchema = """{"type":"object","properties":{"condition":{"type":"string"}}}""";

        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>
        {
            new("If", 1, "Control", false, true, nodeSchema),
        });

        var generator = CreateGenerator();
        var schema = JObject.Parse(generator.GenerateSchema());

        var nodeOneOf = schema["$defs"]?["TransformationNode"]?["oneOf"]?[0];
        var transformationsProp = nodeOneOf?["properties"]?["transformations"];
        Assert.NotNull(transformationsProp);
        Assert.Equal("array", transformationsProp["type"]?.Value<string>());
        Assert.Equal("#/$defs/TransformationNode", transformationsProp["items"]?["$ref"]?.Value<string>());
    }

    [Fact]
    public void GenerateSchema_HoistsNestedDefs()
    {
        var nodeSchema = """
        {
            "type":"object",
            "properties":{
                "items":{"$ref":"#/$defs/SubItem"}
            },
            "$defs":{
                "SubItem":{"type":"object","properties":{"name":{"type":"string"}}}
            }
        }
        """;

        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>
        {
            new("Custom", 1, "Transform", false, false, nodeSchema),
        });

        var generator = CreateGenerator();
        var schema = JObject.Parse(generator.GenerateSchema());

        // Nested def should be hoisted to root $defs with namespaced key
        Assert.NotNull(schema["$defs"]?["Custom@1_SubItem"]);

        // The node schema should NOT have its own $defs anymore
        var nodeOneOf = schema["$defs"]?["TransformationNode"]?["oneOf"]?[0];
        Assert.Null(nodeOneOf?["$defs"]);

        // The $ref should be rewritten to point to the hoisted def
        var itemsRef = nodeOneOf?["properties"]?["items"]?["$ref"]?.Value<string>();
        Assert.Equal("#/$defs/Custom@1_SubItem", itemsRef);
    }

    [Fact]
    public void GenerateSchema_MultipleTriggers_AllIncludedInOneOf()
    {
        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>
        {
            new("Polling", 1, "Trigger", true, false, """{"type":"object","properties":{}}"""),
            new("Event", 1, "Trigger", true, false, """{"type":"object","properties":{}}"""),
            new("Cron", 2, "Trigger", true, false, """{"type":"object","properties":{}}"""),
        });

        var generator = CreateGenerator();
        var schema = JObject.Parse(generator.GenerateSchema());

        var triggerOneOf = schema["$defs"]?["TriggerNode"]?["oneOf"] as JArray;
        Assert.NotNull(triggerOneOf);
        Assert.Equal(3, triggerOneOf.Count);
    }

    [Fact]
    public void GenerateSchema_IsCached()
    {
        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>());

        var generator = CreateGenerator();
        var first = generator.GenerateSchema();
        var second = generator.GenerateSchema();

        Assert.Same(first, second);
        A.CallTo(() => _registry.GetAllDescriptors()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GenerateSchema_InvalidSchemaJson_FallsBackToEmptyObject()
    {
        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>
        {
            new("Bad", 1, "Transform", false, false, "not-json"),
        });

        var generator = CreateGenerator();
        var schema = JObject.Parse(generator.GenerateSchema());

        var nodeOneOf = schema["$defs"]?["TransformationNode"]?["oneOf"]?[0];
        Assert.NotNull(nodeOneOf);
        Assert.Equal("object", nodeOneOf["type"]?.Value<string>());
        Assert.Equal("Bad@1", nodeOneOf["properties"]?["type"]?["const"]?.Value<string>());
    }

    [Fact]
    public void GenerateSchema_RemovesTopLevelSchemaProperties()
    {
        var nodeSchema = """{"$schema":"https://json-schema.org/draft/2020-12/schema","$id":"test","type":"object","properties":{}}""";

        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>
        {
            new("Clean", 1, "Transform", false, false, nodeSchema),
        });

        var generator = CreateGenerator();
        var schema = JObject.Parse(generator.GenerateSchema());

        var nodeOneOf = schema["$defs"]?["TransformationNode"]?["oneOf"]?[0];
        Assert.Null(nodeOneOf?["$schema"]);
        Assert.Null(nodeOneOf?["$id"]);
    }

    [Fact]
    public void GenerateSchema_PreservesExistingRequired()
    {
        var nodeSchema = """{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}""";

        A.CallTo(() => _registry.GetAllDescriptors()).Returns(new List<NodeDescriptor>
        {
            new("Select", 1, "Transform", false, false, nodeSchema),
        });

        var generator = CreateGenerator();
        var schema = JObject.Parse(generator.GenerateSchema());

        var nodeOneOf = schema["$defs"]?["TransformationNode"]?["oneOf"]?[0];
        var required = nodeOneOf?["required"] as JArray;
        Assert.NotNull(required);
        Assert.Contains("path", required.Select(r => r.Value<string>()));
        Assert.Contains("type", required.Select(r => r.Value<string>()));
    }
}
