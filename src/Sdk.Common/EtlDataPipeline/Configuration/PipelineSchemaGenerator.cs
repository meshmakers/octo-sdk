using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

internal class PipelineSchemaGenerator : IPipelineSchemaGenerator
{
    private readonly INodeSchemaRegistry _registry;
    private string? _cachedSchema;
    private readonly object _lock = new();

    public PipelineSchemaGenerator(INodeSchemaRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public string GenerateSchema()
    {
        if (_cachedSchema != null) return _cachedSchema;

        lock (_lock)
        {
            if (_cachedSchema != null) return _cachedSchema;
            _cachedSchema = BuildSchema();
            return _cachedSchema;
        }
    }

    private string BuildSchema()
    {
        var descriptors = _registry.GetAllDescriptors();
        var triggers = new List<NodeDescriptor>();
        var transformations = new List<NodeDescriptor>();

        foreach (var descriptor in descriptors)
        {
            if (descriptor.IsTrigger)
                triggers.Add(descriptor);
            else
                transformations.Add(descriptor);
        }

        var rootDefs = new JObject();

        var triggerOneOf = new JArray();
        foreach (var trigger in triggers)
        {
            var nodeSchema = BuildNodeSchema(trigger, rootDefs, isTransformation: false);
            triggerOneOf.Add(nodeSchema);
        }

        var transformationOneOf = new JArray();
        foreach (var transformation in transformations)
        {
            var nodeSchema = BuildNodeSchema(transformation, rootDefs, isTransformation: true);
            transformationOneOf.Add(nodeSchema);
        }

        // Build the TriggerNode and TransformationNode $defs
        if (triggerOneOf.Count > 0)
        {
            rootDefs["TriggerNode"] = new JObject { ["oneOf"] = triggerOneOf };
        }
        else
        {
            rootDefs["TriggerNode"] = new JObject { ["type"] = "object" };
        }

        if (transformationOneOf.Count > 0)
        {
            rootDefs["TransformationNode"] = new JObject { ["oneOf"] = transformationOneOf };
        }
        else
        {
            rootDefs["TransformationNode"] = new JObject { ["type"] = "object" };
        }

        var rootSchema = new JObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["triggers"] = new JObject
                {
                    ["type"] = "array",
                    ["items"] = new JObject { ["$ref"] = "#/$defs/TriggerNode" }
                },
                ["transformations"] = new JObject
                {
                    ["type"] = "array",
                    ["items"] = new JObject { ["$ref"] = "#/$defs/TransformationNode" }
                }
            },
            ["$defs"] = rootDefs
        };

        return rootSchema.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static JObject BuildNodeSchema(NodeDescriptor descriptor, JObject rootDefs, bool isTransformation)
    {
        var qualifiedName = $"{descriptor.NodeName}@{descriptor.Version}";

        // Parse the configuration schema
        JObject nodeSchema;
        try
        {
            nodeSchema = JObject.Parse(descriptor.ConfigurationSchemaJson);
        }
        catch
        {
            nodeSchema = new JObject { ["type"] = "object" };
        }

        // Ensure the schema has a type and properties
        if (nodeSchema["type"] == null)
        {
            nodeSchema["type"] = "object";
        }

        var properties = nodeSchema["properties"] as JObject;
        if (properties == null)
        {
            properties = new JObject();
            nodeSchema["properties"] = properties;
        }

        // Inject the discriminator "type" property with a const value
        properties["type"] = new JObject
        {
            ["type"] = "string",
            ["const"] = qualifiedName
        };

        // Ensure "type" is in the required array
        var required = nodeSchema["required"] as JArray ?? new JArray();
        if (!required.Any(r => r.Value<string>() == "type"))
        {
            required.Add("type");
        }
        nodeSchema["required"] = required;

        // For child-capable transformation nodes, add/replace "transformations" property
        if (descriptor.SupportsChildren)
        {
            properties["transformations"] = new JObject
            {
                ["type"] = "array",
                ["items"] = new JObject { ["$ref"] = "#/$defs/TransformationNode" }
            };
        }

        // Hoist nested $defs/definitions to root level with namespaced keys
        HoistNestedDefs(nodeSchema, rootDefs, qualifiedName, "$defs");
        HoistNestedDefs(nodeSchema, rootDefs, qualifiedName, "definitions");

        // Remove top-level schema properties that don't belong in a sub-schema
        nodeSchema.Remove("$schema");
        nodeSchema.Remove("$id");

        return nodeSchema;
    }

    private static void HoistNestedDefs(JObject nodeSchema, JObject rootDefs, string prefix, string defsKey)
    {
        if (nodeSchema[defsKey] is not JObject nestedDefs) return;

        foreach (var kvp in nestedDefs)
        {
            var namespacedKey = $"{prefix}_{kvp.Key}";
            if (kvp.Value != null)
            {
                rootDefs[namespacedKey] = kvp.Value.DeepClone();
            }
        }

        // Rewrite internal $ref pointers from #/$defs/X or #/definitions/X to #/$defs/Prefix_X
        RewriteRefs(nodeSchema, $"#/{defsKey}/", "#/$defs/", prefix);

        nodeSchema.Remove(defsKey);
    }

    private static void RewriteRefs(JToken token, string oldRefPrefix, string newRefBase, string prefix)
    {
        switch (token)
        {
            case JObject obj:
            {
                if (obj["$ref"] is JValue refVal && refVal.Value is string refStr &&
                    refStr.StartsWith(oldRefPrefix))
                {
                    var defName = refStr.Substring(oldRefPrefix.Length);
                    obj["$ref"] = $"{newRefBase}{prefix}_{defName}";
                }

                foreach (var property in obj.Properties().ToList())
                {
                    RewriteRefs(property.Value, oldRefPrefix, newRefBase, prefix);
                }
                break;
            }
            case JArray arr:
            {
                foreach (var item in arr)
                {
                    RewriteRefs(item, oldRefPrefix, newRefBase, prefix);
                }
                break;
            }
        }
    }
}
