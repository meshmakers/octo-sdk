

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Represents a device authentication response
/// </summary>
public class DeviceAuthenticationRequestData
{
    /// <summary>
    ///     Gets the estimated date time of life cycle end of the "device_code" and "user_code".
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Gets the verification URI that includes the "user_code" (or other information with the same function as the
    ///     "user_code"), designed for non-textual transmission.
    /// </summary>
    public string? VerificationUriComplete { get; set; }

    /// <summary>
    ///     Gets the end-user verification URI on the authorization server.The URI should be short and easy to remember as end
    ///     users will be asked to manually type it into their user-agent.
    /// </summary>
    public string? VerificationUri { get; set; }

    /// <summary>
    ///     Gets the device verification code.
    /// </summary>
    public string? DeviceCode { get; set; }

    /// <summary>
    ///     Gets the end-user verification code.
    /// </summary>
    public string? UserCode { get; set; }

    /// <summary>
    ///     Gets the minimum amount of time in seconds that the client SHOULD wait between polling requests to the token
    ///     endpoint. If no value is provided, clients MUST use 5 as the default.
    /// </summary>
    public int PollingInterval { get; set; }
}
