namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

public class DeviceAuthenticationData : AuthenticationData
{
    /// <summary>
    ///     Returns true, if the device authentication is still pending.
    /// </summary>
    public bool IsAuthenticationPending { get; set; }
}
