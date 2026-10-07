using System.Collections.Immutable;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.DependencyGraph;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;
using Meshmakers.Octo.Sdk.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CkRecordDto = Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects.CkRecordDto;
using CkTypeAttributeDto = Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects.CkTypeAttributeDto;
using CkTypeDto = Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects.CkTypeDto;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#5534 (WP4 of AB#5528, concept §3.3): generated query DTOs expose a Secret attribute as
///     <see cref="OctoSecretStateDto" />, mutation DTOs as <c>string?</c> - code that read
///     <c>.Password</c> as a string must stop compiling.
/// </summary>
public class SecretAttributeCodeGenerationTests
{
    private const string TenantId = "test";
    private const string ModelId = "Test-1.0.0";
    private const string Ns = "DataTransferObjects.Test.v1";

    private readonly ICkCacheService _cache = A.Fake<ICkCacheService>();
    private readonly Dictionary<CkId<CkAttributeId>, CkAttributeGraph> _attributes = new();

    public SecretAttributeCodeGenerationTests()
    {
        A.CallTo(() => _cache.GetCkAttribute(A<string>._, A<CkId<CkAttributeId>>._))
            .ReturnsLazily((string _, CkId<CkAttributeId> id) => _attributes[id]);
    }

    [Fact]
    public void QueryType_SecretAttribute_IsOctoSecretState()
    {
        var code = QueryDtoCodeGenerator.Instance.GenerateType(Ns, AccountType(), TenantId, _cache);

        Assert.Contains(
            "public global::Meshmakers.Octo.Communication.Contracts.DataTransferObjects.OctoSecretStateDto? Password",
            code, StringComparison.Ordinal);
        Assert.DoesNotContain("string? Password", code, StringComparison.Ordinal);
        Assert.Contains("public string? UserName", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Unsupported by Generator: Password", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MutationType_SecretAttribute_IsNullableString()
    {
        var code = MutationDtoCodeGenerator.Instance.GenerateType(Ns, AccountType(), TenantId, _cache);

        Assert.Contains("public string? Password", code, StringComparison.Ordinal);
        Assert.DoesNotContain("OctoSecretStateDto", code, StringComparison.Ordinal);
        // Not written when null: an omitted secret is "unchanged" (concept §4.3).
        Assert.Contains(
            "[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]" + Environment.NewLine + "  public string? Password",
            code, StringComparison.Ordinal);
    }

    [Fact]
    public void Record_SecretSubAttribute_QueryIsStateAndMutationIsString()
    {
        var record = CredentialRecord();

        var query = QueryDtoCodeGenerator.Instance.GenerateRecord(Ns, record, TenantId, _cache);
        var mutation = MutationDtoCodeGenerator.Instance.GenerateRecord(Ns, record, TenantId, _cache);

        Assert.Contains("OctoSecretStateDto? Value", query, StringComparison.Ordinal);
        Assert.Contains("public string? Value", mutation, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedCode_Compiles_AndReadingTheSecretAsStringDoesNot()
    {
        var record = CredentialRecord();
        var query = QueryDtoCodeGenerator.Instance.GenerateRecord(Ns, record, TenantId, _cache);
        var mutation = MutationDtoCodeGenerator.Instance.GenerateRecord(Ns, record, TenantId, _cache);

        const string validConsumer = $$"""
            namespace Consumer;
            public static class Valid
            {
                public static bool IsSet({{Ns}}.RtCredentialRecordDto dto) => dto.Value?.IsSet == true;
                public static {{Ns}}.RtCredentialRecordMutationDto Set(string plaintext) => new() { Value = plaintext };
            }
            """;
        const string brokenConsumer = $$"""
            namespace Consumer;
            public static class Broken
            {
                public static string Read({{Ns}}.RtCredentialRecordDto dto) => dto.Value;
            }
            """;

        Assert.Empty(Errors(query, mutation, validConsumer));
        var errors = Errors(query, mutation, brokenConsumer);
        Assert.Contains(errors, d => d.Id == "CS0029");
    }

    private static ImmutableArray<Diagnostic> Errors(params string[] sources)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(OctoSecretStateDto).Assembly.Location));
        var compilation = CSharpCompilation.Create("Generated",
            sources.Select(source => CSharpSyntaxTree.ParseText(source)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        return [.. compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];
    }

    private CkTypeDto AccountType()
    {
        return new CkTypeDto
        {
            TypeId = new CkTypeId("Account"),
            Attributes =
            [
                Attribute("UserName", AttributeValueTypesDto.String),
                Attribute("Password", AttributeValueTypesDto.Secret)
            ]
        };
    }

    private CkRecordDto CredentialRecord()
    {
        return new CkRecordDto
        {
            RecordId = new CkRecordId("Credential"),
            RecordKey = "Key",
            Attributes =
            [
                Attribute("Key", AttributeValueTypesDto.String),
                Attribute("Value", AttributeValueTypesDto.Secret)
            ]
        };
    }

    private CkTypeAttributeDto Attribute(string name, AttributeValueTypesDto valueType)
    {
        var id = new CkId<CkAttributeId>($"{ModelId}/{name}");
        _attributes[id] = new CkAttributeGraph(id, valueType, null, null, null, null, null);
        return new CkTypeAttributeDto { CkAttributeId = id, AttributeName = name };
    }
}
