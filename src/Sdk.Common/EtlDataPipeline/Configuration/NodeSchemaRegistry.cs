using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// Registry that lazily generates and caches JSON schemas for all registered pipeline node types.
/// Schemas are generated once per process lifetime using NJsonSchema.
/// </summary>
internal class NodeSchemaRegistry : INodeSchemaRegistry
{
    private readonly ConcurrentDictionary<string, NodeDescriptor> _descriptors = new();
    private readonly ConcurrentDictionary<string, XDocument?> _xmlDocsCache = new();
    private readonly List<NodeLookup> _nodeLookups;
    private readonly Dictionary<string, Type> _nodeConfigurations;
    private volatile bool _initialized;
    private readonly object _initLock = new();

    public NodeSchemaRegistry(List<NodeLookup> nodeLookups, Dictionary<string, Type> nodeConfigurations)
    {
        _nodeLookups = nodeLookups;
        _nodeConfigurations = nodeConfigurations;
    }

    /// <inheritdoc />
    public IReadOnlyList<NodeDescriptor> GetAllDescriptors()
    {
        EnsureInitialized();
        return _descriptors.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public NodeDescriptor? GetDescriptor(string qualifiedName)
    {
        EnsureInitialized();
        return _descriptors.TryGetValue(qualifiedName, out var descriptor) ? descriptor : null;
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            // Process node lookups (nodes registered with both node type and configuration type)
            foreach (var lookup in _nodeLookups)
            {
                if (_descriptors.ContainsKey(lookup.QualifiedName)) continue;

                var descriptor = BuildDescriptor(lookup.QualifiedName, lookup.NodeConfigurationType, lookup.NodeType);
                _descriptors.TryAdd(lookup.QualifiedName, descriptor);
            }

            // Process standalone configurations (registered via RegisterNodeConfiguration without a node type)
            foreach (var kvp in _nodeConfigurations)
            {
                if (_descriptors.ContainsKey(kvp.Key)) continue;

                var descriptor = BuildDescriptor(kvp.Key, kvp.Value, nodeType: null);
                _descriptors.TryAdd(kvp.Key, descriptor);
            }

            _initialized = true;
        }
    }

