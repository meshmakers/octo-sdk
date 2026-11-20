using System.Text;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.DependencyGraph;

namespace Meshmakers.Octo.Sdk.SourceGeneration;

internal static class AttributeCodeGenerator
{
    private const string DtoNamespaceMarker = ".DataTransferObjects.";

    private const string MutationJsonIgnore = "  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]";

    /// <summary>
    ///     AB#9493: the base class of a type DTO. A base type of the same model, and <c>System/Entity</c> (its DTO
    ///     <c>RtEntityDto</c> lives in <c>Meshmakers.Octo.Communication.Contracts</c>), stay unqualified as before; a base
    ///     type of another model is qualified with the namespace of that model's DTOs
    ///     (<c>global::{root}.DataTransferObjects.{Model}.v{Major}</c>), e.g. the System.Communication types deriving from
    ///     <c>System@2/Configuration</c>.
    /// </summary>
    internal static string BaseTypeDtoName(string ns, CkId<CkTypeId> baseTypeId)
    {
        var name = $"Rt{baseTypeId.ElementId.Name.MakeClassName()}Dto";
        var marker = ns.LastIndexOf(DtoNamespaceMarker, StringComparison.Ordinal);
        if (marker < 0 || baseTypeId.ModelId.IsEmpty ||
            (baseTypeId.ModelId.Name == "System" && baseTypeId.ElementId.Name == "Entity"))
        {
            return name;
        }

        var baseNamespace = CkModelSourceEmitter.DtoNamespace(ns.Substring(0, marker), baseTypeId.ModelId.Name,
            baseTypeId.ModelId.Version.Major);
        return baseNamespace == ns ? name : $"global::{baseNamespace}.{name}";
    }

    /// <summary>
    ///     A property of a mutation DTO with its <c>[JsonIgnore]</c>. AB#9493: an attribute the generator does not support
    ///     becomes a comment only (and an Enum or Record attribute without its element id produces nothing); neither gets
    ///     a <c>[JsonIgnore]</c>, which would otherwise attach to the next property
    ///     (CS0579 duplicate attribute).
    /// </summary>
    internal static void GenerateMutationProperty(CkTypeAttributeDto ckTypeAttributeDto, StringBuilder sb,
        CkAttributeGraph ckAttributeGraph)
    {
        var property = new StringBuilder();
        GenerateNullableProperty(ckTypeAttributeDto, property, ckAttributeGraph, true);
        var code = property.ToString();
        // Only a generated property gets the attribute: an unsupported attribute is a comment, and an Enum or Record
        // attribute without its element id produces nothing at all.
        if (code.StartsWith("  public ", StringComparison.Ordinal))
        {
            sb.AppendLine(MutationJsonIgnore);
        }

        sb.Append(code);
    }

    internal static void GenerateNullableProperty(CkTypeAttributeDto ckTypeAttributeDto, StringBuilder sb,
        CkAttributeGraph ckAttributeGraph, bool isMutation)
    {
        switch (ckAttributeGraph.ValueType)
        {
            case AttributeValueTypesDto.String:
                sb.AppendLine($"  public string? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.Int:
                sb.AppendLine($"  public long? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.Int64:
                sb.AppendLine($"  public int? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.DateTime:
                sb.AppendLine($"  public global::System.DateTime? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.TimeSpan:
                sb.AppendLine($"  public global::System.TimeSpan? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.DateTimeOffset:
                sb.AppendLine($"  public global::System.DateTimeOffset? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.Double:
                sb.AppendLine($"  public double? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.Boolean:
                sb.AppendLine($"  public bool? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.Secret:
                // AB#5528 (concept §3.3, §4.1, §4.3): a secret is never readable through the API.
                // Query DTOs expose only whether it is set (GraphQL `OctoSecretState { isSet }`), so
                // consumers that read `.Password` as a string stop compiling instead of silently
                // receiving ciphertext. Mutation DTOs take the new plaintext as a string; null or ""
                // leaves the stored value unchanged, clearing goes through
                // MutationDto.ClearSecretAttributes.
                sb.AppendLine(isMutation
                    ? $"  public string? {ckTypeAttributeDto.AttributeName}"
                    : $"  public global::Meshmakers.Octo.Communication.Contracts.DataTransferObjects.OctoSecretStateDto? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.Enum:
                if (ckAttributeGraph.ValueCkEnumId != null)
                {
                    sb.AppendLine(
                        $"  public Rt{ckAttributeGraph.ValueCkEnumId.ElementId.Name.MakeClassName()}Enum? {ckTypeAttributeDto.AttributeName}");
                    sb.AppendLine("  {");
                    sb.AppendLine("      get; set;");
                    sb.AppendLine("  }");
                }

                break;
            case AttributeValueTypesDto.Record:
                if (ckAttributeGraph.ValueCkRecordId != null)
                {
                    sb.AppendLine(
                        isMutation
                            ? $"  public Rt{ckAttributeGraph.ValueCkRecordId.ElementId.Name.MakeClassName()}RecordMutationDto? {ckTypeAttributeDto.AttributeName}"
                            : $"  public Rt{ckAttributeGraph.ValueCkRecordId.ElementId.Name.MakeClassName()}RecordDto? {ckTypeAttributeDto.AttributeName}");
                    sb.AppendLine("  {");
                    sb.AppendLine("      get; set;");
                    sb.AppendLine("  }");
                }
                break;
            case AttributeValueTypesDto.StringArray:
                sb.AppendLine($"  public IEnumerable<string>? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.IntArray:
                sb.AppendLine($"  public IEnumerable<int>? {ckTypeAttributeDto.AttributeName}");
                sb.AppendLine("  {");
                sb.AppendLine("      get; set;");
                sb.AppendLine("  }");
                break;
            case AttributeValueTypesDto.RecordArray:
                // Not supported by the generator
                if (ckAttributeGraph.ValueCkRecordId != null)
                {
                    sb.AppendLine(
                        isMutation
                            ? $"  public IEnumerable<Rt{ckAttributeGraph.ValueCkRecordId.ElementId.Name.MakeClassName()}RecordMutationDto>? {ckTypeAttributeDto.AttributeName}"
                            : $"  public IEnumerable<Rt{ckAttributeGraph.ValueCkRecordId.ElementId.Name.MakeClassName()}RecordDto>? {ckTypeAttributeDto.AttributeName}");
                    sb.AppendLine("  {");
                    sb.AppendLine("      get; set;");
                    sb.AppendLine("  }");
                }
                sb.AppendLine($"  // Unsupported by Generator: {ckTypeAttributeDto.AttributeName} (Type: {ckAttributeGraph.ValueType})");

                break;
            default:
                sb.AppendLine($"  // Unsupported by Generator: {ckTypeAttributeDto.AttributeName} (Type: {ckAttributeGraph.ValueType})");
                break;
        }
    }
}