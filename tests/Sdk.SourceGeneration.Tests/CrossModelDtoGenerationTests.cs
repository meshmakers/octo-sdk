using System.Text.RegularExpressions;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#9493: the DTOs of a model whose types derive from a type of another model, or carry attributes the generator
///     does not support, compile in a consumer project. Found with System.Communication 4.19.0 (AB#9052): 16 types derive
///     from <c>System@2/Configuration</c> (CS0246 for the unqualified base class) and
///     <c>EnergyCommunityConfiguration</c> has BinaryLinked attributes (CS0579, the <c>[JsonIgnore]</c> of the skipped
///     attribute stacked on the next property). The DTO goldens of the other fixtures pin that nothing else changed.
/// </summary>
public class CrossModelDtoGenerationTests
{
    private const string Ns = "Consumer.DataTransferObjects.System.Communication.v4";

    private static readonly Lazy<SortedDictionary<string, string>> Sources = new(() =>
        GeneratorFixtures.EmitWithDependencies(GeneratorFixtures.SystemCommunication, "Consumer", GeneratorFixtures.SystemDependency));

    [Theory]
    [InlineData("AiConfigurationQueryDto", "public partial class RtAiConfigurationDto : global::Consumer.DataTransferObjects.System.v2.RtConfigurationDto")]
    [InlineData("AiConfigurationMutationDto", "public partial class RtAiConfigurationMutationDto : global::Consumer.DataTransferObjects.System.v2.RtConfigurationDto")]
    [InlineData("SignalChannelQueryDto", "public partial class RtSignalChannelDto : global::Consumer.DataTransferObjects.System.v2.RtConfigurationDto")]
    public void BaseTypeOfAnotherModel_IsQualifiedWithItsModelNamespace(string source, string declaration)
    {
        Assert.Contains(declaration, Sources.Value[$"{Ns}.Type.{source}.g.cs"]);
    }

    [Theory]
    // Same model: unqualified, as before.
    [InlineData("AdapterQueryDto", "public partial class RtAdapterDto : RtDeployableWorkloadDto")]
    [InlineData("AdapterMutationDto", "public partial class RtAdapterMutationDto : RtDeployableWorkloadDto")]
    // System/Entity: RtEntityDto of Meshmakers.Octo.Communication.Contracts, unqualified as before.
    [InlineData("DataFlowQueryDto", "public partial class RtDataFlowDto : RtEntityDto")]
    [InlineData("DataFlowMutationDto", "public partial class RtDataFlowMutationDto : RtEntityDto")]
    public void BaseTypeOfTheSameModelOrSystemEntity_StaysUnqualified(string source, string declaration)
    {
        Assert.Contains(declaration, Sources.Value[$"{Ns}.Type.{source}.g.cs"]);
    }

    [Fact]
    public void UnsupportedAttribute_GetsNoJsonIgnore_SoTheNextPropertyHasOne()
    {
        var code = Sources.Value[$"{Ns}.Type.EnergyCommunityConfigurationMutationDto.g.cs"];

        Assert.Contains("// Unsupported by Generator: HeaderLogo (Type: BinaryLinked)", code);
        Assert.DoesNotMatch(new Regex(@"\[JsonIgnore[^\n]*\n\s*// Unsupported by Generator"), code.Replace("\r\n", "\n"));
        // Every generated property still carries exactly one [JsonIgnore].
        Assert.Equal(Regex.Matches(code, @"\n  public ").Count, Regex.Matches(code, @"\[JsonIgnore\(").Count);
    }

    [Fact]
    public void SystemCommunicationWithItsSystemDependency_CompilesWithoutErrorsOrWarnings()
    {
        Assert.NotNull(GeneratorFixtures.CompileAndLoad(Sources.Value.Values));
    }
}
