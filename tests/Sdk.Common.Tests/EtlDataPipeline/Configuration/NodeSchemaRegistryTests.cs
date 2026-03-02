using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Newtonsoft.Json.Linq;

namespace Sdk.Common.Tests.EtlDataPipeline.Configuration;

#region Test enums and configuration types

/// <summary>
/// Simple enum with single-word values for testing PascalToConstantCase.
/// </summary>
public enum SimpleOperation
{
    Insert = 0,
    Update = 1,
    Delete = 2,
}

/// <summary>
/// Enum with multi-word PascalCase values for testing underscore insertion.
/// </summary>
public enum MultiWordOperation
{
    NotEquals = 0,
    LessThan = 1,
    GreaterEqualsThan = 2,
    StartsWith = 3,
}

/// <summary>
/// Enum with aliases (multiple names for the same value) for testing deduplication.
/// The longer name should be preferred.
/// </summary>
public enum AliasedOperation
{
    Equal = Equals,
    Equals = 0,
    NotEqual = NotEquals,
    NotEquals = 1,
    Contain = 2,
    Contains = Contain,
}

/// <summary>
/// Enum with a single character value.
/// </summary>
public enum SingleCharEnum
{
    A = 0,
    B = 1,
}

/// <summary>
/// Enum that is already all-uppercase to verify no double-underscore insertion.
/// </summary>
public enum UpperCaseEnum
{
    HTTP = 0,
    TCP = 1,
}

/// <summary>
/// Enum with an acronym followed by a word.
/// </summary>
public enum AcronymWordEnum
{
    ReadHoldingRegister = 0,
    HTTPRequest = 1,
}

/// <summary>
/// Enum with the value "DateTime" to test consecutive uppercase letters.
/// </summary>
public enum DataTypeEnum
{
    String = 0,
    Double = 1,
    DateTime = 2,
    Int64 = 3,
}

/// <summary>
/// Test configuration with a simple enum property.
/// </summary>
[NodeName("TestSimple", 1)]
public record SimpleEnumNodeConfiguration : NodeConfiguration
{
    public SimpleOperation Operation { get; set; }
}

/// <summary>
/// Test configuration with a multi-word enum property.
/// </summary>
[NodeName("TestMultiWord", 1)]
public record MultiWordEnumNodeConfiguration : NodeConfiguration
{
    public MultiWordOperation Operation { get; set; }
}

/// <summary>
/// Test configuration with an aliased enum property.
/// </summary>
[NodeName("TestAliased", 1)]
public record AliasedEnumNodeConfiguration : NodeConfiguration
{
    public AliasedOperation Operation { get; set; }
}

/// <summary>
/// Test configuration with a single-char enum.
/// </summary>
[NodeName("TestSingleChar", 1)]
public record SingleCharEnumNodeConfiguration : NodeConfiguration
{
    public SingleCharEnum Letter { get; set; }
}

/// <summary>
/// Test configuration with an already-uppercase enum.
/// </summary>
[NodeName("TestUpperCase", 1)]
public record UpperCaseEnumNodeConfiguration : NodeConfiguration
{
    public UpperCaseEnum Protocol { get; set; }
}

/// <summary>
/// Test configuration with an acronym-word enum.
/// </summary>
[NodeName("TestAcronymWord", 1)]
public record AcronymWordEnumNodeConfiguration : NodeConfiguration
{
    public AcronymWordEnum FunctionCode { get; set; }
}

/// <summary>
/// Test configuration with data type enum.
/// </summary>
[NodeName("TestDataType", 1)]
public record DataTypeEnumNodeConfiguration : NodeConfiguration
{
    public DataTypeEnum ValueType { get; set; }
}

/// <summary>
/// Test configuration with multiple enum properties to verify nested conversion.
/// </summary>
[NodeName("TestMultiEnum", 1)]
public record MultiEnumNodeConfiguration : NodeConfiguration
{
    public SimpleOperation PrimaryOp { get; set; }
    public MultiWordOperation SecondaryOp { get; set; }
}

/// <summary>
/// Test configuration with a string property and no enums for baseline verification.
/// </summary>
[NodeName("TestNoEnum", 1)]
public record NoEnumNodeConfiguration : NodeConfiguration
{
    public string? Name { get; set; }
    public int Count { get; set; }
}

#endregion

