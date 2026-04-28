using IdentityModel;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;

namespace Sdk.ServiceClient.Tests.Authentication;

public class AuthenticatorClientTests
{
    private const string TokenEndpoint = "https://issuer.test/connect/token";
    private const string ClientId = "ci-client";
    private const string ClientSecret = "ci-secret";
    private const string Scope = "openid role octo_api";

    [Fact]
    public void BuildClientCredentialsTokenRequest_WithTenantId_AddsAcrValuesParameter()
    {
        var request = AuthenticatorClient.BuildClientCredentialsTokenRequest(
            TokenEndpoint, ClientId, ClientSecret, Scope, tenantId: "tenantA");

        Assert.True(request.Parameters.ContainsKey(OidcConstants.AuthorizeRequest.AcrValues));
        Assert.Equal("tenant:tenantA",
            request.Parameters.GetValues(OidcConstants.AuthorizeRequest.AcrValues).Single());
    }

    [Fact]
    public void BuildClientCredentialsTokenRequest_WithoutTenantId_DoesNotAddAcrValues()
    {
        var request = AuthenticatorClient.BuildClientCredentialsTokenRequest(
            TokenEndpoint, ClientId, ClientSecret, Scope, tenantId: null);

        Assert.False(request.Parameters.ContainsKey(OidcConstants.AuthorizeRequest.AcrValues));
    }

    [Fact]
    public void BuildClientCredentialsTokenRequest_WithEmptyTenantId_DoesNotAddAcrValues()
    {
        var request = AuthenticatorClient.BuildClientCredentialsTokenRequest(
            TokenEndpoint, ClientId, ClientSecret, Scope, tenantId: string.Empty);

        Assert.False(request.Parameters.ContainsKey(OidcConstants.AuthorizeRequest.AcrValues));
    }

    [Fact]
    public void BuildClientCredentialsTokenRequest_PopulatesGrantTypeAndCredentials()
    {
        var request = AuthenticatorClient.BuildClientCredentialsTokenRequest(
            TokenEndpoint, ClientId, ClientSecret, Scope, tenantId: null);

        Assert.Equal(OidcConstants.GrantTypes.ClientCredentials, request.GrantType);
        Assert.Equal(TokenEndpoint, request.Address);
        Assert.Equal(ClientId, request.ClientId);
        Assert.Equal(ClientSecret, request.ClientSecret);
        Assert.Equal(Scope, request.Scope);
    }
}
