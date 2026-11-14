using System.Reflection;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Sdk.ServiceClient.Tests.IdentityServices;

/// <summary>
///     AB#8416 / AB#8860: the REST user methods that the CK methods of <c>System.Identity/User</c> replace are obsolete
///     on the interface and on the client, and the message names the replacement of the generated <c>RtUserMethodClient</c>.
///     The role methods became obsolete with AB#8416, <c>CreateUser</c> and <c>ResetPassword</c> with AB#8860 (after the
///     first-party consumers moved in AB#8418). The obsolete methods are warnings, not errors: they keep calling REST
///     during their REST window. The SDK has no REST method for SetRoles, Merge, ChangePassword or SetPassword.
/// </summary>
public class UserRestMethodsObsoleteTests
{
    [Theory]
    [InlineData(nameof(IIdentityServicesClient.AddRoleToUser), "RtUserMethodClient.AddRoleAsync")]
    [InlineData(nameof(IIdentityServicesClient.RemoveRoleFromUser), "RtUserMethodClient.RemoveRoleAsync")]
#pragma warning disable CS0618 // nameof of the obsolete members under test
    [InlineData(nameof(IIdentityServicesClient.CreateUser), "RtUserMethodClient.CreateUserAsync")]
    [InlineData(nameof(IIdentityServicesClient.ResetPassword), "RtUserMethodClient.ResetPasswordAsync")]
#pragma warning restore CS0618
    public void MigratedRestMethod_IsObsolete_WithTheReplacement(string method, string replacement)
    {
        foreach (var type in new[] { typeof(IIdentityServicesClient), typeof(IdentityServicesClient) })
        {
            var obsolete = type.GetMethod(method)!.GetCustomAttribute<ObsoleteAttribute>();

            Assert.NotNull(obsolete);
            Assert.False(obsolete.IsError, "the REST method keeps working during its REST window");
            Assert.StartsWith($"Use {replacement} generated from System.Identity-[3.2,4.0)", obsolete.Message);
            Assert.Contains("method-client-migration-guide.md", obsolete.Message);
        }
    }

    [Fact]
    public void OnlyTheMigratedUserMethods_AreObsolete()
    {
        var obsolete = typeof(IIdentityServicesClient).GetMethods()
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() != null)
            .Select(m => m.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["AddRoleToUser", "CreateUser", "RemoveRoleFromUser", "ResetPassword"], obsolete);
    }
}
