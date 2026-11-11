using System.Collections.Immutable;
using System.Reflection;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#8423: restores a compiled fixture model (real <c>octo-ckc Compile</c> output in <c>Fixtures</c>) the way
///     <c>CkDtoSourceGenerator</c> does and returns exactly the sources the generator adds to a compilation.
/// </summary>
internal static class GeneratorFixtures
{
    /// <summary>Test model of the method client generator (<c>Fixtures/MethodClientModel</c>).</summary>
    public const string MethodClientTest = "ck-methodclienttest";

    /// <summary>The asset repository's integration-test model (its schema is <see cref="AssetRepoSchemaFile" />).</summary>
    public const string AssetRepoIntegrationTest = "ck-assetrepositoryintegrationtest";

    /// <summary>One-type range-retaining model of AB#6715.</summary>
    public const string DtoFixture = "ck-dtofixture";

    /// <summary>SDL of the asset repository's integration-test tenant (gzip), see the recording steps in the docs.</summary>
    public const string AssetRepoSchemaFile = "assetrepo-integrationtest-schema.graphql.gz";

    /// <summary>
    ///     AB#8416: the released <c>System.Identity</c> 3.2.0 model (restored from the CK catalog with
    ///     <c>System.Identity-[3.2,4.0)</c>), generator input of the User method client tests only - the SDK ships no
    ///     System.Identity surface; consumers generate it themselves.
    /// </summary>
    public const string SystemIdentity = "ck-system.identity-3";

    /// <summary>SDL (gzip) of an asset repository tenant with <see cref="SystemIdentity" /> imported.</summary>
    public const string IdentitySchemaFile = "assetrepo-system-identity-schema.graphql.gz";

    /// <summary>
    ///     AB#8446: System.Bot 3.7.0 compiled from octo-bot-services (<c>TenantRepository.Dump-1</c>, an asynchronous
    ///     method), generator input of the asynchronous method client tests only.
    /// </summary>
    public const string SystemBot = "ck-system.bot-3";

    /// <summary>SDL (gzip) of a test-2 tenant with <see cref="SystemBot" /> imported (asset repository with AB#8444).</summary>
    public const string SystemBotSchemaFile = "assetrepo-system-bot-schema.graphql.gz";

    public static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static (CkCompiledModelRoot Model, ICkCacheService Cache, string TenantId) Restore(string fixture)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConstructionKit();
        var provider = services.BuildServiceProvider();

        var tenantId = fixture;
        var cache = provider.GetRequiredService<ICkCacheService>();
        cache.CreateTenant(tenantId);
        cache.RestoreCache(tenantId, File.ReadAllText(Path.Combine(FixtureDirectory, fixture + ".cache.json")));

        var yamlFile = Path.Combine(FixtureDirectory, fixture + ".yaml");
        var operationResult = new OperationResult();
        var model = provider.GetRequiredService<ICkYamlSerializer>()
            .DeserializeCompiledModelRoot(File.ReadAllText(yamlFile), yamlFile, operationResult);
        Assert.Empty(operationResult.Messages);
        Assert.Empty(CkDtoModelBinder.BindToCache(model, cache, tenantId));
        return (model, cache, tenantId);
    }

    /// <summary>All sources the generator emits for <paramref name="fixture" />, by hint name (ordinal order).</summary>
    public static SortedDictionary<string, string> Emit(string fixture, string localNamespace = "Golden.Consumer")
    {
        var (model, cache, tenantId) = Restore(fixture);
        var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
        CkModelSourceEmitter.Emit(localNamespace, model, tenantId, cache, (hint, code) => sources.Add(hint, code));
        return sources;
    }

    /// <summary>Compiles <paramref name="sources" /> into an assembly; returns the diagnostics and the image.</summary>
    public static (ImmutableArray<Diagnostic> Diagnostics, byte[]? Image) Compile(IEnumerable<string> sources)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Concat(new[]
                {
                    typeof(OctoSecretStateDto).Assembly, typeof(ITenantClient).Assembly,
                    typeof(global::GraphQL.GraphQLRequest).Assembly, typeof(CkId<>).Assembly
                }
                .Distinct()
                .Select(a => MetadataReference.CreateFromFile(a.Location)));
        var trees = sources
            // What a consumer project has: ImplicitUsings and (octo-cli Global.cs) the tenant client namespace.
            .Append("""
                    global using System;
                    global using System.Collections.Generic;
                    global using System.Linq;
                    global using System.Threading.Tasks;
                    global using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
                    """)
            .Select(source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)));
        var compilation = CSharpCompilation.Create("Generated" + Guid.NewGuid().ToString("N"), trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        return (result.Diagnostics, result.Success ? stream.ToArray() : null);
    }

    /// <summary>Compiles and loads <paramref name="sources" />; fails the test on any error or warning.</summary>
    public static Assembly CompileAndLoad(IEnumerable<string> sources)
    {
        var (diagnostics, image) = Compile(sources);
        var problems = diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ToList();
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        return Assembly.Load(image!);
    }
}
