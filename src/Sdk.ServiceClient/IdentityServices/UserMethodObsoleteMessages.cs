namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
///     AB#8416: messages of the <see cref="System.ObsoleteAttribute" /> on the REST user methods of
///     <see cref="IIdentityServicesClient" /> that the CK methods of <c>System.Identity/User</c> replace. The replacement
///     is the method client <c>RtUserMethodClient</c> a consumer generates from <c>System.Identity-[3.2,4.0)</c>
///     (<c>construction-kits.yaml</c>); see <c>docs/method-client-migration-guide.md</c>. The REST methods keep working
///     during their REST window.
/// </summary>
internal static class UserMethodObsoleteMessages
{
    private const string Suffix =
        " generated from System.Identity-[3.2,4.0) (construction-kits.yaml); the REST method keeps working during its REST window. See docs/method-client-migration-guide.md (AB#8416).";

    private const string PasswordGate =
        " Password methods need Methods:Identity:EnablePasswordMethods on the identity service (off by default); keep this REST call where it is off.";

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateUser" /> (password method, bus-gated).</summary>
    public const string CreateUser = "Use RtUserMethodClient.CreateUserAsync" + Suffix + PasswordGate;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.ResetPassword" /> (password method, bus-gated).</summary>
    public const string ResetPassword = "Use RtUserMethodClient.ResetPasswordAsync" + Suffix + PasswordGate;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.AddRoleToUser" />.</summary>
    public const string AddRoleToUser = "Use RtUserMethodClient.AddRoleAsync" + Suffix;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.RemoveRoleFromUser" />.</summary>
    public const string RemoveRoleFromUser = "Use RtUserMethodClient.RemoveRoleAsync" + Suffix;
}
