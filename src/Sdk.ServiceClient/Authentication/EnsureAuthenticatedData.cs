namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

public class EnsureAuthenticatedData
{
    public bool IsRefreshDone { get; set; }
    public AuthenticationData RefreshedAuthenticationData { get; set; } = null!;
}
