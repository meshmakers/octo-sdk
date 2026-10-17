using Duende.IdentityModel;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;

namespace Sdk.ServiceClient.Tests.Authentication;

public class TenantTokenExchangeRequestTests
{
    [Fact]
    public void Request_CarriesGrantSubjectTokenAndTargetTenant()
    {
        var request = AuthenticatorClient.BuildTokenExchangeRequest("https://issuer.test/connect/token",
            "octo-cli", null, "subject-jwt", "child");

        Assert.Equal("subject-jwt", request.SubjectToken);
        Assert.Equal(OidcConstants.TokenTypeIdentifiers.AccessToken, request.SubjectTokenType);
        Assert.Equal("octo-cli", request.ClientId);
        Assert.Equal("tenant:child", request.Parameters.GetValues(OidcConstants.AuthorizeRequest.AcrValues).Single());
        Assert.Contains("octo_api", request.Scope);
    }

    [Theory]
    [InlineData("tenant_not_provisioned", null, TenantTokenDenialCause.NotProvisioned)]
    [InlineData("unauthorized_client", "cross-tenant access to the target tenant is denied",
        TenantTokenDenialCause.AccessToTenantDenied)]
    [InlineData("unauthorized_client", "This client application is not allowed to use the specified grant type.",
        TenantTokenDenialCause.ClientNotAllowed)]
    [InlineData("unsupported_grant_type", null, TenantTokenDenialCause.ClientNotAllowed)]
    [InlineData("invalid_grant", "subject_token is invalid or expired", TenantTokenDenialCause.SubjectTokenRejected)]
    [InlineData("invalid_grant", "failed to provision the target-tenant user", TenantTokenDenialCause.AccessToTenantDenied)]
    [InlineData("invalid_target", "target tenant could not be resolved for this request",
        TenantTokenDenialCause.TenantUnknown)]
    [InlineData("server_error", null, TenantTokenDenialCause.Unknown)]
    public void IdentityErrors_AreMappedToACause(string error, string? description, TenantTokenDenialCause expected)
    {
        Assert.Equal(expected, AuthenticatorClient.MapExchangeError(error, description));
    }
}
