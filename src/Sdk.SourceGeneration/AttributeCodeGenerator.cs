using System.Text;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.DependencyGraph;

namespace Meshmakers.Octo.Sdk.SourceGeneration;

internal static class AttributeCodeGenerator
{
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