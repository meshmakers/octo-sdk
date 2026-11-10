using System.Text;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#8446 snapshot gate: the method clients of <b>synchronous</b> methods stay byte-identical when the generator
///     learns asynchronous methods. The golden files <c>Fixtures/Golden/*.methodclients.golden.txt</c> were recorded
///     with the generator of origin/main 7c48ed3 (before AB#8446) for three real compiled models whose methods are all
///     synchronous (<c>execution.mode</c> absent or <c>sync</c>): every <c>*MethodClient.g.cs</c> in ordinal hint order as
///     <c>//// {hint}\n{code}</c>. Line endings are normalized.
/// </summary>
public class SyncMethodClientGoldenTests
{
    [Theory]
    [InlineData(GeneratorFixtures.MethodClientTest)]
    [InlineData(GeneratorFixtures.AssetRepoIntegrationTest)]
    [InlineData(GeneratorFixtures.SystemIdentity)]
    public void MethodClientsOfSynchronousMethods_AreByteIdenticalToTheGolden(string fixture)
    {
        var sources = GeneratorFixtures.Emit(fixture);

        var actual = new StringBuilder();
        foreach (var (hint, code) in sources.Where(s => s.Key.EndsWith("MethodClient.g.cs", StringComparison.Ordinal)))
        {
            actual.Append("//// ").Append(hint).Append('\n').Append(code);
        }

        var golden = File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, "Golden",
            fixture + ".methodclients.golden.txt"));
        Assert.NotEqual(string.Empty, golden);
        Assert.Equal(Normalize(golden), Normalize(actual.ToString()));
    }

    [Theory]
    [InlineData(GeneratorFixtures.MethodClientTest)]
    [InlineData(GeneratorFixtures.AssetRepoIntegrationTest)]
    [InlineData(GeneratorFixtures.SystemIdentity)]
    public void GoldenFixtures_HaveNoAsynchronousMethod_SoTheGateCoversSyncOnly(string fixture)
    {
        var cache = File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, fixture + ".cache.json"));

        Assert.DoesNotContain("\"mode\":\"async\"", cache, StringComparison.Ordinal);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