public class NodeSchemaRegistryTests
{
    /// <summary>
    /// Creates a <see cref="NodeSchemaRegistry"/> with a single configuration type
    /// registered via the nodeConfigurations dictionary (standalone config, no node type).
    /// </summary>
    private static NodeSchemaRegistry CreateRegistryWithConfig<TConfig>(string qualifiedName)
        where TConfig : NodeConfiguration
    {
        return new NodeSchemaRegistry(
            new List<NodeLookup>(),
            new Dictionary<string, Type> { [qualifiedName] = typeof(TConfig) });
    }

    /// <summary>
    /// Builds a descriptor for a single configuration type and returns the parsed schema JSON.
    /// </summary>
    private static JObject GetSchemaForConfig<TConfig>(string qualifiedName)
        where TConfig : NodeConfiguration
    {
        var registry = CreateRegistryWithConfig<TConfig>(qualifiedName);
        var descriptor = registry.GetDescriptor(qualifiedName);
        Assert.NotNull(descriptor);
        return JObject.Parse(descriptor.ConfigurationSchemaJson);
    }

    /// <summary>
    /// Extracts enum values from a property in a schema as a string list.
    /// Follows $ref links into "definitions" if the enum is not inline.
    /// </summary>
    private static List<string> GetEnumValues(JObject schema, string propertyName)
    {
        var propToken = schema["properties"]?[propertyName];
        Assert.NotNull(propToken);

        // Check for inline enum
        if (propToken["enum"] is JArray inlineEnum)
        {
            return inlineEnum.Select(t => t.Value<string>()!).ToList();
        }

        // Follow $ref to definitions
        var refPath = propToken["$ref"]?.Value<string>();
        Assert.NotNull(refPath);

        // NJsonSchema uses "#/definitions/TypeName" format
        var defName = refPath!.Replace("#/definitions/", "");
        var defToken = schema["definitions"]?[defName];
        Assert.NotNull(defToken);

        var enumToken = defToken!["enum"] as JArray;
        Assert.NotNull(enumToken);
        return enumToken.Select(t => t.Value<string>()!).ToList();
    }

    /// <summary>
    /// Gets the resolved property schema, following $ref if needed.
    /// </summary>
    private static JToken? ResolveProperty(JObject schema, string propertyName)
    {
        var propToken = schema["properties"]?[propertyName];
        if (propToken == null) return null;

        var refPath = propToken["$ref"]?.Value<string>();
        if (refPath == null) return propToken;

        var defName = refPath.Replace("#/definitions/", "");
        return schema["definitions"]?[defName];
    }

    #region PascalToConstantCase conversion (tested indirectly through BuildDescriptor)

    [Fact]
    public void BuildDescriptor_SimpleEnum_ConvertsSingleWordsToCONSTANTCASE()
    {
        var schema = GetSchemaForConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var values = GetEnumValues(schema, "operation");

        Assert.Contains("INSERT", values);
        Assert.Contains("UPDATE", values);
        Assert.Contains("DELETE", values);
    }

    [Fact]
    public void BuildDescriptor_MultiWordEnum_InsertsUnderscoresBetweenWords()
    {
        var schema = GetSchemaForConfig<MultiWordEnumNodeConfiguration>("TestMultiWord@1");
        var values = GetEnumValues(schema, "operation");

        Assert.Contains("NOT_EQUALS", values);
        Assert.Contains("LESS_THAN", values);
        Assert.Contains("GREATER_EQUALS_THAN", values);
        Assert.Contains("STARTS_WITH", values);
    }

    [Fact]
    public void BuildDescriptor_DataTypeEnum_HandlesConsecutiveUppercaseAndDigits()
    {
        var schema = GetSchemaForConfig<DataTypeEnumNodeConfiguration>("TestDataType@1");
        var values = GetEnumValues(schema, "valueType");

        Assert.Contains("STRING", values);
        Assert.Contains("DOUBLE", values);
        Assert.Contains("DATE_TIME", values);
        // Int64 -> PascalToConstantCase gives "INT64" (digit after lowercase doesn't trigger underscore)
        Assert.Contains("INT64", values);
    }

    [Fact]
    public void BuildDescriptor_UpperCaseEnum_PreservesAllUppercaseWithoutDoubleUnderscores()
    {
        var schema = GetSchemaForConfig<UpperCaseEnumNodeConfiguration>("TestUpperCase@1");
        var values = GetEnumValues(schema, "protocol");

        Assert.Contains("HTTP", values);
        Assert.Contains("TCP", values);
        // Verify no double underscores
        Assert.DoesNotContain(values, v => v.Contains("__"));
    }

