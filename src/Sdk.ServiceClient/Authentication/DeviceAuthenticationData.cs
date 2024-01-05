namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Represents the authentication data for device authentication.
/// </summary>
public class DeviceAuthenticationData : AuthenticationData
{
    /// <summary>
    ///     Returns true, if the device authentication is still pending.
    /// </summary>
    public bool IsAuthenticationPending { get; set; }
}