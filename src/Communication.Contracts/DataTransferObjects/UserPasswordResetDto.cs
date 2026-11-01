namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Request body of the administrator password reset <c>POST {tenant}/v1/users/ResetPassword</c>. The new password
///     travels only in the body; the identity service refuses it as a URL query parameter (it would reach request logs,
///     traces and proxy access logs).
/// </summary>
public class UserPasswordResetDto
{
    /// <summary>
    ///     User name of the user whose password is reset.
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    ///     The new password.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    ///     Names the user only; the password is never part of the text (logs, debugger displays).
    /// </summary>
    public override string ToString() => $"{nameof(UserPasswordResetDto)} {{ UserName = {UserName} }}";
}
