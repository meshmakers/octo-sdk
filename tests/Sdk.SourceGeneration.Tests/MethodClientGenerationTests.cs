using System.Text.RegularExpressions;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.SourceGeneration;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#8423: the method client generator — one client per concrete CK type with methods, one async method per CK
///     method, names and documents following the asset repository's rules (AB#5670), and no change at all for types
///     and models without methods.
/// </summary>
public class MethodClientGenerationTests
{
    private const string Ns = "Golden.Consumer.DataTransferObjects.MethodClientTest.v1";

    [Fact]
    public void TypesWithMethods_GetOneClientEach_TypesWithoutMethodsGetNone()
    {
        var sources = GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest);

        var clients = sources.Keys.Where(k => k.EndsWith("MethodClient.g.cs", StringComparison.Ordinal)).ToList();
        Assert.Equal([$"{Ns}.Type.AccountMethodClient.g.cs", $"{Ns}.Type.ServiceAccountMethodClient.g.cs"], clients);
        Assert.DoesNotContain(sources.Keys, k => k.Contains("NoteMethodClient", StringComparison.Ordinal));
    }

    [Fact]
    public void Client_HasOneAsyncMethodPerCkMethod_IncludingInheritedOnes()
    {
        var sources = GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest);

        foreach (var type in new[] { "Account", "ServiceAccount" })
        {
            var code = sources[$"{Ns}.Type.{type}MethodClient.g.cs"];
            Assert.Contains($"public partial class Rt{type}MethodClient : global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodClientBase", code);
            var methods = Regex.Matches(code, @"public global::System\.Threading\.Tasks\.Task<[^>]+(?:<[^>]+>)?> (\w+)Async\(")
                .Select(m => m.Groups[1].Value).ToList();
            Assert.Equal(["CountLocked", "FindByName", "ResetPassword", "ResetPassword2", "Touch"], methods);
        }
    }

    [Fact]
    public void Signatures_InstanceTakesRtId_StaticNone_RequiredBeforeOptional_IdempotencyKeyLast()
    {
        var code = GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest)[$"{Ns}.Type.AccountMethodClient.g.cs"];
        const string result = "global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodResult";

        Assert.Contains($"Task<{result}<string?>> ResetPasswordAsync(string rtId, string newPassword, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{result}> ResetPassword2Async(string rtId, string newPassword, bool? notifyUser = null, global::Golden.Consumer.DataTransferObjects.MethodClientTest.v1.RtAccountStateEnum? state = null, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{result}> TouchAsync(string rtId, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{result}<global::Golden.Consumer.DataTransferObjects.MethodClientTest.v1.RtAccountSummaryRecordDto?>> FindByNameAsync(string name, int? maxResults = null, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{result}<int?>> CountLockedAsync(string? idempotencyKey = null)", code);
    }

    [Fact]
    public void Documents_FollowTheAssetRepositoryWireContract()
    {
        var code = GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest)[$"{Ns}.Type.AccountMethodClient.g.cs"];

        Assert.Equal(
            "mutation MethodClientTestAccountResetPassword2($rtId: OctoObjectId!, $input: MethodClientTestAccountResetPassword2MethodInput!, $idempotencyKey: String) { runtime { methodClientTestAccounts { resetPassword2(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId } } } }",
            Document(code, "ResetPassword2"));
        Assert.Equal(
            "mutation MethodClientTestAccountTouch($rtId: OctoObjectId!, $idempotencyKey: String) { runtime { methodClientTestAccounts { touch(rtId: $rtId, idempotencyKey: $idempotencyKey) { invocationId } } } }",
            Document(code, "Touch"));
        Assert.Equal(
            "mutation MethodClientTestAccountCountLocked($idempotencyKey: String) { runtime { methodClientTestAccounts { countLocked(idempotencyKey: $idempotencyKey) { invocationId result } } } }",
            Document(code, "CountLocked"));
        Assert.Equal(
            "mutation MethodClientTestAccountFindByName($input: MethodClientTestAccountFindByNameMethodInput!, $idempotencyKey: String) { runtime { methodClientTestAccounts { findByName(input: $input, idempotencyKey: $idempotencyKey) { invocationId result { loginCount name } } } } }",
            Document(code, "FindByName"));
        Assert.Contains("public const string ResetPassword2MethodId = \"MethodClientTest/Account.ResetPassword-2\";", code);
    }

    [Fact]
    public void InheritedMethod_UsesTheDerivedTypeField_AndTheDeclaringTypeInput()
    {
        var code = GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest)[$"{Ns}.Type.ServiceAccountMethodClient.g.cs"];

        Assert.Equal(
            "mutation MethodClientTestServiceAccountResetPassword($rtId: OctoObjectId!, $input: MethodClientTestAccountResetPasswordMethodInput!, $idempotencyKey: String) { runtime { methodClientTestServiceAccounts { resetPassword(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId result } } } }",
            Document(code, "ResetPassword"));
        Assert.Contains("public const string ResetPasswordMethodId = \"MethodClientTest/Account.ResetPassword-1\";", code);
    }

    [Fact]
    public void DeclaredErrors_BecomeConstants()
    {
        var code = GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest)[$"{Ns}.Type.AccountMethodClient.g.cs"];

        Assert.Contains("public static class ResetPassword2Errors", code);
        Assert.Contains("public const string PasswordPolicyViolation = \"PASSWORD_POLICY_VIOLATION\";", code);
        Assert.Contains("public const string PasswordReused = \"PASSWORD_REUSED\";", code);
        Assert.Contains("ResetPassword2DeclaredErrorCodes = new string[] { \"PASSWORD_POLICY_VIOLATION\", \"PASSWORD_REUSED\" };", code);
        Assert.DoesNotContain("TouchErrors", code);
    }

    /// <summary>
    ///     Shared test vectors with the asset repository (<c>CkMethodMutationFields.FieldName</c>,
    ///     <c>Statics.GetGraphQlPascalCaseName</c> / <c>GetGraphQlCamelCaseName</c>, <c>CkMethodGraphTypes</c>).
    /// </summary>
    [Theory]
    [InlineData("ResetPassword-2", "resetPassword2")]
    [InlineData("ResetPassword-1", "resetPassword")]
    [InlineData("Calibrate-2", "calibrate2")]
    [InlineData("Probe-1", "probe")]
    [InlineData("CreateUser-1", "createUser")]
    [InlineData("Load10-1", "load10")]
    public void MethodFieldName_MatchesTheAssetRepository(string methodId, string expected)
    {
        Assert.Equal(expected, GraphQlNames.MethodFieldName(methodId));
    }

    [Theory]
    [InlineData("System.Identity/User", "systemIdentityUsers")]
    [InlineData("System.Identity/Named-2", "systemIdentityNamed2s")]
    [InlineData("AssetRepositoryIntegrationTest/OperableThing", "assetRepositoryIntegrationTestOperableThings")]
    public void TypeMutationFieldName_MatchesTheAssetRepository(string rtCkTypeId, string expected)
    {
        Assert.Equal(expected, GraphQlNames.TypeMutationFieldName(new RtCkId<CkTypeId>(rtCkTypeId)));
    }

    [Theory]
    [InlineData("System.Identity/User", "ResetPassword-2", "SystemIdentityUserResetPassword2MethodInput")]
    [InlineData("AssetRepositoryIntegrationTest/OperableThing", "Calibrate-1", "AssetRepositoryIntegrationTestOperableThingCalibrateMethodInput")]
    public void MethodInputTypeName_MatchesTheAssetRepository(string declaringType, string methodId, string expected)
    {
        Assert.Equal(expected, GraphQlNames.MethodInputTypeName(new RtCkId<CkTypeId>(declaringType), methodId));
    }

    [Theory]
    [InlineData("PASSWORD_POLICY_VIOLATION", "PasswordPolicyViolation")]
    [InlineData("CALIBRATION_OUT_OF_RANGE", "CalibrationOutOfRange")]
    [InlineData("X", "X")]
    public void ErrorConstantName_IsPascalCase(string code, string expected)
    {
        Assert.Equal(expected, MethodClientCodeGenerator.ErrorConstantName(code));
    }

    [Fact]
    public void AbstractTypeWithMethods_GetsNoClient()
    {
        var (model, cache, tenantId) = GeneratorFixtures.Restore(GeneratorFixtures.MethodClientTest);
        var account = model.Types!.Single(t => t.TypeId.Name == "Account");
        account.IsAbstract = true;

        Assert.Null(MethodClientCodeGenerator.Generate("Golden.Consumer", Ns, model.ModelId, account, tenantId, cache));
    }

    [Fact]
    public void ParameterNamedLikeTheResultSelector_StillCompiles()
    {
        // CodeRabbit PR #99: a CK parameter named "response" must not clash with the generated lambda (CS0136).
        var (model, cache, tenantId) = GeneratorFixtures.Restore(GeneratorFixtures.MethodClientTest);
        var account = model.Types!.Single(t => t.TypeId.Name == "Account");
        var findByName = cache.GetCkType(tenantId, new CkId<CkTypeId>(model.ModelId, account.TypeId))
            .AllMethods["FindByName-1"].Definition;
        findByName.Parameters![0].Name = "response";
        var sources = new List<string>();
        CkModelSourceEmitter.Emit("Consumer", model, tenantId, cache, (_, code) => sources.Add(code));

        Assert.Contains(sources, c => c.Contains("FindByNameAsync(string response,", StringComparison.Ordinal));
        GeneratorFixtures.CompileAndLoad(sources);
    }

    [Fact]
    public void WithoutMethodClientRuntime_OnlyTheDtosAreEmitted_Unchanged()
    {
        // CodeRabbit PR #99: a consumer without Sdk.ServiceClient (or with an older one) gets no clients.
        var (model, cache, tenantId) = GeneratorFixtures.Restore(GeneratorFixtures.MethodClientTest);
        var withoutRuntime = new SortedDictionary<string, string>(StringComparer.Ordinal);
        CkModelSourceEmitter.Emit("Golden.Consumer", model, tenantId, cache, (hint, code) => withoutRuntime.Add(hint, code),
            emitMethodClients: false);

        var withRuntime = GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest)
            .Where(s => !s.Key.EndsWith("MethodClient.g.cs", StringComparison.Ordinal));
        Assert.Equal(withRuntime, withoutRuntime);
    }

    [Fact]
    public void GeneratedSources_CompileWithoutWarnings_ForBothFixtureModels()
    {
        GeneratorFixtures.CompileAndLoad(GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest, "Consumer").Values);
        // The type DTOs of this model are left out: the mutation DTO generator emits a dangling [JsonIgnore] before an
        // unsupported attribute (Binary, BinaryLinked; e.g. Product), a pre-existing defect outside AB#8423 that the
        // byte-identity gate keeps as it is. The clients need only the record DTOs and enums.
        GeneratorFixtures.CompileAndLoad(GeneratorFixtures.Emit(GeneratorFixtures.AssetRepoIntegrationTest, "Consumer")
            .Where(s => !s.Key.Contains(".Type.", StringComparison.Ordinal) || s.Key.EndsWith("MethodClient.g.cs", StringComparison.Ordinal))
            .Select(s => s.Value));
    }

    internal static string Document(string code, string method)
    {
        var match = Regex.Match(code, $"public const string {method}Document = \"(?<doc>[^\"]*)\";");
        Assert.True(match.Success, $"no document for {method}");
        return match.Groups["doc"].Value;
    }
}