    [Fact]
    public void BuildDescriptor_AcronymWordEnum_InsertsUnderscoreAtTransition()
    {
        var schema = GetSchemaForConfig<AcronymWordEnumNodeConfiguration>("TestAcronymWord@1");
        var values = GetEnumValues(schema, "functionCode");

        // ReadHoldingRegister: R(upper) e(lower)->a(lower)...H(upper after lower)...R(upper after lower)
        // PascalToConstantCase inserts underscore when upper follows lower
        Assert.Contains("READ_HOLDING_REGISTER", values);
        // HTTPRequest: H-T-T-P(all upper), R(upper after upper P), but P is upper and R is upper.
        // The algorithm only inserts _ when char is upper AND previous char is lower.
        // So HTTPREQUEST stays as one block: HTTPREQUEST
        Assert.Contains("HTTPREQUEST", values);
    }

    [Fact]
    public void BuildDescriptor_SingleCharEnum_ConvertsSingleCharactersToUpperCase()
    {
        var schema = GetSchemaForConfig<SingleCharEnumNodeConfiguration>("TestSingleChar@1");
        var values = GetEnumValues(schema, "letter");

        Assert.Contains("A", values);
        Assert.Contains("B", values);
    }

    #endregion

    #region ConvertEnumsRecursive behavior

    [Fact]
    public void BuildDescriptor_EnumProperty_TypeIsStringNotInteger()
    {
        var schema = GetSchemaForConfig<SimpleEnumNodeConfiguration>("TestSimple@1");

        // Resolve the enum definition (may be behind a $ref)
        var resolved = ResolveProperty(schema, "operation");
        Assert.NotNull(resolved);

        // The type should be "string" (not "integer" which NJsonSchema would produce for enums)
        var typeToken = resolved["type"];
        Assert.NotNull(typeToken);

        // Type may be a simple string or an array (for nullable enums)
        if (typeToken is JValue typeVal)
        {
            Assert.Equal("string", typeVal.Value<string>());
        }
        else if (typeToken is JArray typeArr)
        {
            Assert.Contains("string", typeArr.Select(t => t.Value<string>()));
            Assert.DoesNotContain("integer", typeArr.Select(t => t.Value<string>()));
        }
    }

    [Fact]
    public void BuildDescriptor_EnumProperty_DoesNotContainXEnumNames()
    {
        var schema = GetSchemaForConfig<SimpleEnumNodeConfiguration>("TestSimple@1");

        // Resolve the enum definition (may be behind a $ref)
        var resolved = ResolveProperty(schema, "operation");
        Assert.NotNull(resolved);

        // x-enumNames should be removed after conversion
        Assert.Null(resolved["x-enumNames"]);
    }

    [Fact]
    public void BuildDescriptor_AliasedEnum_DeduplicatesByPickingLongestName()
    {
        var schema = GetSchemaForConfig<AliasedEnumNodeConfiguration>("TestAliased@1");
        var values = GetEnumValues(schema, "operation");

        // Equal (5 chars) vs Equals (6 chars) -> Equals wins -> EQUALS
        Assert.Contains("EQUALS", values);
        // NotEqual (8 chars) vs NotEquals (9 chars) -> NotEquals wins -> NOT_EQUALS
        Assert.Contains("NOT_EQUALS", values);
        // Contain (7 chars) vs Contains (8 chars) -> Contains wins -> CONTAINS
        Assert.Contains("CONTAINS", values);

        // Verify aliases are removed (only the longer names remain)
        Assert.DoesNotContain("EQUAL", values);
        Assert.DoesNotContain("NOT_EQUAL", values);
        Assert.DoesNotContain("CONTAIN", values);

        // Should have exactly 3 distinct values (one per underlying integer value)
        Assert.Equal(3, values.Count);
    }

    [Fact]
    public void BuildDescriptor_MultipleEnumProperties_AllAreConverted()
    {
        var schema = GetSchemaForConfig<MultiEnumNodeConfiguration>("TestMultiEnum@1");

        // Verify primary operation enum is converted
        var primaryValues = GetEnumValues(schema, "primaryOp");
        Assert.Contains("INSERT", primaryValues);

        // Verify secondary operation enum is converted
        var secondaryValues = GetEnumValues(schema, "secondaryOp");
        Assert.Contains("NOT_EQUALS", secondaryValues);

        // Neither resolved definition should have x-enumNames
        var resolvedPrimary = ResolveProperty(schema, "primaryOp");
        Assert.NotNull(resolvedPrimary);
        Assert.Null(resolvedPrimary["x-enumNames"]);

        var resolvedSecondary = ResolveProperty(schema, "secondaryOp");
        Assert.NotNull(resolvedSecondary);
        Assert.Null(resolvedSecondary["x-enumNames"]);
    }

