using System.Collections.Concurrent;
using System.Reflection;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using NJsonSchema;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// Registry that lazily generates and caches JSON schemas for all registered pipeline node types.
/// Schemas are generated once per process lifetime using NJsonSchema.
/// </summary>
internal class NodeSchemaRegistry : INodeSchemaRegistry
{
    private readonly ConcurrentDictionary<string, NodeDescriptor> _descriptors = new();
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

    private static NodeDescriptor BuildDescriptor(string qualifiedName, Type configType, Type? nodeType)
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
            var schema = JsonSchema.FromType(configType);
            schemaJson = schema.ToJson();
        }
        catch (Exception)
        {
            // If schema generation fails, provide a minimal schema
            schemaJson = "{}";
        }

        return new NodeDescriptor(nodeName, version, category, isTrigger, supportsChildren, schemaJson);
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
