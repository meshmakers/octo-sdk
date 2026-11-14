namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
///     AB#8416: messages of the <see cref="System.ObsoleteAttribute" /> on the REST user methods of
///     <see cref="IIdentityServicesClient" /> that the CK methods of <c>System.Identity/User</c> replace. The replacement
///     is the method client <c>RtUserMethodClient</c> a consumer generates from <c>System.Identity-[3.2,4.0)</c>
///     (<c>construction-kits.yaml</c>); see <c>docs/method-client-migration-guide.md</c>. The REST methods keep working
///     during their REST window. AB#8860: <see cref="IIdentityServicesClient.CreateUser" /> and
///     <see cref="IIdentityServicesClient.ResetPassword" /> follow after the first-party consumers (octo-cli,
///     octo-mcp-service) moved to <c>User.CreateUser-1</c> / <c>User.ResetPassword-1</c> (AB#8418).
/// </summary>
internal static class UserMethodObsoleteMessages
{
    private const string Suffix =
        " generated from System.Identity-[3.2,4.0) (construction-kits.yaml); the REST method keeps working during its REST window. See docs/method-client-migration-guide.md (AB#8416).";

    private const string PasswordSuffix =
        " generated from System.Identity-[3.2,4.0) (construction-kits.yaml); the REST method keeps working during its REST window. See docs/method-client-migration-guide.md (AB#8860).";

    /// <summary>Replacement of <see cref="IIdentityServicesClient.AddRoleToUser" />.</summary>
    public const string AddRoleToUser = "Use RtUserMethodClient.AddRoleAsync" + Suffix;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.RemoveRoleFromUser" />.</summary>
    public const string RemoveRoleFromUser = "Use RtUserMethodClient.RemoveRoleAsync" + Suffix;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateUser" /> (AB#8860).</summary>
    public const string CreateUser = "Use RtUserMethodClient.CreateUserAsync" + PasswordSuffix;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.ResetPassword" /> (AB#8860).</summary>
    public const string ResetPassword = "Use RtUserMethodClient.ResetPasswordAsync" + PasswordSuffix;
}
