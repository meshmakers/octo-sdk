using System.IO.Compression;
using GraphQL;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#8423 (AC 2): every GraphQL document a generated method client sends validates against the schema of a real
///     asset repository. The schema is the SDL of the asset repository's integration-test tenant
///     (<c>SchemaExportTests</c> of octo-asset-repo-services, recording steps in <c>docs/method-clients.md</c>); the
///     fixture <c>ck-assetrepositoryintegrationtest</c> is the compiled CK model of that tenant, which declares
///     instance, static, versioned, inherited, interface and parameterless methods.
/// </summary>
public class MethodClientSchemaValidationTests
{
    private static readonly Lazy<ISchema> Schema = new(() => LoadSchema(GeneratorFixtures.AssetRepoSchemaFile));

    public static TheoryData<string, string> Documents()
    {
        var data = new TheoryData<string, string>();
        foreach (var (hint, code) in GeneratorFixtures.Emit(GeneratorFixtures.AssetRepoIntegrationTest)
                     .Where(s => s.Key.EndsWith("MethodClient.g.cs", StringComparison.Ordinal)))
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(code,
                         "public const string (?<name>\\w+)Document = \"(?<doc>[^\"]*)\";"))
            {
                data.Add($"{hint}:{match.Groups["name"].Value}", match.Groups["doc"].Value);
            }
        }

        return data;
    }

    [Fact]
    public void Fixture_CoversEveryMethodShape()
    {
        var names = Documents().Select(row => row.Data.Item1).ToList();

        Assert.Contains(names, n => n.EndsWith("OperableThingMethodClient.g.cs:Calibrate", StringComparison.Ordinal)); // instance, record result
        Assert.Contains(names, n => n.EndsWith("OperableThingMethodClient.g.cs:Calibrate2", StringComparison.Ordinal)); // version 2
        Assert.Contains(names, n => n.EndsWith("OperableThingMethodClient.g.cs:Probe", StringComparison.Ordinal)); // static, no parameters
        Assert.Contains(names, n => n.EndsWith("OperableThingMethodClient.g.cs:Restart", StringComparison.Ordinal)); // interface method, no parameters
        Assert.Contains(names, n => n.EndsWith("OperableThingChildMethodClient.g.cs:Calibrate", StringComparison.Ordinal)); // inherited
        Assert.True(names.Count > 50, $"only {names.Count} documents");
    }

    [Theory]
    [InlineData("mutation M($rtId: OctoObjectId!, $input: AssetRepositoryIntegrationTestOperableThingCalibrateMethodInputX!, $idempotencyKey: String) { runtime { assetRepositoryIntegrationTestOperableThings { calibrate(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId } } } }")]
    [InlineData("mutation M($idempotencyKey: String) { runtime { assetRepositoryIntegrationTestOperableThings { probe(idempotencyKey: $idempotencyKey) { invocationId result { name } } } } }")]
    [InlineData("mutation M($input: AssetRepositoryIntegrationTestOperableThingCalibrate2MethodInput, $idempotencyKey: String) { runtime { assetRepositoryIntegrationTestOperableThings { calibrate2(input: $input, idempotencyKey: $idempotencyKey) { invocationId } } } }")]
    [InlineData("mutation M($idempotencyKey: String) { runtime { assetRepositoryIntegrationTestOperableThings { restart(idempotencyKey: $idempotencyKey) { invocationId } } } }")]
    public async Task BrokenDocument_IsRejected_SoTheGateIsReal(string document)
    {
        Assert.NotEmpty(await DocumentErrorsAsync(Schema.Value, document));
    }

    [Theory]
    [MemberData(nameof(Documents))]
    public async Task Document_ValidatesAgainstTheRecordedAssetRepositorySchema(string method, string document)
    {
        var errors = await DocumentErrorsAsync(Schema.Value, document);

        Assert.True(errors.Count == 0, method + ": " + string.Join("; ", errors));
    }

    /// <summary>Validation errors of <paramref name="document" /> against <paramref name="schema" /> (core rules).</summary>
    internal static async Task<List<string>> DocumentErrorsAsync(ISchema schema, string document)
    {
        var parsed = GraphQLParser.Parser.Parse(document);
        var operation = parsed.Definitions.OfType<GraphQLOperationDefinition>().Single();

        var result = await new DocumentValidator().ValidateAsync(new ValidationOptions
        {
            Schema = schema,
            Document = parsed,
            Operation = operation,
            Rules = DocumentValidator.CoreRules,
            Variables = Inputs.Empty,
            Extensions = Inputs.Empty
        });

        // The document is checked, not variable values: no values are supplied, so the missing-value errors of the
        // required variables ($rtId, $input) are expected and ignored. Unknown or mistyped variable types, fields and
        // arguments are reported by the core rules.
        return result.Errors
            .Where(e => !(e is InvalidVariableError &&
                          e.Message.Contains("No value provided for a non-null variable", StringComparison.Ordinal)))
            .Select(e => e.Message)
            .ToList();
    }

    private static readonly HashSet<string> BuiltInScalars = new(StringComparer.Ordinal)
    {
        "String", "Int", "Float", "Boolean", "ID", "DateTime", "DateTimeOffset", "Date", "Decimal", "Long", "ULong",
        "Byte", "SByte", "Short", "UShort", "UInt", "BigInt", "Uri", "Guid", "Seconds", "Milliseconds", "TimeOnly",
        "DateOnly", "Half"
    };

    /// <summary>Stand-in for the asset repository's custom scalars (validation only checks names and nullability).</summary>
    private sealed class PassThroughScalar : ScalarGraphType
    {
        public PassThroughScalar(string name)
        {
            Name = name;
        }

        public override object? ParseLiteral(GraphQLValue value) => value;

        public override object? ParseValue(object? value) => value;
    }

    /// <summary>Loads a recorded asset-repository SDL (gzip) from the fixture directory for document validation.</summary>
    internal static ISchema LoadSchema(string fixtureFile)
    {
        using var file = File.OpenRead(Path.Combine(GeneratorFixtures.FixtureDirectory, fixtureFile));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        // Only mutations are validated; the subscription root would need stream resolvers to initialize.
        var sdl = System.Text.RegularExpressions.Regex.Replace(reader.ReadToEnd(), @"^\s*subscription:\s*\w+\s*$", string.Empty,
            System.Text.RegularExpressions.RegexOptions.Multiline);
        // The SDL carries no resolvers; unions and interfaces only need a (never called) type resolver to initialize.
        var abstractTypes = GraphQLParser.Parser.Parse(sdl).Definitions
            .Select(d => d switch
            {
                GraphQLUnionTypeDefinition union => union.Name.StringValue,
                GraphQLInterfaceTypeDefinition @interface => @interface.Name.StringValue,
                _ => null
            })
            .OfType<string>()
            .ToList();
        var scalars = GraphQLParser.Parser.Parse(sdl).Definitions.OfType<GraphQLScalarTypeDefinition>()
            .Select(d => d.Name.StringValue)
            .Where(name => !BuiltInScalars.Contains(name))
            .ToList();
        var schema = GraphQL.Types.Schema.For(sdl, builder =>
        {
            foreach (var name in abstractTypes)
            {
                builder.Types.For(name).ResolveType = _ => null!;
            }
        });
        foreach (var name in scalars)
        {
            schema.RegisterType(new PassThroughScalar(name));
        }
        schema.Initialize();
        return schema;
    }
}