    private NodeDescriptor BuildDescriptor(string qualifiedName, Type configType, Type? nodeType)
    {
        var nodeNameAttr = configType.GetCustomAttribute<NodeNameAttribute>();
        var nodeName = nodeNameAttr?.Name ?? qualifiedName;
        var version = nodeNameAttr?.Version ?? 1;

        var isTrigger = typeof(ITriggerNodeConfiguration).IsAssignableFrom(configType);
        var supportsChildren = typeof(IChildNodeConfiguration).IsAssignableFrom(configType);

        var category = DeriveCategory(configType, nodeType, isTrigger);

        string schemaJson;
        try
        {
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true,
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                }
            };
            var schema = JsonSchema.FromType(configType, settings);
            schemaJson = ConvertEnumsToConstantCase(schema.ToJson());
            schemaJson = RemoveAdditionalPropertiesConstraint(schemaJson);
            schemaJson = SimplifyJsonConverterTypes(schemaJson, configType);
            schemaJson = InjectXmlDescriptions(schemaJson, configType);
        }
        catch (Exception)
        {
            // If schema generation fails, provide a minimal schema
            schemaJson = "{}";
        }

        return new NodeDescriptor(nodeName, version, category, isTrigger, supportsChildren, schemaJson);
    }

    /// <summary>
    /// Injects description fields from C# XML documentation into the JSON schema.
    /// </summary>
    private string InjectXmlDescriptions(string schemaJson, Type configType)
    {
        var root = JObject.Parse(schemaJson);

        // Add type-level description
        var typeXmlDoc = LoadXmlDocumentation(configType.Assembly);
        var typeSummary = GetMemberSummary(typeXmlDoc, $"T:{configType.FullName}");
        if (typeSummary != null)
        {
            root["description"] = typeSummary;
        }

        // Add property-level descriptions
        if (root["properties"] is JObject props)
        {
            foreach (var prop in configType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var declaringType = prop.DeclaringType ?? configType;
                var propXmlDoc = LoadXmlDocumentation(declaringType.Assembly);
                var summary = GetMemberSummary(propXmlDoc, $"P:{declaringType.FullName}.{prop.Name}");
                if (summary == null) continue;

                var camelName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                if (props[camelName] is JObject propSchema)
                {
                    propSchema["description"] = summary;
                }
            }
        }

        return root.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>
    /// Loads and caches XML documentation for an assembly, searching next to the DLL and in the NuGet cache.
    /// </summary>
    private XDocument? LoadXmlDocumentation(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        if (assemblyName == null) return null;

        return _xmlDocsCache.GetOrAdd(assemblyName, _ =>
        {
            try
            {
                // Try next to the assembly DLL
                var location = assembly.Location;
                if (!string.IsNullOrEmpty(location))
                {
                    var xmlPath = Path.ChangeExtension(location, ".xml");
                    if (File.Exists(xmlPath))
                        return XDocument.Load(xmlPath);
                }

                // Try NuGet global packages cache
                var nugetFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".nuget", "packages");

                var packageDir = Path.Combine(nugetFolder, assemblyName.ToLowerInvariant());
                if (Directory.Exists(packageDir))
                {
                    foreach (var xmlFile in Directory.EnumerateFiles(
                                 packageDir, assemblyName + ".xml", SearchOption.AllDirectories))
                    {
                        return XDocument.Load(xmlFile);
                    }
                }

                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Extracts the summary text for a given member from an XML documentation file.
    /// </summary>
    private static string? GetMemberSummary(XDocument? xmlDoc, string memberName)
    {
        if (xmlDoc == null) return null;

        var member = xmlDoc.Descendants("member")
            .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

        var summary = member?.Element("summary")?.Value;
        if (string.IsNullOrWhiteSpace(summary)) return null;

        // Normalize whitespace (XML docs often have leading/trailing whitespace and newlines)
        return string.Join(" ", summary!.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Replaces complex object schemas with "type": "string" for properties whose C# types
    /// have a [JsonConverter] attribute that serializes them as strings.
    /// </summary>
    private static string SimplifyJsonConverterTypes(string schemaJson, Type configType)
    {
        var root = JObject.Parse(schemaJson);

        if (root["properties"] is not JObject props) return schemaJson;

        foreach (var prop in configType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

            var camelName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
            if (props[camelName] is not JObject propSchema) continue;

            // Check if it's a collection of a type with JsonConverter (on type or property)
            if (underlyingType.IsGenericType &&
                typeof(System.Collections.IEnumerable).IsAssignableFrom(underlyingType))
            {
                var elementType = underlyingType.GetGenericArguments().FirstOrDefault();
                if (elementType != null &&
                    (HasJsonConverterForType(elementType) || prop.GetCustomAttribute<JsonConverterAttribute>() != null))
                {
                    // Replace array items with string type
                    if (propSchema["items"] is JObject items)
                    {
                        items.RemoveAll();
                        items["type"] = "string";
                    }

                    continue;
                }
            }

            // Check if the type itself or the property has a JsonConverter
            if (!HasJsonConverterForType(underlyingType) &&
                prop.GetCustomAttribute<JsonConverterAttribute>() == null) continue;

            var isNullable = Nullable.GetUnderlyingType(propType) != null || !propType.IsValueType;
            var desc = propSchema["description"]?.Value<string>();

            propSchema.RemoveAll();
            propSchema["type"] = isNullable
                ? new JArray("null", "string")
                : JToken.FromObject("string");

            if (desc != null)
            {
                propSchema["description"] = desc;
            }
        }

        return root.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static readonly ConcurrentDictionary<Type, bool> ConverterCache = new();

    private static bool HasJsonConverterForType(Type type)
    {
        return ConverterCache.GetOrAdd(type, t =>
        {
            // Direct [JsonConverter] attribute on the type
            if (t.GetCustomAttribute<JsonConverterAttribute>() != null)
                return true;

            // Scan the type's assembly for a JsonConverter<T> implementation
            try
            {
                var targetConverterBase = typeof(JsonConverter<>).MakeGenericType(t);
                foreach (var candidate in t.Assembly.GetExportedTypes())
                {
                    if (!candidate.IsAbstract && !candidate.IsInterface &&
                        targetConverterBase.IsAssignableFrom(candidate))
                        return true;
                }
            }
            catch
            {
                // Ignore reflection failures
            }

            return false;
        });
    }

    /// <summary>
    /// Removes additionalProperties: false constraints from the schema.
    /// NJsonSchema generates these by default, but pipeline definitions may contain extra properties
    /// and the runtime deserializer is lenient.
    /// </summary>
    private static string RemoveAdditionalPropertiesConstraint(string schemaJson)
    {
        var root = JObject.Parse(schemaJson);
        RemoveAdditionalPropertiesRecursive(root);
        return root.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static void RemoveAdditionalPropertiesRecursive(JToken token)
    {
        if (token is JObject obj)
        {
            obj.Remove("additionalProperties");
            foreach (var property in obj.Properties().ToList())
            {
                RemoveAdditionalPropertiesRecursive(property.Value);
            }
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr)
            {
                RemoveAdditionalPropertiesRecursive(item);
            }
        }
    }

    /// <summary>
    /// Converts enum schemas to CONSTANT_CASE string enums using x-enumNames.
    /// Handles both integer-based and string-based enums from NJsonSchema,
    /// deduplicates aliases (preferring the longer name), and converts to UPPER_SNAKE_CASE.
    /// </summary>
    private static string ConvertEnumsToConstantCase(string schemaJson)
    {
        var root = JObject.Parse(schemaJson);
        ConvertEnumsRecursive(root);
        return root.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static void ConvertEnumsRecursive(JToken token)
    {
        if (token is JObject obj)
        {
            if (obj["x-enumNames"] is JArray enumNames && obj["enum"] is JArray enumValues)
            {
                // Pair x-enumNames with enum values to detect aliases (same value, different names)
                var names = enumNames.Select(n => n.Value<string>()!).ToList();
                var values = enumValues.Select(v => v.ToString()).ToList();

                // Keep all alias names (e.g. both "Int" and "Integer") for backward compatibility
                var distinctNames = names.Distinct().ToList();

                // Build combined enum: PascalCase + CONSTANT_CASE (deduplicated)
                var allValues = distinctNames
                    .SelectMany(n => new[] { n, PascalToConstantCase(n) })
                    .Distinct()
                    .ToList();

                // Ensure type is "string"
                var typeToken = obj["type"];
                if (typeToken is JValue typeVal)
                {
                    if (typeVal.Value<string>() == "integer")
                        obj["type"] = "string";
                }
                else if (typeToken is JArray typeArr)
                {
                    for (var i = 0; i < typeArr.Count; i++)
                    {
                        if (typeArr[i].Value<string>() == "integer")
                            typeArr[i] = "string";
                    }
                }

                obj["enum"] = new JArray(allValues.Select(n => new JValue(n)));

                // Remove x-enumNames as it's no longer in sync after alias dedup and CONSTANT_CASE conversion
                obj.Remove("x-enumNames");
            }

            foreach (var property in obj.Properties().ToList())
            {
                ConvertEnumsRecursive(property.Value);
            }
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr)
            {
                ConvertEnumsRecursive(item);
            }
        }
    }

    /// <summary>
    /// Converts a PascalCase string to UPPER_SNAKE_CASE (CONSTANT_CASE).
    /// Examples: Insert → INSERT, NotEquals → NOT_EQUALS, LessEqualsThan → LESS_EQUALS_THAN
    /// </summary>
    private static string PascalToConstantCase(string pascalCase)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < pascalCase.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascalCase[i]) && char.IsLower(pascalCase[i - 1]))
                sb.Append('_');
            sb.Append(char.ToUpperInvariant(pascalCase[i]));
        }

        return sb.ToString();
    }

    private static string DeriveCategory(Type configType, Type? nodeType, bool isTrigger)
    {
        if (isTrigger) return "Trigger";

        // Try to derive from the node type's namespace first (more specific)
        var ns = nodeType?.Namespace ?? configType.Namespace ?? string.Empty;

        if (ns.Contains(".Triggers") || ns.Contains(".Trigger")) return "Trigger";
        if (ns.Contains(".Transforms") || ns.Contains(".Transform")) return "Transform";
        if (ns.Contains(".Loads") || ns.Contains(".Load")) return "Load";
        if (ns.Contains(".Extracts") || ns.Contains(".Extract")) return "Extract";
        if (ns.Contains(".Control")) return "Control";
        if (ns.Contains(".Buffering")) return "Buffering";

        return "Other";
    }
}