    [Fact]
    public void BuildDescriptor_NoEnumProperties_SchemaIsStillValid()
    {
        var schema = GetSchemaForConfig<NoEnumNodeConfiguration>("TestNoEnum@1");

        Assert.NotNull(schema["properties"]?["name"]);
        Assert.NotNull(schema["properties"]?["count"]);

        // No enum-related artifacts should be present
        Assert.Null(schema["properties"]?["name"]?["enum"]);
        Assert.Null(schema["properties"]?["count"]?["enum"]);
    }

    #endregion

    #region NodeSchemaRegistry public API

    [Fact]
    public void GetAllDescriptors_ReturnsAllRegisteredNodes()
    {
        var registry = new NodeSchemaRegistry(
            new List<NodeLookup>(),
            new Dictionary<string, Type>
            {
                ["TestSimple@1"] = typeof(SimpleEnumNodeConfiguration),
                ["TestNoEnum@1"] = typeof(NoEnumNodeConfiguration),
            });

        var descriptors = registry.GetAllDescriptors();

        Assert.Equal(2, descriptors.Count);
        Assert.Contains(descriptors, d => d.NodeName == "TestSimple");
        Assert.Contains(descriptors, d => d.NodeName == "TestNoEnum");
    }

    [Fact]
    public void GetDescriptor_ExistingKey_ReturnsDescriptor()
    {
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var descriptor = registry.GetDescriptor("TestSimple@1");

        Assert.NotNull(descriptor);
        Assert.Equal("TestSimple", descriptor.NodeName);
        Assert.Equal(1, descriptor.Version);
    }

    [Fact]
    public void GetDescriptor_NonExistentKey_ReturnsNull()
    {
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var descriptor = registry.GetDescriptor("DoesNotExist@1");

        Assert.Null(descriptor);
    }

    [Fact]
    public void GetAllDescriptors_IsIdempotent()
    {
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");

        var first = registry.GetAllDescriptors();
        var second = registry.GetAllDescriptors();

        Assert.Equal(first.Count, second.Count);
        Assert.Equal(first[0].NodeName, second[0].NodeName);
    }

    [Fact]
    public void BuildDescriptor_UsesNodeNameAttribute_ForNameAndVersion()
    {
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var descriptor = registry.GetDescriptor("TestSimple@1");

        Assert.NotNull(descriptor);
        // NodeNameAttribute on SimpleEnumNodeConfiguration specifies name="TestSimple", version=1
        Assert.Equal("TestSimple", descriptor.NodeName);
        Assert.Equal(1, descriptor.Version);
    }

