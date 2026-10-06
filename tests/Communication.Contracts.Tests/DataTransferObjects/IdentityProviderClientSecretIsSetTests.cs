using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#5540 — the client secret of an OAuth identity provider is write-only. Responses carry
///     <c>clientSecretIsSet</c> instead of the secret; requests built from the same DTO must not
///     send the flag (old servers do not know it).
/// </summary>
public class IdentityProviderClientSecretIsSetTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    public static TheoryData<Type> SecretBearingProviders =>
    [
        typeof(GoogleIdentityProviderDto),
        typeof(MicrosoftIdentityProviderDto),
        typeof(FacebookIdentityProviderDto),
        typeof(AzureEntraIdProviderDto)
    ];

    [Theory]
    [MemberData(nameof(SecretBearingProviders))]
    public void DeserialisesClientSecretIsSetFromAResponse(Type dtoType)
    {
        const string response = """{ "name": "idp", "clientId": "c", "clientSecret": null, "clientSecretIsSet": true }""";

        var dto = JsonSerializer.Deserialize(response, dtoType, WebOptions);

        Assert.NotNull(dto);
        Assert.True((bool?)dtoType.GetProperty("ClientSecretIsSet")!.GetValue(dto));
        Assert.Null(dtoType.GetProperty("ClientSecret")!.GetValue(dto));
    }

    [Theory]
    [MemberData(nameof(SecretBearingProviders))]
    public void OmitsClientSecretIsSetWhenNull(Type dtoType)
    {
        var dto = Activator.CreateInstance(dtoType)!;

        var stjJson = JsonSerializer.Serialize(dto, dtoType, WebOptions);
        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(dto);

        Assert.DoesNotContain("clientSecretIsSet", stjJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecretIsSet", newtonsoftJson, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(SecretBearingProviders))]
    public void WritesClientSecretIsSetCamelCaseWhenSet(Type dtoType)
    {
        var dto = Activator.CreateInstance(dtoType)!;
        dtoType.GetProperty("ClientSecretIsSet")!.SetValue(dto, false);

        var stjJson = JsonSerializer.Serialize(dto, dtoType);
        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(dto);

        Assert.Contains("\"clientSecretIsSet\":false", stjJson);
        Assert.Contains("\"clientSecretIsSet\":false", newtonsoftJson);
    }

    [Fact]
    public void PolymorphicResponseKeepsTheFlag()
    {
        const string response = """{ "$type": 0, "name": "google", "clientId": "c", "clientSecretIsSet": true }""";

        var dto = JsonSerializer.Deserialize<IdentityProviderDto>(response, WebOptions);

        var google = Assert.IsType<GoogleIdentityProviderDto>(dto);
        Assert.True(google.ClientSecretIsSet);
    }
}
