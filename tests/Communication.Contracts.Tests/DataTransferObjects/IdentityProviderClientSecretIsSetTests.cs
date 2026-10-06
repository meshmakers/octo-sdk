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

    [Theory]
    [MemberData(nameof(SecretBearingProviders))]
    public void DeserialisesKeyMissingAndSetAtFromAResponse(Type dtoType)
    {
        const string response =
            """{ "name": "idp", "clientId": "c", "clientSecretIsSet": false, "clientSecretKeyMissing": true, "clientSecretSetAt": "2026-10-06T08:00:00Z" }""";

        var dto = JsonSerializer.Deserialize(response, dtoType, WebOptions);

        Assert.NotNull(dto);
        Assert.False((bool?)dtoType.GetProperty("ClientSecretIsSet")!.GetValue(dto));
        Assert.True((bool?)dtoType.GetProperty("ClientSecretKeyMissing")!.GetValue(dto));
        var setAt = (DateTime?)dtoType.GetProperty("ClientSecretSetAt")!.GetValue(dto);
        Assert.Equal(new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc), setAt!.Value.ToUniversalTime());
    }

    [Theory]
    [MemberData(nameof(SecretBearingProviders))]
    public void OmitsKeyMissingAndSetAtWhenNull(Type dtoType)
    {
        var dto = Activator.CreateInstance(dtoType)!;

        var stjJson = JsonSerializer.Serialize(dto, dtoType, WebOptions);
        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(dto);

        foreach (var name in new[] { "clientSecretKeyMissing", "clientSecretSetAt" })
        {
            Assert.DoesNotContain(name, stjJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(name, newtonsoftJson, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [MemberData(nameof(SecretBearingProviders))]
    public void WritesKeyMissingAndSetAtCamelCaseWhenSet(Type dtoType)
    {
        var dto = Activator.CreateInstance(dtoType)!;
        dtoType.GetProperty("ClientSecretKeyMissing")!.SetValue(dto, true);
        dtoType.GetProperty("ClientSecretSetAt")!.SetValue(dto, new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc));

        var stjJson = JsonSerializer.Serialize(dto, dtoType);
        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(dto);

        Assert.Contains("\"clientSecretKeyMissing\":true", stjJson);
        Assert.Contains("\"clientSecretKeyMissing\":true", newtonsoftJson);
        Assert.Contains("\"clientSecretSetAt\":\"2026-10-06T08:00:00Z\"", stjJson);
        Assert.Contains("\"clientSecretSetAt\":\"2026-10-06T08:00:00Z\"", newtonsoftJson);
    }
}
