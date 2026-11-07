using System.Text;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#8423 snapshot gate: everything the generator emitted before the method clients existed stays byte-identical.
///     The golden files in <c>Fixtures/Golden</c> were recorded with the generator of origin/main a02fdf2 (before
///     AB#8423) for three real compiled models; the only new sources are the <c>*MethodClient.g.cs</c> files of the
///     concrete types with methods. Line endings are normalized (the generator writes <see cref="Environment.NewLine" />).
/// </summary>
public class MethodlessGeneratedCodeGoldenTests
{
    [Theory]
    [InlineData(GeneratorFixtures.DtoFixture)]
    [InlineData(GeneratorFixtures.MethodClientTest)]
    [InlineData(GeneratorFixtures.AssetRepoIntegrationTest)]
    public void EverySourceExceptTheMethodClients_IsByteIdenticalToTheGolden(string fixture)
    {
        var sources = GeneratorFixtures.Emit(fixture);

        var actual = new StringBuilder();
        foreach (var (hint, code) in sources.Where(s => !s.Key.EndsWith("MethodClient.g.cs", StringComparison.Ordinal)))
        {
            actual.Append("//// ").Append(hint).Append('\n').Append(code);
        }

        var golden = File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, "Golden", fixture + ".golden.txt"));
        Assert.Equal(Normalize(golden), Normalize(actual.ToString()));
    }

    [Fact]
    public void ModelWithoutMethods_GetsNoMethodClient()
    {
        Assert.DoesNotContain(GeneratorFixtures.Emit(GeneratorFixtures.DtoFixture).Keys,
            k => k.EndsWith("MethodClient.g.cs", StringComparison.Ordinal));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
