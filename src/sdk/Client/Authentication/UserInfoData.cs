using System.Collections.Generic;
using System.Security.Claims;

namespace Meshmakers.Octo.Frontend.Client.Authentication;

public class UserInfoData
{
    public UserInfoData(bool isAuthenticated, IEnumerable<Claim> claims)
    {
        IsAuthenticated = isAuthenticated;
        Claims = claims;
    }

    public IEnumerable<Claim> Claims { get; }
    public bool IsAuthenticated { get; }
}