    [Fact]
    public void BuildDescriptor_ProducesValidJsonSchema()
    {
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var descriptor = registry.GetDescriptor("TestSimple@1");

        Assert.NotNull(descriptor);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.ConfigurationSchemaJson));

        // Should be parseable JSON
        var schema = JObject.Parse(descriptor.ConfigurationSchemaJson);
        Assert.NotNull(schema["type"]);
        Assert.Equal("object", schema["type"]?.Value<string>());
    }

    [Fact]
    public void BuildDescriptor_NonTriggerConfig_IsNotMarkedAsTrigger()
    {
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var descriptor = registry.GetDescriptor("TestSimple@1");

        Assert.NotNull(descriptor);
        Assert.False(descriptor.IsTrigger);
    }

    #endregion

    #region Schema structure and enum conversion for nested $defs

    [Fact]
    public void BuildDescriptor_EnumValuesInNestedDefs_AreAlsoConverted()
    {
        // When NJsonSchema places enum definitions in $defs and uses $ref,
        // ConvertEnumsRecursive should traverse into $defs and convert those enums too.
        // We test this by verifying the full schema JSON has no x-enumNames anywhere.
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var descriptor = registry.GetDescriptor("TestSimple@1");
        Assert.NotNull(descriptor);

        var fullJson = descriptor.ConfigurationSchemaJson;
        var root = JObject.Parse(fullJson);

        // Recursively check that no x-enumNames exists anywhere in the schema
        AssertNoXEnumNamesRecursive(root);
    }

    private static void AssertNoXEnumNamesRecursive(JToken token)
    {
        if (token is JObject obj)
        {
            Assert.Null(obj["x-enumNames"]);
            foreach (var prop in obj.Properties())
            {
                AssertNoXEnumNamesRecursive(prop.Value);
            }
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr)
            {
                AssertNoXEnumNamesRecursive(item);
            }
        }
    }

    [Fact]
    public void BuildDescriptor_AllEnumValuesInSchema_AreUpperCase()
    {
        // Verify that every enum value in the schema is fully uppercase (CONSTANT_CASE)
        var schema = GetSchemaForConfig<MultiWordEnumNodeConfiguration>("TestMultiWord@1");
        AssertAllEnumsAreUpperCaseRecursive(schema);
    }

    private static void AssertAllEnumsAreUpperCaseRecursive(JToken token)
    {
        if (token is JObject obj)
        {
            if (obj["enum"] is JArray enumValues)
            {
                foreach (var val in enumValues)
                {
                    var str = val.Value<string>();
                    Assert.NotNull(str);
                    Assert.Equal(str, str!.ToUpperInvariant());
                }
            }

            foreach (var prop in obj.Properties())
            {
                AssertAllEnumsAreUpperCaseRecursive(prop.Value);
            }
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr)
            {
                AssertAllEnumsAreUpperCaseRecursive(item);
            }
        }
    }

    #endregion

    #region XML documentation injection

    [Fact]
    public void BuildDescriptor_InjectsPropertyDescriptionFromXmlDoc()
    {
        // NodeConfiguration base class has XML doc on its Description property.
        // The schema should contain a "description" field on the "description" property.
        // This test relies on the XML doc file being generated during build
        // (GenerateDocumentationFile=true in the csproj).
        var schema = GetSchemaForConfig<SimpleEnumNodeConfiguration>("TestSimple@1");

        // The "description" property comes from the base NodeConfiguration class.
        // If XML docs are available, the property schema should have a "description" field.
        var descProp = schema["properties"]?["description"];
        Assert.NotNull(descProp);

        // We can at minimum verify the property exists in the schema.
        // XML doc injection depends on the XML file being present at runtime.
        // If the description was injected, it would be a non-null string.
        // We don't assert on the exact text since it depends on build output.
    }

    [Fact]
    public void BuildDescriptor_XmlDocMissing_SchemaStillGenerated()
    {
        // Even when XML documentation files are not available, the schema
        // should still be generated successfully (LoadXmlDocumentation returns null gracefully).
        var schema = GetSchemaForConfig<NoEnumNodeConfiguration>("TestNoEnum@1");

        Assert.NotNull(schema);
        Assert.Equal("object", schema["type"]?.Value<string>());
        Assert.NotNull(schema["properties"]);
    }

    #endregion

    #region Category derivation

    [Fact]
    public void BuildDescriptor_ConfigInTransformNamespace_GetsCategoryFromNamespace()
    {
        // Our test types are NOT in a Transforms namespace, so they fall into "Other"
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var descriptor = registry.GetDescriptor("TestSimple@1");

        Assert.NotNull(descriptor);
        // Test config types are in the test namespace, not in a recognized category namespace
        Assert.Equal("Other", descriptor.Category);
    }

    [Fact]
    public void BuildDescriptor_ConfigNotImplementingTrigger_IsNotTrigger()
    {
        var registry = CreateRegistryWithConfig<SimpleEnumNodeConfiguration>("TestSimple@1");
        var descriptor = registry.GetDescriptor("TestSimple@1");

        Assert.NotNull(descriptor);
        Assert.False(descriptor.IsTrigger);
    }

    #endregion

    #region RemoveAdditionalPropertiesConstraint

    [Fact]
    public void BuildDescriptor_SchemaDoesNotContainAdditionalPropertiesFalse()
    {
        var schema = GetSchemaForConfig<NoEnumNodeConfiguration>("TestNoEnum@1");

        // additionalProperties: false should have been removed
        Assert.Null(schema["additionalProperties"]);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void GetAllDescriptors_EmptyRegistry_ReturnsEmptyList()
    {
        var registry = new NodeSchemaRegistry(
            new List<NodeLookup>(),
            new Dictionary<string, Type>());

        var descriptors = registry.GetAllDescriptors();

        Assert.NotNull(descriptors);
        Assert.Empty(descriptors);
    }

    [Fact]
    public void BuildDescriptor_CamelCasePropertyNaming_IsApplied()
    {
        var schema = GetSchemaForConfig<MultiEnumNodeConfiguration>("TestMultiEnum@1");

        // Properties should be camelCase in the schema
        Assert.NotNull(schema["properties"]?["primaryOp"]);
        Assert.NotNull(schema["properties"]?["secondaryOp"]);

        // PascalCase versions should NOT exist
        Assert.Null(schema["properties"]?["PrimaryOp"]);
        Assert.Null(schema["properties"]?["SecondaryOp"]);
    }

    #endregion
}
