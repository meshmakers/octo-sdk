using System.Reflection;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Sdk.ServiceClient.Tests.IdentityServices;

/// <summary>
///     AB#8416: the REST user methods that the CK methods of <c>System.Identity/User</c> replace are obsolete on the
///     interface and on the client, and the message names the replacement of the generated <c>RtUserMethodClient</c>.
///     Only the role methods are obsolete: <c>CreateUser</c> and <c>ResetPassword</c> stay usable until password methods
///     are enabled on the method bus of every cluster (B0) and become obsolete with AB#8418; the SDK has no REST method
///     for SetRoles, Merge, ChangePassword or SetPassword.
/// </summary>
public class UserRestMethodsObsoleteTests
{
    [Theory]
    [InlineData(nameof(IIdentityServicesClient.AddRoleToUser), "RtUserMethodClient.AddRoleAsync")]
    [InlineData(nameof(IIdentityServicesClient.RemoveRoleFromUser), "RtUserMethodClient.RemoveRoleAsync")]
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

        Assert.Equal(["AddRoleToUser", "RemoveRoleFromUser"], obsolete);
    }

    [Theory]
    [InlineData(nameof(IIdentityServicesClient.CreateUser))]
    [InlineData(nameof(IIdentityServicesClient.ResetPassword))]
    public void PasswordRestMethod_IsNotObsoleteUntilAB8418(string method)
    {
        // Password methods are disabled on the method bus of staging and production until B0; the SDK must not point
        // consumers at a method that fails with METHOD_HANDLER_NOT_AVAILABLE there (AB#8418 obsoletes them).
        foreach (var type in new[] { typeof(IIdentityServicesClient), typeof(IdentityServicesClient) })
        {
            Assert.Null(type.GetMethod(method)!.GetCustomAttribute<ObsoleteAttribute>());
        }
    }
}
